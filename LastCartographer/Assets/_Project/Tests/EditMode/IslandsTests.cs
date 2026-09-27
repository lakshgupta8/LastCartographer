using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The Blank is built from Wren's choices (bible 10): which islands drift in, and why.</summary>
    public class IslandsTests
    {
        [SetUp]
        public void SetUp() { CommissionCatalog.Reset(); CommissionCatalog.EnsureDefaults(); }

        [Test]
        public void EveryBCommissionHasAnAuthoredIsland()
        {
            var seeded = CommissionCatalog.All.Where(d => d.SeedsIsland).Select(d => d.BlankIsland).ToList();
            CollectionAssert.AreEquivalent(seeded, Islands.Authored.Select(i => i.Id), "one island per [B] commission, and no others authored");
            Assert.AreEqual(Islands.Authored.Length, Islands.Authored.Select(i => i.Node).Distinct().Count());
            Assert.IsFalse(Islands.Authored.Any(i => i.Node == Islands.GenericNode));
        }

        [Test]
        public void AnIslandDriftsInOnlyIfSheLeftThePlaceToTheBlank()
        {
            var w = new WorldState();
            Assert.AreEqual(0, Islands.Count(w), "a new game: nothing left to the Blank");

            Places.Anchor(w, "Halden_Lowmarket_2");
            Assert.IsFalse(Islands.Present(w).Any(i => i.Id == "Lowmarket"), "anchored is not left");
            var v = new WorldState();
            Places.Release(v, "Halden_Lowmarket_2");
            Assert.IsTrue(Islands.Present(v).Any(i => i.Id == "Lowmarket"), "the strike broke: Lowmarket drifts in");

            w.Set("saltmarrow.dotha.decided", 2);
            Assert.IsTrue(Islands.Present(w).Any(i => i.Id == "Merrows_End"), "Dotha let go");
            w.Set("emberdown.hollowvein.walked", true);
            Assert.IsFalse(Islands.Present(w).Any(i => i.Id == "Hollowvein"), "walked: the families have them");
            w.Set("emberdown.hollowvein.buried", true);
            Assert.IsTrue(Islands.Present(w).Any(i => i.Id == "Hollowvein"), "left buried: thirty-one, standing, in the white");
            Places.Release(w, "Verdance_Aldermere_2");
            w.Set("verdance.gate.inn_visited", true);
            Assert.AreEqual(4, Islands.Present(w).Count);
        }

        [Test]
        public void AnyOtherReleasedPlaceIsAGenericIsland()
        {
            var w = new WorldState();
            Places.Release(w, "Verdance_Aldermere_2");
            Places.Release(w, "Emberdown_Baths_2");
            Places.Release(w, "Saltmarrow_Stilts");
            Places.Anchor(w, "Halden_Hall_2");
            CollectionAssert.AreEqual(new[] { "Emberdown_Baths_2", "Saltmarrow_Stilts" }, Islands.GenericPlaces(w), "Aldermere is authored; the Hall is anchored");
            Assert.AreEqual(3, Islands.Count(w));
        }
    }
}
