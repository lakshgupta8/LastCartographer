using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The Blank island generator (PRG-20; bible 4.7, 8.6, 10). An island room for every place Wren left to the white,
    /// built at runtime from <see cref="Islands"/> when RoomManager is asked for its scene: a grey slab of the place in
    /// a field of white paper, its people (NpcTalker on the island's node) and exits to the islands either side of it
    /// in the drift. Nothing is saved; the Blank is rebuilt from WorldState every time.
    /// </summary>
    public static class IslandBuilder
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { if (!RoomManager.Generators.Contains(Build)) RoomManager.Generators.Add(Build); }

        /// <summary>The room for an island scene name, in a new scene; null if the name is not an island's or nothing by that name drifts now.</summary>
        public static Room Build(string sceneName)
        {
            if (!Islands.IsIslandScene(sceneName)) return null;
            var drifts = Islands.Drifting(GameState.World);
            int at = drifts.FindIndex(d => d.Scene == sceneName);
            if (at < 0)
            {
                Debug.LogWarning("[OWSBG] no island drifts as " + sceneName + " in this world");
                return null;
            }
            var west = at > 0 ? drifts[at - 1].Scene : Islands.DriftEntryScene;
            var east = at + 1 < drifts.Count ? drifts[at + 1].Scene : null;
            return RuntimeRooms.InNewScene(sceneName, () => Populate(drifts[at], west, east));
        }

        static Room Populate(Islands.Drift drift, string westScene, string eastScene)
        {
            Islands.CurrentPlaceName = Islands.Spoken(drift.Name);   // what the generic script names (island_place())
            var room = RuntimeRooms.MakeRoom(drift.Scene);
            int seed = RuntimeRooms.Hash(drift.Scene);

            // The white, and the island in it: a grey slab, white paper either side, no drop anywhere.
            var white = RuntimeRooms.Lit("Island_White", new Color(0.97f, 0.96f, 0.93f));
            var grey = RuntimeRooms.Lit("Island_Ground", new Color(0.60f, 0.60f, 0.58f));
            var dark = RuntimeRooms.Lit("Island_Platform", new Color(0.50f, 0.50f, 0.48f));
            RuntimeRooms.MakeGround(room, "Island", new Vector2(0f, -0.5f), new Vector2(24f, 1f), grey);
            RuntimeRooms.MakeGround(room, "White_W", new Vector2(-16f, -0.5f), new Vector2(8f, 1f), white);
            RuntimeRooms.MakeGround(room, "White_E", new Vector2(16f, -0.5f), new Vector2(8f, 1f), white);
            // The white between the drift's edge and the island is untethered: the Clarity meter runs there (PRG-18).
            UntetheredZone.Make("Untethered_W", room.transform, new Vector2(-13.75f, 4f), new Vector2(3.5f, 9f));
            UntetheredZone.Make("Untethered_E", room.transform, new Vector2(13.75f, 4f), new Vector2(3.5f, 9f));
            // Two platforms, placed by the island's name so no two islands are quite the same.
            RuntimeRooms.MakeGround(room, "Platform_1", new Vector2(-8f + seed % 5, 3f), new Vector2(3f, 0.5f), dark);
            RuntimeRooms.MakeGround(room, "Platform_2", new Vector2(3f + (seed / 5) % 5, 5.5f), new Vector2(3f, 0.5f), dark);

            RuntimeRooms.MakePaper(room, "Mid", 3f, 0f, new Color(0.90f, 0.90f, 0.88f), 6f);
            RuntimeRooms.MakePaper(room, "Far", 8f, 4f, new Color(0.94f, 0.94f, 0.92f), 14f);
            RuntimeRooms.MakePaper(room, "White", 16f, 6f, new Color(0.97f, 0.96f, 0.93f), 16f);

            // A half-island's people are paler still: half-remembered.
            if (drift.IsGeneric) RuntimeRooms.MakeNpc(room, "Npc_" + drift.Node, new Vector2(3f, 0f), drift.Node, "Remnant_Pale", drift.IsHalf ? new Color(0.84f, 0.84f, 0.82f) : new Color(0.72f, 0.72f, 0.70f));
            else RuntimeRooms.MakeNpc(room, "Npc_" + drift.Node, new Vector2(3f, 0f), drift.Node, "Remnant_Grey", new Color(0.62f, 0.62f, 0.60f));

            RuntimeRooms.MakeSpawn(room, "Start", new Vector2(-17f, 0.5f));
            RuntimeRooms.MakeSpawn(room, "West", new Vector2(-17.5f, 0.5f));
            RuntimeRooms.MakeSpawn(room, "East", new Vector2(17.5f, 0.5f));
            RuntimeRooms.MakeTransition(room, "To_West", new Vector2(-19.6f, 4f), new Vector2(0.8f, 10f), westScene, "East");
            if (eastScene != null) RuntimeRooms.MakeTransition(room, "To_East", new Vector2(19.6f, 4f), new Vector2(0.8f, 10f), eastScene, "West");
            return room;
        }
    }
}
