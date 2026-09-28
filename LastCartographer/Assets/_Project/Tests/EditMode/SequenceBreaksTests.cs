using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// A noticed sequence break (world-map §2): a zone only a soft gap could have brought her to is a break, once;
    /// it writes its flag and pays a scrap; the matrix's break playthroughs start in one.
    /// </summary>
    public class SequenceBreaksTests
    {
        [Test]
        public void AZoneOnlyASoftGapReachesIsABreak()
        {
            var w = new WorldState();
            Assert.IsTrue(SequenceBreaks.IsBreak(Ability.None, w, "Saltmarrow.SaltChapel"), "past the lighthouse gap with no Wingbeat");
            Assert.IsTrue(SequenceBreaks.IsBreak(Ability.None, w, "Emberdown.KettilsRest"), "up the climb with no Wingbeat");
            Assert.IsTrue(SequenceBreaks.IsBreak(Ability.None, w, "Verdance.QuietHouse"));
            Assert.IsFalse(SequenceBreaks.IsBreak(Ability.Wingbeat, w, "Saltmarrow.SaltChapel"), "with Wingbeat she walked there");
            Assert.IsFalse(SequenceBreaks.IsBreak(Ability.None, w, "Saltmarrow.Quay"), "the quay is anyone's");
            Assert.IsFalse(SequenceBreaks.IsBreak(Ability.None, w, "Emberdown.CinderBaths"), "a Talonhold shaft is hard: she can't be there");
            Assert.IsFalse(SequenceBreaks.IsBreak(Ability.None, w, "Halden.SevenBridges"), "the Plateau is hard either way");
            Assert.IsFalse(SequenceBreaks.IsBreak(Ability.None, w, "Nowhere.AtAll"));
            Assert.IsTrue(SequenceBreaks.IsBreak(Ability.None, null, "Saltmarrow.SaltChapel"), "no world is no flags");
        }

        [Test]
        public void ARoomIsNoticedOnceAndPaysAScrap()
        {
            var w = new WorldState();
            Assert.AreEqual("Saltmarrow.SaltChapel", SequenceBreaks.ZoneOfRoom("Greybox_Saltmarrow_Chapel"));
            Assert.IsNull(SequenceBreaks.ZoneOfRoom("Feel_Course"), "the course is on no place");
            Assert.IsNull(SequenceBreaks.ZoneOfRoom(null));
            Assert.IsNull(SequenceBreaks.Notice(w, "Greybox_Saltmarrow_A"), "the quay is no break");
            int scraps = Commissions.Scraps(w);
            Assert.AreEqual("Saltmarrow.SaltChapel", SequenceBreaks.Notice(w, "Greybox_Saltmarrow_Chapel"));
            Assert.IsTrue(SequenceBreaks.IsNoticed(w, "Saltmarrow.SaltChapel"));
            Assert.IsTrue(w.Is(SequenceBreaks.FlagKey("Saltmarrow.SaltChapel")));
            Assert.AreEqual(scraps + SequenceBreaks.ScrapReward, Commissions.Scraps(w), "the quiet reward");
            Assert.IsNull(SequenceBreaks.Notice(w, "Greybox_Saltmarrow_Chapel"), "once");
            Assert.AreEqual(scraps + SequenceBreaks.ScrapReward, Commissions.Scraps(w));

            var winged = new WorldState();
            winged.Set(AbilitySet.FlagKey(Ability.Wingbeat), true);
            Assert.IsNull(SequenceBreaks.Notice(winged, "Greybox_Saltmarrow_Chapel"), "with Wingbeat, nothing to notice");
            Assert.IsNull(SequenceBreaks.Notice(null, "Greybox_Saltmarrow_Chapel"));
        }

        [Test]
        public void TheMatrixsBreaksStartInOne()
        {
            foreach (var p in Playthroughs.All())
            {
                if (p.Break == Playthroughs.SequenceBreak.None) continue;
                Assert.IsTrue(SequenceBreaks.IsBreak(Ability.None, new WorldState(), p.Steps[0].Zone), p.Name + " starts where the game would notice");
            }
            var fixedRoute = Playthroughs.Build(Ending.Fixed, Playthroughs.ClimbOrder.EmberdownFirst, Playthroughs.Act2Order.HaldenFirst);
            Assert.IsFalse(SequenceBreaks.IsBreak(Ability.None, new WorldState(), fixedRoute.Steps[0].Zone), "the plain route starts on the shore, noticed by nobody");
        }
    }
}
