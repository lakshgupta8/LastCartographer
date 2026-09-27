using System.Collections.Generic;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

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
        public const float Width = 40f, Height = 17f;
        static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        static Mesh _cube, _quad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { RoomManager.Generator = Build; }

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
            var drift = drifts[at];
            var scene = SceneManager.CreateScene(sceneName);
            var previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);   // new objects land in the island's scene
            try
            {
                return Populate(drift, at > 0 ? drifts[at - 1].Scene : Islands.DriftEntryScene, at + 1 < drifts.Count ? drifts[at + 1].Scene : null);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            }
        }

        static Room Populate(Islands.Drift drift, string westScene, string eastScene)
        {
            var room = MakeRoom(drift.Scene, new Rect(-Width * 0.5f, -3f, Width, Height));
            int seed = Hash(drift.Scene);

            // The white, and the island in it: a grey slab, white paper either side, no drop anywhere.
            var white = Lit("Island_White", new Color(0.97f, 0.96f, 0.93f));
            var grey = Lit("Island_Ground", new Color(0.60f, 0.60f, 0.58f));
            var dark = Lit("Island_Platform", new Color(0.50f, 0.50f, 0.48f));
            MakeGround(room, "Island", new Vector2(0f, -0.5f), new Vector2(24f, 1f), grey);
            MakeGround(room, "White_W", new Vector2(-16f, -0.5f), new Vector2(8f, 1f), white);
            MakeGround(room, "White_E", new Vector2(16f, -0.5f), new Vector2(8f, 1f), white);
            // Two platforms, placed by the island's name so no two islands are quite the same.
            MakeGround(room, "Platform_1", new Vector2(-8f + seed % 5, 3f), new Vector2(3f, 0.5f), dark);
            MakeGround(room, "Platform_2", new Vector2(3f + (seed / 5) % 5, 5.5f), new Vector2(3f, 0.5f), dark);

            MakePaper(room, "Mid", 3f, 0f, new Color(0.90f, 0.90f, 0.88f), 6f);
            MakePaper(room, "Far", 8f, 4f, new Color(0.94f, 0.94f, 0.92f), 14f);
            MakePaper(room, "White", 16f, 6f, new Color(0.97f, 0.96f, 0.93f), 16f);

            MakeNpc(room, "Npc_" + drift.Node, new Vector2(3f, 0f), drift.Node, drift.IsGeneric ? new Color(0.72f, 0.72f, 0.70f) : new Color(0.62f, 0.62f, 0.60f));

            MakeSpawn(room, "Start", new Vector2(-17f, 0.5f));
            MakeSpawn(room, "West", new Vector2(-17.5f, 0.5f));
            MakeSpawn(room, "East", new Vector2(17.5f, 0.5f));
            MakeTransition(room, "To_West", new Vector2(-19.6f, 4f), new Vector2(0.8f, 10f), westScene, "East");
            if (eastScene != null) MakeTransition(room, "To_East", new Vector2(19.6f, 4f), new Vector2(0.8f, 10f), eastScene, "West");
            return room;
        }

        static int Hash(string s) { int h = 17; foreach (var c in s) h = unchecked(h * 31 + c); return h & 0x7fffffff; }

        // ---- the greybox recipe, at runtime (mirrors the editor's ProjectSetup helpers) ----

        static Material Lit(string name, Color color)
        {
            if (_materials.TryGetValue(name, out var m) && m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shader) { name = "M_" + name };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.1f);
            _materials[name] = m;
            return m;
        }

        /// <summary>A primitive's mesh, borrowed once: a primitive's own collider cannot be swapped for a 2D one in the same frame.</summary>
        static Mesh MeshOf(PrimitiveType type, ref Mesh cache)
        {
            if (cache != null) return cache;
            var temp = GameObject.CreatePrimitive(type);
            cache = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
            return cache;
        }

        static GameObject MakeShape(Room room, string name, PrimitiveType type, ref Mesh cache, string layer, Material mat, ShadowCastingMode shadows)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(layer) };
            go.transform.SetParent(room.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = MeshOf(type, ref cache);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows;
            r.receiveShadows = shadows != ShadowCastingMode.Off;
            return go;
        }

        static Room MakeRoom(string id, Rect cameraBounds)
        {
            var go = new GameObject("Room_" + id);
            var room = go.AddComponent<Room>();
            room.RoomId = id;
            var boundsGo = new GameObject("CameraBounds") { layer = LayerMask.NameToLayer("Trigger") };
            boundsGo.transform.SetParent(go.transform, false);
            var poly = boundsGo.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.SetPath(0, new[]
            {
                new Vector2(cameraBounds.xMin, cameraBounds.yMin), new Vector2(cameraBounds.xMax, cameraBounds.yMin),
                new Vector2(cameraBounds.xMax, cameraBounds.yMax), new Vector2(cameraBounds.xMin, cameraBounds.yMax),
            });
            room.CameraBounds = poly;
            return room;
        }

        static void MakeGround(Room room, string name, Vector2 center, Vector2 size, Material mat)
        {
            var b = MakeShape(room, name, PrimitiveType.Cube, ref _cube, "Ground", mat, ShadowCastingMode.On);
            b.transform.position = new Vector3(center.x, center.y, 0f);
            b.transform.localScale = new Vector3(size.x, size.y, 2f);
            b.AddComponent<BoxCollider2D>();
        }

        static void MakePaper(Room room, string name, float z, float y, Color color, float height)
        {
            var q = MakeShape(room, "Paper_" + name, PrimitiveType.Quad, ref _quad, "Paper", Lit("Paper_" + name, color), ShadowCastingMode.Off);
            q.transform.position = new Vector3(0f, y + height * 0.5f, z);
            q.transform.localScale = new Vector3(80f, height, 1f);
        }

        static void MakeNpc(Room room, string name, Vector2 pos, string startNode, Color tint)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Trigger") };
            go.transform.SetParent(room.transform, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 1.6f);
            col.offset = new Vector2(0f, 0.8f);
            var talker = go.AddComponent<NpcTalker>();
            talker.StartNode = startNode;
            talker.Prompt = "Talk";
            var quad = MakeShape(room, "Sprite", PrimitiveType.Quad, ref _quad, "Trigger", Lit("Remnant_" + (tint.r > 0.7f ? "Pale" : "Grey"), tint), ShadowCastingMode.TwoSided);
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = new Vector3(0.9f, 1.4f, 1f);
            quad.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        }

        static void MakeSpawn(Room room, string name, Vector2 pos)
        {
            var s = new GameObject("Spawn_" + name);
            s.transform.SetParent(room.transform, false);
            s.transform.position = new Vector3(pos.x, pos.y, 0f);
        }

        static void MakeTransition(Room room, string name, Vector2 center, Vector2 size, string targetScene, string targetSpawn)
        {
            var t = new GameObject("Transition_" + name) { layer = LayerMask.NameToLayer("Trigger") };
            t.transform.SetParent(room.transform, false);
            t.transform.position = new Vector3(center.x, center.y, 0f);
            var col = t.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;
            var tr = t.AddComponent<RoomTransition>();
            tr.TargetScene = targetScene;
            tr.TargetSpawn = targetSpawn;
        }
    }
}
