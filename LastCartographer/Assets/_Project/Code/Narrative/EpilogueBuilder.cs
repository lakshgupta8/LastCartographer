using System.Linq;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// A stand-in room for an epilogue stop whose zone has no built room yet (PRG-23): the walk goes to Halden, one
    /// outer region and the Hollow, and only the coast is built. Owns scenes named "Epilogue_&lt;zone&gt;"; the stop's
    /// speaker stands in it on the stop's node. Replaced by the real rooms as they are built.
    /// </summary>
    public static class EpilogueBuilder
    {
        public const string ScenePrefix = "Epilogue_";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { if (!RoomManager.Generators.Contains(Build)) RoomManager.Generators.Add(Build); }

        public static string SceneFor(string zone) => ScenePrefix + zone.Replace('.', '_');
        public static bool IsEpilogueScene(string scene) => !string.IsNullOrEmpty(scene) && scene.StartsWith(ScenePrefix);

        public static Room Build(string sceneName)
        {
            if (!IsEpilogueScene(sceneName)) return null;
            var zone = WorldGraph.Zones.FirstOrDefault(z => SceneFor(z.Id) == sceneName);
            if (zone == null)
            {
                Debug.LogWarning("[OWSBG] no zone stands in for " + sceneName);
                return null;
            }
            var stop = Endings.EpilogueStops(Endings.Chosen(GameState.World)).FirstOrDefault(s => s.Zone == zone.Id);
            return RuntimeRooms.InNewScene(sceneName, () => Populate(sceneName, zone, stop.Node));
        }

        static Room Populate(string sceneName, Zone zone, string node)
        {
            var room = RuntimeRooms.MakeRoom(sceneName);
            var floor = RuntimeRooms.Lit("Epilogue_Floor", new Color(0.45f, 0.47f, 0.36f));
            var plat = RuntimeRooms.Lit("Epilogue_Platform", new Color(0.52f, 0.46f, 0.36f));
            RuntimeRooms.MakeGround(room, "Floor", new Vector2(0f, -0.5f), new Vector2(RuntimeRooms.Width, 1f), floor);
            RuntimeRooms.MakeGround(room, "Wall_W", new Vector2(-19.5f, 5f), new Vector2(1f, 14f), plat);
            RuntimeRooms.MakeGround(room, "Wall_E", new Vector2(19.5f, 5f), new Vector2(1f, 14f), plat);
            RuntimeRooms.MakeGround(room, "Step", new Vector2(6f, 0.75f), new Vector2(4f, 0.5f), plat);
            RuntimeRooms.MakePaper(room, "Mid", 3f, 0f, new Color(0.62f, 0.62f, 0.58f), 6f);
            RuntimeRooms.MakePaper(room, "Far", 8f, 4f, new Color(0.70f, 0.70f, 0.66f), 14f);
            RuntimeRooms.MakePaper(room, "Farther", 16f, 6f, new Color(0.80f, 0.80f, 0.76f), 16f);
            if (node != null) RuntimeRooms.MakeNpc(room, "Npc_" + node, new Vector2(3f, 0f), node, "Epilogue", new Color(0.55f, 0.50f, 0.42f));
            RuntimeRooms.MakeSpawn(room, "Start", new Vector2(-6f, 0.5f));
            room.gameObject.name = "Room_" + sceneName + " (" + zone.Name + ")";
            return room;
        }
    }
}
