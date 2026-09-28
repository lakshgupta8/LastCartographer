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
    /// The greybox arenas of the bosses built at runtime (CMB-13 to CMB-16): each boss's floor, doors, arena zone and props, its
    /// name, tier and lines from its sheet (NAR-06), and its reward from the sheet too. The arena is eighteen units wide
    /// between the doors, floor top at the origin's height; the test rigs and the arena rooms build from the same recipe.
    /// </summary>
    public static class BossKits
    {
        public const float Width = 18f;
        public const float DoorHeight = 6f;

        /// <summary>The bosses with a kit here, in the plan's order (CMB-13 to CMB-16).</summary>
        public static readonly string[] Ids = { "collapse", "brann", "choir", "gatekeeper", "oriel", "hale", "fallen_star", "voss",
                                                "bells", "corras_drawing", "archivist", "complete_survey" };
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
                    var c = MakeBoss<Collapse>(parent, "Collapse", new Vector2(minX + (maxX - minX) / 8f, floor + 1.5f), new Vector2((maxX - minX) / 4f * 0.8f, 3f));
                    c.floorY = floor; c.arenaMinX = minX; c.arenaMaxX = maxX;
                    kit.Boss = c;
                    break;
                }
                case "brann":
                {
                    var b = MakeBoss<Brann>(parent, "Brann", new Vector2(maxX - 3f, floor + 1f), new Vector2(0.9f, 2f));
                    b.floorY = floor; b.arenaMinX = minX; b.arenaMaxX = maxX;
                    kit.Boss = b;
                    break;
                }
                case "choir":
                {
                    var ch = MakeBoss<Choir>(parent, "Choir", new Vector2(mid, floor + 9f), new Vector2(0.4f, 0.4f));
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
                    var g = MakeBoss<Gatekeeper>(parent, "Gatekeeper", new Vector2(mid, floor + 1.2f), new Vector2(2.4f, 2.4f));
                    g.floorY = floor; g.arenaMinX = minX + 1.2f; g.arenaMaxX = maxX - 1.2f;
                    g.perchY = floor + 7f; g.passY = floor + 2.4f;
                    g.rootPoints.Add(new Vector2(origin.x + 3f, floor + 6f));
                    g.rootPoints.Add(new Vector2(mid, floor + 9.5f));
                    g.rootPoints.Add(new Vector2(origin.x + Width - 3f, floor + 6f));
                    kit.Boss = g;
                    break;
                }
                case "oriel":
                {
                    var o = MakeBoss<Oriel>(parent, "Oriel", new Vector2(maxX - 3f, floor + 0.8f), new Vector2(0.7f, 1.6f));
                    o.floorY = floor; o.arenaMinX = minX; o.arenaMaxX = maxX;
                    var chalk = InkMaterials.Lit("Arena_Chalk", new Color(0.94f, 0.93f, 0.88f));
                    for (int i = 1; i < 6; i++) BossPart.Prop("Chalk", parent, new Vector2(origin.x + i * 3f, floor + 0.02f), new Vector2(0.08f, 0.04f), chalk, -0.4f);
                    kit.Boss = o;
                    break;
                }
                case "hale":
                {
                    var h = MakeBoss<Hale>(parent, "Hale", new Vector2(maxX - 1.5f, floor + 0.9f), new Vector2(0.8f, 1.8f));
                    h.floorY = floor; h.arenaMinX = minX; h.arenaMaxX = maxX;
                    for (int i = 0; i < Hale.StoneCount; i++) h.stoneXs.Add(origin.x + 1f + i * 2f);
                    kit.Boss = h;
                    break;
                }
                case "voss":
                {
                    var v = MakeBoss<Voss>(parent, "Voss", new Vector2(maxX - 3f, floor + 1.1f), new Vector2(0.9f, 2.2f));
                    v.floorY = floor; v.arenaMinX = minX; v.arenaMaxX = maxX;
                    kit.Boss = v;
                    break;
                }
                case "bells":
                {
                    var bl = MakeBoss<HalfCathedralBells>(parent, "Bells", new Vector2(mid, floor + 9f), new Vector2(0.4f, 0.4f));
                    bl.floorY = floor; bl.arenaMinX = minX; bl.arenaMaxX = maxX;
                    bl.ropeXs.AddRange(new[] { mid, origin.x + 5f, origin.x + 13f, origin.x + 16.5f });
                    bl.vantageX = origin.x + 1.5f;
                    BossPart.Prop("Vantage", parent, new Vector2(bl.vantageX, floor + 0.9f), new Vector2(0.15f, 1.8f), InkMaterials.Lit("Arena_Marker", new Color(0.20f, 0.27f, 0.45f)), 0.6f);
                    kit.Boss = bl;
                    break;
                }
                case "corras_drawing":
                {
                    var cd = MakeBoss<CorrasDrawing>(parent, "CorrasDrawing", new Vector2(maxX - 4f, floor + 1.8f), new Vector2(2.2f, 3.6f));
                    cd.floorY = floor; cd.arenaMinX = minX + 1.1f; cd.arenaMaxX = maxX - 1.1f;
                    kit.Boss = cd;
                    break;
                }
                case "archivist":
                {
                    var ar = MakeBoss<Archivist>(parent, "Archivist", new Vector2(mid, floor + 3f), new Vector2(2.2f, 2.4f));
                    ar.floorY = floor; ar.arenaMinX = minX; ar.arenaMaxX = maxX; ar.perchY = floor + 3f;
                    kit.Boss = ar;
                    break;
                }
                case "complete_survey":
                {
                    var cs = MakeBoss<CompleteSurvey>(parent, "CompleteSurvey", new Vector2(mid, floor + 9f), new Vector2(0.4f, 0.4f));
                    cs.floorY = floor; cs.arenaMinX = minX; cs.arenaMaxX = maxX;
                    kit.Boss = cs;
                    break;
                }
                case "fallen_star":
                {
                    var st = MakeBoss<FallenStar>(parent, "FallenStar", new Vector2(mid, floor + 1.6f), new Vector2(2.4f, 3.2f));
                    st.floorY = floor; st.arenaMinX = minX + 1.2f; st.arenaMaxX = maxX - 1.2f;
                    kit.Boss = st;
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
            kit.Arena.RewardCharter = sheet.Charter;
            return kit;
        }

        static Color FloorColour(string bossId) => bossId switch
        {
            "collapse" => new Color(0.24f, 0.22f, 0.21f),
            "brann" => new Color(0.30f, 0.26f, 0.24f),
            "choir" => new Color(0.66f, 0.64f, 0.56f),
            "oriel" => new Color(0.70f, 0.68f, 0.62f),
            "hale" => new Color(0.56f, 0.52f, 0.34f),
            "fallen_star" => new Color(0.32f, 0.28f, 0.26f),
            "voss" => new Color(0.80f, 0.80f, 0.78f),
            "bells" => new Color(0.95f, 0.94f, 0.91f),
            "corras_drawing" => new Color(0.97f, 0.96f, 0.94f),
            "archivist" => new Color(0.36f, 0.34f, 0.38f),
            "complete_survey" => new Color(0.92f, 0.90f, 0.84f),
            _ => new Color(0.44f, 0.48f, 0.38f),
        };

        static T MakeBoss<T>(Transform parent, string name, Vector2 pos, Vector2 size) where T : Boss
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
            return go.AddComponent<T>();   // health comes with the sheet (ApplySheet, Tuning.BossHealth)
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
