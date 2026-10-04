using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The late bosses drawn (CHR-08, CHR-09, CHR-10, docs/design/boss-animation.md): nine bodies with every clip their
    /// Clip properties name, twelve parts the fights make at run time, the Smudge and Cantor families; model sheets
    /// for the bodies and the families; the built rooms rest each boss on its idle strip and carry its parts' sheets;
    /// the regions' smudges and Cantors wear their looks; the memory smudge's sheets ride in the persistent scene.
    /// </summary>
    public class BossDrawingsTests
    {
        const string Art = "Assets/_Project/Art/Characters/";
        const string Materials = "Assets/_Project/Art/Materials/";
        const string Scenes = "Assets/_Project/Scenes/Greybox/";

        static readonly (string name, string[] clips)[] Bodies =
        {
            ("Collapse", new[] { "idle", "rumble", "shake", "surge", "reach", "hurt", "death" }),
            ("Gatekeeper", new[] { "idle", "perch", "fly", "rise", "telegraph", "sweep", "shake", "pass", "land", "recover", "hurt", "death" }),
            ("Hale", new[] { "idle", "move", "sight", "call", "count", "telegraph", "quill", "recover", "hurt", "death" }),
            ("Voss", new[] { "idle", "move", "telegraph", "thrust", "lunge", "guard", "anchor", "recover", "hurt", "death" }),
            ("FallenStar", new[] { "idle", "walk", "telegraph", "slam", "raise", "recover", "burn", "hurt", "death" }),
            ("CorrasDrawing", new[] { "idle", "move", "telegraph", "swipe", "lift", "stomp", "recover", "hurt", "death",
                                      "idle_outline", "move_outline", "telegraph_outline", "swipe_outline", "lift_outline", "stomp_outline", "recover_outline", "hurt_outline", "death_outline" }),
            ("Archivist", new[] { "idle", "telegraph", "draw", "swoop", "recover", "hold", "hurt", "death" }),
            ("CompleteSurvey", new[] { "idle", "hurt", "death" }),
            ("HalfCathedralBells", new[] { "idle", "hurt", "death" }),
            ("ReedmotherBrood", new[] { "idle", "telegraph", "thresh", "call", "open", "burn", "hurt", "death", "calm" }),
        };

        static readonly (string name, string[] clips)[] Parts =
        {
            ("Rubble", new[] { "idle" }),
            ("Surge", new[] { "idle", "move" }),
            ("ChorusLamp", new[] { "dark", "lit" }),
            ("Feather", new[] { "idle" }),
            ("Stone", new[] { "bare", "hale", "wren" }),
            ("StoneStrike", new[] { "erupt" }),
            ("StarFist", new[] { "idle" }),
            ("VossSeal", new[] { "idle" }),
            ("BellRope", new[] { "idle", "ring" }),
            ("CrayonSmall", new[] { "idle" }),
            ("QuillHand", new[] { "idle", "draw" }),
            ("InkPool", new[] { "idle" }),
            ("CordLance", new[] { "fly" }),
            ("BridgeSpan", new[] { "idle", "fall" }),
        };

        static readonly (string name, string[] clips)[] Families =
        {
            ("Smudge_Ember", new[] { "idle", "move", "hurt", "death" }),
            ("Smudge_Leaf", new[] { "idle", "move", "hurt", "death" }),
            ("Smudge_Chalk", new[] { "idle", "move", "hurt", "death" }),
            ("MemorySmudge", new[] { "idle", "move", "hurt", "death" }),
            ("Cantor_Crow", new[] { "idle", "move", "ring", "recover", "hurt", "death" }),
            ("ChoirDove", new[] { "idle", "ring", "hurt", "death" }),
        };

        [System.Serializable] class Manifest { public string character; public int ppu, cell; public float cellUnits, feetUnits; public Entry[] clips; }
        [System.Serializable] class Entry { public string name, file; public int fps, frames; public bool loop; }

        static Manifest Load(string name) => JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath(Art + name + "/" + name.ToLowerInvariant() + ".json")));

        static (int w, int h) PngSize(string path)
        {
            using var fs = File.OpenRead(path);
            var b = new byte[24];
            Assert.AreEqual(24, fs.Read(b, 0, 24));
            return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
        }

        static void AssertSheets((string name, string[] clips)[] set, bool modelSheet)
        {
            foreach (var (name, clips) in set)
            {
                var m = Load(name);
                Assert.AreEqual(name, m.character);
                Assert.AreEqual(96, m.ppu, name + " at 96 px per unit");
                Assert.AreEqual(Mathf.RoundToInt(m.cellUnits * 96f), m.cell, name + "'s cell is its units at the density");
                CollectionAssert.IsSubsetOf(clips, m.clips.Select(c => c.name), name + " has every clip its moves name");
                foreach (var c in m.clips)
                {
                    var path = Path.GetFullPath(Art + name + "/" + c.file);
                    Assert.IsTrue(File.Exists(path), c.file);
                    var (w, h) = PngSize(path);
                    Assert.AreEqual(c.frames * m.cell, w, name + " " + c.name + " is a strip of its frames");
                    Assert.AreEqual(m.cell, h, name + " " + c.name + " is one frame tall");
                    Assert.Greater(c.frames, 1, name + " " + c.name + " moves");
                    Assert.IsTrue(c.fps == 12 || c.fps == 24, name + " " + c.name + " at a drawn rate");
                }
                if (modelSheet)
                {
                    Assert.IsFalse(m.clips.First(c => c.name == "death").loop, name + " dies once");
                    Assert.IsFalse(m.clips.First(c => c.name == "hurt").loop, name + " is hurt once");
                    Assert.IsTrue(m.clips.First(c => c.name == "idle").loop, name + " idles");
                    Assert.IsTrue(File.Exists(Path.GetFullPath("../docs/art/" + name.ToLowerInvariant() + "-turnaround.png")), name + "'s model sheet");
                }
            }
        }

        [Test]
        public void EveryLateBossHasItsMovesOnSheets()
        {
            AssertSheets(Bodies, true);
            // The big ones are big; the Guild birds are Wardens' heights.
            var warden = Load("Warden").cellUnits;
            Assert.AreEqual(warden, Load("Hale").cellUnits, "Hale is a bird on the Warden rig");
            Assert.Greater(Load("Voss").cellUnits, warden, "the Guildmaster is a great heron");
            foreach (var guild in new[] { "Hale", "Voss" })
            {
                var m = Load(guild);
                Assert.Less(m.feetUnits, 0f, guild + " stands in rooms as an NPC too: his feet are below the collider's centre");
                Assert.Greater(m.feetUnits, -m.cellUnits * 0.5f, "and inside the cell");
            }
            foreach (var big in new[] { "Collapse", "Gatekeeper", "FallenStar", "CorrasDrawing" })
                Assert.GreaterOrEqual(Load(big).cellUnits, 4.5f, big + " fills its cell");
            Assert.AreEqual(24, Load("Voss").clips.First(c => c.name == "thrust").fps, "the lance is quick");
            Assert.AreEqual(24, Load("Gatekeeper").clips.First(c => c.name == "sweep").fps);
            Assert.AreEqual(24, Load("FallenStar").clips.First(c => c.name == "slam").fps);
            Assert.IsTrue(Load("Voss").clips.First(c => c.name == "guard").loop, "the guard walks on behind the rose");
            Assert.IsTrue(Load("Archivist").clips.First(c => c.name == "draw").loop, "the quill stays on the page");
            Assert.IsTrue(Load("Archivist").clips.First(c => c.name == "hold").loop, "and the hold holds");
            Assert.IsTrue(Load("FallenStar").clips.First(c => c.name == "burn").loop);
            var drawing = Load("CorrasDrawing");
            foreach (var c in drawing.clips.Where(c => !c.name.EndsWith("_outline")))
            {
                var o = drawing.clips.First(x => x.name == c.name + "_outline");
                Assert.AreEqual(c.frames, o.frames, c.name + " has the same frames in outline");
                Assert.AreEqual(c.fps, o.fps, c.name + " at the same rate in outline");
            }
        }

        [Test]
        public void TheFightsPiecesHaveTheirSheets()
        {
            AssertSheets(Parts, false);
            Assert.AreEqual(6, Load("BellRope").clips.First(c => c.name == "ring").frames, "the bell swings through six frames, sought by the ring's progress");
            Assert.IsFalse(Load("BellRope").clips.First(c => c.name == "ring").loop);
            Assert.IsFalse(Load("StoneStrike").clips.First(c => c.name == "erupt").loop, "the column rises once");
            Assert.IsTrue(Load("ChorusLamp").clips.First(c => c.name == "lit").loop, "a lit lamp flickers");
            Assert.GreaterOrEqual(Load("BellRope").cellUnits, 6f, "a rope is the nave's height");
            Assert.LessOrEqual(Load("ChorusLamp").cellUnits, 1f, "a lamp is small");
        }

        [Test]
        public void TheSmudgeAndCantorFamiliesShareTheirRigs()
        {
            AssertSheets(Families, true);
            float smudge = Load("Smudge").cellUnits, cantor = Load("Cantor").cellUnits;
            foreach (var s in new[] { "Smudge_Ember", "Smudge_Leaf", "Smudge_Chalk", "MemorySmudge" }) Assert.AreEqual(smudge, Load(s).cellUnits, s + " is a smudge's size");
            foreach (var c in new[] { "Cantor_Crow", "ChoirDove" }) Assert.AreEqual(cantor, Load(c).cellUnits, c + " is a Cantor's size");
            Assert.AreEqual(6, Load("ChoirDove").clips.First(c => c.name == "ring").frames, "a dove's bell rises through six frames");
        }

        static int Uses(string scene, string guid) => File.ReadAllText(Path.GetFullPath(Scenes + scene + ".unity")).Split(new[] { guid }, System.StringSplitOptions.None).Length - 1;
        static string MatGuid(string mat) => AssetDatabase.AssetPathToGUID(Materials + mat + ".mat");
        static string SheetGuid(string character, string clip) => AssetDatabase.AssetPathToGUID(Art + character + "/" + character + "_" + clip + ".png");

        [Test]
        public void TheBuiltRoomsDrawTheirBossesAndTheirPieces()
        {
            var ink = Shader.Find("OWSBG/InkSprite");
            var rooms = new[]
            {
                ("Emberdown_Hollow_4", "Collapse", "ChorusLamp", "dark"),
                ("Verdance_Aldermere_2", null, "ChoirDove", "ring"),
                ("Verdance_Gate_2", "Gatekeeper", "Feather", "idle"),
                ("Windreach_Stones_3", "Hale", "Stone", "hale"),
                ("Windreach_Star_2", "FallenStar", "StarFist", "idle"),
                ("Greyfold_Threshold_2", "Voss", "VossSeal", "idle"),
                ("Greyfold_Cathedral_2", "HalfCathedralBells", "BellRope", "ring"),
                ("Blank_Capital_2", "CorrasDrawing", "CrayonSmall", "idle"),
                ("Blank_Capital_4", "Archivist", "QuillHand", "draw"),
                ("Halden_Observatory_2", "CompleteSurvey", "InkPool", "idle"),
            };
            foreach (var (room, boss, part, clip) in rooms)
            {
                if (boss != null)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Boss_" + boss + ".mat");
                    Assert.IsNotNull(mat, "M_Boss_" + boss);
                    Assert.AreEqual(ink, mat.shader, boss);
                    Assert.AreEqual(boss + "_idle", mat.GetTexture("_BaseMap")?.name, boss + " rests on its idle strip");
                    Assert.GreaterOrEqual(Uses("Greybox_" + room, MatGuid("M_Boss_" + boss)), 1, room + " draws " + boss);
                }
                Assert.GreaterOrEqual(Uses("Greybox_" + room, SheetGuid(part, clip)), 1, room + "'s " + boss + " carries the " + part + " sheets for its pieces");
            }
            Assert.GreaterOrEqual(Uses("Greybox_Blank_Capital_4", SheetGuid("Wren", "idle")), 1, "the Archivist carries Wren's own sheets for her drawing");
            var animator = AssetDatabase.AssetPathToGUID("Assets/_Project/Code/World/EnemyAnimator.cs");
            foreach (var (room, boss, _, _) in rooms) if (boss != null) Assert.GreaterOrEqual(Uses("Greybox_" + room, animator), 1, room + "'s boss is animated");
        }

        [Test]
        public void TheRegionsSmudgesAndCantorsWearTheirLooks()
        {
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var (mat, idle) in new[] { ("M_Enemy_Smudge_Ember", "Smudge_Ember_idle"), ("M_Enemy_Smudge_Leaf", "Smudge_Leaf_idle"), ("M_Enemy_Smudge_Chalk", "Smudge_Chalk_idle"), ("M_Enemy_Cantor_Crow", "Cantor_Crow_idle") })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Materials + mat + ".mat");
                Assert.IsNotNull(m, mat);
                Assert.AreEqual(ink, m.shader, mat);
                Assert.AreEqual(idle, m.GetTexture("_BaseMap")?.name, mat + " rests on the idle strip");
            }
            Assert.GreaterOrEqual(Uses("Greybox_Emberdown_Hollow_2", MatGuid("M_Enemy_Smudge_Ember")), 2, "the mine's smudges carry the lamp");
            Assert.GreaterOrEqual(Uses("Greybox_Verdance_Road_2", MatGuid("M_Enemy_Smudge_Leaf")), 1, "the forest's carry the swing");
            Assert.GreaterOrEqual(Uses("Greybox_Halden_Mills_1", MatGuid("M_Enemy_Smudge_Chalk")), 1, "the plateau's are chalk");
            Assert.GreaterOrEqual(Uses("Greybox_Greyfold_Road_1", MatGuid("M_Enemy_Smudge_Chalk")), 2, "and so are the white's");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Ferry", MatGuid("M_Enemy_Smudge")), 2, "the coast's keep the boat");
            Assert.GreaterOrEqual(Uses("Greybox_Halden_Bridges_2", MatGuid("M_Enemy_Cantor_Crow")), 1, "the plateau's Cantor is a crow");
            Assert.GreaterOrEqual(Uses("Greybox_Verdance_Road_3", MatGuid("M_Enemy_Cantor")), 1, "the forest's is a dove");
            Assert.AreEqual(0, Uses("Greybox_Verdance_Road_3", MatGuid("M_Enemy_Cantor_Crow")));
            // The smudge of her own death rides in the persistent scene's MemoryDrops.
            var bootstrap = EditorBuildSettings.scenes[0].path;
            Assert.GreaterOrEqual(File.ReadAllText(Path.GetFullPath(bootstrap)).Split(new[] { SheetGuid("MemorySmudge", "idle") }, System.StringSplitOptions.None).Length - 1, 1, bootstrap + " carries the memory smudge's sheets");
        }
    }
}
