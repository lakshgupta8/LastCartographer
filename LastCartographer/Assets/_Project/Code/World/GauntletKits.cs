using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>One gauntlet as built: where she starts, where it ends, and the pieces its ability works on.</summary>
    public sealed class GauntletKit
    {
        public GauntletPlan Plan;
        public Gauntlet Gauntlet;
        public Vector2 Start;
        public Vector2 Goal;
        public readonly List<TetherAnchor> Anchors = new List<TetherAnchor>();
        public readonly List<Updraft> Updrafts = new List<Updraft>();
        public readonly List<LanternPlatform> LanternPlatforms = new List<LanternPlatform>();
        public readonly List<GauntletZone> Hazards = new List<GauntletZone>();
        /// <summary>The widest gap she must cross, edge to edge, and the tallest climb: what the ability has to answer.</summary>
        public float WidestGap, TallestClimb;
    }

    /// <summary>
    /// The six gauntlets' greybox (CMB-18), one recipe each, built around the region's ability (combat doc §9). Local to
    /// an origin at the room's middle, floor top at its height, x from -18 to 18. The test rigs and the gauntlet rooms
    /// build from the same recipes.
    /// </summary>
    public static class GauntletKits
    {
        public static GauntletKit Build(string id, Transform parent, Vector2 o)
        {
            var plan = Gauntlets.Find(id);
            if (plan == null) { Debug.LogWarning("[OWSBG] no gauntlet " + id); return null; }
            var root = new GameObject("Gauntlet_" + id);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(o.x, o.y, 0f);
            var g = root.AddComponent<Gauntlet>();
            g.Id = id;
            g.Needs = plan.Needs;
            var kit = new GauntletKit { Plan = plan, Gauntlet = g };
            var t = root.transform;
            var ledge = InkMaterials.Lit("Gauntlet_Ledge_" + plan.Region, LedgeColour(plan.Region));

            switch (id)
            {
                case "lamp_posts":
                    Ledge(t, o, "Bank", -18f, -11f, 0f, ledge);
                    Ledge(t, o, "LampPost", -3f, 2f, 0f, ledge);
                    Ledge(t, o, "FarBank", 10f, 18f, 0f, ledge);
                    Hazard(kit, t, o, "Tide", -11f, 10f, -3.5f, -2.5f, new Color(0.62f, 0.70f, 0.72f));
                    kit.WidestGap = 8f;
                    kit.Goal = o + new Vector2(14f, 0f);
                    Goal(kit, t, o, 10f, 18f, 0f);
                    break;

                case "furnace_shafts":
                    Ledge(t, o, "Floor", -18f, -3f, 0f, ledge);
                    Wall(t, o, "Wall_W", -8f, -7f, 2.5f, 15f, ledge);
                    Wall(t, o, "Wall_E", -3f, -2f, 0f, 13f, ledge);
                    Ledge(t, o, "Landing", -2f, 18f, 13.5f, ledge);
                    Hazard(kit, t, o, "Vent_W", -7f, -6.7f, 6f, 7.2f, new Color(0.86f, 0.30f, 0.12f));
                    Hazard(kit, t, o, "Vent_E", -3.3f, -3f, 9.5f, 10.7f, new Color(0.86f, 0.30f, 0.12f));
                    kit.TallestClimb = 13.5f;
                    kit.Goal = o + new Vector2(6f, 13.5f);
                    Goal(kit, t, o, 0f, 18f, 13.5f);
                    break;

                case "canopy_threads":
                    Ledge(t, o, "Branch", -18f, -11f, 0f, ledge);
                    Anchor(kit, t, o + new Vector2(-6f, 4f));
                    Anchor(kit, t, o + new Vector2(1f, 5f));
                    Anchor(kit, t, o + new Vector2(8f, 4f));
                    Ledge(t, o, "FarBranch", 10f, 18f, 0f, ledge);
                    Hazard(kit, t, o, "Thorns", -11f, 10f, -3.5f, -2.5f, new Color(0.30f, 0.36f, 0.22f));
                    kit.WidestGap = 21f;
                    kit.Goal = o + new Vector2(14f, 0f);
                    Goal(kit, t, o, 10f, 18f, 0f);
                    break;

                case "flyer_tower":
                    Ledge(t, o, "Yard", -18f, -6f, 0f, ledge);
                    Wall(t, o, "Tower_W", -12f, -11f, 2.5f, 11f, ledge);
                    Wall(t, o, "Tower_E", -7f, -6f, 0f, 9f, ledge);
                    Ledge(t, o, "TowerTop", -6f, -3f, 9.5f, ledge);
                    Anchor(kit, t, o + new Vector2(1f, 10.5f));
                    Anchor(kit, t, o + new Vector2(7f, 10.5f));
                    Ledge(t, o, "Hall", 10f, 18f, 7f, ledge);
                    Hazard(kit, t, o, "MillRace", -6f, 10f, -3.5f, -2.5f, new Color(0.52f, 0.60f, 0.66f));
                    kit.TallestClimb = 9.5f;
                    kit.WidestGap = 13f;
                    kit.Goal = o + new Vector2(14f, 7f);
                    Goal(kit, t, o, 10f, 18f, 7f);
                    break;

                case "updrafts":
                    Ledge(t, o, "Lip", -18f, -11f, 0f, ledge);
                    foreach (var x in new[] { -7f, 0f, 7f })
                    {
                        var u = Updraft.Make("InkSwirl", t, o + new Vector2(x, 3f), new Vector2(2f, 10f), 9f);
                        kit.Updrafts.Add(u);
                    }
                    Ledge(t, o, "HighGrass", 11f, 18f, 3f, ledge);
                    Hazard(kit, t, o, "LongGrass", -11f, 11f, -3.5f, -2.5f, new Color(0.62f, 0.60f, 0.34f));
                    kit.WidestGap = 22f;
                    kit.TallestClimb = 3f;
                    kit.Goal = o + new Vector2(14f, 3f);
                    Goal(kit, t, o, 11f, 18f, 3f);
                    break;

                case "road_that_stops":
                    Ledge(t, o, "Cobbles", -18f, -12f, 0f, ledge);
                    foreach (var x in new[] { -9f, -3.5f, 2f, 7.5f })
                        kit.LanternPlatforms.Add(LanternStep(t, o, x, x + 2.5f, 0f));
                    Ledge(t, o, "RoadsEnd", 13f, 18f, 0f, ledge);
                    Hazard(kit, t, o, "TheWhite", -12f, 13f, -3.5f, -2.5f, new Color(0.97f, 0.96f, 0.93f));
                    kit.WidestGap = 25f;
                    kit.Goal = o + new Vector2(15f, 0f);
                    Goal(kit, t, o, 13f, 18f, 0f);
                    break;
            }

            kit.Start = o + new Vector2(-15f, 0f);
            g.LastSafe = kit.Start;
            return kit;
        }

        static Color LedgeColour(Region r) => r switch
        {
            Region.Saltmarrow => new Color(0.56f, 0.52f, 0.44f),
            Region.Emberdown => new Color(0.30f, 0.27f, 0.26f),
            Region.Verdance => new Color(0.36f, 0.42f, 0.30f),
            Region.Halden => new Color(0.62f, 0.56f, 0.46f),
            Region.Windreach => new Color(0.66f, 0.62f, 0.44f),
            _ => new Color(0.74f, 0.74f, 0.72f),
        };

        static GameObject Box(Transform parent, string name, string layer, Vector2 centre, Vector2 size, Material mat)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(layer) };
            go.transform.SetParent(parent, false);
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

        static void Ledge(Transform parent, Vector2 o, string name, float x0, float x1, float top, Material mat)
        {
            var go = Box(parent, name, "Ground", o + new Vector2((x0 + x1) * 0.5f, top - 0.25f), new Vector2(x1 - x0, 0.5f), mat);
            go.AddComponent<SolidGround>();
        }

        static void Wall(Transform parent, Vector2 o, string name, float x0, float x1, float y0, float y1, Material mat)
            => Box(parent, name, "Ground", o + new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), new Vector2(x1 - x0, y1 - y0), mat);

        static LanternPlatform LanternStep(Transform parent, Vector2 o, float x0, float x1, float top)
        {
            var go = Box(parent, "LanternStep", "Ground", o + new Vector2((x0 + x1) * 0.5f, top - 0.25f), new Vector2(x1 - x0, 0.5f),
                InkMaterials.Lit("Gauntlet_Cobble", new Color(0.52f, 0.50f, 0.46f)));
            go.AddComponent<SolidGround>();
            return go.AddComponent<LanternPlatform>();
        }

        static void Anchor(GauntletKit kit, Transform parent, Vector2 at)
        {
            var a = TetherAnchor.Spawn(at, float.PositiveInfinity);
            a.name = "Anchor";
            a.transform.SetParent(parent, true);
            kit.Anchors.Add(a);
        }

        static void Hazard(GauntletKit kit, Transform parent, Vector2 o, string name, float x0, float x1, float y0, float y1, Color c)
        {
            var go = Box(parent, name, "Trigger", o + new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), new Vector2(x1 - x0, y1 - y0),
                InkMaterials.Lit("Gauntlet_" + name, c));
            var z = go.AddComponent<GauntletZone>();
            z.Role = GauntletZone.Kind.Hazard;
            z.Gauntlet = kit.Gauntlet;
            kit.Hazards.Add(z);
        }

        static void Goal(GauntletKit kit, Transform parent, Vector2 o, float x0, float x1, float top)
        {
            var go = new GameObject("Goal") { layer = LayerMask.NameToLayer("Trigger") };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(o.x + (x0 + x1) * 0.5f, o.y + top + 1f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(x1 - x0, 2f);
            var z = go.AddComponent<GauntletZone>();
            z.Role = GauntletZone.Kind.Goal;
            z.Gauntlet = kit.Gauntlet;
        }
    }
}
