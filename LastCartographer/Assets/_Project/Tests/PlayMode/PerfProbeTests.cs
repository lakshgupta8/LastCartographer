#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The performance probe a built player runs with <c>-perf</c> (PRG-24), run short in the editor: it walks from the
    /// first room through a way on to a second, times the transition, samples frames in both, and puts the 60 fps cap
    /// back after. The numbers themselves are measured on a build, not here; this proves the probe works.
    /// </summary>
    public class PerfProbeTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            FrameRate.Recap();
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator TheProbeWalksTwoRoomsAndTimesTheWay()
        {
            float lastMs = -1f;
            System.Action<string, float> seen = (_, ms) => lastMs = ms;
            RoomManager.Transitioned += seen;
            var go = new GameObject("~PerfProbe");
            var probe = go.AddComponent<PerfProbe>();
            probe.Rooms = 2; probe.SampleFrames = 30; probe.SettleFrames = 5; probe.SettleSeconds = 0.1f; probe.RoomTimeout = 20f;
            probe.OutPath = System.IO.Path.Combine(Application.temporaryCachePath, "perf-test.json");
            try
            {
                SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
                Object.DontDestroyOnLoad(go);
                float t = 0f;
                bool uncapped = false;
                while (!probe.Done && t < 90f) { t += Time.unscaledDeltaTime; uncapped |= FrameRate.Uncapped; yield return null; }
                Assert.IsTrue(probe.Done, "the probe finished");
                Assert.IsTrue(uncapped, "the cap was lifted while it measured");
                Assert.IsFalse(FrameRate.Uncapped, "and put back after");

                var r = probe.Result;
                Assert.AreEqual(2, r.rooms.Count, "two rooms sampled");
                Assert.AreEqual(1, r.transitions.Count);
                Assert.AreNotEqual(r.transitions[0].from, r.transitions[0].to);
                Assert.Greater(r.transitions[0].ms, 0f);
                Assert.AreEqual(RoomManager.Instance.LastTransitionMs, r.transitions[0].ms, 1e-3f, "the probe reads the room manager's own timing");
                Assert.AreEqual(r.transitions[0].ms, lastMs, 1e-3f, "which it announces");
                foreach (var room in r.rooms)
                {
                    Assert.AreEqual(30, room.frames);
                    Assert.Greater(room.avgMs, 0f);
                    Assert.LessOrEqual(room.p95Ms, room.maxMs);
                    Assert.LessOrEqual(room.p50Ms, room.p95Ms);
                }
                Assert.IsTrue(System.IO.File.Exists(probe.OutPath), "the report is written");
                StringAssert.Contains("\"transitions\"", System.IO.File.ReadAllText(probe.OutPath));

                // The report reads back as Core's, with the card classed, and a two-room walk proves nothing (PRO-06).
                var back = PerfReport.FromJson(System.IO.File.ReadAllText(probe.OutPath));
                Assert.AreEqual(r.rooms.Count, back.rooms.Count);
                Assert.AreEqual(PerfTarget.Classify(SystemInfo.graphicsDeviceName).cls.ToString(), back.gpuClass);
                foreach (var room in back.rooms) Assert.GreaterOrEqual(room.loadCollections, 0);
                var verdict = PerfTarget.Judge(back, "the test's walk");
                Assert.IsFalse(verdict.Proves);
                Assert.IsTrue(verdict.Reasons.Exists(x => x.Contains("2 rooms sampled of " + PerfTarget.Rooms)), string.Join("; ", verdict.Reasons));
            }
            finally
            {
                RoomManager.Transitioned -= seen;
                Object.Destroy(go);
            }
        }

        [UnityTest]
        public IEnumerator WrenIsFoundWithoutSearching()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            float t = 0f;
            while ((RoomManager.Instance == null || string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom)) && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsNotNull(WrenController.Current);
            Assert.AreSame(Object.FindFirstObjectByType<WrenController>(), WrenController.Current);
        }
    }
}
