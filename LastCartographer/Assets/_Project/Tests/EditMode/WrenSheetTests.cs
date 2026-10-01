using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// Wren's sheets (CHR-02, CHR-03, docs/design/wren-animation.md): every clip the animator asks for is packed
    /// at the game's density, locomotion at 12 fps and the quill's moves at 24 (art-direction 4), the model sheet
    /// is in the docs, her material shows the idle sheet, and the persistent scene carries the animator.
    /// </summary>
    public class WrenSheetTests
    {
        const string Folder = "Assets/_Project/Art/Characters/Wren/";
        public static readonly string[] Required =
        {
            "idle", "run", "jump", "fall", "glide", "land", "cling", "dash", "thread",
            "strike1", "strike2", "strike3", "strike_up", "pogo", "bind", "survey", "hurt", "death",
            // CHR-04: the abilities' own frames and the three flourishes
            "glide_rise", "slide", "walljump", "thread_cast", "thread_catch", "crosshatch", "longstroke", "blot",
        };
        static readonly string[] Fast = { "dash", "thread", "strike1", "strike2", "strike3", "strike_up", "pogo", "walljump", "thread_cast", "thread_catch", "crosshatch", "longstroke", "blot" };

        [System.Serializable] class Manifest { public string character; public int ppu, cell; public Entry[] clips; }
        [System.Serializable] class Entry { public string name, file; public int fps, frames; public bool loop; }

        static Manifest Load() => JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath(Folder + "wren.json")));

        static (int w, int h) PngSize(string path)
        {
            using var fs = File.OpenRead(path);
            var b = new byte[24];
            Assert.AreEqual(24, fs.Read(b, 0, 24));
            return ((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
        }

        [Test]
        public void EveryClipIsPackedAtTheGamesDensity()
        {
            var m = Load();
            Assert.AreEqual("Wren", m.character);
            Assert.AreEqual(96, m.ppu, "96 px per unit (art-direction 4)");
            Assert.AreEqual(192, m.cell, "a two-unit frame: room for the quill and the jumps");
            CollectionAssert.IsSubsetOf(Required, m.clips.Select(c => c.name), "every clip the animator plays");
            foreach (var c in m.clips)
            {
                var path = Path.GetFullPath(Folder + c.file);
                Assert.IsTrue(File.Exists(path), c.file);
                var (w, h) = PngSize(path);
                Assert.AreEqual(c.frames * m.cell, w, c.name + " is a strip of its frames");
                Assert.AreEqual(m.cell, h, c.name + " is one frame tall");
                Assert.AreEqual(Fast.Contains(c.name) ? 24 : 12, c.fps, c.name + "'s rate");
                Assert.Greater(c.frames, 1, c.name + " moves");
            }
            foreach (var one in new[] { "jump", "land", "dash", "strike1", "strike2", "strike3", "strike_up", "pogo", "hurt", "death", "walljump", "thread_cast", "thread_catch", "crosshatch", "longstroke", "blot" })
                Assert.IsFalse(m.clips.First(c => c.name == one).loop, one + " plays once");
            foreach (var loop in new[] { "idle", "run", "fall", "glide", "glide_rise", "bind", "survey", "cling", "slide", "thread" })
                Assert.IsTrue(m.clips.First(c => c.name == loop).loop, loop + " loops");
        }

        [Test]
        public void TheAbilitiesAndTheFlourishesHaveTheirOwnFrames()
        {
            // CHR-04: a flourish's drawn frames stand for its game frames (crosshatch 6 hits at 3 apart plus 6 of
            // recovery = 24 → 12 drawn; longstroke 4 + 6 → 6; blot 3 + 6 → 6), the dash 8 game frames → 4 at 24 fps.
            var m = Load();
            int Frames(string clip) => m.clips.First(c => c.name == clip).frames;
            Assert.AreEqual(12, Frames("crosshatch"));
            Assert.AreEqual(6, Frames("longstroke"));
            Assert.AreEqual(6, Frames("blot"));
            Assert.AreEqual(4, Frames("dash"), "the Wingbeat");
            Assert.GreaterOrEqual(Frames("cling"), 4, "the Talonhold breathes");
            Assert.AreEqual(3, Frames("slide"));
            Assert.AreEqual(3, Frames("walljump"));
            Assert.AreEqual(2, Frames("thread_cast"));
            Assert.AreEqual(4, Frames("thread"), "the pull");
            Assert.AreEqual(3, Frames("thread_catch"));
            Assert.AreEqual(6, Frames("glide"), "the Windmemory");
            Assert.AreEqual(4, Frames("glide_rise"));
        }

        [Test]
        public void TheSwingsHaveAFrameForEachPhase()
        {
            // 3 startup, 4 active, 8 recovery game frames (combat doc 1) at 24 fps come to six drawn frames.
            var m = Load();
            foreach (var s in new[] { "strike1", "strike2", "strike3", "strike_up" })
                Assert.AreEqual(6, m.clips.First(c => c.name == s).frames, s);
            Assert.AreEqual(4, m.clips.First(c => c.name == "pogo").frames, "the down-strike");
        }

        [Test]
        public void TheModelSheetAndTheImporterAreInPlace()
        {
            Assert.IsTrue(File.Exists(Path.GetFullPath("../docs/art/wren-turnaround.png")), "the turnaround is the model sheet (CHR-02)");
            var importer = AssetImporter.GetAtPath(Folder + "Wren_idle.png") as TextureImporter;
            Assert.IsNotNull(importer, "idle sheet importer");
            Assert.IsFalse(importer.mipmapEnabled, "a sheet is read one frame at a time");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, "the ink line stays a line");
        }

        [Test]
        public void WrenWearsHerSheetsInThePersistentScene()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Wren_Ink.mat");
            Assert.IsNotNull(mat);
            Assert.AreEqual("Wren_idle", mat.GetTexture("_BaseMap")?.name, "her material rests on the idle sheet");
            var scene = File.ReadAllText(Path.GetFullPath("Assets/_Project/Scenes/Persistent/Persistent.unity"));
            foreach (var script in new[] { "InkSheetPlayer", "WrenAnimator" })
                StringAssert.Contains(AssetDatabase.AssetPathToGUID("Assets/_Project/Code/World/" + script + ".cs"), scene, script + " is on Wren");
            var m = Load();
            foreach (var c in m.clips)
                StringAssert.Contains(AssetDatabase.AssetPathToGUID(Folder + c.file), scene, c.name + "'s sheet is wired");
        }
    }
}
