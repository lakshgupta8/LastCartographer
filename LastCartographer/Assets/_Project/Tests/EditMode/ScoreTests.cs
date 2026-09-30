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

        static readonly Region[] Others = { Region.Emberdown, Region.Verdance, Region.Halden, Region.Windreach };

        [Test]
        public void TheOtherRegionsThemesAreScoredToTheirRowsOfTheTable()
        {
            // AUD-06: each to its mode, beat, band and silence share; whole bars; every note before the rest; one combat drive each.
            foreach (var r in Others)
            {
                var t = Score.ThemeOf(r);
                Assert.IsNotNull(t, r + "'s theme");
                Assert.AreEqual(r, t.Region); Assert.IsNull(t.Boss);
                foreach (var need in Score.RequiredStems) Assert.IsNotNull(t.Stem(need), t.Id + " has a " + need + " stem (audio-direction 6)");
                Assert.AreEqual(AudioDirection.BeatOf(r) * 4f, t.BarSeconds, 1e-4f, r + ": bars of four at the region's beat");
                Assert.AreEqual(1, t.Stems.Count(s => s.Combat), r + ": one combat drive");
                Assert.IsTrue(t.Stems.All(s => s.Phase == 0), r + ": no phases in a region theme");
                var band = Score.BandOf(r);
                Assert.AreEqual(AudioDirection.Of(r).Instruments.Length, band.Length, r + "'s band is the direction's, instrument for instrument");
                foreach (var s in t.Stems)
                {
                    Assert.IsNotEmpty(s.Notes, t.Id + "/" + s.Id);
                    Assert.AreEqual(s.Instrument, Score.InstrumentOf(s.Instrument).Id, t.Id + "/" + s.Id + " plays a known instrument");
                    CollectionAssert.Contains(band, s.Instrument, t.Id + "/" + s.Id + " plays from " + r + "'s band");
                    foreach (var n in s.Notes)
                    {
                        Assert.IsTrue(Score.InMode(r, Score.Semitones(r, n.Degree)), t.Id + "/" + s.Id + " " + n + " is in " + AudioDirection.Of(r).Mode);
                        Assert.LessOrEqual(n.Start + n.Beats, t.Bars * Score.BeatsPerBar + 1e-3f, t.Id + "/" + s.Id + " " + n + " ends before the rest");
                        Assert.GreaterOrEqual(n.Start, 0f);
                    }
                }
                Assert.AreEqual(AudioDirection.Of(r).Silence, t.Silence, 0.03f, r + "'s rests are its silence share as near as whole bars come (designed " + t.Silence + ")");
            }
            Assert.AreEqual(0.1f, Score.ThemeOf(Region.Emberdown).Silence, 1e-4f); Assert.AreEqual(0.7f, Score.ThemeOf(Region.Verdance).Silence, 1e-4f);
            Assert.AreEqual(0.15f, Score.ThemeOf(Region.Halden).Silence, 1e-4f); Assert.AreEqual(0.375f, Score.ThemeOf(Region.Windreach).Silence, 1e-4f);
            Assert.AreEqual(5, Score.Mode(Region.Windreach).Length, "pentatonic: five degrees, the sixth the octave");
            Assert.AreEqual(12, Score.Semitones(Region.Windreach, 5));
        }

        [Test]
        public void EachThemeKeepsItsBrief()
        {
            // Emberdown: the anvil keeps the count, on two and four of every sounding bar; everyone sings (the chorus is the lead).
            var ember = Score.ThemeOf(Region.Emberdown);
            var anvil = ember.Stem("count");
            Assert.IsNotNull(anvil); Assert.AreEqual("anvil", anvil.Instrument);
            for (int bar = 0; bar < ember.Bars; bar++)
            {
                Assert.IsTrue(anvil.Notes.Any(n => Mathf.Approximately(n.Start, bar * 4f + 1f)), "the anvil on two of bar " + (bar + 1));
                Assert.IsTrue(anvil.Notes.Any(n => Mathf.Approximately(n.Start, bar * 4f + 3f)), "and on four");
            }
            Assert.AreEqual("choir", ember.Stem("lead").Instrument, "nobody solos: the lead is the chorus");
            Assert.AreEqual(0, ember.Stem("lead").Notes.OrderBy(n => n.Start).Last().Degree, "the work-song comes home");
            // The Verdance: mostly nothing, one bowed voice, and it stops before it resolves.
            var verd = Score.ThemeOf(Region.Verdance);
            Assert.AreEqual("gamba", verd.Stem("lead").Instrument);
            Assert.AreEqual(1, verd.Stem("lead").Notes.OrderBy(n => n.Start).Last().Degree, "stops on the flat second, unresolved");
            Assert.AreEqual(3, verd.Bars); Assert.AreEqual(7, verd.RestBars, "seven bars of the very large room");
            // Halden: a bar it never finishes; the fifth never comes home.
            var hald = Score.ThemeOf(Region.Halden);
            Assert.AreEqual(17, hald.Bars, "four phrases and the bar that is cut");
            var lead = hald.Stem("lead").Notes.OrderBy(n => n.Start).ToList();
            Assert.AreNotEqual(0, ((lead.Last().Degree % 7) + 7) % 7, "the loop's last note is not the tonic");
            Assert.Less(lead.Last().Start + lead.Last().Beats, 17 * 4f - 1f, "the seventeenth bar stops short");
            var bass = hald.Stem("bed").Notes.OrderBy(n => n.Start).Select(n => ((n.Degree % 7) + 7) % 7).ToList();
            for (int i = 0; i + 1 < bass.Count; i++) if (bass[i] == 4) Assert.AreEqual(5, bass[i + 1], "V goes to vi, never to I: the cadence that never comes");
            Assert.AreEqual("musicbox", hald.Stem("pulse").Instrument, "the clockwork ticks");
            // Windreach: a long flute, wide and slow.
            var wind = Score.ThemeOf(Region.Windreach);
            Assert.AreEqual("flute", wind.Stem("lead").Instrument);
            Assert.GreaterOrEqual(wind.Stem("lead").Notes.Average(n => n.Beats), 1.5f, "long notes");
            Assert.AreEqual("handdrum", wind.Stem("pulse").Instrument, "the camp's drum");
        }

        [Test]
        public void TheOtherThemesRenderToTheirLoopsAndTheirSilences()
        {
            foreach (var r in Others)
            {
                var t = Score.ThemeOf(r);
                var stems = Score.Render(t);
                int len = (int)Math.Round(t.LoopSeconds * Score.SampleRate);
                foreach (var kv in stems) Assert.AreEqual(len, kv.Value.Length, t.Id + "/" + kv.Key + " is exactly the loop");
                var mix = new float[len];
                foreach (var s in stems.Values) for (int i = 0; i < len; i++) mix[i] += s[i];
                Assert.AreEqual(AudioDirection.SfxPeakDbtp, RollCallSong.PeakDb(mix), 0.05f, t.Id + ": the stems together peak at the ceiling");
                float silence = Score.MeasuredSilence(stems.Values);
                Assert.AreEqual(t.Silence, silence, 0.06f, t.Id + ": the rests are the silence, measured " + silence);
                int q = Score.SampleRate / 4;
                float tail = 0f;
                for (int i = 0; i < q; i++) tail += mix[len - 1 - i] * mix[len - 1 - i];
                Assert.Less(Math.Sqrt(tail / q), 0.02f, t.Id + ": quiet at the join");
            }
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
            // AUD-06: the other regions' themes and resolutions.
            var bpm = new System.Collections.Generic.Dictionary<Region, int> { [Region.Emberdown] = 75, [Region.Verdance] = 50, [Region.Halden] = 100, [Region.Windreach] = 60 };
            foreach (var r in Others)
            {
                var t = Score.ThemeOf(r);
                string region = r.ToString().ToLowerInvariant();
                Assert.AreEqual(region + "_music_lead_" + bpm[r] + ".wav", Score.FileName(t, t.Stem("lead")));
                foreach (var s in t.Stems) Assert.IsTrue(File.Exists(Path.Combine(dir, Score.FileName(t, s))), Score.FileName(t, s));
                Assert.IsTrue(File.Exists(Path.Combine(dir, region + "_music_all-stems_" + bpm[r] + ".wav")), region + " mix");
                Assert.IsTrue(File.Exists(Path.Combine(dir, region + "_music_" + t.Id + "_" + bpm[r] + ".mid")), t.Id + " as MIDI");
                Assert.IsTrue(File.Exists(Path.Combine(dir, region + "_music_resolution_" + bpm[r] + ".wav")), region + "'s resolution");
            }
        }
    }
}
