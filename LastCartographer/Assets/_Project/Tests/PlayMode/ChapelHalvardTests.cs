using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The Salt Chapel after the first hunt (boss-sheets.md 6.3): Halvard stands by the altar once the fight is won, says
    /// he is withdrawing, not retreating, and is gone once he has said it.
    /// </summary>
    public class ChapelHalvardTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        [UnityTest]
        public IEnumerator HeWithdrawsAndSaysSoThenTheChapelIsTheSaltsAgain()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            var scene = WorldGraph.GreyboxPrefix + "Saltmarrow_Chapel";
            RoomManager.Instance.Transition(scene, "West");
            yield return RouteReplay.Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 10f, scene);
            yield return null;

            var halvard = Object.FindObjectsByType<FlagPresence>(FindObjectsSortMode.None).FirstOrDefault(p => p.name == "Halvard_Greybox");
            Assert.IsNotNull(halvard, "Halvard by the altar, standing by the world's say");
            Assert.IsFalse(halvard!.Standing, "not before the hunt is won");

            w.Set(Bosses.FlagKey("halvard"), 1);
            Assert.IsTrue(halvard.Standing, "the hunt won: he is there");
            yield return _replay.Talk("Halvard", "Chapel_Halvard_After", new[] { 0 });   // you could let me go
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Not retreating")));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("all the mercy a count allows")));
            Assert.IsTrue(w.Is("saltmarrow.chapel.halvard_spoke"));
            Assert.IsFalse(halvard.Standing, "and he is gone");

            _replay.Heard.Clear();
            yield return _replay.Talk("Halvard", "Chapel_Halvard_After", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Go on, journeyman")), "the standing line, if asked again");
        }
    }
}
