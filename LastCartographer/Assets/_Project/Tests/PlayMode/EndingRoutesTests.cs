#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Every ending is reachable from a new game (DES-12): each route in EndingRoutes replayed through the shipped Yarn
    /// project (<see cref="RouteReplay"/>), with the step's zone proven reachable on the macro map first, using only
    /// what the route has earned.
    /// </summary>
    public class EndingRoutesTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        IEnumerator Replay(Ending ending)
        {
            yield return _replay.Boot();
            var route = EndingRoutes.For(ending);
            Assert.IsNotNull(route, ending + " has a route");
            yield return _replay.Steps(ending.ToString(), route.Steps, ending);
        }

        [UnityTest] public IEnumerator TheFixedWorldIsReachable() { yield return Replay(Ending.Fixed); }
        [UnityTest] public IEnumerator TheOpenWorldIsReachable() { yield return Replay(Ending.Open); }
        [UnityTest] public IEnumerator TheUnwrittenIsReachable() { yield return Replay(Ending.Unwritten); }
        [UnityTest] public IEnumerator TheRestIsReachable() { yield return Replay(Ending.Rest); }

        [UnityTest]
        public IEnumerator TheRoutesEarnWhatTheyClaim()
        {
            yield return _replay.Boot();
            // The Fixed World's route carries every stone but the frame's; the Rest's carries none and anchors nothing.
            var fixedNotes = string.Join(" ", EndingRoutes.For(Ending.Fixed).Steps.Select(s => s.Note));
            foreach (var h in Keystones.Homes.Where(h => h != Keystones.InTheFrame))
                StringAssert.Contains("keystone." + h, fixedNotes, "the Fixed World's route picks up the " + h + " stone");
            var rest = EndingRoutes.For(Ending.Rest).Steps;
            Assert.IsFalse(rest.Any(s => s.Note.Contains("keystone.")), "the Rest carries nothing");
            Assert.IsFalse(rest.Any(s => s.Key == "Lowmarket_Strike"), "and anchors nothing");
            // Every ability but the Sky is earned on the long routes, in the map's order.
            foreach (var e in new[] { Ending.Fixed, Ending.Open })
            {
                var grants = EndingRoutes.For(e).Steps.Where(s => s.Grants != Ability.None).Select(s => s.Grants).ToList();
                CollectionAssert.AreEqual(new[] { Ability.Wingbeat, Ability.Talonhold, Ability.Inkthread, Ability.Clarity, Ability.Windmemory }, grants.Take(5).ToList(), e + ": the spine's order");
            }
        }
    }
}
