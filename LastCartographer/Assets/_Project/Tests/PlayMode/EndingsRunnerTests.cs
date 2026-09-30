#nullable enable
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The endings runner (PRG-23): from a chosen ending it plays Voss's coda, walks the ending's stops through built rooms
    /// and stand-ins, plays each scene, and ends white on the title; an ending's own scene starts it with &lt;&lt;epilogue&gt;&gt;.
    /// </summary>
    public class EndingsRunnerTests
    {
        readonly List<string> _rooms = new List<string>();

        [SetUp] public void SetUp() { Time.timeScale = 1f; GameState.NewGame(); Bootstrap.SkipPrologueOverride = true; EndingsRunner.AutoWalk = true; _rooms.Clear(); }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            if (RoomManager.Instance != null) RoomManager.Instance.RoomChanged -= OnRoom;
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_") || s.name.StartsWith(EpilogueBuilder.ScenePrefix) || s.name.StartsWith(Islands.ScenePrefix))
                    yield return SceneManager.UnloadSceneAsync(s);
            }
            ScreenFade.Clear();
            GameState.NewGame();
        }

        void OnRoom(string scene) => _rooms.Add(scene);

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        /// <summary>Wait for the walk, reading each scene through as the player would.</summary>
        static IEnumerator WalkThrough(System.Func<bool> finished, float seconds)
        {
            var presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            float t = 0f;
            while (!finished() && t < seconds)
            {
                if (presenter.IsShowingLine) presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(finished(), "timed out waiting for the walk");
        }

        IEnumerator Boot()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            RoomManager.Instance!.RoomChanged += OnRoom;
            var runner = EndingsRunner.Instance;
            Assert.IsNotNull(runner, "the runner lives in the persistent scene");
            runner!.HoldSeconds = 0f;
            runner.FadeSeconds = 0f;
        }

        [UnityTest]
        public IEnumerator TheFixedWorldsWalkGoesToHallQuayAndHollow()
        {
            yield return Boot();
            var w = GameState.World;
            var runner = EndingsRunner.Instance!;
            Assert.IsFalse(runner.Begin(), "nothing chosen, nothing to walk");
            w.Set(Endings.ChosenFlag, (int)Ending.Fixed);
            bool finished = false;
            runner.Finished += () => finished = true;
            var stops = new List<string>();
            runner.StopReached += stops.Add;

            Assert.IsTrue(runner.Begin());
            Assert.IsTrue(runner.IsWalking);
            Assert.IsFalse(runner.Begin(), "once");
            yield return WalkThrough(() => finished, 90f);

            Assert.IsTrue(w.Is(EndingsRunner.VossCodaFlag), "Voss's coda first, at the frame's door");
            CollectionAssert.AreEqual(new[] { "Epilogue_Pell", "Epilogue_Sable", "Epilogue_Marrow" }, stops);
            CollectionAssert.AreEqual(new[] { "Epilogue_Halden_JourneymansHall", "Greybox_Saltmarrow_A", "Epilogue_Blank_ThessalyHollow" }, runner.Visited,
                "Halden and the Hollow as stand-ins; the quay is built");
            CollectionAssert.AreEqual(runner.Visited, _rooms, "each stop was a real room change");
            Assert.IsTrue(w.Is("epilogue.done"), "Marrow's verdict played");
            Assert.IsTrue(w.Is(EndingsRunner.FinishedFlag));
            Assert.AreEqual(1f, ScreenFade.Level, 0.001f, "white, for good");
            Assert.AreEqual(ScreenFade.White, ScreenFade.Color);
            Assert.IsFalse(runner.IsWalking);
            Assert.IsFalse(DialogueService.Instance!.IsRunning);
            Assert.AreSame(Score.CodaOf(Ending.Fixed), MusicDriver.Instance!.Coda, "the Fixed World's coda plays the title out (AUD-08)");
        }

        [UnityTest]
        public IEnumerator AnEndingsSceneStartsTheWalkItself()
        {
            yield return Boot();
            var w = GameState.World;
            var runner = EndingsRunner.Instance!;
            w.Set(Endings.ChosenFlag, (int)Ending.Unwritten);
            w.Set(EndingsRunner.VossCodaFlag, true);
            bool finished = false;
            runner.Finished += () => finished = true;

            var svc = DialogueService.Instance!;
            var presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            Assert.IsTrue(svc.StartNode("Observatory_Teodor_Unwritten"));
            float t = 0f;
            while (svc.IsRunning && !runner.IsWalking && t < 30f)
            {
                if (presenter.IsShowingLine) presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(runner.IsWalking, "<<epilogue>> at the scene's end started the walk");
            yield return WalkThrough(() => finished, 90f);   // Teodor's scene finishes first; the walk waits for it
            CollectionAssert.AreEqual(new[] { "Epilogue_Halden_JourneymansHall", "Epilogue_Verdance_QuietHouse", "Epilogue_Blank_ThessalyHollow" }, runner.Visited);
            Assert.IsTrue(w.Is("epilogue.done") && w.Is(EndingsRunner.FinishedFlag));

            // A stand-in has the stop's speaker in it, on the stop's node.
            var room = Room.Current;
            Assert.IsNotNull(room);
            Assert.AreEqual("Epilogue_Marrow", room!.GetComponentInChildren<NpcTalker>().StartNode);
        }

        [UnityTest]
        public IEnumerator WithWalkingOffTheCommandDoesNothing()
        {
            yield return Boot();
            EndingsRunner.AutoWalk = false;
            GameState.World.Set(Endings.ChosenFlag, (int)Ending.Rest);
            Assert.IsFalse(EndingsRunner.Instance!.Begin());
            Assert.IsFalse(EndingsRunner.Instance.IsWalking);
        }
    }
}
