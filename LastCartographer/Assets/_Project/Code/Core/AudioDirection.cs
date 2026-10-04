using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>
    /// The audio direction (AUD-01, docs/design/audio-direction.md) as data: each region's score (its beat, mode,
    /// instruments, how much of it is silence, the ambience layers a fade strips away), the roll-call as the game's
    /// one leitmotif and the forms it takes, and the combat rules that keep a read readable. The beat is not only
    /// for the composer: the smudges flicker on it, the bounds-walks and the rhythm bosses keep it, and the tests hold
    /// every one of them to its region's grid.
    /// </summary>
    public static class AudioDirection
    {
        // ---- the regions ----

        public sealed class RegionScore
        {
            public Region Region;
            /// <summary>The mood the bible gives the region (bible 4.x), which the score serves.</summary>
            public string Mood;
            /// <summary>Seconds a beat. Everything rhythmic in the region sits on whole beats of it.</summary>
            public float Beat;
            public string Mode;
            /// <summary>The instruments the region's music is written for, lead first.</summary>
            public string[] Instruments;
            /// <summary>What share of the region's time the music bus is silent by design (0 is always scored).</summary>
            public float Silence;
            /// <summary>The ambience, most present first. Each fade stage takes the last one away.</summary>
            public string[] AmbienceLayers;
            /// <summary>One line for the composer: what the region's music is for.</summary>
            public string Brief;
            public float Bpm => 60f / Beat;
        }

        static readonly Dictionary<Region, RegionScore> _scores = new Dictionary<Region, RegionScore>();

        static void Score(Region r, string mood, float beat, string mode, float silence, string brief, string[] instruments, string[] ambience) =>
            _scores[r] = new RegionScore { Region = r, Mood = mood, Beat = beat, Mode = mode, Silence = silence, Brief = brief, Instruments = instruments, AmbienceLayers = ambience };

        static AudioDirection()
        {
            // The three beats the Complete Survey fights to (boss 6.15: "Saltmarrow's tide", "Emberdown's ash",
            // "Halden's late afternoon") fix the table's spine: slow and tidal, a work-song, a clock.
            Score(Region.Saltmarrow, "wet, patient, superstitious", 0.9f, "D Dorian", 0.2f,
                "The tide keeps the time: a drone that swells and draws back, and a fiddle that waits for it.",
                new[] { "hardanger fiddle", "low whistle", "tongue drum", "concertina", "bowed psaltery" },
                new[] { "tide on the pilings", "reeds", "rain on boardwalk", "gulls far off", "a bell buoy" });
            Score(Region.Emberdown, "stubborn, loud, communal", 0.8f, "G Mixolydian", 0.1f,
                "A work-song for many voices: everyone sings, nobody solos, the anvil keeps the count.",
                new[] { "work-song chorus", "hurdy-gurdy", "frame drum", "anvil", "tuba" },
                new[] { "the furnaces", "falling ash", "hot springs", "picks in the rock", "the Roll-Call Bell's hum" });
            Score(Region.Verdance, "reverent, near-silent, unsettling", 1.2f, "E Phrygian", 0.7f,
                "Mostly nothing. When music comes it is one bowed voice in a very large room, and it stops before it resolves.",
                new[] { "viola da gamba (harmonics)", "bowed glass", "root-chapel organ pedal", "Cantor handbells" },
                new[] { "canopy wind, very high", "dripping moss", "one bird, far off" });
            Score(Region.Halden, "orderly, handsome, wrong", 0.6f, "C major, never cadences", 0.15f,
                "A handsome clockwork piece that loops a bar it never finishes: the Stillness, heard.",
                new[] { "harpsichord", "string quartet", "music box", "Guild brass" },
                new[] { "the city's murmur", "fountains", "the Observatory's clockwork", "paper mills", "copper roofs ticking in the sun" });
            Score(Region.Windreach, "free, lonely, hospitable", 1.0f, "A pentatonic", 0.35f,
                "Open air: a long flute and a plucked string over the wind, and the camp's drum when the fire is lit.",
                new[] { "long flute", "cittern", "overtone voice", "wind harp", "hand drum at the fire" },
                new[] { "wind through grass", "the wagons", "far thunder", "a fire", "standing stones humming" });
            Score(Region.Greyfold, "dread, then awe", 1.5f, "no mode: one held tone", 0.6f,
                "A single held tone that frays at the edges; at the Threshold, a choir arrives all at once.",
                new[] { "held sine and bowed cymbal", "the Half-Cathedral's bells", "low choir (the Threshold only)" },
                new[] { "white noise, very soft", "footsteps, too close" });
            // Faded things sing at half speed: the Blank's beat is Saltmarrow's doubled, and the faded whale on the
            // Bone Bridge sings Runa's roll-call at it (bible 8.1, [F 3.4]).
            Score(Region.Blank, "the faded are not dead", 1.8f, "the roll-call's own, reversed", 0.4f,
                "Everything the player has heard, remembered wrong: motifs reversed, voices of the Remnant under them.",
                new[] { "reversed piano", "celesta", "Remnant voices", "the roll-call, reversed" },
                new[] { "drift", "a voice from another island", "paper settling" });
        }

        public static RegionScore Of(Region r) => _scores[r];
        public static IEnumerable<RegionScore> All => _scores.Values;
        public static float BeatOf(Region r) => _scores[r].Beat;

        /// <summary>The beat of the room a scene draws ("Greybox_Saltmarrow_B" is Saltmarrow's), or null when it's on no region.</summary>
        public static float? BeatOfScene(string scene)
        {
            var r = Mix.RegionOf(scene);
            return r.HasValue ? BeatOf(r.Value) : (float?)null;
        }

        /// <summary>A length that is a whole number of the region's beats (within a frame).</summary>
        public static bool OnGrid(float seconds, Region r, float allowance = 1f / 60f)
        {
            float beats = seconds / BeatOf(r);
            return Math.Abs(beats - Math.Round(beats)) * BeatOf(r) <= allowance && Math.Round(beats) >= 1;
        }

        /// <summary>
        /// A smudge's flicker in a region (combat doc 5: "their rhythm is the region's music tempo"): drawn on the
        /// beat, undrawn on the off-beat, one beat each.
        /// </summary>
        public static (float drawn, float undrawn) SmudgeCycle(Region r) => (BeatOf(r), BeatOf(r));

        /// <summary>The ambience layers left at a fade stage: each stage takes one away, and at least one stays till the place is gone.</summary>
        public static int AmbienceLayersAt(Region r, int stage)
        {
            int n = Of(r).AmbienceLayers.Length;
            if (stage >= FadeStages.Max) return 0;
            return Math.Max(1, n - Math.Max(0, stage));
        }

        // ---- the roll-call ----

        /// <summary>A note: semitones above the tonic, and its length in beats.</summary>
        public struct Note
        {
            public int Pitch;
            public float Beats;
            public Note(int pitch, float beats) { Pitch = pitch; Beats = beats; }
            public override string ToString() => Pitch + ":" + Beats;
        }

        /// <summary>
        /// Runa's roll-call (bible 3.4, 8.1, 9.2): the one tune the game has. The caller lifts into each name half a
        /// beat ahead and the name lands on the beat (the bounds-walk's call, BoundsWalk.CallAhead); when the verse is
        /// done, the chorus answers down to the tonic. In the walk, the name repeats on each beat; the answer closes the verse.
        /// </summary>
        public static class RollCall
        {
            /// <summary>The pickup, a half beat: the second above the reciting tone, falling to it.</summary>
            public static readonly Note[] Call = { new Note(9, 0.5f) };
            /// <summary>The name, on the beat, on the reciting tone (the fifth).</summary>
            public static readonly Note[] Name = { new Note(7, 1f) };
            /// <summary>The chorus's answer: down the scale to the tonic, held.</summary>
            public static readonly Note[] Answer = { new Note(7, 0.5f), new Note(4, 0.5f), new Note(2, 1f), new Note(0, 2f) };

            /// <summary>One name and the answer: the phrase every form of it is made from.</summary>
            public static Note[] Phrase => Call.Concat(Name).Concat(Answer).ToArray();

            public static float PickupBeats => Call.Sum(n => n.Beats);
            public static float Beats(IEnumerable<Note> notes) => notes.Sum(n => n.Beats);

            /// <summary>Played backwards: the Blank's form.</summary>
            public static Note[] Reversed(IEnumerable<Note> notes) => notes.Reverse().ToArray();
            /// <summary>Each note held longer: the whale's form (two, at the Blank's beat).</summary>
            public static Note[] Augmented(IEnumerable<Note> notes, float factor) => notes.Select(n => new Note(n.Pitch, n.Beats * factor)).ToArray();
            /// <summary>Upside down about the reciting tone: Corvin's form, the same tune pulled the other way.</summary>
            public static Note[] Inverted(IEnumerable<Note> notes, int axis = 7) => notes.Select(n => new Note(2 * axis - n.Pitch, n.Beats)).ToArray();
        }

        public sealed class MotifUse
        {
            public string Where, Form, Who;
        }

        /// <summary>Where the roll-call is heard, and in what form. The true ending is the only place it is whole and everyone sings.</summary>
        public static readonly MotifUse[] RollCallUses =
        {
            new MotifUse { Where = "Merrow's End, Dotha's Last Season", Form = "whole, one voice, nine songs short", Who = "Dotha" },
            new MotifUse { Where = "The Bone Bridge (bible 8.1, [F 3.4])", Form = "augmented ×2, at the Blank's beat", Who = "the faded whale" },
            new MotifUse { Where = "The Roll-Call Bell, Emberdown", Form = "whole, at Emberdown's beat", Who = "Runa and the Holdfast" },
            new MotifUse { Where = "Every bounds-walk", Form = "the call on every beat, the answer at each verse's end", Who = "the walk's chorus" },
            new MotifUse { Where = "Hollowvein, the long roll-call (boss 6.4)", Form = "whole, one voice fewer each verse", Who = "the families" },
            new MotifUse { Where = "The Blank's islands", Form = "reversed", Who = "the Remnant" },
            new MotifUse { Where = "The Archivist (boss 6.14)", Form = "inverted about the reciting tone", Who = "Corvin" },
            new MotifUse { Where = "The Complete Survey (boss 6.15)", Form = "the call alone, at each phase's region beat", Who = "the chorus keeping the beat" },
            new MotifUse { Where = "The Open World (bible 9.2)", Form = "whole, every voice she has met", Who = "everyone" },
        };

        // ---- combat ----

        /// <summary>What a telegraph sounds like, one for each kind of boss attack (World's AttackKind, by name).</summary>
        public enum Tell { Strike, Slam, Window, Shape }

        /// <summary>The tell for an attack kind's name; a kind with no tell of its own sounds like a strike.</summary>
        public static Tell TellOf(string attackKind) => Enum.TryParse<Tell>(attackKind, out var t) ? t : Tell.Strike;

        /// <summary>The longest a tell may sound, in frames: never past the shortest read it announces (tier IV's floor).</summary>
        public static int TellFrames(Tell t) => t switch
        {
            Tell.Strike => 6,       // a short, high scratch of the pen
            Tell.Slam => 8,         // a low drawn breath: the only low tell, so it is never mistaken
            Tell.Window => 4,       // a chime as the opening starts; the opening itself is silent
            _ => 8,                 // a swell that follows the shape across the floor
        };

        /// <summary>Who keeps their voice when too much sounds at once, first kept first.</summary>
        public static readonly string[] Priority =
        {
            "telegraph tells", "Wren hurt", "dialogue", "Wren's strikes and Bind", "enemy hits and deaths",
            "world one-shots", "ambience", "music",
        };

        /// <summary>A one-shot's place in <see cref="Priority"/>, in that order: the sound bank keeps the first and drops the last (AUD-10).</summary>
        public enum Voice { Tell, WrenHurt, Dialogue, Wren, Enemy, World, Ambience, Music }

        /// <summary>What <see cref="Priority"/> calls a voice.</summary>
        public static string PriorityOf(Voice v) => Priority[(int)v];

        /// <summary>The least an unpaused snapshot may leave the Sfx bus, where the tells ride.</summary>
        public const float TellBusFloor = 0.8f;
        /// <summary>At most this many one-shots at once; beyond it the lowest in <see cref="Priority"/> is dropped.</summary>
        public const int VoiceLimit = 24;

        // ---- delivery ----

        /// <summary>Loudness targets for delivered audio, integrated LUFS, and the true-peak ceiling.</summary>
        public const float MusicLufs = -18f, AmbienceLufs = -24f, DialogueLufs = -20f, SfxPeakDbtp = -1f;
    }
}
