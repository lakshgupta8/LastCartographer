using System;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Stand-ins for the Long Grass Camp's rooms (PRG-21) until Windreach is built (ENV-07): the walkers' post (Camp_1,
    /// the desk that stays) and the camp's three sites, each a <see cref="CampSite"/>. Scenes are named
    /// <c>Camp_&lt;planned room&gt;</c> and joined in walking order along the flattened grass, so the camp's road can
    /// be walked as well as travelled with the clan.
    /// </summary>
    public static class CampRooms
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { if (!RoomManager.Generators.Contains(Build)) RoomManager.Generators.Add(Build); }

        public static Room Build(string sceneName)
        {
            if (!Camp.IsStandIn(sceneName)) return null;
            var room = Camp.RoomOfScene(sceneName);
            int at = Array.IndexOf(Camp.Road, room);
            if (at < 0) return null;
            var west = at > 0 ? CampWalk.SceneFor(Camp.Road[at - 1]) : null;
            var east = at + 1 < Camp.Road.Length ? CampWalk.SceneFor(Camp.Road[at + 1]) : null;
            return RuntimeRooms.InNewScene(sceneName, () => Populate(sceneName, room, west, east));
        }

        static Room Populate(string sceneName, string plannedRoom, string westScene, string eastScene)
        {
            var room = RuntimeRooms.MakeRoom(sceneName);
            var grass = RuntimeRooms.Lit("Camp_Grass", new Color(0.66f, 0.62f, 0.40f));
            RuntimeRooms.MakeGround(room, "Grass", new Vector2(0f, -0.5f), new Vector2(40f, 1f), grass);
            RuntimeRooms.MakePaper(room, "Mid", 3f, 0f, new Color(0.78f, 0.72f, 0.48f), 5f);
            RuntimeRooms.MakePaper(room, "Far", 8f, 3f, new Color(0.70f, 0.78f, 0.86f), 14f);
            RuntimeRooms.MakePaper(room, "Sky", 16f, 6f, new Color(0.62f, 0.74f, 0.88f), 16f);

            RuntimeRooms.MakeSpawn(room, "Start", new Vector2(-17f, 0.5f));
            RuntimeRooms.MakeSpawn(room, "West", new Vector2(-17.5f, 0.5f));
            RuntimeRooms.MakeSpawn(room, "East", new Vector2(17.5f, 0.5f));
            RuntimeRooms.MakeSpawn(room, CampWalk.SpawnName, new Vector2(-4f, 0.5f));
            if (westScene != null) RuntimeRooms.MakeTransition(room, "To_West", new Vector2(-19.6f, 4f), new Vector2(0.8f, 10f), westScene, "East");
            if (eastScene != null) RuntimeRooms.MakeTransition(room, "To_East", new Vector2(19.6f, 4f), new Vector2(0.8f, 10f), eastScene, "West");

            var wood = RuntimeRooms.Lit("Camp_Wagon", new Color(0.46f, 0.34f, 0.24f));
            if (plannedRoom == Camp.PostRoom)
            {
                // The walkers' post: one wagon, the desk and the ledger, wherever the camp has gone.
                RuntimeRooms.MakeProp(room.transform, "PostWagon", new Vector2(8f, 1.2f), new Vector3(4f, 2.4f, 2f), wood);
                MakeDesk(room, new Vector2(3f, 0f));
                return room;
            }

            int site = Camp.SiteOf(plannedRoom);
            var campGo = new GameObject("Camp");
            campGo.transform.SetParent(room.transform, false);
            var ashesGo = new GameObject("Ashes");
            ashesGo.transform.SetParent(room.transform, false);

            // The camp: wagons in a ring, the fire, Idrenne at it, and a bedroll for anyone walking with them.
            foreach (var x in new[] { -9f, 7f, 11f })
                RuntimeRooms.MakeProp(campGo.transform, "Wagon", new Vector2(x, 1.2f), new Vector3(3.5f, 2.4f, 2f), wood);
            RuntimeRooms.MakeProp(campGo.transform, "Fire", new Vector2(1f, 0.3f), new Vector3(1.2f, 0.6f, 1.2f), RuntimeRooms.Lit("Camp_Fire", new Color(0.95f, 0.55f, 0.20f)));
            var idrenne = RuntimeRooms.MakeNpc(room, "Npc_Idrenne", new Vector2(3f, 0f), Camp.FireNodeOf(site), "Idrenne", new Color(0.86f, 0.86f, 0.90f));
            idrenne.transform.SetParent(campGo.transform, true);
            var bedroll = RuntimeRooms.MakeNpc(room, "Bedroll", new Vector2(-5.5f, 0f), CampSite.BedrollNode, "Bedroll", new Color(0.55f, 0.40f, 0.30f));
            bedroll.Prompt = "Rest";
            bedroll.transform.SetParent(campGo.transform, true);

            // Where it isn't: a ring of stones round old ashes.
            RuntimeRooms.MakeProp(ashesGo.transform, "Stones", new Vector2(1f, 0.15f), new Vector3(1.6f, 0.3f, 1.6f), RuntimeRooms.Lit("Camp_Ashes", new Color(0.52f, 0.50f, 0.48f)));
            var ashes = RuntimeRooms.MakeNpc(room, "Ashes", new Vector2(1f, 0f), CampSite.AshesAheadNode, "Ashes", new Color(0.52f, 0.50f, 0.48f));
            ashes.Prompt = "Look";
            ashes.transform.SetParent(ashesGo.transform, true);

            var s = room.gameObject.AddComponent<CampSite>();
            s.SiteRoom = plannedRoom;
            s.CampGroup = campGo;
            s.AshesGroup = ashesGo;
            s.Ashes = ashes;
            s.Refresh();
            return room;
        }

        static void MakeDesk(Room room, Vector2 pos)
        {
            var go = new GameObject("Desk") { layer = LayerMask.NameToLayer("Trigger") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 1.6f);
            col.offset = new Vector2(0f, 0.8f);
            go.AddComponent<DraftingDesk>();
            RuntimeRooms.MakeProp(go.transform, "Table", pos + new Vector2(0f, 0.45f), new Vector3(1.4f, 0.9f, 0.8f), RuntimeRooms.Lit("Camp_Desk", new Color(0.40f, 0.30f, 0.22f)));
            RuntimeRooms.MakeSpawn(room, "Desk", pos + new Vector2(0f, 0.5f));
        }
    }
}
