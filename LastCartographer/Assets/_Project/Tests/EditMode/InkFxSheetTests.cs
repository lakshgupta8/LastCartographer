using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The ink effects (ENV-12, docs/design/ink-fx.md): every clip fx.py names is packed at the game's density into one
    /// cell, one-shot at 24 fps (the survey mark alone loops), on a flat ink material in the persistent scene; the weak
    /// floor and the hidden platform stand on their own tiles; the Edge wears the wet edge.
    /// </summary>
    public class InkFxSheetTests
    {
        const string Art = "Assets/_Project/Art/Characters/Fx/";
        const string Materials = "Assets/_Project/Art/Materials/";
        const string Scenes = "Assets/_Project/Scenes/";

        static readonly string[] OneShots = { "splash", "slash", "crosshatch", "longstroke", "blot", "redraw", "eraser", "crumble", "erupt" };

        [System.Serializable] class Manifest { public string character; public int ppu, cell; public float cellUnits; public Entry[] clips; }
        [System.Serializable] class Entry { public string name, file; public int fps, frames; public bool loop; }

        static Manifest Load() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath(Art + "fx.json")));

        static (int w, int h) PngSize(string path)
        {
            using var fs = File.OpenRead(path);
            var b = new byte[24];
            Assert.AreEqual(24, fs.Read(b, 0, 24));
            return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
        }

        static int Uses(string scene, string mat) => File.ReadAllText(Path.GetFullPath(Scenes + scene + ".unity"))
            .Split(new[] { AssetDatabase.AssetPathToGUID(Materials + mat + ".mat") }, System.StringSplitOptions.None).Length - 1;

        [Test]
        public void EveryEffectIsPackedIntoOneCellAtTheGamesDensity()
        {
            var m = Load();
            Assert.AreEqual("Fx", m.character);
            Assert.AreEqual(96, m.ppu);
            Assert.AreEqual(3f, m.cellUnits, 0.001f, "every effect renders into the same three-unit cell");
            CollectionAssert.IsSubsetOf(OneShots.Concat(new[] { "mark" }), m.clips.Select(c => c.name));
            foreach (var c in m.clips)
            {
                var (w, h) = PngSize(Path.GetFullPath(Art + c.file));
                Assert.AreEqual(c.frames * m.cell, w, c.name + " is a strip of its frames");
                Assert.AreEqual(m.cell, h, c.name);
                Assert.AreEqual(24, c.fps, c.name + ": ink moves fast");
                Assert.AreEqual(c.name == "mark", c.loop, c.name + (c.name == "mark" ? " stays on the floor" : " plays once and goes"));
                Assert.GreaterOrEqual(c.frames, 2, c.name);
            }
            Assert.AreEqual(8, m.clips.First(c => c.name == "redraw").frames, "the Bind's outline takes the longest to trace");
        }

        [Test]
        public void ThePersistentSceneCarriesTheEffectsOnAFlatInkMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Fx.mat");
            Assert.IsNotNull(mat, "M_Fx");
            Assert.AreEqual(Shader.Find("OWSBG/InkSprite"), mat.shader);
            Assert.AreEqual(0f, mat.GetFloat("_Lighting"), "ink on the page is not lit");
            Assert.AreEqual(0f, mat.GetFloat("_Shadows"));
            Assert.AreEqual(0f, mat.GetFloat("_GrainStrength"), "no grain over an effect");
            var fx = AssetDatabase.AssetPathToGUID("Assets/_Project/Code/World/InkFx.cs");
            Assert.GreaterOrEqual(File.ReadAllText(Path.GetFullPath(Scenes + "Persistent/Persistent.unity")).Split(new[] { fx }, System.StringSplitOptions.None).Length - 1, 1, "InkFx in the persistent scene");
            var wrenFx = AssetDatabase.AssetPathToGUID("Assets/_Project/Code/World/WrenFx.cs");
            Assert.GreaterOrEqual(File.ReadAllText(Path.GetFullPath(Scenes + "Persistent/Persistent.unity")).Split(new[] { wrenFx }, System.StringSplitOptions.None).Length - 1, 1, "Wren carries her own ink");
        }

        [Test]
        public void TheWeakFloorTheHiddenPlatformAndTheEdgeWearTheirDrawings()
        {
            var ink = Shader.Find("OWSBG/InkSprite");
            foreach (var tile in new[] { "M_Ground_Boardwalk_Weak", "M_Ground_Boardwalk_Hidden" })
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + tile + ".mat");
                Assert.IsNotNull(mat, tile);
                Assert.AreEqual(ink, mat.shader, tile);
                Assert.AreEqual(1f, mat.GetFloat("_WorldUV"), tile + " tiles in world space like the planks");
            }
            Assert.GreaterOrEqual(Uses("Greybox/Greybox_Saltmarrow_A", "M_Ground_Boardwalk_Weak"), 1, "the quay's weak floor is rotten planks");
            Assert.GreaterOrEqual(Uses("Greybox/Greybox_Saltmarrow_A", "M_Ground_Boardwalk_Hidden"), 1, "the hidden platform is lantern-drawn planks");
            Assert.AreEqual(0, Uses("Greybox/Greybox_Saltmarrow_A", "M_Greybox_WeakFloor"), "no greybox weak floor left");
            Assert.AreEqual(0, Uses("Greybox/Greybox_Saltmarrow_A", "M_Greybox_Hidden"));
            Assert.GreaterOrEqual(Uses("Greybox/Greybox_Greyfold_Edge", "M_Prop_WetEdge"), 1, "the wet paper before the white");
        }
    }
}
