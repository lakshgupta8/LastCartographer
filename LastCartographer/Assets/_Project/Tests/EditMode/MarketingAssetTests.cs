using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The store's assets (ENV-13, docs/design/marketing-assets.md): every capsule at the size Steam asks for, the
    /// logo on transparency; every screenshot the list names, from a room that exists, at 1920x1080 and nothing
    /// unlisted beside them; every trailer clip encoded; the title's font carrying its licence.
    /// </summary>
    public class MarketingAssetTests
    {
        static string Docs(string relative) => Path.GetFullPath(Path.Combine("..", "docs", "marketing", relative));

        static Texture2D Decode(string path)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), path);
            return tex;
        }

        [Test]
        public void EveryCapsuleIsTheSizeSteamAsksFor()
        {
            Assert.AreEqual(9, Marketing.Capsules.Count);
            foreach (var (file, w, h, _) in Marketing.Capsules)
            {
                var path = Docs("capsules/" + file);
                Assert.IsTrue(File.Exists(path), file);
                var tex = Decode(path);
                Assert.AreEqual(w, tex.width, file + " width");
                Assert.AreEqual(h, tex.height, file + " height");
                if (file == "library_logo.png")
                {
                    Assert.AreEqual(0, tex.GetPixel(0, 0).a, 1e-3f, "the logo is the title alone, on transparency");
                    Assert.Greater(tex.GetPixels32().Count(p => p.a > 200), 1000, "and the title is on it");
                }
                Object.DestroyImmediate(tex);
            }
            Assert.IsTrue(Marketing.Capsules.Any(c => c.File == "library_hero.png" && !c.Titled), "Steam lays the logo over the hero itself");
        }

        [Test]
        public void EveryScreenshotIsOfARoomThatExists()
        {
            var listed = Marketing.Shots.Select(s => s.Id + ".png").ToList();
            CollectionAssert.AllItemsAreUnique(listed);
            var onDisk = Directory.GetFiles(Docs("screenshots"), "*.png").Select(Path.GetFileName).ToList();
            CollectionAssert.AreEquivalent(listed, onDisk, "the screenshots are the list, no more and no fewer");
            foreach (var shot in Marketing.Shots)
            {
                Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/_Project/Scenes/Greybox/" + shot.Scene + ".unity")), shot.Id + ": " + shot.Scene);
                Assert.IsFalse(string.IsNullOrEmpty(shot.Caption), shot.Id + " has a caption for the store");
                var tex = Decode(Docs("screenshots/" + shot.Id + ".png"));
                Assert.AreEqual(Marketing.ShotWidth, tex.width, shot.Id);
                Assert.AreEqual(Marketing.ShotHeight, tex.height, shot.Id);
                Object.DestroyImmediate(tex);
            }
            Assert.GreaterOrEqual(Marketing.Shots.Count, 10, "Steam wants at least five; a page shows ten");
        }

        [Test]
        public void EveryTrailerClipIsEncoded()
        {
            foreach (var clip in Marketing.Clips)
            {
                var path = Docs("trailer/" + clip.Id + ".mp4");
                Assert.IsTrue(File.Exists(path), clip.Id);
                Assert.Greater(new FileInfo(path).Length, 100_000, clip.Id + " is a clip, not a stub");
                Assert.Greater(clip.Steps.Sum(s => s.Frames), Marketing.ClipFps * 3, clip.Id + " runs over three seconds");
                if (clip.Scene != null)
                    Assert.IsTrue(File.Exists(Path.GetFullPath("Assets/_Project/Scenes/Greybox/" + clip.Scene + ".unity")), clip.Id + ": " + clip.Scene);
            }
        }

        [Test]
        public void TheTitlesFontCarriesItsLicence()
        {
            var fonts = Path.GetFullPath("../tools/marketing/fonts");
            Assert.IsTrue(File.Exists(Path.Combine(fonts, "IMFellEnglish-Regular.ttf")));
            StringAssert.Contains("SIL Open Font License", File.ReadAllText(Path.Combine(fonts, "OFL.txt")));
        }
    }
}
