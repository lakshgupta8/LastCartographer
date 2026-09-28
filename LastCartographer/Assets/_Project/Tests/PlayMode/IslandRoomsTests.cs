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
    /// The Blank island generator (PRG-20): asking RoomManager for an island's scene builds it from WorldState, with the
    /// island's people on its node and exits to the islands either side; the drift is walked island to island.
    /// </summary>
    public class IslandRoomsTests
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

        IEnumerator Boot()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
        }

        IEnumerator GoTo(string scene, string spawn)
        {
            RoomManager.Instance.Transition(scene, spawn);
            yield return Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 10f, scene);
        }

        static RoomTransition? Exit(Room room, string name)
            => room.GetComponentsInChildren<RoomTransition>(true).FirstOrDefault(t => t.gameObject.name == "Transition_" + name);

        [UnityTest]
        public IEnumerator TheDriftIsBuiltFromWhatSheLeftAndWalkedIslandToIsland()
        {
            yield return Boot();
            var w = GameState.World;
            Assert.IsTrue(RoomManager.Generators.Contains(IslandBuilder.Build), "the builder registered itself");
            Places.Release(w, "Verdance_Aldermere_2");
            Places.Release(w, "Halden_Lowmarket_2");
            Places.Release(w, "Emberdown_Baths_2");
            var drifts = Islands.Drifting(w);
            CollectionAssert.AreEqual(new[] { "Island_Aldermere", "Island_Lowmarket", "Island_Emberdown_Baths_2" }, drifts.Select(d => d.Scene).ToList());
            Assert.AreEqual("The baths", drifts[2].Name, "a generic island is named for what the place was");

            var hub = RoomManager.Instance.CurrentRoom;
            yield return GoTo(drifts[0].Scene, "Start");
            Assert.IsTrue(SceneManager.GetSceneByName(drifts[0].Scene).isLoaded, "the island is a scene now");
            Assert.IsFalse(SceneManager.GetSceneByName(hub).isLoaded, "and the hub is gone");
            var room = Room.Current;
            Assert.IsNotNull(room);
            Assert.AreEqual(drifts[0].Scene, room.RoomId);
            Assert.IsNotNull(room.CameraBounds);
            foreach (var s in new[] { "Start", "West", "East" }) Assert.IsNotNull(room.FindSpawn(s), "spawn " + s);
            var talker = room.GetComponentInChildren<NpcTalker>();
            Assert.IsNotNull(talker);
            Assert.AreEqual("Island_Aldermere", talker.StartNode, "Aldermere's people speak from Aldermere's node");
            Assert.AreEqual("Talk", talker.Prompt);
            Assert.AreEqual(Islands.DriftEntryScene, Exit(room, "To_West")!.TargetScene, "the first island leads back to the Hollow's drift");
            Assert.AreEqual(drifts[1].Scene, Exit(room, "To_East")!.TargetScene, "and on to the next");
            var wren = Object.FindFirstObjectByType<WrenController>();
            Assert.Less(Vector2.Distance(wren.Position, room.FindSpawn("Start").position), 1.5f, "Wren stands at the spawn");
            var found = string.Join("; ", room.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject.name + "[" + string.Join(",", t.GetComponents<Component>().Select(c => c.GetType().Name)) + "]"));
            Assert.IsTrue(room.GetComponentsInChildren<BoxCollider2D>().Any(c => !c.isTrigger && c.gameObject.layer == LayerMask.NameToLayer("Ground")), "there is ground; found " + found);

            yield return GoTo(drifts[1].Scene, "West");
            Assert.IsFalse(SceneManager.GetSceneByName(drifts[0].Scene).isLoaded, "the island behind is unloaded");
            room = Room.Current;
            Assert.AreEqual("Island_Lowmarket", room.GetComponentInChildren<NpcTalker>().StartNode);
            Assert.AreEqual(drifts[0].Scene, Exit(room, "To_West")!.TargetScene);

            yield return GoTo(drifts[2].Scene, "West");
            room = Room.Current;
            Assert.AreEqual(Islands.GenericNode, room.GetComponentInChildren<NpcTalker>().StartNode, "any other released place speaks as the Remnant");
            Assert.IsNull(Exit(room, "To_East"), "the drift ends at the last island");
            Assert.IsTrue(DialogueService.Instance!.StartNode(room.GetComponentInChildren<NpcTalker>().StartNode), "and its node exists");
        }

        [UnityTest]
        public IEnumerator NothingIsBuiltForAPlaceSheDidNotLeave()
        {
            yield return Boot();
            Assert.IsNull(IslandBuilder.Build("Greybox_Saltmarrow_B"), "not an island's name");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no island drifts as Island_Aldermere"));
            Assert.IsNull(IslandBuilder.Build("Island_Aldermere"), "Aldermere was not left to the Blank in this world");
            Assert.IsFalse(SceneManager.GetSceneByName("Island_Aldermere").IsValid());
        }
    }
}
