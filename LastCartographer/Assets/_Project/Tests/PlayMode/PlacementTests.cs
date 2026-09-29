#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// What can be read or asked on the built coast stands in its room (NAR-15, DES-15; placed by PlacementSetup): for
    /// every readable piece and every asker whose room is a built greybox, the room has a talker on its scene, Wren can
    /// stand at it, and it is what up would read.
    /// </summary>
    public class PlacementTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static bool Built(string room) => File.Exists(Path.Combine(Application.dataPath, "_Project/Scenes/Greybox/Greybox_" + room + ".unity"));

        /// <summary>Every (room, scene) the coast should have placed: the readable pieces and the askers in built rooms.</summary>
        static List<(string room, string node)> Expected() =>
            Dressing.All.Where(p => p.Readable && Built(p.Room)).Select(p => (p.Room, p.Node))
                .Concat(Offerings.All.Where(a => Built(a.Room)).Select(a => (a.Room, a.Node)))
                .ToList();

        [UnityTest]
        public IEnumerator EverythingReadableOnTheCoastStandsInItsRoom()
        {
            var expected = Expected();
            Assert.GreaterOrEqual(expected.Count, 7, "five pieces and two askers on the coast");
            yield return _replay.Boot();
            var rm = RoomManager.Instance;
            var wren = Object.FindFirstObjectByType<WrenController>()!;
            var interactor = wren.GetComponent<Interactor>();
            foreach (var group in expected.GroupBy(e => e.room))
            {
                string scene = "Greybox_" + group.Key;
                if (rm.CurrentRoom != scene)
                {
                    rm.Transition(scene, group.Key == "Saltmarrow_Shore" ? "East" : "West");
                    yield return RouteReplay.Until(() => rm.CurrentRoom == scene && !rm.IsTransitioning, 15f, scene);
                }
                var talkers = Room.Current!.GetComponentsInChildren<NpcTalker>(true);
                foreach (var (_, node) in group)
                {
                    var talker = talkers.FirstOrDefault(t => t.StartNode == node);
                    Assert.IsNotNull(talker, node + " is placed in " + scene + " (run OWSBG → Place the Coast's Readables)");
                    wren.Teleport((Vector2)talker!.transform.position + new Vector2(0f, 0.05f));
                    float t = 0f;
                    while (t < 0.6f) { t += Time.deltaTime; yield return null; }
                    Assert.IsTrue(wren.IsGrounded, node + ": Wren stands where it's read, in " + scene);
                    Assert.Less(Mathf.Abs(wren.Position.y - talker.transform.position.y), 0.5f, node + ": on its floor, not below it");
                    Assert.AreSame(talker, interactor.Current, node + " is what up reads there");
                }
            }
        }
    }
}
