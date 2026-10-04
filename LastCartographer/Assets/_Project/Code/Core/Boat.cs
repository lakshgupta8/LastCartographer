namespace OWSBG.Core
{
    /// <summary>
    /// Sable's boat north (character-bibles.md §2, Act 2; world-map.md §6): from the Drowned Quay up the coast and into the
    /// Dry River's silted mouth, where the Ferrymen's old hulls lie on their sides. A ride, like the camp's walk, not a way
    /// on the map: the map's ways are walked, and the boat is Sable's to give. It stays where Wren left it, and Sable with
    /// it: at the river she stands by it until Wren rows back. <c>&lt;&lt;row windreach&gt;&gt;</c> and
    /// <c>&lt;&lt;row quay&gt;&gt;</c>.
    /// </summary>
    public static class Boat
    {
        /// <summary>Act 2: Sable has said there is a short way to the steppe, hers.</summary>
        public const string OfferedFlag = "sable.windreach_offered";
        /// <summary>She has rowed Wren north at least once.</summary>
        public const string RowedFlag = "sable.windreach_rowed";
        /// <summary>She counted the hulls aloud: the first time she counts who did not come back.</summary>
        public const string HullsFlag = "sable.hulls_counted";
        /// <summary>Where the boat is: 0 never rowed, <see cref="AtQuay"/>, <see cref="AtRiver"/>.</summary>
        public const string AtKey = "sable.boat_at";
        public const int AtQuay = 1, AtRiver = 2;
        /// <summary>Up the coast and the last mile walked through silt: most of a day.</summary>
        public const float Hours = 9f;

        public const string QuayRoom = "Saltmarrow_A", QuaySpawn = "Desk";
        public const string RiverRoom = "Windreach_River_2", RiverSpawn = "Camp";

        /// <summary>The room, spawn and berth a <c>&lt;&lt;row&gt;&gt;</c> destination names: "windreach" or "quay".</summary>
        public static bool TryDestination(string id, out string room, out string spawn, out int berth)
        {
            switch (id)
            {
                case "windreach": room = RiverRoom; spawn = RiverSpawn; berth = AtRiver; return true;
                case "quay": room = QuayRoom; spawn = QuaySpawn; berth = AtQuay; return true;
                default: room = spawn = null; berth = 0; return false;
            }
        }

        public static int At(WorldState w) => w.Get(AtKey);

        /// <summary>Make the crossing: the boat moves to its berth and the day moves on by its hours. Returns the hours.</summary>
        public static float Row(WorldState w, int berth)
        {
            w.Set(AtKey, berth);
            if (berth == AtRiver) w.Set(RowedFlag, 1);
            DayClock.Advance(w, Hours / 24f);
            return Hours;
        }
    }
}
