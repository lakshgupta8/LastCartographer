using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>One boss's arena as built: the arena, the boss, its doors and the props its kit uses.</summary>
    public sealed class BossKit
    {
        public string BossId;
        public BossSheet Sheet;
        public BossArena Arena;
        public Boss Boss;
        public GameObject DoorW, DoorE;
        /// <summary>Where Wren stands outside the west door.</summary>
        public Vector2 Entry;
        /// <summary>The middle of the arena floor.</summary>
        public Vector2 Centre;
        public readonly List<GameObject> Platforms = new List<GameObject>();
        public VantagePoint Vantage;
    }

    /// <summary>
    /// The greybox arenas of the bosses built at runtime (CMB-13): each boss's floor, doors, arena zone and props, its
    /// name, tier and lines from its sheet (NAR-06), and its reward from the sheet too. The arena is eighteen units wide
    /// between the doors, floor top at the origin's height; the test rigs and the arena rooms build from the same recipe.
    /// </summary>
    public static class BossKits
    {
        public const float Width = 18f;
        public const float DoorHeight = 6f;

        /// <summary>The bosses with a kit here, in the plan's order (CMB-13).</summary>
        public static readonly string[] Ids = { "collapse", "brann", "choir", "gatekeeper" };
        public static bool Has(string bossId) => System.Array.IndexOf(Ids, bossId) >= 0;

        public static BossKit Build(string bossId, Transform parent, Vector2 origin)
        {
            var sheet = Bosses.Find(bossId);
            if (sheet == null || !Has(bossId)) { Debug.LogWarning("[OWSBG] no boss kit for " + bossId); return null; }
            var kit = new BossKit
            {
                BossId = bossId,
                Sheet = sheet,
                Entry = origin + new Vector2(-4f, 0f),
                Centre = origin + new Vector2(Width * 0.5f, 0f),
            };
            float minX = origin.x + 0.5f, maxX = origin.x + Width - 0.5f, floor = origin.y, mid = kit.Centre.x;

            var ground = InkMaterials.Lit("Arena_Floor_" + bossId, FloorColour(bossId));
            Ground(parent, "Floor", new Vector2(mid, floor - 0.5f), new Vector2(Width + 16f, 1f), ground);
            kit.DoorW = Ground(parent, "Door_W", new Vector2(origin.x - 0.5f, floor + DoorHeight * 0.5f), new Vector2(1f, DoorHeight), InkMaterials.Dark);
            kit.DoorE = Ground(parent, "Door_E", new Vector2(origin.x + Width + 0.5f, floor + DoorHeight * 0.5f), new Vector2(1f, DoorHeight), InkMaterials.Dark);
            kit.DoorW.SetActive(false);
            kit.DoorE.SetActive(false);

            switch (bossId)
            {
                case "collapse":
                {
                    var c = MakeBoss<Collapse>(parent, "Collapse", new Vector2(minX + (maxX - minX) / 8f, floor + 1.5f), new Vector2((maxX - minX) / 4f * 0.8f, 3f), 28);
                    c.floorY = floor; c.arenaMinX = minX; c.arenaMaxX = maxX;
                    kit.Boss = c;
                    break;
                }
                case "brann":
                {
                    var b = MakeBoss<Brann>(parent, "Brann", new Vector2(maxX - 3f, floor + 1f), new Vector2(0.9f, 2f), 36);
                    b.floorY = floor; b.arenaMinX = minX; b.arenaMaxX = maxX;
                    kit.Boss = b;
                    break;
                }
                case "choir":
                {
                    var ch = MakeBoss<Choir>(parent, "Choir", new Vector2(mid, floor + 9f), new Vector2(0.4f, 0.4f), 24);
                    ch.floorY = floor; ch.centreX = mid; ch.hoverY = floor + 2.6f;
                    var plat = InkMaterials.Lit("Arena_Platform_choir", new Color(0.62f, 0.60f, 0.50f));
                    foreach (var x in new[] { 2.5f, 7f, 11f, 15.5f })
                        kit.Platforms.Add(Ground(parent, "Square_Platform", new Vector2(origin.x + x, floor + 1.7f), new Vector2(2.4f, 0.4f), plat));
                    ch.platforms.AddRange(kit.Platforms);
                    kit.Vantage = Vantage(parent, "Square", "Verdance_Aldermere_2/Square", new Vector2(origin.x - 2.5f, floor));
                    kit.Boss = ch;
                    break;
                }
                case "gatekeeper":
                {
                    var g = MakeBoss<Gatekeeper>(parent, "Gatekeeper", new Vector2(mid, floor + 1.2f), new Vector2(2.4f, 2.4f), 32);
                    g.floorY = floor; g.arenaMinX = minX + 1.2f; g.arenaMaxX = maxX - 1.2f;
                    g.perchY = floor + 7f; g.passY = floor + 2.4f;
                    g.rootPoints.Add(new Vector2(origin.x + 3f, floor + 6f));
                    g.rootPoints.Add(new Vector2(mid, floor + 9.5f));
                    g.rootPoints.Add(new Vector2(origin.x + Width - 3f, floor + 6f));
                    kit.Boss = g;
                    break;
                }
            }

            kit.Boss.ApplySheet(sheet);
            var arenaGo = new GameObject("Arena_" + bossId) { layer = LayerMask.NameToLayer("Trigger") };
            if (parent != null) arenaGo.transform.SetParent(parent, false);
            arenaGo.transform.position = new Vector3(mid, floor + 5f, 0f);
            var zone = arenaGo.AddComponent<BoxCollider2D>();
            zone.isTrigger = true;
            zone.size = new Vector2(Width - 1f, 11f);
            kit.Arena = arenaGo.AddComponent<BossArena>();
            kit.Arena.Configure(kit.Boss, new[] { kit.DoorW, kit.DoorE }, bossId, sheet.Grants);
            kit.Arena.VellumScraps = sheet.Scraps;
            return kit;
        }

        static Color FloorColour(string bossId) => bossId switch
        {
            "collapse" => new Color(0.24f, 0.22f, 0.21f),
            "brann" => new Color(0.30f, 0.26f, 0.24f),
            "choir" => new Color(0.66f, 0.64f, 0.56f),
            _ => new Color(0.44f, 0.48f, 0.38f),
        };

        static T MakeBoss<T>(Transform parent, string name, Vector2 pos, Vector2 size, int health) where T : Boss
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Enemy") };
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.AddComponent<BoxCollider2D>().size = size;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite";
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = new Vector3(size.x * 1.3f, size.y * 1.3f, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = InkMaterials.Lit("Boss_" + name, Color.white);
            var boss = go.AddComponent<T>();
            boss.SetMaxHealth(health);
            return boss;
        }

        static GameObject Ground(Transform parent, string name, Vector2 centre, Vector2 size, Material mat)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(centre.x, centre.y, 0f);
            go.AddComponent<BoxCollider2D>().size = size;
            var v = GameObject.CreatePrimitive(PrimitiveType.Cube);
            v.name = "Visual";
            Object.Destroy(v.GetComponent<Collider>());
            v.transform.SetParent(go.transform, false);
            v.transform.localScale = new Vector3(size.x, size.y, 2f);
            v.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static VantagePoint Vantage(Transform parent, string name, string vantageId, Vector2 pos)
        {
            var go = new GameObject("Vantage_" + name) { layer = LayerMask.NameToLayer("Trigger") };
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.4f, 2f);
            col.offset = new Vector2(0f, 1f);
            var vp = go.AddComponent<VantagePoint>();
            vp.VantageId = vantageId;
            BossPart.Prop("Marker", go.transform, pos + new Vector2(0f, 0.9f), new Vector2(0.15f, 1.8f), InkMaterials.Lit("Arena_Marker", new Color(0.20f, 0.27f, 0.45f)), 0.6f);
            return vp;
        }
    }
}
