using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The gauntlets' rooms, built at runtime (CMB-18) until their regions are built: scenes named "Gauntlet_&lt;id&gt;",
    /// the greybox recipe around the gauntlet's kit, entered from the west. The Lantern Chain's is a line off the built
    /// chain; the others stand for their planned rooms (<see cref="GauntletPlan.Room"/>).
    /// </summary>
    public static class GauntletRooms
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { if (!RoomManager.Generators.Contains(Build)) RoomManager.Generators.Add(Build); }

        public static Room Build(string sceneName)
        {
            if (!Gauntlets.IsGauntletScene(sceneName)) return null;
            var plan = Gauntlets.Find(sceneName.Substring(Gauntlets.ScenePrefix.Length));
            if (plan == null) { Debug.LogWarning("[OWSBG] no gauntlet for " + sceneName); return null; }
            return RuntimeRooms.InNewScene(sceneName, () => Populate(sceneName, plan));
        }

        static Room Populate(string sceneName, GauntletPlan plan)
        {
            var room = RuntimeRooms.MakeRoom(sceneName);
            var wall = RuntimeRooms.Lit("Gauntlet_Wall", new Color(0.40f, 0.38f, 0.34f));
            RuntimeRooms.MakeGround(room, "Wall_W", new Vector2(-19.5f, 6f), new Vector2(1f, 18f), wall);
            RuntimeRooms.MakeGround(room, "Wall_E", new Vector2(19.5f, 6f), new Vector2(1f, 18f), wall);
            RuntimeRooms.MakePaper(room, "Mid", 3f, -2f, new Color(0.52f, 0.50f, 0.46f), 8f);
            RuntimeRooms.MakePaper(room, "Far", 8f, 2f, new Color(0.62f, 0.60f, 0.56f), 14f);
            var kit = GauntletKits.Build(plan.Id, room.transform, Vector2.zero);
            RuntimeRooms.MakeSpawn(room, "Start", kit.Start + Vector2.up * 0.5f);
            RuntimeRooms.MakeSpawn(room, "West", kit.Start + Vector2.up * 0.5f);
            room.gameObject.name = "Room_" + sceneName + " (" + plan.Name + ")";
            return room;
        }
    }
}
