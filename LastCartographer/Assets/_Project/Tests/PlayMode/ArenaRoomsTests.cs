#nullable enable
using System.Collections;
using System.Linq;
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
    /// The arena rooms built at runtime (CMB-13): each kit's boss has a room in the game, named for its planned room,
    /// that RoomManager travels to like any other; walking in shuts the doors and starts the fight.
    /// </summary>
    public class ArenaRoomsTests
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
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_") || s.name.StartsWith(ArenaRooms.ScenePrefix))
                    yield return SceneManager.UnloadSceneAsync(s);
            }
            ScreenFade.Clear();
            GameState.NewGame();
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        [Test]
        public void EveryKitHasAPlannedRoom()
        {
            foreach (var id in BossKits.Ids)
            {
                var plan = ArenaRooms.RoomOf(id);
                Assert.IsNotNull(plan, id + " is fought in a planned room");
                Assert.AreEqual(Bosses.Find(id).Zone, plan!.Zone, id + "'s room is in the zone its sheet names");
                Assert.AreEqual(ArenaRooms.ScenePrefix + plan.Id, ArenaRooms.SceneFor(id));
            }
            Assert.IsNull(ArenaRooms.Build("Arena_Nowhere"));
            Assert.IsNull(ArenaRooms.Build("Greybox_Saltmarrow_A"), "not its scene");
        }

        [UnityTest]
        public IEnumerator EachArenaRoomLoadsAndItsFightStartsOnEntry()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            var wren = Object.FindFirstObjectByType<WrenController>()!;
            Assert.IsTrue(RoomManager.Generators.Contains(ArenaRooms.Build), "the builder registered itself");

            foreach (var id in BossKits.Ids)
            {
                var scene = ArenaRooms.SceneFor(id)!;
                RoomManager.Instance!.Transition(scene, "Start");
                yield return Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 10f, scene);
                var room = Room.Current!;
                var arena = room.GetComponentInChildren<BossArena>();
                Assert.IsNotNull(arena, scene + " has an arena");
                Assert.AreEqual(id, arena.BossId);
                Assert.AreEqual(Bosses.Find(id).Name, arena.Boss.BossName, "named from the sheet");
                Assert.Less(Vector2.Distance(wren.Position, room.FindSpawn("Start").position), 1.5f, "she comes in at the west spawn");
                Assert.AreEqual(BossArena.ArenaState.Idle, arena.State, "and waits outside the doors");

                wren.Teleport(new Vector2(arena.transform.position.x - 4f, 0.2f));
                yield return Until(() => arena.State == BossArena.ArenaState.Fighting, 6f, id + "'s fight");
                Assert.IsTrue(arena.Boss.IsFightActive);
                arena.ResetFight();
                wren.Teleport(room.FindSpawn("Start").position);
                yield return null;
            }
        }
    }
}
