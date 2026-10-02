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
    /// Windreach built (ENV-07, docs/design/windreach-greyfold-blank-rooms.md): the Steppe's kit is rendered in its palette
    /// at the kit's densities, every one of the fourteen planned rooms is a scene whose exits are the plan's, its vantages,
    /// desks, people and arenas stand where the plan puts them, the south road leaves Lowmarket, the camp's three sites
    /// stand in their real rooms, the grass is rows of tufts on fields that lean, the updrafts are drawn as ink, the Wind
    /// Gate's course is the region's gauntlet, and the optional fights wait for their flags.
    /// </summary>
    public class WindreachRoomsTests
    {
        const string Kit = "Assets/_Project/Art/Environment/Windreach/";
        const string Scenes = "Assets/_Project/Scenes/Greybox/";
        const string Materials = "Assets/_Project/Art/Materials/";

        [Serializable] class Manifest { public string region; public int ppu, tilePpu; public Layer[] layers; }
        [Serializable] class Layer { public string name, kind, file; public float widthUnits, heightUnits; public int ppu, widthPx, heightPx; }

        public static readonly string[] Strips = { "Paper_Fore_Grass", "Paper_Mid_Stones", "Paper_Mid_Camp", "Paper_Mid_River", "Paper_Mid_Cliff", "Paper_Mid_WindGate", "Paper_Mid_HighGrass", "Paper_Mid_Hearth", "Paper_Mid_Crater", "Paper_Far_Steppe", "Paper_Far_Rim", "Paper_Farther_Storm" };
        public static readonly string[] Tiles = { "Ground_Turf", "Ground_Cracked", "Ground_Lip", "Ground_Cinder" };
        public static readonly string[] Props = { "Prop_Desk", "Prop_Ledger", "Prop_Vantage", "Prop_Lamp", "Prop_LampGlow", "Prop_Seeds", "Prop_Bound", "Prop_Stone", "Prop_Wagon", "Prop_Fire", "Prop_Ashes", "Prop_Bedroll", "Prop_Hull", "Prop_Reeds", "Prop_LipStone", "Prop_Swirl", "Prop_Hearth", "Prop_Grass_A", "Prop_Grass_B", "Prop_Grass_C", "Prop_Grass_Tall" };

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

        static IEnumerable<RoomPlan> Plans => RoomPlans.All.Where(p => p.Id.StartsWith("Windreach_"));
        static bool Unbuilt(string to) => false;   // the Greyfold is built (ENV-08): the glide down lands on its white shore

        [Test]
        public void TheSteppesKitIsRenderedInItsPalette()
        {
            var kit = LoadKit();
            Assert.AreEqual("Windreach", kit.region);
            Assert.AreEqual(40, kit.ppu); Assert.AreEqual(96, kit.tilePpu);
            CollectionAssert.AreEquivalent(Strips.Concat(Tiles).Concat(Props), kit.layers.Select(l => l.name), "twelve strips, four tiles, twenty-one props");
            foreach (var l in kit.layers)
            {
                var (w, h) = PngSize(Path.GetFullPath(Kit + l.file));
                Assert.AreEqual(l.widthPx, w, l.name); Assert.AreEqual(l.heightPx, h, l.name);
                Assert.AreEqual(0, w % 4, l.name + " width a multiple of 4"); Assert.AreEqual(0, h % 4, l.name);
                if (l.kind == "strip") { Assert.AreEqual(80f, l.widthUnits, l.name + " is a parallax strip"); Assert.AreEqual(40, l.ppu); }
                if (l.kind == "tile") { Assert.AreEqual(4f, l.widthUnits); Assert.AreEqual(1f, l.heightUnits); Assert.AreEqual(96, l.ppu); }
                if (l.kind == "prop") Assert.AreEqual(96, l.ppu);
            }
            // The paper the kit was drawn on is the paper the rooms are lit with: straw.
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var s in Strips)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + s + ".mat");
                Assert.IsNotNull(mat, "M_" + s + " (the rooms were built with the kit)");
                Assert.AreEqual(ink, mat.shader, s);
                var paper = mat.GetColor("_PaperColor");
                Assert.AreEqual(0.93f, paper.r, 0.01f, s + " on straw paper"); Assert.AreEqual(0.70f, paper.b, 0.01f, s);
            }
            foreach (var t in Tiles)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + t + ".mat");
                Assert.IsNotNull(mat, "M_" + t);
                Assert.AreEqual(1f, mat.GetFloat("_WorldUV"), t + " tiles in world space");
            }
            // No two kits share a layer name: a material is named after its layer, and the last build would win.
            var all = new Dictionary<string, string>();
            foreach (var dir in Directory.GetDirectories(Path.GetFullPath("Assets/_Project/Art/Environment")))
            {
                string manifest = Path.Combine(dir, "kit.json");
                if (!File.Exists(manifest)) continue;
                var k = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifest));
                foreach (var l in k.layers)
                {
                    if (l.kind == "prop") continue;   // the props are the same drawings in each region's palette, named per region by the builder
                    Assert.IsFalse(all.ContainsKey(l.name), l.name + " is in both " + k.region + " and " + (all.ContainsKey(l.name) ? all[l.name] : ""));
                    all[l.name] = k.region;
                }
            }
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Prop_Desk_Windreach.mat"), "the Steppe's desk is its own");
            Assert.AreEqual(0.92f, AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Prop_Desk_Halden.mat").GetColor("_PaperColor").r, 0.01f, "and the Plateau's is still the Plateau's");
        }

        [Test]
        public void EveryPlannedRoomIsASceneWithThePlansExits()
        {
            var plans = Plans.ToList();
            Assert.AreEqual(14, plans.Count, "the plan's fourteen");
            string transition = Guid("World/RoomTransition.cs");
            foreach (var p in plans)
            {
                string path = Scenes + "Greybox_" + p.Id + ".unity";
                Assert.IsTrue(File.Exists(Path.GetFullPath(path)), p.Id + " is built");
                var text = SceneText(p.Id);
                Assert.GreaterOrEqual(Count(text, "RoomId: " + p.Id), 1, p.Id + " names itself");
                foreach (var e in p.Exits)
                {
                    if (Unbuilt(e.To)) continue;
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
                if (p.Arena == "hale") Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/Hale.cs")), 1, "Hale in " + p.Id);
                if (p.Arena == "fallen_star") Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/FallenStar.cs")), 1, "the Star in " + p.Id);
                if (p.Arena != null) { Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/BossArena.cs")), 1, p.Id + "'s arena"); Assert.GreaterOrEqual(Count(text, "_bossId: " + p.Arena), 1); }
            }
            // The south road leaves Lowmarket's gate and comes back to it.
            Assert.GreaterOrEqual(Count(SceneText("Halden_Lowmarket_3"), "TargetScene: Greybox_Windreach_Stones_1"), 1, "the south road out of Lowmarket");
            Assert.GreaterOrEqual(Count(SceneText("Windreach_Stones_1"), "TargetScene: Greybox_Halden_Lowmarket_3"), 1, "and back");
            Assert.AreEqual("Greybox_Windreach_Camp_2", WorldGraph.BuiltRoomScene("Windreach.LongGrassCamp"), "the epilogue walks to Idrenne's fire ring");
            Assert.AreEqual("Windreach", Atlas.FindPlace("Windreach_Camp_1").Region);
            Assert.AreEqual("the wagons' desk", Atlas.FindWaypoint("desk.Windreach_Camp_1").Name);
        }

        [Test]
        public void TheCampStandsInItsRoomsAndTheGrassLeans()
        {
            // The camp's three sites: the camp group with Idrenne on that site's fire scene and the bedroll, the ashes group, the CampSite that chooses.
            var fireNodes = new[] { "Camp_Idrenne", "River_Idrenne_Night", "Grass_Idrenne_Night" };
            for (int site = 0; site < Camp.SiteCount; site++)
            {
                var text = SceneText(Camp.RoomOfSite(site));
                Assert.GreaterOrEqual(Count(text, Guid("Narrative/CampSite.cs")), 1, Camp.RoomOfSite(site) + " is a site");
                Assert.GreaterOrEqual(Count(text, "SiteRoom: " + Camp.RoomOfSite(site)), 1, "and knows which");
                Assert.GreaterOrEqual(Count(text, "_startNode: " + fireNodes[site]), 1, "Idrenne with the " + fireNodes[site] + " scene");
                Assert.GreaterOrEqual(Count(text, "_startNode: " + Narrative.CampSite.BedrollNode), 1, "the bedroll");
                Assert.GreaterOrEqual(Count(text, "_startNode: " + Narrative.CampSite.AshesAheadNode), 1, "the ashes");
                Assert.GreaterOrEqual(Count(text, "m_Name: Spawn_" + World.CampWalk.SpawnName), 1, "where the walk arrives");
                Assert.GreaterOrEqual(Count(text, "m_Name: Prop_Wagon"), 3, "wagons in a ring");
                Assert.GreaterOrEqual(Count(text, "m_Name: Prop_Fire"), 1);
                Assert.GreaterOrEqual(Count(text, "m_Name: Prop_Ashes"), 1);
            }
            var post = SceneText(Camp.PostRoom);
            Assert.AreEqual(0, Count(post, Guid("Narrative/CampSite.cs")), "the post is not a site");
            Assert.GreaterOrEqual(Count(post, Guid("World/CommissionLedger.cs")), 1, "the post keeps the ledger");
            Assert.GreaterOrEqual(Count(post, "_hubId: Windreach"), 1, "the camp's own");
            Assert.GreaterOrEqual(Count(post, "m_Name: Prop_Wagon"), 1, "and the wagon that stays");
            Assert.GreaterOrEqual(Count(post, Guid("World/DraftingDesk.cs")), 1);

            // The grass: rows of tufts on fields, every tuft the kit's drawing on a root; the high grass before and behind her.
            var stones = SceneText("Windreach_Stones_2");
            Assert.GreaterOrEqual(Count(stones, Guid("World/GrassField.cs")), 1, "a field of grass");
            Assert.GreaterOrEqual(Regex.Matches(stones, @"m_Name: Tuft_\d+").Count, 30, "rows of tufts");
            Assert.GreaterOrEqual(Count(stones, "m_Name: Prop_Grass_"), 30, "each the kit's drawing");
            var high = SceneText("Windreach_Fire_1");
            Assert.GreaterOrEqual(Count(high, "m_Name: Prop_Grass_Tall"), 30, "the high grass");
            Assert.GreaterOrEqual(Count(high, MatGuid("M_Paper_Fore_Grass")), 1, "and grass in front of the walk");
            Assert.GreaterOrEqual(Count(high, MatGuid("M_Paper_Mid_HighGrass")), 1, "over her head behind it");
            foreach (var mat in new[] { "M_Ground_Turf", "M_Paper_Mid_Stones", "M_Paper_Far_Steppe", "M_Paper_Farther_Storm" })
                Assert.GreaterOrEqual(Count(stones, MatGuid(mat)), 1, mat + " dresses the walk");

            // The updrafts: drawn as ink and lifting; the Gate's course is the region's gauntlet on the room's own ground.
            Assert.AreEqual(2, Count(stones, Guid("World/Updraft.cs")), "two swirls on the long walk, too weak to ride");
            Assert.GreaterOrEqual(Count(stones, "speed: 3"), 2);
            var gate = SceneText("Windreach_Gate_2");
            Assert.AreEqual(3, Count(gate, Guid("World/Updraft.cs")), "the three ink-swirls to ride");
            Assert.AreEqual(3, Count(gate, Guid("World/InkSwirl.cs")), "each drawn");
            Assert.GreaterOrEqual(Count(gate, "m_Name: Swirl_"), 15, "stacked up the columns");
            Assert.GreaterOrEqual(Count(gate, Guid("World/Gauntlet.cs")), 1, "the gauntlet");
            Assert.GreaterOrEqual(Count(gate, "Id: updrafts"), 1);
            Assert.GreaterOrEqual(Count(gate, "m_Name: Hazard_updrafts"), 1, "the long grass she falls into");
            Assert.GreaterOrEqual(Count(gate, "m_Name: Goal_updrafts"), 1, "and the far ledge");
            Assert.GreaterOrEqual(Count(gate, "m_Name: Ledge_"), 1, "three units up");
            var star = SceneText("Windreach_Star_1");
            Assert.GreaterOrEqual(Count(star, Guid("World/Updraft.cs")), 1, "the crater's heat carries her back to the rim");
            Assert.GreaterOrEqual(Count(star, "m_Name: Prop_Wagon"), 1, "the smiths' wagon");

            // The cut bank climbs by Talonhold; the Gate's lip is carved; the hearth has its stone; the fights wait for their flags.
            var bank = SceneText("Windreach_River_3");
            Assert.GreaterOrEqual(Regex.Matches(bank, @"m_Name: Wall_\d+").Count, 2, "walls to hold");
            Assert.GreaterOrEqual(Count(bank, MatGuid("M_Paper_Mid_Cliff")), 1);
            var lip = SceneText("Windreach_Gate_1");
            Assert.AreEqual(3, Count(lip, "m_Name: Prop_LipStone"), "the flat stones, each carved with a place");
            Assert.GreaterOrEqual(Count(lip, "m_Name: Idrenne_Greybox"), 1, "the clan sings");
            Assert.GreaterOrEqual(Count(lip, Guid("Narrative/NpcInk.cs")), 1, "Idrenne is drawn from her sheets");
            Assert.GreaterOrEqual(Count(SceneText("Windreach_Fire_2"), "m_Name: Prop_Hearth"), 1, "the cooking-stone on the hearth");
            var ninth = SceneText("Windreach_Stones_3");
            Assert.GreaterOrEqual(Count(ninth, "_requiresFlag: windreach.hale.challenged"), 1, "the duel waits for her to call it");
            Assert.GreaterOrEqual(Count(ninth, "m_Name: Hale_Greybox"), 1, "and he speaks first");
            Assert.GreaterOrEqual(Count(ninth, "stoneXs:"), 1, "nine stones along the floor");
            Assert.GreaterOrEqual(Count(SceneText("Windreach_Star_2"), "_requiresFlag: windreach.star.woken"), 1, "the Star wakes when the stone is lifted");
            Assert.AreEqual(7, Count(SceneText("Windreach_Stones_2"), "m_Name: Prop_Stone"), "stones two to eight");
            Assert.GreaterOrEqual(Count(SceneText("Windreach_River_2"), "m_Name: Prop_Hull"), 2, "boats on their sides");
        }

        [Test]
        public void TheSteppesRoomsCarryThePlansCreatures()
        {
            // "smudge ×2" in the plan is two Smudge components in the scene; Hale's escort is a Warden out of uniform; nothing the plan leaves out.
            var scripts = new Dictionary<string, string>
            {
                ["Warden"] = Guid("World/Enemies/Warden.cs"), ["smudge"] = Guid("World/Enemies/Smudge.cs"), ["moths"] = Guid("World/Enemies/Mothcloud.cs"),
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
                Assert.AreEqual(0, Count(text, Guid("World/HeldState.cs")), p.Id + ": Windreach is never held, and never anchored");
                rooms++;
            }
            Assert.AreEqual(14, rooms);
            Assert.AreEqual(9, creatures, "one escort, six smudges and two dust-moth clouds");
            // Every vantage keeps its distance from the creatures (an enemy within eight units cancels a survey).
            foreach (var p in Plans.Where(p => p.Vantage != null))
            {
                var text = SceneText(p.Id);
                var v = Regex.Match(text, @"m_Name: Vantage_" + p.Vantage + @"[\s\S]*?m_LocalPosition: \{x: ([-0-9.]+), y: ([-0-9.]+)");
                Assert.IsTrue(v.Success, p.Id + "'s vantage object");
                var at = new Vector2(F(v.Groups[1].Value), F(v.Groups[2].Value));
                foreach (Match e in Regex.Matches(text, @"m_Name: (Warden|Smudge)_\d+[\s\S]*?m_LocalPosition: \{x: ([-0-9.]+), y: ([-0-9.]+)"))
                    Assert.Greater(Vector2.Distance(at, new Vector2(F(e.Groups[2].Value), F(e.Groups[3].Value))), 8f, p.Id + ": " + e.Groups[1].Value + " clear of the vantage");
            }
        }
    }
}
