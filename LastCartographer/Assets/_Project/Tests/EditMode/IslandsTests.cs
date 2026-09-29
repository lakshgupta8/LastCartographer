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
        public void AWeakenedAnchorDriftsInHalf()
        {
            var w = new WorldState();
            Places.Anchor(w, "Saltmarrow_B");
            Assert.AreEqual(0, Islands.Count(w), "anchored and whole: nothing drifts");
            Memories.Bind(w, "dotha.nine_songs");
            Assert.IsTrue(Offerings.Offer(w, "chapel_door", "dotha.nine_songs"), "its songs given to the chapel's door");
            Assert.AreEqual(1, Offerings.Weakened(w, "Saltmarrow_B"));
            CollectionAssert.AreEqual(new[] { "Saltmarrow_B" }, Islands.HalfPlaces(w));
            Assert.AreEqual(1, Islands.Count(w));
            var drifts = Islands.Drifting(w);
            Assert.AreEqual(1, drifts.Count);
            Assert.AreEqual("Island_Half_Saltmarrow_B", drifts[0].Scene);
            Assert.IsTrue(drifts[0].IsHalf);
            Assert.IsTrue(drifts[0].IsGeneric, "built like a Remnant island, paler");
            Assert.AreEqual(Islands.HalfNode, drifts[0].Node);
            Assert.AreEqual("Merrow's End", drifts[0].Name, "named for what it still is");
            Assert.IsFalse(Islands.Present(w).Any(i => i.Id == "Merrows_End"), "not the released island: the place is still standing");
            Assert.AreEqual(drifts[0].Node, Islands.Resolve(w, "Island_Half_Saltmarrow_B").Node);

            var held = new WorldState();
            Places.Hold(held, "Saltmarrow_B");
            Memories.Bind(held, "dotha.nine_songs");
            Offerings.Offer(held, "chapel_door", "dotha.nine_songs");
            Assert.AreEqual(0, Islands.Count(held), "a held place is held by its people: nothing to halve");
        }

        [Test]
        public void TheDriftIsOrderedAndNamedForWhatThePlacesWere()
        {
            var w = new WorldState();
            Places.Release(w, "Saltmarrow_Stilts");
            Places.Release(w, "Emberdown_Baths_2");
            w.Set("emberdown.hollowvein.buried", true);
            Places.Release(w, "Verdance_Aldermere_2");
            var drifts = Islands.Drifting(w);
            CollectionAssert.AreEqual(new[] { "Island_Hollowvein", "Island_Aldermere", "Island_Emberdown_Baths_2", "Island_Saltmarrow_Stilts" },
                drifts.Select(d => d.Scene).ToList(), "the authored islands in their order, then the rest");
            Assert.AreEqual("The baths", drifts[2].Name, "a planned room's name");
            Assert.IsTrue(drifts[3].Name.Length > 0 && !drifts[3].Name.Contains("_"), "a built room's atlas name, or its id read aloud");
            Assert.IsTrue(drifts[2].IsGeneric && !drifts[0].IsGeneric);
            Assert.AreEqual(Islands.GenericNode, drifts[2].Node);
            Assert.AreEqual("Island_Aldermere", Islands.Resolve(w, "Island_Aldermere").Node);
            Assert.IsNull(Islands.Resolve(w, "Island_Lowmarket"), "not drifting in this world");
            Assert.IsNull(Islands.Resolve(w, "Greybox_Saltmarrow_B"));
            Assert.IsTrue(Islands.IsIslandScene("Island_X") && !Islands.IsIslandScene("Greybox_X"));
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
