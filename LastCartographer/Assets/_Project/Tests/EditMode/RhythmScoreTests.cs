using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The rhythm bosses' themes and the Brood's (AUD-13, docs/design/music.md §3d): the Collapse's at Emberdown's beat
    /// with a pulse on every beat, a round of the lamps a bar, the chorus calling a name on each beat and a voice fewer
    /// in phase 3; the Complete Survey's three, one an ink, each in its region's key and at its beat, two verses a loop,
    /// the chorus calling on the verse's beats and breathing in the break; the Brood's chant, "Ours. Ours. Ours.", asking
    /// in the last phase. They are the bosses' own now, not the shared motif.
    /// </summary>
    public class RhythmScoreTests
    {
        static void AllInLoop(Score.Theme t)
        {
            float loop = t.LoopBars * Score.BeatsPerBar;
            foreach (var need in Score.RequiredStems) Assert.IsNotNull(t.Stem(need), t.Id + " has a " + need + " stem");
            foreach (var s in t.Stems)
            {
                Assert.IsNotEmpty(s.Notes, t.Id + "/" + s.Id);
                Assert.AreEqual(s.Instrument, Score.InstrumentOf(s.Instrument).Id, t.Id + "/" + s.Id + " plays a known instrument");
                Assert.GreaterOrEqual(s.Phase, 1, t.Id + "/" + s.Id + " enters at a phase");
                foreach (var n in s.Notes)
                {
                    Assert.GreaterOrEqual(n.Start, 0f, t.Id + "/" + s.Id + " " + n);
                    Assert.LessOrEqual(n.Start + n.Beats, loop + 1e-3f, t.Id + "/" + s.Id + " " + n + " ends in the loop");
                    Assert.IsTrue(Score.InMode(t.Region, Score.Semitones(t.Region, n.Degree)), t.Id + "/" + s.Id + " " + n + " in the mode");
                }
            }
        }

        static bool OnBeat(Score.Stem s, float beat) => s.Notes.Any(n => Mathf.Abs(n.Start - beat) < 1e-3f);

        [Test]
        public void TheCollapseIsScoredToTheBeatTheChorusKeeps()
        {
            var t = Score.ThemeOfBoss("Collapse");
            Assert.IsNotNull(t, "the Collapse has a theme of its own");
            Assert.AreEqual(Region.Emberdown, t.Region, "Hollowvein: Emberdown's key");
            Assert.IsTrue(t.KeepsBeat, "the driver keeps it on the fight's beat");
            var go = new GameObject("Collapse_Test");
            try
            {
                go.AddComponent<BoxCollider2D>(); go.AddComponent<Rigidbody2D>();
                var c = go.AddComponent<Collapse>();
                Assert.AreEqual(c.beatSeconds, t.Beat, 1e-4f, "at the beat the fight keeps");
                Assert.AreEqual(c.lampCount, Score.CollapseLamps);
                Assert.AreEqual(0, t.Bars * Score.BeatsPerBar % c.lampCount, "the loop is whole rounds of the lamps");
            }
            finally { Object.DestroyImmediate(go); }
            AllInLoop(t);
            float loop = t.Bars * Score.BeatsPerBar;
            var pulse = t.Stem("pulse");
            for (int b = 0; b < loop; b++)
            {
                Assert.IsTrue(OnBeat(pulse, b), "a pulse on beat " + b + ": strike on the drum");
                var n = pulse.Notes.First(x => Mathf.Abs(x.Start - b) < 1e-3f);
                if (b % Score.CollapseLamps == 0) Assert.AreEqual(1f, n.Level, "the round's first lamp the strongest");
                else Assert.Less(n.Level, 1f);
            }
            Assert.AreEqual(pulse.Notes.Count, (int)loop, "and nothing between: the beat is the only rhythm");
            var (lift, name) = Score.CallDegrees(Region.Emberdown);
            var voices = t.Stem("voices");
            for (int b = 0; b < 12; b++)
            {
                Assert.IsTrue(voices.Notes.Any(n => n.Degree == name && Mathf.Abs(n.Start - b) < 1e-3f), "a name called on beat " + b);
                float liftAt = b == 0 ? loop - 0.5f : b - 0.5f;
                Assert.IsTrue(voices.Notes.Any(n => n.Degree == lift && Mathf.Abs(n.Start - liftAt) < 1e-3f), "the lift half a beat ahead of " + b + ", as a bounds-walk calls");
            }
            CollectionAssert.AreEqual(Score.AnswerDegrees(Region.Emberdown), voices.Notes.Where(n => n.Start >= 12f && n.Start < 15.5f).OrderBy(n => n.Start).Select(n => n.Degree), "the answer ends the fourth round");
            Assert.AreEqual(3, voices.Until, "phase 3: the full chorus goes");
            var fewer = t.Stem("fewer");
            Assert.AreEqual(3, fewer.Phase, "and comes back a voice fewer");
            for (int b = 0; b < loop; b++)
                Assert.AreEqual(b % Score.CollapseLamps != Score.CollapseLamps - 1, fewer.Notes.Any(n => n.Degree == name && Mathf.Abs(n.Start - b) < 1e-3f), "beat " + b + ": the last of each round unsung");
            Assert.AreEqual("anvil", t.Stem("count").Instrument);
            Assert.AreEqual(2, t.Stem("count").Phase, "the anvil comes with the surge");
            Assert.IsTrue(OnBeat(t.Stem("count"), 1f) && OnBeat(t.Stem("count"), 3f) && !OnBeat(t.Stem("count"), 0f), "on two and four");
        }

        [Test]
        public void TheCompleteSurveyHasAThemeForEachInkEachKeepingItsBeat()
        {
            var go = new GameObject("Survey_Test");
            float[] beats; int verse, brk;
            try
            {
                go.AddComponent<BoxCollider2D>(); go.AddComponent<Rigidbody2D>();
                var cs = go.AddComponent<CompleteSurvey>();
                beats = cs.beatSeconds; verse = cs.verseBeats; brk = cs.breakBeats;
            }
            finally { Object.DestroyImmediate(go); }
            Assert.AreEqual(verse, Score.SurveyVerseBeats); Assert.AreEqual(brk, Score.SurveyBreakBeats);
            for (int p = 1; p <= 3; p++)
            {
                var t = Score.ThemeOfBoss("CompleteSurvey", Region.Halden, p);
                Assert.IsNotNull(t, "phase " + p + " has its ink's theme");
                Assert.AreEqual(p, t.ForPhase);
                Assert.AreEqual(Score.SurveyInks[p - 1], t.Region, "phase " + p + " in " + CompleteSurvey.Inks[p - 1] + "'s key");
                Assert.That(CompleteSurvey.Inks[p - 1], Does.StartWith(t.Region.ToString()), "the ink the fight names");
                Assert.AreEqual(beats[p - 1], t.Beat, 1e-4f, "at the beat the phase keeps");
                Assert.IsTrue(t.KeepsBeat);
                AllInLoop(t);
                int loop = t.Bars * Score.BeatsPerBar;
                Assert.AreEqual(0, loop % (verse + brk), "whole verses a loop");
                var (lift, name) = Score.CallDegrees(t.Region);
                var voices = t.Stem("voices");
                for (int b = 0; b < loop; b++)
                {
                    Assert.IsTrue(OnBeat(t.Stem("pulse"), b), t.Id + ": a pulse on beat " + b);
                    bool inBreak = b % (verse + brk) >= verse;
                    Assert.AreEqual(!inBreak, voices.Notes.Any(n => n.Degree == name && Mathf.Abs(n.Start - b) < 1e-3f && n.Beats <= 0.5f + 1e-3f && n.Level >= 1f - 1e-3f),
                        t.Id + " beat " + b + (inBreak ? ": the chorus breathes" : ": a name called on the named ground's beat"));
                }
                Assert.AreSame(t, Score.ThemeOfBoss("CompleteSurvey", Region.Halden, p), "kept once found");
            }
            Assert.AreNotSame(Score.ThemeOfBoss("CompleteSurvey", null, 1), Score.ThemeOfBoss("CompleteSurvey", null, 2), "the ink changes with the phase");
        }

        [Test]
        public void TheBroodChantsOursAndAsksInTheLastPhase()
        {
            var t = Score.ThemeOfBoss("ReedmotherBrood");
            Assert.IsNotNull(t, "the Brood has a theme of its own");
            Assert.AreEqual(Region.Saltmarrow, t.Region, "the Pale Iris Fields: the coast's key");
            Assert.IsFalse(t.KeepsBeat, "a fight with no beat to keep");
            AllInLoop(t);
            var voices = t.Stem("voices");
            Assert.AreEqual("concertina", voices.Instrument, "the chant in reeds");
            for (int bar = 0; bar < t.Bars; bar++)
            {
                var chant = voices.Notes.Where(n => n.Start >= bar * 4f && n.Start < bar * 4f + 4f).ToList();
                Assert.AreEqual(3, chant.Count, "\"Ours. Ours. Ours.\"");
                Assert.AreEqual(1, chant.Select(n => n.Degree).Distinct().Count(), "one word, three times");
            }
            Assert.AreEqual(3, voices.Until, "phase 3: the chant stops");
            var huddle = t.Stem("huddle");
            Assert.AreEqual(3, huddle.Phase);
            var asked = huddle.Notes.Where(n => n.Start < 4f).OrderBy(n => n.Start).ToList();
            Assert.Greater(asked.Last().Degree, asked.First().Degree, "\"...ours?\": it rises at the end");
            Assert.AreEqual(2, t.Stem("bed").Phase, "the smoke with the second phase");
            Assert.AreEqual(2, t.Stem("lead").Phase);
            Assert.AreEqual(3, t.Stem("fire").Phase, "the fire reaches the nest");
            Assert.IsNull(t.Stems.FirstOrDefault(s => s.Instrument == "fiddle" && s.Phase == 1), "the Reedmother has no voice: no lead until the nest opens");
        }

        [Test]
        public void ThePhaseLookupFallsBackToTheBossesOwnThemes()
        {
            Assert.AreSame(Score.ThemeOfBoss("Brann"), Score.ThemeOfBoss("Brann", Region.Emberdown, 2), "a boss without phase themes keeps its own");
            Assert.AreEqual(Region.Halden, Score.ThemeOfBoss("Halvard", Region.Halden, 1).Region, "Halvard's count still travels");
            Assert.IsNull(Score.ThemeOfBoss("Hale", Region.Windreach, 1), "an optional still has none");
            Assert.IsNull(Score.ThemeOfBoss(null, Region.Halden, 1));
            Assert.IsNotNull(Score.ThemeOfBoss("Collapse", Region.Emberdown, 3), "a single theme serves every phase");
            Assert.AreEqual(0, Score.ThemeOfBoss("Collapse").ForPhase);
            foreach (var family in new[] { "Collapse", "CompleteSurvey", "ReedmotherBrood" })
                Assert.IsNotNull(Score.ThemeOfBoss(family), family + " no longer fights to the shared motif");
            Assert.AreEqual(5, Score.Themes.Count(t => t.Boss == "Collapse" || t.Boss == "CompleteSurvey" || t.Boss == "ReedmotherBrood"));
        }
    }
}
