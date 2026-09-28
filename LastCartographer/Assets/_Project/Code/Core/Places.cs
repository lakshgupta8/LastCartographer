using System;

namespace OWSBG.Core
{
    /// <summary>A place's fate: the regional decision (bible 4, 10; docs/design/anchoring.md).</summary>
    public enum PlaceFate
    {
        /// <summary>No decision yet: the place fades on story beats.</summary>
        Unwritten = 0,
        /// <summary>Guild-style: fading stops, and so does everything else (held state, Wardens).</summary>
        Anchored = 1,
        /// <summary>Holdfast-style: the people hold it by ritual; fading stops, life goes on.</summary>
        Held = 2,
        /// <summary>Let go: fading continues; the place will be an island in the Blank.</summary>
        Released = 3,
    }

    /// <summary>
    /// The decision store (PRG-13). A fate is the int flag "place.&lt;id&gt;.fate", decided once and final.
    /// WorldState.AnchoredPlaces mirrors the Anchored ones for anything that only cares about that set.
    /// </summary>
    public static class Places
    {
        public static event Action<string, PlaceFate> FateChanged;

        public static string Key(string place) => "place." + place + ".fate";

        public static PlaceFate FateOf(WorldState w, string place) => (PlaceFate)w.Get(Key(place));
        public static bool IsDecided(WorldState w, string place) => FateOf(w, place) != PlaceFate.Unwritten;

        /// <summary>Anchored and Held places stop fading; Unwritten and Released do not.</summary>
        public static bool IsFadeStopped(WorldState w, string place)
        {
            var f = FateOf(w, place);
            return f == PlaceFate.Anchored || f == PlaceFate.Held || w.AnchoredPlaces.Contains(place);
        }

        /// <summary>Write a fate. False when the place already has one or the fate is Unwritten.</summary>
        public static bool Decide(WorldState w, string place, PlaceFate fate)
        {
            if (string.IsNullOrEmpty(place) || fate == PlaceFate.Unwritten || IsDecided(w, place)) return false;
            w.Set(Key(place), (int)fate);
            if (fate == PlaceFate.Anchored) { w.AnchoredPlaces.Add(place); DayClock.Lock(w, place); }
            else w.AnchoredPlaces.Remove(place);
            FateChanged?.Invoke(place, fate);
            return true;
        }

        public static bool Anchor(WorldState w, string place) => Decide(w, place, PlaceFate.Anchored);
        public static bool Hold(WorldState w, string place) => Decide(w, place, PlaceFate.Held);
        public static bool Release(WorldState w, string place) => Decide(w, place, PlaceFate.Released);

        /// <summary>How many places carry a fate (the endings count anchors and keystones).</summary>
        public static int Count(WorldState w, PlaceFate fate)
        {
            int n = 0;
            foreach (var kv in w.Flags)
                if (kv.Key.StartsWith("place.") && kv.Key.EndsWith(".fate") && kv.Value == (int)fate) n++;
            return n;
        }

        /// <summary>The fate's word for Yarn and logs; English, as scripts compare it.</summary>
        public static string Describe(PlaceFate f) => f.ToString().ToLowerInvariant();

        /// <summary>The fate as a player reads it (NAR-18).</summary>
        public static string Display(PlaceFate f) => f switch
        {
            PlaceFate.Anchored => Loc.T("fate.anchored", "anchored"),
            PlaceFate.Held => Loc.T("fate.held", "held"),
            PlaceFate.Released => Loc.T("fate.released", "released"),
            _ => Loc.T("fate.unwritten", "unwritten"),
        };

        public static bool TryParse(string s, out PlaceFate fate)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "anchor": case "anchored": fate = PlaceFate.Anchored; return true;
                case "hold": case "held": fate = PlaceFate.Held; return true;
                case "release": case "released": fate = PlaceFate.Released; return true;
                case "unwritten": fate = PlaceFate.Unwritten; return true;
                default: fate = PlaceFate.Unwritten; return false;
            }
        }
    }
}
