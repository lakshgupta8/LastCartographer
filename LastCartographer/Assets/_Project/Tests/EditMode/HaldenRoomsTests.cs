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
    /// Halden built (ENV-05, docs/design/halden-rooms.md): the Plateau's kit is rendered in its palette at the kit's
    /// densities, every one of the twenty-one planned rooms is a scene whose exits are the plan's, its vantages, desks,
    /// people and arenas stand where the plan puts them, both climbs' east roads come down onto it, the flyer-tower has
    /// walls to hold and anchor-points to thread, and the optional fights wait for their flags.
    /// </summary>
    public class HaldenRoomsTests
    {
        const string Kit = "Assets/_Project/Art/Environment/Halden/";
        const string Scenes = "Assets/_Project/Scenes/Greybox/";
        const string Materials = "Assets/_Project/Art/Materials/";

        [Serializable] class Manifest { public string region; public int ppu, tilePpu; public Layer[] layers; }
        [Serializable] class Layer { public string name, kind, file; public float widthUnits, heightUnits; public int ppu, widthPx, heightPx; }

        public static readonly string[] Strips = { "Paper_Fore_Balustrade", "Paper_Mid_Bridges", "Paper_Mid_Mills", "Paper_Mid_Lowmarket", "Paper_Mid_Hall", "Paper_Mid_Orchard", "Paper_Mid_Tower", "Paper_Mid_Dome", "Paper_Far_Citadel", "Paper_Far_Drop", "Paper_Farther_Sky" };
        public static readonly string[] Tiles = { "Ground_Granite", "Ground_Boards", "Ground_Parquet", "Ground_Cobble" };
        public static readonly string[] Props = { "Prop_Desk", "Prop_Ledger", "Prop_Vantage", "Prop_Lamp", "Prop_LampGlow", "Prop_Seeds", "Prop_Bound", "Prop_Anchor", "Prop_Gravestone", "Prop_Wheel", "Prop_Scaffold", "Prop_Frame", "Prop_Slots", "Prop_ExamDesk", "Prop_Notice",
            "Prop_TollBoard", "Prop_Sheets", "Prop_Order", "Prop_Roll", "Prop_Plaque", "Prop_Drawing", "Prop_Leaves", "Prop_Notice_Complete" };   // the dressing (ENV-06)

        static Manifest LoadKit() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath(Kit + "kit.json")));
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

        static IEnumerable<RoomPlan> Plans => RoomPlans.All.Where(p => p.Id.StartsWith("Halden_"));
        static bool Unbuilt(string to) => false;   // the Greyfold is built (ENV-08): every road off the Plateau leads somewhere

        [Test]
        public void ThePlateausKitIsRenderedInItsPalette()
        {
            var kit = LoadKit();
            Assert.AreEqual("Halden", kit.region);
            Assert.AreEqual(40, kit.ppu); Assert.AreEqual(96, kit.tilePpu);
            CollectionAssert.AreEquivalent(Strips.Concat(Tiles).Concat(Props), kit.layers.Select(l => l.name), "eleven strips, four tiles, twenty-three props");
            foreach (var l in kit.layers)
            {
                var (w, h) = PngSize(Path.GetFullPath(Kit + l.file));
                Assert.AreEqual(l.widthPx, w, l.name); Assert.AreEqual(l.heightPx, h, l.name);
                Assert.AreEqual(0, w % 4, l.name + " width a multiple of 4"); Assert.AreEqual(0, h % 4, l.name);
                if (l.kind == "strip") { Assert.AreEqual(80f, l.widthUnits, l.name + " is a parallax strip"); Assert.AreEqual(40, l.ppu); }
                if (l.kind == "tile") { Assert.AreEqual(4f, l.widthUnits); Assert.AreEqual(1f, l.heightUnits); Assert.AreEqual(96, l.ppu); }
                if (l.kind == "prop") Assert.AreEqual(96, l.ppu);
            }
            // The paper the kit was drawn on is the paper the rooms are lit with: cool cream.
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var s in Strips)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + s + ".mat");
                Assert.IsNotNull(mat, "M_" + s + " (the rooms were built with the kit)");
                Assert.AreEqual(ink, mat.shader, s);
                var paper = mat.GetColor("_PaperColor");
                Assert.AreEqual(0.92f, paper.r, 0.01f, s + " on cool cream paper"); Assert.AreEqual(0.87f, paper.b, 0.01f, s);
            }
            foreach (var t in Tiles)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + t + ".mat");
                Assert.IsNotNull(mat, "M_" + t);
                Assert.AreEqual(1f, mat.GetFloat("_WorldUV"), t + " tiles in world space");
            }
            // A prop's material is the region's own (ENV-04): the coast's desk is still the coast's.
            var coastDesk = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Prop_Desk.mat");
            Assert.AreEqual(0.93f, coastDesk.GetColor("_PaperColor").r, 0.01f, "the coast's desk on the coast's paper");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Prop_Desk_Halden.mat"), "the Plateau's desk is its own");
        }

        [Test]
        public void EveryPlannedRoomIsASceneWithThePlansExits()
        {
            var plans = Plans.ToList();
            Assert.AreEqual(21, plans.Count, "the plan's twenty-one");
            string transition = Guid("World/RoomTransition.cs");
            foreach (var p in plans)
            {
                string path = Scenes + "Greybox_" + p.Id + ".unity";
                Assert.IsTrue(File.Exists(Path.GetFullPath(path)), p.Id + " is built");
                var text = SceneText(p.Id);
                Assert.GreaterOrEqual(Count(text, "RoomId: " + p.Id), 1, p.Id + " names itself");
                foreach (var e in p.Exits)
                {
                    if (Unbuilt(e.To)) continue;   // the orchard's road to the Edge waits for ENV-08
                    Assert.GreaterOrEqual(Count(text, "TargetScene: Greybox_" + e.To), 1, p.Id + " exits to " + e.To + " as planned");
                }
                Assert.GreaterOrEqual(Count(text, transition), p.Exits.Count(e => !Unbuilt(e.To)), p.Id + "'s transitions");
                if (!string.IsNullOrEmpty(p.Vantage)) Assert.GreaterOrEqual(Count(text, "_vantageId: " + p.Id + "/" + p.Vantage), 1, p.Id + "'s vantage " + p.Vantage);
                if (p.Desk) Assert.GreaterOrEqual(Count(text, Guid("World/DraftingDesk.cs")), 1, p.Id + " has its desk");
                foreach (var npc in p.Npcs)
                {
                    string name = char.ToUpperInvariant(npc[0]) + npc.Substring(1) + "_Greybox";
                    Assert.GreaterOrEqual(Count(text, "m_Name: " + name), 1, npc + " stands in " + p.Id);
                }
                if (p.Arena == "halvard_2") Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/Halvard.cs")), 1, "Halvard in " + p.Id);
                if (p.Arena == "oriel") Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/Oriel.cs")), 1, "Oriel in " + p.Id);
                if (p.Arena == "complete_survey") Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/CompleteSurvey.cs")), 1, "the Survey in " + p.Id);
                if (p.Arena != null) { Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/BossArena.cs")), 1, p.Id + "'s arena"); Assert.GreaterOrEqual(Count(text, "_bossId: " + p.Arena), 1); }
            }
            // Both climbs come down onto the Plateau.
            var overlook = SceneText("Emberdown_Overlook_2");
            Assert.GreaterOrEqual(Count(overlook, "TargetScene: Greybox_Halden_Bridges_1"), 1, "the Overlook's road to the bridges");
            var gate = SceneText("Verdance_Gate_2");
            Assert.GreaterOrEqual(Count(gate, "TargetScene: Greybox_Halden_Mills_1"), 1, "the canopy road to the mills");
            Assert.GreaterOrEqual(Count(SceneText("Halden_Bridges_1"), "TargetScene: Greybox_Emberdown_Overlook_2"), 1);
            Assert.GreaterOrEqual(Count(SceneText("Halden_Mills_1"), "TargetScene: Greybox_Verdance_Gate_2"), 1);
            Assert.AreEqual("Greybox_Halden_Hall_2", WorldGraph.BuiltRoomScene("Halden.JourneymansHall"), "the epilogue walks to Pell's Hall");
            Assert.AreEqual("Halden", Atlas.FindPlace("Halden_Hall_2").Region);
        }

        [Test]
        public void TheHallStandsOnTheKitAndTheTowerHasNoStairs()
        {
            var hall = SceneText("Halden_Hall_2");
            Assert.GreaterOrEqual(Count(hall, Guid("World/CommissionLedger.cs")), 1, "the Hall's ledger");
            Assert.GreaterOrEqual(Count(hall, "_hubId: Halden"), 1, "the ledger is Halden's");
            foreach (var prop in new[] { "Prop_Desk", "Prop_Ledger", "Prop_Vantage" }) Assert.GreaterOrEqual(Count(hall, "m_Name: " + prop), 1, prop + " in the Hall");
            foreach (var mat in new[] { "M_Ground_Parquet", "M_Paper_Mid_Hall", "M_Paper_Far_Citadel" })
                Assert.GreaterOrEqual(Count(hall, MatGuid(mat)), 1, mat + " dresses the Hall");
            Assert.AreEqual(0, Count(hall, MatGuid("M_Paper_Farther_Sky")), "no sky inside the Hall");
            Assert.GreaterOrEqual(Count(hall, "m_Name: Pell_Greybox"), 1);
            Assert.GreaterOrEqual(Count(hall, Guid("Narrative/NpcInk.cs")), 1, "Pell is drawn from his sheets");
            Assert.AreEqual(4, Count(SceneText("Halden_Hall_3"), "m_Name: Prop_ExamDesk"), "rows of desks with the same papers");

            // The flyer-tower: walls to hold, anchor-points to thread, no stairs; a desk on the top landing.
            var orchard = SceneText("Halden_Orchard_2");
            Assert.GreaterOrEqual(Regex.Matches(orchard, @"m_Name: Wall_\d+").Count, 2, "the tower's walls rise from the orchard wall");
            Assert.GreaterOrEqual(Count(orchard, Guid("World/Instruments/TetherAnchor.cs")), 1, "and a thread point");
            Assert.GreaterOrEqual(Count(orchard, "m_Name: Prop_Gravestone"), 1, "the gravestone with a crest");
            Assert.GreaterOrEqual(Count(orchard, "m_Name: Isolde_Greybox"), 1, "her cache");
            Assert.AreEqual(1, Count(orchard, Guid("Narrative/NpcInk.cs")), "the keeper's (CHR-12); her cache is drawn as her pages (a stand-in), not as her");
            var tower = SceneText("Halden_Bastion_1");
            Assert.GreaterOrEqual(Regex.Matches(tower, @"m_Name: Wall_\d+").Count, 2, "Talonhold up the walls");
            Assert.AreEqual(2, Count(tower, Guid("World/Instruments/TetherAnchor.cs")), "Inkthread across the gaps");
            var desk = Regex.Match(tower, @"m_Name: DraftingDesk[\s\S]*?m_LocalPosition: \{x: ([-0-9.]+), y: ([-0-9.]+)");
            Assert.IsTrue(desk.Success, "the landing's desk");
            Assert.Greater(F(desk.Groups[2].Value), 10f, "on the top landing");
            Assert.GreaterOrEqual(Count(tower, "m_Name: Maren_Greybox"), 1, "the Crown's audience");
            Assert.GreaterOrEqual(Count(tower, MatGuid("M_Paper_Mid_Tower")), 1, "the tower's inside wall");

            // The optional fights wait for their flags; the Vault has its slots.
            var yard = SceneText("Halden_Bastion_2");
            Assert.GreaterOrEqual(Count(yard, "_requiresFlag: pell.report_sent"), 1, "Oriel receives her only if the report went");
            Assert.GreaterOrEqual(Count(yard, "m_Name: Oriel_Greybox"), 1, "and speaks first");
            var span = SceneText("Halden_Bridges_4");
            Assert.GreaterOrEqual(Count(span, "_requiresFlag: act2.started"), 1, "the second hunt is Act 2's");
            Assert.GreaterOrEqual(Count(span, "m_Name: Halvard_Greybox"), 1, "four paces, now");
            var frame = SceneText("Halden_Observatory_2");
            Assert.GreaterOrEqual(Count(frame, "_requiresFlag: ending.chorus_led"), 1, "the Survey follows Runa's chorus");
            Assert.GreaterOrEqual(Count(frame, "m_Name: Prop_Frame"), 1, "the frame of the Great Atlas");
            foreach (var who in new[] { "Pell", "Voss", "Runa", "Teodor" }) Assert.GreaterOrEqual(Count(frame, "m_Name: " + who + "_Greybox"), 1, who + " at the frame");
            Assert.GreaterOrEqual(Count(frame, MatGuid("M_Paper_Mid_Dome")), 1, "under the dome");
            var vault = SceneText("Halden_Vault_1");
            Assert.GreaterOrEqual(Count(vault, "m_Name: Prop_Slots"), 1, "seven slots, one empty");
            Assert.GreaterOrEqual(Count(vault, "m_Name: Pell_Greybox"), 1, "Pell counts them");
            var bridges = SceneText("Halden_Bridges_3");
            Assert.GreaterOrEqual(Count(bridges, "m_Name: Prop_Scaffold"), 1, "under repair for forty years");
            Assert.GreaterOrEqual(Count(bridges, "m_Name: Arden_Greybox"), 1, "the family paid to stand on it");
            var market = SceneText("Halden_Lowmarket_2");
            Assert.GreaterOrEqual(Count(market, "m_Name: Prop_Notice"), 1, "survey scheduled");
            foreach (var who in new[] { "Brisk", "Anvers" }) Assert.GreaterOrEqual(Count(market, "m_Name: " + who + "_Greybox"), 1, who + " at the strike");
            Assert.GreaterOrEqual(Count(SceneText("Halden_Mills_1"), "m_Name: Prop_Wheel"), 1, "a mill wheel on the race");
        }

        [Test]
        public void ThePlateausRoomsCarryThePlansCreatures()
        {
            // "Warden x2" in the plan is two Warden components in the scene, awake (Halden is anchored), and nothing the plan leaves out.
            var scripts = new Dictionary<string, string>
            {
                ["Warden"] = Guid("World/Enemies/Warden.cs"), ["Cantor"] = Guid("World/Enemies/Cantor.cs"), ["smudge"] = Guid("World/Enemies/Smudge.cs"), ["pulp-wasp"] = Guid("World/Enemies/Pulpwasp.cs"), ["Sketch"] = Guid("World/Enemies/Sketch.cs"),
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
                Assert.AreEqual(0, Count(text, Guid("World/HeldState.cs")), p.Id + ": the Wardens are awake, not held");
                rooms++;
            }
            Assert.AreEqual(21, rooms);
            Assert.AreEqual(19, creatures, "ten Wardens, a Cantor, four smudges, three pulp-wasps and Lowmarket's thin Sketch");
            // Every vantage keeps its distance from the creatures (an enemy within eight units cancels a survey).
            foreach (var p in Plans.Where(p => p.Vantage != null))
            {
                var text = SceneText(p.Id);
                var v = Regex.Match(text, @"m_Name: Vantage_" + p.Vantage + @"[\s\S]*?m_LocalPosition: \{x: ([-0-9.]+), y: ([-0-9.]+)");
                Assert.IsTrue(v.Success, p.Id + "'s vantage object");
                var at = new Vector2(F(v.Groups[1].Value), F(v.Groups[2].Value));
                foreach (Match e in Regex.Matches(text, @"m_Name: (Warden|Cantor|Smudge)_\d+[\s\S]*?m_LocalPosition: \{x: ([-0-9.]+), y: ([-0-9.]+)"))
                    Assert.Greater(Vector2.Distance(at, new Vector2(F(e.Groups[2].Value), F(e.Groups[3].Value))), 8f, p.Id + ": " + e.Groups[1].Value + " clear of the vantage");
            }
        }
    }
}
