using NUnit.Framework;
using OWSBG.Core;
using OWSBG.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.Tests
{
    /// <summary>
    /// The player's options (DES-14): what they start as, that they are kept, the quarters the sliders move in, how long
    /// a caption stays for each choice, and the high-contrast palette: every text colour at 7:1 on its paper (WCAG AAA),
    /// swatches that can't be mistaken for each other, and a redraw that moves only the palette's colours.
    /// </summary>
    public class OptionsTests
    {
        [SetUp] public void SetUp() => Options.ResetToDefaults();
        [TearDown] public void TearDown() => Options.ResetToDefaults();

        [Test]
        public void TheDefaultsAreAsDesignedAndChoicesAreKept()
        {
            Assert.AreEqual(1f, Options.Hitstop);
            Assert.AreEqual(1f, Options.Shake);
            Assert.AreEqual(CaptionTime.Normal, Options.Captions);
            Assert.IsFalse(Options.HighContrast);
            foreach (Hold h in System.Enum.GetValues(typeof(Hold))) Assert.IsFalse(Options.IsToggle(h), h + " is held by default");

            Options.Hitstop = 0.3f;     // a slider moves in quarters
            Options.Shake = 0f;
            Options.Captions = CaptionTime.UntilDismissed;
            Options.HighContrast = true;
            Options.SetToggle(Hold.Survey, true);
            Assert.AreEqual(0.25f, Options.Hitstop);

            Options.Load();             // as the next session would find them
            Assert.AreEqual(0.25f, Options.Hitstop);
            Assert.AreEqual(0f, Options.Shake);
            Assert.AreEqual(CaptionTime.UntilDismissed, Options.Captions);
            Assert.IsTrue(Options.HighContrast);
            Assert.IsTrue(Options.IsToggle(Hold.Survey));
            Assert.IsFalse(Options.IsToggle(Hold.Bind));
            Assert.AreEqual(1f, Shader.GetGlobalFloat(Options.ContrastId), "the world is told to draw in high contrast");

            Options.ResetToDefaults();
            Assert.AreEqual(1f, Options.Hitstop);
            Assert.AreEqual(0f, Shader.GetGlobalFloat(Options.ContrastId));
        }

        [Test]
        public void ChangesAreAnnouncedOnce()
        {
            int changes = 0;
            System.Action on = () => changes++;
            Options.Changed += on;
            try
            {
                Options.Shake = 0.5f;
                Options.Shake = 0.5f;
                Options.Shake = 0.55f;     // the same quarter
                Options.SetToggle(Hold.Glide, true);
                Options.SetToggle(Hold.Glide, true);
                Options.HighContrast = false;
                Assert.AreEqual(2, changes);
            }
            finally { Options.Changed -= on; }
        }

        [Test]
        public void HitstopAndCaptionsFollowTheChoice()
        {
            Assert.AreEqual(8, Options.HitstopFrames(8));
            Options.Hitstop = 0.5f;
            Assert.AreEqual(4, Options.HitstopFrames(8));
            Options.Hitstop = 0.25f;
            Assert.AreEqual(1, Options.HitstopFrames(2), "any kept is at least a frame");
            Options.Hitstop = 0f;
            Assert.AreEqual(0, Options.HitstopFrames(8), "none at all");

            Assert.AreEqual(3.5f, Options.CaptionSeconds(3.5f));
            Options.Captions = CaptionTime.Double;
            Assert.AreEqual(7f, Options.CaptionSeconds(3.5f));
            Options.Captions = CaptionTime.Triple;
            Assert.AreEqual(10.5f, Options.CaptionSeconds(3.5f));
            Options.Captions = CaptionTime.UntilDismissed;
            Assert.IsTrue(float.IsPositiveInfinity(Options.CaptionSeconds(3.5f)), "a caption waits to be dismissed");
        }

        [Test]
        public void HighContrastInkReadsAtSevenToOne()
        {
            var hc = InkTheme.HighContrast;
            var paper = hc[(int)InkTheme.Swatch.Paper];
            var paperDark = hc[(int)InkTheme.Swatch.PaperDark];
            foreach (var s in new[] { InkTheme.Swatch.Ink, InkTheme.Swatch.Dim, InkTheme.Swatch.Wash })
            {
                Assert.GreaterOrEqual(InkTheme.ContrastRatio(hc[(int)s], paper), 7f, s + " on the paper");
                Assert.GreaterOrEqual(InkTheme.ContrastRatio(hc[(int)s], paperDark), 7f, s + " on a selected row");
            }
            Assert.GreaterOrEqual(InkTheme.ContrastRatio(hc[(int)InkTheme.Swatch.Ochre], paper), 4.5f, "ochre marks (a miss, a fulfilled commission)");
            Assert.AreEqual(1f, paper.a, "no world through the paper");

            // And better than the warm palette, colour for colour.
            var warm = InkTheme.Warm;
            foreach (var s in new[] { InkTheme.Swatch.Ink, InkTheme.Swatch.Dim, InkTheme.Swatch.Wash, InkTheme.Swatch.Ochre })
                Assert.Greater(InkTheme.ContrastRatio(hc[(int)s], paper), InkTheme.ContrastRatio(warm[(int)s], warm[(int)InkTheme.Swatch.Paper]), s.ToString());

            // Each swatch is its own colour in each palette, so a redraw can tell them apart.
            foreach (var palette in new[] { warm, hc })
                for (int i = 0; i < palette.Length; i++)
                    for (int j = i + 1; j < palette.Length; j++)
                        Assert.AreNotEqual(palette[i], palette[j], (InkTheme.Swatch)i + " and " + (InkTheme.Swatch)j);
        }

        [Test]
        public void ARedrawMovesOnlyThePalettesColours()
        {
            var root = new VisualElement();
            var panel = new VisualElement();
            panel.style.backgroundColor = InkTheme.Warm[(int)InkTheme.Swatch.Paper];
            InkTheme.SetBorder(panel, InkTheme.Warm[(int)InkTheme.Swatch.InkFaint], 1f);
            var label = new Label("Options");
            label.style.color = InkTheme.Warm[(int)InkTheme.Swatch.Dim];
            var tint = new Label("a region's own colour");
            tint.style.color = new Color(0.7f, 0.2f, 0.2f);
            var plain = new Label("inherits");
            panel.Add(label); panel.Add(tint); panel.Add(plain); root.Add(panel);

            InkTheme.Recolour(root, true);
            Assert.AreEqual(InkTheme.HighContrast[(int)InkTheme.Swatch.Paper], panel.style.backgroundColor.value);
            Assert.AreEqual(InkTheme.HighContrast[(int)InkTheme.Swatch.InkFaint], panel.style.borderLeftColor.value);
            Assert.AreEqual(InkTheme.HighContrast[(int)InkTheme.Swatch.Dim], label.style.color.value);
            Assert.AreEqual(new Color(0.7f, 0.2f, 0.2f), tint.style.color.value, "not a swatch: left alone");
            Assert.AreNotEqual(StyleKeyword.Undefined, plain.style.color.keyword, "nothing set where nothing was");

            InkTheme.Recolour(root, false);
            Assert.AreEqual(InkTheme.Warm[(int)InkTheme.Swatch.Paper], panel.style.backgroundColor.value);
            Assert.AreEqual(InkTheme.Warm[(int)InkTheme.Swatch.Dim], label.style.color.value);

            Options.HighContrast = true;
            Assert.AreEqual(InkTheme.HighContrast[(int)InkTheme.Swatch.Ink], InkTheme.Ink, "new elements draw in the chosen palette");
        }
    }
}
