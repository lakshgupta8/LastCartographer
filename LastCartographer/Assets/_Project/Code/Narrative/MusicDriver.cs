using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The music in the game (AUD-04, AUD-06, docs/design/music.md). It wakes with the game, decides the theme from
    /// the room's region and the boss whose fight has told (the theme arrives with the first telegraph,
    /// audio-direction 4), renders a theme's stems on a worker thread the first time they are wanted, and plays them
    /// in step: one source a stem, all started on the same DSP clock, each at its level times the Music bus's gain
    /// and under the bus's low-pass. A region theme's rests are in its loop; its combat drive fades in while the mix
    /// is in combat. A boss theme's stems enter by phase, each new one on the next bar line; a beaten boss's theme
    /// stops and the roll-call's answer resolves it in the region's key; a reset returns the room's theme.
    /// Region to region is a handover: the next theme's stems are scheduled on the playing theme's next bar line and
    /// the two cross over there, the old going out as the new comes in; nothing is ever cut.
    /// </summary>
    public sealed class MusicDriver : MonoBehaviour
    {
        public static MusicDriver Instance { get; private set; }

        /// <summary>Seconds a stem takes to come in or go (a layer change is a fade from the bar line, not a cut).</summary>
        public const float FadeSeconds = 0.6f;
        /// <summary>Seconds two region themes cross over at the bar line where one hands to the other.</summary>
        public const float CrossfadeSeconds = 2f;
        /// <summary>Lead-in before scheduled stems start, so every source is queued on the same clock.</summary>
        public const double LeadIn = 0.1;

        /// <summary>Tests: the room the music believes it is in, instead of the room manager's.</summary>
        public string RoomOverride { get; set; }

        sealed class Outgoing { public AudioSource Src; public AudioLowPassFilter Filter; public float Level; public double From; public float Seconds; }

        readonly Dictionary<string, Dictionary<string, AudioClip>> _clips = new Dictionary<string, Dictionary<string, AudioClip>>();
        readonly Dictionary<string, Task<Dictionary<string, float[]>>> _rendering = new Dictionary<string, Task<Dictionary<string, float[]>>>();
        readonly Dictionary<string, AudioSource> _sources = new Dictionary<string, AudioSource>();
        readonly Dictionary<string, AudioLowPassFilter> _filters = new Dictionary<string, AudioLowPassFilter>();
        readonly Dictionary<string, float> _levels = new Dictionary<string, float>();
        readonly List<Outgoing> _outgoing = new List<Outgoing>();
        AudioSource _oneShot;
        AudioLowPassFilter _oneShotFilter;
        double _start;
        float _ramp = FadeSeconds;
        int _lastBar = -1;
        Boss _boss;
        int _phase, _pendingPhase;
        double _resolvingUntil;
        AudioClip _resolution;

        /// <summary>The theme playing now (or scheduled on the bar line), or null.</summary>
        public Score.Theme Current { get; private set; }
        /// <summary>The theme the state asks for now (it may still be rendering).</summary>
        public Score.Theme Wanted { get; private set; }
        /// <summary>The DSP time the current theme's stems started, or will start.</summary>
        public double StartAt => _start;
        /// <summary>Stems of a previous theme still going out over their crossfade.</summary>
        public int OutgoingCount => _outgoing.Count;
        /// <summary>The loudest outgoing stem's volume now (0 with none).</summary>
        public float OutgoingVolume => _outgoing.Count == 0 ? 0f : _outgoing.Max(o => o.Src != null ? o.Src.volume : 0f);
        /// <summary>The boss phase the stems are playing, and the one waiting for the bar line.</summary>
        public int Phase => _phase;
        public int PendingPhase => _pendingPhase;
        public bool IsResolving => AudioSettings.dspTime < _resolvingUntil;
        public int BarsPlayed => Current == null ? 0 : Mathf.Max(0, (int)((AudioSettings.dspTime - _start) / Current.BarSeconds));
        public bool IsRendering(Score.Theme t) => t != null && _rendering.ContainsKey(t.Id);
        public bool IsReady(Score.Theme t) => t != null && _clips.ContainsKey(t.Id);
        public AudioSource Source(string stem) => _sources.TryGetValue(stem, out var s) ? s : null;
        public AudioLowPassFilter Filter(string stem) => _filters.TryGetValue(stem, out var f) ? f : null;
        /// <summary>A stem's level now (0..its design level), fading toward its target.</summary>
        public float Level(string stem) => _levels.TryGetValue(stem, out var l) ? l : 0f;
        public Boss Boss => _boss;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("~Music");
            DontDestroyOnLoad(go);
            go.AddComponent<MusicDriver>();
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            _oneShot = MakeSource("Resolution", out _oneShotFilter);
        }

        AudioSource MakeSource(string name, out AudioLowPassFilter filter)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = true;
            src.ignoreListenerPause = true;   // the paused snapshot shapes it, like the mix's own sources
            filter = child.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = Mix.Open;
            return src;
        }

        void OnEnable() { Enemy.Telegraphed += OnTelegraphed; GameState.Loaded += OnNewGame; }
        void OnDisable() { Enemy.Telegraphed -= OnTelegraphed; GameState.Loaded -= OnNewGame; Watch(null); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>The room the music reads: the override, else the room manager's.</summary>
        public string Room => RoomOverride ?? (RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : null);

        void OnNewGame() { Watch(null); _resolvingUntil = 0; }

        void OnTelegraphed(Enemy e, AttackKind kind)
        {
            if (e is Boss b && b.IsFightActive && b != _boss) Watch(b);
        }

        void Watch(Boss b)
        {
            if (_boss != null)
            {
                _boss.PhaseStarted -= OnPhase; _boss.Defeated -= OnDefeated; _boss.FightReset -= OnReset;
            }
            _boss = b;
            _phase = _pendingPhase = b != null ? Mathf.Max(1, b.Phase) : 0;
            if (_boss != null)
            {
                _boss.PhaseStarted += OnPhase; _boss.Defeated += OnDefeated; _boss.FightReset += OnReset;
            }
        }

        void OnPhase(Boss b, int phase, string line) { if (phase > _pendingPhase) _pendingPhase = phase; }   // taken on the next bar line
        void OnReset(Boss b) { Watch(null); }

        void OnDefeated(Boss b)
        {
            var theme = Current;
            Watch(null);
            Stop();
            // The theme resolves on the roll-call's answer, in the boss's region's key (audio-direction 4).
            var region = theme != null ? theme.Region : (Mix.RegionOf(Room) ?? Region.Saltmarrow);
            if (_resolution == null || _resolution.name != "resolution:" + region)
            {
                var s = Score.Resolution(region);
                _resolution = AudioClip.Create("resolution:" + region, s.Length, 1, Score.SampleRate, false);
                _resolution.SetData(s, 0);
            }
            _oneShot.loop = false;
            _oneShot.clip = _resolution;
            _oneShot.Play();
            _resolvingUntil = AudioSettings.dspTime + _resolution.length;
        }

        /// <summary>The theme the state asks for: the fight that told, else the room's region, else nothing; nothing while a theme resolves.</summary>
        public Score.Theme Decide()
        {
            if (IsResolving) return null;
            if (_boss != null && _boss.IsFightActive)
            {
                var boss = Score.ThemeOfBoss(_boss.Family);
                if (boss != null) return boss;
            }
            var r = Mix.RegionOf(Room);
            return r.HasValue ? Score.ThemeOf(r.Value) : null;
        }

        void Update()
        {
            Wanted = Decide();
            if (Wanted != Current)
            {
                if (Wanted == null) Release(FadeSeconds, AudioSettings.dspTime);
                else if (IsReady(Wanted)) Start(Wanted);
                else Request(Wanted);
            }
            Finish();
            Tick();
        }

        void Request(Score.Theme t)
        {
            if (_rendering.ContainsKey(t.Id)) return;
            _rendering[t.Id] = Task.Run(() => Score.Render(t));
        }

        /// <summary>Rendered stems become clips on the main thread.</summary>
        void Finish()
        {
            foreach (var id in _rendering.Keys.ToList())
            {
                var task = _rendering[id];
                if (!task.IsCompleted) continue;
                _rendering.Remove(id);
                if (task.IsFaulted) { Debug.LogWarning("[OWSBG] the music could not render " + id + ": " + task.Exception?.GetBaseException().Message); continue; }
                var clips = new Dictionary<string, AudioClip>();
                foreach (var kv in task.Result)
                {
                    var clip = AudioClip.Create(id + ":" + kv.Key, kv.Value.Length, 1, Score.SampleRate, false);
                    clip.SetData(kv.Value, 0);
                    clips[kv.Key] = clip;
                }
                _clips[id] = clips;
            }
        }

        /// <summary>
        /// Start a theme. Region to region, the new stems are scheduled on the playing theme's next bar line and the
        /// two cross over there; into or out of a boss's theme, the change is now (the fight's theme arrives with the
        /// telegraph), the old stems going out over the short fade.
        /// </summary>
        void Start(Score.Theme t)
        {
            double now = AudioSettings.dspTime;
            bool handover = Current != null && Current.Boss == null && t.Boss == null;
            double at = now + LeadIn;
            float ramp = FadeSeconds;
            if (handover)
            {
                double bar = Current.BarSeconds;
                double elapsed = now + LeadIn - _start;                                   // where the playing theme is, a lead-in ahead
                at = _start + Math.Ceiling(Math.Max(0.0, elapsed) / bar) * bar;           // its next bar line, no sooner than the lead-in
                ramp = CrossfadeSeconds;
            }
            Release(ramp, at);
            Current = t;
            var clips = _clips[t.Id];
            _start = at;
            _ramp = ramp;
            _lastBar = -1;
            foreach (var stem in t.Stems)
            {
                var src = MakeSource("Stem_" + stem.Id, out var f);
                src.clip = clips[stem.Id];
                src.volume = 0f;
                src.PlayScheduled(_start);
                _sources[stem.Id] = src;
                _filters[stem.Id] = f;
                _levels[stem.Id] = 0f;
            }
        }

        /// <summary>The playing stems go out over <paramref name="seconds"/> from <paramref name="from"/> (a bar line, or now).</summary>
        void Release(float seconds, double from)
        {
            foreach (var kv in _sources)
                if (kv.Value != null) _outgoing.Add(new Outgoing { Src = kv.Value, Filter = _filters[kv.Key], Level = _levels[kv.Key], From = from, Seconds = seconds });
            _sources.Clear(); _filters.Clear(); _levels.Clear();
            Current = null;
        }

        /// <summary>Everything stops now (a boss beaten: the answer rings alone).</summary>
        void Stop()
        {
            foreach (var src in _sources.Values) if (src != null) Destroy(src.gameObject);
            foreach (var o in _outgoing) if (o.Src != null) Destroy(o.Src.gameObject);
            _sources.Clear(); _filters.Clear(); _levels.Clear(); _outgoing.Clear();
            Current = null;
        }

        /// <summary>Whether a stem should be heard now: a region's by combat, a boss's by the phase the bar line has reached.</summary>
        public bool StemOn(Score.Stem stem)
        {
            if (Current == null) return false;
            if (Current.Boss != null) return stem.Phase <= _phase;
            if (stem.Combat) return Mix.Live != null && Mix.Live.InCombat;
            return true;
        }

        void Tick()
        {
            float gain = Mix.Live != null ? Mix.Live.Gain(Mix.Bus.Music) : Options.Get(Options.Volume.Master) * Options.Get(Options.Volume.Music);
            float cutoff = Mix.Live != null ? Mix.Live.Cutoff(Mix.Bus.Music) : Mix.Open;
            _oneShot.volume = gain;
            _oneShotFilter.cutoffFrequency = cutoff;
            double now = AudioSettings.dspTime;
            // The outgoing stems hold to their bar line, then go out over their crossfade.
            for (int i = _outgoing.Count - 1; i >= 0; i--)
            {
                var o = _outgoing[i];
                if (o.Src == null) { _outgoing.RemoveAt(i); continue; }
                float k = (float)((now - o.From) / o.Seconds);
                if (k >= 1f) { Destroy(o.Src.gameObject); _outgoing.RemoveAt(i); continue; }
                o.Src.volume = o.Level * (1f - Mathf.Max(0f, k)) * gain;
                o.Filter.cutoffFrequency = cutoff;
            }
            if (Current == null) return;
            // A phase change is taken on the bar line.
            int bar = BarsPlayed;
            if (bar != _lastBar) { _lastBar = bar; if (_pendingPhase > _phase) _phase = _pendingPhase; }
            bool begun = now >= _start;
            if (_ramp != FadeSeconds && now > _start + _ramp) _ramp = FadeSeconds;    // the crossfade over, layers move at the short fade again
            float step = Time.unscaledDeltaTime / _ramp;
            foreach (var stem in Current.Stems)
            {
                float target = begun && StemOn(stem) ? stem.Level : 0f;
                float level = Mathf.MoveTowards(_levels[stem.Id], target, step * Mathf.Max(0.01f, stem.Level));
                _levels[stem.Id] = level;
                var src = _sources[stem.Id];
                src.volume = level * gain;
                _filters[stem.Id].cutoffFrequency = cutoff;
            }
        }
    }
}
