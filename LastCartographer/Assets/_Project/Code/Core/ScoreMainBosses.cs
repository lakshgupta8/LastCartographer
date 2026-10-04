using System.Linq;

namespace OWSBG.Core
{
    public static partial class Score
    {
        /// <summary>
        /// The last four main bosses' themes (AUD-14, docs/design/music.md §3e): the Gatekeeper's, Oriel's, the
        /// Half-Cathedral Bells' and Corra's Drawing's, each in the key and at the beat of the region it is fought in,
        /// from the fight's opening stems at the first telegraph and a layer added or changed a phase. With these every
        /// main boss has a theme of its own; only the optionals fight to their region's motif.
        /// </summary>
        static void ComposeMainBosses()
        {
            ComposeGatekeeper();
            ComposeOriel();
            ComposeBells();
            ComposeCorra();
        }

        /// <summary>The Guild's motif played backwards, its lengths with it: Oriel's mirror of Wren's own kit.</summary>
        public static (int degree, float beats)[] MirroredMotif => GuildMotif.Reverse().ToArray();

        /// <summary>The Bells' hymn, a bar a note: "For the flock, going north", rising.</summary>
        public static readonly int[] BellsNorth = { 7, 8, 9, 11 };

        // ---- the Gatekeeper: the Verdance, the Overgrown Gate; a statue of the flying age, roots for wings ----
        static void ComposeGatekeeper()
        {
            var t = new Theme { Id = "gatekeeper", Region = Region.Verdance, Boss = "Gatekeeper", Bars = 6, RestBars = 0 };
            _themes.Add(t);
            float loop = t.Bars * BeatsPerBar;
            // The roots in the ground: the root-chapel's pedal, tonic and fifth, torn up in phase 3.
            t.Add("bed", "organ", 0.6f, phase: 1, until: 3).Add(0, 0f, loop, 1f).Add(4, 0f, loop, 0.5f);
            // The stone wings: a Cantor handbell on one and three, the tonic and the flat second.
            var pulse = t.Add("pulse", "handbell", 0.5f, phase: 1);
            for (int bar = 0; bar < t.Bars; bar++) pulse.Add(0, bar * 4f, 1f, 0.9f).Add(1, bar * 4f + 2f, 1f, 0.6f);
            // "Passage is for the winged.": the Verdance's one bowed voice, slow as stone, stopping on the flat second, unresolved.
            var lead = t.Add("lead", "gamba", 0.85f, phase: 1, until: 3);
            lead.Add(0, 0f, 3f).Add(1, 3f, 1f).Add(3, 4f, 2f).Add(4, 6f, 2f).Add(3, 8f, 2f).Add(1, 10f, 2f)
                .Add(0, 12f, 2f).Add(-1, 14f, 2f).Add(-2, 16f, 2f).Add(0, 18f, 2f).Add(1, 20f, 4f);
            // Phase 2: it rises to the top of the gate and the fight goes up the threads: the glass, held high, a step higher every bar.
            var voices = t.Add("voices", "glass", 0.4f, phase: 2);
            for (int bar = 0; bar < t.Bars; bar++) voices.Add(4 + bar, bar * 4f, 4f, 0.8f);
            // Phase 3: the roots tear free (the pedal leaves) and it flies, badly, for the first time since the Grounding:
            // the voice that stood climbs a wingbeat at a time and comes down hard, each climb a little lower.
            var flight = t.Add("flight", "gamba", 0.8f, phase: 3);
            int[] peaks = { 7, 6, 7, 5, 6, 4 };
            for (int bar = 0; bar < t.Bars; bar++)
            {
                float b = bar * 4f;
                int top = peaks[bar];
                flight.Add(0, b, 1f, 0.9f).Add(top - 3, b + 1f, 1f, 0.8f).Add(top, b + 2f, 1.5f, 1f).Add(top - 5, b + 3.5f, 0.5f, 0.9f);
            }
        }

        // ---- Warden-Captain Oriel: Halden, the Bastion's drill-yard; she mirrors Wren's own kit ----
        static void ComposeOriel()
        {
            var t = new Theme { Id = "oriel", Region = Region.Halden, Boss = "Oriel", Bars = 8, RestBars = 0 };
            _themes.Add(t);
            float loop = t.Bars * BeatsPerBar;
            // The drill-yard's count: the music box on every beat, one and three marked; chalk lines.
            var pulse = t.Add("pulse", "musicbox", 0.45f, phase: 1);
            for (int b = 0; b < loop; b++) pulse.Add(b % 2 == 0 ? 7 : 4, b, 0.5f, b % 4 == 0 ? 0.9f : b % 2 == 0 ? 0.7f : 0.5f);
            // Halden's clockwork under her, I IV V vi, the fifth never home; it gives way in phase 3 to the dominant, held.
            var bed = t.Add("bed", "strings", 0.5f, phase: 1, until: 3);
            int[] roots = { 0, 3, 4, 5 };
            for (int bar = 0; bar < t.Bars; bar++) bed.Add(roots[bar % 4] - 7, bar * 4f, 4f, 0.9f);
            // "Pell writes well. Show me the rest.": the Guild's motif mirrored, played backwards on the Guild's brass, as
            // she plays Wren's own combo reversed.
            var lead = t.Add("lead", "brass", 0.9f, phase: 1);
            Motif(lead, MirroredMotif, 0f); Motif(lead, MirroredMotif, 8f, 2); Motif(lead, MirroredMotif, 16f); Motif(lead, MirroredMotif, 24f, 3);
            // Phase 2: "That's my stance.": the motif the right way up on the strings, above, against her mirror; and Wren's
            // Flourish, the pen's run on the harpsichord, up an octave and back, every other bar.
            var voices = t.Add("voices", "strings", 0.5f, phase: 2);
            Guild(voices, 0f, 7, 0.8f); Guild(voices, 8f, 9, 0.8f); Guild(voices, 16f, 7, 0.8f); Guild(voices, 24f, 10, 0.8f);
            var flourish = t.Add("flourish", "harpsichord", 0.55f, phase: 2);
            for (int bar = 1; bar < t.Bars; bar += 2)
            {
                float b = bar * 4f;
                for (int k = 0; k < 8; k++) flourish.Add(7 + k, b + k * 0.25f, 0.25f, k == 0 ? 0.9f : 0.7f);
                for (int k = 0; k < 8; k++) flourish.Add(14 - k, b + 2f + k * 0.25f, 0.25f, 0.6f);
            }
            // Phase 3: she Binds, once, and the fight is denying it: the cello holds the dominant and the violin its leading
            // tone under everything, the cadence she is writing, never let home.
            var bind = t.Add("bind", "strings", 0.6f, phase: 3);
            for (int bar = 0; bar < t.Bars; bar += 2) bind.Add(-3, bar * 4f, 8f, 1f).Add(6, bar * 4f, 8f, 0.6f);
        }

        /// <summary>A motif written into a stem from a beat, transposed by degrees.</summary>
        static void Motif(Stem s, (int degree, float beats)[] motif, float at, int up = 0, float level = 1f)
        {
            foreach (var (d, beats) in motif) { s.Add(d + up, at, beats, level); at += beats; }
        }

        // ---- the Half-Cathedral Bells: the Greyfold, the nave, white; each ring takes the light ----
        static void ComposeBells()
        {
            var t = new Theme { Id = "bells", Region = Region.Greyfold, Boss = "HalfCathedralBells", Bars = 4, RestBars = 0 };
            _themes.Add(t);
            float loop = t.Bars * BeatsPerBar;
            // The music strikes no bell: every ring in the nave is the fight's own tell. What it has is the hum a bell leaves.
            // The Greyfold's held tone, until the white (phase 3).
            t.Add("bed", "heldtone", 0.6f, phase: 1, until: 3).Add(0, 0f, loop, 1f);
            // A bowed cymbal every bar, swelling off the bar line.
            var pulse = t.Add("pulse", "cymbal", 0.45f, phase: 1);
            for (int bar = 0; bar < t.Bars; bar++) pulse.Add(0, bar * 4f + 1f, 3f, bar % 2 == 0 ? 0.8f : 0.6f);
            // "For the flock, going north.": the first bell's hum, high, rising a step a bar.
            var lead = t.Add("lead", "heldtone", 0.7f, phase: 1);
            for (int bar = 0; bar < t.Bars; bar++) lead.Add(BellsNorth[bar], bar * 4f, 4f, 0.9f);
            // Phase 2, two bells in canon: "For the ones who stayed.": the second bell's hum a bar behind the first, a fourth under.
            var voices = t.Add("voices", "heldtone", 0.6f, phase: 2);
            for (int bar = 0; bar < t.Bars; bar++) voices.Add(BellsNorth[(bar + t.Bars - 1) % t.Bars] - 3, bar * 4f, 4f, 0.8f);
            // Phase 3, the great bell, its rope in the white: the held tone leaves, and the great bell's hum sings the
            // roll-call's answer under everything, a bar a note: "We did not forget you."
            var great = t.Add("great", "heldtone", 0.8f, phase: 3);
            var answer = AnswerDegrees(Region.Greyfold);
            for (int i = 0; i < answer.Length; i++) great.Add(answer[i], i * 4f, 4f, 1f);
        }

        // ---- Corra's Drawing: the Blank, the Old Capital District; a child's drawing of her father, crayon, wrong ----
        static void ComposeCorra()
        {
            var t = new Theme { Id = "corra", Region = Region.Blank, Boss = "CorrasDrawing", Bars = 4, RestBars = 0 };
            _themes.Add(t);
            float loop = t.Bars * BeatsPerBar;
            // The crayon's colour: the Remnant's voices under, gone when the crayon runs out.
            t.Add("bed", "remnant", 0.5f, phase: 1, until: 3).Add(0, 0f, loop, 1f).Add(4, 0f, loop, 0.5f);
            // A child counting: the celesta on every beat, low, on the beat (the Blank's clock is off it; hers is not).
            var pulse = t.Add("pulse", "celesta", 0.35f, phase: 1);
            for (int b = 0; b < loop; b++) pulse.Add(b % 4 == 0 ? -7 : -3, b, 0.5f, b % 4 == 0 ? 0.8f : 0.5f);
            // "That's my father. He's coming. Don't.": her song, a child's sing-song on the fifth and the third, the sixth
            // reached for; it does not come home.
            var lead = t.Add("lead", "celesta", 0.6f, phase: 1, until: 3);
            var song = new (int d, float beats)[][]
            {
                new[] { (4, 0.5f), (4, 0.5f), (2, 1f), (5, 0.5f), (4, 0.5f), (2, 1f) },
                new[] { (4, 0.5f), (4, 0.5f), (2, 0.5f), (2, 0.5f), (5, 0.5f), (4, 0.5f), (2, 1f) },
                new[] { (4, 0.5f), (4, 0.5f), (2, 1f), (5, 0.5f), (4, 0.5f), (2, 1f) },
                new[] { (4, 0.5f), (5, 0.5f), (4, 0.5f), (2, 0.5f), (1, 2f) },
            };
            for (int bar = 0; bar < song.Length; bar++)
            {
                float at = bar * 4f;
                foreach (var (d, beats) in song[bar]) { lead.Add(d, at, beats, at % 1f == 0f ? 0.9f : 0.7f); at += beats; }
            }
            // Phase 2: it draws a second Voss, small, holding its hand. Her father's motif, the Guild's: drawn bigger, at
            // twice its length low on the reversed piano ("He was bigger."), and the small one beside it at its own size,
            // high on the celesta, twice, the two starting together.
            var voices = t.Add("voices", "reversedpiano", 0.6f, phase: 2);
            Motif(voices, GuildMotif.Select(m => (m.degree, m.beats * 2f)).ToArray(), 0f, 0, 0.9f);
            var small = t.Add("small", "celesta", 0.4f, phase: 2);
            Guild(small, 0f, 0, 0.7f); Guild(small, 8f, 0, 0.7f);
            // Phase 3: the crayon runs out and it fights in outline, faster, and the room's colour goes: the voices under and
            // her song leave, and the song comes back as its outline, the notes on the beat only, at twice the pace.
            var outline = t.Add("outline", "celesta", 0.55f, phase: 3);
            foreach (var n in lead.Notes.Where(n => n.Start % 1f == 0f))
                for (int twice = 0; twice < 2; twice++) outline.Add(n.Degree, n.Start / 2f + twice * loop / 2f, 0.25f, 0.8f);
        }
    }
}
