using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>
    /// Wren's sounds and the telegraph tells (AUD-03, docs/design/wren-sounds.md), made from ink and paper: every cue
    /// is a few lines of noise, filters and envelopes rendered on demand, never a recording. Her quill is never
    /// metal (audio-direction 4): a strike is a pen stroke, a pogo a nib's tap, a Bind a word being written, a dash
    /// a page turned fast; hurt is a smudge, not a cry; her voice is never heard. The four tells are the direction's:
    /// a short high scratch, the only low breath, a chime, a swell, each over before the fastest read.
    /// Pure C#: the tests render and measure, the bank in the game turns them into clips, the exporter writes them out.
    /// </summary>
    public static class InkSounds
    {
        public const int SampleRate = RollCallSong.SampleRate;

        public enum Kind { Wren, Layer, Flourish, Tell }

        public sealed class Cue
        {
            public string Id;
            public Kind Kind;
            /// <summary>What it is, for the doc and the sound designer.</summary>
            public string What;
            /// <summary>Played at this against the rendered peak (a jump is quieter than a strike).</summary>
            public float Gain = 1f;
            /// <summary>Loops seamlessly while held (the survey's hatching).</summary>
            public bool Loop;
            public Func<float[]> Render;
            /// <summary>A tell's kind, for the frames it may last; null for anything else.</summary>
            public AudioDirection.Tell? Tell;
        }

        static readonly List<Cue> _cues = new List<Cue>();
        public static IReadOnlyList<Cue> Cues => _cues;
        public static Cue Of(string id) => _cues.FirstOrDefault(c => c.Id == id);
        public static bool Has(string id) => Of(id) != null;
        public static string TellCue(AudioDirection.Tell t) => "tell_" + t.ToString().ToLowerInvariant();

        static void Add(string id, Kind kind, string what, Func<float[]> render, float gain = 1f, bool loop = false, AudioDirection.Tell? tell = null) =>
            _cues.Add(new Cue { Id = id, Kind = kind, What = what, Render = render, Gain = gain, Loop = loop, Tell = tell });

        static InkSounds()
        {
            // ---- Wren ----
            Add("stroke", Kind.Wren, "the swing: a pen stroke, a short scratch sweeping down as the nib runs", Stroke);
            Add("hit", Kind.Layer, "a strike landing: a denser scratch with the paper's thump under it", Hit);
            Add("kill", Kind.Layer, "a kill: a long scrape and an ink drop falling and soaking", Kill, 0.9f);
            Add("pogo", Kind.Wren, "a down-strike bouncing: the nib's tap", Pogo, 0.8f);
            Add("dash", Kind.Wren, "the Wingbeat: a page turned fast", Dash, 0.8f);
            Add("thread", Kind.Wren, "the Inkthread: a thin line drawn out fast and taking at its end", Thread, 0.8f);
            Add("refused", Kind.Wren, "no ink for it: a dry dot", Refused, 0.6f);
            Add("bind", Kind.Wren, "a Bind: a word being written, six scratches and a full stop", Bind);
            Add("survey", Kind.Wren, "surveying: steady hatching while the button is held", Survey, 0.6f, loop: true);
            Add("drawn", Kind.Wren, "a vantage drawn: one long stroke and a tap", Drawn, 0.9f);
            Add("hurt", Kind.Wren, "a mask taken: a smudge, low and dull, not a cry", Hurt);
            Add("died", Kind.Wren, "the last mask: the smudge drawn out until there is only paper", Died);
            Add("jump", Kind.Wren, "a jump: the pen lifting", Jump, 0.35f);
            Add("land", Kind.Wren, "a landing: the pen set down", Land, 0.45f);
            // ---- the Flourishes ----
            Add("crosshatch", Kind.Flourish, "the Crosshatch: six quick ticks", Crosshatch);
            Add("longstroke", Kind.Flourish, "the Longstroke: one long stroke, dry-brushed at its end", Longstroke);
            Add("blot", Kind.Flourish, "the Blot: a wet drop spreading", Blot);
            // ---- the tells (audio-direction 4) ----
            Add(TellCue(AudioDirection.Tell.Strike), Kind.Tell, "a short, high scratch of the pen", TellStrike, 1f, false, AudioDirection.Tell.Strike);
            Add(TellCue(AudioDirection.Tell.Slam), Kind.Tell, "a low drawn breath: the only low tell", TellSlam, 1f, false, AudioDirection.Tell.Slam);
            Add(TellCue(AudioDirection.Tell.Window), Kind.Tell, "a chime as the opening starts", TellWindow, 0.8f, false, AudioDirection.Tell.Window);
            Add(TellCue(AudioDirection.Tell.Shape), Kind.Tell, "a swell that follows the shape across the floor", TellShape, 0.9f, false, AudioDirection.Tell.Shape);
        }

        /// <summary>Render a cue's samples: mono at <see cref="SampleRate"/>, peaks at −1 dBTP (audio-direction 6).</summary>
        public static float[] Render(string id)
        {
            var c = Of(id);
            if (c == null) return null;
            var s = c.Render();
            RollCallSong.Normalize(s, AudioDirection.SfxPeakDbtp);
            return s;
        }

        public static float Seconds(float[] s) => s.Length / (float)SampleRate;

        // ---- the tools: a few lines each ----

        static int N(float seconds) => (int)(seconds * SampleRate);
        static float[] Buf(float seconds) => new float[N(seconds)];

        /// <summary>White noise, seeded so a cue renders the same every time.</summary>
        sealed class Rng
        {
            uint _s;
            public Rng(int seed) { _s = (uint)(seed * 2654435761u + 12345u); }
            public float Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return (_s / 4294967295f) * 2f - 1f; }
        }

        /// <summary>A resonant band-pass (biquad), re-tuned per sample so a scratch can sweep.</summary>
        sealed class Band
        {
            float _x1, _x2, _y1, _y2;
            public float Tick(float x, float hz, float q)
            {
                float w = 2f * (float)Math.PI * Math.Min(hz, SampleRate * 0.45f) / SampleRate;
                float alpha = (float)Math.Sin(w) / (2f * q), cosw = (float)Math.Cos(w);
                float b0 = alpha, b2 = -alpha, a0 = 1f + alpha, a1 = -2f * cosw, a2 = 1f - alpha;
                float y = (b0 * x + b2 * _x2 - a1 * _y1 - a2 * _y2) / a0;   // direct form I
                _x2 = _x1; _x1 = x; _y2 = _y1; _y1 = y;
                return y;
            }
        }

        sealed class OnePole
        {
            float _y;
            public float Low(float x, float hz) { float a = 1f - (float)Math.Exp(-2 * Math.PI * hz / SampleRate); _y += (x - _y) * a; return _y; }
        }

        static float Env(float t, float attack, float length, float decay)
        {
            if (t < 0f || t >= length) return 0f;
            float a = attack <= 0f ? 1f : Math.Min(1f, t / attack);
            float d = decay <= 0f ? 1f : (float)Math.Exp(-(t / decay));
            float tail = length - t < 0.004f ? (length - t) / 0.004f : 1f;   // never a click at the end
            return a * d * tail;
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * Math.Max(0f, Math.Min(1f, t));

        /// <summary>A pen scratch: band-passed noise with its centre sweeping, from <paramref name="at"/> for <paramref name="len"/> seconds.</summary>
        static void Scratch(float[] s, float at, float len, float fromHz, float toHz, float q, float gain, int seed, float attack = 0.003f, float decay = 0.05f)
        {
            var rng = new Rng(seed); var band = new Band();
            int start = N(at), end = Math.Min(s.Length, N(at + len));
            for (int i = start; i < end; i++)
            {
                float t = (i - start) / (float)SampleRate;
                float hz = Lerp(fromHz, toHz, t / len);
                s[i] += band.Tick(rng.Next(), hz, q) * Env(t, attack, len, decay) * gain;
            }
        }

        /// <summary>A soft low thump or a drop: a sine sliding down.</summary>
        static void Drop(float[] s, float at, float len, float fromHz, float toHz, float gain, float attack = 0.002f, float decay = 0.06f)
        {
            int start = N(at), end = Math.Min(s.Length, N(at + len));
            double ph = 0;
            for (int i = start; i < end; i++)
            {
                float t = (i - start) / (float)SampleRate;
                float hz = Lerp(fromHz, toHz, t / len);
                ph += hz / SampleRate;
                s[i] += (float)Math.Sin(2 * Math.PI * ph) * Env(t, attack, len, decay) * gain;
            }
        }

        /// <summary>Low, dull noise: a smudge, a breath, the spread of a blot.</summary>
        static void Smear(float[] s, float at, float len, float fromHz, float toHz, float gain, int seed, float attack = 0.01f, float decay = 0.08f)
        {
            var rng = new Rng(seed); var lp = new OnePole(); var lp2 = new OnePole();
            int start = N(at), end = Math.Min(s.Length, N(at + len));
            for (int i = start; i < end; i++)
            {
                float t = (i - start) / (float)SampleRate;
                float hz = Lerp(fromHz, toHz, t / len);
                s[i] += lp2.Low(lp.Low(rng.Next(), hz), hz) * Env(t, attack, len, decay) * gain;   // two poles: dull means dull
            }
        }

        static float[] Stroke()
        {
            var s = Buf(0.1f);
            Scratch(s, 0f, 0.09f, 3600f, 1100f, 3f, 1f, 1, 0.002f, 0.035f);
            return s;
        }

        static float[] Hit()
        {
            var s = Buf(0.09f);
            Scratch(s, 0f, 0.07f, 1400f, 700f, 6f, 4f, 2, 0.001f, 0.025f);
            Drop(s, 0f, 0.05f, 110f, 70f, 0.35f, 0.001f, 0.02f);
            return s;
        }

        static float[] Kill()
        {
            var s = Buf(0.42f);
            Scratch(s, 0f, 0.2f, 900f, 250f, 4f, 3.5f, 3, 0.002f, 0.09f);
            Drop(s, 0.1f, 0.14f, 340f, 105f, 0.45f, 0.004f, 0.06f);
            Smear(s, 0.18f, 0.24f, 500f, 150f, 0.6f, 4, 0.02f, 0.1f);
            return s;
        }

        static float[] Pogo()
        {
            var s = Buf(0.05f);
            Scratch(s, 0f, 0.006f, 5000f, 5000f, 1f, 0.7f, 5, 0f, 0.003f);
            Drop(s, 0f, 0.04f, 2600f, 2400f, 0.8f, 0.0005f, 0.012f);
            Drop(s, 0f, 0.03f, 5200f, 5000f, 0.25f, 0.0005f, 0.006f);
            return s;
        }

        static float[] Dash()
        {
            var s = Buf(0.15f);
            var rng = new Rng(6); var lp = new OnePole(); var band = new Band();
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float flap = 0.55f + 0.45f * (float)Math.Cos(2 * Math.PI * t / 0.07f);   // two humps: the page lifts and falls
                float x = band.Tick(lp.Low(rng.Next(), 4000f), 900f, 0.7f);
                s[i] = x * flap * Env(t, 0.008f, 0.15f, 0.07f);
            }
            return s;
        }

        static float[] Thread()
        {
            var s = Buf(0.18f);
            Drop(s, 0f, 0.14f, 480f, 2300f, 0.7f, 0.004f, 0.12f);          // drawn out: the line rising as it runs
            Scratch(s, 0f, 0.14f, 1500f, 4000f, 2f, 0.35f, 7, 0.004f, 0.08f);
            Scratch(s, 0.145f, 0.02f, 3000f, 3000f, 4f, 0.8f, 8, 0f, 0.006f);   // and takes
            return s;
        }

        static float[] Refused()
        {
            var s = Buf(0.035f);
            Smear(s, 0f, 0.03f, 1200f, 600f, 1f, 9, 0.0005f, 0.006f);
            return s;
        }

        static float[] Bind()
        {
            var s = Buf(0.6f);
            float[] centres = { 2200f, 2600f, 1900f, 2400f, 2100f, 2500f };
            for (int k = 0; k < 6; k++)
                Scratch(s, 0.02f + k * 0.085f, 0.045f, centres[k], centres[k] * 0.8f, 5f, 0.8f, 10 + k, 0.002f, 0.02f);
            Scratch(s, 0.54f, 0.012f, 3200f, 3200f, 6f, 1f, 17, 0f, 0.004f);   // the full stop
            return s;
        }

        static float[] Survey()
        {
            var s = Buf(0.5f);
            for (int k = 0; k < 8; k++)
                Scratch(s, 0.006f + k * 0.0625f, 0.05f, k % 2 == 0 ? 2400f : 2000f, k % 2 == 0 ? 1800f : 2400f, 4f, 0.8f, 20 + k, 0.003f, 0.025f);
            return s;   // every tick is over before the loop point: seamless
        }

        static float[] Drawn()
        {
            var s = Buf(0.27f);
            Scratch(s, 0f, 0.16f, 1500f, 3200f, 3f, 3f, 30, 0.004f, 0.09f);
            Drop(s, 0.19f, 0.04f, 2600f, 2400f, 0.4f, 0.0005f, 0.012f);
            Scratch(s, 0.19f, 0.006f, 5000f, 5000f, 1f, 0.5f, 31, 0f, 0.003f);
            return s;
        }

        static float[] Hurt()
        {
            var s = Buf(0.24f);
            Smear(s, 0f, 0.22f, 420f, 120f, 1f, 40, 0.012f, 0.07f);
            return s;
        }

        static float[] Died()
        {
            var s = Buf(0.7f);
            Smear(s, 0f, 0.6f, 500f, 100f, 1f, 41, 0.02f, 0.18f);
            Smear(s, 0.62f, 0.05f, 900f, 500f, 0.3f, 42, 0.001f, 0.01f);   // the pen set down, softly
            return s;
        }

        static float[] Jump()
        {
            var s = Buf(0.045f);
            Scratch(s, 0f, 0.04f, 1200f, 2400f, 1.2f, 1f, 50, 0.004f, 0.015f);
            return s;
        }

        static float[] Land()
        {
            var s = Buf(0.06f);
            Smear(s, 0f, 0.03f, 900f, 400f, 1f, 51, 0.001f, 0.01f);
            Drop(s, 0f, 0.04f, 120f, 80f, 0.5f, 0.001f, 0.015f);
            return s;
        }

        static float[] Crosshatch()
        {
            var s = Buf(0.2f);
            for (int k = 0; k < 6; k++)
                Scratch(s, k * 0.03f, 0.028f, k % 2 == 0 ? 3000f : 2200f, k % 2 == 0 ? 2000f : 3200f, 4f, 0.9f, 60 + k, 0.001f, 0.012f);
            return s;
        }

        static float[] Longstroke()
        {
            var s = Buf(0.32f);
            var rng = new Rng(70); var band = new Band();
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float hz = Lerp(2600f, 900f, t / 0.28f);
                float dry = t > 0.2f ? 0.5f + 0.5f * (float)Math.Cos(2 * Math.PI * t * 38f) : 1f;   // the brush runs dry: it breaks up
                s[i] = band.Tick(rng.Next(), hz, 2.5f) * Env(t, 0.003f, 0.3f, 0.16f) * dry;
            }
            return s;
        }

        static float[] Blot()
        {
            var s = Buf(0.32f);
            Drop(s, 0f, 0.09f, 430f, 150f, 0.8f, 0.002f, 0.05f);
            Smear(s, 0.03f, 0.28f, 700f, 200f, 0.9f, 80, 0.03f, 0.1f);
            return s;
        }

        static float FramesSeconds(AudioDirection.Tell t) => AudioDirection.TellFrames(t) / 60f;

        static float[] TellStrike()
        {
            float len = FramesSeconds(AudioDirection.Tell.Strike);
            var s = Buf(len);
            Scratch(s, 0f, len - 0.01f, 4200f, 5200f, 4f, 1f, 90, 0.002f, 0.04f);
            return s;
        }

        static float[] TellSlam()
        {
            float len = FramesSeconds(AudioDirection.Tell.Slam);
            var s = Buf(len);
            Smear(s, 0f, len - 0.005f, 180f, 260f, 1f, 91, 0.07f, 10f);   // a breath drawn in: it swells and stops
            return s;
        }

        static float[] TellWindow()
        {
            float len = FramesSeconds(AudioDirection.Tell.Window);
            var s = Buf(len);
            Drop(s, 0f, len - 0.005f, 2093f, 2093f, 0.8f, 0.0005f, 0.03f);   // C7
            Drop(s, 0f, len - 0.005f, 5274f, 5274f, 0.3f, 0.0005f, 0.012f);
            return s;
        }

        static float[] TellShape()
        {
            float len = FramesSeconds(AudioDirection.Tell.Shape);
            var s = Buf(len);
            var rng = new Rng(92); var band = new Band();
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float hz = Lerp(900f, 2400f, t / len);
                s[i] = band.Tick(rng.Next(), hz, 2f) * Env(t, len * 0.85f, len, 0f);   // rising to its end and cut
            }
            return s;
        }
    }
}
