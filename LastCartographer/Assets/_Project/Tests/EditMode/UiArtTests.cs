using System.IO;
using NUnit.Framework;
using OWSBG.UI;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The UI's drawings (ENV-11, docs/design/ui-art.md): every piece <see cref="InkArt"/> asks for is packed under
    /// Art/UI/Resources/UI at the size ui.json gives it, reachable through Resources, its slices and the Inkwell's
    /// fill inside it, imported crisp; the two faces and their licences sit beside them; the sheet is in the docs.
    /// </summary>
    public class UiArtTests
    {
        const string Art = "Assets/_Project/Art/UI/Resources/UI/";
        const string Fonts = Art + "Fonts/";

        [SetUp] public void SetUp() => InkArt.Forget();

        [Test]
        public void EveryDrawingIsPackedAtItsSizeAndLoads()
        {
            foreach (var name in InkArt.Required)
            {
                var path = Art + name + ".png";
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.IsNotNull(tex, name + " is packed at " + path + " (tools/ui/ui_art.py, then tools/ui/pack.py)");
                var piece = InkArt.PieceOf(name);
                Assert.IsNotNull(piece, name + " is in ui.json");
                Assert.AreEqual(piece.w, tex.width, name + " is as wide as ui.json says");
                Assert.AreEqual(piece.h, tex.height, name + " is as tall as ui.json says");
                if (piece.slices != null && piece.slices.Any)
                {
                    Assert.Less(piece.slices.l + piece.slices.r, piece.w, name + "'s side slices leave a middle");
                    Assert.Less(piece.slices.t + piece.slices.b, piece.h, name + "'s top and bottom slices leave a middle");
                }
                Assert.AreSame(tex, InkArt.Tex(name), name + " loads through Resources");
            }
            Assert.IsTrue(InkArt.Available);
        }

        [Test]
        public void ThePaperSlicesAndTheInkwellsFillAreSet()
        {
            foreach (var paper in new[] { "UI_Page", "UI_Strip", "UI_Spread", "UI_Portrait" })
            {
                var s = InkArt.PieceOf(paper)?.slices;
                Assert.IsNotNull(s, paper + " slices");
                Assert.Greater(s.l, 0); Assert.Greater(s.t, 0); Assert.Greater(s.r, 0); Assert.Greater(s.b, 0);
            }
            var fill = InkArt.PieceOf("UI_InkwellFill")?.fill;
            Assert.IsNotNull(fill, "where the ink runs in the bottle");
            Assert.Greater(fill.bottom, 0f); Assert.Greater(fill.top, fill.bottom); Assert.LessOrEqual(fill.top, 1f);
            var glyph = InkArt.PieceOf("UI_MaskFull");
            Assert.IsFalse(glyph.slices != null && glyph.slices.Any, "a glyph does not stretch");
            Assert.IsFalse(glyph.fill != null && glyph.fill.Any, "and has no fill");
        }

        [Test]
        public void TheDrawingsImportCrisp()
        {
            foreach (var name in InkArt.Required)
            {
                var importer = AssetImporter.GetAtPath(Art + name + ".png") as TextureImporter;
                Assert.IsNotNull(importer, name);
                Assert.IsFalse(importer.mipmapEnabled, name + ": no mipmaps");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, name + ": uncompressed");
                Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode, name + ": clamped");
                Assert.IsTrue(importer.alphaIsTransparency, name + ": straight alpha");
            }
        }

        [Test]
        public void TheFacesAreThereWithTheirLicences()
        {
            foreach (var face in new[] { InkArt.TitleFontName, InkArt.BodyFontName, InkArt.BodyItalicName, InkArt.BodyBoldName })
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Font>(Fonts + face + ".ttf"), face + ".ttf imports as a font");
                Assert.IsNotNull(Resources.Load<Font>(InkArt.FontsFolder + face), face + " loads through Resources");
            }
            foreach (var licence in new[] { "OFL-AlegreyaSans.txt", "OFL-IMFellEnglish.txt" })
            {
                var path = Path.GetFullPath(Fonts + licence);
                Assert.IsTrue(File.Exists(path), licence + " sits beside the fonts");
                StringAssert.Contains("SIL Open Font License", File.ReadAllText(path));
            }
            Assert.AreSame(InkArt.BodyFont, InkTheme.Font, "the body is Alegreya Sans");
            Assert.AreSame(InkArt.TitleFont, InkTheme.TitleFont, "the titles are IM Fell English");
            Assert.AreNotSame(InkTheme.Font, InkTheme.TitleFont);
        }

        [Test]
        public void TheIconsAnswerEveryCharterAndInstrument()
        {
            foreach (OWSBG.Core.CharterKind k in System.Enum.GetValues(typeof(OWSBG.Core.CharterKind)))
                Assert.IsNotNull(InkArt.CharterIcon(k), k + "'s cowl");
            foreach (OWSBG.Core.InstrumentKind k in System.Enum.GetValues(typeof(OWSBG.Core.InstrumentKind)))
            {
                if (k == OWSBG.Core.InstrumentKind.None) Assert.IsNull(InkArt.InstrumentIcon(k), "an empty loop has no drawing");
                else Assert.IsNotNull(InkArt.InstrumentIcon(k), k + "'s drawing");
            }
        }

        [Test]
        public void TheSheetIsInTheDocs()
        {
            var root = Directory.GetParent(Application.dataPath).Parent.FullName;
            Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "art", "ui-sheet.png")), "docs/art/ui-sheet.png (tools/ui/pack.py draws it)");
            Assert.IsTrue(File.Exists(Path.Combine(root, "docs", "design", "ui-art.md")));
        }
    }
}
