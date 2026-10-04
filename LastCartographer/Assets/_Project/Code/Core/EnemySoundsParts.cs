using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    public static partial class EnemySounds
    {
        /// <summary>
        /// A piece of a boss fight that is not the boss's body (AUD-16, docs/design/enemy-sounds.md §2b): a stone
        /// feather, a block of rubble, the ink surge, the Star's fist. It is heard at the four moments a part has: as it
        /// appears (the rubble crashing down where it lands), as a strike lands on it (a feather cracked by the pogo),
        /// as it goes without one (a feather shattering on the floor), and while it is there (the surge flowing). A part
        /// is known by the name its boss makes it under, or the name's stem (<c>Strike_3</c> is a <c>Strike_</c>).
        /// </summary>
        public sealed class Part
        {
            public string Name;
            public string What;
            public string Appear, Struck, Gone, Loop;
            public IEnumerable<string> Cues => new[] { Appear, Struck, Gone, Loop }.Where(c => c != null);
        }

        static readonly List<Part> _parts = new List<Part>();
        public static IReadOnlyList<Part> Parts => _parts;

        /// <summary>The parts whose owner speaks for them: a dove's hit is the Choir's, a rope's cut the Bells' event.</summary>
        public static readonly string[] SpokenFor = { "Dove_", "Rope_", "GreatRope", "SmallVoss", "QuillHand", "WrenDrawing", "CordLance" };

        /// <summary>A part's voice by the name it was made under, or its stem's; null for a part with none of its own.</summary>
        public static Part PartOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Part best = null;
            foreach (var p in _parts)
                if ((name == p.Name || (p.Name.EndsWith("_") && name.StartsWith(p.Name, StringComparison.Ordinal))) && (best == null || p.Name.Length > best.Name.Length)) best = p;
            return best;
        }

        static void Piece(string name, string what, string appear = null, string struck = null, string gone = null, string loop = null) =>
            _parts.Add(new Part { Name = name, What = what, Appear = appear, Struck = struck, Gone = gone, Loop = loop });

        static void BossParts()
        {
            Piece("Feather", "the Gatekeeper's stone feather: cracked by a pogo, shattering where it meets the floor", struck: "feather_crack", gone: "feather_shatter");
            Piece("Rubble", "a block of the Collapse's roof: a crash as it lands, broken by a pogo", appear: "rubble_crash", struck: "rubble_break");
            Piece("Surge", "the Collapse's ink surge: wet ink rushing along the floor", loop: "surge_flow");
            Piece("Fist", "the Fallen Star's fist, left down after the slam: iron rung by the pogo", struck: "fist_ring");
            Piece("Seal", "Voss's seal on a section: pressed down, and broken from its edge", appear: "voss_seal_set", struck: "voss_seal_break");
            Piece("Fire", "the Guild's fire in the Brood's beds: burning while it is there, a stamp hissing", struck: "fire_stamp", loop: "fire_burn");
            Piece("InkPool", "the Complete Survey's ink pooling where the named ground was", appear: "pool_spread");
            Piece("Strike_", "Hale's stone strike erupting from a sighted stone", appear: "stone_erupt");
            Piece("Frame_", "the Archivist's frame drawn closed at a side of the arena", appear: "frame_drawn");
        }

        static void RegisterParts()
        {
            void One(string id, string what, Func<float[]> render, float gain = 1f, bool loop = false) =>
                InkSounds.Add(id, InkSounds.Kind.Enemy, what, render, gain, loop, null, AudioDirection.Voice.World);
            One("feather_crack", "a stone feather cracked: a tick and its short ring", FeatherCrack, 0.8f);
            One("feather_shatter", "a stone feather shattering on the floor", FeatherShatter, 0.8f);
            One("rubble_crash", "a block of the roof crashing down", RubbleCrash, 1f);
            One("rubble_break", "the block broken: a knock and grit", RubbleBreak, 0.8f);
            One("surge_flow", "wet ink rushing along the floor", SurgeFlow, 0.45f, true);
            One("fist_ring", "the iron fist rung by the pogo", FistRing, 0.8f);
            One("star_walls", "iron walls grinding up out of the ground", StarWalls, 0.9f);
            One("voss_seal_set", "a seal pressed down: a thump, wax and a little brass", SealSet, 0.8f);
            One("voss_seal_break", "a seal broken: a crack and the brass let go", SealBreak, 0.8f);
            One("fire_burn", "the fire in the reeds, burning", FireBurn, 0.4f, true);
            One("fire_stamp", "a stamp on the fire: a thud and a hiss", FireStamp, 0.8f);
            One("pool_spread", "ink pooling, wet", PoolSpread, 0.7f);
            One("stone_erupt", "stone bursting up from the ground", StoneErupt, 0.9f);
            One("frame_drawn", "the frame drawn closed: two long strokes", FrameDrawn, 0.7f);
            foreach (var p in _parts)
                foreach (var c in p.Cues)
                    if (!InkSounds.Has(c)) throw new InvalidOperationException(p.Name + " names a cue that does not exist: " + c);
        }

        static float[] FeatherCrack()
        {
            var s = Buf(0.12f);
            Click(s, 0f, 3000f, 1f, 800);
            Ring(s, 0f, 0.1f, 1600f, 0.6f, 0.025f, Shell);
            return s;
        }

        static float[] FeatherShatter()
        {
            var s = Buf(0.42f);
            Click(s, 0f, 3600f, 1f, 801);
            Drop(s, 0f, 0.1f, 220f, 110f, 0.5f, 0.001f, 0.04f);
            Grain(s, 0.01f, 0.33f, 120f, 0.008f, 2500f, 6000f, 3f, 0.8f, 802, 15f);
            return s;
        }

        static float[] RubbleCrash()
        {
            var s = Buf(0.7f);
            Drop(s, 0f, 0.35f, 80f, 40f, 1f, 0.001f, 0.12f);
            Smear(s, 0f, 0.35f, 300f, 100f, 0.7f, 803, 0.002f, 0.1f);
            Grain(s, 0.03f, 0.55f, 50f, 0.012f, 900f, 2800f, 2f, 0.5f, 804, 6f);
            return s;
        }

        static float[] RubbleBreak()
        {
            var s = Buf(0.4f);
            Click(s, 0f, 1800f, 1f, 805);
            Drop(s, 0f, 0.1f, 150f, 70f, 0.8f, 0.001f, 0.04f);
            Grain(s, 0.02f, 0.3f, 70f, 0.01f, 1200f, 3200f, 2f, 0.6f, 806, 10f);
            return s;
        }

        static float[] SurgeFlow()
        {
            var s = Buf(0.9f + Ov);
            Smear(s, 0f, 0.9f + Ov, 500f, 500f, 1f, 807, 0.05f, 10f);
            Grain(s, 0f, 0.9f + Ov, 40f, 0.03f, 400f, 900f, 1f, 0.5f, 808, soft: true);   // the ink's lumps turning over
            return Seamless(s, Ov);
        }

        static float[] FistRing()
        {
            var s = Buf(0.55f);
            Click(s, 0f, 2200f, 0.6f, 809);
            Ring(s, 0f, 0.5f, 520f, 1f, 0.2f, Iron);
            return s;
        }

        static float[] StarWalls()
        {
            var s = Buf(0.8f);
            Scratch(s, 0f, 0.6f, 200f, 500f, 4f, 1f, 810, 0.05f, 0.3f);
            Smear(s, 0f, 0.6f, 150f, 250f, 0.6f, 811, 0.05f, 0.25f);
            Ring(s, 0.58f, 0.2f, 350f, 0.6f, 0.08f, Iron);
            return s;
        }

        static float[] SealSet()
        {
            var s = Buf(0.35f);
            Drop(s, 0f, 0.12f, 180f, 90f, 1f, 0.001f, 0.04f);
            Smear(s, 0f, 0.15f, 800f, 300f, 0.4f, 812, 0.004f, 0.05f);
            Ring(s, 0.01f, 0.25f, 1400f, 0.35f, 0.07f, Brass);
            return s;
        }

        static float[] SealBreak()
        {
            var s = Buf(0.3f);
            Click(s, 0f, 3200f, 1f, 813);
            Grain(s, 0f, 0.15f, 120f, 0.006f, 2500f, 5000f, 3f, 0.6f, 814, 20f);
            Ring(s, 0.02f, 0.2f, 1800f, 0.5f, 0.05f, Brass);
            return s;
        }

        static float[] FireBurn()
        {
            var s = Buf(1f + Ov);
            Smear(s, 0f, 1f + Ov, 900f, 900f, 1f, 815, 0.05f, 10f);
            Grain(s, 0f, 1f + Ov, 40f, 0.006f, 3000f, 6500f, 2f, 0.7f, 816);
            return Seamless(s, Ov);
        }

        static float[] FireStamp()
        {
            var s = Buf(0.4f);
            Drop(s, 0f, 0.1f, 140f, 70f, 1f, 0.001f, 0.04f);
            Smear(s, 0.02f, 0.3f, 3000f, 1200f, 0.7f, 817, 0.005f, 0.1f);
            return s;
        }

        static float[] PoolSpread()
        {
            var s = Buf(0.5f);
            Smear(s, 0f, 0.4f, 600f, 200f, 1f, 818, 0.02f, 0.12f);
            Drop(s, 0f, 0.08f, 260f, 120f, 0.5f, 0.002f, 0.03f);
            return s;
        }

        static float[] StoneErupt()
        {
            var s = Buf(0.5f);
            Drop(s, 0f, 0.2f, 100f, 50f, 1f, 0.001f, 0.07f);
            Grain(s, 0f, 0.35f, 90f, 0.01f, 1500f, 4000f, 2f, 0.7f, 819, 15f);
            Smear(s, 0f, 0.3f, 900f, 300f, 0.5f, 820, 0.002f, 0.08f);
            return s;
        }

        static float[] FrameDrawn()
        {
            var s = Buf(0.6f);
            Scratch(s, 0f, 0.25f, 1800f, 1200f, 2f, 1f, 821, 0.01f, 0.08f);
            Scratch(s, 0.27f, 0.25f, 1600f, 1100f, 2f, 0.9f, 822, 0.01f, 0.08f);
            return s;
        }
    }
}
