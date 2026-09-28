namespace OWSBG.Core
{
    /// <summary>
    /// Clarity (bible 4.0, 10, 12; combat doc §3; PRG-18): Wren's capacity to be in the Blank untethered, a meter and a
    /// gate. Its level grows with the story: learned at the Road That Stops (1), grown by the Half-Cathedral Bells (2)
    /// and again by Isolde's atlas at the start of Act 3 (3). The level sets how long she lasts untethered and how wide
    /// her lantern-radius is; the radius narrows as the meter runs down, so the picture is the meter.
    /// </summary>
    public static class Clarity
    {
        public const string BellsFlag = "boss.bells.defeated";
        public const string Act3Flag = "act3.started";
        public const int MaxLevel = 3;

        /// <summary>Seconds untethered she lasts at each level. Without Clarity the white takes her at once: the gate.</summary>
        static readonly float[] Capacities = { 0f, 6f, 10f, 16f };
        /// <summary>Her lantern-radius at full Clarity, per level: about six units in the Greyfold (DES-11), seven by Act 3.</summary>
        static readonly float[] Radii = { 3.5f, 5f, 6f, 7f };

        /// <summary>As small as the radius goes: the meter's floor and the bells' (6.12).</summary>
        public const float MinRadius = 1.2f;
        /// <summary>Above this fraction of the meter the radius is full; below it the radius narrows toward the floor.</summary>
        public const float NarrowsBelow = 0.5f;
        public const float DrainPerSecond = 1f;
        public const float RefillPerSecond = 4f;
        /// <summary>A lost Remnant's touch takes this much more (combat doc §3: "hits from Remnant drain more").</summary>
        public const float RemnantHitSeconds = 3f;
        /// <summary>The Field lantern, in the Blank, extends the radius by this much while it burns (combat doc §5).</summary>
        public const float LanternExtends = 3f;
        /// <summary>The one room that is untethered from wall to wall: the drift, where the islands pass.</summary>
        public const string DriftRoom = "Blank_Hollow_3";

        public static int Level(bool hasClarity, WorldState w)
        {
            if (!hasClarity) return 0;
            int level = 1;
            if (w != null && w.Is(BellsFlag)) level++;
            if (w != null && w.Is(Act3Flag)) level++;
            return level;
        }

        public static float Capacity(int level) => Capacities[Clamp(level)];
        public static float FullRadius(int level) => Radii[Clamp(level)];

        /// <summary>The radius for a level and how full the meter is (0..1).</summary>
        public static float Radius(int level, float fraction)
        {
            float full = FullRadius(level);
            if (fraction >= NarrowsBelow) return full;
            float t = fraction <= 0f ? 0f : fraction / NarrowsBelow;
            return MinRadius + (full - MinRadius) * t;
        }

        /// <summary>
        /// Whether a room is drawn round her lantern: the Blank everywhere, the Greyfold but for the Edge Camp and the Edge
        /// itself (the last places colour reaches by itself). Runtime rooms answer for the room they stand in for.
        /// </summary>
        public static bool IsLanternLit(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return false;
            if (Islands.IsIslandScene(roomId)) return true;
            var planned = PlannedRoomOf(roomId);
            if (planned == null) return false;
            if (planned == "Greyfold_Edge") return false;
            var zone = RoomPlans.ZoneOf(planned);
            if (zone == null) return false;
            if (zone.StartsWith("Blank.")) return true;
            return zone.StartsWith("Greyfold.") && zone != "Greyfold.EdgeCamp";
        }

        /// <summary>Whether the whole room is untethered (elsewhere, only its white patches are).</summary>
        public static bool IsUntetheredRoom(string roomId) => PlannedRoomOf(roomId) == DriftRoom;

        /// <summary>The planned room a scene stands for: itself, an arena's room, a gauntlet's room.</summary>
        public static string PlannedRoomOf(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return null;
            if (roomId.StartsWith("Arena_")) roomId = roomId.Substring("Arena_".Length);
            else if (roomId.StartsWith(Gauntlets.ScenePrefix))
            {
                var g = Gauntlets.Find(roomId.Substring(Gauntlets.ScenePrefix.Length));
                if (g == null || string.IsNullOrEmpty(g.Room)) return null;
                roomId = g.Room;
            }
            return RoomPlans.Find(roomId) != null ? roomId : null;
        }

        static int Clamp(int level) => level < 0 ? 0 : level > MaxLevel ? MaxLevel : level;
    }
}
