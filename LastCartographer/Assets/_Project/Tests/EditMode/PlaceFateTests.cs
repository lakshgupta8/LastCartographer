using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The regional decision (DES-03, PRG-13): decided once, mirrors the anchored set, stops fading for anchored and held, survives saves.</summary>
    public class PlaceFateTests
    {
        [Test]
        public void AFateIsDecidedOnceAndFinal()
        {
            var w = new WorldState();
            var seen = new List<(string, PlaceFate)>();
            void On(string p, PlaceFate f) => seen.Add((p, f));
            Places.FateChanged += On;
            try
            {
                Assert.AreEqual(PlaceFate.Unwritten, Places.FateOf(w, "Saltmarrow_A"));
                Assert.IsFalse(Places.Decide(w, "Saltmarrow_A", PlaceFate.Unwritten), "unwritten is not a decision");
                Assert.IsTrue(Places.Anchor(w, "Saltmarrow_A"));
                Assert.IsFalse(Places.Release(w, "Saltmarrow_A"), "no second decision");
                Assert.AreEqual(PlaceFate.Anchored, Places.FateOf(w, "Saltmarrow_A"));
                Assert.IsTrue(w.AnchoredPlaces.Contains("Saltmarrow_A"), "the anchored set mirrors it");
                Assert.IsTrue(Places.Hold(w, "Emberdown_Rest"));
                Assert.IsTrue(Places.Release(w, "Verdance_Aldermere"));
                Assert.IsFalse(w.AnchoredPlaces.Contains("Emberdown_Rest"));
                CollectionAssert.AreEqual(new[] { ("Saltmarrow_A", PlaceFate.Anchored), ("Emberdown_Rest", PlaceFate.Held), ("Verdance_Aldermere", PlaceFate.Released) }, seen);
                Assert.AreEqual(1, Places.Count(w, PlaceFate.Anchored));
                Assert.AreEqual(1, Places.Count(w, PlaceFate.Held));
                Assert.AreEqual(0, Places.Count(w, PlaceFate.Unwritten), "unwritten places are not counted; they have no flag");
            }
            finally { Places.FateChanged -= On; }
        }

        [Test]
        public void AnchoredAndHeldStopFadingReleasedDoesNot()
        {
            var w = new WorldState();
            Places.Anchor(w, "A"); Places.Hold(w, "H"); Places.Release(w, "R");
            Assert.IsFalse(FadeStages.Advance(w, "A", 2), "anchored: no fade");
            Assert.IsFalse(FadeStages.Advance(w, "H", 2), "held: no fade");
            Assert.IsTrue(FadeStages.Advance(w, "R", 2), "released: fades on");
            Assert.IsTrue(FadeStages.Advance(w, "U", 1), "unwritten: fades on");
            Assert.IsFalse(Places.IsFadeStopped(w, "R"));
            Assert.IsTrue(Places.IsFadeStopped(w, "H"));
        }

        [Test]
        public void VerbsParseAndFatesSurviveTheSave()
        {
            Assert.IsTrue(Places.TryParse("anchor", out var f) && f == PlaceFate.Anchored);
            Assert.IsTrue(Places.TryParse("Held", out f) && f == PlaceFate.Held);
            Assert.IsTrue(Places.TryParse("release", out f) && f == PlaceFate.Released);
            Assert.IsFalse(Places.TryParse("burn", out _));
            Assert.AreEqual("released", Places.Describe(PlaceFate.Released));

            var w = new WorldState();
            Places.Anchor(w, "Saltmarrow_A");
            Places.Hold(w, "Emberdown_Rest");
            var back = GameState.FromJson(GameState.ToJson(w));
            Assert.AreEqual(PlaceFate.Anchored, Places.FateOf(back, "Saltmarrow_A"));
            Assert.AreEqual(PlaceFate.Held, Places.FateOf(back, "Emberdown_Rest"));
            Assert.IsTrue(back.AnchoredPlaces.Contains("Saltmarrow_A"));
        }
    }
}
