using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The ambience in the game (AUD-05, docs/design/ambience.md). It wakes with the game, reads the room's region,
    /// renders that region's layers once on a worker thread, and plays each on its own looping source (loops of
    /// different lengths, started together, drifting apart) at its level times the Ambience bus's gain, under the
    /// bus's low-pass (which the mix already dulls by the place's fade stage). The stage also takes the layers away,
    /// last first, each fading over a second and a half; an erased place has none. Another region's room is a crossfade:
    /// the old layers go out over the same fade as the new come in. Nothing is loaded from disk.
    /// </summary>
    public sealed class AmbienceDriver : MonoBehaviour
    {
        public static AmbienceDriver Instance { get; private set; }
        public const float FadeSeconds = 1.5f;
        /// <summary>A point layer is full within this many units and gone past the room's width (or this span, with no room).</summary>
        public const float PointNear = 3f, DefaultSpan = 16f;

        /// <summary>Tests: the room the ambience believes it is in, instead of the room manager's.</summary>
        public string RoomOverride { get; set; }

        readonly Dictionary<Region, Dictionary<string, AudioClip>> _clips = new Dictionary<Region, Dictionary<string, AudioClip>>();
        readonly Dictionary<Region, Task<Dictionary<string, float[]>>> _rendering = new Dictionary<Region, Task<Dictionary<string, float[]>>>();
        readonly Dictionary<string, AudioSource> _sources = new Dictionary<string, AudioSource>();
        readonly Dictionary<string, AudioLowPassFilter> _filters = new Dictionary<string, AudioLowPassFilter>();
        readonly Dictionary<string, float> _levels = new Dictionary<string, float>();
        readonly Dictionary<string, Vector2> _points = new Dictionary<string, Vector2>();
        sealed class Outgoing { public AudioSource Src; public AudioLowPassFilter Filter; public float Level; public double From; }
        readonly List<Outgoing> _outgoing = new List<Outgoing>();
        AudioListener _listener;

        /// <summary>Layers of a previous region still going out over the fade.</summary>
        public int OutgoingCount => _outgoing.Count;
        /// <summary>The loudest outgoing layer's volume now (0 with none).</summary>
        public float OutgoingVolume => _outgoing.Count == 0 ? 0f : _outgoing.Max(o => o.Src != null ? o.Src.volume : 0f);

        public Region? Current { get; private set; }
        /// <summary>Where a point layer sits in the room (null for a layer heard from everywhere).</summary>
        public Vector2? Position(string layer) => _points.TryGetValue(layer, out var p) ? p : (Vector2?)null;
        public Region? Wanted { get; private set; }
        public bool IsReady(Region r) => _clips.ContainsKey(r);
        public bool IsRendering(Region r) => _rendering.ContainsKey(r);
        public AudioSource Source(string layer) => _sources.TryGetValue(layer, out var s) ? s : null;
        public AudioLowPassFilter Filter(string layer) => _filters.TryGetValue(layer, out var f) ? f : null;
        public float Level(string layer) => _levels.TryGetValue(layer, out var l) ? l : 0f;
        /// <summary>How many layers the place's stage leaves (0 once erased).</summary>
        public int LayersLeft => Current.HasValue ? AudioDirection.AmbienceLayersAt(Current.Value, Stage) : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("~Ambience");
            DontDestroyOnLoad(go);
            go.AddComponent<AmbienceDriver>();
        }

        void Awake() { if (Instance == null) Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public string Room => RoomOverride ?? (RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : null);

        /// <summary>The fade stage of the place the room draws (the mix's reading, so the filter and the layers agree).</summary>
        public int Stage
        {
            get
            {
                string room = Room;
                if (string.IsNullOrEmpty(room)) return 0;
                string place = room.StartsWith(WorldGraph.GreyboxPrefix, System.StringComparison.Ordinal) ? room.Substring(WorldGraph.GreyboxPrefix.Length) : room;
                try { return FadeStages.Get(GameState.World, place); } catch { return 0; }
            }
        }

        void Update()
        {
            Wanted = Mix.RegionOf(Room);
            if (Wanted != Current)
            {
                if (!Wanted.HasValue) Release();
                else if (IsReady(Wanted.Value)) Start(Wanted.Value);
                else Request(Wanted.Value);
            }
            Finish();
            Tick();
        }

        void Request(Region r)
        {
            if (_rendering.ContainsKey(r)) return;
            var layers = Ambience.Of(r);
            _rendering[r] = Task.Run(() => layers.ToDictionary(l => l.Name, l => Ambience.Render(l)));
        }

        void Finish()
        {
            foreach (var r in _rendering.Keys.ToList())
            {
                var task = _rendering[r];
                if (!task.IsCompleted) continue;
                _rendering.Remove(r);
                if (task.IsFaulted) { Debug.LogWarning("[OWSBG] the ambience could not render " + r + ": " + task.Exception?.GetBaseException().Message); continue; }
                var clips = new Dictionary<string, AudioClip>();
                foreach (var kv in task.Result)
                {
                    var clip = AudioClip.Create(r + ":" + kv.Key, kv.Value.Length, 1, Ambience.SampleRate, false);
                    clip.SetData(kv.Value, 0);
                    clips[kv.Key] = clip;
                }
                _clips[r] = clips;
            }
        }

        void Start(Region r)
        {
            Release();
            Current = r;
            var clips = _clips[r];
            double at = AudioSettings.dspTime + 0.05;
            // The room's span, for the point layers: its camera bounds, else a span around the listener.
            var here = World.Room.Current;   // the room object, not this driver's room name
            var bounds = here != null && here.CameraBounds != null ? here.CameraBounds.bounds : (Bounds?)null;
            float minX = bounds?.min.x ?? -DefaultSpan / 2f, maxX = bounds?.max.x ?? DefaultSpan / 2f;
            float minY = bounds?.min.y ?? 0f, maxY = bounds?.max.y ?? 8f;
            foreach (var layer in Ambience.Of(r))
            {
                var child = new GameObject("Layer_" + layer.Slug);
                child.transform.SetParent(transform, false);
                var src = child.AddComponent<AudioSource>();
                src.playOnAwake = false; src.spatialBlend = 0f; src.loop = true; src.ignoreListenerPause = true;
                src.clip = clips[layer.Name];
                src.volume = 0f;
                if (layer.Point)
                {
                    var (x, y) = Ambience.PointIn(layer, Room, minX, maxX, minY, maxY);
                    _points[layer.Name] = new Vector2(x, y);
                    child.transform.position = new Vector3(x, y, ListenerZ);
                    src.spatialBlend = 1f;
                    src.rolloffMode = AudioRolloffMode.Linear;
                    src.minDistance = PointNear;
                    src.maxDistance = Mathf.Max(12f, (maxX - minX) * 0.9f);
                    src.dopplerLevel = 0f;
                    src.spread = 30f;
                }
                src.PlayScheduled(at);
                var f = child.AddComponent<AudioLowPassFilter>();
                f.cutoffFrequency = Mix.Open;
                _sources[layer.Name] = src; _filters[layer.Name] = f; _levels[layer.Name] = 0f;
            }
        }

        /// <summary>The playing layers go out over the fade from now (a region change is a crossfade, not a cut).</summary>
        void Release()
        {
            double now = AudioSettings.dspTime;
            foreach (var kv in _sources)
                if (kv.Value != null) _outgoing.Add(new Outgoing { Src = kv.Value, Filter = _filters[kv.Key], Level = _levels[kv.Key], From = now });
            _sources.Clear(); _filters.Clear(); _levels.Clear(); _points.Clear();
            Current = null;
        }

        /// <summary>The listener's depth, so a point layer's distance is measured in the room's plane, not to the camera.</summary>
        float ListenerZ
        {
            get
            {
                if (_listener == null) _listener = FindFirstObjectByType<AudioListener>();
                return _listener != null ? _listener.transform.position.z : 0f;
            }
        }

        void Tick()
        {
            float gain = Mix.Live != null ? Mix.Live.Gain(Mix.Bus.Ambience) : Options.Get(Options.Volume.Master) * Options.Get(Options.Volume.Sound);
            float cutoff = Mix.Live != null ? Mix.Live.Cutoff(Mix.Bus.Ambience) : Mix.StageCutoff(Stage);
            double now = AudioSettings.dspTime;
            for (int i = _outgoing.Count - 1; i >= 0; i--)
            {
                var o = _outgoing[i];
                if (o.Src == null) { _outgoing.RemoveAt(i); continue; }
                float k = (float)((now - o.From) / FadeSeconds);
                if (k >= 1f) { Destroy(o.Src.gameObject); _outgoing.RemoveAt(i); continue; }
                o.Src.volume = o.Level * (1f - k) * gain;
                o.Filter.cutoffFrequency = cutoff;
            }
            if (!Current.HasValue) return;
            var targets = Ambience.LevelsAt(Current.Value, Stage);
            var layers = Ambience.Of(Current.Value);
            float step = Time.unscaledDeltaTime / FadeSeconds;
            float z = ListenerZ;
            for (int i = 0; i < layers.Count; i++)
            {
                var l = layers[i];
                float level = Mathf.MoveTowards(_levels[l.Name], targets[i], step * l.Level);
                _levels[l.Name] = level;
                var src = _sources[l.Name];
                src.volume = level * gain;
                _filters[l.Name].cutoffFrequency = cutoff;
                if (_points.TryGetValue(l.Name, out var p)) src.transform.position = new Vector3(p.x, p.y, z);
            }
        }
    }
}
