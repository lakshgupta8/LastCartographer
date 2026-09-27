using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>An island in the Blank made from a place Wren did not hold (bible 4.7, 8.6): who lives there, and when it is there.</summary>
    public sealed class Island
    {
        /// <summary>Matches the [B] commission's BlankIsland ("Aldermere").</summary>
        public string Id;
        public string Name;
        /// <summary>The Yarn node its people speak from.</summary>
        public string Node;
        /// <summary>The place ids it is made from (room ids with a fate).</summary>
        public string[] Places;
        /// <summary>When it drifts in the Blank: the outcome that sent it there.</summary>
        public Func<WorldState, bool> Present;
    }

    /// <summary>
    /// The Blank is built from Wren's choices (bible 10): an island for every place she left to it. Five are authored,
    /// one per [B] commission; any other released place drifts in as a generic Remnant island. The island scenes
    /// (NAR-14) read how she left each place; the generator that builds their rooms is PRG-20.
    /// Fixed islands (Thessaly Hollow, the Old Capital, Aury's Lighthouse) are on the map, not here.
    /// </summary>
    public static class Islands
    {
        public const string GenericNode = "Island_Remnant";

        static bool Released(WorldState w, string place) => OWSBG.Core.Places.FateOf(w, place) == PlaceFate.Released;

        public static readonly Island[] Authored =
        {
            new Island { Id = "Merrows_End", Name = "Merrow's End", Node = "Island_Merrow_Dotha", Places = new[] { "Saltmarrow_B" },
                         Present = w => Released(w, "Saltmarrow_B") || w.Get("saltmarrow.dotha.decided") == 2 },
            new Island { Id = "Hollowvein", Name = "Hollowvein", Node = "Island_Hollowvein",
                         Places = new[] { "Emberdown_Hollow_1", "Emberdown_Hollow_2", "Emberdown_Hollow_3", "Emberdown_Hollow_4" },
                         Present = w => w.Is("emberdown.hollowvein.buried") },
            new Island { Id = "Aldermere", Name = "Aldermere", Node = "Island_Aldermere",
                         Places = new[] { "Verdance_Aldermere_1", "Verdance_Aldermere_2", "Verdance_Aldermere_3" },
                         Present = w => Released(w, "Verdance_Aldermere_2") },
            new Island { Id = "Overgrown_Inn", Name = "The inn at the road's end", Node = "Island_Inn", Places = new string[0],
                         Present = w => w.Is("verdance.gate.inn_visited") },
            new Island { Id = "Lowmarket", Name = "Lowmarket", Node = "Island_Lowmarket",
                         Places = new[] { "Halden_Lowmarket_1", "Halden_Lowmarket_2", "Halden_Lowmarket_3" },
                         Present = w => Released(w, "Halden_Lowmarket_2") },
        };

        public static Island Find(string id) => Authored.FirstOrDefault(i => i.Id == id);

        /// <summary>The authored islands drifting in the Blank now.</summary>
        public static List<Island> Present(WorldState w) => Authored.Where(i => i.Present(w)).ToList();

        /// <summary>Every released place no authored island covers: each drifts in as a generic Remnant island.</summary>
        public static List<string> GenericPlaces(WorldState w)
        {
            var covered = new HashSet<string>(Authored.SelectMany(i => i.Places));
            var list = new List<string>();
            foreach (var kv in w.Flags)
            {
                if (!kv.Key.StartsWith("place.") || !kv.Key.EndsWith(".fate") || kv.Value != (int)PlaceFate.Released) continue;
                var place = kv.Key.Substring("place.".Length, kv.Key.Length - "place.".Length - ".fate".Length);
                if (!covered.Contains(place)) list.Add(place);
            }
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        /// <summary>How many islands the Blank holds beyond its fixed three.</summary>
        public static int Count(WorldState w) => Present(w).Count + GenericPlaces(w).Count;
    }
}
