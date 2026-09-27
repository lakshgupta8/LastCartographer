using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>Fade stages (DES-04, PRG-14): advance only, anchored places hold, restore is the way back, saves carry it.</summary>
    public class FadeStageTests
    {
        [Test]
        public void StagesOnlyAdvanceAndClamp()
        {
            var w = new WorldState();
            var seen = new List<(string, int)>();
            void On(string p, int s) => seen.Add((p, s));
            FadeStages.Changed += On;
            try
            {
                Assert.AreEqual(0, FadeStages.Get(w, "Saltmarrow_B"));
                Assert.IsTrue(FadeStages.Advance(w, "Saltmarrow_B", 2));
                Assert.IsFalse(FadeStages.Advance(w, "Saltmarrow_B", 2), "same stage is not an advance");
                Assert.IsFalse(FadeStages.Advance(w, "Saltmarrow_B", 1), "stages never go back by advancing");
                Assert.IsTrue(FadeStages.Advance(w, "Saltmarrow_B", 9), "clamped to blank");
                Assert.AreEqual(4, FadeStages.Get(w, "Saltmarrow_B"));
                Assert.IsFalse(FadeStages.Advance(w, "", 1));
                CollectionAssert.AreEqual(new[] { ("Saltmarrow_B", 2), ("Saltmarrow_B", 4) }, seen);
            }
            finally { FadeStages.Changed -= On; }
        }

        [Test]
        public void AnchoredPlacesDoNotFadeAndRestoreGoesBack()
        {
            var w = new WorldState();
            w.AnchoredPlaces.Add("Halden_Lowmarket");
            Assert.IsFalse(FadeStages.Advance(w, "Halden_Lowmarket", 3), "anchoring freezes the stage");
            Assert.AreEqual(0, FadeStages.Get(w, "Halden_Lowmarket"));

            FadeStages.Advance(w, "Verdance_Gate", 3);
            Assert.IsFalse(FadeStages.Restore(w, "Verdance_Gate", 3));
            Assert.IsTrue(FadeStages.Restore(w, "Verdance_Gate", 1), "re-survey after erasure");
            Assert.AreEqual(1, FadeStages.Get(w, "Verdance_Gate"));
        }

        [Test]
        public void InkFollowsTheStageTable()
        {
            Assert.AreEqual(1f, FadeStages.InkFor(0));
            Assert.AreEqual(0.8f, FadeStages.InkFor(1), 0.001f);
            Assert.AreEqual(0.55f, FadeStages.InkFor(2), 0.001f);
            Assert.AreEqual(0.3f, FadeStages.InkFor(3), 0.001f);
            Assert.AreEqual(0f, FadeStages.InkFor(4));
            Assert.AreEqual("washing", FadeStages.Describe(2));
        }

        [Test]
        public void StageSurvivesTheSaveRoundTrip()
        {
            var w = new WorldState();
            FadeStages.Advance(w, "Saltmarrow_B", 2);
            var back = GameState.FromJson(GameState.ToJson(w));
            Assert.AreEqual(2, FadeStages.Get(back, "Saltmarrow_B"));
        }
    }
}
