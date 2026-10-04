using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Keeps a test round's session (PRO-04, docs/design/playtest-round1.md) while a tester plays the slice: every room
    /// entered with the seconds and the deaths in it, the boss fights started and won, the nodes read, the vantages
    /// surveyed, the bug reports filed (F12), the exceptions, the options changed, and the longest stretch with no new
    /// room. The game is played as anyone would play it; the recorder only listens.
    /// </summary>
    public sealed class PlaytestRecorder : MonoBehaviour
    {
        public string Tester { get; set; } = "";
        public Playtest.Session Session { get; } = new Playtest.Session();
        /// <summary>The slice's boss fell: the round's finish line, said once.</summary>
        public event Action Finished;

        readonly List<string> _rooms = new List<string>();
        readonly List<float> _roomSeconds = new List<float>();
        readonly List<int> _roomDeaths = new List<int>();
        readonly List<string> _bosses = new List<string>();
        readonly List<int> _attempts = new List<int>();
        readonly List<bool> _won = new List<bool>();
        string _room;
        float _sinceNew;
        RoomManager _rm;
        WrenVitals _vitals;
        DialogueService _dialogue;
        WorldState _world;

        public string Room => _room;
        public float SinceNewRoom => _sinceNew;

        void OnEnable()
        {
            BossArena.FightStarted += OnFightStarted;
            BossArena.FightWon += OnFightWon;
            BugReporter.Saved += OnBugReport;
            LogTail.ExceptionLogged += OnException;
            Options.Changed += OnOptions;
            Session.started = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        void OnDisable()
        {
            BossArena.FightStarted -= OnFightStarted;
            BossArena.FightWon -= OnFightWon;
            BugReporter.Saved -= OnBugReport;
            LogTail.ExceptionLogged -= OnException;
            Options.Changed -= OnOptions;
            if (_rm != null) _rm.RoomChanged -= OnRoom;
            if (_vitals != null) _vitals.Died -= OnDied;
            if (_dialogue != null) _dialogue.NodeStarted -= OnNode;
            if (_world != null) _world.VantageSurveyed -= OnSurveyed;
        }

        /// <summary>Find what to listen to as it appears: the room manager, Wren's vitals, the dialogue, the world.</summary>
        void Bind()
        {
            if (_rm == null && RoomManager.Instance != null)
            {
                _rm = RoomManager.Instance;
                _rm.RoomChanged += OnRoom;
                if (!string.IsNullOrEmpty(_rm.CurrentRoom)) OnRoom(_rm.CurrentRoom);
            }
            var wren = WrenController.Current;
            var vitals = wren != null ? wren.GetComponent<WrenVitals>() : null;
            if (vitals != _vitals)
            {
                if (_vitals != null) _vitals.Died -= OnDied;
                _vitals = vitals;
                if (_vitals != null) _vitals.Died += OnDied;
            }
            if (_dialogue == null && DialogueService.Instance != null)
            {
                _dialogue = DialogueService.Instance;
                _dialogue.NodeStarted += OnNode;
            }
            if (_world != GameState.World)   // a load replaces the world
            {
                if (_world != null) _world.VantageSurveyed -= OnSurveyed;
                _world = GameState.World;
                if (_world != null) _world.VantageSurveyed += OnSurveyed;
            }
        }

        void Update()
        {
            Bind();
            float dt = Time.unscaledDeltaTime;
            Session.seconds += dt;
            _sinceNew += dt;
            if (_sinceNew > Session.longestLost) { Session.longestLost = _sinceNew; Session.longestLostIn = _room ?? ""; }
            int i = _room != null ? _rooms.IndexOf(_room) : -1;
            if (i >= 0) _roomSeconds[i] += dt;
        }

        // ---- what it hears (public, so a test can say them without staging a fight)

        public void OnRoom(string room)
        {
            if (string.IsNullOrEmpty(room)) return;
            _room = room;
            if (_rooms.Contains(room)) return;
            _rooms.Add(room); _roomSeconds.Add(0f); _roomDeaths.Add(0);
            _sinceNew = 0f;
        }

        public void OnDied()
        {
            Session.deaths++;
            int i = _room != null ? _rooms.IndexOf(_room) : -1;
            if (i >= 0) _roomDeaths[i]++;
        }

        public void OnFightStarted(string bossId)
        {
            int i = Boss(bossId);
            _attempts[i]++;
        }

        public void OnFightWon(string bossId)
        {
            int i = Boss(bossId);
            if (_won[i]) return;
            _won[i] = true;
            if (bossId == Playtest.FinishBoss && !Session.finished)
            {
                Session.finished = true;
                Session.finishedAt = Session.seconds;
                Finished?.Invoke();
            }
        }

        void OnFightStarted(BossArena arena) => OnFightStarted(arena.BossId);
        void OnFightWon(BossArena arena) => OnFightWon(arena.BossId);
        void OnNode(string node) => Session.nodesRead++;
        void OnSurveyed(string vantage) => Session.vantagesSurveyed++;
        void OnBugReport(string folder) => Session.bugReports++;
        void OnException(string message, string trace) => Session.exceptions++;
        void OnOptions() => Session.optionsChanged++;

        int Boss(string id)
        {
            id = string.IsNullOrEmpty(id) ? "unknown" : id;
            int i = _bosses.IndexOf(id);
            if (i >= 0) return i;
            _bosses.Add(id); _attempts.Add(0); _won.Add(false);
            return _bosses.Count - 1;
        }

        /// <summary>The session as it stands, with the build, the device and the tester filled in.</summary>
        public Playtest.Session Snapshot(bool closed)
        {
            var s = Session;
            s.tester = Tester ?? "";
            s.version = BuildInfo.Label;
            s.device = Controls.LastDevice.ToString();
            s.closed = closed;
            s.rooms = _rooms.ToArray(); s.roomSeconds = _roomSeconds.ToArray(); s.roomDeaths = _roomDeaths.ToArray();
            s.bosses = _bosses.ToArray(); s.bossAttempts = _attempts.ToArray(); s.bossWon = _won.ToArray();
            return s;
        }

        public string Save(string path, bool closed)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, Snapshot(closed).ToJson());
            return path;
        }
    }

    /// <summary>
    /// A test round's session from the command line: <c>-playtest</c>, with <c>-tester name</c> and <c>-playtestOut path</c>
    /// (default: the game's data folder, playtest/&lt;tester&gt;-&lt;time&gt;.json). The game starts as it always does, from
    /// the prologue. The session is written every <see cref="Playtest.FlushSeconds"/> while it is open (so a crash still
    /// leaves one, marked open) and closed when the game quits. When the Lamp-Keeper falls the game says the round is
    /// over, and the tester may keep playing or stop.
    /// </summary>
    public sealed class PlaytestRun : MonoBehaviour
    {
        public const string Arg = "-playtest";
        public const string OutArg = "-playtestOut";
        public const string TesterArg = "-tester";

        public static PlaytestRun Instance { get; private set; }
        public static PlaytestRecorder Recorder => Instance != null ? Instance._recorder : null;

        public string OutPath { get; set; }
        PlaytestRecorder _recorder;
        float _flush;
        bool _toldTime;

        public static bool Wanted() => Environment.GetCommandLineArgs().Contains(Arg);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void FromCommandLine()
        {
            if (!Wanted() || Instance != null) return;
            var args = Environment.GetCommandLineArgs();
            int t = Array.IndexOf(args, TesterArg);
            string tester = t >= 0 && t + 1 < args.Length ? args[t + 1] : Environment.UserName;
            int o = Array.IndexOf(args, OutArg);
            string path = o >= 0 && o + 1 < args.Length ? args[o + 1] : DefaultPath(tester);
            Begin(tester, path);
        }

        public static string DefaultPath(string tester) =>
            Path.Combine(Application.persistentDataPath, "playtest", Safe(tester) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");

        static string Safe(string name) => new string((name ?? "tester").Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());

        /// <summary>Start a session (the command line, or a test).</summary>
        public static PlaytestRun Begin(string tester, string path)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("~PlaytestRun");
            DontDestroyOnLoad(go);
            var run = go.AddComponent<PlaytestRun>();
            run.OutPath = path;
            run._recorder.Tester = tester;
            Debug.Log("[OWSBG] playtest: " + tester + "'s session goes to " + path);
            return run;
        }

        void Awake()
        {
            Instance = this;
            _recorder = gameObject.AddComponent<PlaytestRecorder>();
            _recorder.Finished += OnFinished;
        }

        void OnDestroy()
        {
            if (_recorder != null) _recorder.Finished -= OnFinished;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            _flush += Time.unscaledDeltaTime;
            if (_flush >= Playtest.FlushSeconds) { _flush = 0f; SaveNow(false); }
            if (!_toldTime && _recorder.Session.seconds > Playtest.SessionMinutes * 60f)
            {
                _toldTime = true;
                Captions.Show(Loc.T("caption.playtest.hour", "An hour's play: the test asks no more of you. Thank you; stop whenever you like."), 8f);
            }
        }

        void OnFinished()
        {
            SaveNow(false);
            Captions.Show(Loc.T("caption.playtest.done", "That is the end of this test. Thank you. You may keep playing, or stop here."), 8f);
        }

        void OnApplicationQuit() { SaveNow(true); }

        public string SaveNow(bool closed)
        {
            if (_recorder == null || string.IsNullOrEmpty(OutPath)) return null;
            try { return _recorder.Save(OutPath, closed); }
            catch (Exception e) { Debug.LogWarning("[OWSBG] playtest: could not write the session: " + e.Message); return null; }
        }
    }
}
