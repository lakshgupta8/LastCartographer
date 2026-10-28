using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>
    /// Wren's feet on the world's ground (AUD-12, docs/design/footsteps.md). Every ground block wears a paper-kit tile
    /// (`Ground_Boardwalk`, `Ground_Ash`, `Ground_Moss`...), and the tile says what it is drawn as, so it says what a
    /// step on it sounds like: a <see cref="Surface"/>. A step is three takes of the surface's sound, turned in order so
    /// no two in a row are the same; a landing is the surface under the pen set down. Where a block wears no tile (the
    /// greybox) the region says what the ground is. Ink and paper like everything else: a few lines of noise, filters
    /// and envelopes, registered into <see cref="InkSounds"/>' table and ranked as world one-shots, the first dropped.
    /// </summary>
    public static class FootstepSounds
    {
        public enum Surface { Wood, Stone, Iron, Ash, Moss, Earth, Grass, Sand, Water, Paper }

        /// <summary>Takes of each step, turned in order.</summary>
        public const int Takes = 3;

        /// <summary>A step's cue: step_&lt;surface&gt;_&lt;take&gt;, take 1 to <see cref="Takes"/>.</summary>
        public static string StepCue(Surface s, int take) => "step_" + s.ToString().ToLowerInvariant() + "_" + (1 + ((take % Takes) + Takes) % Takes);
        public static string LandCue(Surface s) => "landing_" + s.ToString().ToLowerInvariant();

        // ---- what the ground is ----

        static readonly Dictionary<string, Surface> _tiles = new Dictionary<string, Surface>(StringComparer.OrdinalIgnoreCase)
        {
            // the coast
            ["Boardwalk"] = Surface.Wood, ["Boardwalk_Faded"] = Surface.Wood, ["Boardwalk_Weak"] = Surface.Wood, ["Boardwalk_Hidden"] = Surface.Wood,
            ["Stone"] = Surface.Stone, ["Shallows"] = Surface.Water,
            // the highland
            ["Basalt"] = Surface.Stone, ["Iron"] = Surface.Iron, ["Timber"] = Surface.Wood, ["Ash"] = Surface.Ash,
            // the forest
            ["Root"] = Surface.Moss, ["Moss"] = Surface.Moss, ["Flag"] = Surface.Stone, ["Lane"] = Surface.Earth,
            // the city
            ["Granite"] = Surface.Stone, ["Boards"] = Surface.Wood, ["Parquet"] = Surface.Wood, ["Cobble"] = Surface.Stone,
            // the steppe
            ["Turf"] = Surface.Grass, ["Cracked"] = Surface.Earth, ["Lip"] = Surface.Stone, ["Cinder"] = Surface.Ash,
            // the Greyfold
            ["Chalk"] = Surface.Earth, ["Cobbles"] = Surface.Stone, ["WhiteSand"] = Surface.Sand, ["Line"] = Surface.Paper,
            // the Blank
            ["Grey"] = Surface.Stone, ["Street"] = Surface.Stone, ["Crayon"] = Surface.Paper, ["Causeway"] = Surface.Stone,
        };

        /// <summary>The tiles the table knows, by their kit name without "Ground_".</summary>
        public static IEnumerable<string> KnownTiles => _tiles.Keys;

        /// <summary>
        /// The surface a ground block's material says: "M_Ground_Boardwalk", "Ground_Ash", "M_Ground_Moss (Instance)" all
        /// read as their tile. Null for a material that is no kit tile (the greybox's grey).
        /// </summary>
        public static Surface? OfMaterial(string materialName)
        {
            if (string.IsNullOrEmpty(materialName)) return null;
            string n = materialName;
            int paren = n.IndexOf(" (", StringComparison.Ordinal);
            if (paren > 0) n = n.Substring(0, paren);
            if (n.StartsWith("M_", StringComparison.Ordinal)) n = n.Substring(2);
            if (!n.StartsWith("Ground_", StringComparison.Ordinal)) return null;
            n = n.Substring("Ground_".Length);
            return _tiles.TryGetValue(n, out var s) ? s : (Surface?)null;
        }

        /// <summary>What the ground is where no tile says: the region's most common ground.</summary>
        public static Surface OfRegion(Region? r) => r switch
        {
            Region.Saltmarrow => Surface.Wood,
            Region.Emberdown => Surface.Ash,
            Region.Verdance => Surface.Moss,
            Region.Halden => Surface.Stone,
            Region.Windreach => Surface.Grass,
            Region.Greyfold => Surface.Earth,
            Region.Blank => Surface.Paper,
            _ => Surface.Paper,
        };

        /// <summary>How far she runs between footfalls, from the run clip's loop (two footfalls a loop); a run at 9 u/s on a 12-frame 12 fps loop steps every 4.5 units.</summary>
        public static float Stride(float runSpeed, float runLoopSeconds) =>
            runLoopSeconds > 0.05f ? Math.Max(0.8f, runSpeed * runLoopSeconds / 2f) : DefaultStride;

        /// <summary>The stride with no run clip to keep time to: about 3.5 steps a second at a full run.</summary>
        public const float DefaultStride = 2.6f;

        /// <summary>A landing's loudness by how long she was in the air: a hop is half, a long fall is whole.</summary>
        public static float LandGain(float airSeconds) => Math.Max(0.5f, Math.Min(1f, 0.5f + airSeconds));

        // ---- a fading place (AUD-17) ----

        /// <summary>The fade stage from which the ground's drawing is gone and only the page is underfoot.</summary>
        public const int PaperStage = 3;

        /// <summary>What a step is on in a place this faded: the ground itself, until the drawing of it is gone (stage
        /// <see cref="PaperStage"/>), and then the page. Forgetting is subtraction (audio-direction 1): the ground loses
        /// its sound before it loses its colour.</summary>
        public static Surface UnderFade(Surface s, int stage) => stage >= PaperStage ? Surface.Paper : s;

        /// <summary>How loud her feet are in a place this faded: whole when it is whole, thinning a stage at a time, a
        /// whisper where it is erased.</summary>
        public static float FadeGain(int stage) => Math.Max(0, Math.Min(stage, FadeStages.Max)) switch { 0 => 1f, 1 => 0.85f, 2 => 0.65f, 3 => 0.45f, _ => 0.3f };

        // ---- other birds' feet (AUD-23) ----

        /// <summary>How far a townsfolk this tall walks between footfalls: a sparrow's short steps, a goose's long ones.</summary>
        public static float NpcStride(float height) => Math.Max(0.4f, 0.55f * height);

        /// <summary>The pitch another bird's step is played at: the same ground, under smaller feet higher, under bigger lower.</summary>
        public static float NpcPitch(float height) => (float)Math.Max(0.75, Math.Min(1.3, Math.Sqrt(1.8 / Math.Max(0.5f, height))));

        /// <summary>How loud another bird's step is against hers: lighter, and lighter the smaller the bird; never hers.</summary>
        public static float NpcGain(float height) => Math.Max(0.3f, Math.Min(0.7f, 0.25f + 0.2f * height));

        // ---- the wall (AUD-17; by its surface, AUD-21) ----

        /// <summary>The Talonhold's catch as she grips a wall of this surface.</summary>
        public static string ClingCue(Surface s) => "wall_cling_" + s.ToString().ToLowerInvariant();
        /// <summary>Her talons scraping down a wall of this surface once the hold is spent; a loop.</summary>
        public static string SlideCue(Surface s) => "wall_slide_" + s.ToString().ToLowerInvariant();
        /// <summary>The push off a wall of this surface.</summary>
        public static string KickCue(Surface s) => "wall_kick_" + s.ToString().ToLowerInvariant();

        /// <summary>
        /// A wall as her talons find it: the pitch their scrape takes on it and how narrow (iron screeches, moss barely
        /// speaks), how hard it is (how much of the scratch there is, against the surface's own step), and the grit that
        /// catches and lets go as she slides.
        /// </summary>
        public static (float hz, float q, float hard, float grit, float gritLo, float gritHi) WallOf(Surface s) => s switch
        {
            Surface.Wood => (1500f, 1.5f, 0.7f, 25f, 1600f, 2800f),
            Surface.Stone => (2600f, 3f, 1f, 40f, 2600f, 4200f),
            Surface.Iron => (1900f, 8f, 1f, 15f, 3000f, 4500f),
            Surface.Ash => (1700f, 1.2f, 0.5f, 120f, 2200f, 5000f),
            Surface.Moss => (600f, 1f, 0.15f, 10f, 900f, 1600f),
            Surface.Earth => (1200f, 1.2f, 0.5f, 60f, 1800f, 3200f),
            Surface.Grass => (2400f, 1f, 0.3f, 80f, 2500f, 5000f),
            Surface.Sand => (1900f, 1f, 0.5f, 150f, 3000f, 6000f),
            Surface.Water => (2200f, 1f, 0.2f, 50f, 1500f, 3000f),
            _ => (1800f, 1.5f, 0.8f, 30f, 2200f, 3600f),   // paper: the page itself
        };

        // ---- the sounds ----

        internal static void Register()
        {
            foreach (Surface s in Enum.GetValues(typeof(Surface)))
            {
                var surface = s;
                for (int k = 0; k < Takes; k++)
                {
                    int take = k;
                    InkSounds.Add(StepCue(s, k), InkSounds.Kind.Wren, "a step on " + What(s) + ", take " + (k + 1), () => Step(surface, take), StepGain(s), false, null, AudioDirection.Voice.World);
                }
                InkSounds.Add(LandCue(s), InkSounds.Kind.Wren, "a landing on " + What(s), () => Land(surface), 0.5f, false, null, AudioDirection.Voice.World);
            }
            foreach (Surface s in Enum.GetValues(typeof(Surface)))
            {
                var surface = s;
                string on = WallWhat(s);
                InkSounds.Add(ClingCue(s), InkSounds.Kind.Wren, "the Talonhold's catch on " + on + ": talons biting, and the wall under them", () => Cling(surface), 0.35f, false, null, AudioDirection.Voice.World);
                InkSounds.Add(SlideCue(s), InkSounds.Kind.Wren, "talons scraping down " + on + ", the hold spent", () => Slide(surface), 0.3f, true, null, AudioDirection.Voice.World);
                InkSounds.Add(KickCue(s), InkSounds.Kind.Wren, "the push off " + on + ": a scuff and the air", () => Kick(surface), 0.4f, false, null, AudioDirection.Voice.World);
            }
        }

        static string WallWhat(Surface s) => s switch
        {
            Surface.Wood => "a wall of boards",
            Surface.Stone => "stone",
            Surface.Iron => "iron, screeching",
            Surface.Ash => "an ash bank, crumbling",
            Surface.Moss => "moss, soft",
            Surface.Earth => "an earth bank, gritty",
            Surface.Grass => "turf, swishing",
            Surface.Sand => "sand, running",
            Surface.Water => "a wet wall",
            _ => "the page",
        };

        static string What(Surface s) => s switch
        {
            Surface.Wood => "boards: a hollow knock",
            Surface.Stone => "stone: a hard click",
            Surface.Iron => "iron plate: a short clank",
            Surface.Ash => "ash: a crunch",
            Surface.Moss => "moss: almost nothing, soft",
            Surface.Earth => "earth: a dull pat and a little grit",
            Surface.Grass => "grass: a swish",
            Surface.Sand => "sand: a shuffle",
            Surface.Water => "the shallows: a splash",
            _ => "paper: a dry tick",
        };

        /// <summary>Steps are quiet, and the soft grounds quieter: moss is nearly silent, iron the loudest.</summary>
        static float StepGain(Surface s) => s switch
        {
            Surface.Moss => 0.2f,
            Surface.Paper => 0.2f,
            Surface.Sand => 0.25f,
            Surface.Grass => 0.25f,
            Surface.Iron => 0.4f,
            Surface.Water => 0.4f,
            _ => 0.3f,
        };

        static float[] Buf(float seconds) => InkSounds.Buf(seconds);

        /// <summary>One footfall: the take changes the pitch a little and the noise's seed, so three in a row are three.</summary>
        static float[] Step(Surface s, int take)
        {
            float v = 1f + (take - 1) * 0.06f;            // 0.94, 1.0, 1.06
            int seed = 1100 + (int)s * 10 + take;
            var b = Buf(s == Surface.Water ? 0.16f : 0.1f);
            switch (s)
            {
                case Surface.Wood:
                    InkSounds.Drop(b, 0f, 0.07f, 210f * v, 160f * v, 1f, 0.001f, 0.02f);
                    EnemySounds.Click(b, 0f, 2200f * v, 0.5f, seed);
                    InkSounds.Smear(b, 0f, 0.05f, 900f * v, 500f, 0.3f, seed + 5, 0.001f, 0.015f);
                    break;
                case Surface.Stone:
                    EnemySounds.Click(b, 0f, 3200f * v, 1f, seed);   // a hard click: nothing low under it
                    InkSounds.Scratch(b, 0f, 0.04f, 2200f * v, 1700f * v, 3f, 1f, seed + 5, 0.001f, 0.01f);
                    break;
                case Surface.Iron:
                    EnemySounds.Click(b, 0f, 3800f, 0.6f, seed);
                    EnemySounds.Ring(b, 0f, 0.09f, 820f * v, 1f, 0.03f, EnemySounds.Brass);
                    break;
                case Surface.Ash:
                    EnemySounds.Grain(b, 0f, 0.08f, 300f, 0.004f, 2200f * v, 5000f * v, 2f, 1f, seed);
                    InkSounds.Smear(b, 0f, 0.06f, 700f * v, 300f, 0.5f, seed + 5, 0.002f, 0.02f);
                    break;
                case Surface.Moss:
                    InkSounds.Smear(b, 0f, 0.08f, 420f * v, 220f, 1f, seed, 0.006f, 0.025f);
                    break;
                case Surface.Earth:
                    InkSounds.Drop(b, 0f, 0.06f, 120f * v, 80f, 1f, 0.002f, 0.02f);
                    InkSounds.Smear(b, 0f, 0.06f, 600f * v, 300f, 0.6f, seed, 0.002f, 0.02f);
                    EnemySounds.Grain(b, 0.01f, 0.05f, 120f, 0.004f, 1800f, 3200f, 2f, 0.3f, seed + 5);
                    break;
                case Surface.Grass:
                    EnemySounds.Whoosh(b, 0f, 0.09f, 1600f * v, 3200f * v, 1.2f, 1f, seed, 0.35f);
                    EnemySounds.Grain(b, 0f, 0.07f, 150f, 0.005f, 2500f, 5000f, 2f, 0.3f, seed + 5);
                    break;
                case Surface.Sand:
                    InkSounds.Scratch(b, 0f, 0.09f, 1900f * v, 1300f * v, 1f, 1f, seed, 0.01f, 0.04f);
                    EnemySounds.Grain(b, 0f, 0.08f, 250f, 0.003f, 3000f, 6000f, 2f, 0.3f, seed + 5);
                    break;
                case Surface.Water:
                    InkSounds.Smear(b, 0f, 0.1f, 2400f * v, 900f, 1f, seed, 0.002f, 0.04f);
                    InkSounds.Drop(b, 0.02f, 0.05f, 700f * v, 1400f * v, 0.4f, 0.002f, 0.02f);   // a bubble rising
                    InkSounds.Drop(b, 0.07f, 0.05f, 900f * v, 1700f * v, 0.3f, 0.002f, 0.02f);
                    break;
                default:   // paper
                    InkSounds.Scratch(b, 0f, 0.03f, 2600f * v, 2200f * v, 2f, 1f, seed, 0.001f, 0.01f);
                    break;
            }
            return b;
        }

        /// <summary>Another sound laid into this one at a time and a gain, cut at its end.</summary>
        static void Lay(float[] into, float[] what, float at, float gain)
        {
            int o = (int)(at * InkSounds.SampleRate);
            for (int i = 0; i < what.Length && o + i < into.Length; i++) into[o + i] += what[i] * gain;
        }

        /// <summary>The catch: two quick scratches of the talons, as hard as the wall lets them be, and the wall itself
        /// taking her weight (its own step, under them).</summary>
        static float[] Cling(Surface s)
        {
            var w = WallOf(s);
            int seed = 1400 + (int)s * 10;
            var b = Buf(0.14f);
            float k = w.hz / 1800f;
            InkSounds.Scratch(b, 0f, 0.04f, 3000f * k, 2200f * k, 2.5f, w.hard, seed, 0.001f, 0.012f);
            InkSounds.Scratch(b, 0.03f, 0.04f, 2800f * k, 2000f * k, 2.5f, 0.8f * w.hard, seed + 1, 0.001f, 0.012f);
            Lay(b, Step(s, 0), 0.02f, 1f - 0.5f * w.hard);
            return b;
        }

        const float SlideOverlap = 0.05f;

        /// <summary>The scrape down: a steady drag at the wall's pitch and the grit catching and letting go; a soft wall
        /// is more its own rub than any scratch, a hard one all scratch.</summary>
        static float[] Slide(Surface s)
        {
            var w = WallOf(s);
            int seed = 1402 + (int)s * 10;
            float len = 0.6f + SlideOverlap;
            var b = Buf(len);
            InkSounds.Scratch(b, 0f, len, w.hz, w.hz, w.q, 0.6f * w.hard, seed, 0.05f, 10f);              // the drag, steady
            EnemySounds.Grain(b, 0f, len, w.grit, 0.008f, w.gritLo, w.gritHi, 2.5f, 1f, seed + 1);         // the talons catching and letting go
            if (w.hard < 0.5f) InkSounds.Smear(b, 0f, len, w.hz * 0.6f, w.hz * 0.6f, 0.6f * (1f - w.hard), seed + 2, 0.05f, 10f);   // the wall's own rub
            return EnemySounds.Seamless(b, SlideOverlap);
        }

        /// <summary>The push off: the wall's step under the foot that kicks, the talons' scuff and the air.</summary>
        static float[] Kick(Surface s)
        {
            var w = WallOf(s);
            int seed = 1404 + (int)s * 10;
            var b = Buf(0.2f);
            Lay(b, Step(s, 2), 0f, 0.8f);
            InkSounds.Scratch(b, 0f, 0.05f, w.hz * 0.9f, w.hz * 0.6f, 1.5f, w.hard, seed, 0.001f, 0.02f);
            EnemySounds.Whoosh(b, 0.02f, 0.15f, 900f, 2200f, 1.2f, 0.6f, seed + 1, 0.3f);
            return b;
        }

        /// <summary>A landing: the step's sound heavier and longer, with the body's weight under it.</summary>
        static float[] Land(Surface s)
        {
            int seed = 1300 + (int)s * 10;
            var b = Buf(0.22f);
            InkSounds.Drop(b, 0f, 0.1f, 110f, 60f, 0.6f, 0.001f, 0.035f);   // the weight
            switch (s)
            {
                case Surface.Wood:
                    InkSounds.Drop(b, 0f, 0.12f, 180f, 130f, 1f, 0.001f, 0.035f);
                    EnemySounds.Click(b, 0f, 2000f, 0.5f, seed);
                    break;
                case Surface.Stone:
                    EnemySounds.Click(b, 0f, 3000f, 1f, seed);
                    InkSounds.Scratch(b, 0f, 0.06f, 1600f, 1100f, 2f, 0.7f, seed + 1, 0.001f, 0.015f);
                    break;
                case Surface.Iron:
                    EnemySounds.Click(b, 0f, 3600f, 0.6f, seed);
                    EnemySounds.Ring(b, 0f, 0.2f, 700f, 1f, 0.06f, EnemySounds.Brass);
                    break;
                case Surface.Ash:
                    EnemySounds.Grain(b, 0f, 0.15f, 300f, 0.004f, 2000f, 5000f, 2f, 1f, seed, 60f);
                    InkSounds.Smear(b, 0f, 0.12f, 800f, 250f, 0.6f, seed + 1, 0.002f, 0.04f);
                    break;
                case Surface.Moss:
                    InkSounds.Smear(b, 0f, 0.14f, 380f, 160f, 1f, seed, 0.008f, 0.04f);
                    break;
                case Surface.Earth:
                    InkSounds.Smear(b, 0f, 0.12f, 600f, 220f, 0.8f, seed, 0.002f, 0.04f);
                    EnemySounds.Grain(b, 0.02f, 0.12f, 100f, 0.004f, 1800f, 3200f, 2f, 0.4f, seed + 1, 20f);
                    break;
                case Surface.Grass:
                    EnemySounds.Whoosh(b, 0f, 0.15f, 1400f, 3000f, 1.2f, 1f, seed, 0.3f);
                    break;
                case Surface.Sand:
                    InkSounds.Scratch(b, 0f, 0.16f, 1800f, 1000f, 1f, 1f, seed, 0.01f, 0.06f);
                    EnemySounds.Grain(b, 0f, 0.14f, 250f, 0.003f, 3000f, 6000f, 2f, 0.3f, seed + 1, 40f);
                    break;
                case Surface.Water:
                    InkSounds.Smear(b, 0f, 0.18f, 2600f, 700f, 1f, seed, 0.002f, 0.07f);
                    for (int k = 0; k < 3; k++) InkSounds.Drop(b, 0.04f + k * 0.04f, 0.05f, 700f + k * 150f, 1400f + k * 250f, 0.35f, 0.002f, 0.02f);
                    break;
                default:
                    InkSounds.Scratch(b, 0f, 0.05f, 2400f, 1800f, 2f, 1f, seed, 0.001f, 0.015f);
                    break;
            }
            return b;
        }
    }
}
