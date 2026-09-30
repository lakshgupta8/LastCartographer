using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The coast's creatures (CHR-06, docs/design/enemy-animation.md): each of the six enemies and the Lamp-Keeper
    /// has its sheets packed at the game's density with the clips its family's moves ask for, a model sheet in the
    /// docs, its material resting on the idle strip, and the rooms carry the animator.
    /// </summary>
    public class EnemySheetTests
    {
        const string Art = "Assets/_Project/Art/Characters/";
        const string Materials = "Assets/_Project/Art/Materials/";
        const string Scenes = "Assets/_Project/Scenes/Greybox/";

        // creature, its material, the clips its Enemy.Clip can name
        static readonly (string name, string mat, string[] clips)[] Creatures =
        {
            ("MarshCrab", "M_Enemy_MarshCrab", new[] { "idle", "move", "hop", "hurt", "death" }),
            ("ReedSkimmer", "M_Enemy_ReedSkimmer", new[] { "idle", "move", "rise", "dive", "hurt", "death" }),
            ("Smudge", "M_Enemy_Smudge", new[] { "idle", "move", "hurt", "death" }),
            ("Cantor", "M_Enemy_Cantor", new[] { "idle", "move", "ring", "recover", "hurt", "death" }),
            ("Warden", "M_Enemy_Warden", new[] { "idle", "move", "measure", "telegraph", "thrust", "recover", "hurt", "death" }),
            ("LostRemnant", null, new[] { "idle", "move", "hurt", "death" }),
            ("LampKeeper", "M_Boss_LampKeeper", new[] { "idle", "telegraph", "beam", "dive", "grounded", "return", "hurt", "death" }),
        };

        [System.Serializable] class Manifest { public string character; public int ppu, cell; public float cellUnits; public Entry[] clips; }
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
        public void EveryCreatureHasItsClipsPackedAtTheGamesDensity()
        {
            foreach (var (name, _, clips) in Creatures)
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
                Assert.IsFalse(m.clips.First(c => c.name == "death").loop, name + " dies once");
                Assert.IsFalse(m.clips.First(c => c.name == "hurt").loop, name + " is hurt once");
                Assert.IsTrue(m.clips.First(c => c.name == "idle").loop, name + " idles");
                Assert.IsTrue(File.Exists(Path.GetFullPath("../docs/art/" + name.ToLowerInvariant() + "-turnaround.png")), name + "'s model sheet");
            }
        }

        [Test]
        public void TheFamiliesKeepTheirSilhouettesSizes()
        {
            // Wardens are tall (a lance's reach); the Lamp-Keeper opens her shutters wide; the rest are rounds.
            Assert.GreaterOrEqual(Load("Warden").cellUnits, 2.5f);
            Assert.GreaterOrEqual(Load("LampKeeper").cellUnits, 3f);
            Assert.LessOrEqual(Load("MarshCrab").cellUnits, 1.6f);
            Assert.LessOrEqual(Load("ReedSkimmer").cellUnits, 1.6f);
            var ring = Load("Cantor").clips.First(c => c.name == "ring");
            Assert.AreEqual(6, ring.frames, "the bell rises through six frames, sought by the ring's progress");
        }

        [Test]
        public void TheRoomsDrawTheirEnemiesFromTheSheets()
        {
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var (name, matName, _) in Creatures)
            {
                if (matName == null) continue;   // the Remnant is not placed by the greybox yet
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + matName + ".mat");
                Assert.IsNotNull(mat, matName);
                Assert.AreEqual(ink, mat.shader, matName);
                Assert.AreEqual(name + "_idle", mat.GetTexture("_BaseMap")?.name, matName + " rests on the idle strip");
            }
            var animator = AssetDatabase.AssetPathToGUID("Assets/_Project/Code/World/EnemyAnimator.cs");
            int Uses(string scene) => File.ReadAllText(Path.GetFullPath(Scenes + scene + ".unity")).Split(new[] { animator }, System.StringSplitOptions.None).Length - 1;
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A"), 3, "the quay's crab, skimmer and smudge are drawn");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_B"), 1, "Merrow's End's Cantor is drawn");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Lighthouse"), 1, "the Lamp-Keeper is drawn");
        }
    }
}
