using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The score (AUD-04, docs/design/music.md): the coast's theme and the Lamp-Keeper's are scored to the direction
    /// (the stems it asks for, the coast's band, the mode, whole bars at the region's beat), the coast's rests are its
    /// silence share and the boss has none, the lead quotes the roll-call's answer in the mode, the boss theme adds a
    /// stem a phase and resolves on the answer in its key, the loops are seamless and under the peak, and the files
    /// are the spec's.
    /// </summary>
    public class ScoreTests
    {
        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [Test]
        public void TheCoastsThemesAreScoredToTheDirection()
        {
            var salt = Score.ThemeOf(Region.Saltmarrow);
            var lamp = Score.ThemeOfBoss("LampKeeper");
            Assert.IsNotNull(salt, "the Saltmarrow theme"); Assert.IsNotNull(lamp, "the Lamp-Keeper's");
            Assert.AreEqual(Region.Saltmarrow, lamp.Region, "her fight is in the coast's key");
            var band = AudioDirection.Of(Region.Saltmarrow).Instruments;
            foreach (var t in new[] { salt, lamp })
            {
                foreach (var need in Score.RequiredStems) Assert.IsNotNull(t.Stem(need), t.Id + " has a " + need + " stem (audio-direction 6)");
                Assert.AreEqual(AudioDirection.BeatOf(Region.Saltmarrow) * 4f, t.BarSeconds, 1e-4f, "bars of four beats at the region's beat");
                foreach (var s in t.Stems)
                {
                    Assert.IsNotEmpty(s.Notes, t.Id + "/" + s.Id);
                    var ins = Score.InstrumentOf(s.Instrument);
                    Assert.AreEqual(s.Instrument, ins.Id, t.Id + "/" + s.Id + " plays a known instrument");
                    if (ins.Id != "drone" && ins.Id != "bell")
                        Assert.IsTrue(band.Any(b => b.Contains(ins.Id)), ins.Id + " is in the coast's band: " + string.Join(", ", band));
                    foreach (var n in s.Notes)
                    {
                        Assert.IsTrue(Score.InMode(Region.Saltmarrow, Score.Semitones(Region.Saltmarrow, n.Degree)), t.Id + "/" + s.Id + " " + n + " is in D Dorian");
                        Assert.LessOrEqual(n.Start + n.Beats, t.Bars * Score.BeatsPerBar + 1e-3f, t.Id + "/" + s.Id + " " + n + " ends before the rest");
                        Assert.GreaterOrEqual(n.Start, 0f);
                    }
                }
            }
            Assert.AreEqual(AudioDirection.Of(Region.Saltmarrow).Silence, salt.Silence, 1e-4f, "the coast's rests are its silence share, by design");
            Assert.AreEqual(0f, lamp.Silence, "a boss theme never rests");
            Assert.AreEqual(1, salt.Stems.Count(s => s.Combat), "one combat drive");
            Assert.IsTrue(salt.Stems.All(s => s.Phase == 0)); Assert.IsTrue(lamp.Stems.All(s => !s.Combat));
            Assert.AreEqual(36f, salt.LoopSeconds, 1e-3f, "ten bars: eight sounding, two of rest");
        }

        [Test]
        public void TheLeadQuotesTheRollCallInTheMode()
        {
            var answer = Score.AnswerDegrees(Region.Saltmarrow);
            CollectionAssert.AreEqual(new[] { 4, 2, 1, 0 }, answer, "fifth, third (bent into Dorian), second, tonic");
            var lead = Score.ThemeOf(Region.Saltmarrow).Stem("lead").Notes.OrderBy(n => n.Start).Select(n => n.Degree).ToArray();
            bool quoted = Enumerable.Range(0, lead.Length - 3).Any(i => lead.Skip(i).Take(4).SequenceEqual(answer));
            Assert.IsTrue(quoted, "the fiddle quotes the roll-call's answer: " + string.Join(" ", lead));
            var first = Score.ThemeOf(Region.Saltmarrow).Stem("lead").Notes.Min(n => n.Start);
            Assert.GreaterOrEqual(first, 8f, "a fiddle that waits for the tide: two bars");
            Assert.AreEqual(0, Score.ThemeOf(Region.Saltmarrow).Stem("lead").Notes.OrderBy(n => n.Start).Last().Degree, "and comes home");
            Assert.AreEqual(4, Score.DegreeOf(Region.Saltmarrow, 7)); Assert.AreEqual(2, Score.DegreeOf(Region.Saltmarrow, 4), "a major third has no home in Dorian: the minor third under it");
            Assert.AreEqual(7, Score.DegreeOf(Region.Saltmarrow, 12), "the octave");
            Assert.AreEqual(12, Score.Semitones(Region.Saltmarrow, 7)); Assert.AreEqual(-2, Score.Semitones(Region.Saltmarrow, -1), "the degree under the tonic");
        }

        [Test]
        public void TheLoopsAreWholeBarsSeamlessAndUnderThePeakAndTheRestsAreTheSilence()
        {
            var salt = Score.ThemeOf(Region.Saltmarrow);
            var stems = Score.Render(salt);
            int len = (int)Math.Round(salt.LoopSeconds * Score.SampleRate);
            foreach (var kv in stems) Assert.AreEqual(len, kv.Value.Length, kv.Key + " is exactly the loop");
            var mix = new float[len];
            foreach (var s in stems.Values) for (int i = 0; i < len; i++) mix[i] += s[i];
            Assert.AreEqual(AudioDirection.SfxPeakDbtp, RollCallSong.PeakDb(mix), 0.05f, "the stems together peak at the ceiling");
            float silence = Score.MeasuredSilence(stems.Values);
            Assert.AreEqual(salt.Silence, silence, 0.06f, "a fifth of the loop is the room alone (measured " + silence + ")");
            // Seamless: the last quarter-second and the first are both quiet (the rest runs into the loop's start).
            int q = Score.SampleRate / 4;
            float head = 0f, tail = 0f;
            for (int i = 0; i < q; i++) { head += mix[i] * mix[i]; tail += mix[len - 1 - i] * mix[len - 1 - i]; }
            Assert.Less(Math.Sqrt(tail / q), 0.02f, "quiet at the join");
            var again = Score.RenderStem(salt, salt.Stem("lead"), RollCallSong.TonicHz(Region.Saltmarrow));
            var once = Score.RenderStem(salt, salt.Stem("lead"), RollCallSong.TonicHz(Region.Saltmarrow));
            CollectionAssert.AreEqual(once, again, "a render is the same every time");
            // The fiddle's first note is the fifth, an octave above the low tonic: A3.
            float heard = RollCallSong.PitchOf(once, (int)((8f * 0.9f + 1f) * Score.SampleRate), 8192, 100f, 1000f);
            Assert.AreEqual(0f, RollCallSong.Semitones(RollCallSong.Hz(RollCallSong.TonicHz(Region.Saltmarrow), 12 + 7), heard), 0.6f, "the fiddle waits, then the fifth");
        }

        [Test]
        public void TheBossThemeAddsAStemAPhaseAndResolvesOnTheAnswer()
        {
            var lamp = Score.ThemeOfBoss("LampKeeper");
            var byPhase = lamp.Stems.GroupBy(s => s.Phase).OrderBy(g => g.Key).Select(g => g.Key).ToArray();
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, byPhase, "a layer enters at each of the three phases");
            CollectionAssert.AreEquivalent(new[] { "bed", "pulse", "lead" }, lamp.Stems.Where(s => s.Phase == 1).Select(s => s.Id), "the fight opens with the drone, the drum and the fiddle");
            Assert.AreEqual("voices", lamp.Stems.Single(s => s.Phase == 2).Id, "the lamp splits: a second line");
            Assert.AreEqual("bells", lamp.Stems.Single(s => s.Phase == 3).Id, "the lamp gutters: a toll");
            Assert.AreEqual(28.8f, lamp.LoopSeconds, 1e-3f, "eight bars, no rest");
            var res = Score.Resolution(Region.Saltmarrow);
            Assert.AreEqual(7.2f, res.Length / (float)Score.SampleRate, 1e-3f, "two bars: the answer and its ring");
            float tonic = RollCallSong.TonicHz(Region.Saltmarrow);
            float first = RollCallSong.PitchOf(res, (int)(0.15f * Score.SampleRate), 8192, 100f, 1000f);
            float last = RollCallSong.PitchOf(res, (int)(2.6f * Score.SampleRate), 8192, 100f, 1000f);
            Assert.AreEqual(0f, RollCallSong.Semitones(RollCallSong.Hz(tonic, 12 + 7), first), 0.7f, "the answer starts on the fifth");
            Assert.AreEqual(0f, RollCallSong.Semitones(RollCallSong.Hz(tonic, 12), last), 0.7f, "and comes home to the tonic, in the boss's region's key");
        }

        [Test]
        public void TheFilesAreTheSpecAndTheDeliverablesAreRendered()
        {
            var salt = Score.ThemeOf(Region.Saltmarrow);
            var lamp = Score.ThemeOfBoss("LampKeeper");
            Assert.AreEqual("saltmarrow_music_bed_67.wav", Score.FileName(salt, salt.Stem("bed")));
            Assert.AreEqual("saltmarrow_music_lampkeeper-lead_67.wav", Score.FileName(lamp, lamp.Stem("lead")));
            var midi = Score.Midi(salt);
            Assert.AreEqual("MThd", System.Text.Encoding.ASCII.GetString(midi, 0, 4));
            Assert.AreEqual(salt.Stems.Count + 1, midi[11], "the tempo track and one a stem");
            Assert.AreEqual(900000, (midi[26] << 16) | (midi[27] << 8) | midi[28], "the coast's beat as the tempo");

            string dir = Path.Combine(RepoRoot, "docs/audio/music");
            Assert.IsTrue(Directory.Exists(dir), dir);
            foreach (var t in new[] { salt, lamp })
            {
                foreach (var s in t.Stems)
                {
                    string file = Path.Combine(dir, Score.FileName(t, s));
                    Assert.IsTrue(File.Exists(file), file);
                    var head = new byte[44];
                    using (var fs = File.OpenRead(file)) Assert.AreEqual(44, fs.Read(head, 0, 44), file + " is a WAV, not an LFS pointer");
                    var (rate, bits, _) = RollCallSong.WavHeader(head);
                    Assert.AreEqual(48000, rate, file); Assert.AreEqual(24, bits, file);
                }
                Assert.IsTrue(File.Exists(Path.Combine(dir, "saltmarrow_music_" + t.Id + "_67.mid")), t.Id + " as MIDI");
            }
            Assert.IsTrue(File.Exists(Path.Combine(dir, "saltmarrow_music_resolution_67.wav")), "the resolution");
        }
    }
}
