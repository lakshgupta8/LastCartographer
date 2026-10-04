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

        /// <summary>The place the island being built or stood in was, as its Remnants say it (Yarn's island_place()); set by the builder.</summary>
        public static string CurrentPlaceName { get; set; }

        /// <summary>A place's name as a Remnant says it mid-sentence: "the Cinder Baths", not "The Cinder Baths"; "a place" when none is known.</summary>
        public static string Spoken(string name) =>
            string.IsNullOrEmpty(name) ? "a place" : name.StartsWith("The ") ? "the " + name.Substring(4) : name;
        /// <summary>The people of an anchored place whose seal was loosened (Offerings): half-remembered, the place itself still standing.</summary>
        public const string HalfNode = "Island_Half";
        /// <summary>Island rooms are scenes made at runtime (PRG-20), named for what they were.</summary>
        public const string ScenePrefix = "Island_";
        /// <summary>The Hollow's far edge, built (ENV-08: the greybox of Blank_Hollow_3), where the chain of islands begins and the first island's west exit leads.</summary>
        public const string DriftEntryScene = "Greybox_Blank_Hollow_3";

        /// <summary>One island drifting in the Blank now: its room's scene name, its name, and the node its people speak from.</summary>
        public sealed class Drift
        {
            public string Scene;
            public string Name;
            public string Node;
            /// <summary>The released place a generic island is made from; null for an authored one.</summary>
            public string PlaceId;
            public Island Island;
            /// <summary>A half-island: the place is anchored and stands where it is; only its people drift, half-remembered.</summary>
            public bool IsHalf;
            public bool IsGeneric => Island == null;
        }

        public static bool IsIslandScene(string scene) => !string.IsNullOrEmpty(scene) && scene.StartsWith(ScenePrefix);
        public static string SceneOf(Island i) => ScenePrefix + i.Id;
        public static string SceneOfPlace(string place) => ScenePrefix + place;
        public static string SceneOfHalf(string place) => ScenePrefix + "Half_" + place;

        /// <summary>What a released place was called: its plan's name, its atlas name, or its id read aloud.</summary>
        public static string PlaceName(string place)
            => RoomPlans.Find(place)?.Name ?? Atlas.FindPlace(place)?.Name ?? place.Replace('_', ' ');

        /// <summary>The islands in the Blank now, in the order they drift past the Hollow: the authored ones, then every other released place.</summary>
        public static List<Drift> Drifting(WorldState w)
        {
            var list = new List<Drift>();
            foreach (var i in Present(w)) list.Add(new Drift { Scene = SceneOf(i), Name = i.Name, Node = i.Node, Island = i });
            foreach (var p in GenericPlaces(w)) list.Add(new Drift { Scene = SceneOfPlace(p), Name = PlaceName(p), Node = GenericNode, PlaceId = p });
            foreach (var p in HalfPlaces(w)) list.Add(new Drift { Scene = SceneOfHalf(p), Name = PlaceName(p), Node = HalfNode, PlaceId = p, IsHalf = true });
            return list;
        }

        /// <summary>The island a scene name means, if it is drifting now.</summary>
        public static Drift Resolve(WorldState w, string scene) => IsIslandScene(scene) ? Drifting(w).Find(d => d.Scene == scene) : null;

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

        /// <summary>
        /// Anchored places whose seal was loosened, a memory of theirs given away (Offerings.Weakened): the place stays where
        /// it is, held, and its people drift in as a half-island. Bible 10's cost, seen from inside.
        /// </summary>
        public static List<string> HalfPlaces(WorldState w)
        {
            var list = new List<string>();
            foreach (var kv in w.Flags)
            {
                if (!kv.Key.StartsWith("place.") || !kv.Key.EndsWith(".weakened") || kv.Value <= 0) continue;
                var place = kv.Key.Substring("place.".Length, kv.Key.Length - "place.".Length - ".weakened".Length);
                if (OWSBG.Core.Places.FateOf(w, place) == PlaceFate.Anchored) list.Add(place);
            }
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        /// <summary>How many islands the Blank holds beyond its fixed three, half-islands counted.</summary>
        public static int Count(WorldState w) => Present(w).Count + GenericPlaces(w).Count + HalfPlaces(w).Count;
    }
}
