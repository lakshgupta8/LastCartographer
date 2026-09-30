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
    /// Emberdown built (ENV-03, docs/design/emberdown-verdance-rooms.md §1): the highland's kit is rendered in its palette
    /// at the kit's densities, every one of the twenty-one planned rooms is a scene whose exits are the plan's, its
    /// vantages, desks, people, walk and arenas stand where the plan puts them, and the Bone Bridge joins the coast to
    /// the stair.
    /// </summary>
    public class EmberdownRoomsTests
    {
        const string Kit = "Assets/_Project/Art/Environment/Emberdown/";
        const string Scenes = "Assets/_Project/Scenes/Greybox/";
        const string Materials = "Assets/_Project/Art/Materials/";

        [Serializable] class Manifest { public string region; public int ppu, tilePpu; public Layer[] layers; }
        [Serializable] class Layer { public string name, kind, file; public float widthUnits, heightUnits; public int ppu, widthPx, heightPx; }

        public static readonly string[] Strips = { "Paper_Fore_Slag", "Paper_Mid_Roosts", "Paper_Mid_Furnaces", "Paper_Mid_Springs", "Paper_Mid_Gallery", "Paper_Far_Chimneys", "Paper_Far_Bell", "Paper_Far_Dark", "Paper_Farther_Ridge", "Paper_Farther_White" };
        public static readonly string[] Tiles = { "Ground_Basalt", "Ground_Iron", "Ground_Timber", "Ground_Ash" };
        public static readonly string[] Props = { "Prop_Desk", "Prop_Ledger", "Prop_Vantage", "Prop_Lamp", "Prop_LampGlow", "Prop_Seeds", "Prop_Bound", "Prop_Bell", "Prop_Anvil", "Prop_Boards", "Prop_Porch" };

        static Manifest LoadKit() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath(Kit + "kit.json")));
        static string SceneText(string id) => File.ReadAllText(Path.GetFullPath(Scenes + "Greybox_" + id + ".unity"));
        static string Guid(string codePath) => AssetDatabase.AssetPathToGUID("Assets/_Project/Code/" + codePath);
        static int Count(string text, string needle) => text.Split(new[] { needle }, StringSplitOptions.None).Length - 1;

        static (int w, int h) PngSize(string path)
        {
            using var fs = File.OpenRead(path);
            var b = new byte[24];
            Assert.AreEqual(24, fs.Read(b, 0, 24));
            return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
        }

        static IEnumerable<RoomPlan> Plans => RoomPlans.All.Where(p => p.Id.StartsWith("Emberdown_"));

        [Test]
        public void TheHighlandsKitIsRenderedInItsPalette()
        {
            var kit = LoadKit();
            Assert.AreEqual("Emberdown", kit.region);
            Assert.AreEqual(40, kit.ppu); Assert.AreEqual(96, kit.tilePpu);
            CollectionAssert.IsSubsetOf(Strips.Concat(Tiles).Concat(Props), kit.layers.Select(l => l.name), "every layer the rooms ask for");
            foreach (var l in kit.layers)
            {
                var (w, h) = PngSize(Path.GetFullPath(Kit + l.file));
                Assert.AreEqual(l.widthPx, w, l.name); Assert.AreEqual(l.heightPx, h, l.name);
                Assert.AreEqual(0, w % 4, l.name + " width a multiple of 4"); Assert.AreEqual(0, h % 4, l.name);
                if (l.kind == "strip") { Assert.AreEqual(80f, l.widthUnits, l.name + " is a parallax strip"); Assert.AreEqual(40, l.ppu); }
                if (l.kind == "tile") { Assert.AreEqual(4f, l.widthUnits); Assert.AreEqual(1f, l.heightUnits); Assert.AreEqual(96, l.ppu); }
                if (l.kind == "prop") Assert.AreEqual(96, l.ppu);
            }
            Assert.AreEqual(21, Strips.Length + Tiles.Length + Props.Length - 4, "ten strips, four tiles, eleven props");
            // The paper the kit was drawn on is the paper the rooms are lit with.
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var s in Strips)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + s + ".mat");
                Assert.IsNotNull(mat, "M_" + s + " (the rooms were built with the kit)");
                Assert.AreEqual(ink, mat.shader, s);
                var paper = mat.GetColor("_PaperColor");
                Assert.AreEqual(0.82f, paper.r, 0.01f, s + " on smoke-grey paper"); Assert.AreEqual(0.78f, paper.b, 0.01f, s);
            }
            foreach (var t in Tiles)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + t + ".mat");
                Assert.IsNotNull(mat, "M_" + t);
                Assert.AreEqual(1f, mat.GetFloat("_WorldUV"), t + " tiles in world space");
            }
            var coast = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath("Assets/_Project/Art/Environment/Saltmarrow/kit.json")));
            Assert.IsTrue(coast.layers.Any(l => l.name == "Paper_Mid_Bones"), "the whale's bones are in the coast's kit");
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
                    if (e.To.StartsWith("Halden_")) continue;   // the road to the Plateau waits for ENV-05
                    string target = e.To.StartsWith("Saltmarrow.") ? "Greybox_Saltmarrow_BoneBridge" : "Greybox_" + e.To;
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
                if (p.Arena == "brann") Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/Brann.cs")), 1, "Brann in " + p.Id);
                if (p.Arena == "collapse") Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/Collapse.cs")), 1, "the Collapse in " + p.Id);
                if (p.Arena != null) Assert.GreaterOrEqual(Count(text, Guid("World/Bosses/BossArena.cs")), 1, p.Id + "'s arena");
            }
        }

        [Test]
        public void TheTownStandsOnTheKitAndTheBellTeachesTheWalk()
        {
            var square = SceneText("Emberdown_Rest_2");
            Assert.GreaterOrEqual(Count(square, Guid("World/CommissionLedger.cs")), 1, "Kettil's ledger");
            Assert.GreaterOrEqual(Count(square, "_hubId: Emberdown"), 1, "the ledger is Emberdown's");
            foreach (var prop in new[] { "Prop_Porch", "Prop_Anvil", "Prop_Desk", "Prop_Ledger" }) Assert.GreaterOrEqual(Count(square, "m_Name: " + prop), 1, prop + " in the square");
            Assert.GreaterOrEqual(Count(square, "m_Name: Paper_Fore_Slag"), 1, "slag in front of the square");
            foreach (var mat in new[] { "M_Ground_Ash", "M_Paper_Mid_Roosts", "M_Paper_Far_Bell", "M_Paper_Farther_Ridge" })
                Assert.GreaterOrEqual(Count(square, AssetDatabase.AssetPathToGUID(Materials + mat + ".mat")), 1, mat + " dresses the square");
            Assert.AreEqual(0, Count(square, AssetDatabase.AssetPathToGUID(Materials + "M_Paper_Mid_Reeds.mat")), "no reeds in the highland");

            var bell = SceneText("Emberdown_Bell_2");
            Assert.GreaterOrEqual(Count(bell, Guid("World/BoundsWalk.cs")), 1, "the lesson walk");
            Assert.GreaterOrEqual(Count(bell, "_id: kettils_rest"), 1, "named as the script starts it (<<walk kettils_rest>>)");
            float beat = AudioDirection.BeatOf(Region.Emberdown) * 3f;
            var m = Regex.Match(bell, @"_secondsPerBeat: ([0-9.]+)");
            Assert.IsTrue(m.Success);
            Assert.AreEqual(beat, float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 0.01f, "three Emberdown beats a bound (bounds-walk.md)");
            Assert.AreEqual(3, Count(bell, "Title: "), "three verses");
            Assert.AreEqual(15, Regex.Matches(bell, @"- Name: the ").Count, "five bounds a verse");
            Assert.GreaterOrEqual(Count(bell, "m_Name: Prop_Bell"), 1, "the bell itself");
            Assert.GreaterOrEqual(Count(bell, "m_Name: Runa_Greybox"), 1); Assert.GreaterOrEqual(Count(bell, "m_Name: Kettil_Greybox"), 1);

            var pit = SceneText("Emberdown_Rest_3");
            Assert.GreaterOrEqual(Count(pit, "m_Name: Prop_Boards"), 1, "the mine mouth, boarded");
            var stair = SceneText("Emberdown_Stair_3");
            Assert.GreaterOrEqual(Count(stair, "_bossId: brann"), 1);
            Assert.GreaterOrEqual(Count(stair, AssetDatabase.AssetPathToGUID(Materials + "M_Ground_Iron.mat")), 1, "iron landings");
            var overlook = SceneText("Emberdown_Overlook_1");
            Assert.GreaterOrEqual(Count(overlook, Guid("World/Enemies/Warden.cs")), 1, "the Guild watches the road");
            Assert.GreaterOrEqual(Count(overlook, AssetDatabase.AssetPathToGUID(Materials + "M_Paper_Farther_White.mat")), 1, "the Greyfold on the horizon");
            var bottom = SceneText("Emberdown_Hollow_4");
            Assert.GreaterOrEqual(Count(bottom, "_bossId: collapse"), 1);
        }

        [Test]
        public void TheBoneBridgeJoinsTheCoastToTheStair()
        {
            var bridge = SceneText("Saltmarrow_BoneBridge");
            Assert.GreaterOrEqual(Count(bridge, "TargetScene: Greybox_Saltmarrow_Chapel"), 1, "west to the chapel");
            Assert.GreaterOrEqual(Count(bridge, "TargetScene: Greybox_Emberdown_Stair_1"), 1, "east to the stair");
            Assert.GreaterOrEqual(Count(bridge, AssetDatabase.AssetPathToGUID(Materials + "M_Paper_Mid_Bones.mat")), 1, "the whale's bones over the channel");
            Assert.GreaterOrEqual(Count(bridge, "_vantageId: Saltmarrow_BoneBridge/Whale"), 1, "the whale is drawn here");
            var chapel = SceneText("Saltmarrow_Chapel");
            Assert.GreaterOrEqual(Count(chapel, "TargetScene: Greybox_Saltmarrow_BoneBridge"), 1, "the chapel opens east");
            Assert.GreaterOrEqual(Count(chapel, "m_Name: Spawn_East"), 1);
            var stair = SceneText("Emberdown_Stair_1");
            Assert.GreaterOrEqual(Count(stair, "TargetScene: Greybox_Saltmarrow_BoneBridge"), 1, "and the stair back to the bridge");
            CollectionAssert.Contains(OWSBG.Narrative.RollCallSinger.WhaleVantages, "Saltmarrow_BoneBridge/Whale", "the whale sings for the bridge's drawing too");
        }
    }
}
