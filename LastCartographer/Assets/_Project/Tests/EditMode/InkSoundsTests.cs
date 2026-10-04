using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// Wren's sounds and the tells (AUD-03, docs/design/wren-sounds.md): every cue renders under the peak and the same
    /// every time; a tell for every attack kind, each over before the fastest read, the slam the only low one; her
    /// sounds are paper (short, no sustained tone, nothing sung); the survey's hatching loops without a click; the
    /// deliverables are rendered.
    /// </summary>
    public class InkSoundsTests
    {
        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [Test]
        public void EveryCueRendersUnderThePeakAndTheSameEveryTime()
        {
            Assert.GreaterOrEqual(InkSounds.Cues.Count, 20);
            foreach (var c in InkSounds.Cues)
            {
                var s = InkSounds.Render(c.Id);
                Assert.IsNotNull(s, c.Id);
                Assert.Greater(s.Length, InkSounds.SampleRate / 100, c.Id + " is at least 10 ms");
                Assert.AreEqual(AudioDirection.SfxPeakDbtp, RollCallSong.PeakDb(s), 0.05f, c.Id + " peaks at the ceiling");
                Assert.IsNotEmpty(c.What, c.Id + " says what it is");
                Assert.That(c.Gain, Is.InRange(0.2f, 1f), c.Id);
                var again = InkSounds.Render(c.Id);
                CollectionAssert.AreEqual(s, again, c.Id + " is deterministic");
                if (c.Loop)
                {
                    // A loop's join is no bigger a step than any inside it: it meets itself (AUD-10's hums and rumbles).
                    float inside = 0f;
                    for (int i = 1; i < s.Length; i++) inside = Math.Max(inside, Math.Abs(s[i] - s[i - 1]));
                    Assert.LessOrEqual(Math.Abs(s[0] - s[s.Length - 1]), inside + 0.01f, c.Id + " meets itself: no click at the join");
                }
                else Assert.AreEqual(0f, Math.Abs(s[s.Length - 1]), 0.02f, c.Id + " ends at nothing: no click");
            }
            Assert.IsNull(InkSounds.Render("nothing"));
            Assert.IsFalse(InkSounds.Has("wren_voice"), "her voice is never heard");
        }

        [Test]
        public void ATellForEveryAttackKindWithinTheFastestRead()
        {
            foreach (AttackKind k in Enum.GetValues(typeof(AttackKind)))
            {
                var tell = AudioDirection.TellOf(k.ToString());
                var cue = InkSounds.Of(InkSounds.TellCue(tell));
                Assert.IsNotNull(cue, k + " has a tell");
                Assert.AreEqual(InkSounds.Kind.Tell, cue.Kind);
                Assert.AreEqual(tell, cue.Tell);
                float seconds = InkSounds.Seconds(InkSounds.Render(cue.Id));
                Assert.LessOrEqual(seconds, AudioDirection.TellFrames(tell) / 60f + 0.001f, k + "'s tell is over within its frames");
                Assert.LessOrEqual(seconds, Tuning.TelegraphFloor(4) / 60f + 0.001f, "and before the fastest read");
            }
            var slam = InkSounds.Render(InkSounds.TellCue(AudioDirection.Tell.Slam));
            Assert.Less(RollCallSong.EnergyAbove(slam, 500f), 0.15f, "the slam is low");
            foreach (var t in new[] { AudioDirection.Tell.Strike, AudioDirection.Tell.Window, AudioDirection.Tell.Shape })
                Assert.Greater(RollCallSong.EnergyAbove(InkSounds.Render(InkSounds.TellCue(t)), 500f), 0.7f, t + " is not low: the slam is the only low tell");
            var strike = InkSounds.Render(InkSounds.TellCue(AudioDirection.Tell.Strike));
            Assert.Greater(RollCallSong.EnergyAbove(strike, 2500f), 0.6f, "the strike's tell is high");
            float window = RollCallSong.PitchOf(InkSounds.Render(InkSounds.TellCue(AudioDirection.Tell.Window)), 0, 2048, 500f, 4000f);
            Assert.AreEqual(2093f, window, 40f, "the window's chime rings a C");
        }

        [Test]
        public void HerQuillIsInkAndPaper()
        {
            var stroke = InkSounds.Render("stroke");
            var hit = InkSounds.Render("hit");
            var pogo = InkSounds.Render("pogo");
            var hurt = InkSounds.Render("hurt");
            var bind = InkSounds.Render("bind");
            Assert.Less(InkSounds.Seconds(pogo), InkSounds.Seconds(stroke), "a tap is shorter than a stroke");
            Assert.Less(InkSounds.Seconds(stroke), 0.15f, "a stroke is a stroke: short");
            Assert.Greater(RollCallSong.EnergyAbove(stroke, 800f), 0.8f, "a pen on paper: nothing low in a stroke");
            Assert.Less(RollCallSong.EnergyAbove(hurt, 600f), 0.2f, "hurt is a smudge: low and dull, not a cry");
            Assert.Greater(RollCallSong.EnergyAbove(hit, 60f) - RollCallSong.EnergyAbove(stroke, 60f), -1f);
            Assert.Less(RollCallSong.EnergyAbove(hit, 300f), RollCallSong.EnergyAbove(stroke, 300f), "the hit layer has the paper's thump under it");
            Assert.Greater(InkSounds.Seconds(bind), 0.5f, "a word takes a moment to write");
            // Six scratches: six bursts of energy above the noise between them.
            int bursts = 0; bool inBurst = false;
            int win = InkSounds.SampleRate / 200;
            for (int i = 0; i + win < bind.Length; i += win)
            {
                float e = 0f; for (int j = i; j < i + win; j++) e += bind[j] * bind[j];
                bool on = e / win > 0.002f;
                if (on && !inBurst) bursts++;
                inBurst = on;
            }
            Assert.That(bursts, Is.InRange(6, 8), "six scratches and a full stop");
            Assert.Greater(InkSounds.Of("jump").Gain * 2f, 0f); Assert.Less(InkSounds.Of("jump").Gain, InkSounds.Of("stroke").Gain, "steps are quieter than strikes");
            Assert.AreEqual(InkSounds.Kind.Layer, InkSounds.Of("hit").Kind); Assert.AreEqual(InkSounds.Kind.Layer, InkSounds.Of("kill").Kind);
            Assert.Greater(InkSounds.Seconds(InkSounds.Render("kill")), InkSounds.Seconds(hit), "a kill is the longer layer");
            foreach (var k in new[] { FlourishKind.Crosshatch, FlourishKind.Longstroke, FlourishKind.Blot })
                Assert.AreEqual(InkSounds.Kind.Flourish, InkSounds.Of(k.ToString().ToLowerInvariant()).Kind, k + " has its sound");
        }

        [Test]
        public void TheSurveyHatchesInALoop()
        {
            var cue = InkSounds.Of("survey");
            Assert.IsTrue(cue.Loop, "held, so looped");
            var s = InkSounds.Render("survey");
            Assert.AreEqual(0.5f, InkSounds.Seconds(s), 0.001f, "half a second round");
            int edge = InkSounds.SampleRate / 1000;
            float head = 0f, tail = 0f;
            for (int i = 0; i < edge; i++) { head += Math.Abs(s[i]); tail += Math.Abs(s[s.Length - 1 - i]); }
            Assert.Less(head / edge, 0.02f, "silent at the start"); Assert.Less(tail / edge, 0.02f, "and at the end: no click at the join");
            CollectionAssert.AreEquivalent(new[] { "survey", FootstepSounds.SlideCue }, InkSounds.Cues.Where(c => c.Loop && c.Kind != InkSounds.Kind.Enemy).Select(c => c.Id),
                "of hers, only the survey and the wall's scrape loop (AUD-17)");
        }

        [Test]
        public void TheDeliverablesAreRendered()
        {
            string dir = Path.Combine(RepoRoot, "docs/audio/sfx");
            Assert.IsTrue(Directory.Exists(dir), dir);
            foreach (var c in InkSounds.Cues)
            {
                string file = Path.Combine(dir, InkSounds.FileName(c));
                Assert.IsTrue(File.Exists(file), c.Id + " is rendered");
                var head = new byte[44];
                using (var fs = File.OpenRead(file)) Assert.AreEqual(44, fs.Read(head, 0, 44), file);
                var (rate, bits, _) = RollCallSong.WavHeader(head);
                Assert.AreEqual(48000, rate, c.Id); Assert.AreEqual(24, bits, c.Id);
            }
        }
    }
}
