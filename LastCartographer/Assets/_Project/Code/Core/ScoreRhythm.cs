using System.Linq;

namespace OWSBG.Core
{
    public static partial class Score
    {
        /// <summary>The Collapse's lamps (boss 6.4): one lit a beat, in turn, so a bar of four is one round of them.</summary>
        public const int CollapseLamps = 4;
        /// <summary>A verse of the Complete Survey (boss 6.15): beats on named ground, then beats the chorus breathes.</summary>
        public const int SurveyVerseBeats = 8, SurveyBreakBeats = 2;
        /// <summary>The Complete Survey's inks in order, each a region's key and beat: "Saltmarrow's tide", "Emberdown's ash", "Halden's late afternoon".</summary>
        public static readonly Region[] SurveyInks = { Region.Saltmarrow, Region.Emberdown, Region.Halden };

        /// <summary>The roll-call's call as a mode's degrees: the lift (the sixth) half a beat ahead, then the name (the fifth) on the beat.</summary>
        public static (int lift, int name) CallDegrees(Region r) =>
            (DegreeOf(r, AudioDirection.RollCall.Call[0].Pitch), DegreeOf(r, AudioDirection.RollCall.Name[0].Pitch));

        /// <summary>
        /// The rhythm bosses' themes and the Brood's (AUD-13, docs/design/music.md §3d). The Collapse and the Complete
        /// Survey are scored to the beat the fight keeps (audio-direction 4): a pulse on every beat and the chorus calling
        /// a name on it, the lift half a beat ahead, as a bounds-walk calls. The driver holds their loops to the fight's
        /// own clock, so a hearing player can play the fight by ear. The Brood is a chant in reeds.
        /// </summary>
        static void ComposeRhythm()
        {
            ComposeCollapse();
            for (int i = 0; i < SurveyInks.Length; i++) ComposeSurvey(SurveyInks[i], i + 1);
            ComposeBrood();
        }

        /// <summary>The chorus's call on a beat: the lift half a beat before it (wrapping into the loop's end), the name on it.</summary>
        static void Call(Stem s, Region r, float beat, float loopBeats, float level = 1f)
        {
            var (lift, name) = CallDegrees(r);
            float liftAt = beat - 0.5f;
            if (liftAt < 0f) liftAt += loopBeats;
            s.Add(lift, liftAt, 0.5f, level * 0.8f).Add(name, beat, 0.5f, level);
        }

        // ---- the Collapse: Hollowvein, the long roll-call, the lamps lit in turn by the chorus ----
        static void ComposeCollapse()
        {
            var r = Region.Emberdown;
            var t = new Theme { Id = "collapse", Region = r, Boss = "Collapse", Bars = 4, RestBars = 0, KeepsBeat = true };
            _themes.Add(t);
            float loop = t.Bars * BeatsPerBar;
            // The pulse: a frame drum on every beat, the lamp round's first beat the strongest; strike on the drum.
            var pulse = t.Add("pulse", "framedrum", 0.9f, phase: 1);
            for (int b = 0; b < loop; b++) pulse.Add(0, b, 0.5f, b % CollapseLamps == 0 ? 1f : 0.7f);
            // The mine's drone, gone when it reaches for the lamps.
            t.Add("bed", "hurdygurdy", 0.55f, phase: 1, until: 3).Add(0, 0f, loop, 1f).Add(4, 0f, loop, 0.6f);
            // The chorus: a name called on each beat for three rounds, then the answer, home on the tonic.
            var voices = t.Add("voices", "choir", 0.75f, phase: 1, until: 3);
            for (int b = 0; b < 12; b++) Call(voices, r, b, loop);
            var answer = AnswerDegrees(r);
            voices.Add(answer[0], 12f, 0.5f).Add(answer[1], 12.5f, 0.5f).Add(answer[2], 13f, 1f).Add(answer[3], 14f, 1.5f);
            // Phase 2: the ink surges; the tuba walks under it and the anvil keeps two and four.
            var lead = t.Add("lead", "tuba", 0.75f, phase: 2);
            int[] walk = { 0, -3, -2, -3 };
            for (int bar = 0; bar < t.Bars; bar++) lead.Add(walk[bar], bar * 4f, 2f, 0.9f).Add(walk[bar] + 2, bar * 4f + 2f, 2f, 0.7f);
            var count = t.Add("count", "anvil", 0.5f, phase: 2);
            for (int bar = 0; bar < t.Bars; bar++) count.Add(7, bar * 4f + 1f, 0.5f, 0.9f).Add(7, bar * 4f + 3f, 0.5f, 0.7f);
            // Phase 3: it puts the lamps out; the chorus is a voice fewer, the last beat of each round unsung.
            var fewer = t.Add("fewer", "choir", 0.7f, phase: 3);
            for (int b = 0; b < loop; b++) if (b % CollapseLamps != CollapseLamps - 1) Call(fewer, r, b, loop, 0.9f);
        }

        // ---- the Complete Survey: the page fixing her in each region's ink, the chorus keeping the only safe rhythm ----
        static void ComposeSurvey(Region r, int phase)
        {
            string ink = r == Region.Saltmarrow ? "tide" : r == Region.Emberdown ? "ash" : "afternoon";
            int verse = SurveyVerseBeats + SurveyBreakBeats;
            var t = new Theme { Id = "survey-" + ink, Region = r, Boss = "CompleteSurvey", Bars = verse * 2 / BeatsPerBar, RestBars = 0, KeepsBeat = true, ForPhase = phase };
            _themes.Add(t);
            float loop = t.Bars * BeatsPerBar;   // two verses
            string beatInstrument = r == Region.Saltmarrow ? "drum" : r == Region.Emberdown ? "framedrum" : "harpsichord";
            string bedInstrument = r == Region.Saltmarrow ? "drone" : r == Region.Emberdown ? "hurdygurdy" : "strings";
            // The pulse on every beat, the verse's first the strongest, and softer while the chorus breathes.
            var pulse = t.Add("pulse", beatInstrument, 0.85f, phase: 1);
            for (int b = 0; b < loop; b++)
            {
                int inVerse = b % verse;
                pulse.Add(0, b, 0.5f, inVerse == 0 ? 1f : inVerse >= SurveyVerseBeats ? 0.4f : 0.7f);
            }
            t.Add("bed", bedInstrument, 0.5f, phase: 1).Add(0, 0f, loop, 1f).Add(4, 0f, loop, 0.6f);
            // The chorus (Runa, and everyone met): a name called on every beat of a verse; in the break, the answer, and breath.
            var voices = t.Add("voices", "choir", 0.8f, phase: 1);
            var answer = AnswerDegrees(r);
            for (int v = 0; v < 2; v++)
            {
                float at = v * verse;
                for (int b = 0; b < SurveyVerseBeats; b++) Call(voices, r, at + b, loop);
                voices.Add(answer[0], at + SurveyVerseBeats, 0.5f, 0.8f).Add(answer[1], at + SurveyVerseBeats + 0.5f, 0.5f, 0.8f).Add(answer[2], at + SurveyVerseBeats + 1f, 0.5f, 0.8f);
            }
            // The region's own tune underneath, as far as it goes in two verses: which ink this is.
            var lead = t.Add("lead", ThemeOf(r).Stem("lead").Instrument, 0.55f, phase: 1);
            foreach (var n in ThemeOf(r).Stem("lead").Notes.Where(n => n.Start + n.Beats <= loop)) lead.Notes.Add(n);
            if (lead.Notes.Count == 0) lead.Add(4, 0f, 2f);
        }

        // ---- the Reedmother's Brood: the chicks in chorus, "Ours. Ours. Ours."; the Reedmother has no voice ----
        static void ComposeBrood()
        {
            var r = Region.Saltmarrow;
            var t = new Theme { Id = "brood", Region = r, Boss = "ReedmotherBrood", Bars = 8, RestBars = 0 };
            _themes.Add(t);
            float loop = t.Bars * BeatsPerBar;
            // Their feet: the tongue drum in quick pairs.
            var pulse = t.Add("pulse", "drum", 0.7f, phase: 1);
            for (int b = 0; b < loop; b++) { pulse.Add(0, b, 0.25f, b % 4 == 0 ? 1f : 0.6f); if (b % 2 == 1) pulse.Add(0, b + 0.5f, 0.25f, 0.4f); }
            // "Ours. Ours. Ours.": the chant in reeds, three on the fifth and a beat's breath, every bar.
            var voices = t.Add("voices", "concertina", 0.75f, phase: 1, until: 3);
            for (int bar = 0; bar < t.Bars; bar++) voices.Add(4, bar * 4f, 0.75f).Add(4, bar * 4f + 1f, 0.75f).Add(4, bar * 4f + 2f, 1f);
            // Phase 2: the nest opens and the smoke comes in from the east: the drone, and the fiddle frantic over it.
            t.Add("bed", "drone", 0.7f, phase: 2).Add(0, 0f, loop, 1f).Add(2, 0f, loop, 0.5f);
            var lead = t.Add("lead", "fiddle", 0.7f, phase: 2);
            int[] run = { 4, 5, 6, 5, 4, 3, 4, 2 };
            for (int bar = 0; bar < t.Bars; bar++) for (int k = 0; k < 8; k++) lead.Add(run[(k + bar) % run.Length], bar * 4f + k * 0.5f, 0.5f, k % 2 == 0 ? 0.9f : 0.6f);
            // Phase 3: the fire reaches the nest and the chicks huddle on it: "...ours?", the chant asking, and the fire's tremolo.
            var huddle = t.Add("huddle", "concertina", 0.6f, phase: 3);
            for (int bar = 0; bar < t.Bars; bar += 2) huddle.Add(4, bar * 4f, 1f, 0.8f).Add(4, bar * 4f + 1f, 1f, 0.7f).Add(5, bar * 4f + 2.5f, 1.5f, 0.6f);
            var fire = t.Add("fire", "psaltery", 0.45f, phase: 3);
            for (int b = 0; b < loop; b++) fire.Add(7 + (b / 4) % 3, b, 0.5f, 0.6f).Add(8 + (b / 4) % 3, b + 0.5f, 0.5f, 0.5f);
        }
    }
}
