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
        /// <summary>Where the island's speaker stands.</summary>
        public const float TalkerX = 3f;

        /// <summary>The crowd spread over the island either side of the speaker, never on top of them, a little jittered by the island's name.</summary>
        public static float CrowdX(int i, int count, int seed)
        {
            float t = count <= 1 ? 0.5f : i / (float)(count - 1);
            float x = Mathf.Lerp(-10f, 10.5f, t) + ((seed >> (i % 8)) % 7 - 3) * 0.12f;
            if (Mathf.Abs(x - TalkerX) < 1.6f) x = TalkerX + (x < TalkerX ? -1.6f : 1.6f);
            return x;
        }

        /// <summary>
        /// The drawn island: the place's strip behind it and its far strip above, both greyed and torn out of the white;
        /// the Blank's islands and grey above them; the place's props on its floor.
        /// </summary>
        static void Draw(Room room, IslandDrawing d)
        {
            var look = d.Look;
            var mid = IslandDrawing.Strip(room, look.Mid, d.Mid, 3f, 0f, look.Wash);
            var far = IslandDrawing.Strip(room, look.Far, d.Far, 8f, 2f, Mathf.Min(1f, look.Wash + 0.2f));
            IslandDrawing.Strip(room, "Blank_Far_Islands", d.BlankFar, 12f, 4f, 0f);
            IslandDrawing.Strip(room, "Blank_Farther_Grey", d.BlankFarther, 16f, 6f, 0f);
            float midTop = mid != null ? mid.transform.position.y + mid.transform.localScale.y * 0.5f : 6f;
            float farTop = far != null ? far.transform.position.y + far.transform.localScale.y * 0.5f : 12f;
            var paper = d.BlankFarther ?? d.White;
            IslandDrawing.TearOut(room, "Mid", paper, 2.8f, IslandDrawing.MidEdge, -2f, midTop + 0.6f);
            IslandDrawing.TearOut(room, "Far", paper, 7.8f, IslandDrawing.FarEdge, 1f, farTop + 0.8f, 34f);
            foreach (var (prop, mat) in d.Props) IslandDrawing.Prop(room, prop, mat, 0.7f, look.Wash);
        }
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
            var look = IslandLooks.For(drift);
            var drawing = IslandDrawing.Load(look, room.gameObject);

            // The white, and the island in it: the place's floor, white paper either side, no drop anywhere.
            var white = drawing?.White ?? RuntimeRooms.Lit("Island_White", new Color(0.97f, 0.96f, 0.93f));
            var ground = drawing?.Tile ?? RuntimeRooms.Lit("Island_Ground", new Color(0.60f, 0.60f, 0.58f));
            var platform = drawing?.Tile ?? RuntimeRooms.Lit("Island_Platform", new Color(0.50f, 0.50f, 0.48f));
            float wash = drawing != null ? look.Wash : 0f;
            IslandDrawing.Grey(RuntimeRooms.MakeGround(room, "Island", new Vector2(0f, -0.5f), new Vector2(24f, 1f), ground), wash);
            RuntimeRooms.MakeGround(room, "White_W", new Vector2(-16f, -0.5f), new Vector2(8f, 1f), white);
            RuntimeRooms.MakeGround(room, "White_E", new Vector2(16f, -0.5f), new Vector2(8f, 1f), white);
            // The white between the drift's edge and the island is untethered: the Clarity meter runs there (PRG-18).
            UntetheredZone.Make("Untethered_W", room.transform, new Vector2(-13.75f, 4f), new Vector2(3.5f, 9f));
            UntetheredZone.Make("Untethered_E", room.transform, new Vector2(13.75f, 4f), new Vector2(3.5f, 9f));
            // Two platforms, placed by the island's name so no two islands are quite the same.
            IslandDrawing.Grey(RuntimeRooms.MakeGround(room, "Platform_1", new Vector2(-8f + seed % 5, 3f), new Vector2(3f, 0.5f), platform), wash);
            IslandDrawing.Grey(RuntimeRooms.MakeGround(room, "Platform_2", new Vector2(3f + (seed / 5) % 5, 5.5f), new Vector2(3f, 0.5f), platform), wash);

            if (drawing != null) Draw(room, drawing);
            else
            {
                RuntimeRooms.MakePaper(room, "Mid", 3f, 0f, new Color(0.90f, 0.90f, 0.88f), 6f);
                RuntimeRooms.MakePaper(room, "Far", 8f, 4f, new Color(0.94f, 0.94f, 0.92f), 14f);
                RuntimeRooms.MakePaper(room, "White", 16f, 6f, new Color(0.97f, 0.96f, 0.93f), 16f);
            }

            // Its people: the one who speaks, in the body they were met in, and the crowd standing round them. Greybox
            // quads when the art is not there; a half-island's people are paler still: half-remembered.
            var talker = drift.IsGeneric
                ? RuntimeRooms.MakeNpc(room, "Npc_" + drift.Node, new Vector2(TalkerX, 0f), drift.Node, "Remnant_Pale", drift.IsHalf ? new Color(0.84f, 0.84f, 0.82f) : new Color(0.72f, 0.72f, 0.70f))
                : RuntimeRooms.MakeNpc(room, "Npc_" + drift.Node, new Vector2(TalkerX, 0f), drift.Node, "Remnant_Grey", new Color(0.62f, 0.62f, 0.60f));
            // An unnamed person speaks as Remnant in the look they stand in: the page shows that look's face (portraits.md §1).
            if (drift.IsGeneric && look != null)
                PortraitFace.Lend(talker.gameObject, look.Speaker, AddressableArt.Load<Texture2D>(AddressableArt.PortraitPath(look.Speaker), talker.gameObject));
            if (drawing != null)
            {
                IslandDrawing.Dress(talker.gameObject, look.Speaker, drawing.People, true, "");
                for (int i = 0; i < look.Crowd.Length; i++)
                {
                    float x = CrowdX(i, look.Crowd.Length, seed);
                    var go = new GameObject("Folk_" + look.Crowd[i] + "_" + i);
                    go.transform.SetParent(room.transform, false);
                    go.transform.position = new Vector3(x, 0f, 0.6f + 0.18f * (i % 3));
                    IslandDrawing.Dress(go, Townsfolk.Character(look.Crowd[i]), drawing.People, x > TalkerX, look.CrowdActivity);
                }
            }

            RuntimeRooms.MakeSpawn(room, "Start", new Vector2(-17f, 0.5f));
            RuntimeRooms.MakeSpawn(room, "West", new Vector2(-17.5f, 0.5f));
            RuntimeRooms.MakeSpawn(room, "East", new Vector2(17.5f, 0.5f));
            RuntimeRooms.MakeTransition(room, "To_West", new Vector2(-19.6f, 4f), new Vector2(0.8f, 10f), westScene, "East");
            if (eastScene != null) RuntimeRooms.MakeTransition(room, "To_East", new Vector2(19.6f, 4f), new Vector2(0.8f, 10f), eastScene, "West");
            return room;
        }
    }
}
