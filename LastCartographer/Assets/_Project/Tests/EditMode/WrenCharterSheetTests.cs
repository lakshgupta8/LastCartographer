using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// Wren's Charter silhouettes (CHR-05, combat doc 5, docs/design/wren-animation.md §8): each Charter but the
    /// Surveyor has her every clip drawn again in its own cowl and grip, frame for frame with the base sheets; the
    /// six read apart at rest; the persistent scene carries every set; the model sheets are in the docs.
    /// </summary>
    public class WrenCharterSheetTests
    {
        const string Art = "Assets/_Project/Art/Characters/";

        [System.Serializable] class Manifest { public string character; public int ppu, cell; public Entry[] clips; }
        [System.Serializable] class Entry { public string name, file; public int fps, frames; public bool loop; }

        static string FolderOf(CharterKind kind) => Art + (CharterSet.SheetSetOf(kind) is string set ? "Wren_" + set : "Wren") + "/";

        static Manifest Load(CharterKind kind)
        {
            var folder = FolderOf(kind);
            var name = folder.TrimEnd('/').Substring(Art.Length);
            var path = Path.GetFullPath(folder + name.ToLowerInvariant() + ".json");
            Assert.IsTrue(File.Exists(path), kind + "'s manifest");
            return JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        }

        static CharterKind[] Variants => System.Enum.GetValues(typeof(CharterKind)).Cast<CharterKind>().Where(k => k != CharterKind.Surveyor).ToArray();

        [Test]
        public void EveryCharterButTheSurveyorIsASetOfItsOwn()
        {
            Assert.IsNull(CharterSet.SheetSetOf(CharterKind.Surveyor), "the Surveyor wears the base sheets");
            Assert.AreEqual(5, Variants.Length);
            foreach (var k in Variants) Assert.AreEqual(k.ToString(), CharterSet.SheetSetOf(k));
        }

        [Test]
        public void EachCharterDrawsEveryClipFrameForFrame()
        {
            var b = Load(CharterKind.Surveyor);
            foreach (var k in Variants)
            {
                var m = Load(k);
                Assert.AreEqual("Wren_" + k, m.character);
                Assert.AreEqual(b.ppu, m.ppu, k + "'s density");
                Assert.AreEqual(b.cell, m.cell, k + "'s cell");
                CollectionAssert.AreEquivalent(b.clips.Select(c => c.name), m.clips.Select(c => c.name), k + " has every clip and no other");
                foreach (var c in m.clips)
                {
                    var o = b.clips.First(x => x.name == c.name);
                    Assert.AreEqual(o.frames, c.frames, k + " " + c.name + " frames");
                    Assert.AreEqual(o.fps, c.fps, k + " " + c.name + " rate");
                    Assert.AreEqual(o.loop, c.loop, k + " " + c.name + " loop");
                    Assert.AreEqual("Wren_" + k + "_" + c.name + ".png", c.file);
                    var tex = Decode(FolderOf(k) + c.file);
                    Assert.AreEqual(c.frames * m.cell, tex.width, k + " " + c.name + " is a strip of its frames");
                    Assert.AreEqual(m.cell, tex.height);
                    Object.DestroyImmediate(tex);
                }
                var importer = AssetImporter.GetAtPath(FolderOf(k) + "Wren_" + k + "_idle.png") as TextureImporter;
                Assert.IsNotNull(importer, k + "'s idle importer");
                Assert.IsFalse(importer.mipmapEnabled);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, "the ink line stays a line");
            }
        }

        [Test]
        public void TheSixReadApartAtRest()
        {
            // The first idle frame's outline (alpha over half) for each Charter: every pair differs over at least a
            // tenth of the pixels either covers, so the cowl and the grip change the silhouette, not only its colour.
            var kinds = System.Enum.GetValues(typeof(CharterKind)).Cast<CharterKind>().ToArray();
            var masks = kinds.ToDictionary(k => k, k => IdleMask(k, out _));
            for (int i = 0; i < kinds.Length; i++)
            for (int j = i + 1; j < kinds.Length; j++)
            {
                bool[] a = masks[kinds[i]], b = masks[kinds[j]];
                int either = 0, differ = 0;
                for (int p = 0; p < a.Length; p++)
                {
                    if (a[p] || b[p]) either++;
                    if (a[p] != b[p]) differ++;
                }
                Assert.Greater(either, 0);
                Assert.Greater(differ / (float)either, 0.10f, kinds[i] + " and " + kinds[j] + " share an outline");
            }
        }

        [Test]
        public void ThePersistentWrenCarriesEverySetAndTheDocsHaveTheModelSheets()
        {
            var scene = File.ReadAllText(Path.GetFullPath("Assets/_Project/Scenes/Persistent/Persistent.unity"));
            foreach (var k in Variants)
            {
                StringAssert.Contains("Name: " + k, scene, k + "'s set is named on the sheet player");
                foreach (var c in Load(k).clips)
                    StringAssert.Contains(AssetDatabase.AssetPathToGUID(FolderOf(k) + c.file), scene, k + " " + c.name + " is wired");
                Assert.IsTrue(File.Exists(Path.GetFullPath("../docs/art/wren_" + k.ToString().ToLowerInvariant() + "-turnaround.png")), k + "'s model sheet");
            }
            Assert.IsTrue(File.Exists(Path.GetFullPath("../docs/art/wren-charters.png")), "the six side by side");
        }

        static Texture2D Decode(string assetPath)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(Path.GetFullPath(assetPath))), assetPath);
            return tex;
        }

        static bool[] IdleMask(CharterKind kind, out int cell)
        {
            var m = Load(kind);
            cell = m.cell;
            var tex = Decode(FolderOf(kind) + m.clips.First(c => c.name == "idle").file);
            var px = tex.GetPixels32();
            var mask = new bool[cell * cell];
            for (int y = 0; y < cell; y++)
            for (int x = 0; x < cell; x++)
                mask[y * cell + x] = px[y * tex.width + x].a > 127;
            Object.DestroyImmediate(tex);
            return mask;
        }
    }
}
