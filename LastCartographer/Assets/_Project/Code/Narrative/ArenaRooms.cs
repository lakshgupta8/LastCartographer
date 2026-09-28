using System.Linq;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Arena rooms built at runtime (CMB-13, CMB-14) for the planned rooms whose boss has a kit but whose region is not
    /// built yet: the Collapse at the bottom of Hollowvein, Brann in the cold furnace, the Choir over Aldermere's square,
    /// the Gatekeeper at the Overgrown Gate, Oriel in the Bastion's drill-yard, Hale at the Nine Stones, the Fallen Star
    /// in its anvil-crater. Owns scenes named "Arena_&lt;planned room id&gt;"; the room is the greybox recipe
    /// around the boss's kit, entered from the west. Replaced by the region's built rooms when they come.
    /// </summary>
    public static class ArenaRooms
    {
        public const string ScenePrefix = "Arena_";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { if (!RoomManager.Generators.Contains(Build)) RoomManager.Generators.Add(Build); }

        /// <summary>The planned room a boss is fought in (RoomPlans' Arena column).</summary>
        public static RoomPlan RoomOf(string bossId) => RoomPlans.All.FirstOrDefault(r => r.Arena == bossId);
        public static string SceneFor(string bossId) { var r = RoomOf(bossId); return r == null ? null : ScenePrefix + r.Id; }
        public static bool IsArenaScene(string scene) => !string.IsNullOrEmpty(scene) && scene.StartsWith(ScenePrefix);

        public static Room Build(string sceneName)
        {
            if (!IsArenaScene(sceneName)) return null;
            var plan = RoomPlans.Find(sceneName.Substring(ScenePrefix.Length));
            if (plan == null || !BossKits.Has(plan.Arena))
            {
                Debug.LogWarning("[OWSBG] no arena room for " + sceneName);
                return null;
            }
            return RuntimeRooms.InNewScene(sceneName, () => Populate(sceneName, plan));
        }

        static Room Populate(string sceneName, RoomPlan plan)
        {
            var room = RuntimeRooms.MakeRoom(sceneName);
            var wall = RuntimeRooms.Lit("Arena_Wall", new Color(0.40f, 0.38f, 0.34f));
            RuntimeRooms.MakeGround(room, "Wall_W", new Vector2(-19.5f, 5f), new Vector2(1f, 14f), wall);
            RuntimeRooms.MakeGround(room, "Wall_E", new Vector2(19.5f, 5f), new Vector2(1f, 14f), wall);
            RuntimeRooms.MakeGround(room, "Floor_W", new Vector2(-15f, -0.5f), new Vector2(10f, 1f), wall);
            RuntimeRooms.MakeGround(room, "Floor_E", new Vector2(15f, -0.5f), new Vector2(10f, 1f), wall);
            RuntimeRooms.MakePaper(room, "Mid", 3f, 0f, new Color(0.46f, 0.44f, 0.40f), 7f);
            RuntimeRooms.MakePaper(room, "Far", 8f, 3f, new Color(0.56f, 0.54f, 0.50f), 14f);
            var kit = BossKits.Build(plan.Arena, room.transform, new Vector2(-BossKits.Width * 0.5f, 0f));
            RuntimeRooms.MakeSpawn(room, "Start", new Vector2(-16f, 0.5f));
            RuntimeRooms.MakeSpawn(room, "West", new Vector2(-17f, 0.5f));
            room.gameObject.name = "Room_" + sceneName + " (" + plan.Name + ", " + kit.Sheet.Name + ")";
            return room;
        }
    }
}
