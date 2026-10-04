using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>
    /// The enemies' voices (AUD-10, docs/design/enemy-sounds.md). Every creature in the game is a drawing, so every one
    /// of them sounds like what it is drawn as: a shell is a hard tick, a wing is air, a smudge is wet ink, a Cantor is a
    /// handbell, a Warden is brass, a Remnant is dry paper, a salamander is embers, a moth cloud is many small wings, a
    /// pulp-wasp is a hum and a wet pop, a Sketch is graphite, a tussock is earth, a reedling is fluff and a peep.
    /// A family's <see cref="Voice"/> is its material (the hurt and death every drawing of that stuff shares, and what a
    /// turned-away strike sounds like, if anything) and the cues its own moves make, named by the clips the animator
    /// already asks for (<c>Enemy.Clip</c>): a one-shot on a clip's first frame, or a loop while the clip runs. Nothing
    /// here is a recording: the cues are made by <see cref="InkSounds"/>' tools and a few more, and registered into its
    /// table so the bank, the tests and the exporter treat them like hers.
    /// </summary>
    public static partial class EnemySounds
    {
        const int SampleRate = InkSounds.SampleRate;

        /// <summary>What a creature is drawn as; the hurt, death and block it shares with everything of the same stuff.</summary>
        public enum Material { Shell, Wing, Ink, Bell, Brass, Paper, Ember, Moth, Pulp, Graphite, Earth, Fluff }

        public sealed class Voice
        {
            public string Family;
            public Material Material;
            /// <summary>What it sounds like, in a line, for the doc and the sound designer.</summary>
            public string What;
            /// <summary>A strike turned away (a shell, a burning back, a cloud the quill passes through); null is silence.</summary>
            public string Blocked;
            /// <summary>The clips in which a turned-away strike sounds at all (a tussock's shell only when it is up); null is any.</summary>
            public string[] BlockedClips;
            /// <summary>Clip → cue played once as the clip begins.</summary>
            public IReadOnlyDictionary<string, string> Moves = Empty;
            /// <summary>Clip → cue looped while the clip runs.</summary>
            public IReadOnlyDictionary<string, string> Loops = Empty;
            /// <summary>A boss's count → cue, played each time the count goes up (a bell tolled, a rope cut, a limb redrawn):
            /// the moments that are no clip of the body's. The name is a public int the boss keeps.</summary>
            public IReadOnlyDictionary<string, string> Events = Empty;
            /// <summary>A death of its own (the Star's fall, the Bells' last hum), or null for the material's.</summary>
            public string OwnDeath;
            public string Hurt => CueOf(Material, "hurt");
            public string Death => OwnDeath ?? CueOf(Material, "death");
            /// <summary>Every cue the voice can play.</summary>
            public IEnumerable<string> Cues
            {
                get
                {
                    yield return Hurt; yield return Death;
                    if (Blocked != null) yield return Blocked;
                    foreach (var c in Moves.Values) yield return c;
                    foreach (var c in Loops.Values) yield return c;
                    foreach (var c in Events.Values) yield return c;
                }
            }
        }

        static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();
        static readonly Dictionary<string, Voice> _voices = new Dictionary<string, Voice>(StringComparer.Ordinal);
        public static IReadOnlyCollection<Voice> Voices => _voices.Values;
        /// <summary>The voice every drawing falls back to: wet ink.</summary>
        public const string Default = "*";

        public static string CueOf(Material m, string what) => m.ToString().ToLowerInvariant() + "_" + what;
        public static bool Has(string family) => family != null && _voices.ContainsKey(family);
        /// <summary>A family's voice, or the default's for a family the table does not know (a boss's part, a test's target).</summary>
        public static Voice Of(string family) => family != null && _voices.TryGetValue(family, out var v) ? v : _voices[Default];
        public static string MoveCue(string family, string clip) => clip != null && Of(family).Moves.TryGetValue(clip, out var c) ? c : null;
        public static string LoopCue(string family, string clip) => clip != null && Of(family).Loops.TryGetValue(clip, out var c) ? c : null;

        static Dictionary<string, string> Map(params string[] pairs)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i + 1 < pairs.Length; i += 2) d[pairs[i]] = pairs[i + 1];
            return d;
        }

        static void Family(string family, Material m, string what, string blocked = null, Dictionary<string, string> moves = null, Dictionary<string, string> loops = null, string[] blockedIn = null,
                           Dictionary<string, string> events = null, string death = null) =>
            _voices[family] = new Voice { Family = family, Material = m, What = what, Blocked = blocked, BlockedClips = blockedIn, Moves = moves ?? Empty, Loops = loops ?? Empty,
                                          Events = events ?? Empty, OwnDeath = death };

        static EnemySounds()
        {
            Family(Default, Material.Ink, "a drawing with no voice of its own: wet ink");
            // ---- the coast (CMB-08) ----
            Family("MarshCrab", Material.Shell, "a shelled round: claws ticking as it walks, a flick off the paper when it hops, a clack when the quill meets the shell",
                blocked: "shell_block", moves: Map("hop", "crab_hop"), loops: Map("move", "crab_scuttle"));
            Family("ReedSkimmer", Material.Wing, "a round on the wing: a thin whistle as it rises, air torn as it dives",
                moves: Map("rise", "skimmer_rise", "dive", "skimmer_dive"));
            Family("Smudge", Material.Ink, "something forgotten: wet ink, and nothing while it is undrawn");
            Family("MemorySmudge", Material.Ink, "her own death's smudge: the same wet ink");
            Family("Cantor", Material.Bell, "a teardrop with a handbell: the bell struck when it is hit, and the toll when the ring is done",
                moves: Map("recover", "cantor_toll"));
            Family("Warden", Material.Brass, "a heron in brass: the lens turned to measure her, the lance drawn back along the gorget, the thrust's air and its clink",
                moves: Map("measure", "warden_measure", "telegraph", "warden_draw", "thrust", "warden_thrust"));
            Family("LostRemnant", Material.Paper, "a bird with the ink gone: dry paper rustling as it drifts, and torn when it goes",
                loops: Map("move", "remnant_drift"));
            // ---- the highland (CMB-09) ----
            Family("CaveBat", Material.Wing, "a hung cloak: paper opening as it unfurls, air falling and rising as it swoops",
                moves: Map("unfurl", "bat_unfurl", "swoop", "bat_swoop"));
            Family("Salamander", Material.Ember, "a low black length with embers down its back: the crackle as they stand up, a dry scrape along the ground, a hiss where a strike meets them",
                blocked: "ember_block", moves: Map("flare", "salamander_flare", "rush", "salamander_rush"), loops: Map("flare", "ember_crackle", "rush", "ember_crackle"));
            // ---- the roster (CMB-09, later batches) ----
            Family("Mothcloud", Material.Moth, "many small wings: a flutter while it lives, a puff as the eye-spots open, a hiss as it darts, and nothing where the quill passes through",
                blocked: "moth_pass", moves: Map("flare", "moth_flare", "dart", "moth_dart"), loops: Map("idle", "moth_flutter", "move", "moth_flutter", "gather", "moth_flutter"));
            Family("Pulpwasp", Material.Pulp, "a paper-wasp: a hum while it hovers, a wet pop and a whizz as it spits",
                moves: Map("spit", "wasp_spit"), loops: Map("idle", "wasp_buzz", "move", "wasp_buzz", "spit", "wasp_buzz"));
            Family("Sketch", Material.Graphite, "a townsfolk oval in pencil: fast hatching as it fills itself in, a dry drag as it lunges, rubbed out when it dies",
                moves: Map("fill", "sketch_fill", "lunge", "sketch_lunge"));
            Family("Tussock", Material.Earth, "a shelled burrower: the ground rumbling as its ridge travels, a deep heave, grit bursting as it breaches and falling as it burrows",
                blocked: "shell_block", moves: Map("heave", "tussock_heave", "breach", "tussock_breach", "burrow", "tussock_burrow"), loops: Map("ridge", "tussock_ridge"),
                blockedIn: new[] { "breach", "idle" });   // under the grass a strike meets nothing
            Family("Reedling", Material.Fluff, "a half-drawn chick: quick steps, two taps of the bill before it lunges, and a peep",
                moves: Map("peck", "reedling_peck", "lunge", "reedling_peep", "move", "reedling_scurry"));
            // ---- the bosses (AUD-15): their own voices, EnemySoundsBosses.cs ----
            Bosses();
        }

        /// <summary>Put every cue the voices name into <see cref="InkSounds"/>' table. Called once, by its static constructor.</summary>
        internal static void Register()
        {
            void One(string id, string what, Func<float[]> render, float gain = 1f, bool loop = false, bool move = false) =>
                InkSounds.Add(id, InkSounds.Kind.Enemy, what, render, gain, loop, null, move ? AudioDirection.Voice.World : AudioDirection.Voice.Enemy);
            // ---- the materials: hurt, death, and a strike turned away ----
            One("shell_hurt", "a hard tick and the shell's short ring", ShellHurt);
            One("shell_death", "the shell cracking: two ticks, and grit running out", ShellDeath);
            One("shell_block", "a dry clack: the quill turned away", ShellBlock, 0.8f);
            One("wing_hurt", "a puff of the wing, struck", WingHurt);
            One("wing_death", "a flutter slowing as it falls", WingDeath);
            One("ink_hurt", "a wet smear, short: the ink struck", InkHurt);
            One("ink_death", "the ink dissolving, two drops leaving it", InkDeath);
            One("bell_hurt", "the handbell struck off-centre, in D", BellHurt);
            One("bell_death", "the bell cracking: the note sagging and breaking into shards", BellDeath);
            One("brass_hurt", "a clink of the gorget", BrassHurt);
            One("brass_death", "the brass coming off: three clinks and a thud", BrassDeath);
            One("paper_hurt", "dry paper crumpling under the stroke", PaperHurt, 0.5f);
            One("paper_death", "the page torn through", PaperDeath, 0.8f);
            One("ember_hurt", "a hiss and a pop: the embers struck", EmberHurt);
            One("ember_death", "the embers put out: a long hiss, the crackle going", EmberDeath);
            One("ember_block", "a hiss where the quill meets the burning back", EmberBlock, 0.7f);
            One("moth_hurt", "a puff of many small wings", MothHurt);
            One("moth_death", "the cloud coming down as dust", MothDeath);
            One("moth_pass", "the quill passing through: almost nothing", MothPass, 0.4f);
            One("pulp_hurt", "a wet pop", PulpHurt);
            One("pulp_death", "the sac bursting and the pulp pattering down", PulpDeath);
            One("graphite_hurt", "a pencil snapped", GraphiteHurt);
            One("graphite_death", "rubbed out: four strokes of the eraser", GraphiteDeath);
            One("earth_hurt", "a thud, and grit", EarthHurt);
            One("earth_death", "the mound going down: a rumble and grit falling", EarthDeath);
            One("fluff_hurt", "a small peep and a puff of down", FluffHurt);
            One("fluff_death", "a peep falling, and the down settling", FluffDeath);
            // ---- the moves (world one-shots: kept after the hits and deaths) ----
            One("crab_hop", "a flick off the paper", CrabHop, 0.7f, move: true);
            One("crab_scuttle", "claws ticking as it walks", CrabScuttle, 0.3f, true, true);
            One("skimmer_rise", "a thin whistle rising as the wings lift", SkimmerRise, 0.6f, move: true);
            One("skimmer_dive", "air torn as it dives past", SkimmerDive, 0.8f, move: true);
            One("cantor_toll", "the toll: a bell in D, with its hum", CantorToll, 1f, move: true);
            One("warden_measure", "the lens turned twice: two brass clicks", WardenMeasure, 0.6f, move: true);
            One("warden_draw", "the lance drawn back along the gorget", WardenDraw, 0.5f, move: true);
            One("warden_thrust", "the lance's air, and a clink at full reach", WardenThrust, 0.9f, move: true);
            One("remnant_drift", "dry paper breathing as it drifts", RemnantDrift, 0.3f, true, true);
            One("bat_unfurl", "the cloak opening: paper and air", BatUnfurl, 0.7f, move: true);
            One("bat_swoop", "the swoop: air falling past and climbing away", BatSwoop, 0.9f, move: true);
            One("salamander_flare", "the embers standing up: a crackle and a hiss", SalamanderFlare, 0.8f, move: true);
            One("salamander_rush", "a dry scrape along the ground", SalamanderRush, 0.8f, move: true);
            One("ember_crackle", "embers crackling while they are up", EmberCrackle, 0.35f, true, true);
            One("moth_flutter", "the flutter of the cloud", MothFlutter, 0.3f, true, true);
            One("moth_flare", "the eye-spots shown: all the wings open at once", MothFlare, 0.6f, move: true);
            One("moth_dart", "the cloud darting through: a hiss of wings", MothDart, 0.7f, move: true);
            One("wasp_buzz", "the hum of a paper-wasp", WaspBuzz, 0.4f, true, true);
            One("wasp_spit", "a wet pop and the pellet's whizz", WaspSpit, 0.8f, move: true);
            One("sketch_fill", "the outline hatched in, fast", SketchFill, 0.8f, move: true);
            One("sketch_lunge", "a dry drag of the pencil", SketchLunge, 0.8f, move: true);
            One("tussock_ridge", "the ground rumbling under the grass", TussockRidge, 0.45f, true, true);
            One("tussock_heave", "the turf lifting: a deep heave", TussockHeave, 1f, move: true);
            One("tussock_breach", "grit bursting as the shell comes up", TussockBreach, 0.9f, move: true);
            One("tussock_burrow", "grit falling back as it goes under", TussockBurrow, 0.7f, move: true);
            One("reedling_peck", "two taps of the bill", ReedlingPeck, 0.6f, move: true);
            One("reedling_peep", "a peep", ReedlingPeep, 0.7f, move: true);
            One("reedling_scurry", "quick small steps", ReedlingScurry, 0.3f, move: true);
            RegisterBosses();
            foreach (var v in _voices.Values)
                foreach (var c in v.Cues)
                    if (!InkSounds.Has(c)) throw new InvalidOperationException(v.Family + " names a cue that does not exist: " + c);
        }

        // ---- a few more tools on top of InkSounds' ----

        static int N(float seconds) => InkSounds.N(seconds);
        static float[] Buf(float seconds) => InkSounds.Buf(seconds);
        static void Scratch(float[] s, float at, float len, float fromHz, float toHz, float q, float gain, int seed, float attack = 0.003f, float decay = 0.05f) => InkSounds.Scratch(s, at, len, fromHz, toHz, q, gain, seed, attack, decay);
        static void Drop(float[] s, float at, float len, float fromHz, float toHz, float gain, float attack = 0.002f, float decay = 0.06f) => InkSounds.Drop(s, at, len, fromHz, toHz, gain, attack, decay);
        static void Smear(float[] s, float at, float len, float fromHz, float toHz, float gain, int seed, float attack = 0.01f, float decay = 0.08f) => InkSounds.Smear(s, at, len, fromHz, toHz, gain, seed, attack, decay);
        internal static void Click(float[] s, float at, float hz, float gain, int seed) => Scratch(s, at, 0.006f, hz, hz, 1f, gain, seed, 0f, 0.003f);

        /// <summary>The partials of a struck thing: (ratio to the pitch, loudness).</summary>
        internal static readonly (float, float)[] Handbell = { (1f, 1f), (2.76f, 0.4f), (5.4f, 0.15f) };
        internal static readonly (float, float)[] TowerBell = { (0.5f, 0.5f), (1f, 1f), (1.2f, 0.6f), (1.5f, 0.4f), (2f, 0.5f) };
        internal static readonly (float, float)[] Brass = { (1f, 1f), (1.5f, 0.5f), (2.9f, 0.35f), (4.2f, 0.2f) };
        internal static readonly (float, float)[] Shell = { (1f, 1f), (2.3f, 0.3f) };

        /// <summary>A struck thing ringing: damped sines at the pitch's partials, the upper ones dying first; the pitch may sag to <paramref name="slideTo"/>.</summary>
        internal static void Ring(float[] s, float at, float len, float hz, float gain, float decay, (float ratio, float amp)[] partials, float slideTo = 0f)
        {
            int start = N(at), end = Math.Min(s.Length, N(at + len));
            var ph = new double[partials.Length];
            for (int i = start; i < end; i++)
            {
                float t = (i - start) / (float)SampleRate;
                float f = slideTo > 0f ? InkSounds.Lerp(hz, slideTo, t / len) : hz;
                float env = InkSounds.Env(t, 0.001f, len, decay);
                float v = 0f;
                for (int p = 0; p < partials.Length; p++)
                {
                    ph[p] += f * partials[p].ratio / SampleRate;
                    v += (float)Math.Sin(2 * Math.PI * ph[p]) * partials[p].amp * (float)Math.Exp(-t * p * 2f / decay);
                }
                s[i] += v * env * gain;
            }
        }

        /// <summary>Many small things at random: ticks (or soft puffs) <paramref name="perSecond"/>, each <paramref name="tickLen"/> long, pitched between the two; the rate may change over the length.</summary>
        internal static void Grain(float[] s, float at, float len, float perSecond, float tickLen, float hzLo, float hzHi, float q, float gain, int seed, float perSecondEnd = -1f, bool soft = false)
        {
            var rng = new InkSounds.Rng(seed);
            float t = at;
            int k = 0;
            while (t < at + len)
            {
                float u = (t - at) / len;
                float rate = perSecondEnd < 0f ? perSecond : InkSounds.Lerp(perSecond, perSecondEnd, u);
                float hz = hzLo + (rng.Next() * 0.5f + 0.5f) * (hzHi - hzLo);
                float g = gain * (0.6f + 0.4f * (rng.Next() * 0.5f + 0.5f));
                if (soft) Smear(s, t, tickLen, hz, hz * 0.7f, g, seed * 101 + k, 0.002f, tickLen * 0.4f);
                else Scratch(s, t, tickLen, hz, hz * 0.8f, q, g, seed * 101 + k, 0.001f, tickLen * 0.4f);
                k++;
                t += (0.5f + (rng.Next() * 0.5f + 0.5f)) / Math.Max(1f, rate);
            }
        }

        /// <summary>A pulse train through a low-pass, with a slow wobble: a wasp. Seamless when hz and wobbleHz times the length are whole.</summary>
        internal static void Buzz(float[] s, float at, float len, float hz, float gain, float wobbleHz, float wobbleDepth, float cutoff, int seed)
        {
            var lp = new InkSounds.OnePole(); var lp2 = new InkSounds.OnePole(); var rng = new InkSounds.Rng(seed);
            int start = N(at), end = Math.Min(s.Length, N(at + len));
            double ph = 0;
            for (int i = start; i < end; i++)
            {
                float t = (i - start) / (float)SampleRate;
                float f = hz * (1f + wobbleDepth * (float)Math.Sin(2 * Math.PI * wobbleHz * t));
                ph += f / SampleRate;
                float saw = (float)(ph - Math.Floor(ph)) * 2f - 1f;
                float x = saw * saw * saw + rng.Next() * 0.08f;   // a buzzing edge and a little air
                s[i] += lp2.Low(lp.Low(x, cutoff), cutoff) * gain;
            }
        }

        /// <summary>Air moved fast: band noise sweeping, swelling to <paramref name="humpAt"/> of the length and gone.</summary>
        internal static void Whoosh(float[] s, float at, float len, float fromHz, float toHz, float q, float gain, int seed, float humpAt = 0.4f)
        {
            var rng = new InkSounds.Rng(seed); var band = new InkSounds.Band();
            int start = N(at), end = Math.Min(s.Length, N(at + len));
            for (int i = start; i < end; i++)
            {
                float t = (i - start) / (float)SampleRate, u = t / len;
                float env = u < humpAt ? u / humpAt : 1f - (u - humpAt) / (1f - humpAt);
                env = env * env * (3f - 2f * env);
                float tail = len - t < 0.004f ? (len - t) / 0.004f : 1f;
                s[i] += band.Tick(rng.Next(), InkSounds.Lerp(fromHz, toHz, u), q) * env * tail * gain;
            }
        }

        /// <summary>A small bird's note: a sine sliding with a little vibrato, an octave above it and a breath of noise.</summary>
        internal static void Peep(float[] s, float at, float len, float fromHz, float toHz, float gain, int seed, float attack = 0.004f, float decay = 0.08f)
        {
            var rng = new InkSounds.Rng(seed); var band = new InkSounds.Band();
            int start = N(at), end = Math.Min(s.Length, N(at + len));
            double ph = 0;
            for (int i = start; i < end; i++)
            {
                float t = (i - start) / (float)SampleRate;
                float hz = InkSounds.Lerp(fromHz, toHz, t / len) * (1f + 0.012f * (float)Math.Sin(2 * Math.PI * 38f * t));
                ph += hz / SampleRate;
                float env = InkSounds.Env(t, attack, len, decay);
                s[i] += ((float)Math.Sin(2 * Math.PI * ph) + 0.25f * (float)Math.Sin(4 * Math.PI * ph) + band.Tick(rng.Next(), hz, 6f) * 0.3f) * env * gain;
            }
        }

        /// <summary>A loop's end made to meet its start: the tail past the loop's length is crossfaded onto its head.</summary>
        internal static float[] Seamless(float[] s, float overlap)
        {
            int ov = N(overlap), len = s.Length - ov;
            var r = new float[len];
            Array.Copy(s, r, len);
            for (int i = 0; i < ov; i++) { float k = i / (float)ov; r[i] = s[i] * k + s[len + i] * (1f - k); }
            return r;
        }

        const float Ov = 0.05f;   // every loop's crossfade

        // ---- the materials ----

        static float[] ShellHurt()
        {
            var s = Buf(0.09f);
            Click(s, 0f, 4200f, 0.8f, 200);
            Ring(s, 0f, 0.08f, 1900f, 0.6f, 0.02f, Shell);
            Drop(s, 0f, 0.03f, 300f, 200f, 0.2f, 0.001f, 0.012f);
            return s;
        }

        static float[] ShellDeath()
        {
            var s = Buf(0.36f);
            Click(s, 0f, 4000f, 0.9f, 201);
            Click(s, 0.06f, 3200f, 0.8f, 202);
            Ring(s, 0.06f, 0.1f, 1400f, 0.5f, 0.03f, Shell);
            Grain(s, 0.1f, 0.25f, 60f, 0.012f, 2500f, 5000f, 3f, 0.5f, 203, 10f);
            Smear(s, 0.12f, 0.2f, 900f, 300f, 0.4f, 204, 0.01f, 0.06f);
            return s;
        }

        static float[] ShellBlock()
        {
            var s = Buf(0.05f);
            Click(s, 0f, 5000f, 0.7f, 205);
            Ring(s, 0f, 0.045f, 2600f, 0.8f, 0.012f, Shell);
            return s;
        }

        static float[] WingHurt()
        {
            var s = Buf(0.1f);
            Smear(s, 0f, 0.08f, 1500f, 600f, 1f, 210, 0.004f, 0.03f);
            Scratch(s, 0f, 0.03f, 2600f, 1800f, 2f, 0.4f, 211, 0.002f, 0.012f);
            return s;
        }

        static float[] WingDeath()
        {
            var s = Buf(0.46f);
            Grain(s, 0f, 0.4f, 40f, 0.02f, 1200f, 2400f, 1.5f, 0.8f, 212, 8f);
            Drop(s, 0.05f, 0.35f, 600f, 150f, 0.35f, 0.01f, 0.2f);
            Smear(s, 0.3f, 0.14f, 700f, 300f, 0.4f, 213, 0.01f, 0.05f);
            return s;
        }

        static float[] InkHurt()
        {
            var s = Buf(0.14f);
            Smear(s, 0f, 0.12f, 700f, 200f, 1f, 220, 0.004f, 0.04f);
            Drop(s, 0f, 0.05f, 320f, 140f, 0.5f, 0.001f, 0.02f);
            return s;
        }

        static float[] InkDeath()
        {
            var s = Buf(0.5f);
            Smear(s, 0f, 0.45f, 600f, 120f, 1f, 221, 0.01f, 0.14f);
            Drop(s, 0.08f, 0.12f, 260f, 90f, 0.4f, 0.004f, 0.05f);
            Drop(s, 0.3f, 0.1f, 200f, 70f, 0.3f, 0.004f, 0.05f);
            return s;
        }

        public const float CantorHz = 1174.66f;   // D6: the Cantor's handbell rings the coast's tonic

        static float[] BellHurt()
        {
            var s = Buf(0.4f);
            Ring(s, 0f, 0.4f, CantorHz, 1f, 0.12f, Handbell);
            return s;
        }

        static float[] BellDeath()
        {
            var s = Buf(0.7f);
            Ring(s, 0f, 0.6f, CantorHz, 1f, 0.2f, Handbell, CantorHz * 0.85f);
            Grain(s, 0.3f, 0.35f, 40f, 0.015f, 3000f, 6000f, 4f, 0.5f, 230, 6f);
            return s;
        }

        static float[] BrassHurt()
        {
            var s = Buf(0.12f);
            Click(s, 0f, 5000f, 0.5f, 240);
            Ring(s, 0f, 0.11f, 3200f, 1f, 0.03f, Brass);
            return s;
        }

        static float[] BrassDeath()
        {
            var s = Buf(0.5f);
            Ring(s, 0f, 0.1f, 2800f, 1f, 0.03f, Brass);
            Ring(s, 0.12f, 0.1f, 2400f, 0.9f, 0.03f, Brass);
            Ring(s, 0.26f, 0.12f, 2000f, 0.8f, 0.035f, Brass);
            Drop(s, 0.3f, 0.15f, 140f, 70f, 0.5f, 0.002f, 0.05f);
            Smear(s, 0.3f, 0.18f, 500f, 200f, 0.3f, 241, 0.005f, 0.05f);
            return s;
        }

        static float[] PaperHurt()
        {
            var s = Buf(0.11f);
            Grain(s, 0f, 0.1f, 90f, 0.008f, 2200f, 4200f, 2f, 1f, 250);
            return s;
        }

        static float[] PaperDeath()
        {
            var s = Buf(0.4f);
            Scratch(s, 0f, 0.3f, 3400f, 900f, 2f, 1f, 251, 0.003f, 0.12f);
            Grain(s, 0f, 0.3f, 80f, 0.008f, 2000f, 4000f, 2f, 0.5f, 252);
            Smear(s, 0.28f, 0.1f, 1200f, 500f, 0.3f, 253, 0.005f, 0.03f);
            return s;
        }

        static float[] EmberHurt()
        {
            var s = Buf(0.16f);
            Smear(s, 0f, 0.14f, 3000f, 1200f, 0.8f, 260, 0.002f, 0.05f);
            Drop(s, 0f, 0.04f, 220f, 80f, 0.5f, 0.001f, 0.015f);
            Grain(s, 0f, 0.12f, 60f, 0.005f, 3500f, 7000f, 2f, 0.5f, 261);
            return s;
        }

        static float[] EmberDeath()
        {
            var s = Buf(0.5f);
            Smear(s, 0f, 0.45f, 2600f, 600f, 1f, 262, 0.005f, 0.15f);
            Grain(s, 0f, 0.4f, 30f, 0.006f, 3000f, 6000f, 2f, 0.6f, 263, 4f);
            return s;
        }

        static float[] EmberBlock()
        {
            var s = Buf(0.09f);
            Smear(s, 0f, 0.08f, 3500f, 1500f, 1f, 264, 0.001f, 0.025f);
            Click(s, 0f, 4500f, 0.4f, 265);
            return s;
        }

        static float[] MothHurt()
        {
            var s = Buf(0.1f);
            Grain(s, 0f, 0.09f, 160f, 0.01f, 900f, 1800f, 1f, 1f, 270, soft: true);
            Smear(s, 0f, 0.08f, 1200f, 500f, 0.6f, 271, 0.004f, 0.03f);
            return s;
        }

        static float[] MothDeath()
        {
            var s = Buf(0.5f);
            Grain(s, 0f, 0.45f, 160f, 0.01f, 900f, 1800f, 1f, 1f, 272, 20f, true);
            Smear(s, 0.1f, 0.4f, 800f, 200f, 0.5f, 273, 0.02f, 0.14f);
            return s;
        }

        static float[] MothPass()
        {
            var s = Buf(0.08f);
            Smear(s, 0f, 0.07f, 1600f, 700f, 1f, 274, 0.004f, 0.025f);
            return s;
        }

        static float[] PulpHurt()
        {
            var s = Buf(0.09f);
            Drop(s, 0f, 0.06f, 520f, 160f, 1f, 0.001f, 0.02f);
            Smear(s, 0f, 0.05f, 900f, 400f, 0.5f, 280, 0.001f, 0.02f);
            return s;
        }

        static float[] PulpDeath()
        {
            var s = Buf(0.38f);
            Drop(s, 0f, 0.05f, 480f, 140f, 1f, 0.001f, 0.02f);
            Smear(s, 0f, 0.1f, 1200f, 400f, 0.6f, 281, 0.001f, 0.03f);
            var rng = new InkSounds.Rng(282);
            for (int k = 0; k < 8; k++)
            {
                float hz = 320f + (rng.Next() * 0.5f + 0.5f) * 300f;
                Drop(s, 0.06f + k * 0.036f + rng.Next() * 0.008f, 0.03f, hz, hz * 0.45f, 0.5f - k * 0.04f, 0.001f, 0.012f);
            }
            return s;
        }

        static float[] GraphiteHurt()
        {
            var s = Buf(0.09f);
            Click(s, 0f, 3000f, 1f, 290);
            Scratch(s, 0.005f, 0.07f, 2400f, 1500f, 3f, 1f, 291, 0.001f, 0.02f);
            return s;
        }

        static float[] GraphiteDeath()
        {
            var s = Buf(0.45f);
            for (int k = 0; k < 4; k++)
            {
                Smear(s, k * 0.1f, 0.09f, 1100f, 600f, 1f, 292 + k, 0.01f, 0.03f);
                Scratch(s, k * 0.1f, 0.08f, 1800f, 1200f, 1.5f, 0.3f, 296 + k, 0.01f, 0.03f);
            }
            return s;
        }

        static float[] EarthHurt()
        {
            var s = Buf(0.14f);
            Drop(s, 0f, 0.1f, 110f, 55f, 1f, 0.001f, 0.04f);
            Smear(s, 0f, 0.1f, 700f, 250f, 0.5f, 300, 0.001f, 0.03f);
            return s;
        }

        static float[] EarthDeath()
        {
            var s = Buf(0.55f);
            Smear(s, 0f, 0.5f, 180f, 60f, 1f, 301, 0.01f, 0.18f);
            Drop(s, 0f, 0.15f, 90f, 45f, 0.6f, 0.002f, 0.06f);
            Grain(s, 0.05f, 0.45f, 50f, 0.01f, 1500f, 3500f, 2f, 0.5f, 302, 8f);
            return s;
        }

        static float[] FluffHurt()
        {
            var s = Buf(0.09f);
            Peep(s, 0f, 0.07f, 2700f, 2100f, 1f, 310, 0.002f, 0.03f);
            Smear(s, 0f, 0.06f, 1500f, 700f, 0.3f, 311, 0.002f, 0.02f);
            return s;
        }

        static float[] FluffDeath()
        {
            var s = Buf(0.3f);
            Peep(s, 0f, 0.2f, 2400f, 900f, 1f, 312, 0.004f, 0.08f);
            Smear(s, 0.12f, 0.16f, 900f, 400f, 0.4f, 313, 0.01f, 0.05f);
            return s;
        }

        // ---- the moves ----

        static float[] CrabHop()
        {
            var s = Buf(0.12f);
            Click(s, 0f, 3800f, 1f, 400);
            Whoosh(s, 0.01f, 0.1f, 900f, 2000f, 1.2f, 0.8f, 401, 0.3f);
            return s;
        }

        static float[] CrabScuttle()
        {
            var s = Buf(0.6f + Ov);
            Grain(s, 0f, 0.6f + Ov, 14f, 0.008f, 3000f, 5200f, 2.5f, 1f, 402);
            return Seamless(s, Ov);
        }

        static float[] SkimmerRise()
        {
            var s = Buf(0.4f);
            Drop(s, 0f, 0.38f, 900f, 2100f, 0.5f, 0.01f, 0.3f);
            Whoosh(s, 0f, 0.4f, 1200f, 3000f, 3f, 0.6f, 410, 0.8f);
            return s;
        }

        static float[] SkimmerDive()
        {
            var s = Buf(0.35f);
            Whoosh(s, 0f, 0.33f, 2800f, 700f, 1.5f, 1f, 411, 0.35f);
            return s;
        }

        static float[] CantorToll()
        {
            var s = Buf(0.9f);
            Ring(s, 0f, 0.9f, CantorHz / 2f, 1f, 0.3f, TowerBell);
            return s;
        }

        static float[] WardenMeasure()
        {
            var s = Buf(0.3f);
            Click(s, 0f, 2600f, 0.6f, 420);
            Ring(s, 0f, 0.12f, 2600f, 0.5f, 0.03f, Brass);
            Click(s, 0.18f, 2200f, 0.6f, 421);
            Ring(s, 0.18f, 0.1f, 2200f, 0.5f, 0.03f, Brass);
            return s;
        }

        static float[] WardenDraw()
        {
            var s = Buf(0.25f);
            Scratch(s, 0f, 0.22f, 700f, 1400f, 2f, 1f, 422, 0.01f, 0.12f);
            return s;
        }

        static float[] WardenThrust()
        {
            var s = Buf(0.2f);
            Whoosh(s, 0f, 0.14f, 800f, 2600f, 1.5f, 1f, 423, 0.25f);
            Ring(s, 0.1f, 0.1f, 3400f, 0.5f, 0.02f, Brass);
            return s;
        }

        static float[] RemnantDrift()
        {
            var s = Buf(0.8f + Ov);
            Grain(s, 0f, 0.8f + Ov, 30f, 0.012f, 1800f, 3200f, 2f, 1f, 430, soft: true);
            Smear(s, 0f, 0.8f + Ov, 1200f, 1200f, 0.3f, 431, 0.05f, 10f);
            return Seamless(s, Ov);
        }

        static float[] BatUnfurl()
        {
            var s = Buf(0.3f);
            Whoosh(s, 0f, 0.12f, 600f, 1500f, 1f, 0.7f, 440, 0.5f);
            Scratch(s, 0.08f, 0.15f, 1800f, 900f, 1.5f, 0.6f, 441, 0.005f, 0.06f);
            Smear(s, 0.1f, 0.2f, 900f, 500f, 0.5f, 442, 0.01f, 0.06f);
            return s;
        }

        static float[] BatSwoop()
        {
            var s = Buf(0.6f);
            Whoosh(s, 0f, 0.35f, 2200f, 800f, 1.5f, 1f, 443, 0.5f);
            Whoosh(s, 0.3f, 0.3f, 800f, 1800f, 1.5f, 0.7f, 444, 0.5f);
            return s;
        }

        static float[] SalamanderFlare()
        {
            var s = Buf(0.25f);
            Grain(s, 0f, 0.22f, 120f, 0.006f, 3500f, 7000f, 2f, 0.9f, 450);
            Smear(s, 0f, 0.2f, 2500f, 900f, 0.6f, 451, 0.005f, 0.07f);
            return s;
        }

        static float[] SalamanderRush()
        {
            var s = Buf(0.4f);
            Scratch(s, 0f, 0.38f, 1300f, 700f, 1.5f, 1f, 452, 0.01f, 0.2f);
            Grain(s, 0f, 0.35f, 60f, 0.006f, 3000f, 6000f, 2f, 0.4f, 453);
            return s;
        }

        static float[] EmberCrackle()
        {
            var s = Buf(0.7f + Ov);
            Grain(s, 0f, 0.7f + Ov, 28f, 0.006f, 3200f, 6500f, 2f, 1f, 454);
            return Seamless(s, Ov);
        }

        static float[] MothFlutter()
        {
            var s = Buf(0.9f + Ov);
            Grain(s, 0f, 0.9f + Ov, 70f, 0.012f, 700f, 1600f, 1f, 1f, 460, soft: true);
            return Seamless(s, Ov);
        }

        static float[] MothFlare()
        {
            var s = Buf(0.2f);
            Smear(s, 0f, 0.18f, 1800f, 700f, 1f, 461, 0.01f, 0.06f);
            Grain(s, 0f, 0.1f, 200f, 0.01f, 900f, 1800f, 1f, 0.6f, 462, soft: true);
            return s;
        }

        static float[] MothDart()
        {
            var s = Buf(0.3f);
            Whoosh(s, 0f, 0.28f, 1200f, 2600f, 1.2f, 1f, 463, 0.3f);
            Grain(s, 0f, 0.25f, 120f, 0.01f, 900f, 1800f, 1f, 0.4f, 464, soft: true);
            return s;
        }

        public const float WaspHz = 150f;

        static float[] WaspBuzz()
        {
            var s = Buf(1f + Ov);
            Buzz(s, 0f, 1f + Ov, WaspHz, 1f, 3f, 0.03f, 900f, 470);   // 150 cycles and 3 wobbles a second: the loop meets itself
            return Seamless(s, Ov);
        }

        static float[] WaspSpit()
        {
            var s = Buf(0.22f);
            Drop(s, 0f, 0.05f, 420f, 160f, 1f, 0.001f, 0.02f);
            Whoosh(s, 0.03f, 0.18f, 2000f, 4200f, 2f, 0.7f, 471, 0.3f);
            return s;
        }

        static float[] SketchFill()
        {
            var s = Buf(0.35f);
            for (int k = 0; k < 10; k++)
                Scratch(s, k * 0.032f, 0.028f, k % 2 == 0 ? 2600f : 1800f, k % 2 == 0 ? 1800f : 2600f, 3f, 0.9f, 480 + k, 0.001f, 0.012f);
            return s;
        }

        static float[] SketchLunge()
        {
            var s = Buf(0.22f);
            Scratch(s, 0f, 0.2f, 1600f, 900f, 2f, 1f, 490, 0.004f, 0.1f);
            return s;
        }

        static float[] TussockRidge()
        {
            var s = Buf(0.8f + Ov);
            Smear(s, 0f, 0.8f + Ov, 140f, 140f, 1f, 500, 0.05f, 10f);
            Grain(s, 0f, 0.8f + Ov, 20f, 0.015f, 600f, 1400f, 1f, 0.4f, 501, soft: true);
            return Seamless(s, Ov);
        }

        static float[] TussockHeave()
        {
            var s = Buf(0.3f);
            Drop(s, 0f, 0.25f, 95f, 50f, 1f, 0.004f, 0.1f);
            Smear(s, 0f, 0.28f, 220f, 90f, 0.7f, 502, 0.02f, 0.1f);
            return s;
        }

        static float[] TussockBreach()
        {
            var s = Buf(0.35f);
            Grain(s, 0f, 0.3f, 90f, 0.008f, 1800f, 4000f, 2f, 0.8f, 503, 20f);
            Smear(s, 0f, 0.2f, 1200f, 400f, 0.7f, 504, 0.002f, 0.05f);
            Drop(s, 0f, 0.08f, 180f, 90f, 0.5f, 0.001f, 0.03f);
            return s;
        }

        static float[] TussockBurrow()
        {
            var s = Buf(0.45f);
            Grain(s, 0f, 0.4f, 70f, 0.01f, 1500f, 3200f, 2f, 0.7f, 505, 10f);
            Smear(s, 0.05f, 0.4f, 400f, 120f, 0.6f, 506, 0.03f, 0.12f);
            return s;
        }

        static float[] ReedlingPeck()
        {
            var s = Buf(0.14f);
            for (int k = 0; k < 2; k++)
            {
                Click(s, k * 0.07f, 4200f, 0.7f, 510 + k);
                Drop(s, k * 0.07f, 0.03f, 2800f, 2600f, 0.5f, 0.0005f, 0.01f);
            }
            return s;
        }

        static float[] ReedlingPeep()
        {
            var s = Buf(0.12f);
            Peep(s, 0f, 0.1f, 2300f, 3100f, 1f, 512, 0.003f, 0.05f);
            return s;
        }

        static float[] ReedlingScurry()
        {
            var s = Buf(0.2f);
            Grain(s, 0f, 0.18f, 40f, 0.006f, 3500f, 5500f, 2f, 1f, 513);
            return s;
        }
    }
}
