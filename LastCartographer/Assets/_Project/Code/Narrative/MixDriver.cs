using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The mix running in the game (AUD-09): one <see cref="AudioSource"/> per bus, the <see cref="Mixer"/> ticked with
    /// unscaled time, and the snapshot chosen from the state each frame: paused, a boss, a conversation, the Blank,
    /// combat, else the room as designed. Music and ambience sit under a low-pass the snapshot and the place's fade
    /// stage set. It wakes with the game and lives across rooms; the music and ambience rows hand it their clips
    /// through <see cref="Play"/> and <see cref="Loop"/>.
    /// </summary>
    public sealed class MixDriver : MonoBehaviour
    {
        public static MixDriver Instance { get; private set; }

        public Mixer Mixer { get; private set; }
        /// <summary>Tests: the room the mix believes it is in, instead of the room manager's.</summary>
        public string RoomOverride { get; set; }

        readonly AudioSource[] _sources = new AudioSource[Mix.BusCount];
        readonly AudioLowPassFilter[] _filters = new AudioLowPassFilter[Mix.BusCount];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("~Mix");
            DontDestroyOnLoad(go);
            go.AddComponent<MixDriver>();
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            Mixer = new Mixer();
            foreach (var bus in Mix.Buses)
            {
                if (bus == Mix.Bus.Master) continue;
                var child = new GameObject(bus.ToString());
                child.transform.SetParent(transform, false);
                var src = child.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.loop = bus == Mix.Bus.Music || bus == Mix.Bus.Ambience;
                src.ignoreListenerPause = true;   // the paused snapshot is the mix's to shape, not a silence
                _sources[(int)bus] = src;
                if (bus == Mix.Bus.Music || bus == Mix.Bus.Ambience)
                {
                    var f = child.AddComponent<AudioLowPassFilter>();
                    f.cutoffFrequency = Mix.Open;
                    _filters[(int)bus] = f;
                }
            }
            if (Instance == this) Mix.Live = Mixer;
            Apply();
        }

        void OnDestroy()
        {
            if (Mix.Live == Mixer) Mix.Live = null;
            if (Instance == this) Instance = null;
        }

        public AudioSource Source(Mix.Bus bus) => _sources[(int)bus];
        public AudioLowPassFilter Filter(Mix.Bus bus) => _filters[(int)bus];

        /// <summary>The room the mix reads: the override, else the room manager's.</summary>
        public string Room => RoomOverride ?? (RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : null);

        /// <summary>The snapshot the state asks for now, most pressing first.</summary>
        public Mix.Snapshot Decide()
        {
            if (Pause.Active) return Mix.Snapshot.Paused;
            if (Boss.FightsActive > 0) return Mix.Snapshot.Boss;
            if (DialogueService.Instance != null && DialogueService.Instance.IsRunning) return Mix.Snapshot.Dialogue;
            if (Mix.RegionOf(Room) == Region.Blank) return Mix.Snapshot.Blank;
            if (Mixer.InCombat) return Mix.Snapshot.Combat;
            return Mix.Snapshot.Explore;
        }

        /// <summary>The fade stage of the place the room draws, for the ambience's filter; 0 where nothing is known.</summary>
        public int StageOfRoom()
        {
            string place = Mix.PlaceOf(Room);
            if (string.IsNullOrEmpty(place)) return 0;
            try { return FadeStages.Get(GameState.World, place); } catch { return 0; }
        }

        void Update()
        {
            Mixer.Go(Decide());
            Mixer.Stage = StageOfRoom();
            Mixer.Tick(Time.unscaledDeltaTime);
            Apply();
        }

        void Apply()
        {
            foreach (var bus in Mix.Buses)
            {
                var src = _sources[(int)bus];
                if (src == null) continue;
                src.volume = Mixer.Gain(bus);
                var f = _filters[(int)bus];
                if (f != null) f.cutoffFrequency = Mixer.Cutoff(bus);
            }
        }

        /// <summary>Play a clip once on a bus (a strike, a line, a page turn).</summary>
        public void Play(Mix.Bus bus, AudioClip clip, float volume = 1f)
        {
            var src = _sources[(int)bus];
            if (src == null || clip == null) return;
            src.PlayOneShot(clip, volume);
        }

        /// <summary>Loop a clip on a bus (a theme, a room's ambience); null stops it.</summary>
        public void Loop(Mix.Bus bus, AudioClip clip)
        {
            var src = _sources[(int)bus];
            if (src == null) return;
            if (clip == null) { src.Stop(); src.clip = null; return; }
            if (src.clip == clip && src.isPlaying) return;
            src.clip = clip;
            src.loop = true;
            src.Play();
        }
    }
}
