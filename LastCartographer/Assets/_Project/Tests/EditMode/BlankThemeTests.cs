using System;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The Blank's theme as this player remembers it (AUD-18, AUD-24, docs/design/music.md §3c): only the regions drawn on
    /// her page are in its lead, their tunes run together in the order the world is walked with the white after them, or
    /// (the playtest's other reading) each in its own slot and a rest where an undrawn region's would be; the coast is
    /// always heard; every region drawn is the Blank's own theme either way; a page's regions are its surveyed vantages'
    /// that are not erased.
    /// </summary>
    public class BlankThemeTests
    {
        static float[] Slot(Score.Theme t, int k) => t.Stem("lead").Notes.Where(n => n.Start >= 1f + 2f * k - 1e-3f && n.Start < 3f + 2f * k - 1e-3f).Select(n => (float)n.Degree).ToArray();

        [Test]
        public void TheTunesSheHeardRunTogether()
        {
            Assert.IsFalse(Score.BlankWithHoles, "run together, unless the playtest asks for the holes");
            var full = Score.ThemeOf(Region.Blank);
            Assert.AreSame(full, Score.BlankThemeOf(Score.Heard), "every region drawn: the Blank's own theme");
            var some = Score.BlankThemeOf(new[] { Region.Windreach, Region.Emberdown });
            Assert.AreSame(some, Score.BlankThemeOf(new[] { Region.Emberdown, Region.Windreach }), "kept once made, whatever the order");
            Assert.AreEqual("blank-sew", some.Id, "named for what it remembers: the coast, Emberdown, Windreach");
            int coast = 0, ember = Array.IndexOf(Score.Heard, Region.Emberdown), wind = Array.IndexOf(Score.Heard, Region.Windreach);
            CollectionAssert.AreEqual(Slot(full, coast), Slot(some, 0), "the coast first");
            CollectionAssert.AreEqual(Slot(full, ember), Slot(some, 1), "Emberdown's tune next");
            CollectionAssert.AreEqual(Slot(full, wind), Slot(some, 2), "and Windreach's straight after it, though the Verdance and Halden lie between");
            for (int k = 3; k < Score.Heard.Length; k++) Assert.IsEmpty(Slot(some, k), "then the white: slot " + k);
            Assert.AreEqual(Slot(full, 0).Length + Slot(full, ember).Length + Slot(full, wind).Length, some.Stem("lead").Notes.Count, "nothing but the three tunes");
            foreach (var stem in full.Stems.Where(s => s.Id != "lead"))
                CollectionAssert.AreEqual(stem.Notes, some.Stem(stem.Id).Notes, stem.Id + ": the rest of the Blank is the same");
            var none = Score.BlankThemeOf(null);
            CollectionAssert.AreEqual(Slot(full, 0), Slot(none, 0), "the coast alone, at the top");
            Assert.AreEqual(4, none.Stem("lead").Notes.Count);
            Assert.IsFalse(Score.Themes.Contains(some), "a player's variant is not a deliverable");
        }

        [Test]
        public void ThePlaytestsOtherReadingKeepsEachTuneInItsSlot()
        {
            Score.BlankWithHoles = true;
            try { OnlyTheRegionsDrawnAreRememberedEachInItsSlot(); }
            finally { Score.BlankWithHoles = false; }
            var runTogether = Score.BlankThemeOf(new[] { Region.Halden, Region.Emberdown });
            Score.BlankWithHoles = true;
            try { Assert.AreNotSame(runTogether, Score.BlankThemeOf(new[] { Region.Halden, Region.Emberdown }), "the two readings are kept apart"); }
            finally { Score.BlankWithHoles = false; }
            Assert.AreSame(runTogether, Score.BlankThemeOf(new[] { Region.Halden, Region.Emberdown }));
        }

        void OnlyTheRegionsDrawnAreRememberedEachInItsSlot()
        {
            var full = Score.ThemeOf(Region.Blank);
            Assert.AreSame(full, Score.BlankThemeOf(Score.Heard), "every region drawn: the Blank's own theme, holes or none");
            Assert.AreEqual("blank", full.Id);

            var some = Score.BlankThemeOf(new[] { Region.Halden, Region.Emberdown });
            Assert.AreSame(some, Score.BlankThemeOf(new[] { Region.Emberdown, Region.Halden, Region.Halden }), "kept once made, whatever the order");
            Assert.AreNotSame(full, some);
            Assert.AreEqual("blank-holes-seh", some.Id, "named for what it remembers, and for the holes: the coast, Emberdown, Halden");
            for (int k = 0; k < Score.Heard.Length; k++)
            {
                var r = Score.Heard[k];
                bool heard = r == Region.Saltmarrow || r == Region.Emberdown || r == Region.Halden;
                if (heard) CollectionAssert.AreEqual(Slot(full, k), Slot(some, k), r + "'s tune, backwards, in its own slot");
                else Assert.IsEmpty(Slot(some, k), r + " never drawn: a rest where its tune would be");
            }
            foreach (var stem in full.Stems.Where(s => s.Id != "lead"))
                CollectionAssert.AreEqual(stem.Notes, some.Stem(stem.Id).Notes, stem.Id + ": the Remnant, the clock and the reversed roll-call are the same");
            Assert.AreEqual(full.Bars, some.Bars); Assert.AreEqual(full.RestBars, some.RestBars, "the same loop and the same white");

            var none = Score.BlankThemeOf(null);
            Assert.AreSame(none, Score.BlankThemeOf(new Region[0]));
            Assert.AreSame(none, Score.BlankThemeOf(new[] { Region.Greyfold, Region.Blank }), "only the five heard regions count");
            Assert.AreEqual(4, none.Stem("lead").Notes.Count, "the coast she woke on is always heard");
            CollectionAssert.AreEqual(Slot(full, 0), Slot(none, 0));

            var stems = Score.Render(some);
            int len = (int)Math.Round(some.LoopSeconds * Score.SampleRate);
            var mix = new float[len];
            foreach (var s in stems.Values) { Assert.AreEqual(len, s.Length); for (int i = 0; i < len; i++) mix[i] += s[i]; }
            Assert.AreEqual(AudioDirection.SfxPeakDbtp, RollCallSong.PeakDb(mix), 0.05f, "at the ceiling");
            Assert.IsFalse(Score.Themes.Contains(some), "a player's variant is not a deliverable");
        }

        [Test]
        public void APagesRegionsAreItsDrawnVantagesNotTheErasedOnes()
        {
            var w = new WorldState();
            CollectionAssert.IsEmpty(Score.RegionsDrawn(w));
            Assert.IsEmpty(Score.RegionsDrawn(null));
            w.SurveyedVantages.Add("Emberdown_Sound_Test/Top");
            w.SurveyedVantages.Add("Verdance_Sound_Test/Root");
            w.SurveyedVantages.Add("Greyfold_Sound_Test/Edge");
            CollectionAssert.AreEquivalent(new[] { Region.Emberdown, Region.Verdance }, Score.RegionsDrawn(w), "the Greyfold is not a tune the Blank remembers");
            w.ErasedVantages.Add("Verdance_Sound_Test/Root");
            CollectionAssert.AreEquivalent(new[] { Region.Emberdown }, Score.RegionsDrawn(w), "a vantage erased is off the page");
            Assert.AreEqual("blank-se", Score.BlankThemeOf(Score.RegionsDrawn(w)).Id, "run together, the default");
        }
    }
}
