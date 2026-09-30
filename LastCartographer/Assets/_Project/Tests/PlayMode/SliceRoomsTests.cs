#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The vertical slice's rooms (DES-08): every scene loads with a Room, every exit points at a scene that
    /// exists and a spawn that scene has, up and down exits pair, every vantage is on the atlas, and every
    /// room sits on the macro map.
    /// </summary>
    public class SliceRoomsTests
    {
        static readonly string[] Rooms =
        {
            "Greybox_Greyfold_Edge", "Greybox_Saltmarrow_Shore", "Greybox_Saltmarrow_A", "Greybox_Saltmarrow_Stilts", "Greybox_Saltmarrow_Boardwalk",
            "Greybox_Saltmarrow_B", "Greybox_Saltmarrow_Tetherline", "Greybox_Saltmarrow_Ferry", "Greybox_Saltmarrow_Chain_1", "Greybox_Saltmarrow_Chain_2",
            "Greybox_Saltmarrow_Chain_3", "Greybox_Saltmarrow_Lighthouse", "Greybox_Saltmarrow_Chapel", "Greybox_Saltmarrow_Roots_1", "Greybox_Saltmarrow_Roots_2", "Greybox_Saltmarrow_Roots_3",
            "Greybox_Saltmarrow_Roots_4", "Greybox_Saltmarrow_BoneBridge",
            // The highland (ENV-03): twenty-one rooms past the bridge.
            "Greybox_Emberdown_Stair_1", "Greybox_Emberdown_Stair_2", "Greybox_Emberdown_Stair_3", "Greybox_Emberdown_Rest_1", "Greybox_Emberdown_Rest_2", "Greybox_Emberdown_Rest_3",
            "Greybox_Emberdown_Bell_1", "Greybox_Emberdown_Bell_2", "Greybox_Emberdown_Chimneys_1", "Greybox_Emberdown_Chimneys_2", "Greybox_Emberdown_Chimneys_3", "Greybox_Emberdown_Chimneys_4",
            "Greybox_Emberdown_Baths_1", "Greybox_Emberdown_Baths_2", "Greybox_Emberdown_Baths_3", "Greybox_Emberdown_Overlook_1", "Greybox_Emberdown_Overlook_2",
            "Greybox_Emberdown_Hollow_1", "Greybox_Emberdown_Hollow_2", "Greybox_Emberdown_Hollow_3", "Greybox_Emberdown_Hollow_4",
        };

        sealed class Info
        {
            public string RoomId = "";
            public readonly HashSet<string> Spawns = new HashSet<string>();
            public readonly List<(string name, string target, string spawn)> Exits = new List<(string, string, string)>();
            public readonly List<string> Vantages = new List<string>();
            public int Enemies;
        }

        [SetUp] public void SetUp() { GameState.NewGame(); Bootstrap.SkipPrologueOverride = true; }

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
                if (s.name.StartsWith("Greybox_") || s.name == Bootstrap.PersistentSceneName) yield return SceneManager.UnloadSceneAsync(s);
            }
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator EveryExitLeadsSomewhereThatExists()
        {
            var infos = new Dictionary<string, Info>();
            foreach (var name in Rooms)
            {
                // Rooms are Addressables, not built-in scenes (PRG-07): load them the way the game does.
                var handle = Addressables.LoadSceneAsync(name, LoadSceneMode.Additive);
                yield return handle;
                Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, name + " is an addressable room");
                var scene = handle.Result.Scene;
                Assert.IsTrue(scene.isLoaded, name);
                var info = new Info();
                Room? room = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    room ??= root.GetComponentInChildren<Room>(true);
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        if (t.name.StartsWith("Spawn_")) info.Spawns.Add(t.name.Substring(6));
                    foreach (var tr in root.GetComponentsInChildren<RoomTransition>(true)) info.Exits.Add((tr.name, tr.TargetScene, tr.TargetSpawn));
                    foreach (var v in root.GetComponentsInChildren<VantagePoint>(true)) info.Vantages.Add(v.VantageId);
                    info.Enemies += root.GetComponentsInChildren<Enemy>(true).Length;
                }
                Assert.IsNotNull(room, name + " has a Room");
                Assert.IsNotNull(room!.CameraBounds, name + " has camera bounds");
                info.RoomId = room.RoomId;
                infos[name] = info;
                yield return Addressables.UnloadSceneAsync(handle, true);
            }

            foreach (var kv in infos)
            {
                var info = kv.Value;
                if (kv.Key != "Greybox_Greyfold_Edge") Assert.IsNotEmpty(info.Exits, kv.Key + " has an exit");   // the Edge leaves through the white (BlankEdge)
                foreach (var e in info.Exits)
                {
                    Assert.IsTrue(infos.ContainsKey(e.target), kv.Key + " exit " + e.name + " leads to a scene in the slice: " + e.target);
                    Assert.IsTrue(infos[e.target].Spawns.Contains(e.spawn), kv.Key + " exit " + e.name + " arrives at " + e.target + " spawn " + e.spawn + " (has: " + string.Join(", ", infos[e.target].Spawns) + ")");
                    if (e.name == "Transition_To_Up")
                        Assert.IsTrue(infos[e.target].Exits.Any(b => b.name == "Transition_To_Down" && b.target == kv.Key), kv.Key + " climbs into " + e.target + ", which drops back");
                    if (e.name == "Transition_To_Down")
                        Assert.IsTrue(infos[e.target].Exits.Any(b => b.name == "Transition_To_Up" && b.target == kv.Key), kv.Key + " drops into " + e.target + ", which climbs back");
                }
                Assert.IsNotNull(WorldGraph.ZoneOfPlace(info.RoomId), info.RoomId + " sits on the macro map");
                foreach (var v in info.Vantages)
                {
                    Assert.IsNotNull(Atlas.FindVantage(v), v + " is on the atlas");
                    Assert.AreEqual(info.RoomId, Atlas.PlaceOf(v), v + " belongs to its room");
                }
                var plan = RoomPlans.All.FirstOrDefault(p => "Greybox_" + p.Id == kv.Key);
                if (plan == null || !string.IsNullOrEmpty(plan.Enemies)) Assert.Greater(info.Enemies, 0, kv.Key + " has something to fight");   // a planned room may be empty on purpose (the town, the bell)
            }

            // The whole slice is one connected space from the shore.
            var seen = new HashSet<string> { "Greybox_Saltmarrow_Shore" };
            var open = new Stack<string>(seen);
            while (open.Count > 0)
                foreach (var e in infos[open.Pop()].Exits)
                    if (seen.Add(e.target)) open.Push(e.target);
            CollectionAssert.AreEquivalent(Rooms.Where(r => r != "Greybox_Greyfold_Edge"), seen, "every coast and highland room is reachable on foot from the shore");

            int vantages = infos.Values.Sum(i => i.Vantages.Count);
            Assert.AreEqual(18, vantages, "eight coast vantages to stand at, the whale's, the Edge's (the fourth lamp is the boss's beacon), and Emberdown's eight");
            var highland = infos.Values.SelectMany(i => i.Vantages).Where(v => v.StartsWith("Emberdown")).ToList();
            CollectionAssert.AreEquivalent(RoomPlans.All.Where(p => p.Id.StartsWith("Emberdown_") && p.Vantage != null).Select(p => p.VantageId), highland, "the highland's vantages are the plan's");
            var slice = Atlas.AllVantages.Where(v => v.Id.StartsWith("Saltmarrow")).Select(v => v.Id).ToList();
            var inScenes = infos.Values.SelectMany(i => i.Vantages).Where(v => v.StartsWith("Saltmarrow")).ToList();
            inScenes.Add("Saltmarrow_Lighthouse/Lamp");
            CollectionAssert.AreEquivalent(slice, inScenes, "the atlas lists exactly the coast's vantages");
        }
    }
}
