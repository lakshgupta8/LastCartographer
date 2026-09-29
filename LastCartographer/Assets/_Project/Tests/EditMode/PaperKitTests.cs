using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The paper-kit pipeline (ENV-01, docs/design/paper-kit.md): the Quay's backdrop comes off the Saltmarrow kit
    /// at the size its quads expect, every layer is on the ink shader so the place's fade thins it, and the
    /// walkway's planks tile in world space.
    /// </summary>
    public class PaperKitTests
    {
        const string Kit = "Assets/_Project/Art/Environment/Saltmarrow/";
        const string Materials = "Assets/_Project/Art/Materials/";
        static readonly string[] Backdrop = { "Fore_Reeds", "Mid_Reeds", "Far_Roosts", "Farther_Cliffs" };

        [System.Serializable] class Manifest { public string region; public int ppu; public int tilePpu; public Layer[] layers; }
        [System.Serializable] class Layer { public string name, kind, file; public float widthUnits, heightUnits; public int ppu, widthPx, heightPx; }

        static Manifest Load() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath(Kit + "kit.json")));

        static (int w, int h) PngSize(string path)
        {
            using var fs = File.OpenRead(path);
            var b = new byte[24];
            Assert.AreEqual(24, fs.Read(b, 0, 24), "png header");
            int w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
            int h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
            return (w, h);
        }

        [Test]
        public void TheSaltmarrowKitIsOnDiskAtTheSizeItsQuadsExpect()
        {
            var kit = Load();
            Assert.AreEqual("Saltmarrow", kit.region);
            CollectionAssert.AreEquivalent(Backdrop.Select(n => "Paper_" + n).Append("Ground_Boardwalk"), kit.layers.Select(l => l.name));
            foreach (var l in kit.layers)
            {
                var path = Path.GetFullPath(Kit + l.file);
                Assert.IsTrue(File.Exists(path), l.file);
                var (w, h) = PngSize(path);
                Assert.AreEqual(Mathf.RoundToInt(l.widthUnits * l.ppu), w, l.name + " width");
                Assert.AreEqual(Mathf.RoundToInt(l.heightUnits * l.ppu), h, l.name + " height");
                Assert.AreEqual(l.widthPx, w, l.name + " manifest width");
                Assert.AreEqual(l.heightPx, h, l.name + " manifest height");
                Assert.AreEqual(0, w % 4, l.name + " compresses"); Assert.AreEqual(0, h % 4, l.name + " compresses");
                Assert.AreEqual(l.kind == "strip" ? kit.ppu : kit.tilePpu, l.ppu, l.name + " density");
            }
            // The strips are cut for the quads MakePaperLayer makes: 80 wide, at the greybox's heights.
            var by = kit.layers.ToDictionary(l => l.name);
            foreach (var n in Backdrop) Assert.AreEqual(80f, by["Paper_" + n].widthUnits, n + " spans the quad");
            Assert.AreEqual(1.6f, by["Paper_Fore_Reeds"].heightUnits, 0.001f);
            Assert.AreEqual(6f, by["Paper_Mid_Reeds"].heightUnits, 0.001f);
            Assert.AreEqual(10f, by["Paper_Far_Roosts"].heightUnits, 0.001f);
            Assert.AreEqual(16f, by["Paper_Farther_Cliffs"].heightUnits, 0.001f);
        }

        [Test]
        public void TheQuaysLayersAreInkedFromTheKit()
        {
            var ink = Shader.Find("OWSBG/InkSprite");
            Assert.IsNotNull(ink);
            foreach (var layer in Backdrop)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Paper_" + layer + ".mat");
                Assert.IsNotNull(mat, layer);
                Assert.AreEqual(ink, mat.shader, layer + " on the ink shader");
                var tex = mat.GetTexture("_BaseMap");
                Assert.IsNotNull(tex, layer + " has its drawing");
                Assert.AreEqual("Paper_" + layer, tex.name);
                Assert.AreEqual(0f, mat.GetFloat("_WorldUV"), layer + " is a strip, not a tile");
                Assert.AreEqual(0f, mat.GetFloat("_Shadows"), layer + " takes no shadow");
                Assert.AreEqual(0.3f, mat.GetFloat("_Lighting"), 0.001f, layer + " keeps its wash under the sun");
            }
            var planks = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_Ground_Boardwalk.mat");
            Assert.IsNotNull(planks, "the boardwalk tile");
            Assert.AreEqual(ink, planks.shader);
            Assert.AreEqual(1f, planks.GetFloat("_WorldUV"), "planks run in world space");
            Assert.AreEqual(1f, planks.GetFloat("_Shadows"), "the walkway takes Wren's shadow");
            Assert.AreEqual(0.25f, planks.GetTextureScale("_BaseMap").x, 0.001f, "one tile every four units");

            Assert.AreEqual(0.7f, planks.GetFloat("_Lighting"), 0.001f, "the ground takes more of the light than a backdrop");
            var scene = File.ReadAllText(Path.GetFullPath("Assets/_Project/Scenes/Greybox/Greybox_Saltmarrow_A.unity"));
            var guid = AssetDatabase.AssetPathToGUID(Materials + "M_Ground_Boardwalk.mat");
            Assert.GreaterOrEqual(scene.Split(new[] { guid }, System.StringSplitOptions.None).Length - 1, 5,
                "the walkway, three platforms and the stilt are on the planks");
        }

        [Test]
        public void TheImporterClampsStripsAndRepeatsTiles()
        {
            var strip = AssetImporter.GetAtPath(Kit + "Paper_Mid_Reeds.png") as TextureImporter;
            var tile = AssetImporter.GetAtPath(Kit + "Ground_Boardwalk.png") as TextureImporter;
            Assert.IsNotNull(strip, "strip importer");
            Assert.IsNotNull(tile, "tile importer");
            Assert.AreEqual(TextureWrapMode.Clamp, strip.wrapMode, "a strip maps once");
            Assert.AreEqual(TextureWrapMode.Repeat, tile.wrapMode, "a tile repeats");
            Assert.IsTrue(strip.alphaIsTransparency && tile.alphaIsTransparency, "straight alpha");
            Assert.IsTrue(strip.mipmapEnabled && tile.mipmapEnabled, "mipmapped");
            Assert.AreEqual(TextureImporterNPOTScale.None, strip.npotScale, "strips keep their size");
        }
    }
}
