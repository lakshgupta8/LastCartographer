using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>Something a hub sells: an Instrument at a base price in iris seeds.</summary>
    public sealed class StockItem
    {
        public InstrumentKind Kind;
        public int Price;
        public string Hub;
        /// <summary>What the seller says of it.</summary>
        public string Pitch;
    }

    /// <summary>
    /// The economy (DES-05, docs/design/economy.md). Two currencies: <b>iris seeds</b> (Numbers["$iris_seeds"]),
    /// dropped by enemies and found in the fields, spent at hub shops on Instruments; and <b>vellum scraps</b>
    /// (Commissions.ScrapsKey), from bosses, commissions and caches, spent at desks on masks and the fourth
    /// belt slot. Ferrymen prices rise by half if the iris fields burn. No XP: this is all of Wren's growth
    /// that money buys.
    /// </summary>
    public static class Economy
    {
        public const string SeedsKey = "$iris_seeds";
        public const string MaskUpgradesKey = "$mask_upgrades";
        public const int MaskUpgradeCost = 3;
        /// <summary>Masks go 5 to 9 (GDD 7): four upgrades.</summary>
        public const int MaxMaskUpgrades = 4;
        public const int SlotUpgradeCost = 4;
        public const string IrisBurnedFlag = "saltmarrow.iris_burned";
        public const string IrisCommission = "saltmarrow.iris_harvest";
        public const float BurnedMarkup = 1.5f;

        public static event Action<int> SeedsChanged;
        public static event Action<StockItem> Bought;
        public static event Action<int> MaskBought;
        public static event Action SlotBought;

        // ---- Seeds and scraps ------------------------------------------------------------------------------

        public static int Seeds(WorldState w) => w.Numbers.TryGetValue(SeedsKey, out var n) ? (int)n : 0;

        public static void AddSeeds(WorldState w, int n)
        {
            if (n <= 0) return;
            w.Numbers[SeedsKey] = Seeds(w) + n;
            SeedsChanged?.Invoke(Seeds(w));
        }

        public static bool SpendSeeds(WorldState w, int n)
        {
            if (n < 0 || Seeds(w) < n) return false;
            w.Numbers[SeedsKey] = Seeds(w) - n;
            SeedsChanged?.Invoke(Seeds(w));
            return true;
        }

        public static int Scraps(WorldState w) => Commissions.Scraps(w);

        public static bool SpendScraps(WorldState w, int n)
        {
            if (n < 0 || Scraps(w) < n) return false;
            w.Numbers[Commissions.ScrapsKey] = Scraps(w) - n;
            return true;
        }

        // ---- Shops ---------------------------------------------------------------------------------------

        static readonly List<StockItem> _stock = new List<StockItem>
        {
            new StockItem { Kind = InstrumentKind.FieldLantern, Price = 12, Hub = "Saltmarrow", Pitch = "Honest light. The Guild's, before it was mine." },
            new StockItem { Kind = InstrumentKind.IrisTincture, Price = 8, Hub = "Saltmarrow", Pitch = "Pressed from the pale iris. Don't ask whose fields." },
            new StockItem { Kind = InstrumentKind.WaxSeal, Price = 6, Hub = "Saltmarrow", Pitch = "Guild wax. They'd hang me for having it." },
            new StockItem { Kind = InstrumentKind.TetherHook, Price = 15, Hub = "Saltmarrow", Pitch = "A Ferryman's hook. It has held people over worse than a drop." },
        };

        public static IReadOnlyList<StockItem> Stock => _stock;

        public static List<StockItem> StockAt(string hub) => _stock.FindAll(s => s.Hub == hub);

        public static StockItem Find(string hub, InstrumentKind kind) => _stock.Find(s => s.Hub == hub && s.Kind == kind);

        /// <summary>Ferrymen prices follow the Iris Harvest (bible 6.2, 8): burned fields, dearer tincture and all.</summary>
        public static bool IrisBurned(WorldState w) => w.Is(IrisBurnedFlag) || Commissions.Is(w, IrisCommission, CommissionState.Failed);

        public static int PriceOf(WorldState w, StockItem item) =>
            item == null ? 0 : IrisBurned(w) ? (int)Math.Ceiling(item.Price * BurnedMarkup) : item.Price;

        public static bool Owns(WorldState w, StockItem item) => item != null && w.Equipment.OwnsInstrument(item.Kind);
        public static bool CanBuy(WorldState w, StockItem item) => item != null && !Owns(w, item) && Seeds(w) >= PriceOf(w, item);

        public static bool Buy(WorldState w, StockItem item)
        {
            if (!CanBuy(w, item) || !SpendSeeds(w, PriceOf(w, item))) return false;
            w.Equipment.OwnedInstruments.Add(item.Kind);
            Bought?.Invoke(item);
            return true;
        }

        // ---- Upgrades at the desk ----------------------------------------------------------------------------

        public static int MaskUpgrades(WorldState w) => w.Numbers.TryGetValue(MaskUpgradesKey, out var n) ? (int)n : 0;
        public static bool MasksFull(WorldState w) => MaskUpgrades(w) >= MaxMaskUpgrades;
        public static bool CanBuyMask(WorldState w) => !MasksFull(w) && Scraps(w) >= MaskUpgradeCost;

        /// <summary>Stitch a vellum cache into the cowl: one more mask, up to nine.</summary>
        public static bool BuyMask(WorldState w)
        {
            if (!CanBuyMask(w) || !SpendScraps(w, MaskUpgradeCost)) return false;
            w.Numbers[MaskUpgradesKey] = MaskUpgrades(w) + 1;
            MaskBought?.Invoke(MaskUpgrades(w));
            return true;
        }

        public static bool CanBuySlot(WorldState w) => !w.Equipment.FourthSlotUnlocked && Scraps(w) >= SlotUpgradeCost;

        public static bool BuySlot(WorldState w)
        {
            if (!CanBuySlot(w) || !SpendScraps(w, SlotUpgradeCost)) return false;
            w.Equipment.FourthSlotUnlocked = true;
            w.Equipment.NotifySlotsChanged();
            SlotBought?.Invoke();
            return true;
        }
    }
}
