using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The ink sounds in the game (AUD-03, AUD-10): wakes with the game, turns <see cref="InkSounds"/>' cues into clips the
    /// first time they are asked for, and plays them at the Sfx bus's gain (the mix's snapshots and the player's
    /// Sounds volume apply; no duck touches the Sfx bus, so the tells are never lowered). The world's own sounds hook
    /// here: an enemy's death is the kill layer, and every telegraph (<see cref="Enemy.Telegraphed"/>) is the tell
    /// for its kind, on the telegraph's first frame. Wren's are <see cref="WrenSounds"/>', the enemies' <see cref="EnemyVoice"/>'s.
    /// At most <see cref="AudioDirection.VoiceLimit"/> one-shots sound at once (audio-direction 4): each plays on its own
    /// source, and when they are all taken the one that matters least (<see cref="AudioDirection.Priority"/>, the oldest
    /// of its rank) is stopped for a newcomer that matters more, or the newcomer is the one dropped. A sound given a
    /// place in the room is panned toward it and quieter past the screen's edge (<see cref="InkSounds.Pan"/>).
    /// </summary>
    public sealed class InkSoundBank : MonoBehaviour
    {
        public static InkSoundBank Instance { get; private set; }

        AudioSource _loop;
        readonly AudioSource[] _pool = new AudioSource[AudioDirection.VoiceLimit];
        readonly AudioDirection.Voice[] _rank = new AudioDirection.Voice[AudioDirection.VoiceLimit];
        readonly float[] _since = new float[AudioDirection.VoiceLimit];
        readonly float[] _level = new float[AudioDirection.VoiceLimit];
        readonly bool[] _ui = new bool[AudioDirection.VoiceLimit];
        float _uiGain;
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        readonly List<string> _recent = new List<string>(RecentKept);
        const int RecentKept = 32;
        float _gain;
        Camera _cam;
        AudioListener _listener;
        float _nextLook;

        public int Played { get; private set; }
        /// <summary>One-shots refused because everything sounding mattered more.</summary>
        public int Dropped { get; private set; }
        public string Last { get; private set; }
        /// <summary>The last cues played, oldest first (the tests read the order).</summary>
        public IReadOnlyList<string> Recent => _recent;
        public string Looping => _loop != null && _loop.isPlaying && _loop.clip != null ? _loop.clip.name : null;
        public int Cached => _clips.Count;
        /// <summary>The Sfx bus's gain, which every sound in the room is under.</summary>
        public float Volume => _gain;
        /// <summary>The Ui bus's gain, which the pages' sounds are under: whole while the game is paused (AUD-11).</summary>
        public float UiVolume => _uiGain;
        /// <summary>The pitch the last sound was played at.</summary>
        public float LastPitch { get; private set; } = 1f;
        /// <summary>How many one-shots are sounding now.</summary>
        public int Active { get { int n = 0; for (int i = 0; i < _pool.Length; i++) if (_pool[i] != null && _pool[i].isPlaying) n++; return n; } }
        /// <summary>Where the last placed sound stood: its pan, and how much of its gain the distance left it.</summary>
        public float LastPan { get; private set; }
        public float LastGain { get; private set; } = 1f;
        /// <summary>The last tell played, and for whom.</summary>
        public AudioDirection.Tell? LastTell { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("~Sounds");
            DontDestroyOnLoad(go);
            go.AddComponent<InkSoundBank>();
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            for (int i = 0; i < _pool.Length; i++) _pool[i] = Source("Voice " + (i + 1), false);
            _loop = Source("Loop", true);
        }

        AudioSource Source(string name, bool loop)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = loop;
            return src;
        }

        void OnEnable()
        {
            Enemy.AnyDied += OnEnemyDied;
            Enemy.Telegraphed += OnTelegraphed;
        }

        void OnDisable()
        {
            Enemy.AnyDied -= OnEnemyDied;
            Enemy.Telegraphed -= OnTelegraphed;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            _gain = Mix.Live != null ? Mix.Live.Gain(Mix.Bus.Sfx) : Options.Get(Options.Volume.Master) * Options.Get(Options.Volume.Sound);
            _uiGain = Mix.Live != null ? Mix.Live.Gain(Mix.Bus.Ui) : Options.Get(Options.Volume.Master) * Options.Get(Options.Volume.Sound);
            for (int i = 0; i < _pool.Length; i++)
                if (_pool[i].isPlaying) _pool[i].volume = (_ui[i] ? _uiGain : _gain) * _level[i];
            _loop.volume = _gain;
        }

        public AudioClip Clip(string id)
        {
            if (_clips.TryGetValue(id, out var clip) && clip != null) return clip;
            var samples = InkSounds.Render(id);
            if (samples == null) return null;
            clip = AudioClip.Create(id, samples.Length, 1, InkSounds.SampleRate, false);
            clip.SetData(samples, 0);
            _clips[id] = clip;
            return clip;
        }

        // ---- where a sound stands ----

        /// <summary>The listener's place along the room; 0 without one.</summary>
        public float ListenerX
        {
            get
            {
                Look();
                return _listener != null ? _listener.transform.position.x : 0f;
            }
        }

        /// <summary>Half the screen's width in the play plane: the camera's at the plane, or nine units without one.</summary>
        public float HalfWidth
        {
            get
            {
                Look();
                if (_cam == null) return 9f;
                if (_cam.orthographic) return _cam.orthographicSize * _cam.aspect;
                float dist = Mathf.Abs(_cam.transform.position.z);
                return Mathf.Max(1f, dist * Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * _cam.aspect);
            }
        }

        void Look()
        {
            if ((_cam != null && _listener != null) || Time.unscaledTime < _nextLook) return;
            _nextLook = Time.unscaledTime + 1f;   // a room with no camera is not asked every frame
            if (_cam == null) _cam = Camera.main;
            if (_listener == null) _listener = FindFirstObjectByType<AudioListener>();
        }

        /// <summary>A sound at <paramref name="at"/>: its pan toward where it stands, and how much of its gain the distance leaves.</summary>
        public (float pan, float gain) Place(Vector2 at)
        {
            float dx = at.x - ListenerX, w = HalfWidth;
            return (InkSounds.Pan(dx, w), InkSounds.Falloff(dx, w));
        }

        // ---- playing ----

        /// <summary>Play a cue once from nowhere in particular; a cue that does not exist is a no-op. Returns the clip played.</summary>
        public static AudioClip Play(string id, float volume = 1f) => Play(id, volume, null);

        /// <summary>Play a cue once from a place in the room (panned, and quieter past the screen), at a pitch; null if it was dropped or too far.</summary>
        public static AudioClip Play(string id, float volume, Vector2? at, float pitch = 1f)
        {
            var bank = Instance;
            if (bank == null) return null;
            var cue = InkSounds.Of(id);
            if (cue == null) return null;
            var clip = bank.Clip(id);
            if (clip == null) return null;
            float pan = 0f, gain = 1f;
            if (at.HasValue)
            {
                (pan, gain) = bank.Place(at.Value);
                if (gain <= 0f) return null;   // three screens away: not heard, not counted
            }
            var src = bank.Take(cue.Voice, out int slot);
            if (src == null) { bank.Dropped++; return null; }
            bank._level[slot] = volume * cue.Gain * gain;
            bank._ui[slot] = cue.Bus == Mix.Bus.Ui;
            src.clip = clip;
            src.panStereo = pan;
            src.pitch = pitch;
            src.volume = (bank._ui[slot] ? bank._uiGain : bank._gain) * bank._level[slot];
            src.Play();
            bank.Played++;
            bank.Last = id;
            bank.LastPan = pan;
            bank.LastGain = gain;
            bank.LastPitch = pitch;
            bank.Note(id);
            return clip;
        }

        /// <summary>A free source, or the one playing that matters least if the newcomer matters more; null when the newcomer is the one to drop.</summary>
        AudioSource Take(AudioDirection.Voice rank, out int slot)
        {
            int worst = -1;
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].isPlaying) { _rank[i] = rank; _since[i] = Time.unscaledTime; slot = i; return _pool[i]; }
                if (worst < 0 || _rank[i] > _rank[worst] || (_rank[i] == _rank[worst] && _since[i] < _since[worst])) worst = i;
            }
            if (_rank[worst] < rank) { slot = -1; return null; }   // everything sounding is kept before this: dropped from the bottom
            _pool[worst].Stop();
            _rank[worst] = rank;
            _since[worst] = Time.unscaledTime;
            slot = worst;
            return _pool[worst];
        }

        void Note(string id)
        {
            if (_recent.Count == RecentKept) _recent.RemoveAt(0);
            _recent.Add(id);
        }

        /// <summary>Loop a cue while something is held (the survey); null stops it.</summary>
        public static void Loop(string id)
        {
            var bank = Instance;
            if (bank == null) return;
            if (id == null) { if (bank._loop.isPlaying) bank._loop.Stop(); bank._loop.clip = null; return; }
            var clip = bank.Clip(id);
            if (clip == null) return;
            if (bank._loop.clip == clip && bank._loop.isPlaying) return;
            bank._loop.clip = clip;
            bank._loop.Play();
            bank.Played++;
            bank.Last = id;
            bank.Note(id);
        }

        /// <summary>The tell for an attack kind, by the direction's name for it.</summary>
        public static AudioClip Tell(AttackKind kind)
        {
            var t = AudioDirection.TellOf(kind.ToString());
            if (Instance != null) Instance.LastTell = t;
            return Play(InkSounds.TellCue(t));
        }

        void OnEnemyDied(Enemy e) => Play("kill");
        void OnTelegraphed(Enemy e, AttackKind kind) => Tell(kind);

        /// <summary>Stop everything (a new game, the tests).</summary>
        public void Hush()
        {
            for (int i = 0; i < _pool.Length; i++) if (_pool[i] != null) _pool[i].Stop();
            if (_loop != null) { _loop.Stop(); _loop.clip = null; }
            _recent.Clear();
            LastTell = null;
            Dropped = 0;
        }
    }
}
