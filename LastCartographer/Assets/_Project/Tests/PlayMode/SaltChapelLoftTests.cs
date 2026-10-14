using System.Collections;
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
    /// The Salt Chapel's loft and vestry (saltmarrow-rooms.md §3): the ledges behind the altar climb in steps a jump makes
    /// to the way up; she comes up beside the loft's gap and stands; the loft's west door is the vestry's; the register
    /// there reads, and its last line is Aury's.
    /// </summary>
    public class SaltChapelLoftTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static RoomTransition Exit(string target) =>
            Object.FindObjectsByType<RoomTransition>(FindObjectsSortMode.None).FirstOrDefault(t => t.TargetScene == WorldGraph.GreyboxPrefix + target);

        static IEnumerator Take(RoomTransition t, string what)
        {
            Assert.IsNotNull(t, what);
            string scene = t.TargetScene;
            RoomManager.Instance.Transition(scene, t.TargetSpawn);
            yield return RouteReplay.Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 15f, scene);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UpBehindTheAltarToTheLoftThenTheVestrysRegister()
        {
            yield return _replay.Boot();
            var chapel = WorldGraph.GreyboxPrefix + "Saltmarrow_Chapel";
            RoomManager.Instance.Transition(chapel, "West");
            yield return RouteReplay.Until(() => RoomManager.Instance.CurrentRoom == chapel && !RoomManager.Instance.IsTransitioning, 15f, chapel);
            yield return null;

            // The ledges: each a jump above the last, and the way up a jump above the top one.
            var wren = Object.FindFirstObjectByType<WrenController>();
            Assert.IsNotNull(wren);
            float reach = wren.maxJumpHeight * 0.8f;
            float last = 1.6f;   // the altar's top
            foreach (var name in new[] { "Salt_1", "Salt_2", "Salt_3" })
            {
                var ledge = GameObject.Find(name);
                Assert.IsNotNull(ledge, name);
                float top = ledge.GetComponent<BoxCollider2D>().bounds.max.y;
                Assert.Less(top - last, reach, name + " is a jump above the last");
                Assert.Greater(ledge.GetComponent<BoxCollider2D>().bounds.min.x, 15.5f, name + " stands east of the arena's door");
                last = top;
            }
            var up = Exit("Saltmarrow_Chapel_2");
            Assert.IsNotNull(up, "the way up");
            Assert.Less(up.GetComponent<BoxCollider2D>().bounds.min.y - last, reach, "a jump above the top ledge");
            Assert.AreEqual(Ability.None, up.Needs, "nothing gates it");
            Assert.IsNotNull(Exit("Saltmarrow_BoneBridge"), "and the road east is still there");

            yield return Take(up, "up to the loft");
            for (int i = 0; i < 60; i++) yield return null;
            Assert.Greater(wren.transform.position.y, -0.5f, "she comes up beside the gap and stands, not into it");
            Assert.IsNotNull(Exit("Saltmarrow_Chapel"), "the gap drops back behind the altar");

            yield return Take(Exit("Saltmarrow_Chapel_3"), "west to the vestry");
            var talker = Object.FindObjectsByType<NpcTalker>(FindObjectsSortMode.None).FirstOrDefault(t => t.StartNode == "Chapel_Register");
            Assert.IsNotNull(talker, "the register can be read");
            yield return _replay.Talk("Register", "Chapel_Register", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("register of the drowned")));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("NOT DROWNED")), "the last line is Aury's");
            Assert.IsNotNull(Exit("Saltmarrow_Chapel_2"), "and the one way out is back through the loft");
        }
    }
}
