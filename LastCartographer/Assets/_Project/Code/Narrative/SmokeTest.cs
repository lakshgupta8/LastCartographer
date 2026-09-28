using System;
using System.Collections;
using System.Linq;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// A built player's first check (PRG-25): <c>LastCartographer.exe -batchmode -nographics -smoke -logFile smoke.log</c>
    /// boots as a player would, then:
    /// <list type="number">
    /// <item>waits for the first room (the prologue's, in a new game);</item>
    /// <item>walks into a second room through Addressables;</item>
    /// <item>finds the build stamp the pipeline wrote;</item>
    /// <item>starts a conversation from the compiled Yarn project and stops it.</item>
    /// </list>
    /// Any error or exception logged along the way fails it. It logs one "[OWSBG] smoke:" line and quits with 0 when it
    /// passes, 1 when a step fails, 2 when it times out. CI runs it on the Windows build.
    /// </summary>
    public sealed class SmokeTest : MonoBehaviour
    {
        public const string Arg = "-smoke";
        public const string SecondRoom = "Greybox_Saltmarrow_A";
        public const string Conversation = "Camp_Ashes_Ahead";

        public enum Result { Running, Passed, Failed, TimedOut }

        public Result Outcome { get; private set; } = Result.Running;
        public string Report { get; private set; } = "";
        public int Errors { get; private set; }
        public string FirstError { get; private set; }
        /// <summary>Quit the player with the result's exit code when done (command line only).</summary>
        public bool QuitWhenDone { get; set; }
        public float RoomTimeout { get; set; } = 60f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void FromCommandLine()
        {
            if (!Environment.GetCommandLineArgs().Contains(Arg)) return;
            var go = new GameObject("~SmokeTest");
            DontDestroyOnLoad(go);
            go.AddComponent<SmokeTest>().QuitWhenDone = true;
        }

        void OnEnable() { Application.logMessageReceived += OnLog; }
        void OnDisable() { Application.logMessageReceived -= OnLog; }

        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.StartsWith("[OWSBG] smoke:")) return;
            Errors++;
            FirstError ??= message;
        }

        static IEnumerator Wait(Func<bool> done, float seconds, Action<bool> result)
        {
            float start = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - start < seconds) yield return null;
            result(done());
        }

        IEnumerator Start()
        {
            var steps = new System.Collections.Generic.List<string>();
            bool ok = false;

            yield return Wait(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning,
                RoomTimeout, r => ok = r);
            if (!ok) { Finish(Result.TimedOut, "no room came in"); yield break; }
            var rooms = RoomManager.Instance;
            string first = rooms.CurrentRoom;
            steps.Add("first room " + first + (rooms.IsAddressable(first) ? " (addressable)" : ""));

            if (first != SecondRoom)
            {
                rooms.Transition(SecondRoom, "West");
                yield return Wait(() => rooms.CurrentRoom == SecondRoom && !rooms.IsTransitioning, RoomTimeout, r => ok = r);
                if (!ok) { Finish(Result.TimedOut, string.Join("; ", steps) + "; " + SecondRoom + " never came in"); yield break; }
                steps.Add("then " + SecondRoom + (rooms.IsAddressable(SecondRoom) ? " (addressable)" : ""));
            }

            // A player the pipeline built carries its stamp; the editor's is "dev".
            if (!Application.isEditor && BuildInfo.Version == "dev") { Finish(Result.Failed, string.Join("; ", steps) + "; the build wasn't stamped"); yield break; }
            steps.Add("build " + BuildInfo.Label);

            var dialogue = DialogueService.Instance;
            if (dialogue == null || !dialogue.StartNode(Conversation)) { Finish(Result.Failed, string.Join("; ", steps) + "; the dialogue didn't start"); yield break; }
            yield return null;
            bool talking = dialogue.IsRunning;
            dialogue.Stop();
            if (!talking) { Finish(Result.Failed, string.Join("; ", steps) + "; the dialogue stopped at once"); yield break; }
            steps.Add("dialogue " + Conversation);

            yield return null;
            Finish(Errors == 0 ? Result.Passed : Result.Failed, string.Join("; ", steps) + (Errors > 0 ? "; " + Errors + " errors logged, the first: " + FirstError : ""));
        }

        void Finish(Result result, string report)
        {
            Outcome = result;
            Report = report;
            Debug.Log("[OWSBG] smoke: " + result.ToString().ToLowerInvariant() + " (" + Time.realtimeSinceStartup.ToString("0.0") + " s) " + report);
            if (!QuitWhenDone) return;
            Application.Quit(result == Result.Passed ? 0 : result == Result.TimedOut ? 2 : 1);
        }
    }
}
