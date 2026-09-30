using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The paper kit (ENV-01, ENV-02, docs/design/paper-kit.md): every backdrop strip and ground tile the coast's
    /// sixteen rooms use comes off the Saltmarrow kit at the size its quads expect, every layer is on the ink shader
    /// so the place's fade thins it, the ground tiles in world space, and each room stands on the tile it should.
    /// </summary>
    public class PaperKitTests
    {
        const string Kit = "Assets/_Project/Art/Environment/Saltmarrow/";
        const string Materials = "Assets/_Project/Art/Materials/";
        static readonly string[] Backdrop =
        {
            "Fore_Reeds", "Mid_Reeds", "Far_Roosts", "Farther_Cliffs", "Mid_Reeds_Faded", "Far_Roosts_Faded", "Farther_Cliffs_Faded",
            "Mid_Salt", "Far_Chapel", "Far_Tower", "Farther_Sea",
        };
        static readonly string[] Tiles = { "Ground_Boardwalk", "Ground_Boardwalk_Faded", "Ground_Shallows", "Ground_Stone" };
        const string Scenes = "Assets/_Project/Scenes/Greybox/";

        [System.Serializable] class Manifest { public string region; public int ppu; public int tilePpu; public Layer[] layers; }
        [System.Serializable] class Layer { public string name, kind, file; public float widthUnits, heightUnits; public int ppu, widthPx, heightPx; public bool faded; }

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
            CollectionAssert.AreEquivalent(Backdrop.Select(n => "Paper_" + n).Concat(Tiles), kit.layers.Select(l => l.name));
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
            foreach (var n in new[] { "Mid_Reeds", "Mid_Reeds_Faded", "Mid_Salt" }) Assert.AreEqual(6f, by["Paper_" + n].heightUnits, 0.001f, n);
            foreach (var n in new[] { "Far_Roosts", "Far_Roosts_Faded" }) Assert.AreEqual(10f, by["Paper_" + n].heightUnits, 0.001f, n);
            foreach (var n in new[] { "Far_Chapel", "Far_Tower" }) Assert.AreEqual(14f, by["Paper_" + n].heightUnits, 0.001f, n);
            foreach (var n in new[] { "Farther_Cliffs", "Farther_Cliffs_Faded", "Farther_Sea" }) Assert.AreEqual(16f, by["Paper_" + n].heightUnits, 0.001f, n);
            foreach (var n in Tiles) { Assert.AreEqual(4f, by[n].widthUnits, n); Assert.AreEqual(1f, by[n].heightUnits, n); }
            Assert.IsTrue(by["Paper_Mid_Reeds_Faded"].faded && !by["Paper_Mid_Reeds"].faded, "the manifest says which layers are the faded third's");
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
            foreach (var tile in Tiles)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "M_" + tile + ".mat");
                Assert.IsNotNull(mat, tile);
                Assert.AreEqual(ink, mat.shader, tile);
                Assert.AreEqual(tile, mat.GetTexture("_BaseMap")?.name, tile + " has its drawing");
                Assert.AreEqual(1f, mat.GetFloat("_WorldUV"), tile + " tiles in world space");
                Assert.AreEqual(1f, mat.GetFloat("_Shadows"), tile + " takes Wren's shadow");
                Assert.AreEqual(0.25f, mat.GetTextureScale("_BaseMap").x, 0.001f, "one tile every four units");
                Assert.AreEqual(0.7f, mat.GetFloat("_Lighting"), 0.001f, "the ground takes more of the light than a backdrop");
            }
            int Uses(string scene, string mat) => File.ReadAllText(Path.GetFullPath(Scenes + scene + ".unity"))
                .Split(new[] { AssetDatabase.AssetPathToGUID(Materials + mat + ".mat") }, System.StringSplitOptions.None).Length - 1;
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_A", "M_Ground_Boardwalk"), 5, "the quay: walkway, three platforms, the stilt");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_B", "M_Ground_Boardwalk"), 6, "Merrow's End: floor, steps, the shaft");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Boardwalk", "M_Ground_Shallows"), 2, "the boardwalk's two gaps hold the tide");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Chain_3", "M_Ground_Boardwalk_Faded"), 1, "the faded third's planks are paler");
            Assert.AreEqual(0, Uses("Greybox_Saltmarrow_Chain_3", "M_Ground_Boardwalk"), "and none of the drawn ones");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Lighthouse", "M_Ground_Stone"), 4, "the fourth lighthouse is stone");
            Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_Chapel", "M_Ground_Stone"), 4, "the chapel is stone");
            foreach (var r in new[] { "Shore", "Stilts", "Tetherline", "Ferry", "Chain_1", "Chain_2", "Roots_1", "Roots_2", "Roots_3", "Roots_4" })
                Assert.GreaterOrEqual(Uses("Greybox_Saltmarrow_" + r, "M_Ground_Boardwalk"), 1, r + " walks on planks");
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
