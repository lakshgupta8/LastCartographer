using System;
using System.Collections;
using System.IO;
using System.Linq;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The feel-test's course (PRO-03) as a runtime room, "Feel_Course", drawn from <see cref="FeelTest"/>'s
    /// numbers: the run, the four hops over pits, the steps, the low ceiling, the shaft with its ladder path, the
    /// dash gap and the three crabs, with a post at each end. <see cref="FeelRun"/> takes a tester there:
    /// <c>LastCartographer.exe -feel [-feelOut session.json] [-tester name]</c>, or OWSBG → Play the Feel Course.
    /// </summary>
    public static class FeelCourseRooms
    {
        public const string SceneName = "Feel_Course";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { if (!RoomManager.Generators.Contains(Build)) RoomManager.Generators.Add(Build); }

        public static Room Build(string sceneName)
        {
            if (sceneName != SceneName) return null;
            return RuntimeRooms.InNewScene(sceneName, Populate);
        }

        static Room Populate()
        {
            var room = RuntimeRooms.MakeRoom(SceneName);
            var ledge = RuntimeRooms.Lit("Feel_Ledge", new Color(0.46f, 0.44f, 0.40f));
            var wall = RuntimeRooms.Lit("Feel_Wall", new Color(0.38f, 0.36f, 0.33f));
            var post = RuntimeRooms.Lit("Feel_Post", new Color(0.72f, 0.30f, 0.18f));
            var pit = RuntimeRooms.Lit("Feel_Pit", new Color(0.30f, 0.30f, 0.32f));

            foreach (var l in FeelTest.Ledges())
            {
                float w = l.X1 - l.X0;
                if (l.Name == "CeilingRoof")
                    RuntimeRooms.MakeGround(room, l.Name, new Vector2((l.X0 + l.X1) / 2f, l.Y + 1.5f), new Vector2(w, 3f), wall);   // hangs from the roof height up
                else
                    RuntimeRooms.MakeGround(room, l.Name, new Vector2((l.X0 + l.X1) / 2f, l.Y - 0.5f), new Vector2(w, 1f), ledge);
            }
            foreach (var b in FeelTest.Walls())
                RuntimeRooms.MakeGround(room, b.Name, new Vector2(b.X, (b.Y0 + b.Y1) / 2f), new Vector2(1f, b.Y1 - b.Y0), wall);
            // The pits' catch floor, the whole length, so nobody falls for ever.
            RuntimeRooms.MakeGround(room, "PitFloor", new Vector2((FeelTest.StartX + FeelTest.EndX) / 2f, FeelTest.PitY - 0.5f), new Vector2(FeelTest.EndX - FeelTest.StartX + 20f, 1f), pit);
            RuntimeRooms.MakeGround(room, "Wall_W", new Vector2(FeelTest.StartX - 5f, 4f), new Vector2(1f, 30f), wall);
            RuntimeRooms.MakeGround(room, "Wall_E", new Vector2(FeelTest.EndX + 4f, FeelTest.ShaftHeight + 6f), new Vector2(1f, 30f), wall);
            RuntimeRooms.MakeProp(room.transform, "StartPost", new Vector2(FeelTest.StartX, 1.5f), new Vector3(0.4f, 3f, 0.4f), post);
            RuntimeRooms.MakeProp(room.transform, "RunPost", new Vector2(29f, 1.5f), new Vector3(0.4f, 3f, 0.4f), post);
            RuntimeRooms.MakeProp(room.transform, "EndPost", new Vector2(FeelTest.EndX, FeelTest.ShaftHeight + 1.5f), new Vector3(0.4f, 3f, 0.4f), post);
            foreach (var s in FeelTest.Stations)
                RuntimeRooms.MakeProp(room.transform, "Mark_" + s.Id, new Vector2(s.From, (s.Id == FeelTest.Station.Dash || s.Id == FeelTest.Station.Targets ? FeelTest.ShaftHeight : 0f) + 0.15f), new Vector3(0.2f, 0.3f, 0.2f), post);
            RuntimeRooms.MakePaper(room, "Mid", 3f, -2f, new Color(0.55f, 0.53f, 0.49f), 8f);
            RuntimeRooms.MakePaper(room, "Far", 8f, 2f, new Color(0.65f, 0.63f, 0.59f), 14f);

            foreach (var x in FeelTest.TargetXs)
            {
                var go = new GameObject("Crab_" + x) { layer = LayerMask.NameToLayer("Enemy") };
                go.transform.SetParent(room.transform, false);
                go.transform.position = new Vector3(x, FeelTest.ShaftHeight + 0.5f, 0f);
                go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
                go.AddComponent<Rigidbody2D>();
                go.AddComponent<MarshCrab>();
                go.AddComponent<FeelTarget>();
            }

            RuntimeRooms.MakeSpawn(room, "Start", new Vector2(FeelTest.StartX + 1f, 0.5f));
            RuntimeRooms.MakeSpawn(room, "West", new Vector2(FeelTest.StartX + 1f, 0.5f));
            room.gameObject.name = "Room_" + SceneName;
            return room;
        }
    }

    /// <summary>A crab on the course: each time its health drops, the recorder counts a hit.</summary>
    public sealed class FeelTarget : MonoBehaviour
    {
        Enemy _enemy;
        int _last;
        void Awake() { _enemy = GetComponent<Enemy>(); _last = _enemy != null ? _enemy.Health : 0; }
        void Update()
        {
            if (_enemy == null) return;
            if (_enemy.Health < _last)
            {
                var rec = FeelRun.Recorder;
                if (rec != null) rec.NoteHit();
            }
            _last = _enemy.Health;
        }
    }

    /// <summary>
    /// Takes a tester to the course and keeps their session: <c>-feel</c> (or the editor pref the menu sets), with
    /// <c>-feelOut path</c> for the JSON (default: the game's data folder, feel/&lt;time&gt;.json) and <c>-tester name</c>.
    /// The session is written when the game quits, and by <see cref="SaveNow"/>.
    /// </summary>
    public sealed class FeelRun : MonoBehaviour
    {
        public const string Arg = "-feel";
        public const string OutArg = "-feelOut";
        public const string TesterArg = "-tester";
        public const string EditorPref = "OWSBG.FeelCourse";

        public static FeelRun Instance { get; private set; }
        public static FeelRecorder Recorder => Instance != null ? Instance._recorder : null;

        public string OutPath { get; set; }
        FeelRecorder _recorder;
        bool _saved;

        /// <summary>Asked for by the command line or the editor menu.</summary>
        public static bool Wanted()
        {
            if (Environment.GetCommandLineArgs().Contains(Arg)) return true;
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(EditorPref, false);
#else
            return false;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void FromCommandLine()
        {
            if (!Wanted() || Instance != null) return;
            var go = new GameObject("~FeelRun");
            DontDestroyOnLoad(go);
            var run = go.AddComponent<FeelRun>();
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, OutArg);
            run.OutPath = i >= 0 && i + 1 < args.Length ? args[i + 1]
                : Path.Combine(Application.persistentDataPath, "feel", DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            int t = Array.IndexOf(args, TesterArg);
            run._recorder.Tester = t >= 0 && t + 1 < args.Length ? args[t + 1] : Environment.UserName;
        }

        void Awake()
        {
            Instance = this;
            _recorder = gameObject.AddComponent<FeelRecorder>();
            _recorder.OnCourse = true;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            float t0 = Time.realtimeSinceStartup;
            while ((RoomManager.Instance == null || string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) || RoomManager.Instance.IsTransitioning)
                   && Time.realtimeSinceStartup - t0 < 30f) yield return null;
            if (RoomManager.Instance == null) { Debug.LogWarning("[OWSBG] feel: no room manager"); yield break; }
            if (RoomManager.Instance.CurrentRoom != FeelCourseRooms.SceneName)
                RoomManager.Instance.Transition(FeelCourseRooms.SceneName, "Start");
            Debug.Log("[OWSBG] feel: the course is up; the session goes to " + OutPath);
        }

        void OnApplicationQuit() { SaveNow(); }

        public string SaveNow()
        {
            if (_saved || _recorder == null || string.IsNullOrEmpty(OutPath)) return null;
            _saved = true;
            return _recorder.Save(OutPath);
        }
    }
}
