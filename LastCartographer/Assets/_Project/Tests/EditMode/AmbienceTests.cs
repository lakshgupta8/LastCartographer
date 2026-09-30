using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The ambience (AUD-05, docs/design/ambience.md): every layer the direction names for every region has a recipe
    /// and renders to its own loop at the bed's level under the peak, loop lengths that never lock, the fade taking
    /// the last layer first and an erased place silent, the recipes sounding like what they name (a wash breathes,
    /// the clock ticks on Halden's beat, the thunder is low), and the deliverables rendered.
    /// </summary>
    public class AmbienceTests
    {
        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [Test]
        public void EveryLayerTheDirectionNamesHasARecipe()
        {
            foreach (var score in AudioDirection.All)
            {
                var layers = Ambience.Of(score.Region);
                CollectionAssert.AreEqual(score.AmbienceLayers, layers.Select(l => l.Name).ToArray(), score.Region + "'s layers, in the direction's order");
                for (int i = 0; i < layers.Count; i++)
                {
                    Assert.AreEqual(i, layers[i].Index);
                    if (i > 0) Assert.Less(layers[i].Level, layers[i - 1].Level, "most present first");
                }
                var seconds = layers.Select(l => l.Seconds).ToArray();
                Assert.AreEqual(seconds.Length, seconds.Distinct().Count(), score.Region + ": no two loops the same length");
                for (int a = 0; a < seconds.Length; a++) for (int b = a + 1; b < seconds.Length; b++)
                    Assert.AreEqual(1, Gcd(seconds[a], seconds[b]), score.Region + ": " + seconds[a] + " and " + seconds[b] + " share no factor, so the layers never lock");
            }
            Assert.AreEqual(AudioDirection.All.Sum(s => s.AmbienceLayers.Length), Ambience.Layers.Count, "every layer, once");
            Assert.AreEqual("saltmarrow_ambience_tide-on-the-pilings.wav", Ambience.Of(Region.Saltmarrow)[0].FileName);
            Assert.AreEqual("emberdown_ambience_the-roll-call-bells-hum.wav", Ambience.Find(Region.Emberdown, "the Roll-Call Bell's hum").FileName);
        }

        static int Gcd(int a, int b) { while (b != 0) { int t = a % b; a = b; b = t; } return a; }

        [Test]
        public void TheFadeTakesTheLastLayerFirstAndAnErasedPlaceIsSilent()
        {
            foreach (var score in AudioDirection.All)
            {
                var r = score.Region;
                var whole = Ambience.LevelsAt(r, 0);
                Assert.IsTrue(whole.All(l => l > 0f), r + " whole");
                for (int stage = 1; stage < FadeStages.Max; stage++)
                {
                    var now = Ambience.LevelsAt(r, stage);
                    int on = now.Count(l => l > 0f);
                    Assert.AreEqual(AudioDirection.AmbienceLayersAt(r, stage), on, r + " at stage " + stage);
                    Assert.IsTrue(now.Take(on).All(l => l > 0f) && now.Skip(on).All(l => l == 0f), r + ": the last layers go, the first stay");
                    Assert.GreaterOrEqual(on, 1, "at least one layer stays until the place is gone");
                }
                Assert.IsTrue(Ambience.LevelsAt(r, FadeStages.Max).All(l => l == 0f), r + " erased is silent");
            }
        }

        [Test]
        public void EveryLayerRendersAtTheBedsLevelUnderThePeakAndTheSameEveryTime()
        {
            foreach (var l in Ambience.Layers)
            {
                var s = Ambience.Render(l);
                Assert.AreEqual(l.Seconds * Ambience.SampleRate, s.Length, l.Name + " is exactly its loop");
                Assert.LessOrEqual(RollCallSong.PeakDb(s), AudioDirection.SfxPeakDbtp + 0.05f, l.Name + " never over the peak");
                float rms = Ambience.RmsDb(s);
                Assert.That(rms, Is.InRange(Ambience.TargetRmsDb - 10f, Ambience.TargetRmsDb + 0.1f), l.Name + " at the bed's level (a sparse layer sits well under it once its peaks are held)");
                Assert.IsFalse(s.Any(v => float.IsNaN(v) || float.IsInfinity(v)), l.Name);
            }
            var once = Ambience.Render(Ambience.Find(Region.Saltmarrow, "reeds"));
            var again = Ambience.Render(Ambience.Find(Region.Saltmarrow, "reeds"));
            CollectionAssert.AreEqual(once, again, "the same every time");
        }

        [Test]
        public void TheRecipesSoundLikeWhatTheyName()
        {
            var tide = Ambience.Render(Ambience.Find(Region.Saltmarrow, "tide on the pilings"));
            var reeds = Ambience.Render(Ambience.Find(Region.Saltmarrow, "reeds"));
            Assert.Less(RollCallSong.EnergyAbove(tide, 1500f), 0.2f, "the tide is low");
            Assert.Greater(RollCallSong.EnergyAbove(reeds, 1500f), 0.5f, "the reeds hiss");
            Assert.Greater(RollCallSong.EnergyAbove(reeds, 1500f), RollCallSong.EnergyAbove(tide, 1500f) * 3f, "and are far brighter than the tide");
            // The tide breathes: its loudest second is well above its quietest.
            int sec = Ambience.SampleRate;
            var perSecond = Enumerable.Range(0, tide.Length / sec).Select(k => { double e = 0; for (int i = k * sec; i < (k + 1) * sec; i++) e += tide[i] * tide[i]; return Math.Sqrt(e / sec); }).ToArray();
            Assert.Greater(perSecond.Max() / perSecond.Min(), 1.5, "the tide swells and draws back");

            var clock = Ambience.Find(Region.Halden, "the Observatory's clockwork");
            Assert.AreEqual(AudioDirection.BeatOf(Region.Halden), clock.A, 1e-4f, "the clock ticks on Halden's beat");
            var ticks = Ambience.Render(clock);
            int period = (int)(clock.A * Ambience.SampleRate), window = Ambience.SampleRate / 100;
            float onBeat = 0f, offBeat = 0f;
            for (int k = 1; k < 10; k++)
            {
                for (int i = 0; i < window; i++) { onBeat += Math.Abs(ticks[k * period + i]); offBeat += Math.Abs(ticks[k * period + period / 4 + i]); }
            }
            Assert.Greater(onBeat, offBeat * 4f, "a tick on every beat, and quiet between");

            var thunder = Ambience.Render(Ambience.Find(Region.Windreach, "far thunder"));
            Assert.Less(RollCallSong.EnergyAbove(thunder, 400f), 0.1f, "thunder is low");
            var bird = Ambience.Find(Region.Verdance, "one bird, far off");
            Assert.Less(bird.A * bird.Seconds, 2.1f, "one bird: once or twice a loop");
            var hum = Ambience.Render(Ambience.Find(Region.Emberdown, "the Roll-Call Bell's hum"));
            float heard = RollCallSong.PitchOf(hum, Ambience.SampleRate, 8192, 40f, 400f);
            Assert.AreEqual(0f, RollCallSong.Semitones(RollCallSong.TonicHz(Region.Emberdown), heard), 0.6f, "the Bell hums Emberdown's G");
        }

        [Test]
        public void TheDeliverablesAreRendered()
        {
            string dir = Path.Combine(RepoRoot, "docs/audio/ambience");
            Assert.IsTrue(Directory.Exists(dir), dir);
            foreach (var l in Ambience.Layers)
            {
                string file = Path.Combine(dir, l.FileName);
                Assert.IsTrue(File.Exists(file), l.FileName);
                var head = new byte[44];
                using (var fs = File.OpenRead(file)) Assert.AreEqual(44, fs.Read(head, 0, 44), file + " is a WAV, not an LFS pointer");
                var (rate, bits, _) = RollCallSong.WavHeader(head);
                Assert.AreEqual(48000, rate, file); Assert.AreEqual(24, bits, file);
            }
        }
    }
}
