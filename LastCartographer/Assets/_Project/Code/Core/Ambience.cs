using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>
    /// The ambience (AUD-05, docs/design/ambience.md): every layer the direction names for every region
    /// (<see cref="AudioDirection.RegionScore.AmbienceLayers"/>, most present first), each made from a recipe (a wash of
    /// filtered noise breathing on slow cycles, sparse drops or ticks, a far call, a toll, a hum, a fire's crackle,
    /// footsteps too close) into its own seamless loop of a length no other layer shares, so nothing locks. A fade stage
    /// takes the last layer away (<see cref="AudioDirection.AmbienceLayersAt"/>) and the mix dulls what is left; an
    /// erased place is silent. Pure C#: the driver renders on a worker, the exporter writes the files.
    /// </summary>
    public static class Ambience
    {
        public const int SampleRate = RollCallSong.SampleRate;
        /// <summary>Loop lengths by layer index, seconds: no two share a factor, so the layers drift against each other.</summary>
        public static readonly int[] LoopSeconds = { 11, 13, 17, 19, 23 };
        /// <summary>The level of each layer by index: most present first.</summary>
        public static readonly float[] Levels = { 1f, 0.8f, 0.65f, 0.5f, 0.4f };
        /// <summary>The RMS every layer is brought to, dBFS: the direction's −24 LUFS, near enough for a bed.</summary>
        public const float TargetRmsDb = -24f;

        public enum Recipe { Wash, Drops, Ticks, Calls, Toll, Hum, Rumble, Crackle, Steps, Clockwork }

        public sealed class Layer
        {
            public Region Region;
            public int Index;
            public string Name;
            public Recipe Recipe;
            /// <summary>Recipe parameters: for a wash the low-pass, high-pass and breath period; for events their rate and pitch; see <see cref="Render"/>.</summary>
            public float A, B, C;
            public int Seconds => LoopSeconds[Math.Min(Index, LoopSeconds.Length - 1)];
            public float Level => Levels[Math.Min(Index, Levels.Length - 1)];
            public string Slug => Name.ToLowerInvariant().Replace("'", "").Replace(",", "").Replace(' ', '-');
            /// <summary>The delivery name: region_ambience_layer.wav (no beat: a bed).</summary>
            public string FileName => Region.ToString().ToLowerInvariant() + "_ambience_" + Slug + ".wav";
        }

        static readonly Dictionary<string, (Recipe recipe, float a, float b, float c)> _recipes = new Dictionary<string, (Recipe, float, float, float)>
        {
            // ---- Saltmarrow: wet, patient ----
            ["tide on the pilings"] = (Recipe.Wash, 500f, 40f, 9f),          // a low wash breathing every nine seconds
            ["reeds"] = (Recipe.Wash, 4000f, 700f, 5f),                        // a thin hiss, moving
            ["rain on boardwalk"] = (Recipe.Drops, 14f, 1800f, 0.03f),         // fourteen drops a second on wood
            ["gulls far off"] = (Recipe.Calls, 0.25f, 900f, 0.35f),            // a call every four seconds or so, high, far
            ["a bell buoy"] = (Recipe.Toll, 0.14f, 330f, 2.5f),                // a toll every seven seconds, ringing long
            // ---- Emberdown: stubborn, loud ----
            ["the furnaces"] = (Recipe.Wash, 400f, 40f, 4f),                   // a deep roar
            ["falling ash"] = (Recipe.Wash, 2500f, 900f, 7f),                  // soft, dry, drifting
            ["hot springs"] = (Recipe.Drops, 6f, 500f, 0.08f),                 // slow bubbles
            ["picks in the rock"] = (Recipe.Ticks, 1.6f, 2600f, 0.02f),        // irregular, hard
            ["the Roll-Call Bell's hum"] = (Recipe.Hum, 98f, 0.4f, 0f),        // the bell's G, beating slowly
            // ---- The Verdance: reverent, near-silent ----
            ["canopy wind, very high"] = (Recipe.Wash, 6000f, 2000f, 11f),     // a whisper far above
            ["dripping moss"] = (Recipe.Drops, 0.7f, 1200f, 0.12f),            // a drop every second or two, wet
            ["one bird, far off"] = (Recipe.Calls, 0.08f, 2400f, 0.15f),       // once in twelve seconds
            // ---- Halden Reach: orderly, wrong ----
            ["the city's murmur"] = (Recipe.Wash, 1200f, 200f, 6f),            // many voices, none of them words
            ["fountains"] = (Recipe.Wash, 5000f, 1200f, 3f),                   // bright water
            ["the Observatory's clockwork"] = (Recipe.Clockwork, 0.6f, 3200f, 0f),   // on Halden's beat, exactly
            ["paper mills"] = (Recipe.Wash, 1800f, 300f, 1.4f),                // a rhythmic wash, machines turning
            ["copper roofs ticking in the sun"] = (Recipe.Ticks, 0.9f, 4200f, 0.015f),
            // ---- Windreach: free, lonely ----
            ["wind through grass"] = (Recipe.Wash, 3000f, 300f, 8f),
            ["the wagons"] = (Recipe.Ticks, 2.2f, 700f, 0.05f),                // wood on wood, creaking along
            ["far thunder"] = (Recipe.Rumble, 0.06f, 120f, 4f),                // once in sixteen seconds, long
            ["a fire"] = (Recipe.Crackle, 25f, 2500f, 0f),
            ["standing stones humming"] = (Recipe.Hum, 55f, 0.15f, 1f),       // A1, almost still
            // ---- The Greyfold: dread ----
            ["white noise, very soft"] = (Recipe.Wash, 8000f, 100f, 20f),
            ["footsteps, too close"] = (Recipe.Steps, 0.25f, 160f, 0f),        // a pair of steps every four seconds or so
            // ---- The Blank: the faded are not dead ----
            ["drift"] = (Recipe.Wash, 700f, 80f, 13f),
            ["a voice from another island"] = (Recipe.Calls, 0.06f, 260f, 0.9f),   // low, slow, rare: someone calling a name you cannot hear
            ["paper settling"] = (Recipe.Ticks, 0.5f, 1500f, 0.04f),
        };

        static readonly List<Layer> _layers = new List<Layer>();

        static Ambience()
        {
            foreach (var score in AudioDirection.All)
                for (int i = 0; i < score.AmbienceLayers.Length; i++)
                {
                    string name = score.AmbienceLayers[i];
                    if (!_recipes.TryGetValue(name, out var r)) continue;
                    _layers.Add(new Layer { Region = score.Region, Index = i, Name = name, Recipe = r.recipe, A = r.a, B = r.b, C = r.c });
                }
        }

        public static IReadOnlyList<Layer> Layers => _layers;
        public static List<Layer> Of(Region r) => _layers.Where(l => l.Region == r).OrderBy(l => l.Index).ToList();
        public static Layer Find(Region r, string name) => _layers.FirstOrDefault(l => l.Region == r && l.Name == name);

        /// <summary>Each layer's level at a fade stage: the last goes first, and an erased place has none (audio-direction 5).</summary>
        public static float[] LevelsAt(Region r, int stage)
        {
            var layers = Of(r);
            int keep = AudioDirection.AmbienceLayersAt(r, stage);
            return layers.Select(l => l.Index < keep ? l.Level : 0f).ToArray();
        }

        // ---- the tools ----

        sealed class Rng
        {
            uint _s;
            public Rng(int seed) { _s = (uint)(seed * 2654435761u + 977u); if (_s == 0) _s = 1; }
            public float Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return (_s / 4294967295f) * 2f - 1f; }
            public float Unit() => (Next() + 1f) * 0.5f;
        }

        sealed class OnePole
        {
            float _y;
            public float Low(float x, float hz) { float a = 1f - (float)Math.Exp(-2 * Math.PI * hz / SampleRate); _y += (x - _y) * a; return _y; }
            public float High(float x, float hz) => x - Low(x, hz);
        }

        static void Add(float[] s, int at, float v) { s[((at % s.Length) + s.Length) % s.Length] += v; }

        /// <summary>A decaying sine dropped into the loop at a sample (wrapping): a drop, a toll, a tick's ring.</summary>
        static void Ping(float[] s, int at, float hz, float decay, float gain, float inharmonic = 0f, int partials = 1)
        {
            int len = (int)(decay * 5f * SampleRate);
            for (int p = 1; p <= partials; p++)
            {
                float f = hz * p * (1f + inharmonic * (p - 1)), g = gain / p;
                double ph = 0;
                for (int i = 0; i < len; i++)
                {
                    ph += f / SampleRate;
                    Add(s, at + i, (float)Math.Sin(2 * Math.PI * ph) * (float)Math.Exp(-i / (decay * SampleRate)) * g * Math.Min(1f, i / 48f));
                }
            }
        }

        /// <summary>A burst of noise through a low-pass, decaying: a tick, a step, a crackle.</summary>
        static void Burst(float[] s, int at, float lpHz, float decay, float gain, Rng rng)
        {
            int len = (int)(decay * 5f * SampleRate);
            var lp = new OnePole();
            for (int i = 0; i < len; i++) Add(s, at + i, lp.Low(rng.Next(), lpHz) * (float)Math.Exp(-i / (decay * SampleRate)) * gain);
        }

        /// <summary>Render a layer's loop: seamless (events wrap), brought to the bed's RMS, never over the peak.</summary>
        public static float[] Render(Layer l)
        {
            int len = l.Seconds * SampleRate;
            var s = new float[len];
            var rng = new Rng(Score.Stable(l.Region + "/" + l.Name));
            switch (l.Recipe)
            {
                case Recipe.Wash:
                {
                    // Noise through a low-pass (A) and a high-pass (B), breathing on a cycle of C seconds and a slower one under it.
                    var lp = new OnePole(); var hp = new OnePole(); var lp2 = new OnePole();
                    float twoPi = 2f * (float)Math.PI;
                    // Cycles that divide the loop, so the breath is seamless too.
                    float c1 = Math.Max(1, (float)Math.Round(len / (float)SampleRate / l.C)), c2 = 1f;
                    for (int i = 0; i < len; i++)
                    {
                        float t = i / (float)len;
                        float breath = 0.6f + 0.25f * (float)Math.Sin(twoPi * c1 * t) + 0.15f * (float)Math.Sin(twoPi * c2 * t + 1f);
                        s[i] = hp.High(lp2.Low(lp.Low(rng.Next(), l.A), l.A), l.B) * breath;
                    }
                    break;
                }
                case Recipe.Drops:
                {
                    // A drops a second on average (Poisson-ish), each a short ping around B Hz decaying over C seconds, a little different each time.
                    int n = (int)(l.A * l.Seconds);
                    for (int k = 0; k < n; k++)
                        Ping(s, (int)(rng.Unit() * len), l.B * (0.7f + 0.6f * rng.Unit()), l.C * (0.6f + 0.8f * rng.Unit()), 0.25f + 0.2f * rng.Unit(), 0.01f, 2);
                    break;
                }
                case Recipe.Ticks:
                {
                    int n = (int)(l.A * l.Seconds);
                    for (int k = 0; k < n; k++)
                        Burst(s, (int)(rng.Unit() * len), l.B * (0.6f + 0.8f * rng.Unit()), l.C, 0.5f + 0.4f * rng.Unit(), rng);
                    break;
                }
                case Recipe.Calls:
                {
                    // A calls a second on average: a sine sliding down from B Hz over C seconds with a wobble, far off (soft attack).
                    int n = Math.Max(1, (int)Math.Round(l.A * l.Seconds));
                    for (int k = 0; k < n; k++)
                    {
                        int at = (int)(rng.Unit() * len), dur = (int)(l.C * (0.7f + 0.6f * rng.Unit()) * SampleRate);
                        float f0 = l.B * (0.85f + 0.3f * rng.Unit());
                        double ph = 0;
                        for (int i = 0; i < dur; i++)
                        {
                            float t = i / (float)dur;
                            float f = f0 * (1f - 0.3f * t) * (1f + 0.02f * (float)Math.Sin(2 * Math.PI * 7 * i / SampleRate));
                            ph += f / SampleRate;
                            float env = (float)Math.Sin(Math.PI * t);
                            Add(s, at + i, (float)Math.Sin(2 * Math.PI * ph) * env * env * 0.3f);
                        }
                    }
                    break;
                }
                case Recipe.Toll:
                {
                    int n = Math.Max(1, (int)Math.Round(l.A * l.Seconds));
                    for (int k = 0; k < n; k++)
                        Ping(s, (int)(rng.Unit() * len), l.B * (0.98f + 0.04f * rng.Unit()), l.C, 0.35f, 0.03f, 4);
                    break;
                }
                case Recipe.Hum:
                {
                    // Two sines B... no: A Hz, the second B Hz off it, beating; C adds a soft low wash under.
                    var lp = new OnePole();
                    double p1 = 0, p2 = 0;
                    for (int i = 0; i < len; i++)
                    {
                        p1 += l.A / SampleRate; p2 += (l.A + l.B) / SampleRate;
                        float v = 0.5f * (float)(Math.Sin(2 * Math.PI * p1) + Math.Sin(2 * Math.PI * p2)) + 0.15f * (float)Math.Sin(2 * Math.PI * p1 * 2);
                        if (l.C > 0f) v += lp.Low(rng.Next(), 200f) * 0.3f * l.C;
                        s[i] = v;
                    }
                    break;
                }
                case Recipe.Rumble:
                {
                    // A rumbles a second on average: a long swell of very low noise (B Hz) over C seconds.
                    int n = Math.Max(1, (int)Math.Round(l.A * l.Seconds));
                    for (int k = 0; k < n; k++)
                    {
                        int at = (int)(rng.Unit() * len), dur = (int)(l.C * SampleRate);
                        var lp = new OnePole(); var lp2 = new OnePole();
                        for (int i = 0; i < dur; i++)
                        {
                            float t = i / (float)dur, env = (float)Math.Pow(Math.Sin(Math.PI * t), 1.5);
                            Add(s, at + i, lp2.Low(lp.Low(rng.Next(), l.B), l.B) * env);
                        }
                    }
                    break;
                }
                case Recipe.Crackle:
                {
                    // A tiny bursts a second over a soft hot wash.
                    var lp = new OnePole();
                    for (int i = 0; i < len; i++) s[i] = lp.Low(rng.Next(), 300f) * 0.15f;
                    int n = (int)(l.A * l.Seconds);
                    for (int k = 0; k < n; k++) Burst(s, (int)(rng.Unit() * len), l.B * (0.5f + rng.Unit()), 0.004f + 0.01f * rng.Unit(), 0.6f + 0.6f * rng.Unit(), rng);
                    break;
                }
                case Recipe.Steps:
                {
                    // A pairs a second on average: two soft thumps (B Hz) half a second apart, each pair a different weight.
                    int n = Math.Max(1, (int)Math.Round(l.A * l.Seconds));
                    for (int k = 0; k < n; k++)
                    {
                        int at = (int)(rng.Unit() * len);
                        float w = 0.5f + 0.5f * rng.Unit();
                        Burst(s, at, l.B, 0.05f, w, rng);
                        Burst(s, at + (int)(0.5f * SampleRate * (0.9f + 0.2f * rng.Unit())), l.B * 0.9f, 0.05f, w * 0.8f, rng);
                    }
                    break;
                }
                case Recipe.Clockwork:
                {
                    // A tick every A seconds exactly (the region's beat), a tock between: the clock never slips.
                    int period = (int)(l.A * SampleRate);
                    for (int at = 0; at + period <= len; at += period)
                    {
                        Burst(s, at, l.B, 0.008f, 1f, rng);
                        Burst(s, at + period / 2, l.B * 0.7f, 0.008f, 0.6f, rng);
                    }
                    break;
                }
            }
            // The bed's level: RMS to the target, then never over the peak.
            double e = 0; foreach (var v in s) e += v * v;
            float rms = (float)Math.Sqrt(e / len);
            if (rms > 0f) { float g = (float)Math.Pow(10.0, TargetRmsDb / 20.0) / rms; for (int i = 0; i < len; i++) s[i] *= g; }
            float peak = s.Max(v => Math.Abs(v)), ceiling = (float)Math.Pow(10.0, AudioDirection.SfxPeakDbtp / 20.0);
            if (peak > ceiling) { float g = ceiling / peak; for (int i = 0; i < len; i++) s[i] *= g; }
            return s;
        }

        public static float RmsDb(float[] s)
        {
            double e = 0; foreach (var v in s) e += v * v;
            return 20f * (float)Math.Log10(Math.Sqrt(e / Math.Max(1, s.Length)) + 1e-9);
        }
    }
}
