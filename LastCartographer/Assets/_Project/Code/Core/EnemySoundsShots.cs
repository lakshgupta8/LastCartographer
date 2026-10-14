using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>How a thing thrown at Wren ended: on her, on the ground, its time up in the air, or unwritten by a strike.</summary>
    public enum ShotEnd { Her, Ground, Time, Unwritten }

    public static partial class EnemySounds
    {
        /// <summary>
        /// A thing thrown at Wren (AUD-19, docs/design/enemy-sounds.md §2c): the wasp's pellet of pulp, Hale's flicked
        /// ink. It is heard as it leaves (unless its thrower's own move is that sound, as the wasp's spit is), while it
        /// flies (a loop that goes where it goes), and as it ends, by how it ended: on her, on the ground, its time up in
        /// the air, or unwritten by the Unwriter's strike (the same rub for anything thrown). A shot is known by the name
        /// it is thrown under, or the name's stem; a name no shot knows is ink, as Hale's flick is.
        /// </summary>
        public sealed class Shot
        {
            public string Name;
            public string What;
            public string Launch, Fly, Her, Ground, Time;
            public string Unwritten => "shot_unwritten";
            public IEnumerable<string> Cues => new[] { Launch, Fly, Her, Ground, Time, Unwritten }.Where(c => c != null);
        }

        static readonly List<Shot> _shots = new List<Shot>();
        public static IReadOnlyList<Shot> Shots => _shots;

        /// <summary>The shot a name is thrown as: its own, its stem's, or the flicked ink's when no shot knows it.</summary>
        public static Shot ShotOf(string name)
        {
            Shot best = null;
            if (!string.IsNullOrEmpty(name))
                foreach (var s in _shots)
                    if ((name == s.Name || (s.Name.EndsWith("_") && name.StartsWith(s.Name, StringComparison.Ordinal))) && (best == null || s.Name.Length > best.Name.Length)) best = s;
            return best ?? _shots.First(s => s.Name == DefaultShot);
        }

        /// <summary>What a thing thrown under this name would be heard as without one of its own.</summary>
        public const string DefaultShot = "Flick";

        /// <summary>The cue a shot ends with, by how it ended.</summary>
        public static string EndCue(Shot s, ShotEnd how) => s == null ? null : how switch
        {
            ShotEnd.Her => s.Her,
            ShotEnd.Ground => s.Ground,
            ShotEnd.Time => s.Time,
            _ => s.Unwritten,
        };

        static void Thrown(string name, string what, string launch, string fly, string her, string ground, string time) =>
            _shots.Add(new Shot { Name = name, What = what, Launch = launch, Fly = fly, Her = her, Ground = ground, Time = time });

        static void ShotsThrown()
        {
            Thrown("Pulp", "the Pulpwasp's pellet of chewed paper: spat (the wasp's own spit is its leaving), a small whizz tumbling, a wet slap on her, a wet pat on the ground, crumbling dry in the air",
                launch: null, fly: "pellet_fly", her: "pellet_splat", ground: "pellet_land", time: "pellet_crumble");
            Thrown("Flick", "ink flicked off Hale's quill: thrown off the nib, a thin hiss through the air, splashing on her, spattering on the ground, drying to nothing in the air",
                launch: "flick_throw", fly: "flick_fly", her: "flick_splash", ground: "flick_spatter", time: "flick_dry");
        }

        static void RegisterShots()
        {
            void One(string id, string what, Func<float[]> render, float gain = 1f, bool loop = false) =>
                InkSounds.Add(id, InkSounds.Kind.Enemy, what, render, gain, loop, null, AudioDirection.Voice.World);
            // pellet_land is the world's (AUD-11, WorldSounds): the pellet's landing was heard before its flight was
            One("pellet_fly", "a pellet of pulp in flight: a small whizz, tumbling", PelletFly, 0.25f, true);
            One("pellet_splat", "a pellet of pulp on her: a wet slap", PelletSplat, 0.6f);
            One("pellet_crumble", "a pellet dried in the air: crumbling", PelletCrumble, 0.35f);
            One("flick_throw", "ink thrown off the nib: a flick and its air", FlickThrow, 0.7f);
            One("flick_fly", "flicked ink in flight: a thin hiss", FlickFly, 0.22f, true);
            One("flick_splash", "flicked ink on her: a splash", FlickSplash, 0.65f);
            One("flick_spatter", "flicked ink on the ground: a spatter of drops", FlickSpatter, 0.55f);
            One("flick_dry", "flicked ink drying in the air: a hiss to nothing", FlickDry, 0.3f);
            One("shot_unwritten", "a thing thrown, unwritten by her strike: rubbed out, quick", ShotUnwritten, 0.7f);
            foreach (var s in _shots)
                foreach (var c in s.Cues)
                    if (c != "pellet_land" && !InkSounds.Has(c)) throw new InvalidOperationException(s.Name + " names a cue that does not exist: " + c);
        }

        /// <summary>Band noise at <paramref name="hz"/>, its level turning <paramref name="turnHz"/> times a second: a thing tumbling through the air.</summary>
        static void Tumble(float[] s, float at, float len, float hz, float q, float turnHz, float depth, float gain, int seed)
        {
            var rng = new InkSounds.Rng(seed); var band = new InkSounds.Band();
            int start = N(at), end = Math.Min(s.Length, N(at + len));
            for (int i = start; i < end; i++)
            {
                float t = (i - start) / (float)SampleRate;
                float turn = 1f - depth * (0.5f + 0.5f * (float)Math.Sin(2 * Math.PI * turnHz * t));
                s[i] += band.Tick(rng.Next(), hz, q) * turn * gain;
            }
        }

        static float[] PelletFly()
        {
            var s = Buf(0.5f + Ov);
            Tumble(s, 0f, 0.5f + Ov, 2400f, 3f, 12f, 0.6f, 1f, 880);   // six turns in the loop: it meets itself
            Tumble(s, 0f, 0.5f + Ov, 900f, 1.5f, 12f, 0.3f, 0.3f, 881);
            return Seamless(s, Ov);
        }

        static float[] PelletSplat()
        {
            var s = Buf(0.22f);
            Drop(s, 0f, 0.08f, 360f, 110f, 1f, 0.001f, 0.035f);   // a lump: the weight is in the slap
            Smear(s, 0f, 0.14f, 900f, 280f, 0.6f, 882, 0.001f, 0.05f);
            Grain(s, 0.02f, 0.12f, 90f, 0.006f, 1500f, 3200f, 2f, 0.25f, 883, 20f, soft: true);   // the pulp's wet bits
            return s;
        }

        static float[] PelletCrumble()
        {
            var s = Buf(0.3f);
            Grain(s, 0f, 0.26f, 70f, 0.008f, 1400f, 3200f, 2f, 1f, 884, 10f);
            return s;
        }

        static float[] FlickThrow()
        {
            var s = Buf(0.2f);
            Scratch(s, 0f, 0.05f, 2600f, 3800f, 2f, 1f, 885, 0.001f, 0.02f);
            Whoosh(s, 0.02f, 0.15f, 2400f, 4600f, 2f, 0.6f, 886, 0.3f);
            return s;
        }

        static float[] FlickFly()
        {
            var s = Buf(0.5f + Ov);
            Tumble(s, 0f, 0.5f + Ov, 4200f, 5f, 6f, 0.25f, 1f, 887);   // ink is a thread, not a lump: high and barely turning
            return Seamless(s, Ov);
        }

        static float[] FlickSplash()
        {
            var s = Buf(0.3f);
            Smear(s, 0f, 0.2f, 2600f, 900f, 1f, 888, 0.001f, 0.07f);   // ink is thin: the splash is all its drops
            Drop(s, 0f, 0.03f, 500f, 260f, 0.2f, 0.001f, 0.012f);
            Grain(s, 0.03f, 0.22f, 110f, 0.005f, 2800f, 6000f, 3f, 0.45f, 889, 12f, soft: true);
            return s;
        }

        static float[] FlickSpatter()
        {
            var s = Buf(0.3f);
            Grain(s, 0f, 0.24f, 140f, 0.005f, 2500f, 5500f, 3f, 1f, 890, 15f, soft: true);
            Drop(s, 0f, 0.04f, 280f, 140f, 0.4f, 0.001f, 0.015f);
            return s;
        }

        static float[] FlickDry() { var s = Buf(0.3f); Smear(s, 0f, 0.28f, 3200f, 1100f, 1f, 891, 0.01f, 0.18f); return s; }

        static float[] ShotUnwritten()
        {
            var s = Buf(0.28f);
            Scratch(s, 0f, 0.11f, 1800f, 1200f, 2f, 1f, 892, 0.005f, 0.04f);   // two quick rubs, all paper: nothing low
            Scratch(s, 0.12f, 0.12f, 1700f, 1100f, 2f, 0.8f, 893, 0.005f, 0.05f);
            return s;
        }
    }
}
