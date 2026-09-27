using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The economy (DES-05): seeds, prices and the market, buying, desk upgrades, and the save.</summary>
    public class EconomyTests
    {
        [Test]
        public void SeedsAddAndSpendAndRefuse()
        {
            var w = new WorldState();
            Assert.AreEqual(0, Economy.Seeds(w));
            int seen = -1;
            Economy.SeedsChanged += n => seen = n;
            Economy.AddSeeds(w, 5);
            Assert.AreEqual(5, seen);
            Economy.AddSeeds(w, 0);
            Assert.AreEqual(5, Economy.Seeds(w));
            Assert.IsFalse(Economy.SpendSeeds(w, 6), "cannot go below zero");
            Assert.IsTrue(Economy.SpendSeeds(w, 5));
            Assert.AreEqual(0, Economy.Seeds(w));
        }

        [Test]
        public void PricesFollowTheIrisFields()
        {
            var w = new WorldState();
            var lantern = Economy.Find("Saltmarrow", InstrumentKind.FieldLantern);
            Assert.IsNotNull(lantern);
            Assert.AreEqual(12, Economy.PriceOf(w, lantern));
            Assert.IsFalse(Economy.IrisBurned(w));
            w.Set(Economy.IrisBurnedFlag, true);
            Assert.IsTrue(Economy.IrisBurned(w));
            Assert.AreEqual(18, Economy.PriceOf(w, lantern), "half again, rounded up");
            Assert.AreEqual(12, Economy.PriceOf(w, Economy.Find("Saltmarrow", InstrumentKind.IrisTincture)), "8 × 1.5");

            var w2 = new WorldState();
            CommissionCatalog.Reset(); CommissionCatalog.EnsureDefaults();
            Commissions.Post(w2, Economy.IrisCommission);
            Commissions.Take(w2, Economy.IrisCommission);
            Commissions.Fail(w2, Economy.IrisCommission);
            Assert.IsTrue(Economy.IrisBurned(w2), "a failed Iris Harvest is burned fields");
            Assert.AreEqual(4, Economy.StockAt("Saltmarrow").Count, "Sable sells four");
            Assert.IsEmpty(Economy.StockAt("Nowhere"));
        }

        [Test]
        public void BuyingNeedsSeedsAndHappensOnce()
        {
            var w = new WorldState();
            var seal = Economy.Find("Saltmarrow", InstrumentKind.WaxSeal);
            Assert.IsFalse(Economy.CanBuy(w, seal), "broke");
            Assert.IsFalse(Economy.Buy(w, seal));
            Economy.AddSeeds(w, 10);
            StockItem bought = null;
            Economy.Bought += i => bought = i;
            Assert.IsTrue(Economy.Buy(w, seal));
            Assert.AreSame(seal, bought);
            Assert.IsTrue(w.Equipment.OwnsInstrument(InstrumentKind.WaxSeal));
            Assert.AreEqual(4, Economy.Seeds(w));
            Assert.IsFalse(Economy.CanBuy(w, seal), "owned");
            Assert.IsFalse(Economy.Buy(w, seal));
            Assert.AreEqual(4, Economy.Seeds(w), "nothing charged twice");
            Assert.IsFalse(Economy.Buy(w, null));
        }

        [Test]
        public void DeskUpgradesCostScrapsAndCap()
        {
            var w = new WorldState();
            Assert.IsFalse(Economy.CanBuyMask(w));
            Commissions.AddScraps(w, 14);
            int masks = 0;
            Economy.MaskBought += n => masks = n;
            for (int i = 1; i <= Economy.MaxMaskUpgrades; i++)
            {
                Assert.IsTrue(Economy.BuyMask(w), "upgrade " + i);
                Assert.AreEqual(i, Economy.MaskUpgrades(w));
                Assert.AreEqual(i, masks);
            }
            Assert.IsTrue(Economy.MasksFull(w), "nine masks is the ceiling");
            Assert.IsFalse(Economy.BuyMask(w));
            Assert.AreEqual(2, Economy.Scraps(w), "four upgrades at three scraps");
            Assert.IsFalse(Economy.CanBuySlot(w), "two scraps is not four");
            Commissions.AddScraps(w, 2);
            Assert.IsTrue(Economy.BuySlot(w));
            Assert.IsTrue(w.Equipment.FourthSlotUnlocked);
            Assert.AreEqual(4, w.Equipment.SlotCount);
            Assert.AreEqual(0, Economy.Scraps(w));
            Assert.IsFalse(Economy.BuySlot(w), "once");
        }

        [Test]
        public void TheSaveKeepsThePurse()
        {
            var w = new WorldState();
            Economy.AddSeeds(w, 7);
            Commissions.AddScraps(w, 3);
            Economy.BuyMask(w);
            var back = GameState.FromJson(GameState.ToJson(w));
            Assert.AreEqual(7, Economy.Seeds(back));
            Assert.AreEqual(1, Economy.MaskUpgrades(back));
            Assert.AreEqual(0, Economy.Scraps(back));
        }
    }
}
