using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The Greyfold and the Blank built (ENV-08, docs/design/windreach-greyfold-blank-rooms.md): two kits rendered in
    /// white and ghost-grey at the kits' densities, every planned room of both regions a scene whose exits are the plan's,
    /// its vantages, desks, people and arenas where the plan puts them; the orchard road reaches the Edge and the Edge has
    /// doors; the Gate's glide lands on the white shore; the white patches are where the meter runs; the Road's cobbles are
    /// drawn only by her lantern; the drift shows what she left and has a step onto it; the late fights wait for their
    /// scenes; and nothing here is ever held.
    /// </summary>
    public class GreyfoldBlankRoomsTests
    {
        const string Kits = "Assets/_Project/Art/Environment/";
        const string Scenes = "Assets/_Project/Scenes/Greybox/";
        const string Materials = "Assets/_Project/Art/Materials/";

        [Serializable] class Manifest { public string region; public int ppu, tilePpu; public Layer[] layers; }
        [Serializable] class Layer { public string name, kind, file; public float widthUnits, heightUnits; public int ppu, widthPx, heightPx; }

        public static readonly string[] GreyfoldStrips = { "Paper_Mid_Edge", "Paper_Far_Cathedral", "Paper_Farther_Edge", "Paper_Mid_Fence", "Paper_Mid_Outpost", "Paper_Mid_Nave", "Paper_Mid_Road", "Paper_Mid_Shore", "Paper_Mid_Pool", "Paper_Mid_Line", "Paper_Mid_LastCamp", "Paper_Far_White", "Paper_Farther_Blank" };
        public static readonly string[] GreyfoldTiles = { "Ground_Chalk", "Ground_Cobbles", "Ground_WhiteSand", "Ground_Line" };
        public static readonly string[] GreyfoldProps = { "Prop_WetEdge", "Prop_Desk", "Prop_Ledger", "Prop_Vantage", "Prop_Lamp", "Prop_LampGlow", "Prop_Seeds", "Prop_Bound", "Prop_Tether", "Prop_Fence", "Prop_Milepost", "Prop_Tent", "Prop_Stake", "Prop_Cobble", "Prop_Atlas", "Prop_Footprints", "Prop_Beam",
            "Prop_CutTether", "Prop_OldTether" };   // the dressing (ENV-06)
        public static readonly string[] BlankStrips = { "Paper_Mid_Lantern", "Paper_Mid_Hollow", "Paper_Mid_Drift", "Paper_Mid_Capital", "Paper_Mid_Crayon", "Paper_Mid_Mirror", "Paper_Mid_Causeway", "Paper_Mid_LampRoom", "Paper_Far_Islands", "Paper_Farther_Grey" };
        public static readonly string[] BlankTiles = { "Ground_Grey", "Ground_Street", "Ground_Crayon", "Ground_Causeway" };
        public static readonly string[] BlankProps = { "Prop_Desk", "Prop_Lamp", "Prop_LampGlow", "Prop_Seeds", "Prop_Bound", "Prop_House", "Prop_Island", "Prop_Chair", "Prop_Crayon", "Prop_Beacon", "Prop_Well", "Prop_Door", "Prop_Doorframe" };   // Ilse's doorframe is the dressing's (ENV-06)

        static Manifest LoadKit(string region) => JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath(Kits + region + "/kit.json")));
        static string SceneText(string id) => File.ReadAllText(Path.GetFullPath(Scenes + "Greybox_" + id + ".unity"));
        static string Guid(string codePath) => AssetDatabase.AssetPathToGUID("Assets/_Project/Code/" + codePath);
        static string MatGuid(string mat) => AssetDatabase.AssetPathToGUID(Materials + mat + ".mat");
        static int Count(string text, string needle) => text.Split(new[] { needle }, StringSplitOptions.None).Length - 1;
        static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

        static (int w, int h) PngSize(string path)
        {
            using var fs = File.OpenRead(path);
            var b = new byte[24];
            Assert.AreEqual(24, fs.Read(b, 0, 24));
            return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
        }

        static IEnumerable<RoomPlan> Plans => RoomPlans.All.Where(p => p.Id.StartsWith("Greyfold_") || p.Id.StartsWith("Blank_"));
        const string Edge = "Greyfold_Edge";   // the prologue's hand-built room: its own layout, its people by name, Isolde's desk not a waypoint

        static void CheckKit(string region, string[] strips, string[] tiles, string[] props, string what)
        {
            var kit = LoadKit(region);
            Assert.AreEqual(region, kit.region);
            Assert.AreEqual(40, kit.ppu); Assert.AreEqual(96, kit.tilePpu);
            CollectionAssert.AreEquivalent(strips.Concat(tiles).Concat(props), kit.layers.Select(l => l.name), what);
            foreach (var l in kit.layers)
            {
                var (w, h) = PngSize(Path.GetFullPath(Kits + region + "/" + l.file));
                Assert.AreEqual(l.widthPx, w, l.name); Assert.AreEqual(l.heightPx, h, l.name);
                Assert.AreEqual(0, w % 4, l.name + " width a multiple of 4"); Assert.AreEqual(0, h % 4, l.name);
                if (l.kind == "strip") { Assert.AreEqual(80f, l.widthUnits, l.name + " is a parallax strip"); Assert.AreEqual(40, l.ppu); }
                if (l.kind == "tile") { Assert.AreEqual(4f, l.widthUnits); Assert.AreEqual(1f, l.heightUnits); Assert.AreEqual(96, l.ppu); }
                if (l.kind == "prop") Assert.AreEqual(96, l.ppu);
            }
            // The paper the kit was drawn on is the paper the rooms are lit with: white.
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var s in strips)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + s + ".mat");
                Assert.IsNotNull(mat, "M_" + s + " (the rooms were built with the kit)");
                Assert.AreEqual(ink, mat.shader, s);
                var paper = mat.GetColor("_PaperColor");
                Assert.AreEqual(0.98f, paper.r, 0.01f, s + " on white paper"); Assert.AreEqual(0.97f, paper.b, 0.01f, s);
            }
            foreach (var t in tiles)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + t + ".mat");
                Assert.IsNotNull(mat, "M_" + t);
                Assert.AreEqual(1f, mat.GetFloat("_WorldUV"), t + " tiles in world space");
            }
        }

        [Test]
        public void BothKitsAreRenderedInWhiteAndGhostGrey()
        {
            CheckKit("Greyfold", GreyfoldStrips, GreyfoldTiles, GreyfoldProps, "thirteen strips, four tiles, nineteen props (the wet edge among them)");
            CheckKit("Blank", BlankStrips, BlankTiles, BlankProps, "ten strips, four tiles, thirteen props");
            // No two kits share a layer name: a material is named after its layer, and the last build would win.
            var all = new Dictionary<string, string>();
            foreach (var dir in Directory.GetDirectories(Path.GetFullPath("Assets/_Project/Art/Environment")))
            {
                string manifest = Path.Combine(dir, "kit.json");
                if (!File.Exists(manifest)) continue;
                var k = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifest));
                foreach (var l in k.layers)
                {
                    if (l.kind == "prop") continue;
                    Assert.IsFalse(all.ContainsKey(l.name), l.name + " is in both " + k.region + " and " + (all.ContainsKey(l.name) ? all[l.name] : ""));
                    all[l.name] = k.region;
                }
            }
            // The two regions' furniture is their own; the wet edge keeps the one name the Edge was built with.
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Prop_Desk_Greyfold.mat"), "the Greyfold's desk");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Prop_Desk_Blank.mat"), "the Blank's desk");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Prop_WetEdge.mat"), "the wet edge");
            Assert.AreEqual(0.93f, AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Prop_Desk.mat").GetColor("_PaperColor").r, 0.01f, "and the coast's desk is still the coast's");
        }

        [Test]
        public void EveryPlannedRoomIsASceneWithThePlansExits()
        {
            var plans = Plans.ToList();
            Assert.AreEqual(21, plans.Count, "the Greyfold's twelve and the Blank's nine");
            string transition = Guid("World/RoomTransition.cs");
            var arenas = new Dictionary<string, string>
            {
                ["bells"] = "World/Bosses/HalfCathedralBells.cs", ["halvard_3"] = "World/Bosses/Halvard.cs", ["voss"] = "World/Bosses/Voss.cs",
                ["corras_drawing"] = "World/Bosses/CorrasDrawing.cs", ["archivist"] = "World/Bosses/Archivist.cs",
            };
            foreach (var p in plans)
            {
                string path = Scenes + "Greybox_" + p.Id + ".unity";
                Assert.IsTrue(File.Exists(Path.GetFullPath(path)), p.Id + " is built");
                var text = SceneText(p.Id);
                Assert.GreaterOrEqual(Count(text, "RoomId: " + p.Id), 1, p.Id + " names itself");
                int exits = 0;
                foreach (var e in p.Exits)
                {
                    if (e.To.Contains(".")) { Assert.GreaterOrEqual(Count(text, "TargetScene: Greybox_Saltmarrow_Chain_3"), 1, p.Id + " comes from the faded third by tether"); exits++; continue; }   // a zone, not a room: Aury's causeway
                    Assert.GreaterOrEqual(Count(text, "TargetScene: Greybox_" + e.To), 1, p.Id + " exits to " + e.To + " as planned");
                    exits++;
                }
                Assert.GreaterOrEqual(Count(text, transition), exits, p.Id + "'s transitions");
                if (!string.IsNullOrEmpty(p.Vantage)) Assert.GreaterOrEqual(Count(text, "_vantageId: " + p.Id + "/" + p.Vantage), 1, p.Id + "'s vantage " + p.Vantage);
                if (p.Desk && p.Id != Edge) Assert.GreaterOrEqual(Count(text, Guid("World/DraftingDesk.cs")), 1, p.Id + " has its desk");
                if (p.Id != Edge)
                    foreach (var npc in p.Npcs)
                    {
                        string name = char.ToUpperInvariant(npc[0]) + npc.Substring(1) + "_Greybox";
                        Assert.GreaterOrEqual(Count(text, "m_Name: " + name), 1, npc + " stands in " + p.Id);
                    }
                if (p.Arena != null)
                {
                    Assert.IsTrue(arenas.ContainsKey(p.Arena), p.Id + " plans a fight the greybox builds: " + p.Arena);
                    Assert.GreaterOrEqual(Count(text, Guid(arenas[p.Arena])), 1, p.Arena + " in " + p.Id);
                    Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/BossArena.cs")), 1, p.Id + "'s arena");
                    Assert.GreaterOrEqual(Count(text, "_bossId: " + p.Arena), 1);
                }
                Assert.AreEqual(0, Count(text, Guid("World/HeldState.cs")), p.Id + ": the Greyfold and the Blank are never held");
            }
            // The orchard road reaches the Edge Camp and comes back; the Edge has doors; the Gate's glide lands on the white shore and the shore climbs back.
            Assert.GreaterOrEqual(Count(SceneText("Halden_Orchard_2"), "TargetScene: Greybox_Greyfold_EdgeCamp_1"), 1, "the orchard road to the Edge");
            Assert.GreaterOrEqual(Count(SceneText("Greyfold_EdgeCamp_1"), "TargetScene: Greybox_Halden_Orchard_2"), 1, "and back");
            var edge = SceneText(Edge);
            Assert.GreaterOrEqual(Count(edge, "TargetScene: Greybox_Greyfold_EdgeCamp_2"), 1, "the Edge opens west onto the camp");
            Assert.GreaterOrEqual(Count(edge, "TargetScene: Greybox_Greyfold_Cathedral_2"), 1, "and east into the nave");
            Assert.GreaterOrEqual(Count(SceneText("Windreach_Gate_2"), "TargetScene: Greybox_Greyfold_Pool_1"), 1, "the long glide down into the white");
            Assert.GreaterOrEqual(Count(SceneText("Greyfold_Pool_1"), "TargetScene: Greybox_Windreach_Gate_2"), 1, "and the way back up");
            Assert.AreEqual("Greybox_Blank_Hollow_2", WorldGraph.BuiltRoomScene("Blank.ThessalyHollow"), "the epilogue walks to Ilse's house");
            Assert.AreEqual("The Blank", Atlas.FindPlace("Blank_Hollow_2").Region);
            Assert.AreEqual("The Greyfold", Atlas.FindPlace("Greyfold_Road_3").Region);
            Assert.AreEqual("the mirror streets' desk", Atlas.FindWaypoint("desk.Blank_Capital_3").Name);
            Assert.IsNull(Atlas.FindWaypoint("desk." + Edge), "Isolde's desk at the Edge is the prologue's, not a waypoint");
            Assert.AreEqual("Greybox_Blank_Hollow_3", Islands.DriftEntryScene, "the islands' chain leads back to the built drift");
        }

        [Test]
        public void TheWhiteTheRoadAndTheDriftAreBuilt()
        {
            string untethered = Guid("World/ClarityMeter.cs");   // UntetheredZone lives in the meter's file
            string lantern = Guid("World/LanternPlatform.cs"), solid = Guid("World/SolidGround.cs");
            // White patches: ground she can stand on with the untethered zone over it, where the road stops, on the shore, past the line, inside, on the causeway.
            foreach (var id in new[] { "Greyfold_Road_3", "Greyfold_Pool_1", "Greyfold_Threshold_2", "Blank_Hollow_1", "Blank_Aury_1" })
            {
                var t = SceneText(id);
                Assert.GreaterOrEqual(Count(t, "m_Name: White_0"), 1, id + " has white ground");
                Assert.GreaterOrEqual(Count(t, "m_Name: Untethered_0"), 1, id + " and the meter runs on it");
            }
            // The Road That Stops: cobbles that are LanternPlatforms, the gauntlet in its room, the floors solid but the cobbles not.
            var road = SceneText("Greyfold_Road_1");
            Assert.AreEqual(4, Count(road, lantern), "four strides her lantern draws");
            Assert.AreEqual(4, Regex.Matches(road, @"m_Name: Cobble_\d").Count);
            Assert.GreaterOrEqual(Count(road, MatGuid("M_Ground_Cobbles")), 1, "on the kit's cobbles");
            Assert.GreaterOrEqual(Count(road, "Id: road_that_stops"), 1, "the gauntlet");
            Assert.GreaterOrEqual(Count(road, "m_Name: Hazard_road_that_stops"), 1, "the white under it");
            Assert.GreaterOrEqual(Count(road, "m_Name: Goal_road_that_stops"), 1, "and the road's end");
            Assert.AreEqual(2, Count(road, solid), "the two floors are solid ground; a cobble is not there when she falls");
            Assert.AreEqual(3, Count(SceneText("Greyfold_Road_2"), lantern), "the mileposts' three steps up");
            // The nave: Marrow glimpsed and silent, the bells waiting for Clarity, the kit's nave twelve units tall.
            var nave = SceneText("Greyfold_Cathedral_2");
            Assert.GreaterOrEqual(Count(nave, "m_Name: Marrow_Greybox"), 1, "a grey chick thirty steps in");
            Assert.AreEqual(0, Count(nave, Guid("Narrative/NpcTalker.cs")), "and nothing to talk to");
            Assert.GreaterOrEqual(Count(nave, "_requiresFlag: " + AbilitySet.FlagKey(Ability.Clarity)), 1, "with Clarity, the bells ring");
            Assert.GreaterOrEqual(Count(nave, MatGuid("M_Paper_Mid_Nave")), 1);
            // The Edge keeps the prologue's white sheets and its wet edge, and wears the kit's three strips now.
            var edge = SceneText(Edge);
            foreach (var m in new[] { "M_Paper_Mid_Edge", "M_Paper_Far_Cathedral", "M_Paper_Farther_Edge", "M_Prop_WetEdge" })
                Assert.GreaterOrEqual(Count(edge, MatGuid(m)), 1, m + " in the Edge");
            Assert.GreaterOrEqual(Count(edge, "m_Name: Blank_0"), 1, "the prologue's white sheets");
            // The line: stakes across the white, the fights waiting for their scenes; Isolde's camp with her tent, her lamp and her atlas.
            var line = SceneText("Greyfold_Threshold_1");
            Assert.AreEqual(4, Count(line, "m_Name: Prop_Stake"), "tethers staked across the white");
            Assert.GreaterOrEqual(Count(line, "_requiresFlag: threshold.halvard.spoken"), 1, "Halvard speaks first");
            Assert.GreaterOrEqual(Count(SceneText("Greyfold_Threshold_2"), "_requiresFlag: threshold.voss.spoken"), 1, "Voss's one speech first");
            var camp = SceneText("Greyfold_LastCamp_1");
            foreach (var prop in new[] { "Prop_Tent", "Prop_Lamp", "Prop_Atlas" }) Assert.GreaterOrEqual(Count(camp, "m_Name: " + prop), 1, prop + " at her last camp");
            Assert.GreaterOrEqual(Count(camp, Guid("Narrative/NpcInk.cs")), 1, "Isolde is drawn from her sheets");
            // The Hollow: houses and the well, Ilse grey, the doorframe to read; the drift with its islands and the step onto them.
            var hollow = SceneText("Blank_Hollow_2");
            Assert.AreEqual(2, Count(hollow, "m_Name: Prop_House"));
            Assert.GreaterOrEqual(Count(hollow, "m_Name: Prop_Well"), 1);
            Assert.GreaterOrEqual(Count(hollow, "m_Name: Read_Hollow_Doorframe"), 1, "the height marks");
            var drift = SceneText("Blank_Hollow_3");
            Assert.AreEqual(1, Count(drift, Guid("World/DriftField.cs")), "the drift");
            Assert.AreEqual(1, Count(drift, Guid("World/DriftCrossing.cs")), "and the step onto it");
            Assert.AreEqual(6, Regex.Matches(drift, @"m_Name: Island_\d").Count, "six islands drawn, as many shown as drift");
            Assert.GreaterOrEqual(Count(drift, MatGuid("M_Prop_Island_Blank")), 1, "the kit's islands");
            Assert.AreEqual(0, Count(drift, "m_Name: Untethered_"), "no patch: the drift is untethered wall to wall, by its id");
            // The capital: the office door and its desk, Corra's crayon room, the Archivist waiting on the choice; Aury's lamp.
            Assert.GreaterOrEqual(Count(SceneText("Blank_Capital_1"), "m_Name: Prop_Door"), 1);
            var corra = SceneText("Blank_Capital_2");
            Assert.GreaterOrEqual(Count(corra, MatGuid("M_Ground_Crayon")), 1, "a crayon floor");
            Assert.GreaterOrEqual(Count(corra, "m_Name: Prop_Crayon"), 1, "her father on the wall");
            Assert.GreaterOrEqual(Count(SceneText("Blank_Capital_4"), "_requiresFlag: corvin.stance"), 1, "the choice laid out first");
            Assert.GreaterOrEqual(Count(SceneText("Blank_Aury_2"), "m_Name: Prop_Beacon"), 1, "the light still turning");
        }

        [Test]
        public void TheRoomsCarryThePlansCreatures()
        {
            // "lost Remnant ×2" in the plan is two LostRemnant components in the scene; nothing the plan leaves out. The Edge's smudges are the prologue's.
            var scripts = new Dictionary<string, string>
            {
                ["lost Remnant"] = Guid("World/Enemies/LostRemnant.cs"), ["smudge"] = Guid("World/Enemies/Smudge.cs"), ["Warden"] = Guid("World/Enemies/Warden.cs"), ["Sketch"] = Guid("World/Enemies/Sketch.cs"), ["moths"] = Guid("World/Enemies/Mothcloud.cs"),
            };
            int rooms = 0, creatures = 0;
            foreach (var p in Plans.Where(p => p.Id != Edge))
            {
                var text = SceneText(p.Id);
                var wanted = scripts.ToDictionary(kv => kv.Key, _ => 0);
                foreach (var part in Regex.Replace(p.Enemies ?? "", @"\s*\([^)]*\)", "").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var m = Regex.Match(part, @"^(lost Remnant|smudge|Warden|Sketch|moths)(?: ×(\d+))?$");
                    Assert.IsTrue(m.Success, p.Id + " plans a creature the greybox knows: " + part);
                    wanted[m.Groups[1].Value] += m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 1;
                }
                foreach (var kv in wanted)
                {
                    Assert.AreEqual(kv.Value, Count(text, scripts[kv.Key]), p.Id + " has the plan's " + kv.Key + "s");
                    creatures += kv.Value;
                }
                rooms++;
            }
            Assert.AreEqual(20, rooms);
            Assert.AreEqual(23, creatures, "ten of the lost, four smudges, the line's three Wardens, five Sketches and the nave's ash-moths");
            // Every vantage keeps its distance from the creatures (an enemy within eight units cancels a survey).
            foreach (var p in Plans.Where(p => p.Vantage != null && p.Id != Edge))
            {
                var text = SceneText(p.Id);
                var v = Regex.Match(text, @"m_Name: Vantage_" + p.Vantage + @"[\s\S]*?m_LocalPosition: \{x: ([-0-9.]+), y: ([-0-9.]+)");
                Assert.IsTrue(v.Success, p.Id + "'s vantage object");
                var at = new Vector2(F(v.Groups[1].Value), F(v.Groups[2].Value));
                foreach (Match e in Regex.Matches(text, @"m_Name: (Warden|Smudge|Lost)_\d+[\s\S]*?m_LocalPosition: \{x: ([-0-9.]+), y: ([-0-9.]+)"))
                    Assert.Greater(Vector2.Distance(at, new Vector2(F(e.Groups[2].Value), F(e.Groups[3].Value))), 8f, p.Id + ": " + e.Groups[1].Value + " clear of the vantage");
            }
        }
    }
}
