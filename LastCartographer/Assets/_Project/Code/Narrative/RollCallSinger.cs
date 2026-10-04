using System;
using System.Collections.Generic;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The roll-call sung in the game (AUD-02, docs/design/roll-call.md). It wakes with the game like the mix, turns
    /// <see cref="RollCallSong"/>'s renders into clips as they are first asked for, and sings on the Dialogue bus's
    /// gain (the mix's rule: the roll-call's names are voices). The bounds-walk is sung live: every name called is the
    /// call at the walk's beat in the walk's chorus, a miss is the chorus faltering, and a verse's end is the answer at
    /// the region's beat. The whale sings when the Reedmother is drawn. Scripts ask for the other uses with
    /// &lt;&lt;sing use&gt;&gt; (whale, runa, dotha, chorus, blank, archivist); the true ending's chorus is whoever was met.
    /// </summary>
    public sealed class RollCallSinger : MonoBehaviour
    {
        public static RollCallSinger Instance { get; private set; }

        /// <summary>The vantage whose drawing the whale sings under (the Reedmother's roots look over the Bone Bridge).</summary>
        public const string WhaleVantage = "Saltmarrow_A/Reedmother";
        /// <summary>And on the Bone Bridge itself, once it is built (ENV-03): drawing the whale's bones.</summary>
        public static readonly string[] WhaleVantages = { WhaleVantage, "Saltmarrow_BoneBridge/Whale" };
        /// <summary>Seconds after the drawing before the whale is heard: the "Drawn:" caption first.</summary>
        public const float WhaleDelay = 1.5f;

        AudioSource _call, _voice;
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        WorldState _bound;
        float _whaleIn = -1f;

        /// <summary>How many times anything was sung (tests).</summary>
        public int Sung { get; private set; }
        /// <summary>The last thing sung: a use's id, "call", "falter" or "answer".</summary>
        public string Last { get; private set; }
        public AudioClip LastClip { get; private set; }
        /// <summary>The walk's chorus is faltering on a missed beat.</summary>
        public bool Faltered { get; private set; }
        /// <summary>The clip the walk's caller is singing now, or null between calls.</summary>
        public AudioClip Calling => _call != null && _call.isPlaying ? _call.clip : null;
        public bool VoiceIsPlaying => _voice != null && _voice.isPlaying;
        public float Volume => _voice != null ? _voice.volume : 0f;
        public int Cached => _clips.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("~RollCall");
            DontDestroyOnLoad(go);
            go.AddComponent<RollCallSinger>();
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            _call = Source("Caller");
            _voice = Source("Voices");
        }

        AudioSource Source(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = false;
            return src;
        }

        void OnEnable()
        {
            BoundsWalk.NameCalled += OnCalled;
            BoundsWalk.BeatLanded += OnLanded;
            BoundsWalk.VerseDone += OnVerseDone;
            BoundsWalk.VerseRestarted += OnRestarted;
            GameState.Loaded += Rebind;
            Rebind();
        }

        void OnDisable()
        {
            BoundsWalk.NameCalled -= OnCalled;
            BoundsWalk.BeatLanded -= OnLanded;
            BoundsWalk.VerseDone -= OnVerseDone;
            BoundsWalk.VerseRestarted -= OnRestarted;
            GameState.Loaded -= Rebind;
            if (_bound != null) { _bound.VantageSurveyed -= OnSurveyed; _bound = null; }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Rebind()
        {
            if (_bound == GameState.World) return;
            if (_bound != null) _bound.VantageSurveyed -= OnSurveyed;
            _bound = GameState.World;
            if (_bound != null) _bound.VantageSurveyed += OnSurveyed;
            Hush();   // a new game or a load: whatever was about to be sung belonged to the old one
        }

        /// <summary>Stop everything being sung and forget a pending whale (a new game, a load, the tests).</summary>
        public void Hush()
        {
            _whaleIn = -1f;
            Faltered = false;
            if (_call != null) _call.Stop();
            if (_voice != null) _voice.Stop();
        }

        void Update()
        {
            if (_bound != GameState.World) Rebind();
            // The voices ride the Dialogue bus: its gain is theirs, snapshot, ducks and the player's volumes included.
            float gain = MixDriver.Instance != null ? MixDriver.Instance.Mixer.Gain(Mix.Bus.Dialogue) : Options.Get(Options.Volume.Master) * Options.Get(Options.Volume.Voices);
            _call.volume = gain;
            _voice.volume = gain;
            if (_whaleIn >= 0f)
            {
                _whaleIn -= Time.deltaTime;
                if (_whaleIn < 0f) Sing("whale");
            }
        }

        // ---- clips ----

        AudioClip Clip(string key, Func<RollCallSong.Line> render)
        {
            if (_clips.TryGetValue(key, out var clip) && clip != null) return clip;
            var line = render();
            clip = AudioClip.Create(key, line.Samples.Length, 1, RollCallSong.SampleRate, false);
            clip.SetData(line.Samples, 0);
            _clips[key] = clip;
            return clip;
        }

        static string Key(string what, RollCallSong.Form form, Region key, float beat, IList<string> voices, int names = 0, int falter = -1) =>
            what + ":" + form + ":" + key + ":" + beat.ToString("0.###") + ":" + names + ":" + falter + ":" + string.Join(",", voices);

        AudioClip Make(string what, RollCallSong.Form form, Region key, float beat, IList<string> voices, int names = 1, int falter = -1) =>
            Clip(Key(what, form, key, beat, voices, names, falter), () => RollCallSong.Chorus(RollCallSong.Notes(form, names), RollCallSong.TonicHz(key), beat, voices, falter));

        // ---- the uses ----

        /// <summary>Sing one of the roll-call's uses now (a script's &lt;&lt;sing use&gt;&gt;). False for a use that does not exist.</summary>
        public bool Sing(string use)
        {
            var u = RollCallSong.UseOf(use);
            if (u == null) return false;
            var voices = RollCallSong.VoicesFor(u, GameState.World);
            int names = RollCallSong.NamesFor(u, voices);
            var clip = Make(u.Id, u.Form, u.Key, AudioDirection.BeatOf(u.Tempo), voices, names);
            _voice.PlayOneShot(clip, u.Gain);
            Last = u.Id; LastClip = clip; Sung++;
            Captions.Show(Loc.T("caption.sing." + u.Id, u.Caption), Mathf.Clamp(clip.length, 2.5f, 6f));
            return true;
        }

        void OnSurveyed(string vantageId)
        {
            if (System.Array.IndexOf(WhaleVantages, vantageId) >= 0) _whaleIn = WhaleDelay;
        }

        // ---- the walk ----

        /// <summary>The region a walk sings in: its place's, else the coast's.</summary>
        public static Region RegionOf(BoundsWalk walk) => Mix.RegionOf(walk.PlaceId) ?? Region.Saltmarrow;

        void OnCalled(BoundsWalk walk, BoundsWalk.Bound bound)
        {
            var chorus = RollCallSong.WalkChorus(walk.Id, walk.WholeVerse);
            var clip = Make("call", RollCallSong.Form.WalkCall, RegionOf(walk), walk.SecondsPerBeat, chorus);
            _call.Stop();
            _call.clip = clip;
            _call.Play();
            Faltered = false;
            Last = "call"; LastClip = clip; Sung++;
        }

        void OnLanded(BoundsWalk walk, BoundsWalk.Bound bound, bool hit)
        {
            if (hit) return;
            // The name should land now and she is not there: the chorus falters on it.
            var chorus = RollCallSong.WalkChorus(walk.Id, walk.WholeVerse);
            var clip = Make("falter", RollCallSong.Form.Name, RegionOf(walk), walk.SecondsPerBeat, chorus, 1, 0);
            _call.Stop();
            _call.clip = clip;
            _call.Play();
            Faltered = true;
            Last = "falter"; LastClip = clip; Sung++;
        }

        void OnVerseDone(BoundsWalk walk, int verse)
        {
            // The last name was on the reciting tone; the answer starts there and comes home, in the region's own time.
            var chorus = RollCallSong.WalkChorus(walk.Id, walk.WholeVerse);
            var clip = Make("answer", RollCallSong.Form.Answer, RegionOf(walk), AudioDirection.BeatOf(RegionOf(walk)), chorus);
            _call.Stop();
            _voice.PlayOneShot(clip);
            Faltered = false;
            Last = "answer"; LastClip = clip; Sung++;
        }

        void OnRestarted(BoundsWalk walk)
        {
            _call.Stop();
            Faltered = false;
        }
    }
}
