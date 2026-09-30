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
    /// The Verdance built (ENV-04, docs/design/emberdown-verdance-rooms.md §2): the forest's kit is rendered in its palette
    /// at the kit's densities, every one of the nineteen planned rooms is a scene whose exits are the plan's, its vantages,
    /// desks, people and arenas stand where the plan puts them, the thread has anchor-points where only a thread crosses,
    /// the Choir waits for the last day to be stopped, and the Pale Iris Fields join Reedmother's crown to the road.
    /// </summary>
    public class VerdanceRoomsTests
    {
        const string Kit = "Assets/_Project/Art/Environment/Verdance/";
        const string Scenes = "Assets/_Project/Scenes/Greybox/";
        const string Materials = "Assets/_Project/Art/Materials/";

        [Serializable] class Manifest { public string region; public int ppu, tilePpu; public Layer[] layers; }
        [Serializable] class Layer { public string name, kind, file; public float widthUnits, heightUnits; public int ppu, widthPx, heightPx; }

        public static readonly string[] Strips = { "Paper_Fore_Ferns", "Paper_Mid_Trunks", "Paper_Mid_Roots", "Paper_Mid_Branches", "Paper_Mid_Shelves", "Paper_Mid_Village", "Paper_Mid_Ash", "Paper_Mid_Gate", "Paper_Far_Canopy", "Paper_Far_Lanterns", "Paper_Farther_Forest" };
        public static readonly string[] Tiles = { "Ground_Root", "Ground_Moss", "Ground_Flag", "Ground_Lane" };
        public static readonly string[] Props = { "Prop_Desk", "Prop_Ledger", "Prop_Vantage", "Prop_Lamp", "Prop_LampGlow", "Prop_Seeds", "Prop_Bound", "Prop_Milestone", "Prop_Lantern", "Prop_Lectern", "Prop_Bunting", "Prop_Anchor" };

        static Manifest LoadKit() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath(Kit + "kit.json")));
        static string SceneText(string id) => File.ReadAllText(Path.GetFullPath(Scenes + "Greybox_" + id + ".unity"));
        static string Guid(string codePath) => AssetDatabase.AssetPathToGUID("Assets/_Project/Code/" + codePath);
        static string MatGuid(string mat) => AssetDatabase.AssetPathToGUID(Materials + mat + ".mat");
        static int Count(string text, string needle) => text.Split(new[] { needle }, StringSplitOptions.None).Length - 1;

        static (int w, int h) PngSize(string path)
        {
            using var fs = File.OpenRead(path);
            var b = new byte[24];
            Assert.AreEqual(24, fs.Read(b, 0, 24));
            return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
        }

        static IEnumerable<RoomPlan> Plans => RoomPlans.All.Where(p => p.Id.StartsWith("Verdance_"));

        [Test]
        public void TheForestsKitIsRenderedInItsPalette()
        {
            var kit = LoadKit();
            Assert.AreEqual("Verdance", kit.region);
            Assert.AreEqual(40, kit.ppu); Assert.AreEqual(96, kit.tilePpu);
            CollectionAssert.AreEquivalent(Strips.Concat(Tiles).Concat(Props), kit.layers.Select(l => l.name), "eleven strips, four tiles, twelve props");
            foreach (var l in kit.layers)
            {
                var (w, h) = PngSize(Path.GetFullPath(Kit + l.file));
                Assert.AreEqual(l.widthPx, w, l.name); Assert.AreEqual(l.heightPx, h, l.name);
                Assert.AreEqual(0, w % 4, l.name + " width a multiple of 4"); Assert.AreEqual(0, h % 4, l.name);
                if (l.kind == "strip") { Assert.AreEqual(80f, l.widthUnits, l.name + " is a parallax strip"); Assert.AreEqual(40, l.ppu); }
                if (l.kind == "tile") { Assert.AreEqual(4f, l.widthUnits); Assert.AreEqual(1f, l.heightUnits); Assert.AreEqual(96, l.ppu); }
                if (l.kind == "prop") Assert.AreEqual(96, l.ppu);
            }
            // The paper the kit was drawn on is the paper the rooms are lit with: pale gold.
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var s in Strips)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + s + ".mat");
                Assert.IsNotNull(mat, "M_" + s + " (the rooms were built with the kit)");
                Assert.AreEqual(ink, mat.shader, s);
                var paper = mat.GetColor("_PaperColor");
                Assert.AreEqual(0.94f, paper.r, 0.01f, s + " on pale gold paper"); Assert.AreEqual(0.72f, paper.b, 0.01f, s);
            }
            foreach (var t in Tiles)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + t + ".mat");
                Assert.IsNotNull(mat, "M_" + t);
                Assert.AreEqual(1f, mat.GetFloat("_WorldUV"), t + " tiles in world space");
            }
            var coast = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath("Assets/_Project/Art/Environment/Saltmarrow/kit.json")));
            Assert.IsTrue(coast.layers.Any(l => l.name == "Paper_Mid_Irises"), "the Pale Iris Fields are in the coast's kit");
        }

        [Test]
        public void EveryPlannedRoomIsASceneWithThePlansExits()
        {
            var plans = Plans.ToList();
            Assert.AreEqual(19, plans.Count, "the plan's nineteen");
            string transition = Guid("World/RoomTransition.cs");
            foreach (var p in plans)
            {
                string path = Scenes + "Greybox_" + p.Id + ".unity";
                Assert.IsTrue(File.Exists(Path.GetFullPath(path)), p.Id + " is built");
                var text = SceneText(p.Id);
                Assert.GreaterOrEqual(Count(text, "RoomId: " + p.Id), 1, p.Id + " names itself");
                foreach (var e in p.Exits)
                {
                    if (e.To.StartsWith("Halden_")) continue;   // the canopy road to the Paper Mills waits for ENV-05
                    string target = e.To.StartsWith("Saltmarrow.") ? "Greybox_Saltmarrow_IrisFields" : "Greybox_" + e.To;
                    Assert.GreaterOrEqual(Count(text, "TargetScene: " + target), 1, p.Id + " exits to " + target + " as planned");
                }
                Assert.GreaterOrEqual(Count(text, transition), p.Exits.Count(e => !e.To.StartsWith("Halden_")), p.Id + "'s transitions");
                if (!string.IsNullOrEmpty(p.Vantage)) Assert.GreaterOrEqual(Count(text, "_vantageId: " + p.Id + "/" + p.Vantage), 1, p.Id + "'s vantage " + p.Vantage);
                if (p.Desk) Assert.GreaterOrEqual(Count(text, Guid("World/DraftingDesk.cs")), 1, p.Id + " has its desk");
                foreach (var npc in p.Npcs)
                {
                    string name = char.ToUpperInvariant(npc[0]) + npc.Substring(1) + "_Greybox";
                    Assert.GreaterOrEqual(Count(text, "m_Name: " + name), 1, npc + " stands in " + p.Id);
                }
                if (p.Arena == "choir") Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/Choir.cs")), 1, "the Choir in " + p.Id);
                if (p.Arena == "gatekeeper") Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/Gatekeeper.cs")), 1, "the Gatekeeper in " + p.Id);
                if (p.Arena != null) Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/BossArena.cs")), 1, p.Id + "'s arena");
                // An Inkthread gate is a gap only a thread crosses: the room on each side of it has anchor-points.
                if (p.Exits.Any(e => e.Needs == Ability.Inkthread && !e.To.StartsWith("Halden_")))
                    Assert.GreaterOrEqual(Count(text, Guid("World/Instruments/TetherAnchor.cs")), 2, p.Id + " has anchor-points over its thread gap");
            }
        }

        [Test]
        public void TheHouseStandsOnTheKitAndTheThreadHasItsAnchors()
        {
            var cloister = SceneText("Verdance_House_2");
            Assert.GreaterOrEqual(Count(cloister, Guid("World/CommissionLedger.cs")), 1, "Teodor's ledger");
            Assert.GreaterOrEqual(Count(cloister, "_hubId: Verdance"), 1, "the ledger is the Verdance's");
            foreach (var prop in new[] { "Prop_Desk", "Prop_Ledger", "Prop_Lantern", "Prop_Vantage" }) Assert.GreaterOrEqual(Count(cloister, "m_Name: " + prop), 1, prop + " in the cloister");
            foreach (var mat in new[] { "M_Ground_Root", "M_Paper_Mid_Roots", "M_Paper_Far_Canopy", "M_Paper_Farther_Forest" })
                Assert.GreaterOrEqual(Count(cloister, MatGuid(mat)), 1, mat + " dresses the cloister");
            Assert.AreEqual(0, Count(cloister, MatGuid("M_Paper_Mid_Reeds")), "no reeds in the forest");
            Assert.GreaterOrEqual(Count(cloister, "m_Name: Teodor_Greybox"), 1);
            Assert.GreaterOrEqual(Count(cloister, Guid("Narrative/NpcInk.cs")), 1, "Teodor is drawn from his sheets");

            // The chapel's thread gap and the grove's gauntlet: permanent anchor-points, drawn as the kit's knots.
            var chapel = SceneText("Verdance_Chapel_2");
            Assert.AreEqual(2, Count(chapel, Guid("World/Instruments/TetherAnchor.cs")), "two anchor-points over the chapel's gap");
            Assert.AreEqual(2, Count(chapel, "_permanent: 1"), "and they last");
            Assert.GreaterOrEqual(Count(chapel, "m_Name: Prop_Anchor"), 2, "drawn as knots with a bone ring");
            Assert.GreaterOrEqual(Count(chapel, MatGuid("M_Paper_Far_Lanterns")), 1, "the grove's lanterns show from the chapel");
            var grove = SceneText("Verdance_Grove_1");
            Assert.AreEqual(3, Count(grove, Guid("World/Instruments/TetherAnchor.cs")), "three anchor-points in the first gauntlet");
            var vigil = SceneText("Verdance_Grove_2");
            Assert.AreEqual(11, Count(vigil, "m_Name: Prop_Lantern"), "eleven lanterns in a ring");

            // Aldermere's square: the Choir's arena waits for the last day to be stopped, and erases the square's platforms.
            var square = SceneText("Verdance_Aldermere_2");
            Assert.GreaterOrEqual(Count(square, "_bossId: choir"), 1);
            Assert.GreaterOrEqual(Count(square, "_requiresFlag: verdance.aldermere.stopped"), 1, "the Choir sings only if Wren tries to stop the last day");
            Assert.GreaterOrEqual(Count(square, "placeId: Verdance_Aldermere_2"), 1, "the bells erase the square");
            var plats = Regex.Match(square, @"platforms:\r?\n((?:\s+- \{fileID: \d+\}\r?\n)+)");
            Assert.IsTrue(plats.Success, "the square's platforms are the Choir's");
            Assert.AreEqual(3, Regex.Matches(plats.Groups[1].Value, @"fileID: [1-9]").Count, "three platforms to erase, one a bell");
            Assert.GreaterOrEqual(Count(square, "m_Name: Prop_Bunting"), 2, "the last day dressed as a festival");
            Assert.GreaterOrEqual(Count(square, "m_Name: Hollin_Greybox"), 1, "the mayor in the square");
            var ash = SceneText("Verdance_Aldermere_3");
            Assert.GreaterOrEqual(Count(ash, MatGuid("M_Paper_Mid_Ash")), 1, "the ash field is already paper");
            var brann = SceneText("Emberdown_Stair_3");
            Assert.AreEqual(0, Count(brann, "_requiresFlag: verdance"), "a required fight has no flag");

            // The gate: the Gatekeeper holds by three roots, the inn's keeper is a Remnant.
            var gate = SceneText("Verdance_Gate_2");
            Assert.GreaterOrEqual(Count(gate, "_bossId: gatekeeper"), 1);
            var roots = Regex.Match(gate, @"rootPoints:\r?\n((?:\s+- \{x: [^\r\n]+\r?\n)+)");
            Assert.IsTrue(roots.Success, "the Gatekeeper's roots");
            Assert.AreEqual(3, Regex.Matches(roots.Groups[1].Value, @"- \{x:").Count, "three roots to hold by");
            Assert.GreaterOrEqual(Count(gate, "m_Name: Innkeeper_Greybox"), 1, "the one-night inn");
            Assert.GreaterOrEqual(Count(gate, MatGuid("M_Paper_Mid_Gate")), 1, "the gate's stone and roots");
            var library = SceneText("Verdance_Library_2");
            Assert.GreaterOrEqual(Count(library, "m_Name: Ansel_Greybox"), 1, "Brother Ansel on page 214");
            Assert.GreaterOrEqual(Count(library, "m_Name: Prop_Lectern"), 1, "his lectern");
            Assert.GreaterOrEqual(Count(library, MatGuid("M_Ground_Flag")), 1, "on flagstones");
            var road = SceneText("Verdance_Road_2");
            Assert.AreEqual(3, Count(road, "m_Name: Prop_Milestone"), "three milestones");
        }

        [Test]
        public void TheForestsRoomsCarryThePlansCreatures()
        {
            // "skimmer x2" in the plan is two ReedSkimmer components in the scene, and nothing the plan leaves out.
            var scripts = new Dictionary<string, string>
            {
                ["crab"] = Guid("World/Enemies/MarshCrab.cs"), ["skimmer"] = Guid("World/Enemies/ReedSkimmer.cs"),
                ["smudge"] = Guid("World/Enemies/Smudge.cs"), ["Cantor"] = Guid("World/Enemies/Cantor.cs"),
            };
            int rooms = 0, creatures = 0;
            foreach (var p in Plans)
            {
                var text = SceneText(p.Id);
                var wanted = scripts.ToDictionary(kv => kv.Key, _ => 0);
                foreach (var part in Regex.Replace(p.Enemies ?? "", @"\s*\([^)]*\)", "").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var m = Regex.Match(part, @"^([A-Za-z-]+)(?: ×(\d+))?");
                    Assert.IsTrue(m.Success && wanted.ContainsKey(m.Groups[1].Value), p.Id + " plans a creature the greybox knows: " + part);
                    wanted[m.Groups[1].Value] += m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 1;
                }
                foreach (var kv in wanted)
                {
                    Assert.AreEqual(kv.Value, Count(text, scripts[kv.Key]), p.Id + " has the plan's " + kv.Key + "s");
                    creatures += kv.Value;
                }
                rooms++;
            }
            Assert.AreEqual(19, rooms);
            Assert.AreEqual(14, creatures, "two crabs, five skimmers, four smudges and three Cantors");
            // Every vantage keeps its distance from the creatures (an enemy within eight units cancels a survey).
            foreach (var p in Plans.Where(p => p.Vantage != null))
            {
                var text = SceneText(p.Id);
                var v = Regex.Match(text, @"m_Name: Vantage_" + p.Vantage + @"[\s\S]*?m_LocalPosition: \{x: ([-0-9.]+), y: ([-0-9.]+)");
                Assert.IsTrue(v.Success, p.Id + "'s vantage object");
                var at = new Vector2(F(v.Groups[1].Value), F(v.Groups[2].Value));
                foreach (Match e in Regex.Matches(text, @"m_Name: (Crab|Skimmer|Smudge|Cantor)_\d+[\s\S]*?m_LocalPosition: \{x: ([-0-9.]+), y: ([-0-9.]+)"))
                    Assert.Greater(Vector2.Distance(at, new Vector2(F(e.Groups[2].Value), F(e.Groups[3].Value))), 8f, p.Id + ": " + e.Groups[1].Value + " clear of the vantage");
            }
        }

        static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

        [Test]
        public void TheIrisFieldsJoinTheCrownToTheRoad()
        {
            var fields = SceneText("Saltmarrow_IrisFields");
            Assert.GreaterOrEqual(Count(fields, "TargetScene: Greybox_Saltmarrow_Roots_4"), 1, "west to Reedmother's crown");
            Assert.GreaterOrEqual(Count(fields, "TargetScene: Greybox_Verdance_Road_1"), 1, "east to the road");
            Assert.GreaterOrEqual(Count(fields, MatGuid("M_Paper_Mid_Irises")), 1, "irises to the horizon");
            Assert.GreaterOrEqual(Count(fields, "_vantageId: Saltmarrow_IrisFields/Irises"), 1, "the fields are drawn here");
            Assert.GreaterOrEqual(Count(fields, Guid("World/IrisSeed.cs")), 3, "iris seeds lying about (the Ferrymen's purse)");
            var crown = SceneText("Saltmarrow_Roots_4");
            Assert.GreaterOrEqual(Count(crown, "TargetScene: Greybox_Saltmarrow_IrisFields"), 1, "the crown opens east");
            Assert.GreaterOrEqual(Count(crown, "m_Name: Spawn_East"), 1);
            var road = SceneText("Verdance_Road_1");
            Assert.GreaterOrEqual(Count(road, "TargetScene: Greybox_Saltmarrow_IrisFields"), 1, "and the road back to the fields");
            Assert.IsNotNull(Atlas.FindPlace("Saltmarrow_IrisFields"), "on the atlas");
            Assert.AreEqual("Saltmarrow.IrisFields", WorldGraph.ZoneOfPlace("Saltmarrow_IrisFields"), "on the map");
            Assert.AreEqual("Verdance.QuietHouse", WorldGraph.ZoneOfPlace("Verdance_House_2"));
            Assert.AreEqual("The Verdance", Atlas.FindPlace("Verdance_House_2").Region);
        }
    }
}
