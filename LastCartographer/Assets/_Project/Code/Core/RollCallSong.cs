using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OWSBG.Core
{
    /// <summary>
    /// The roll-call sung (AUD-02, docs/design/roll-call.md): the tune in <see cref="AudioDirection.RollCall"/> made into
    /// sound. A voice is a bird's timbre (its octave, its vowel, how it shakes and breathes); a form is the tune arranged
    /// for a use (Runa's verse at the Bell, the whale's slow one, the walk's call, the chorus that has everyone); the
    /// synth renders notes for a voice into samples, and a chorus is voices summed with a little drift between them.
    /// Pure C#: the tests render and measure it in edit mode, the singer in the game turns the samples into clips, and
    /// the exporter writes the same renders as WAV and MIDI for the composer to replace.
    /// </summary>
    public static class RollCallSong
    {
        public const int SampleRate = 48000;
        /// <summary>The tune is written an octave above the tonic's Hz for a middle voice; each voice moves from there.</summary>
        public const int WrittenOctave = 12;

        // ---- keys ----

        /// <summary>The tonic of a region's mode, as Hz of the low octave (D2 for Saltmarrow's D Dorian).</summary>
        public static float TonicHz(Region r) => r switch
        {
            Region.Saltmarrow => 73.42f,   // D2
            Region.Emberdown => 98.00f,    // G2
            Region.Verdance => 82.41f,     // E2
            Region.Halden => 65.41f,       // C2
            Region.Windreach => 110.00f,   // A2
            Region.Greyfold => 73.42f,     // one held tone: it borrows the coast's
            _ => 73.42f,                   // the Blank sings the coast's key, reversed
        };

        public static float Hz(float tonicHz, int semitones) => tonicHz * (float)Math.Pow(2.0, semitones / 12.0);

        // ---- voices ----

        /// <summary>How a bird sings: everything but the notes.</summary>
        public sealed class Voice
        {
            public string Id;
            /// <summary>Semitones from the written octave: a chick sings twelve up, a heron twelve down.</summary>
            public int Transpose;
            /// <summary>The vowel: the first two formants in Hz ("ah" is about 700/1100, "oh" 450/800).</summary>
            public float Formant1 = 700f, Formant2 = 1100f;
            /// <summary>How wide the formants are (Hz); wider is breathier, narrower is more nasal.</summary>
            public float FormantWidth = 120f;
            /// <summary>Vibrato: cycles a second, and depth in semitones.</summary>
            public float VibratoRate = 5f, VibratoDepth = 0.25f;
            /// <summary>Breath noise mixed in, 0..1.</summary>
            public float Breath = 0.05f;
            /// <summary>Seconds to open and to let go of a note.</summary>
            public float Attack = 0.08f, Release = 0.25f;
            /// <summary>Seconds a note slides from the one before (a singer's portamento).</summary>
            public float Glide = 0.05f;
            /// <summary>Cents off true, on purpose (a chorus is many voices a little apart).</summary>
            public float Detune;
            /// <summary>The whale: a sub-sine, no formants, a long attack, an echo (see <see cref="Render"/>).</summary>
            public bool Whale;
            /// <summary>Faded: greyer and breathier, a little flat (Ilse, the Remnant).</summary>
            public bool Faded;
        }

        static readonly Dictionary<string, Voice> _voices = new Dictionary<string, Voice>(StringComparer.OrdinalIgnoreCase);

        static Voice V(string id, int transpose, float f1, float f2, float width = 120f, float vibRate = 5f, float vibDepth = 0.25f, float breath = 0.05f, float attack = 0.08f, float release = 0.25f, float glide = 0.05f, bool faded = false)
        {
            var v = new Voice { Id = id, Transpose = transpose, Formant1 = f1, Formant2 = f2, FormantWidth = width, VibratoRate = vibRate, VibratoDepth = vibDepth, Breath = breath, Attack = attack, Release = release, Glide = glide, Faded = faded };
            _voices[id] = v;
            return v;
        }

        static RollCallSong()
        {
            // The cast by species (character-bibles.md; cast.py's birds). The tune is written for Runa's range.
            V("runa", 0, 720f, 1150f, 110f, 5.2f, 0.3f, 0.04f, 0.06f, 0.3f);                  // capercaillie: warm, loud, the lead
            V("kettil", -7, 600f, 950f, 160f, 4.2f, 0.45f, 0.10f, 0.12f, 0.35f);              // old capercaillie: lower, wavering
            V("dotha", 5, 800f, 1300f, 90f, 6.0f, 0.2f, 0.12f, 0.10f, 0.4f);                  // oystercatcher: thin, high, dry
            V("sable", -5, 520f, 900f, 140f, 3.8f, 0.15f, 0.08f, 0.05f, 0.2f);                // cormorant: low, flat, no ornament
            V("teodor", -3, 480f, 850f, 150f, 4.5f, 0.2f, 0.10f, 0.15f, 0.5f);                // mourning dove: soft, cooing
            V("pell", 4, 760f, 1250f, 100f, 6.5f, 0.2f, 0.03f, 0.03f, 0.15f);                 // jackdaw: quick, bright
            V("idrenne", -9, 560f, 1000f, 130f, 3.5f, 0.25f, 0.06f, 0.14f, 0.45f);            // crane: low and long
            V("maren", -2, 650f, 1050f, 80f, 4.8f, 0.35f, 0.02f, 0.10f, 0.35f);               // swan: clean, exact
            V("corvin", -12, 500f, 800f, 170f, 3.0f, 0.2f, 0.12f, 0.20f, 0.6f);               // great owl: the lowest, hollow
            V("voss", -10, 540f, 900f, 100f, 3.6f, 0.15f, 0.04f, 0.08f, 0.3f);                // grey heron: low, precise
            V("halvard", -8, 560f, 950f, 120f, 3.6f, 0.15f, 0.05f, 0.06f, 0.25f);             // heron
            V("isolde", 2, 700f, 1200f, 110f, 5.0f, 0.3f, 0.06f, 0.08f, 0.35f);               // curlew
            V("ilse", 7, 780f, 1350f, 200f, 5.5f, 0.2f, 0.25f, 0.15f, 0.5f, faded: true);     // wren, grey: small and breathy
            V("aury", -4, 520f, 900f, 180f, 3.8f, 0.15f, 0.20f, 0.10f, 0.4f, faded: true);    // Sable's brother, faded
            V("corra", 12, 900f, 1500f, 100f, 7.0f, 0.25f, 0.06f, 0.04f, 0.2f);               // heron chick: high, eager
            V("marrow", 12, 950f, 1600f, 120f, 7.5f, 0.3f, 0.08f, 0.05f, 0.25f);              // chick, grey: high
            V("family", -1, 680f, 1100f, 130f, 4.8f, 0.3f, 0.06f, 0.08f, 0.3f);               // the Holdfast, one of many
            V("remnant", 1, 640f, 1000f, 220f, 4.0f, 0.15f, 0.30f, 0.20f, 0.7f, faded: true); // the faded, under everything
            var whale = V("whale", -12, 0f, 0f, 0f, 0.35f, 0.6f, 0.0f, 0.9f, 1.6f, 0.4f);     // an octave down at the low tonic (D2 up), far off, slow: lower is under what small speakers carry
            whale.Whale = true;
        }

        /// <summary>A bird's voice by cast id; a stranger sings as one of the family.</summary>
        public static Voice VoiceOf(string id) => !string.IsNullOrEmpty(id) && _voices.TryGetValue(id, out var v) ? v : _voices["family"];
        public static IEnumerable<Voice> Voices => _voices.Values;
        /// <summary>Wren's voice is never heard (audio-direction 4): she has no entry, and asking for hers gets nobody.</summary>
        public static bool CanSing(string id) => !string.IsNullOrEmpty(id) && id != "wren" && _voices.ContainsKey(id);

        // ---- forms ----

        public enum Form
        {
            /// <summary>One name and the answer: the phrase.</summary>
            Whole,
            /// <summary>A verse: names then the answer (Runa at the Bell, the chorus).</summary>
            Verse,
            /// <summary>The call and the name as written (the Complete Survey's chorus keeping the beat).</summary>
            Call,
            /// <summary>The call as the walk sings it: the pickup half a beat, the name held half a beat to the next call, one walk beat in all.</summary>
            WalkCall,
            /// <summary>The name alone (the walk's miss: it falters).</summary>
            Name,
            /// <summary>The answer alone (the end of a walk's verse).</summary>
            Answer,
            /// <summary>The whole phrase, each note twice as long, at the Blank's beat (the whale).</summary>
            Whale,
            /// <summary>Backwards (the Blank's islands).</summary>
            Reversed,
            /// <summary>Upside down about the reciting tone (the Archivist).</summary>
            Inverted,
        }

        /// <summary>The notes a form asks for. A verse of n names is the call and name n times, then the answer.</summary>
        public static AudioDirection.Note[] Notes(Form form, int names = 3)
        {
            var rc = typeof(AudioDirection.RollCall);
            switch (form)
            {
                case Form.Verse:
                    var v = new List<AudioDirection.Note>();
                    for (int i = 0; i < Math.Max(1, names); i++) { v.AddRange(AudioDirection.RollCall.Call); v.AddRange(AudioDirection.RollCall.Name); }
                    v.AddRange(AudioDirection.RollCall.Answer);
                    return v.ToArray();
                case Form.Call: return AudioDirection.RollCall.Call.Concat(AudioDirection.RollCall.Name).ToArray();
                case Form.WalkCall: return AudioDirection.RollCall.Call.Concat(new[] { new AudioDirection.Note(AudioDirection.RollCall.Name[0].Pitch, 1f - AudioDirection.RollCall.PickupBeats) }).ToArray();
                case Form.Name: return AudioDirection.RollCall.Name;
                case Form.Answer: return AudioDirection.RollCall.Answer;
                case Form.Whale: return AudioDirection.RollCall.Augmented(AudioDirection.RollCall.Phrase, 2f);
                case Form.Reversed: return AudioDirection.RollCall.Reversed(AudioDirection.RollCall.Phrase);
                case Form.Inverted: return AudioDirection.RollCall.Inverted(AudioDirection.RollCall.Phrase);
                default: return AudioDirection.RollCall.Phrase;
            }
        }

        /// <summary>Seconds a form lasts at a beat, before the last note's release.</summary>
        public static float Seconds(Form form, float beat, int names = 3) => AudioDirection.RollCall.Beats(Notes(form, names)) * beat;

        // ---- the uses ----

        /// <summary>One place the roll-call is heard (audio-direction 3), as the singer plays it: &lt;&lt;sing id&gt;&gt; in a script.</summary>
        public sealed class Use
        {
            public string Id;
            public Form Form;
            /// <summary>The region whose tonic it is sung in.</summary>
            public Region Key;
            /// <summary>The region whose beat it keeps.</summary>
            public Region Tempo;
            /// <summary>Names in a verse; 0 is one for every voice.</summary>
            public int Names;
            /// <summary>Who sings; null is the true ending's roster from the world (<see cref="Chorus"/>).</summary>
            public string[] Voices;
            /// <summary>The caption a deaf player reads (audio-direction 9).</summary>
            public string Caption;
            /// <summary>How loud it plays against the rendered peak: the whale is far off, the Remnant under everything.</summary>
            public float Gain = 1f;
        }

        public static readonly Use[] Uses =
        {
            // The whale on the Bone Bridge: the coast's key, at the Blank's beat, two octaves down and far away.
            new Use { Id = "whale", Form = Form.Whale, Key = Region.Saltmarrow, Tempo = Region.Blank, Names = 1, Voices = new[] { "whale" }, Caption = "[far off, under the bridge: a song, slow, like names]", Gain = 0.45f },
            // Runa at the Bell, the Holdfast answering.
            new Use { Id = "runa", Form = Form.Verse, Key = Region.Emberdown, Tempo = Region.Emberdown, Names = 3, Voices = new[] { "runa", "family", "family", "family", "family" }, Caption = "[Runa sings the roll-call; the Holdfast answers]" },
            // Dotha alone, to the water.
            new Use { Id = "dotha", Form = Form.Whole, Key = Region.Saltmarrow, Tempo = Region.Saltmarrow, Names = 1, Voices = new[] { "dotha" }, Caption = "[Dotha sings it, alone, to the water]" },
            // The true ending: everyone met, in order, whole; the coast's key where it was first heard, Runa's beat since she leads.
            new Use { Id = "chorus", Form = Form.Verse, Key = Region.Saltmarrow, Tempo = Region.Emberdown, Names = 0, Voices = null, Caption = "[everyone she has met sings the roll-call, whole]" },
            // The Blank's islands: reversed, the Remnant under it (AUD-08 places it).
            new Use { Id = "blank", Form = Form.Reversed, Key = Region.Saltmarrow, Tempo = Region.Blank, Names = 1, Voices = new[] { "remnant", "remnant", "remnant" }, Caption = "[a song, the wrong way round]", Gain = 0.6f },
            // The Archivist: inverted about the reciting tone, in Halden's key (AUD-07 places it).
            new Use { Id = "archivist", Form = Form.Inverted, Key = Region.Halden, Tempo = Region.Halden, Names = 1, Voices = new[] { "corvin" }, Caption = "[the roll-call, pulled the other way]" },
        };

        public static Use UseOf(string id) => string.IsNullOrEmpty(id) ? null : Uses.FirstOrDefault(u => string.Equals(u.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        public static IList<string> VoicesFor(Use u, WorldState w) => u.Voices ?? (IList<string>)Chorus(w);
        public static int NamesFor(Use u, IList<string> voices) => u.Names > 0 ? u.Names : Math.Max(1, voices.Count);

        // ---- the choruses ----

        /// <summary>The walk's chorus by walk id: Merrow's End is Dotha alone; the Holdfast's walks are Runa, Kettil and the families.</summary>
        public static string[] WalkChorus(string walkId) => walkId switch
        {
            "merrows_end" => new[] { "dotha" },
            "hollowvein" => new[] { "runa", "family", "family", "family", "family" },
            _ => new[] { "runa", "kettil", "family", "family", "family" },
        };

        /// <summary>
        /// Who sings in the true ending, in the order met: the flags are the ones Runa's chorus scene checks
        /// (Observatory_Runa_Chorus), one voice per line she sings, Runa first and Isolde last. Wren never.
        /// </summary>
        public static readonly (string id, string flag)[] ChorusRoster =
        {
            ("sable", "saltmarrow.sable.talked"),
            ("dotha", "saltmarrow.dotha.met"),
            ("kettil", "emberdown.kettil.met"),
            ("teodor", "verdance.teodor.met"),
            ("pell", "halden.hall.pell_minder"),
            ("idrenne", "windreach.idrenne.met"),
            ("ilse", "blank.ilse.heard"),
            ("corra", "corra.decided"),
            ("voss", "return.voss.met"),
        };

        /// <summary>The true ending's voices for a game: Runa leads, everyone met follows in order, Isolde closes.</summary>
        public static List<string> Chorus(WorldState w)
        {
            var list = new List<string> { "runa" };
            foreach (var (id, flag) in ChorusRoster) if (w != null && w.Is(flag)) list.Add(id);
            list.Add("isolde");
            return list;
        }

        // ---- the synth ----

        /// <summary>A rendered line: mono samples at <see cref="SampleRate"/>.</summary>
        public sealed class Line
        {
            public float[] Samples;
            public float Seconds => Samples.Length / (float)SampleRate;
        }

        const int TableSize = 2048;
        const int Harmonics = 32;

        /// <summary>A note's spectrum through the voice's vowel, as one period (built per note, since the formants are fixed in Hz).</summary>
        static float[] Table(Voice v, float hz)
        {
            var t = new float[TableSize];
            if (v.Whale)
            {
                for (int i = 0; i < TableSize; i++)
                {
                    double ph = 2 * Math.PI * i / TableSize;
                    t[i] = (float)(Math.Sin(ph) + 0.5 * Math.Sin(2 * ph) + 0.12 * Math.Sin(3 * ph));
                }
                return t;
            }
            var amp = new float[Harmonics + 1];
            float w1 = v.FormantWidth, w2 = v.FormantWidth * 1.4f;
            for (int h = 1; h <= Harmonics; h++)
            {
                float f = hz * h;
                float d1 = (f - v.Formant1) / w1, d2 = (f - v.Formant2) / w2;
                float r1 = 1f / (1f + d1 * d1), r2 = 0.6f / (1f + d2 * d2);
                float tilt = 1f / h;                                    // a glottal source: 1/h
                float grey = v.Faded ? (float)Math.Exp(-f / 1500f) : 1f;   // the faded lose their highs
                amp[h] = tilt * (0.25f + r1 + r2) * grey;
            }
            for (int i = 0; i < TableSize; i++)
            {
                double ph = 2 * Math.PI * i / TableSize, s = 0;
                for (int h = 1; h <= Harmonics; h++) if (hz * h < SampleRate * 0.45f) s += amp[h] * Math.Sin(h * ph);
                t[i] = (float)s;
            }
            float peak = t.Max(x => Math.Abs(x));
            if (peak > 0f) for (int i = 0; i < TableSize; i++) t[i] /= peak;
            return t;
        }

        /// <summary>
        /// Render notes for one voice. <paramref name="tonicHz"/> is the region's low tonic; the tune sits an octave up
        /// and the voice transposes from there. <paramref name="falterAt"/>: the index of a note that falters (a miss in
        /// the walk): it breaks off after a third of its length and slips a semitone as it goes.
        /// </summary>
        public static Line Render(IList<AudioDirection.Note> notes, float tonicHz, float beat, Voice voice, int falterAt = -1, int seed = 0)
        {
            var rng = new Random(seed * 7919 + voice.Id.GetHashCode());
            float total = notes.Sum(n => n.Beats) * beat + voice.Release + 0.05f + (voice.Whale ? 1.2f : 0f);
            int len = (int)(total * SampleRate);
            var outp = new float[len];
            float t0 = 0f;
            double phase = 0;
            float prevHz = 0f;
            float detune = (float)Math.Pow(2.0, voice.Detune / 1200.0) * (voice.Faded ? 0.994f : 1f);
            float breath = 0f;
            const float BreathLp = 0.145f;   // the breath is coloured, not white: a one-pole low-pass near 1.2 kHz
            for (int ni = 0; ni < notes.Count; ni++)
            {
                var n = notes[ni];
                float hz = Hz(tonicHz, WrittenOctave + voice.Transpose + n.Pitch) * detune;
                float dur = n.Beats * beat;
                bool falter = ni == falterAt;
                float sung = falter ? dur * 0.35f : dur;
                var table = Table(voice, hz);
                int start = (int)(t0 * SampleRate);
                int hold = (int)(sung * SampleRate);
                int rel = (int)(voice.Release * SampleRate);
                int end = Math.Min(len, start + hold + rel);
                float from = prevHz > 0f ? prevHz : hz;
                int glide = (int)(voice.Glide * SampleRate);
                for (int i = start; i < end; i++)
                {
                    float tn = (i - start) / (float)SampleRate;
                    float env = tn < voice.Attack ? tn / voice.Attack : 1f;
                    if (i >= start + hold) env *= 1f - (i - start - hold) / (float)Math.Max(1, rel);
                    float f = glide > 0 && i - start < glide ? from + (hz - from) * (i - start) / glide : hz;
                    if (falter) f *= (float)Math.Pow(2.0, -Math.Min(1f, tn / Math.Max(0.01f, sung)) / 12.0);   // slips a semitone
                    float vib = (float)Math.Sin(2 * Math.PI * voice.VibratoRate * tn) * voice.VibratoDepth * Math.Min(1f, tn / 0.3f);
                    f *= (float)Math.Pow(2.0, vib / 12.0);
                    phase += f / SampleRate;
                    if (phase >= 1) phase -= 1;
                    float pos = (float)(phase * TableSize);
                    int i0 = (int)pos; int i1 = (i0 + 1) % TableSize;
                    float s = table[i0] + (table[i1] - table[i0]) * (pos - i0);
                    if (voice.Breath > 0f)
                    {
                        breath += (((float)rng.NextDouble() * 2f - 1f) - breath) * BreathLp;
                        s += breath * voice.Breath * 1.5f * env;
                    }
                    outp[i] += s * env * 0.5f;
                }
                prevHz = hz;
                t0 += dur;
            }
            if (voice.Whale) Echo(outp, 0.42f, 0.45f, 4);
            return new Line { Samples = outp };
        }

        /// <summary>A far-off room: the sound comes back softer, later, duller.</summary>
        static void Echo(float[] s, float delaySeconds, float gain, int taps)
        {
            int d = (int)(delaySeconds * SampleRate);
            for (int k = 1; k <= taps; k++)
            {
                float g = (float)Math.Pow(gain, k);
                for (int i = s.Length - 1; i >= k * d; i--) s[i] += s[i - k * d] * g;
            }
            float lp = 0f;
            for (int i = 0; i < s.Length; i++) { lp += (s[i] - lp) * 0.08f; s[i] = lp; }
        }

        /// <summary>Voices together, each a little apart in time and tuning, then brought under the peak (audio-direction 6: −1 dBTP).</summary>
        public static Line Chorus(IList<AudioDirection.Note> notes, float tonicHz, float beat, IList<string> voiceIds, int falterAt = -1, float peakDb = AudioDirection.SfxPeakDbtp)
        {
            var lines = new List<float[]>();
            int maxLen = 0;
            for (int k = 0; k < voiceIds.Count; k++)
            {
                var basis = VoiceOf(voiceIds[k]);
                var v = new Voice
                {
                    Id = basis.Id, Transpose = basis.Transpose, Formant1 = basis.Formant1, Formant2 = basis.Formant2, FormantWidth = basis.FormantWidth,
                    VibratoRate = basis.VibratoRate * (1f + 0.07f * ((k % 3) - 1)), VibratoDepth = basis.VibratoDepth, Breath = basis.Breath,
                    Attack = basis.Attack, Release = basis.Release, Glide = basis.Glide, Whale = basis.Whale, Faded = basis.Faded,
                    Detune = voiceIds.Count > 1 ? ((k * 37) % 21 - 10) : 0f,
                };
                var line = Render(notes, tonicHz, beat, v, falterAt, k);
                int offset = voiceIds.Count > 1 ? (int)(SampleRate * 0.012f * ((k * 5) % 4)) : 0;   // up to 36 ms apart: many, not one
                var shifted = new float[line.Samples.Length + offset];
                Array.Copy(line.Samples, 0, shifted, offset, line.Samples.Length);
                lines.Add(shifted);
                maxLen = Math.Max(maxLen, shifted.Length);
            }
            var sum = new float[maxLen];
            foreach (var l in lines) for (int i = 0; i < l.Length; i++) sum[i] += l[i];
            Normalize(sum, peakDb);
            return new Line { Samples = sum };
        }

        public static void Normalize(float[] s, float peakDb)
        {
            float peak = 0f;
            foreach (var x in s) peak = Math.Max(peak, Math.Abs(x));
            if (peak <= 0f) return;
            float target = (float)Math.Pow(10.0, peakDb / 20.0);
            float g = target / peak;
            for (int i = 0; i < s.Length; i++) s[i] *= g;
        }

        public static float PeakDb(float[] s)
        {
            float peak = 0f;
            foreach (var x in s) peak = Math.Max(peak, Math.Abs(x));
            return peak <= 0f ? -80f : 20f * (float)Math.Log10(peak);
        }

        /// <summary>The pitch of a stretch of samples in Hz by autocorrelation (the tests' ear); 0 where there is nothing to hear.</summary>
        public static float PitchOf(float[] s, int start, int count, float minHz = 40f, float maxHz = 2000f)
        {
            int end = Math.Min(s.Length, start + count);
            int n = end - start;
            if (n < SampleRate / minHz * 2) return 0f;
            float energy = 0f;
            for (int i = start; i < end; i++) energy += s[i] * s[i];
            if (energy < 1e-6f) return 0f;
            int minLag = (int)(SampleRate / maxHz), maxLag = (int)(SampleRate / minHz);
            float best = 0f; int bestLag = 0;
            for (int lag = minLag; lag <= maxLag && lag < n / 2; lag++)
            {
                float c = 0f;
                for (int i = start; i + lag < end; i++) c += s[i] * s[i + lag];
                c /= energy;
                if (c > best) { best = c; bestLag = lag; }
            }
            if (bestLag == 0) return 0f;
            // Prefer the shortest lag that is nearly as good: the fundamental, not an octave below it.
            for (int div = 4; div >= 2; div--)
            {
                int lag = bestLag / div;
                if (lag < minLag) continue;
                float c = 0f;
                for (int i = start; i + lag < end; i++) c += s[i] * s[i + lag];
                c /= energy;
                if (c > best * 0.9f) { bestLag = lag; break; }
            }
            return SampleRate / (float)bestLag;
        }

        /// <summary>Semitones between two frequencies.</summary>
        public static float Semitones(float fromHz, float toHz) => 12f * (float)Math.Log(toHz / fromHz, 2.0);

        /// <summary>How much of the energy sits above a frequency (0..1): the whale is dark, a chick bright.</summary>
        public static float EnergyAbove(float[] s, float hz)
        {
            // A one-pole high-pass at hz, energy of what passes over energy of everything.
            float rc = 1f / (2f * (float)Math.PI * hz), dt = 1f / SampleRate, a = rc / (rc + dt);
            float y = 0f, xPrev = 0f, hi = 0f, all = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                y = a * (y + s[i] - xPrev); xPrev = s[i];
                hi += y * y; all += s[i] * s[i];
            }
            return all <= 0f ? 0f : hi / all;
        }

        // ---- files ----

        /// <summary>A delivery name (audio-direction 6): region_kind_name_bpm.wav.</summary>
        public static string FileName(Region region, string kind, string name, float bpm) =>
            region.ToString().ToLowerInvariant() + "_" + kind + "_" + name + "_" + (int)Math.Round(bpm) + ".wav";

        /// <summary>Write mono 24-bit PCM at <see cref="SampleRate"/> (the delivery spec).</summary>
        public static byte[] Wav(float[] samples)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            int bytes = samples.Length * 3;
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes); w.Write(Encoding.ASCII.GetBytes("WAVE"));
            w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(SampleRate); w.Write(SampleRate * 3); w.Write((short)3); w.Write((short)24);
            w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (var s in samples)
            {
                int v = (int)Math.Round(Math.Max(-1f, Math.Min(1f, s)) * 8388607f);
                w.Write((byte)(v & 0xFF)); w.Write((byte)((v >> 8) & 0xFF)); w.Write((byte)((v >> 16) & 0xFF));
            }
            w.Flush();
            return ms.ToArray();
        }

        /// <summary>Read a WAV's sample rate and bits (the tests check the deliverables against the spec).</summary>
        public static (int rate, int bits, int channels) WavHeader(byte[] wav) =>
            (BitConverter.ToInt32(wav, 24), BitConverter.ToInt16(wav, 34), BitConverter.ToInt16(wav, 22));

        /// <summary>A MIDI track for the composer: one instrument, the notes as written (pitch from the region's tonic, MIDI note of the low tonic + written octave).</summary>
        public sealed class MidiTrack
        {
            public string Name;
            public int Program;
            public int Transpose;
            public IList<AudioDirection.Note> Notes;
        }

        /// <summary>The MIDI note of a region's low tonic (D2 is 38).</summary>
        public static int TonicMidi(Region r) => (int)Math.Round(69 + 12 * Math.Log(TonicHz(r) / 440.0, 2.0));

        /// <summary>A standard MIDI file (format 1, 480 ticks a beat) with the tempo and one track per voice: the hand-off for the score.</summary>
        public static byte[] Midi(float beatSeconds, int tonicMidi, IList<MidiTrack> tracks)
        {
            const int Ppq = 480;
            using var ms = new MemoryStream();
            void Chunk(string id, byte[] body)
            {
                ms.Write(Encoding.ASCII.GetBytes(id), 0, 4);
                ms.Write(new[] { (byte)(body.Length >> 24), (byte)(body.Length >> 16), (byte)(body.Length >> 8), (byte)body.Length }, 0, 4);
                ms.Write(body, 0, body.Length);
            }
            static void VarLen(List<byte> b, int v)
            {
                var stack = new List<byte> { (byte)(v & 0x7F) };
                v >>= 7;
                while (v > 0) { stack.Add((byte)((v & 0x7F) | 0x80)); v >>= 7; }
                stack.Reverse();
                b.AddRange(stack);
            }
            Chunk("MThd", new byte[] { 0, 1, (byte)((tracks.Count + 1) >> 8), (byte)((tracks.Count + 1) & 0xFF), (byte)(Ppq >> 8), (byte)(Ppq & 0xFF) });
            var tempo = new List<byte>();
            int us = (int)Math.Round(beatSeconds * 1_000_000);
            VarLen(tempo, 0); tempo.AddRange(new byte[] { 0xFF, 0x51, 3, (byte)(us >> 16), (byte)(us >> 8), (byte)us });
            VarLen(tempo, 0); tempo.AddRange(new byte[] { 0xFF, 0x2F, 0 });
            Chunk("MTrk", tempo.ToArray());
            for (int ch = 0; ch < tracks.Count; ch++)
            {
                var t = tracks[ch];
                var b = new List<byte>();
                var name = Encoding.ASCII.GetBytes(t.Name ?? "voice");
                VarLen(b, 0); b.AddRange(new byte[] { 0xFF, 0x03 }); VarLen(b, name.Length); b.AddRange(name);
                VarLen(b, 0); b.AddRange(new[] { (byte)(0xC0 | (ch % 16)), (byte)t.Program });
                foreach (var n in t.Notes)
                {
                    int note = tonicMidi + WrittenOctave + t.Transpose + n.Pitch;
                    int ticks = (int)Math.Round(n.Beats * Ppq);
                    VarLen(b, 0); b.AddRange(new[] { (byte)(0x90 | (ch % 16)), (byte)note, (byte)96 });
                    VarLen(b, ticks); b.AddRange(new[] { (byte)(0x80 | (ch % 16)), (byte)note, (byte)0 });
                }
                VarLen(b, 0); b.AddRange(new byte[] { 0xFF, 0x2F, 0 });
                Chunk("MTrk", b.ToArray());
            }
            return ms.ToArray();
        }
    }
}
