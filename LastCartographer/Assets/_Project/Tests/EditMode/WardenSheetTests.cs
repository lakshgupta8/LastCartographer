using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The Warden family drawn (CHR-07, docs/design/enemy-animation.md §2a): Halvard, Brann, Oriel and the two more
    /// patrol looks have their sheets with every clip their moves name; Halvard's manifest knows where his feet are so
    /// the lighthouse stands him on the floor; the Chapel's Halvard and the lighthouse's are on his idle strip; the
    /// anchored towns' patrols wear the three looks.
    /// </summary>
    public class WardenSheetTests
    {
        const string Art = "Assets/_Project/Art/Characters/";
        const string Materials = "Assets/_Project/Art/Materials/";
        const string Scenes = "Assets/_Project/Scenes/Greybox/";

        static readonly (string name, string[] clips)[] Family =
        {
            ("Halvard", new[] { "idle", "move", "walk", "talk", "measure", "telegraph", "thrust", "lunge", "survey", "call", "count", "recover", "hurt", "death" }),
            ("Brann", new[] { "idle", "move", "telegraph", "thrust", "charge", "crosscut", "hold", "recover", "hurt", "death" }),
            ("Oriel", new[] { "idle", "move", "telegraph", "strike1", "strike2", "strike3", "flourish", "step", "bind", "recover", "hurt", "death" }),
            ("Warden_B", new[] { "idle", "move", "measure", "telegraph", "thrust", "recover", "hurt", "death" }),
            ("Warden_C", new[] { "idle", "move", "measure", "telegraph", "thrust", "recover", "hurt", "death" }),
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

        [Test]
        public void EveryWardenHasItsMovesOnSheets()
        {
            var warden = Load("Warden");
            foreach (var (name, clips) in Family)
            {
                var m = Load(name);
                Assert.AreEqual(name, m.character);
                Assert.AreEqual(96, m.ppu, name + " at 96 px per unit");
                Assert.AreEqual(warden.cellUnits, m.cellUnits, name + " is a Warden's height: the family shares the cell");
                CollectionAssert.IsSubsetOf(clips, m.clips.Select(c => c.name), name + " has every clip its moves name");
                foreach (var c in m.clips)
                {
                    var (w, h) = PngSize(Path.GetFullPath(Art + name + "/" + c.file));
                    Assert.AreEqual(c.frames * m.cell, w, name + " " + c.name + " is a strip of its frames");
                    Assert.AreEqual(m.cell, h, name + " " + c.name);
                    Assert.IsTrue(c.fps == 12 || c.fps == 24, name + " " + c.name + " at a drawn rate");
                }
                Assert.IsTrue(File.Exists(Path.GetFullPath("../docs/art/" + name.ToLowerInvariant() + "-turnaround.png")), name + "'s model sheet");
            }
            // The strikes are quick: 24 fps, as Wren's are.
            Assert.AreEqual(24, Load("Halvard").clips.First(c => c.name == "lunge").fps);
            Assert.AreEqual(24, Load("Halvard").clips.First(c => c.name == "count").fps);
            Assert.AreEqual(24, Load("Brann").clips.First(c => c.name == "crosscut").fps);
            Assert.AreEqual(24, Load("Oriel").clips.First(c => c.name == "strike1").fps);
            Assert.IsTrue(Load("Oriel").clips.First(c => c.name == "bind").loop, "the Bind holds until denied or done");
            Assert.IsTrue(Load("Brann").clips.First(c => c.name == "hold").loop, "the hold walks on");
        }

        [Test]
        public void HalvardKnowsWhereHisFeetAre()
        {
            var m = Load("Halvard");
            Assert.Less(m.feetUnits, 0f, "the feet are below the collider's centre");
            Assert.Greater(m.feetUnits, -m.cellUnits * 0.5f, "and inside the cell");
            Assert.AreEqual(0f, Load("Warden").feetUnits, "an enemy drawn on its collider says nothing");
        }

        [Test]
        public void TheCoastStandsItsWardens()
        {
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var (mat, idle) in new[] { ("M_Npc_Halvard", "Halvard_idle"), ("M_Boss_Halvard", "Halvard_idle"), ("M_Enemy_Warden", "Warden_idle"), ("M_Enemy_Warden_B", "Warden_B_idle"), ("M_Enemy_Warden_C", "Warden_C_idle") })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(Materials + mat + ".mat");
                Assert.IsNotNull(m, mat);
                Assert.AreEqual(ink, m.shader, mat);
                Assert.AreEqual(idle, m.GetTexture("_BaseMap")?.name, mat + " rests on the idle strip");
            }
            int Uses(string scene, string mat) => File.ReadAllText(Path.GetFullPath(Scenes + scene + ".unity"))
                .Split(new[] { AssetDatabase.AssetPathToGUID(Materials + mat + ".mat") }, System.StringSplitOptions.None).Length - 1;
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Chapel", "M_Boss_Halvard"), 1, "the Chapel's Halvard");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Lighthouse", "M_Npc_Halvard"), 1, "the lighthouse's Halvard");
            int looks = 0;
            foreach (var look in new[] { "M_Enemy_Warden", "M_Enemy_Warden_B", "M_Enemy_Warden_C" })
                if (Uses("Greybox_Saltmarrow_A", look) + Uses("Greybox_Saltmarrow_B", look) > 0) looks++;
            Assert.AreEqual(3, looks, "the two hubs' four patrols wear all three looks between them");
        }
    }
}
