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
    /// Aldermere after its last day (verdance-arc.md §1): attended, the lane and the square stand empty and the village is
    /// paper in the ash field, Teodor sitting with them in his own colours; stopped and the Choir answered, the village is
    /// still here, with half a song, and Teodor has gone.
    /// </summary>
    public class AldermereAfterTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static IEnumerator GoTo(string room)
        {
            var scene = WorldGraph.GreyboxPrefix + room;
            RoomManager.Instance.Transition(scene, "West");
            yield return RouteReplay.Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 10f, scene);
            yield return null;
        }

        static FlagPresence Gate(string objectName)
        {
            var p = Object.FindObjectsByType<FlagPresence>(FindObjectsSortMode.None).FirstOrDefault(f => f.name == objectName);
            Assert.IsNotNull(p, objectName + " stands by the world's say");
            return p!;
        }

        [UnityTest]
        public IEnumerator AttendedTheVillageIsPaperInTheAshFieldAndTeodorSitsWithThem()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            w.Set("verdance.teodor.met", 1);

            yield return GoTo("Verdance_Aldermere_3");
            Assert.IsFalse(Gate("Teodor_Greybox").Standing, "the last day not yet come: nobody in the ash field");
            Assert.IsFalse(Gate("Folk_Jay_0").Standing);

            yield return _replay.Talk("Hollin", "Aldermere_Hollin", new[] { 0 });   // Hollin starts the last day; then I'll stay to the end
            Assert.IsTrue(w.Is("verdance.aldermere.attended"));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("always bread on the last day")), "Hollin's node is the last day while it is undecided");
            yield return null; yield return null;

            Assert.IsTrue(Gate("Teodor_Greybox").Standing, "Teodor sits with them");
            foreach (var folk in new[] { "Folk_Jay_0", "Folk_Finch_1", "Folk_Woodpecker_2" })
            {
                Assert.IsTrue(Gate(folk).Standing, folk + " in the ash field");
                Assert.AreEqual(NpcInkState.Remnant, Gate(folk).GetComponent<NpcInk>().State, folk + " is paper");
            }
            var teodor = Gate("Teodor_Greybox").GetComponent<NpcInk>();
            Assert.IsFalse(teodor.FollowPlace, "a visitor");
            Assert.AreEqual(NpcInkState.Drawn, teodor.State, "in his own colours among the paper");

            _replay.Heard.Clear();
            yield return _replay.Talk("Teodor", "Aldermere_Ash_Teodor", new[] { 1 });   // do they know you come?
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("too thick")));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Hollin said bread")));
            Assert.IsTrue(w.Is("verdance.aldermere.ash_sat"));
            _replay.Heard.Clear();
            yield return _replay.Talk("Teodor", "Aldermere_Ash_Teodor", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("bunting is still up")), "and after, the standing line");

            yield return GoTo("Verdance_Aldermere_2");
            Assert.IsFalse(Gate("Teodor_Greybox").Standing, "the square: Teodor gone to the field");
            Assert.IsFalse(Gate("Hollin_Greybox").Standing, "and Hollin with the village");
            Assert.IsFalse(Gate("Folk_Jay_0").Standing);
            yield return GoTo("Verdance_Aldermere_1");
            Assert.IsFalse(Gate("Folk_Jay_0").Standing, "the lane empty");
            Assert.IsFalse(Gate("Folk_Finch_1").Standing);
        }

        [UnityTest]
        public IEnumerator StoppedTheVillageIsStillHereWithHalfASong()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            w.Set("verdance.teodor.met", 1);
            yield return _replay.Talk("Teodor", "Aldermere_Teodor", new[] { 1, 0 });   // I won't let you fade
            Assert.IsTrue(w.Is("verdance.aldermere.stopped"));

            _replay.Heard.Clear();
            yield return _replay.Talk("Hollin", "Aldermere_Hollin", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("still here")), "before the Choir is answered, the standing line");
            Assert.IsFalse(w.Is("verdance.aldermere.after_heard"));

            w.Set(Bosses.FlagKey("choir"), 1);
            _replay.Heard.Clear();
            yield return _replay.Talk("Hollin", "Aldermere_Hollin", new[] { 1 });   // what do you need?
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("stopped halfway")), "the bells stopped mid-song");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("the rest of the song")));
            Assert.IsTrue(w.Is("verdance.aldermere.after_heard"));
            _replay.Heard.Clear();
            yield return _replay.Talk("Teodor", "Aldermere_Teodor", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("We are still here")), "after, from either of them, the standing line");

            yield return GoTo("Verdance_Aldermere_2");
            Assert.IsFalse(Gate("Teodor_Greybox").Standing, "Teodor will neither stop them nor help them: gone");
            Assert.IsTrue(Gate("Hollin_Greybox").Standing, "Hollin stays");
            Assert.IsTrue(Gate("Folk_Jay_0").Standing, "and the village");
            yield return GoTo("Verdance_Aldermere_3");
            Assert.IsFalse(Gate("Folk_Jay_0").Standing, "no paper in the ash field");
        }
    }
}
