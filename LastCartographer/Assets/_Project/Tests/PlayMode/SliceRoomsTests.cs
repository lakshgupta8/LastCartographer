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
            "Greybox_Saltmarrow_Roots_4", "Greybox_Saltmarrow_BoneBridge", "Greybox_Saltmarrow_IrisFields",
            // The highland (ENV-03): twenty-one rooms past the bridge.
            "Greybox_Emberdown_Stair_1", "Greybox_Emberdown_Stair_2", "Greybox_Emberdown_Stair_3", "Greybox_Emberdown_Rest_1", "Greybox_Emberdown_Rest_2", "Greybox_Emberdown_Rest_3",
            "Greybox_Emberdown_Bell_1", "Greybox_Emberdown_Bell_2", "Greybox_Emberdown_Chimneys_1", "Greybox_Emberdown_Chimneys_2", "Greybox_Emberdown_Chimneys_3", "Greybox_Emberdown_Chimneys_4",
            "Greybox_Emberdown_Baths_1", "Greybox_Emberdown_Baths_2", "Greybox_Emberdown_Baths_3", "Greybox_Emberdown_Overlook_1", "Greybox_Emberdown_Overlook_2",
            "Greybox_Emberdown_Hollow_1", "Greybox_Emberdown_Hollow_2", "Greybox_Emberdown_Hollow_3", "Greybox_Emberdown_Hollow_4",
            // The forest (ENV-04): nineteen rooms past the iris gap.
            "Greybox_Verdance_Road_1", "Greybox_Verdance_Road_2", "Greybox_Verdance_Road_3", "Greybox_Verdance_House_1", "Greybox_Verdance_House_2", "Greybox_Verdance_House_3",
            "Greybox_Verdance_Chapel_1", "Greybox_Verdance_Chapel_2", "Greybox_Verdance_Grove_1", "Greybox_Verdance_Grove_2", "Greybox_Verdance_Grove_3", "Greybox_Verdance_Grove_4",
            "Greybox_Verdance_Library_1", "Greybox_Verdance_Library_2", "Greybox_Verdance_Aldermere_1", "Greybox_Verdance_Aldermere_2", "Greybox_Verdance_Aldermere_3",
            "Greybox_Verdance_Gate_1", "Greybox_Verdance_Gate_2",
            // The Plateau (ENV-05): twenty-one rooms, reached from both climbs.
            "Greybox_Halden_Bridges_1", "Greybox_Halden_Bridges_2", "Greybox_Halden_Bridges_3", "Greybox_Halden_Bridges_4", "Greybox_Halden_Mills_1", "Greybox_Halden_Mills_2", "Greybox_Halden_Mills_3",
            "Greybox_Halden_Lowmarket_1", "Greybox_Halden_Lowmarket_2", "Greybox_Halden_Lowmarket_3", "Greybox_Halden_Hall_1", "Greybox_Halden_Hall_2", "Greybox_Halden_Hall_3",
            "Greybox_Halden_Orchard_1", "Greybox_Halden_Orchard_2", "Greybox_Halden_Bastion_1", "Greybox_Halden_Bastion_2", "Greybox_Halden_Bastion_3",
            "Greybox_Halden_Observatory_1", "Greybox_Halden_Observatory_2", "Greybox_Halden_Vault_1",
            // The Steppe (ENV-07): fourteen rooms out of Lowmarket's south gate.
            "Greybox_Windreach_Stones_1", "Greybox_Windreach_Stones_2", "Greybox_Windreach_Stones_3", "Greybox_Windreach_Camp_1", "Greybox_Windreach_Camp_2",
            "Greybox_Windreach_River_1", "Greybox_Windreach_River_2", "Greybox_Windreach_River_3", "Greybox_Windreach_Gate_1", "Greybox_Windreach_Gate_2",
            "Greybox_Windreach_Fire_1", "Greybox_Windreach_Fire_2", "Greybox_Windreach_Star_1", "Greybox_Windreach_Star_2",
            // The Greyfold (ENV-08): eleven rooms either side of the Edge, from the orchard road to Isolde's Last Camp.
            "Greybox_Greyfold_EdgeCamp_1", "Greybox_Greyfold_EdgeCamp_2", "Greybox_Greyfold_Cathedral_2", "Greybox_Greyfold_Road_1", "Greybox_Greyfold_Road_2", "Greybox_Greyfold_Road_3",
            "Greybox_Greyfold_Pool_1", "Greybox_Greyfold_Pool_2", "Greybox_Greyfold_Threshold_1", "Greybox_Greyfold_Threshold_2", "Greybox_Greyfold_LastCamp_1",
            // The Blank (ENV-08): the nine fixed islands.
            "Greybox_Blank_Hollow_1", "Greybox_Blank_Hollow_2", "Greybox_Blank_Hollow_3", "Greybox_Blank_Capital_1", "Greybox_Blank_Capital_2", "Greybox_Blank_Capital_3", "Greybox_Blank_Capital_4",
            "Greybox_Blank_Aury_1", "Greybox_Blank_Aury_2",
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
                Assert.IsNotEmpty(info.Exits, kv.Key + " has an exit");   // the Edge too, since ENV-08: the camp one way, the nave the other
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
            CollectionAssert.AreEquivalent(Rooms, seen, "every coast, highland, forest, Plateau, Steppe, Greyfold and Blank room is reachable on foot from the shore");

            int vantages = infos.Values.Sum(i => i.Vantages.Count);
            Assert.AreEqual(46, vantages, "eight coast vantages to stand at, the whale's, the irises', the Edge's (the fourth lamp is the boss's beacon), eight each for Emberdown, the Verdance and Halden, seven for Windreach, four more for the Greyfold, and none in the Blank");
            var greyfold = infos.Values.SelectMany(i => i.Vantages).Where(v => v.StartsWith("Greyfold")).ToList();
            CollectionAssert.AreEquivalent(RoomPlans.All.Where(p => p.Id.StartsWith("Greyfold_") && p.Vantage != null).Select(p => p.VantageId), greyfold, "the Greyfold's vantages are the plan's, the Edge's among them");
            Assert.IsEmpty(infos.Values.SelectMany(i => i.Vantages).Where(v => v.StartsWith("Blank")), "the Blank cannot be surveyed");
            var steppe = infos.Values.SelectMany(i => i.Vantages).Where(v => v.StartsWith("Windreach")).ToList();
            CollectionAssert.AreEquivalent(RoomPlans.All.Where(p => p.Id.StartsWith("Windreach_") && p.Vantage != null).Select(p => p.VantageId), steppe, "the Steppe's vantages are the plan's");
            var plateau = infos.Values.SelectMany(i => i.Vantages).Where(v => v.StartsWith("Halden")).ToList();
            CollectionAssert.AreEquivalent(RoomPlans.All.Where(p => p.Id.StartsWith("Halden_") && p.Vantage != null).Select(p => p.VantageId), plateau, "the Plateau's vantages are the plan's");
            var forest = infos.Values.SelectMany(i => i.Vantages).Where(v => v.StartsWith("Verdance")).ToList();
            CollectionAssert.AreEquivalent(RoomPlans.All.Where(p => p.Id.StartsWith("Verdance_") && p.Vantage != null).Select(p => p.VantageId), forest, "the forest's vantages are the plan's");
            var highland = infos.Values.SelectMany(i => i.Vantages).Where(v => v.StartsWith("Emberdown")).ToList();
            CollectionAssert.AreEquivalent(RoomPlans.All.Where(p => p.Id.StartsWith("Emberdown_") && p.Vantage != null).Select(p => p.VantageId), highland, "the highland's vantages are the plan's");
            var slice = Atlas.AllVantages.Where(v => v.Id.StartsWith("Saltmarrow")).Select(v => v.Id).ToList();
            var inScenes = infos.Values.SelectMany(i => i.Vantages).Where(v => v.StartsWith("Saltmarrow")).ToList();
            inScenes.Add("Saltmarrow_Lighthouse/Lamp");
            CollectionAssert.AreEquivalent(slice, inScenes, "the atlas lists exactly the coast's vantages");
        }
    }
}
