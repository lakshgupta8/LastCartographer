#nullable enable
using System.Collections;
using System.Linq;
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
    /// An island's crowd watches Wren go by (docs/design/blank-generator.md §3): one of them turns to face her when she
    /// comes near, from behind them, and turns back the way they were stood once she has gone; the inn's sleepers sleep on.
    /// </summary>
    public class IslandCrowdTests
    {
        [SetUp] public void SetUp() { Time.timeScale = 1f; GameState.NewGame(); Bootstrap.SkipPrologueOverride = true; }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_") || s.name.StartsWith(Islands.ScenePrefix))
                    yield return SceneManager.UnloadSceneAsync(s);
            }
            GameState.NewGame();
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        static IEnumerator GoTo(string scene)
        {
            RoomManager.Instance.Transition(scene, "Start");
            yield return Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 10f, scene);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheCrowdTurnsToWatchHerGoByAndTheSleepersSleepOn()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            var w = GameState.World;
            w.Set("verdance.gate.inn_visited", 1);       // the inn, asleep over the soup
            Places.Release(w, "Emberdown_Baths_2");      // a generic island
            var drifts = Islands.Drifting(w);
            var generic = drifts.FirstOrDefault(d => d.IsGeneric && !d.IsHalf);
            var inn = drifts.FirstOrDefault(d => IslandLooks.For(d)?.CrowdActivity == "asleep");
            Assert.IsNotNull(generic, "a generic island drifts: " + string.Join(", ", drifts.Select(d => d.Scene)));
            Assert.IsNotNull(inn, "the inn drifts");

            yield return GoTo(generic!.Scene);
            var crowd = Room.Current.GetComponentsInChildren<CrowdTurns>();
            Assert.AreEqual(IslandLooks.For(generic)!.Crowd.Length, crowd.Length, "everyone in the crowd can turn");
            Assert.IsNull(Room.Current.GetComponentInChildren<NpcTalker>().GetComponent<CrowdTurns>(), "the one who speaks turns when spoken to, not before");
            var one = crowd[0];
            yield return null;   // stood, and their way of standing taken
            int rest = one.RestFacing;
            var ctrl = WrenController.Current!;
            float x = one.transform.position.x;

            // She comes up behind them: they turn round to her.
            ctrl.Teleport(new Vector2(x - rest * 1.5f, 0.5f));
            yield return Until(() => one.Facing == -rest, 2f, one.name + " turning to her");
            Assert.IsTrue(one.Watching);

            // She goes off the other way, past where they let her go: they turn back as they were stood.
            ctrl.Teleport(new Vector2(x > 0f ? x - 9f : x + 9f, 0.5f));
            yield return Until(() => one.Facing == rest, 2f, one.name + " turning back");
            Assert.IsFalse(one.Watching);

            // The inn's sleepers do not stir.
            yield return GoTo(inn!.Scene);
            var sleepers = Room.Current.GetComponentsInChildren<CrowdTurns>();
            Assert.Greater(sleepers.Length, 0);
            yield return null;
            var facing = sleepers.Select(s => s.Facing).ToArray();
            foreach (var s in sleepers)
            {
                ctrl.Teleport(new Vector2(s.transform.position.x - s.RestFacing * 1.5f, 0.5f));
                float t = 0f;
                while (t < 1f) { t += Time.deltaTime; yield return null; }
            }
            CollectionAssert.AreEqual(facing, sleepers.Select(s => s.Facing).ToArray(), "asleep over the soup, they do not turn");
        }
    }
}
