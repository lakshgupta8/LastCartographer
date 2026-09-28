using System;

namespace OWSBG.Core
{
    /// <summary>
    /// A noticed sequence break (world-map §2: "the game notices; quietly rewarded"). A zone Wren stands in that
    /// nothing she has could have walked her to, only a soft Wingbeat gap crossed by skill, is a break. The first
    /// time she is seen in each such zone the game writes it down (<see cref="FlagKey"/>), gives a scrap of vellum,
    /// and says one line; after that the zone is hers. The rule is here; <c>BreakWatcher</c> (World) watches the rooms.
    /// </summary>
    public static class SequenceBreaks
    {
        public const string Prefix = "break.noticed.";
        public const int ScrapReward = 1;

        public static string FlagKey(string zone) => Keys.Of(Prefix, zone);

        /// <summary>The zone a room draws, or null for a room that is on no place (a test scene, the course).</summary>
        public static string ZoneOfRoom(string room)
        {
            if (string.IsNullOrEmpty(room)) return null;
            string place = room.StartsWith(WorldGraph.GreyboxPrefix, StringComparison.Ordinal) ? room.Substring(WorldGraph.GreyboxPrefix.Length) : room;
            return WorldGraph.ZoneOfPlace(place);
        }

        /// <summary>
        /// Standing in this zone with this kit and these flags is a break: not reachable by any hard way, reachable
        /// with the soft gaps allowed. A zone reachable either way, or by neither (she can't be there; a test put her
        /// there), is not.
        /// </summary>
        public static bool IsBreak(Ability kit, WorldState w, string zone)
        {
            if (string.IsNullOrEmpty(zone) || WorldGraph.Find(zone) == null) return false;
            Func<string, bool> flags = w != null ? w.Is : (Func<string, bool>)(_ => false);
            if (WorldGraph.Reachable(kit, flags, false).Contains(zone)) return false;
            return WorldGraph.Reachable(kit, flags, true).Contains(zone);
        }

        public static bool IsNoticed(WorldState w, string zone) => w != null && !string.IsNullOrEmpty(zone) && w.Is(FlagKey(zone));

        /// <summary>
        /// Wren is in this room. If it is a break not yet noticed, notice it: the flag, the scrap. Returns the zone
        /// when something was written, else null.
        /// </summary>
        public static string Notice(WorldState w, string room)
        {
            if (w == null) return null;
            string zone = ZoneOfRoom(room);
            if (zone == null || IsNoticed(w, zone)) return null;
            if (!IsBreak(AbilitySet.FromWorld(w), w, zone)) return null;
            w.Set(FlagKey(zone), true);
            Commissions.AddScraps(w, ScrapReward);
            return zone;
        }
    }
}
