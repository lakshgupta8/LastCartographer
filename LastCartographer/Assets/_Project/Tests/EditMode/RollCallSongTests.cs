using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The roll-call sung (AUD-02, docs/design/roll-call.md): the synth sings the direction's notes in tune, a voice is a
    /// bird (the whale two octaves down and dark, a chick an octave up), the forms are the direction's forms, the true
    /// ending's chorus is whoever was met in the order Runa names them, a chorus sits under the peak and a miss
    /// falters, and the deliverables in docs/audio are the spec's files.
    /// </summary>
    public class RollCallSongTests
    {
        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        const float D2 = 73.42f;

        static float Expected(RollCallSong.Voice v, int pitch) => RollCallSong.Hz(D2, RollCallSong.WrittenOctave + v.Transpose + pitch);

        /// <summary>The pitch heard in the middle of a note, where the attack is over and the release not begun.</summary>
        static float Heard(float[] s, float startSeconds, float lengthSeconds, float maxHz = 2000f)
        {
            int window = Mathf.Min(8192, (int)(lengthSeconds * 0.5f * RollCallSong.SampleRate));
            int at = (int)((startSeconds + lengthSeconds * 0.5f) * RollCallSong.SampleRate) - window / 2;
            return RollCallSong.PitchOf(s, at, window, 30f, maxHz);
        }

        [Test]
        public void TheTuneIsSungInTune()
        {
            var runa = RollCallSong.VoiceOf("runa");
            var phrase = AudioDirection.RollCall.Phrase;
            float beat = AudioDirection.BeatOf(Region.Saltmarrow);
            var line = RollCallSong.Render(phrase, D2, beat, runa);
            Assert.GreaterOrEqual(line.Seconds, AudioDirection.RollCall.Beats(phrase) * beat, "long enough for every note and the release");
            float t = 0f;
            foreach (var n in phrase)
            {
                float heard = Heard(line.Samples, t, n.Beats * beat);
                Assert.Greater(heard, 0f, "something to hear at " + n);
                Assert.AreEqual(0f, RollCallSong.Semitones(Expected(runa, n.Pitch), heard), 0.6f, "note " + n + " in tune (vibrato allowed)");
                t += n.Beats * beat;
            }
        }

        [Test]
        public void AVoiceIsABird()
        {
            var name = AudioDirection.RollCall.Name;
            float beat = 1.2f;
            var runa = RollCallSong.Render(name, D2, beat, RollCallSong.VoiceOf("runa"));
            var corra = RollCallSong.Render(name, D2, beat, RollCallSong.VoiceOf("corra"));
            var corvin = RollCallSong.Render(name, D2, beat, RollCallSong.VoiceOf("corvin"));
            Assert.AreEqual(12f, RollCallSong.Semitones(Heard(runa.Samples, 0f, beat), Heard(corra.Samples, 0f, beat)), 0.7f, "a chick sings an octave up");
            Assert.AreEqual(-12f, RollCallSong.Semitones(Heard(runa.Samples, 0f, beat), Heard(corvin.Samples, 0f, beat, 600f)), 0.7f, "the owl an octave down");
            Assert.Less(RollCallSong.EnergyAbove(corvin.Samples, 800f), RollCallSong.EnergyAbove(corra.Samples, 800f), "and darker");

            var whaleVoice = RollCallSong.VoiceOf("whale");
            Assert.IsTrue(whaleVoice.Whale);
            Assert.AreEqual(-12, whaleVoice.Transpose, "an octave down: bones, but not under what a small speaker carries");
            var whaleNotes = RollCallSong.Notes(RollCallSong.Form.Whale);
            float blank = AudioDirection.BeatOf(Region.Blank);
            var whale = RollCallSong.Render(whaleNotes, D2, blank, whaleVoice);
            Assert.GreaterOrEqual(whale.Seconds, 2f * AudioDirection.RollCall.Beats(AudioDirection.RollCall.Phrase) * blank, "each note twice as long, at the Blank's beat");
            float second = whaleNotes[0].Beats * blank;   // the name, the longest held early note
            float heard = Heard(whale.Samples, second, whaleNotes[1].Beats * blank, 200f);
            Assert.AreEqual(0f, RollCallSong.Semitones(Expected(whaleVoice, whaleNotes[1].Pitch), heard), 1f, "the whale's name, an octave under Runa's");
            Assert.Less(RollCallSong.EnergyAbove(whale.Samples, 500f), 0.1f, "far off and low: nothing bright in it");

            Assert.IsFalse(RollCallSong.CanSing("wren"), "her voice is never heard");
            Assert.IsTrue(RollCallSong.VoiceOf("ilse").Faded && RollCallSong.VoiceOf("remnant").Faded, "the faded sing grey");
            Assert.AreSame(RollCallSong.VoiceOf("family"), RollCallSong.VoiceOf("nobody"), "a stranger sings as one of the family");
        }

        [Test]
        public void TheFormsAreTheDirectionsForms()
        {
            var phrase = AudioDirection.RollCall.Phrase;
            CollectionAssert.AreEqual(phrase, RollCallSong.Notes(RollCallSong.Form.Whole));
            CollectionAssert.AreEqual(AudioDirection.RollCall.Augmented(phrase, 2f), RollCallSong.Notes(RollCallSong.Form.Whale));
            CollectionAssert.AreEqual(AudioDirection.RollCall.Reversed(phrase), RollCallSong.Notes(RollCallSong.Form.Reversed));
            CollectionAssert.AreEqual(AudioDirection.RollCall.Inverted(phrase), RollCallSong.Notes(RollCallSong.Form.Inverted));
            var verse = RollCallSong.Notes(RollCallSong.Form.Verse, 3);
            Assert.AreEqual(3, verse.Count(n => n.Pitch == 9), "three pickups: three names");
            Assert.AreEqual(3 * 1.5f + AudioDirection.RollCall.Beats(AudioDirection.RollCall.Answer), AudioDirection.RollCall.Beats(verse), 1e-4f);
            Assert.AreEqual(0, verse.Last().Pitch, "and home");
            Assert.AreEqual(1f, AudioDirection.RollCall.Beats(RollCallSong.Notes(RollCallSong.Form.WalkCall)), 1e-4f, "the walk's call is one walk beat: the pickup, then the name held to the next call");
            Assert.AreEqual(AudioDirection.RollCall.PickupBeats, RollCallSong.Notes(RollCallSong.Form.WalkCall)[0].Beats, "called half a beat ahead");
            Assert.AreEqual(7, RollCallSong.Notes(RollCallSong.Form.Name).Single().Pitch, "a name is the reciting tone");
            Assert.AreEqual(4f * AudioDirection.BeatOf(Region.Saltmarrow), RollCallSong.Seconds(RollCallSong.Form.Answer, AudioDirection.BeatOf(Region.Saltmarrow)), 1e-4f,
                "the answer at the coast's beat is one Merrow's End walk beat: it fills the gap between verses");
            Assert.AreEqual(38, RollCallSong.TonicMidi(Region.Saltmarrow), "D2");
            Assert.AreEqual(43, RollCallSong.TonicMidi(Region.Emberdown), "G2");
        }

        [Test]
        public void TheChorusIsWhoeverWasMetInTheOrderRunaNamesThem()
        {
            var w = new WorldState();
            CollectionAssert.AreEqual(new[] { "runa", "isolde" }, RollCallSong.Chorus(w), "nobody met: Runa, and Isolde who is in there");
            w.Set("emberdown.kettil.met", true);
            w.Set("saltmarrow.sable.talked", true);
            w.Set("corra.decided", true);
            CollectionAssert.AreEqual(new[] { "runa", "sable", "kettil", "corra", "isolde" }, RollCallSong.Chorus(w), "in the order met, not the order set");
            foreach (var (id, _) in RollCallSong.ChorusRoster) Assert.IsTrue(RollCallSong.CanSing(id), id + " has a voice");
            CollectionAssert.DoesNotContain(RollCallSong.Chorus(w), "wren");

            // The roster is the script's: one flag per line Runa sings in Observatory_Runa_Chorus, in her order.
            var yarn = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Dialogue/Halden/Halden_Observatory_Endings.yarn"));
            var node = Regex.Match(yarn, @"title: Observatory_Runa_Chorus\r?\n---(.*?)===", RegexOptions.Singleline).Groups[1].Value;
            var flags = Regex.Matches(node, "has_flag\\(\"([^\"]+)\"\\)").Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
            CollectionAssert.AreEqual(RollCallSong.ChorusRoster.Select(r => r.flag).ToArray(), flags, "the chorus sings exactly who Runa names");
            StringAssert.Contains("<<sing chorus>>", node, "and the scene asks for the song");

            // The uses: the whale in the coast's key at the Blank's beat; only the true ending's roster comes from the world.
            var whale = RollCallSong.UseOf("whale");
            Assert.AreEqual(Region.Saltmarrow, whale.Key); Assert.AreEqual(Region.Blank, whale.Tempo);
            Assert.AreEqual(RollCallSong.Form.Whale, whale.Form);
            Assert.AreEqual(1, RollCallSong.Uses.Count(u => u.Voices == null), "only the true ending has everyone");
            Assert.AreEqual("chorus", RollCallSong.Uses.Single(u => u.Voices == null).Id);
            Assert.AreEqual(5, RollCallSong.NamesFor(RollCallSong.UseOf("chorus"), RollCallSong.Chorus(w)), "a name for every voice");
            Assert.IsNull(RollCallSong.UseOf("nothing")); Assert.IsNotNull(RollCallSong.UseOf("Whale"));
            foreach (var u in RollCallSong.Uses) Assert.IsNotEmpty(u.Caption, u.Id + " has a caption for a deaf player");
            CollectionAssert.AreEqual(new[] { "dotha" }, RollCallSong.WalkChorus("merrows_end"), "Dotha alone, nine songs short");
            Assert.AreEqual("runa", RollCallSong.WalkChorus("kettils_rest")[0], "Runa leads the Holdfast's");
            foreach (var f in new[] { "Emberdown/Emberdown_Bell_Runa.yarn", "Saltmarrow/Saltmarrow_MerrowsEnd_Dotha.yarn" })
                StringAssert.Contains("<<sing ", File.ReadAllText(Path.Combine(Application.dataPath, "_Project/Dialogue/" + f)), f + " sings");
        }

        [Test]
        public void AChorusIsManyVoicesUnderThePeakAndAMissFalters()
        {
            var phrase = AudioDirection.RollCall.Phrase;
            float beat = AudioDirection.BeatOf(Region.Saltmarrow);
            var three = RollCallSong.Chorus(phrase, D2, beat, new[] { "runa", "kettil", "dotha" });
            Assert.AreEqual(AudioDirection.SfxPeakDbtp, RollCallSong.PeakDb(three.Samples), 0.05f, "brought to the ceiling, never over it");
            var one = RollCallSong.Chorus(phrase, D2, beat, new[] { "runa" });
            Assert.AreEqual(AudioDirection.SfxPeakDbtp, RollCallSong.PeakDb(one.Samples), 0.05f);
            Assert.Greater(three.Samples.Length, one.Samples.Length, "voices a little apart: the chorus runs longer than one voice");
            Assert.AreEqual(0f, RollCallSong.Semitones(Expected(RollCallSong.VoiceOf("runa"), 7), Heard(one.Samples, 0.5f * beat, beat)), 0.6f, "one voice through the chorus is still in tune");
            Assert.Less(RollCallSong.EnergyAbove(three.Samples, 3000f), 0.1f, "the breath is coloured, not a hiss: little above 3 kHz");
            Assert.AreEqual(0.45f, RollCallSong.UseOf("whale").Gain, "the whale plays far off");

            var name = RollCallSong.Notes(RollCallSong.Form.Name);
            float walkBeat = beat * 4f;
            var sung = RollCallSong.Chorus(name, D2, walkBeat, new[] { "dotha" });
            var faltered = RollCallSong.Chorus(name, D2, walkBeat, new[] { "dotha" }, 0);
            int half = (int)(walkBeat * 0.5f * RollCallSong.SampleRate), quarter = half / 2;
            float Energy(float[] s, int from, int count) { float e = 0f; for (int i = from; i < from + count && i < s.Length; i++) e += s[i] * s[i]; return e; }
            Assert.Less(Energy(faltered.Samples, half, quarter), Energy(sung.Samples, half, quarter) * 0.05f, "a faltered name has broken off by its middle");
            float early = Heard(faltered.Samples, 0.02f, 0.2f), late = Heard(faltered.Samples, walkBeat * 0.3f - 0.1f, 0.1f);
            Assert.Less(late, early, "and it slips flat as it goes");
        }

        [Test]
        public void TheFilesAreTheSpecs()
        {
            var wav = RollCallSong.Wav(new[] { 0f, 0.5f, -0.5f, 1f });
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(wav, 0, 4));
            var (rate, bits, channels) = RollCallSong.WavHeader(wav);
            Assert.AreEqual(48000, rate, "48 kHz"); Assert.AreEqual(24, bits, "24-bit"); Assert.AreEqual(1, channels);
            Assert.AreEqual(44 + 4 * 3, wav.Length);
            Assert.AreEqual("saltmarrow_voice_rollcall-whale_33.wav", RollCallSong.FileName(Region.Saltmarrow, "voice", "rollcall-whale", 60f / AudioDirection.BeatOf(Region.Blank)));

            var midi = RollCallSong.Midi(0.9f, 38, new[] { new RollCallSong.MidiTrack { Name = "runa", Program = 52, Notes = AudioDirection.RollCall.Phrase } });
            Assert.AreEqual("MThd", System.Text.Encoding.ASCII.GetString(midi, 0, 4));
            Assert.AreEqual(2, midi[11], "the tempo track and one voice");
            Assert.AreEqual(480, (midi[12] << 8) | midi[13], "480 ticks a beat");
            int us = (midi[26] << 16) | (midi[27] << 8) | midi[28];
            Assert.AreEqual(900000, us, "the tempo is the beat in microseconds");
            int tracks = 0;
            for (int i = 0; i + 4 <= midi.Length; i++) if (midi[i] == 'M' && midi[i + 1] == 'T' && midi[i + 2] == 'r' && midi[i + 3] == 'k') tracks++;
            Assert.AreEqual(2, tracks);
        }

        [Test]
        public void TheDeliverablesAreRendered()
        {
            string dir = Path.Combine(RepoRoot, "docs/audio/rollcall");
            Assert.IsTrue(Directory.Exists(dir), dir);
            var wavs = Directory.GetFiles(dir, "*.wav").Select(Path.GetFileName).OrderBy(n => n).ToArray();
            foreach (var u in RollCallSong.Uses)
                Assert.IsTrue(wavs.Any(n => n.Contains("_rollcall-" + u.Id + "_")), u.Id + " is rendered");
            foreach (var piece in new[] { "walk-call", "walk-falter", "walk-answer" })
                Assert.IsTrue(wavs.Any(n => n.Contains("_rollcall-" + piece + "_")), "the walk's " + piece);
            foreach (var n in wavs)
            {
                StringAssert.IsMatch(@"^[a-z]+_voice_[a-z-]+_\d+\.wav$", n, "named region_kind_name_bpm");
                var head = new byte[44];
                using (var fs = File.OpenRead(Path.Combine(dir, n))) Assert.AreEqual(44, fs.Read(head, 0, 44), n + " is a WAV, not an LFS pointer");
                var (rate, bits, _) = RollCallSong.WavHeader(head);
                Assert.AreEqual(48000, rate, n); Assert.AreEqual(24, bits, n);
                Assert.IsTrue(File.Exists(Path.Combine(dir, Path.ChangeExtension(n, ".mid"))), n + " has its MIDI beside it");
            }
            StringAssert.Contains("rollcall-whale_33", string.Join(" ", wavs), "the whale at the Blank's beat");
            StringAssert.Contains("rollcall-runa_75", string.Join(" ", wavs), "Runa at Emberdown's");
            StringAssert.Contains("rollcall-walk-call_17", string.Join(" ", wavs), "the walk's call at the walk's beat");
        }
    }
}
