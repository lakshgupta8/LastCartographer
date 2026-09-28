using System.Collections.Generic;
using OWSBG.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The greybox room recipe at runtime, for rooms made when they are asked for rather than loaded (the Blank's
    /// islands, the epilogue's stops). Mirrors the editor's ProjectSetup helpers. Shapes are built from borrowed
    /// primitive meshes: a primitive's own collider cannot be swapped for a 2D one in the frame it is made.
    /// </summary>
    internal static class RuntimeRooms
    {
        public const float Width = 40f, Height = 17f;
        static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        static Mesh _cube, _quad;

        public static Material Lit(string name, Color color)
        {
            if (_materials.TryGetValue(name, out var m) && m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shader) { name = "M_" + name };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.1f);
            _materials[name] = m;
            return m;
        }

        static Mesh MeshOf(PrimitiveType type, ref Mesh cache)
        {
            if (cache != null) return cache;
            var temp = GameObject.CreatePrimitive(type);
            cache = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
            return cache;
        }

        static GameObject MakeShape(Transform parent, string name, PrimitiveType type, ref Mesh cache, string layer, Material mat, ShadowCastingMode shadows)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(layer) };
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = MeshOf(type, ref cache);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows;
            r.receiveShadows = shadows != ShadowCastingMode.Off;
            return go;
        }

        public static Room MakeRoom(string id)
        {
            var cameraBounds = new Rect(-Width * 0.5f, -3f, Width, Height);
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

        public static void MakeGround(Room room, string name, Vector2 center, Vector2 size, Material mat)
        {
            var b = MakeShape(room.transform, name, PrimitiveType.Cube, ref _cube, "Ground", mat, ShadowCastingMode.On);
            b.transform.position = new Vector3(center.x, center.y, 0f);
            b.transform.localScale = new Vector3(size.x, size.y, 2f);
            b.AddComponent<BoxCollider2D>();
        }

        public static void MakePaper(Room room, string name, float z, float y, Color color, float height)
        {
            var q = MakeShape(room.transform, "Paper_" + name, PrimitiveType.Quad, ref _quad, "Paper", Lit("Paper_" + name, color), ShadowCastingMode.Off);
            q.transform.position = new Vector3(0f, y + height * 0.5f, z);
            q.transform.localScale = new Vector3(80f, height, 1f);
        }

        public static NpcTalker MakeNpc(Room room, string name, Vector2 pos, string startNode, string tintName, Color tint)
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
            var quad = MakeShape(go.transform, "Sprite", PrimitiveType.Quad, ref _quad, "Trigger", Lit("Npc_" + tintName, tint), ShadowCastingMode.TwoSided);
            quad.transform.localScale = new Vector3(0.9f, 1.4f, 1f);
            quad.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            return talker;
        }

        public static void MakeSpawn(Room room, string name, Vector2 pos)
        {
            var s = new GameObject("Spawn_" + name);
            s.transform.SetParent(room.transform, false);
            s.transform.position = new Vector3(pos.x, pos.y, 0f);
        }

        public static void MakeTransition(Room room, string name, Vector2 center, Vector2 size, string targetScene, string targetSpawn)
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

        /// <summary>A scene of this name with new objects landing in it while <paramref name="populate"/> runs; the room it returns.</summary>
        public static Room InNewScene(string sceneName, System.Func<Room> populate)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.CreateScene(sceneName);
            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            try { return populate(); }
            finally { if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous); }
        }

        public static int Hash(string s) { int h = 17; foreach (var c in s) h = unchecked(h * 31 + c); return h & 0x7fffffff; }
    }
}
