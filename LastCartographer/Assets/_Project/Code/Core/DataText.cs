using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>
    /// Every string the catalogs give the player, with its key (NAR-18): place, vantage and waypoint names; memories;
    /// commissions, their hubs and steps; bosses' names and phase lines; gauntlets; the seller's pitches; the walks'
    /// verses and bounds; ability names. The code's own <c>Loc.T</c> calls are harvested from source; these keys are
    /// built from ids, so they are listed here, from the shipped catalogs, for the English table
    /// (<c>LocalizationSetup.Refresh</c>). The World assembly adds its own (Instruments, Charters).
    /// </summary>
    public static class DataText
    {
        /// <summary>
        /// The shipped catalogs' strings as (key, English, source). Resets the atlas first, so what a session registered
        /// from its scenes isn't mistaken for the catalog: for tools and tests, not for a running game.
        /// </summary>
        public static List<(string key, string english, string source)> All()
        {
            var list = new List<(string, string, string)>();
            void Add(string key, string english, string source)
            {
                if (!string.IsNullOrEmpty(english)) list.Add((key, english, source));
            }

            Atlas.Reset();
            foreach (var p in Atlas.AllPlaces)
            {
                Add("place." + p.Id, p.Name, "Atlas");
                if (!string.IsNullOrEmpty(p.Region) && !list.Exists(e => e.Item1 == Atlas.RegionKey(p.Region))) Add(Atlas.RegionKey(p.Region), p.Region, "Atlas");
            }
            foreach (var v in Atlas.AllVantages) Add("vantage." + v.Id, v.Name, "Atlas");
            foreach (var wp in Atlas.AllWaypoints) Add("waypoint." + wp.Id, wp.Name, "Atlas");

            foreach (var kv in Memories.English) Add("memory." + kv.Key, kv.Value, "Memories");

            var hubs = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var d in CommissionCatalog.All)
            {
                Add(Commissions.Key(d, "title"), d.Title, "Commissions");
                Add(Commissions.Key(d, "poster"), d.Poster, "Commissions");
                Add(Commissions.Key(d, "brief"), d.Brief, "Commissions");
                Add(Commissions.Key(d, "journal"), d.Journal, "Commissions");
                Add(Commissions.Key(d, "aftermath"), d.Aftermath, "Commissions");
                for (int i = 0; i < d.Steps.Length; i++) Add(Commissions.Key(d, "step." + i), d.Steps[i].Text, "Commissions");
                if (!string.IsNullOrEmpty(d.Hub)) hubs.Add(d.Hub);
            }
            foreach (var h in hubs) Add("hub." + h, h, "Commissions");

            foreach (var b in Bosses.All)
            {
                Add("boss." + b.Id + ".name", b.Name, "Bosses");
                var lines = b.Lines;
                for (int i = 0; i < Bosses.LineFields.Length && i < lines.Length; i++) Add("boss." + b.Id + "." + Bosses.LineFields[i], lines[i], "Bosses");
            }

            foreach (var g in Gauntlets.All) Add("gauntlet." + g.Id, g.Name, "Gauntlets");
            foreach (var item in Economy.Stock) Add(Economy.PitchKey(item), item.Pitch, "Economy");

            foreach (var kv in BoundsWalks.Words)
            {
                for (int i = 0; i < kv.Value.verses.Length; i++) Add(BoundsWalks.VerseKey(kv.Key, i), kv.Value.verses[i], "BoundsWalks");
                foreach (var b in kv.Value.bounds) Add(BoundsWalks.BoundKey(kv.Key, b), b, "BoundsWalks");
            }

            foreach (Ability a in Enum.GetValues(typeof(Ability)))
                if (a != Ability.None) Add(AbilityNames.Key(a), a.ToString(), "Abilities");
            foreach (var kv in Flavour.English) Add(kv.Key, kv.Value, "Flavour");   // NAR-17
            foreach (var kv in Keystones.English) Add(Keystones.NameKey(kv.Key), kv.Value, "Keystones");
            return list;
        }
    }
}
