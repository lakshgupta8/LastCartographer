using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OWSBG.Core
{
    /// <summary>
    /// The score (AUD-04, docs/design/music.md): a theme is stems (lead, bed, pulse, voices, and a combat drive),
    /// each a list of notes in the region's mode at the region's beat, looping on whole bars; an instrument is a
    /// timbre the synth renders a note through. The region theme bakes its rests into the loop, so the direction's
    /// silence share is a number the tests can measure; a boss theme has none and adds a stem a phase. Pure C#:
    /// <c>MusicDriver</c> renders the stems into clips on a worker thread and plays them in step, and the exporter
    /// writes the same renders as WAV and MIDI for the composer to replace.
    /// </summary>
    public static class Score
    {
        public const int SampleRate = RollCallSong.SampleRate;
        public const int BeatsPerBar = 4;

        // ---- modes ----

        /// <summary>A mode as semitones above the tonic for its seven degrees.</summary>
        public static int[] Mode(Region r) => r switch
        {
            Region.Saltmarrow => new[] { 0, 2, 3, 5, 7, 9, 10 },    // D Dorian
            Region.Emberdown => new[] { 0, 2, 4, 5, 7, 9, 10 },     // G Mixolydian
            Region.Verdance => new[] { 0, 1, 3, 5, 7, 8, 10 },      // E Phrygian
            Region.Halden => new[] { 0, 2, 4, 5, 7, 9, 11 },        // C major
            Region.Windreach => new[] { 0, 2, 4, 7, 9 },            // A pentatonic
            _ => new[] { 0, 2, 3, 5, 7, 9, 10 },
        };

        public static bool InMode(Region r, int semitones) => Mode(r).Contains(((semitones % 12) + 12) % 12);

        /// <summary>The degree a pitch falls on in a mode, or the one just under it: how a Dorian fiddle quotes a tune with a major third.</summary>
        public static int DegreeOf(Region r, int semitones)
        {
            var mode = Mode(r);
            int oct = (int)Math.Floor(semitones / 12.0), pc = ((semitones % 12) + 12) % 12, best = 0;
            for (int i = 0; i < mode.Length; i++) if (mode[i] <= pc) best = i;
            return best + oct * mode.Length;
        }

        /// <summary>The roll-call's answer as a mode's degrees (audio-direction 3: regional themes may quote it).</summary>
        public static int[] AnswerDegrees(Region r) => AudioDirection.RollCall.Answer.Select(n => DegreeOf(r, n.Pitch)).ToArray();

        /// <summary>A hash that is the same in every process (string.GetHashCode is not), so a render is the same every time.</summary>
        public static int Stable(string s)
        {
            unchecked { uint h = 2166136261; foreach (char c in s) { h ^= c; h *= 16777619; } return (int)h; }
        }

        // ---- instruments ----

        /// <summary>How an instrument sounds: its harmonic recipe and how a note opens, holds and lets go.</summary>
        public sealed class Instrument
        {
            public string Id;
            /// <summary>Weight of each harmonic, from the fundamental.</summary>
            public float[] Harmonics;
            /// <summary>Seconds to open; how long the held part decays toward <see cref="Sustain"/>; the level it holds at; seconds to let go.</summary>
            public float Attack = 0.02f, Decay = 0.3f, Sustain = 0.8f, Release = 0.2f;
            public float VibratoRate = 5f, VibratoDepth = 0f;
            /// <summary>Bow or breath noise, 0..1, coloured.</summary>
            public float Noise;
            /// <summary>A second oscillator this many cents off (the concertina's paired reeds); 0 for none.</summary>
            public float Detune;
            /// <summary>Partials this far from harmonic (a struck drum); 0 is harmonic.</summary>
            public float Inharmonic;
            /// <summary>A one-pole low-pass on the output, Hz; 0 for none (the drone's darkness).</summary>
            public float LowPass;
            /// <summary>Semitones from written: the drone sits two octaves under the fiddle.</summary>
            public int Transpose;
        }

        static readonly Dictionary<string, Instrument> _instruments = new Dictionary<string, Instrument>();
        static Instrument I(Instrument i) { _instruments[i.Id] = i; return i; }
        public static Instrument InstrumentOf(string id) => _instruments.TryGetValue(id, out var i) ? i : _instruments["fiddle"];
        public static IEnumerable<Instrument> Instruments => _instruments.Values;

        static Score()
        {
            // The coast's band (audio-direction 2): hardanger fiddle, low whistle, tongue drum, concertina, bowed psaltery.
            I(new Instrument { Id = "fiddle", Harmonics = Saw(18, 1.0f), Attack = 0.09f, Decay = 0.4f, Sustain = 0.85f, Release = 0.25f, VibratoRate = 5.5f, VibratoDepth = 0.18f, Noise = 0.06f, Detune = 4f, Transpose = 0 });
            I(new Instrument { Id = "whistle", Harmonics = new[] { 1f, 0.35f, 0.12f, 0.08f, 0.03f }, Attack = 0.06f, Decay = 0.5f, Sustain = 0.8f, Release = 0.18f, VibratoRate = 4.5f, VibratoDepth = 0.12f, Noise = 0.1f, Transpose = 0 });
            I(new Instrument { Id = "drum", Harmonics = new[] { 1f, 0.5f, 0.15f, 0.08f }, Attack = 0.002f, Decay = 0.35f, Sustain = 0f, Release = 0.3f, Inharmonic = 0.03f, LowPass = 1800f, Transpose = -12 });
            I(new Instrument { Id = "concertina", Harmonics = Square(12), Attack = 0.05f, Decay = 0.2f, Sustain = 0.9f, Release = 0.15f, Detune = 9f, Noise = 0.02f, Transpose = 0 });
            I(new Instrument { Id = "psaltery", Harmonics = Saw(24, 0.7f), Attack = 0.12f, Decay = 0.8f, Sustain = 0.6f, Release = 0.5f, VibratoRate = 0.3f, VibratoDepth = 0.05f, Noise = 0.03f, Transpose = 0 });
            I(new Instrument { Id = "drone", Harmonics = new[] { 1f, 0.6f, 0.35f, 0.25f, 0.15f, 0.1f, 0.07f, 0.05f }, Attack = 0.8f, Decay = 1f, Sustain = 1f, Release = 1.2f, Detune = 6f, LowPass = 700f, Transpose = -24 });
            I(new Instrument { Id = "bell", Harmonics = new[] { 1f, 0.6f, 0f, 0.35f, 0f, 0.2f, 0f, 0.1f }, Attack = 0.003f, Decay = 1.2f, Sustain = 0.1f, Release = 0.8f, Inharmonic = 0.02f, Transpose = 12 });
            // The other regions' bands (AUD-06, audio-direction 2).
            // Emberdown: work-song chorus, hurdy-gurdy, frame drum, anvil, tuba.
            I(new Instrument { Id = "choir", Harmonics = new[] { 1f, 0.9f, 0.7f, 0.8f, 0.35f, 0.2f, 0.12f, 0.08f, 0.05f }, Attack = 0.16f, Decay = 0.6f, Sustain = 0.9f, Release = 0.3f, VibratoRate = 5f, VibratoDepth = 0.1f, Noise = 0.07f, Detune = 11f });
            I(new Instrument { Id = "hurdygurdy", Harmonics = Saw(22, 0.8f), Attack = 0.05f, Decay = 0.3f, Sustain = 0.95f, Release = 0.12f, VibratoRate = 6.5f, VibratoDepth = 0.03f, Noise = 0.05f, Detune = 7f });
            I(new Instrument { Id = "framedrum", Harmonics = new[] { 1f, 0.35f, 0.12f }, Attack = 0.002f, Decay = 0.22f, Sustain = 0f, Release = 0.25f, Inharmonic = 0.05f, LowPass = 900f, Transpose = -12 });
            I(new Instrument { Id = "anvil", Harmonics = new[] { 1f, 0f, 0.8f, 0f, 0f, 0.6f, 0f, 0f, 0.4f, 0f, 0f, 0.3f }, Attack = 0.001f, Decay = 0.5f, Sustain = 0f, Release = 0.35f, Inharmonic = 0.14f, Transpose = 12 });
            I(new Instrument { Id = "tuba", Harmonics = new[] { 1f, 0.7f, 0.45f, 0.3f, 0.15f, 0.08f }, Attack = 0.08f, Decay = 0.4f, Sustain = 0.9f, Release = 0.2f, Noise = 0.03f, LowPass = 1200f, Transpose = -12 });
            // The Verdance: viola da gamba (harmonics), bowed glass, the root-chapel's organ pedal, Cantor handbells.
            I(new Instrument { Id = "gamba", Harmonics = Saw(14, 1.2f), Attack = 0.3f, Decay = 0.8f, Sustain = 0.85f, Release = 0.5f, VibratoRate = 4.2f, VibratoDepth = 0.06f, Noise = 0.06f, Transpose = 12 });
            I(new Instrument { Id = "glass", Harmonics = new[] { 1f, 0.05f, 0.02f }, Attack = 0.6f, Decay = 1f, Sustain = 1f, Release = 1.2f, Detune = 3f, Transpose = 12 });
            I(new Instrument { Id = "organ", Harmonics = new[] { 1f, 0.5f, 0.6f, 0.3f, 0.4f, 0.2f }, Attack = 0.3f, Decay = 1f, Sustain = 1f, Release = 0.7f, LowPass = 500f, Transpose = -24 });
            I(new Instrument { Id = "handbell", Harmonics = new[] { 1f, 0.4f, 0f, 0.6f, 0f, 0.25f, 0f, 0.15f }, Attack = 0.002f, Decay = 1.4f, Sustain = 0.05f, Release = 1f, Inharmonic = 0.03f, Transpose = 12 });
            // Halden: harpsichord, string quartet, music box, Guild brass.
            I(new Instrument { Id = "harpsichord", Harmonics = Saw(30, 0.6f), Attack = 0.002f, Decay = 0.45f, Sustain = 0.12f, Release = 0.08f, Detune = 2f });
            I(new Instrument { Id = "strings", Harmonics = Saw(16, 1.1f), Attack = 0.2f, Decay = 0.6f, Sustain = 0.9f, Release = 0.4f, VibratoRate = 5f, VibratoDepth = 0.1f, Noise = 0.03f, Detune = 5f });
            I(new Instrument { Id = "musicbox", Harmonics = new[] { 1f, 0.3f, 0.6f, 0.1f, 0.2f }, Attack = 0.001f, Decay = 0.4f, Sustain = 0f, Release = 0.3f, Inharmonic = 0.02f, Transpose = 24 });
            I(new Instrument { Id = "brass", Harmonics = Saw(10, 0.9f), Attack = 0.06f, Decay = 0.3f, Sustain = 0.85f, Release = 0.15f, Noise = 0.02f, LowPass = 3000f });
            // Windreach: long flute, cittern, overtone voice, wind harp, the hand drum at the fire.
            I(new Instrument { Id = "flute", Harmonics = new[] { 1f, 0.25f, 0.1f, 0.05f }, Attack = 0.12f, Decay = 0.5f, Sustain = 0.85f, Release = 0.25f, VibratoRate = 4.8f, VibratoDepth = 0.12f, Noise = 0.15f, Transpose = 12 });
            I(new Instrument { Id = "cittern", Harmonics = Saw(20, 0.8f), Attack = 0.003f, Decay = 0.6f, Sustain = 0.1f, Release = 0.2f, Detune = 5f });
            I(new Instrument { Id = "overtone", Harmonics = new[] { 1f, 0.2f, 0.15f, 0.1f, 0.9f, 0.1f, 0.05f, 0.6f, 0.05f, 0.3f }, Attack = 0.3f, Decay = 1f, Sustain = 1f, Release = 0.5f, Noise = 0.04f, Transpose = -12 });
            I(new Instrument { Id = "windharp", Harmonics = new[] { 1f, 0.4f, 0.3f, 0.2f, 0.15f }, Attack = 0.6f, Decay = 2f, Sustain = 0.7f, Release = 1.5f, Detune = 8f, VibratoRate = 0.2f, VibratoDepth = 0.04f, Noise = 0.06f });
            I(new Instrument { Id = "handdrum", Harmonics = new[] { 1f, 0.4f, 0.1f }, Attack = 0.002f, Decay = 0.2f, Sustain = 0f, Release = 0.2f, Inharmonic = 0.04f, LowPass = 1200f, Transpose = -12 });
            // The Greyfold and the Blank (AUD-07): a held tone that frays, a bowed cymbal, a low choir; a reversed piano, a celesta, the Remnant's voices.
            I(new Instrument { Id = "heldtone", Harmonics = new[] { 1f, 0.08f }, Attack = 1.5f, Decay = 2f, Sustain = 1f, Release = 2f, Detune = 4f, VibratoRate = 0.15f, VibratoDepth = 0.05f, Transpose = -12 });
            I(new Instrument { Id = "cymbal", Harmonics = new[] { 1f, 0.7f, 0.8f, 0.5f, 0.6f, 0.4f, 0.3f, 0.3f, 0.2f, 0.2f }, Attack = 0.5f, Decay = 1.5f, Sustain = 0.7f, Release = 1.5f, Inharmonic = 0.3f, Noise = 0.1f, Transpose = 12 });
            I(new Instrument { Id = "lowchoir", Harmonics = new[] { 1f, 0.9f, 0.7f, 0.8f, 0.35f, 0.2f, 0.12f }, Attack = 0.4f, Decay = 1f, Sustain = 0.95f, Release = 0.8f, VibratoRate = 4.5f, VibratoDepth = 0.08f, Noise = 0.06f, Detune = 12f, LowPass = 900f, Transpose = -12 });
            I(new Instrument { Id = "reversedpiano", Harmonics = Saw(24, 0.9f), Attack = 1.1f, Decay = 0.1f, Sustain = 1f, Release = 0.02f, Detune = 3f });
            I(new Instrument { Id = "celesta", Harmonics = new[] { 1f, 0.5f, 0.1f, 0.3f }, Attack = 0.001f, Decay = 0.8f, Sustain = 0.05f, Release = 0.6f, Inharmonic = 0.01f, Transpose = 24 });
            I(new Instrument { Id = "remnant", Harmonics = new[] { 1f, 0.8f, 0.6f, 0.5f, 0.25f, 0.1f }, Attack = 0.5f, Decay = 1f, Sustain = 0.9f, Release = 1f, VibratoRate = 4f, VibratoDepth = 0.06f, Noise = 0.15f, Detune = 14f, LowPass = 1400f, Transpose = -12 });
            Compose();
            ComposeRegions();
            ComposeBosses();
        }

        /// <summary>The instruments a region's theme may use: audio-direction 2's bands, as ids (the coast's with the drone and bell its theme leans on).</summary>
        public static string[] BandOf(Region r) => r switch
        {
            Region.Saltmarrow => new[] { "fiddle", "whistle", "drum", "concertina", "psaltery", "drone", "bell" },
            Region.Emberdown => new[] { "choir", "hurdygurdy", "framedrum", "anvil", "tuba" },
            Region.Verdance => new[] { "gamba", "glass", "organ", "handbell" },
            Region.Halden => new[] { "harpsichord", "strings", "musicbox", "brass" },
            Region.Windreach => new[] { "flute", "cittern", "overtone", "windharp", "handdrum" },
            _ => new string[0],
        };

        static float[] Saw(int n, float tilt) { var h = new float[n]; for (int k = 1; k <= n; k++) h[k - 1] = (float)Math.Pow(1.0 / k, tilt); return h; }
        static float[] Square(int n) { var h = new float[n]; for (int k = 1; k <= n; k++) h[k - 1] = k % 2 == 1 ? 1f / k : 0f; return h; }

        // ---- the notes ----

        /// <summary>A note in a stem: a scale degree (0 is the tonic; 7 the octave; negative below), when it starts in beats, how long in beats, and how loud.</summary>
        public struct Note
        {
            public int Degree;
            public float Start, Beats;
            public float Level;
            public Note(int degree, float start, float beats, float level = 1f) { Degree = degree; Start = start; Beats = beats; Level = level; }
            public override string ToString() => Degree + "@" + Start + ":" + Beats;
        }

        /// <summary>Degrees to semitones in a mode: 7 is the octave, and a degree past the scale wraps up.</summary>
        public static int Semitones(Region r, int degree)
        {
            var mode = Mode(r);
            int n = mode.Length;
            int oct = (int)Math.Floor(degree / (double)n);
            int idx = ((degree % n) + n) % n;
            return mode[idx] + 12 * oct;
        }

        public sealed class Stem
        {
            public string Id;
            public string Instrument;
            /// <summary>Its level as designed, 0..1.</summary>
            public float Level = 1f;
            public List<Note> Notes = new List<Note>();
            /// <summary>The boss phase it enters at (1 is from the first telegraph); 0 for a region theme's stem.</summary>
            public int Phase;
            /// <summary>Only heard in combat (the region theme's drive).</summary>
            public bool Combat;
            /// <summary>The boss phase it leaves at (a layer changed rather than added, audio-direction 4); 0 to stay.</summary>
            public int Until;
            public Stem Add(int degree, float start, float beats, float level = 1f) { Notes.Add(new Note(degree, start, beats, level)); return this; }
        }

        public sealed class Theme
        {
            public string Id;
            public Region Region;
            /// <summary>The boss it is for, by family; null for a region's theme.</summary>
            public string Boss;
            /// <summary>Bars that sound, then bars of rest, per loop.</summary>
            public int Bars, RestBars;
            public List<Stem> Stems = new List<Stem>();
            public float Beat => AudioDirection.BeatOf(Region);
            public float BarSeconds => Beat * BeatsPerBar;
            public int LoopBars => Bars + RestBars;
            public float LoopSeconds => LoopBars * BarSeconds;
            /// <summary>The share of the loop that is rest by design.</summary>
            public float Silence => RestBars / (float)LoopBars;
            public Stem Stem(string id) => Stems.FirstOrDefault(s => s.Id == id);
            public Stem Add(string id, string instrument, float level = 1f, int phase = 0, bool combat = false, int until = 0)
            {
                var s = new Stem { Id = id, Instrument = instrument, Level = level, Phase = phase, Combat = combat, Until = until };
                Stems.Add(s);
                return s;
            }
            /// <summary>Whether a stem sounds in a boss phase: entered, and not yet left.</summary>
            public static bool Sounds(Stem s, int phase) => s.Phase <= phase && (s.Until == 0 || phase < s.Until);
        }

        static readonly List<Theme> _themes = new List<Theme>();
        public static IReadOnlyList<Theme> Themes => _themes;
        public static Theme ThemeOf(Region r) => _themes.FirstOrDefault(t => t.Region == r && t.Boss == null);
        public static Theme ThemeOfBoss(string family) => _themes.FirstOrDefault(t => t.Boss == family);
        /// <summary>A boss's theme in the region it is fought in (Halvard's count travels: three keys), else its first, else none.</summary>
        public static Theme ThemeOfBoss(string family, Region? region) =>
            (region.HasValue ? _themes.FirstOrDefault(t => t.Boss == family && t.Region == region.Value) : null) ?? ThemeOfBoss(family);

        /// <summary>The bosses the shared motif serves: any whose family has no theme of its own.</summary>
        public const string SharedBoss = "*";
        static readonly Dictionary<Region, Theme> _shared = new Dictionary<Region, Theme>();
        /// <summary>The shared themes built so far (the exporter renders every region's).</summary>
        public static IEnumerable<Theme> SharedThemes => new[] { Region.Saltmarrow, Region.Emberdown, Region.Verdance, Region.Halden, Region.Windreach }.Select(SharedThemeOf).Where(t => t != null);
        /// <summary>
        /// The optionals' theme (AUD-07, "shared motifs for optionals"): the region's own theme fought in, its rests
        /// gone and its stems by phase: the bed, the pulse and the drive from the first telegraph, the lead in the
        /// second phase, the voices in the third. Every boss without a theme of its own fights to its region's.
        /// </summary>
        public static Theme SharedThemeOf(Region r)
        {
            if (_shared.TryGetValue(r, out var made)) return made;
            var region = ThemeOf(r);
            if (region == null) return null;
            var t = new Theme { Id = "shared", Region = r, Boss = SharedBoss, Bars = region.Bars, RestBars = 0 };
            foreach (var s in region.Stems)
            {
                int phase = s.Id == "lead" ? 2 : s.Id == "voices" ? 3 : 1;
                var copy = t.Add(s.Id, s.Instrument, s.Level, phase);
                copy.Notes.AddRange(s.Notes);
            }
            _shared[r] = t;
            return t;
        }

        /// <summary>The Guild's motif (AUD-07): paces counted, shared by every Warden's theme on the Guild's brass.</summary>
        public static readonly (int degree, float beats)[] GuildMotif = { (0, 1f), (0, 1f), (4, 1f), (4, 1f), (5, 1.5f), (4, 0.5f), (2, 1f), (0, 1f) };
        /// <summary>The stems the direction asks for at least (audio-direction 6).</summary>
        public static readonly string[] RequiredStems = { "lead", "bed", "pulse", "voices" };

        static void Compose()
        {
            // ---- Saltmarrow: the tide keeps the time; a drone that swells and draws back, and a fiddle that waits for it. ----
            // Eight bars, then two of rest: a fifth of the loop is the room alone (silence 20%).
            var salt = new Theme { Id = "saltmarrow", Region = Region.Saltmarrow, Bars = 8, RestBars = 2 };
            _themes.Add(salt);
            var bed = salt.Add("bed", "drone", 0.8f);
            // The drone in two swells of four bars: the tide in, the tide out (the level is the swell's peak; the render breathes it).
            bed.Add(0, 0f, 16f, 1f).Add(4, 0f, 16f, 0.7f);
            bed.Add(0, 16f, 15f, 0.9f).Add(4, 16f, 15f, 0.6f);                     // drawn back a beat early: its release is done as the rest begins
            var pulse = salt.Add("pulse", "drum", 0.7f);
            for (int bar = 0; bar < 8; bar++)
            {
                float b = bar * 4f;
                pulse.Add(0, b, 1f, 1f).Add(-3, b + 2f, 1f, 0.7f);                 // the tongue drum on one and three
                if (bar % 2 == 1) pulse.Add(0, b + 3.5f, 0.5f, 0.4f);              // a ghost before the bar turns
            }
            var lead = salt.Add("lead", "fiddle", 0.9f);
            // She waits two bars for the tide, then a long phrase; the second phrase answers with the roll-call's answer.
            lead.Add(4, 8f, 3f).Add(5, 11f, 1f).Add(6, 12f, 2f).Add(4, 14f, 2f)
                .Add(2, 16f, 1.5f).Add(1, 17.5f, 0.5f).Add(0, 18f, 2f).Add(-3, 20f, 1f).Add(0, 21f, 3f)
                .Add(5, 24f, 0.5f).Add(4, 24.5f, 1.5f);                                        // the call's lift, up to the fifth
            {
                var answer = AnswerDegrees(Region.Saltmarrow);                                 // the roll-call's answer, its third bent into the mode
                lead.Add(answer[0], 26f, 1f).Add(answer[1], 27f, 1f).Add(answer[2], 28f, 1f).Add(answer[3], 29f, 2f);   // ...home, and held
            }
            var voices = salt.Add("voices", "whistle", 0.6f);
            voices.Add(2, 16f, 4f, 0.8f).Add(1, 20f, 2f, 0.7f).Add(0, 22f, 2f, 0.7f)             // the low whistle under the second phrase
                  .Add(-1, 24f, 4f, 0.8f).Add(-3, 28f, 3f, 0.9f);
            var drive = salt.Add("drive", "psaltery", 0.6f, combat: true);
            for (int bar = 0; bar < 8; bar++)
            {
                float b = bar * 4f;
                foreach (var (d, at) in new[] { (0, 0f), (4, 0.5f), (7, 1f), (4, 1.5f), (0, 2f), (4, 2.5f), (7, 3f), (5, 3.5f) })
                    drive.Add(d, b + at, 0.5f, at % 1f == 0f ? 0.9f : 0.6f);              // the bowed psaltery in eighths: the fight's pulse
            }

            // ---- The Lamp-Keeper: the lamp turns; the beam sweeps low and slow; she dives through it. No rest. ----
            var lamp = new Theme { Id = "lampkeeper", Region = Region.Saltmarrow, Boss = "LampKeeper", Bars = 8, RestBars = 0 };
            _themes.Add(lamp);
            var lbed = lamp.Add("bed", "drone", 0.9f, phase: 1);
            lbed.Add(0, 0f, 16f, 1f).Add(3, 0f, 16f, 0.6f).Add(0, 16f, 16f, 1f).Add(4, 16f, 16f, 0.6f);   // the fourth, then the fifth: the lamp turning
            var lpulse = lamp.Add("pulse", "drum", 0.9f, phase: 1);
            for (int bar = 0; bar < 8; bar++)
            {
                float b = bar * 4f;
                lpulse.Add(0, b, 0.5f, 1f).Add(0, b + 1f, 0.5f, 0.6f).Add(-3, b + 2f, 0.5f, 1f).Add(0, b + 2.5f, 0.5f, 0.5f).Add(-3, b + 3f, 0.5f, 0.7f);
            }
            var llead = lamp.Add("lead", "fiddle", 1f, phase: 1);
            for (int half = 0; half < 2; half++)
            {
                float b = half * 16f;
                // The beam's sweep: a rising figure over a bar, again a step higher, then the dive falling through it.
                llead.Add(0, b, 0.5f).Add(2, b + 0.5f, 0.5f).Add(4, b + 1f, 0.5f).Add(7, b + 1.5f, 1.5f).Add(6, b + 3f, 1f)
                     .Add(1, b + 4f, 0.5f).Add(3, b + 4.5f, 0.5f).Add(5, b + 5f, 0.5f).Add(8, b + 5.5f, 1.5f).Add(7, b + 7f, 1f)
                     .Add(9, b + 8f, 1f).Add(7, b + 9f, 0.5f).Add(4, b + 9.5f, 0.5f).Add(2, b + 10f, 1f).Add(0, b + 11f, 1f)
                     .Add(-1, b + 12f, 2f).Add(0, b + 14f, 2f);
            }
            var lvoices = lamp.Add("voices", "whistle", 0.7f, phase: 2);
            for (int bar = 0; bar < 8; bar++)
            {
                float b = bar * 4f;
                lvoices.Add(bar % 2 == 0 ? 4 : 3, b, 2f, 0.8f).Add(bar % 2 == 0 ? 2 : 1, b + 2f, 2f, 0.7f);   // phase 2: the lamp splits, a second line under the first
            }
            var lbells = lamp.Add("bells", "bell", 0.6f, phase: 3);
            for (int bar = 0; bar < 8; bar++)
            {
                float b = bar * 4f;
                lbells.Add(7, b, 0.5f, 0.9f).Add(bar % 4 == 3 ? 6 : 4, b + 2.5f, 0.5f, 0.6f);                  // phase 3: the lamp gutters, a high toll on the bar
            }
        }

        /// <summary>The four regions' themes (AUD-06), each to its row of the direction's table: mode, beat, band, brief and silence share.</summary>
        static void ComposeRegions()
        {
            // ---- Emberdown: a work-song for many voices; everyone sings, nobody solos, the anvil keeps the count. ----
            // Nine bars, then one of rest: a tenth of the loop is the room alone (silence 10%).
            var ember = new Theme { Id = "emberdown", Region = Region.Emberdown, Bars = 9, RestBars = 1 };
            _themes.Add(ember);
            ember.Add("bed", "hurdygurdy", 0.55f).Add(0, 0f, 35f, 1f).Add(4, 0f, 35f, 0.6f);          // the wheel's bourdon, tonic and fifth, drawn back a beat before the rest
            var epulse = ember.Add("pulse", "framedrum", 0.8f);
            var count = ember.Add("count", "anvil", 0.5f);
            for (int bar = 0; bar < 9; bar++)
            {
                float b = bar * 4f;
                epulse.Add(0, b, 0.5f, 1f).Add(0, b + 1.5f, 0.5f, 0.6f).Add(-3, b + 2f, 0.5f, 0.9f).Add(0, b + 3f, 0.5f, 0.7f);   // the frame drum: one, two-and, three, four
                count.Add(7, b + 1f, 0.5f, 1f).Add(7, b + 3f, 0.5f, 0.8f);                                                  // the anvil on two and four
            }
            var elead = ember.Add("lead", "choir", 0.9f);
            var song = new (int d, float beats)[][]
            {
                new[] { (0, 1f), (0, 0.5f), (2, 0.5f), (4, 1f), (4, 1f) },        // the call
                new[] { (5, 1f), (4, 1f), (2, 1.5f), (0, 0.5f) },
                new[] { (0, 1f), (2, 1f), (4, 1f), (6, 1f) },                     // up to the flat seventh
                new[] { (4, 2f), (2, 1f), (0, 1f) },
                new[] { (4, 1f), (4, 0.5f), (5, 0.5f), (7, 1f), (7, 1f) },        // the second call, higher
                new[] { (6, 1f), (4, 1f), (2, 1.5f), (0, 0.5f) },
                new[] { (0, 1f), (2, 1f), (4, 1f), (2, 1f) },
                new[] { (1, 1f), (-1, 1f), (0, 2f) },
                new[] { (0, 1f), (4, 1f), (0, 1.5f) },                            // the tag, a beat short of the rest
            };
            for (int bar = 0; bar < song.Length; bar++)
            {
                float at = bar * 4f;
                foreach (var (d, beats) in song[bar]) { elead.Add(d, at, beats, at % 4f == 0f ? 1f : 0.85f); at += beats; }
            }
            var evoices = ember.Add("voices", "tuba", 0.6f);
            int[] roots = { -7, -7, -4, -7, -7, -3, -4, -3, -7 };
            for (int bar = 0; bar < 9; bar++) evoices.Add(roots[bar], bar * 4f, bar == 8 ? 3.5f : 4f, 0.9f);              // the tuba on the roots
            var edrive = ember.Add("drive", "hurdygurdy", 0.6f, combat: true);
            for (int bar = 0; bar < 9; bar++)
            {
                float b = bar * 4f;
                foreach (var (d, at) in new[] { (0, 0f), (0, 0.5f), (4, 1f), (0, 1.5f), (0, 2f), (0, 2.5f), (4, 3f), (6, 3.5f) })
                    edrive.Add(d, b + at, 0.5f, at % 1f == 0f ? 0.9f : 0.6f);                                             // the trompette's buzz in eighths
            }

            // ---- The Verdance: mostly nothing; one bowed voice in a very large room, and it stops before it resolves. ----
            // Three bars, then seven of rest (silence 70%).
            var verd = new Theme { Id = "verdance", Region = Region.Verdance, Bars = 3, RestBars = 7 };
            _themes.Add(verd);
            verd.Add("bed", "organ", 0.5f).Add(0, 0f, 11f, 1f);                                                       // the root-chapel's pedal, under the phrase only
            verd.Add("lead", "gamba", 0.9f).Add(0, 0f, 3f).Add(1, 3f, 2f).Add(3, 5f, 2f).Add(2, 7f, 2f).Add(1, 9f, 2.5f);   // ...and stops on the flat second, unresolved
            verd.Add("pulse", "handbell", 0.45f).Add(0, 0f, 1f, 0.9f).Add(4, 8f, 1f, 0.6f);                            // two handbells in three bars
            verd.Add("voices", "glass", 0.35f).Add(7, 1f, 9f, 1f);                                                     // the glass, high and held
            var vdrive = verd.Add("drive", "handbell", 0.5f, combat: true);
            for (int bar = 0; bar < 3; bar++)
                for (int beat = 0; beat < 4; beat++)
                    vdrive.Add(beat % 2 == 0 ? 0 : 1, bar * 4f + beat, 0.5f, beat == 0 ? 0.9f : 0.6f);                   // the Cantors' bells, a beat each, tonic and second

            // ---- Halden: a handsome clockwork piece that loops a bar it never finishes. ----
            // Four phrases of I IV V vi (the fifth never comes home), then the first bar again, cut short by three of rest (silence 15%).
            var hald = new Theme { Id = "halden", Region = Region.Halden, Bars = 17, RestBars = 3 };
            _themes.Add(hald);
            var chords = new (int root, int[] tones)[] { (0, new[] { 0, 2, 4 }), (3, new[] { 3, 5, 7 }), (4, new[] { 4, 6, 8 }), (5, new[] { 5, 7, 9 }) };
            var hlead = hald.Add("lead", "harpsichord", 0.8f);
            var hbed = hald.Add("bed", "strings", 0.5f);
            var hvoices = hald.Add("voices", "strings", 0.45f);
            var hpulse = hald.Add("pulse", "musicbox", 0.4f);
            var hdrive = hald.Add("drive", "brass", 0.55f, combat: true);
            for (int bar = 0; bar < 17; bar++)
            {
                float b = bar * 4f;
                var (root, tones) = chords[bar % 4];
                bool cut = bar == 16;                                                                                     // the seventeenth bar stops after two beats
                int[] figure = { tones[0], tones[2], tones[1], tones[2] };                                                // the broken chord: low, high, middle, high
                for (int i = 0; i < (cut ? 4 : 8); i++) hlead.Add(figure[i % 4], b + i * 0.5f, 0.5f, i % 4 == 0 ? 0.9f : 0.7f);
                float held = cut ? 2f : 4f;
                hbed.Add(root - 7, b, held, 0.9f);                                                                        // the quartet's cello on the root
                hvoices.Add(tones[2] + (bar % 4 == 3 ? 0 : 7), b, held, 0.8f);                                           // its violin on the top tone, dropping for vi
                for (int beat = 0; beat < (cut ? 2 : 4); beat++) hpulse.Add(beat % 2 == 0 ? 7 : 9, b + beat, 0.5f, beat == 0 ? 0.8f : 0.5f);   // the music box ticks
                for (int i = 0; i < (cut ? 4 : 8); i++) hdrive.Add(i % 2 == 0 ? root : tones[2], b + i * 0.5f, 0.5f, i % 2 == 0 ? 0.9f : 0.6f);   // the Guild's brass in eighths
            }

            // ---- Windreach: open air; a long flute and a plucked string over the wind, the camp's drum when the fire is lit. ----
            // Five bars, then three of rest (silence 37.5%: the direction's 35% as near as whole bars come).
            var wind = new Theme { Id = "windreach", Region = Region.Windreach, Bars = 5, RestBars = 3 };
            _themes.Add(wind);
            wind.Add("bed", "windharp", 0.55f).Add(0, 0f, 19f, 1f).Add(3, 0f, 19f, 0.7f).Add(5, 0f, 19f, 0.5f);         // the wind harp: tonic, fifth, octave
            wind.Add("lead", "flute", 0.9f).Add(5, 0f, 3f).Add(4, 3f, 1f).Add(3, 4f, 3f).Add(1, 7f, 1f).Add(2, 8f, 2f).Add(3, 10f, 2f)
                .Add(4, 12f, 1.5f).Add(3, 13.5f, 0.5f).Add(1, 14f, 2f).Add(0, 16f, 3.5f);                                 // the long flute, wide and slow, home at the end
            var wpulse = wind.Add("pulse", "handdrum", 0.45f);
            for (int bar = 0; bar < 5; bar++) wpulse.Add(0, bar * 4f, 0.5f, 0.6f).Add(0, bar * 4f + 2f, 0.5f, 0.4f);      // the hand drum, soft, on one and three
            wind.Add("voices", "overtone", 0.5f).Add(-5, 0f, 19f, 1f);                                                    // the overtone singer's drone, an octave under
            var wdrive = wind.Add("drive", "cittern", 0.6f, combat: true);
            for (int bar = 0; bar < 5; bar++)
            {
                float b = bar * 4f;
                foreach (var (d, at) in new[] { (0, 0f), (3, 0.5f), (5, 1f), (3, 1.5f), (0, 2f), (3, 2.5f), (4, 3f), (3, 3.5f) })
                    wdrive.Add(d, b + at, 0.5f, at % 1f == 0f ? 0.9f : 0.6f);                                             // the cittern picked in eighths
            }
        }

        /// <summary>The Guild motif written into a stem from a beat, transposed by degrees.</summary>
        static void Guild(Stem lead, float at, int up = 0, float level = 1f)
        {
            foreach (var (d, beats) in GuildMotif) { lead.Add(d + up, at, beats, level); at += beats; }
        }

        /// <summary>The roll-call's phrase in a form, as a mode's degrees (the Archivist's lead quotes it inverted).</summary>
        public static int[] PhraseDegrees(Region r, RollCallSong.Form form) => RollCallSong.Notes(form, 1).Select(n => DegreeOf(r, n.Pitch)).ToArray();

        /// <summary>Warden-Sergeant Halvard's theme in a key: the same count in every region he hunts her through.</summary>
        static Theme HalvardIn(Region r, string id)
        {
            // The count: three paces and a rest, every bar; the Guild's motif on its brass; the survey's marks; and then the count itself, tolled.
            var t = new Theme { Id = id, Region = r, Boss = "Halvard", Bars = 8, RestBars = 0 };
            _themes.Add(t);
            t.Add("bed", "drone", 0.8f, phase: 1).Add(0, 0f, 16f, 1f).Add(4, 0f, 16f, 0.5f).Add(0, 16f, 16f, 1f).Add(3, 16f, 16f, 0.5f);
            var pulse = t.Add("pulse", "drum", 0.8f, phase: 1);
            for (int bar = 0; bar < 8; bar++) pulse.Add(0, bar * 4f, 0.5f, 1f).Add(0, bar * 4f + 1f, 0.5f, 0.8f).Add(0, bar * 4f + 2f, 0.5f, 0.8f);   // "Three paces. I measured them."
            var lead = t.Add("lead", "brass", 0.9f, phase: 1);
            Guild(lead, 0f); Guild(lead, 8f, 2); Guild(lead, 16f); Guild(lead, 24f, 4);
            var voices = t.Add("voices", "whistle", 0.6f, phase: 2);
            int[] marks = { 3, 5, 3, 6, 3, 5, 2, 1 };
            for (int bar = 0; bar < 8; bar++) voices.Add(marks[bar], bar * 4f, 4f, 0.8f);                                       // phase 2: the marks, one a bar
            var count = t.Add("count", "bell", 0.6f, phase: 3);
            for (int bar = 0; bar < 8; bar++) for (int k = 0; k < 4; k++) count.Add(7, bar * 4f + k, 0.5f, k == 0 ? 0.9f : 0.6f);   // phase 3: the count, tolled on every beat
            return t;
        }

        /// <summary>The bosses' themes (AUD-07): Halvard's in three keys, Brann's, Voss's, the Archivist's. Each adds or changes a layer a phase.</summary>
        static void ComposeBosses()
        {
            HalvardIn(Region.Saltmarrow, "halvard");            // the Salt Chapel
            HalvardIn(Region.Halden, "halvard-halden");         // the Seven Bridges
            HalvardIn(Region.Greyfold, "halvard-greyfold");     // the Threshold, beside Voss

            // ---- Cinder Warden Brann: the furnace floor, red-hot in sections that cool; in the dark only his brass glows. ----
            var brann = new Theme { Id = "brann", Region = Region.Emberdown, Boss = "Brann", Bars = 8, RestBars = 0 };
            _themes.Add(brann);
            brann.Add("bed", "hurdygurdy", 0.6f, phase: 1, until: 3).Add(0, 0f, 32f, 1f).Add(4, 0f, 32f, 0.6f);                  // the furnace's roar, gone when it is dark
            var bpulse = brann.Add("pulse", "framedrum", 0.8f, phase: 1);
            var bcount = brann.Add("count", "anvil", 0.55f, phase: 1);
            for (int bar = 0; bar < 8; bar++)
            {
                float b = bar * 4f;
                bpulse.Add(0, b, 0.5f, 1f).Add(0, b + 1f, 0.5f, 0.7f).Add(-3, b + 2f, 0.5f, 0.9f).Add(0, b + 3f, 0.5f, 0.7f);
                bcount.Add(7, b, 0.5f, 1f).Add(7, b + 2f, 0.5f, 0.8f);                                                          // "I am the schedule": the anvil on one and three
            }
            var blead = brann.Add("lead", "brass", 0.9f, phase: 1);
            Guild(blead, 0f); Guild(blead, 8f, 2); Guild(blead, 16f); Guild(blead, 24f, 4);
            var bvoices = brann.Add("voices", "tuba", 0.7f, phase: 2);
            int[] cuts = { -3, -4, -5, -4, -3, -2, -4, -3 };
            for (int bar = 0; bar < 8; bar++) bvoices.Add(cuts[bar], bar * 4f, 2f, 0.9f).Add(cuts[bar] - 1, bar * 4f + 2f, 2f, 0.7f);   // phase 2: both lances, the cross-cuts under the motif
            var glow = brann.Add("glow", "bell", 0.5f, phase: 3);
            for (int bar = 0; bar < 8; bar++) { glow.Add(7, bar * 4f, 1f, 0.9f); if (bar % 2 == 1) glow.Add(9, bar * 4f + 2.5f, 0.5f, 0.6f); }   // phase 3: only his brass glows

            // ---- Guildmaster Voss: one held tone that frays; he anchors and everything holds; the Blank eats the arena and the bells toll. ----
            var voss = new Theme { Id = "voss", Region = Region.Greyfold, Boss = "Voss", Bars = 6, RestBars = 0 };
            _themes.Add(voss);
            voss.Add("bed", "heldtone", 0.7f, phase: 1, until: 3).Add(0, 0f, 24f, 1f);                                            // the held tone, gone when the white comes
            var vpulse = voss.Add("pulse", "cymbal", 0.5f, phase: 1);
            for (int bar = 0; bar < 6; bar++) vpulse.Add(0, bar * 4f, 2f, 0.8f);                                                  // a bowed cymbal on every bar
            var vlead = voss.Add("lead", "brass", 0.85f, phase: 1);
            Guild(vlead, 0f); Guild(vlead, 8f, 4); Guild(vlead, 16f);                                                             // formal, textbook, slow
            var vvoices = voss.Add("voices", "lowchoir", 0.6f, phase: 2);
            for (int bar = 0; bar < 6; bar++) vvoices.Add(0, bar * 4f, 4f, 1f).Add(4, bar * 4f, 4f, 0.7f).Add(7, bar * 4f, 4f, 0.5f);   // phase 2: "Hold. Everything holds": a choir all at once
            var white = voss.Add("white", "bell", 0.5f, phase: 3);
            for (int bar = 0; bar < 6; bar++) for (int k = 0; k < 4; k++) white.Add(7, bar * 4f + k, 0.5f, k == 0 ? 0.9f : 0.5f);   // phase 3: the Half-Cathedral's bells

            // ---- The Archivist: the mirror-Observatory; the roll-call pulled the other way; her drawing sings it the right way up; the frame closes. ----
            var arch = new Theme { Id = "archivist", Region = Region.Blank, Boss = "Archivist", Bars = 4, RestBars = 0 };
            _themes.Add(arch);
            var apulse = arch.Add("pulse", "celesta", 0.5f, phase: 1);
            for (int bar = 0; bar < 4; bar++) for (int k = 0; k < 4; k++) apulse.Add(k % 2 == 0 ? 7 : 9, bar * 4f + k + 0.5f, 0.5f, k == 0 ? 0.8f : 0.5f);   // the clockwork, mirrored: on the off-beats
            arch.Add("bed", "remnant", 0.6f, phase: 1).Add(0, 0f, 16f, 1f).Add(-3, 0f, 16f, 0.6f);                                // the Remnant's voices under
            var alead = arch.Add("lead", "reversedpiano", 0.9f, phase: 1);
            var inverted = PhraseDegrees(Region.Blank, RollCallSong.Form.Inverted);
            var beats = RollCallSong.Notes(RollCallSong.Form.Inverted, 1).Select(n => n.Beats).ToArray();
            foreach (float start in new[] { 0f, 8f })
            {
                float at = start;
                for (int i = 0; i < inverted.Length; i++) { alead.Add(inverted[i], at, beats[i], 1f); at += beats[i]; }             // the roll-call inverted about its reciting tone
            }
            var avoices = arch.Add("voices", "celesta", 0.6f, phase: 2);
            var answer = AnswerDegrees(Region.Blank);
            foreach (float start in new[] { 4f, 12f })
            {
                float at = start;
                for (int i = 0; i < answer.Length; i++) { avoices.Add(answer[i] + 7, at, AudioDirection.RollCall.Answer[i].Beats, 0.9f); at += AudioDirection.RollCall.Answer[i].Beats; }   // phase 2: her drawing answers the right way up
            }
            var frame = arch.Add("frame", "reversedpiano", 0.7f, phase: 3);
            for (int bar = 0; bar < 4; bar++) for (int k = 0; k < 4; k++) frame.Add(bar + 1 + k, bar * 4f + k, 1f, 0.8f);          // phase 3: the frame closes, a wingspan a beat
        }

        // ---- the synth ----

        const int TableSize = 2048;

        static float[] Table(Instrument ins, float hz)
        {
            var t = new float[TableSize];
            for (int i = 0; i < TableSize; i++)
            {
                double ph = 2 * Math.PI * i / TableSize, s = 0;
                for (int h = 1; h <= ins.Harmonics.Length; h++)
                {
                    float w = ins.Harmonics[h - 1];
                    if (w <= 0f || hz * h > SampleRate * 0.45f) continue;
                    double k = h * (1 + ins.Inharmonic * (h - 1));
                    s += w * Math.Sin(k * ph);
                }
                t[i] = (float)s;
            }
            float peak = t.Max(x => Math.Abs(x));
            if (peak > 0f) for (int i = 0; i < TableSize; i++) t[i] /= peak;
            return t;
        }

        /// <summary>
        /// Render a stem's loop: every note through its instrument, the stem's level, the loop's length exactly (a note's
        /// release past the loop end wraps to the start, so the loop is seamless). The bed breathes: a slow swell over
        /// two bars, the tide in and out.
        /// </summary>
        public static float[] RenderStem(Theme theme, Stem stem, float tonicHz)
        {
            var ins = InstrumentOf(stem.Instrument);
            int len = (int)Math.Round(theme.LoopSeconds * SampleRate);
            var outp = new float[len];
            float beat = theme.Beat;
            foreach (var n in stem.Notes)
            {
                float hz = RollCallSong.Hz(tonicHz, RollCallSong.WrittenOctave + ins.Transpose + Semitones(theme.Region, n.Degree));
                var table = Table(ins, hz);
                float detune = ins.Detune > 0f ? (float)Math.Pow(2.0, ins.Detune / 1200.0) : 1f;
                int start = (int)(n.Start * beat * SampleRate);
                int hold = (int)(n.Beats * beat * SampleRate);
                int rel = (int)(ins.Release * SampleRate);
                var rng = new Random(Stable(stem.Id) ^ start);
                double ph = 0, ph2 = 0;
                float noise = 0f, lp = 0f;
                float lpA = ins.LowPass > 0f ? 1f - (float)Math.Exp(-2 * Math.PI * ins.LowPass / SampleRate) : 1f;
                for (int i = 0; i < hold + rel; i++)
                {
                    float t = i / (float)SampleRate;
                    float env;
                    if (i < hold)
                    {
                        float a = ins.Attack <= 0f ? 1f : Math.Min(1f, t / ins.Attack);
                        float d = ins.Sustain + (1f - ins.Sustain) * (float)Math.Exp(-t / Math.Max(0.001f, ins.Decay));
                        env = a * d;
                    }
                    else env = (ins.Sustain + (1f - ins.Sustain) * (float)Math.Exp(-(hold / (float)SampleRate) / Math.Max(0.001f, ins.Decay))) * (1f - (i - hold) / (float)Math.Max(1, rel));
                    float vib = ins.VibratoDepth > 0f ? (float)Math.Pow(2.0, Math.Sin(2 * Math.PI * ins.VibratoRate * t) * ins.VibratoDepth * Math.Min(1f, t / 0.4f) / 12.0) : 1f;
                    ph += hz * vib / SampleRate; if (ph >= 1) ph -= 1;
                    float pos = (float)(ph * TableSize); int i0 = Math.Min((int)pos, TableSize - 1); int i1 = (i0 + 1) % TableSize;   // the float rounds up to the table's end just under a period
                    float s = table[i0] + (table[i1] - table[i0]) * (pos - i0);
                    if (ins.Detune > 0f)
                    {
                        ph2 += hz * vib * detune / SampleRate; if (ph2 >= 1) ph2 -= 1;
                        float p2 = (float)(ph2 * TableSize); int j0 = Math.Min((int)p2, TableSize - 1); int j1 = (j0 + 1) % TableSize;
                        s = 0.6f * s + 0.4f * (table[j0] + (table[j1] - table[j0]) * (p2 - j0));
                    }
                    if (ins.Noise > 0f) { noise += (((float)rng.NextDouble() * 2f - 1f) - noise) * 0.2f; s += noise * ins.Noise * 2f; }
                    if (ins.LowPass > 0f) { lp += (s - lp) * lpA; s = lp; }
                    float breathe = stem.Id == "bed" ? 0.55f + 0.45f * (float)Math.Sin(2 * Math.PI * (start / (float)SampleRate + t) / (2f * theme.BarSeconds) - Math.PI / 2) : 1f;
                    int at = (start + i) % len;
                    outp[at] += s * env * n.Level * stem.Level * breathe * 0.4f;
                }
            }
            return outp;
        }

        /// <summary>Every stem of a theme rendered, by id, each brought to the same headroom (the mix of them peaks under −1 dBTP).</summary>
        public static Dictionary<string, float[]> Render(Theme theme)
        {
            float tonic = RollCallSong.TonicHz(theme.Region);
            var stems = theme.Stems.ToDictionary(s => s.Id, s => RenderStem(theme, s, tonic));
            int len = stems.Values.First().Length;
            float peak = 0f;
            for (int i = 0; i < len; i++) { float sum = 0f; foreach (var s in stems.Values) sum += s[i]; peak = Math.Max(peak, Math.Abs(sum)); }
            if (peak > 0f)
            {
                float g = (float)Math.Pow(10.0, AudioDirection.SfxPeakDbtp / 20.0) / peak;
                foreach (var s in stems.Values) for (int i = 0; i < len; i++) s[i] *= g;
            }
            return stems;
        }

        /// <summary>The share of a loop's time that is quiet in a mix of stems, by quarter-second windows (the direction's silence, measured).</summary>
        public static float MeasuredSilence(IEnumerable<float[]> stems, float quietDb = -40f)
        {
            var list = stems.ToList();
            int len = list[0].Length, win = SampleRate / 4, windows = len / win, quiet = 0;
            float thresh = (float)Math.Pow(10.0, quietDb / 20.0);
            for (int w = 0; w < windows; w++)
            {
                double e = 0; int from = w * win, to = from + win;
                for (int i = from; i < to; i++) { float sum = 0f; foreach (var s in list) sum += s[i]; e += sum * sum; }
                if (Math.Sqrt(e / win) < thresh) quiet++;
            }
            return quiet / (float)Math.Max(1, windows);
        }

        /// <summary>The roll-call's answer as a boss's theme resolves it: fifth, third, second, tonic, on the fiddle in the region's key.</summary>
        public static float[] Resolution(Region r)
        {
            var theme = new Theme { Id = "resolution", Region = r, Bars = 2, RestBars = 0 };
            var stem = theme.Add("lead", "fiddle");
            var answer = AudioDirection.RollCall.Answer;
            var degrees = AnswerDegrees(r);
            float at = 0f;
            for (int i = 0; i < answer.Length; i++)
            {
                stem.Add(degrees[i], at, answer[i].Beats, 1f);
                at += answer[i].Beats;
            }
            theme.Add("bed", "drone", 0.7f).Add(0, 0f, at, 1f);
            var stems = Render(theme);
            int len = stems["lead"].Length;
            var mix = new float[len];
            foreach (var s in stems.Values) for (int i = 0; i < len; i++) mix[i] += s[i];
            return mix;
        }

        // ---- files ----

        /// <summary>A stem's delivery name: region_music_theme-stem_bpm.wav (audio-direction 6).</summary>
        public static string FileName(Theme theme, Stem stem) =>
            RollCallSong.FileName(theme.Region, "music", (theme.Boss != null ? theme.Id + "-" : "") + stem.Id, 60f / theme.Beat);

        /// <summary>A standard MIDI file of the theme: the tempo, one track per stem with its notes at their times (format 1, 480 a beat).</summary>
        public static byte[] Midi(Theme theme)
        {
            const int Ppq = 480;
            int tonic = RollCallSong.TonicMidi(theme.Region) + RollCallSong.WrittenOctave;
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
            Chunk("MThd", new byte[] { 0, 1, (byte)((theme.Stems.Count + 1) >> 8), (byte)((theme.Stems.Count + 1) & 0xFF), (byte)(Ppq >> 8), (byte)(Ppq & 0xFF) });
            var tempo = new List<byte>();
            int us = (int)Math.Round(theme.Beat * 1_000_000);
            VarLen(tempo, 0); tempo.AddRange(new byte[] { 0xFF, 0x51, 3, (byte)(us >> 16), (byte)(us >> 8), (byte)us });
            VarLen(tempo, 0); tempo.AddRange(new byte[] { 0xFF, 0x2F, 0 });
            Chunk("MTrk", tempo.ToArray());
            for (int ch = 0; ch < theme.Stems.Count; ch++)
            {
                var stem = theme.Stems[ch];
                var ins = InstrumentOf(stem.Instrument);
                var events = new List<(int tick, bool on, int note, int vel)>();
                foreach (var n in stem.Notes)
                {
                    int note = tonic + ins.Transpose + Semitones(theme.Region, n.Degree);
                    events.Add(((int)Math.Round(n.Start * Ppq), true, note, (int)(40 + 87 * n.Level)));
                    events.Add(((int)Math.Round((n.Start + n.Beats) * Ppq), false, note, 0));
                }
                events.Sort((a, b) => a.tick != b.tick ? a.tick.CompareTo(b.tick) : a.on.CompareTo(b.on));   // offs before ons at the same tick
                var b = new List<byte>();
                var name = Encoding.ASCII.GetBytes(stem.Id + " (" + ins.Id + ")");
                VarLen(b, 0); b.AddRange(new byte[] { 0xFF, 0x03 }); VarLen(b, name.Length); b.AddRange(name);
                int program = ins.Id switch
                {
                    "fiddle" => 110, "whistle" => 75, "drum" => 116, "concertina" => 21, "psaltery" => 46, "bell" => 14,
                    "choir" => 52, "hurdygurdy" => 109, "framedrum" => 117, "anvil" => 113, "tuba" => 58,
                    "gamba" => 42, "glass" => 92, "organ" => 19, "handbell" => 112,
                    "harpsichord" => 6, "strings" => 48, "musicbox" => 10, "brass" => 61,
                    "flute" => 73, "cittern" => 25, "overtone" => 54, "windharp" => 89, "handdrum" => 118,
                    "heldtone" => 80, "cymbal" => 119, "lowchoir" => 52, "reversedpiano" => 0, "celesta" => 8, "remnant" => 54,
                    _ => 48,
                };
                VarLen(b, 0); b.AddRange(new[] { (byte)(0xC0 | (ch % 16)), (byte)program });
                int last = 0;
                foreach (var e in events)
                {
                    VarLen(b, e.tick - last); last = e.tick;
                    b.AddRange(new[] { (byte)((e.on ? 0x90 : 0x80) | (ch % 16)), (byte)Math.Clamp(e.note, 0, 127), (byte)Math.Clamp(e.vel, 0, 127) });
                }
                VarLen(b, 0); b.AddRange(new byte[] { 0xFF, 0x2F, 0 });
                Chunk("MTrk", b.ToArray());
            }
            return ms.ToArray();
        }
    }
}
