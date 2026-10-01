#nullable enable
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
    /// The fledglings in their room (CHR-14): the loop reads the world (nobody glides at first, one more for each
    /// ability and further, an anchored place's never, a fade thins them, the Open World flies the newest), and the
    /// birds leap in turn along the flights it computes, in the clips the phases name, and are gone until their next turn.
    /// </summary>
    public class FledglingLoopPlayTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static IEnumerator Enter(string scene, string spawn)
        {
            var rm = RoomManager.Instance;
            rm.Transition(scene, spawn);
            yield return RouteReplay.Until(() => rm.CurrentRoom == scene && !rm.IsTransitioning, 15f, scene);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheLoopReadsTheWorld()
        {
            yield return _replay.Boot();
            yield return Enter("Greybox_Saltmarrow_Stilts", "West");
            var loop = Room.Current!.GetComponentInChildren<FledglingLoop>(true);
            Assert.IsNotNull(loop, "the coast's fledglings leap from the top stilt-roost");
            Assert.AreEqual(Region.Saltmarrow, loop.Region);
            Assert.AreEqual("Saltmarrow_Stilts", loop.PlaceId);
            Assert.AreEqual(Dressing.Leapers, loop.Birds.Count);
            Assert.IsTrue(loop.Birds.All(b => b.GetComponent<InkSheetPlayer>() != null && b.GetComponent<InkSheetPlayer>().Has("glide")), "every bird on the gull's sheets");
            Assert.AreEqual(Dressing.Leapers, loop.State.Leaping, "six leap from the start");
            Assert.AreEqual(0, loop.State.Gliding, "and nobody glides");
            float drop = loop.Wingspan * FledglingLoop.DropReach;
            Assert.AreEqual(drop, loop.Reach(0), 1e-4f, "a dropper's leap");

            var wren = Object.FindFirstObjectByType<WrenController>();
            wren.GetComponent<AbilitySet>().Unlock(Ability.Wingbeat);
            Assert.AreEqual(1, loop.State.Gliding, "the first piece of the sky: one glides");
            Assert.Greater(loop.Reach(0), drop, "and further than a drop");
            Assert.AreEqual(drop, loop.Reach(1), 1e-4f, "the rest drop as they always have");
            wren.GetComponent<AbilitySet>().Unlock(Ability.Talonhold);
            Assert.AreEqual(2, loop.State.Gliding);
            Assert.Greater(loop.Reach(1), loop.Reach(0), "the newest goes furthest");
            Assert.IsFalse(loop.Flies(1));

            var w = GameState.World;
            FadeStages.Advance(w, "Saltmarrow_Stilts", 2);
            Assert.AreEqual(Dressing.Leapers / 2, loop.State.Leaping, "a fade thins them");
            w.Set(Places.Key("Saltmarrow_Stilts"), (int)PlaceFate.Anchored);
            Assert.AreEqual(0, loop.State.Gliding, "anchored: the same leap forever");
            Assert.IsFalse(loop.Glides(0));

            w.Set(Endings.ChosenFlag, (int)Ending.Open);   // the choice itself is the frame's (EndingsTests); here only what it does to the sky
            Assert.AreEqual(loop.State.Leaping, loop.State.Gliding, "the Open World: they all glide, even here");
            Assert.IsTrue(loop.State.OneFlies);
            Assert.IsTrue(loop.Flies(loop.State.Gliding - 1), "and the newest doesn't come down");
            var away = loop.Evaluate(loop.State.Gliding - 1, (loop.State.Gliding - 1) * loop.Interval + FledglingLoop.LeapSeconds + loop.FlightSeconds(loop.State.Gliding - 1) * 0.9f);
            Assert.Greater(away.Position.y, loop.Perch.y + 1f, "up and away");
        }

        [UnityTest]
        public IEnumerator TheyLeapInTurnAlongTheirFlightsAndAreGoneUntilTheirNextTurn()
        {
            yield return _replay.Boot();
            yield return Enter("Greybox_Halden_Bridges_2", "West");
            var loop = Room.Current!.GetComponentInChildren<FledglingLoop>(true);
            Assert.IsNotNull(loop, "the plateau's pigeons off the parapet");
            Assert.AreEqual(-1, loop.Direction, "onto the net under the bridge, back the way she came");
            Assert.AreEqual(0, loop.State.Gliding, "Halden was anchored before the story: nobody glides");

            // Bird 0's turn starts at zero: the leap at the perch, then the drop, then the landing, then gone, then back on the perch.
            var start = loop.Evaluate(0, 0.05f);
            Assert.AreEqual(FledglingLoop.Phase.Leaping, start.Phase); Assert.IsTrue(start.Visible); Assert.AreEqual(loop.Perch, start.Position);
            float flight = loop.FlightSeconds(0);
            var mid = loop.Evaluate(0, FledglingLoop.LeapSeconds + flight * 0.5f);
            Assert.AreEqual(FledglingLoop.Phase.Dropping, mid.Phase);
            Assert.Less(mid.Position.x, loop.Perch.x, "leaping west");
            Assert.Greater(mid.Position.x, loop.Perch.x - loop.Reach(0));
            Assert.Less(mid.Position.y, loop.Perch.y); Assert.Greater(mid.Position.y, loop.LandingY);
            var down = loop.Evaluate(0, FledglingLoop.LeapSeconds + flight + 0.1f);
            Assert.AreEqual(FledglingLoop.Phase.Landing, down.Phase);
            Assert.AreEqual(loop.LandingY, down.Position.y, 1e-4f);
            Assert.AreEqual(loop.Perch.x - loop.Reach(0), down.Position.x, 1e-4f, "a dropper's reach");
            var gone = loop.Evaluate(0, FledglingLoop.LeapSeconds + flight + FledglingLoop.LandSeconds + 0.5f);
            Assert.AreEqual(FledglingLoop.Phase.Gone, gone.Phase); Assert.IsFalse(gone.Visible);
            var back = loop.Evaluate(0, loop.Period - 0.5f);
            Assert.AreEqual(FledglingLoop.Phase.Perched, back.Phase); Assert.IsTrue(back.Visible); Assert.AreEqual(loop.Perch, back.Position);
            // The second waits its turn on the perch; the third is still climbing back.
            Assert.AreEqual(FledglingLoop.Phase.Perched, loop.Evaluate(1, 0.05f).Phase);
            Assert.AreEqual(FledglingLoop.Phase.Leaping, loop.Evaluate(1, loop.Interval + 0.05f).Phase, "each in turn");
            Assert.AreEqual(FledglingLoop.Phase.Gone, loop.Evaluate(2, 0.05f).Phase);

            // And the loop moves them in their clips: a moment into the first turn (applied by hand: a batch frame can be longer than a leap).
            loop.Apply(FledglingLoop.LeapSeconds + 0.2f);
            var first = loop.Birds[0];
            Assert.IsTrue(first.gameObject.activeSelf, "the first is in the air");
            Assert.AreEqual("drop", first.GetComponent<InkSheetPlayer>().Current);
            Assert.Less(first.localPosition.x, loop.Perch.x);
            Assert.AreEqual("idle", loop.Birds[1].GetComponent<InkSheetPlayer>().Current, "the next on the perch");
            Assert.IsFalse(loop.Birds[2].gameObject.activeSelf, "the third is gone");
            // Running, every bird is where Evaluate says at the loop's own time.
            loop.Restart();
            yield return null; yield return null;
            Assert.Greater(loop.Time, 0f, "the loop runs");
            for (int i = 0; i < loop.Birds.Count; i++)
            {
                var p = loop.Evaluate(i, loop.Time);
                Assert.AreEqual(p.Visible, loop.Birds[i].gameObject.activeSelf, "bird " + i + " shows as evaluated");
                if (p.Visible) Assert.AreEqual(FledglingLoop.ClipOf(p.Phase), loop.Birds[i].GetComponent<InkSheetPlayer>().Current, "bird " + i + "'s clip");
            }

            // A fade thins them in play, at once.
            GameState.World.Set(FadeStages.Key("Halden_Bridges_2"), 3);
            yield return null;
            Assert.AreEqual(0, loop.State.Leaping);
            Assert.IsTrue(loop.Birds.All(b => !b.gameObject.activeSelf), "stage 3: nobody left to leap");
        }
    }
}
