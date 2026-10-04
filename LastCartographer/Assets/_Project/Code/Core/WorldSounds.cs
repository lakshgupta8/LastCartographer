using System;

namespace OWSBG.Core
{
    /// <summary>
    /// The world's sounds and the pages' (AUD-11, docs/design/world-sounds.md). The world is a drawing on paper and so
    /// is every page of the atlas, so everything here is ink and paper too: a room changes as a page turns, a lamp is lit
    /// with a whump and a ring of glass, the desk is a pen set down and a long breath, a seed is a drop, a seal pressed
    /// is a stamp, an erased place is the eraser dragged, a recovered one is ink drawn back, an ability is a flourish; the
    /// pages open as paper lifted and close as paper set down, the cursor is the nib's tick, a line of dialogue is a quick
    /// scratch. Her Instruments are hers (a dart's air, the plumb's fall, the lens's glass, the lantern's wick, the hook's
    /// line, the tincture swallowed, the seal's wax, a parry's ring) and ride with her other sounds. The pages ride the Ui
    /// bus, so they are heard while the game is paused and the room is not. Registered into <see cref="InkSounds"/>' table.
    /// </summary>
    public static class WorldSounds
    {
        const int SampleRate = InkSounds.SampleRate;

        /// <summary>The cue an Instrument makes when it is used; null for none.</summary>
        public static string InstrumentCue(InstrumentKind k) => k switch
        {
            InstrumentKind.CompassDart => "dart_throw",
            InstrumentKind.PlumbWeight => "plumb_drop",
            InstrumentKind.SightingLens => "lens_raise",
            InstrumentKind.FieldLantern => "lantern_light",
            InstrumentKind.TetherHook => "hook_cast",
            InstrumentKind.IrisTincture => "tincture_drink",
            InstrumentKind.WaxSeal => "seal_set",
            _ => null,
        };

        /// <summary>What a place's decision sounds like at the desk; null for Unwritten.</summary>
        public static string FateCue(PlaceFate f) => f switch
        {
            PlaceFate.Anchored => "anchor",
            PlaceFate.Held => "held",
            PlaceFate.Released => "released",
            _ => null,
        };

        /// <summary>What a commission's step sounds like; null where the ledger says nothing (posted, unknown).</summary>
        public static string CommissionCue(CommissionState s) => s switch
        {
            CommissionState.Taken => "ledger_take",
            CommissionState.Fulfilled => "commission_done",
            CommissionState.Closed => "stamp",
            CommissionState.Failed => "commission_failed",
            _ => null,
        };

        /// <summary>The pitch a seed drops at: a little higher for every seed it is worth, up to a fifth.</summary>
        public static float SeedPitch(int count) => 1f + 0.06f * Math.Max(0, Math.Min(8, count - 1));

        /// <summary>The pitch of a setting's tick, by its value: an octave across the slider, so the ear reads the level.</summary>
        public static float TickPitch(float value01) => value01 < 0f ? 1f : 0.75f + 0.75f * Math.Max(0f, Math.Min(1f, value01));

        /// <summary>Put every cue into <see cref="InkSounds"/>' table. Called once, by its static constructor.</summary>
        internal static void Register()
        {
            void Ui(string id, string what, Func<float[]> render, float gain) => InkSounds.Add(id, InkSounds.Kind.Ui, what, render, gain);
            void World(string id, string what, Func<float[]> render, float gain) => InkSounds.Add(id, InkSounds.Kind.World, what, render, gain);
            void Hers(string id, string what, Func<float[]> render, float gain) => InkSounds.Add(id, InkSounds.Kind.Wren, what, render, gain);
            // ---- the pages ----
            Ui("ui_open", "a page opened: paper lifted", UiOpen, 0.5f);
            Ui("ui_close", "a page closed: paper set down", UiClose, 0.5f);
            Ui("ui_move", "the cursor moved: the nib's tick", UiMove, 0.35f);
            Ui("ui_tick", "a setting changed: a tick with a note in it, pitched by the value", UiTick, 0.45f);
            Ui("ui_select", "chosen: a short stroke", UiSelect, 0.5f);
            Ui("ui_denied", "not possible: a dry dot", UiDenied, 0.4f);
            Ui("ui_line", "a line of dialogue shown: a quick scratch", UiLine, 0.25f);
            Ui("ui_toast", "a note in the journal, a prompt: two small taps", UiToast, 0.35f);
            Ui("ui_back", "a page back: a soft flap", UiBack, 0.4f);
            // ---- the world ----
            World("page_turn", "a room changes: the page turned, slowly", PageTurn, 0.6f);
            World("lamp_lit", "a travel lamp lit: a whump and a ring of glass", LampLit, 0.7f);
            World("desk_rest", "the desk: the pen set down, the chair, a long breath", DeskRest, 0.6f);
            World("seed", "a seed taken: a drop, pitched by its worth", Seed, 0.55f);
            World("memories_back", "her memories back: the ink refilling", MemoriesBack, 0.7f);
            World("buy", "seeds counted out", Buy, 0.6f);
            World("ledger_take", "a commission taken: the pen's tick and a short stroke", LedgerTake, 0.5f);
            World("commission_done", "a commission fulfilled: a long stroke and a tap, low", CommissionDone, 0.6f);
            World("stamp", "a commission closed, a seal pressed: a stamp", Stamp, 0.8f);
            World("commission_failed", "a commission failed: the line struck through", CommissionFailed, 0.6f);
            World("ability", "an ability learned: a flourish of the pen, rising", Ability, 0.8f);
            World("fade_step", "a place fades a stage: a soft pass of the eraser", FadeStep, 0.35f);
            World("erased", "a place erased: the eraser dragged across the page, twice, and the paper left", Erased, 0.6f);
            World("recovered", "a place recovered: ink drawn back", Recovered, 0.6f);
            World("anchor", "a place anchored: a stake driven and the Guild's seal pressed", Anchor, 0.9f);
            World("held", "a place held: a hand laid on the page", Held, 0.5f);
            World("released", "a place released: the pen lifted, and a breath", Released, 0.5f);
            World("waypoint", "a waypoint found: a mark made on the map", Waypoint, 0.6f);
            World("travel", "fast travel: the map folding, three flaps of paper", Travel, 0.6f);
            World("redrawn", "respawn: the pen drawing her again, six strokes rising and a tap", Redrawn, 0.7f);
            World("seal_break", "the wax seal spent: cracked", SealBreak, 0.7f);
            World("fell", "fallen into the margin: a page's flutter and a smudge", Fell, 0.7f);
            World("barred", "a shut way: a dull knock on the page", Barred, 0.6f);
            World("offered", "a memory handed over: a wet stroke and the handbell, far", Offered, 0.6f);
            World("pellet_land", "a pellet of pulp landing: a wet pat", PelletLand, 0.5f);
            // ---- her Instruments ----
            Hers("dart_throw", "the Compass-dart thrown: its air", DartThrow, 0.7f);
            Hers("plumb_drop", "the Plumb-weight dropped: falling, and a thud", PlumbDrop, 0.7f);
            Hers("lens_raise", "the Sighting-lens raised: glass", LensRaise, 0.6f);
            Hers("lantern_light", "the Field-lantern lit: a wick catching", LanternLight, 0.7f);
            Hers("hook_cast", "the Tether-hook cast: a line running out and taking", HookCast, 0.7f);
            Hers("tincture_drink", "the Iris-tincture: a drop swallowed", TinctureDrink, 0.6f);
            Hers("seal_set", "the Wax-seal set: wax pressed", SealSet, 0.7f);
            Hers("parry", "a parry: the lens rings", Parry, 1f);
            Hers("charter", "a Charter put on: the cowl drawn", Charter, 0.6f);
            Hers("clarity_gone", "the white takes her: a hiss out to nothing", ClarityGone, 0.7f);
            Hers("clarity_grew", "Clarity grows: a bloom, and the bell", ClarityGrew, 0.6f);
            Hers("belt_turn", "the belt turned to the next Instrument: a tick", BeltTurn, 0.3f);
            Hers("prompt", "something to read here: the smallest tick", Prompt, 0.2f);
            foreach (InstrumentKind k in Enum.GetValues(typeof(InstrumentKind)))
                if (k != InstrumentKind.None && !InkSounds.Has(InstrumentCue(k))) throw new InvalidOperationException(k + " has no sound");
        }

        // ---- the tools are InkSounds' and EnemySounds' ----

        static float[] Buf(float seconds) => InkSounds.Buf(seconds);
        static void Scratch(float[] s, float at, float len, float fromHz, float toHz, float q, float gain, int seed, float attack = 0.003f, float decay = 0.05f) => InkSounds.Scratch(s, at, len, fromHz, toHz, q, gain, seed, attack, decay);
        static void Drop(float[] s, float at, float len, float fromHz, float toHz, float gain, float attack = 0.002f, float decay = 0.06f) => InkSounds.Drop(s, at, len, fromHz, toHz, gain, attack, decay);
        static void Smear(float[] s, float at, float len, float fromHz, float toHz, float gain, int seed, float attack = 0.01f, float decay = 0.08f) => InkSounds.Smear(s, at, len, fromHz, toHz, gain, seed, attack, decay);
        static void Click(float[] s, float at, float hz, float gain, int seed) => EnemySounds.Click(s, at, hz, gain, seed);
        static void Whoosh(float[] s, float at, float len, float fromHz, float toHz, float q, float gain, int seed, float humpAt = 0.4f) => EnemySounds.Whoosh(s, at, len, fromHz, toHz, q, gain, seed, humpAt);
        static void Ring(float[] s, float at, float len, float hz, float gain, float decay, (float, float)[] partials, float slideTo = 0f) => EnemySounds.Ring(s, at, len, hz, gain, decay, partials, slideTo);
        static void Grain(float[] s, float at, float len, float perSecond, float tickLen, float hzLo, float hzHi, float q, float gain, int seed, float perSecondEnd = -1f, bool soft = false) => EnemySounds.Grain(s, at, len, perSecond, tickLen, hzLo, hzHi, q, gain, seed, perSecondEnd, soft);
        /// <summary>Glass: a lamp's chimney, the lens.</summary>
        static readonly (float, float)[] Glass = { (1f, 1f), (2.32f, 0.5f), (4.25f, 0.2f) };
        /// <summary>A crumple of paper: many soft ticks.</summary>
        static void Crumple(float[] s, float at, float len, float gain, int seed) => Grain(s, at, len, 80f, 0.008f, 1800f, 3600f, 2f, gain, seed);

        // ---- the pages ----

        static float[] UiOpen() { var s = Buf(0.16f); Whoosh(s, 0f, 0.14f, 600f, 1400f, 1f, 1f, 600, 0.4f); Crumple(s, 0f, 0.1f, 0.3f, 601); return s; }
        static float[] UiClose() { var s = Buf(0.14f); Whoosh(s, 0f, 0.1f, 1400f, 500f, 1f, 1f, 602, 0.5f); Smear(s, 0.07f, 0.06f, 500f, 250f, 0.5f, 603, 0.002f, 0.02f); return s; }
        static float[] UiMove() { var s = Buf(0.04f); Click(s, 0f, 3800f, 0.6f, 604); Drop(s, 0f, 0.03f, 2400f, 2300f, 0.8f, 0.0005f, 0.01f); return s; }
        static float[] UiTick() { var s = Buf(0.07f); Click(s, 0f, 4200f, 0.5f, 605); Drop(s, 0f, 0.06f, 1800f, 1800f, 1f, 0.0005f, 0.02f); return s; }
        static float[] UiSelect() { var s = Buf(0.09f); Scratch(s, 0f, 0.08f, 2600f, 1400f, 3f, 1f, 606, 0.002f, 0.03f); return s; }
        static float[] UiDenied() { var s = Buf(0.05f); Smear(s, 0f, 0.035f, 1200f, 600f, 1f, 607, 0.0005f, 0.008f); return s; }
        static float[] UiLine() { var s = Buf(0.06f); Scratch(s, 0f, 0.05f, 3200f, 2200f, 4f, 1f, 608, 0.002f, 0.02f); return s; }
        static float[] UiToast() { var s = Buf(0.12f); for (int k = 0; k < 2; k++) { Click(s, k * 0.06f, 4000f, 0.6f, 609 + k); Drop(s, k * 0.06f, 0.03f, 2800f, 2700f, 0.7f, 0.0005f, 0.01f); } return s; }
        static float[] UiBack() { var s = Buf(0.1f); Whoosh(s, 0f, 0.09f, 1000f, 600f, 1f, 1f, 611, 0.4f); return s; }

        // ---- the world ----

        static float[] PageTurn()
        {
            var s = Buf(0.45f);
            Whoosh(s, 0f, 0.42f, 500f, 1600f, 0.8f, 1f, 620, 0.5f);
            Crumple(s, 0.05f, 0.35f, 0.35f, 621);
            return s;
        }

        static float[] LampLit()
        {
            var s = Buf(0.5f);
            Smear(s, 0f, 0.25f, 300f, 120f, 1f, 630, 0.02f, 0.08f);
            Drop(s, 0f, 0.12f, 90f, 60f, 0.5f, 0.004f, 0.05f);
            Ring(s, 0.06f, 0.42f, 2600f, 0.35f, 0.15f, Glass);
            return s;
        }

        static float[] DeskRest()
        {
            var s = Buf(0.6f);
            Smear(s, 0f, 0.04f, 900f, 400f, 1f, 640, 0.001f, 0.012f);   // the pen set down
            Drop(s, 0f, 0.05f, 120f, 80f, 0.5f, 0.001f, 0.015f);
            Click(s, 0.12f, 2600f, 0.3f, 641);                               // the chair
            Smear(s, 0.15f, 0.42f, 400f, 250f, 0.7f, 642, 0.12f, 0.25f);    // a long breath
            return s;
        }

        static float[] Seed()
        {
            var s = Buf(0.12f);
            Drop(s, 0f, 0.07f, 1400f, 900f, 1f, 0.001f, 0.025f);
            Click(s, 0f, 3000f, 0.4f, 650);
            return s;
        }

        static float[] MemoriesBack()
        {
            var s = Buf(0.5f);
            Smear(s, 0f, 0.45f, 300f, 700f, 1f, 660, 0.05f, 0.3f);
            Drop(s, 0.05f, 0.35f, 200f, 500f, 0.5f, 0.02f, 0.25f);
            return s;
        }

        static float[] Buy()
        {
            var s = Buf(0.3f);
            float[] hz = { 1500f, 1350f, 1600f, 1250f, 1450f };
            for (int k = 0; k < hz.Length; k++) { Drop(s, k * 0.05f, 0.05f, hz[k], hz[k] * 0.7f, 0.8f, 0.001f, 0.02f); Click(s, k * 0.05f, 3200f, 0.3f, 670 + k); }
            return s;
        }

        static float[] LedgerTake() { var s = Buf(0.12f); Click(s, 0f, 3600f, 0.7f, 680); Scratch(s, 0.02f, 0.09f, 2400f, 1500f, 3f, 1f, 681, 0.002f, 0.03f); return s; }

        static float[] CommissionDone()
        {
            var s = Buf(0.4f);
            Scratch(s, 0f, 0.25f, 1200f, 2400f, 3f, 1f, 690, 0.004f, 0.12f);
            Drop(s, 0.3f, 0.05f, 2000f, 1900f, 0.4f, 0.0005f, 0.015f);
            Click(s, 0.3f, 4200f, 0.5f, 691);
            return s;
        }

        static float[] Stamp()
        {
            var s = Buf(0.15f);
            Drop(s, 0f, 0.08f, 160f, 90f, 1f, 0.001f, 0.03f);
            Smear(s, 0f, 0.06f, 800f, 300f, 0.6f, 700, 0.001f, 0.02f);
            Click(s, 0f, 3000f, 0.4f, 701);
            return s;
        }

        static float[] CommissionFailed() { var s = Buf(0.3f); Scratch(s, 0f, 0.25f, 1800f, 700f, 2f, 1f, 710, 0.004f, 0.1f); return s; }

        static float[] Ability()
        {
            var s = Buf(0.5f);
            float[] hz = { 600f, 900f, 1350f };
            for (int k = 0; k < 3; k++) Drop(s, k * 0.09f, 0.12f, hz[k], hz[k] * 1.02f, 0.6f, 0.004f, 0.05f);
            Scratch(s, 0f, 0.3f, 1200f, 3200f, 2f, 0.7f, 720, 0.004f, 0.15f);
            Ring(s, 0.27f, 0.22f, 2093f, 0.4f, 0.08f, EnemySounds.Handbell);
            return s;
        }

        static float[] FadeStep() { var s = Buf(0.5f); Smear(s, 0f, 0.45f, 1100f, 600f, 1f, 730, 0.05f, 0.15f); return s; }

        static float[] Erased()
        {
            var s = Buf(0.9f);
            Smear(s, 0f, 0.35f, 1200f, 500f, 1f, 740, 0.03f, 0.12f);
            Smear(s, 0.4f, 0.35f, 1100f, 450f, 0.9f, 741, 0.03f, 0.12f);
            Smear(s, 0.7f, 0.19f, 300f, 150f, 0.4f, 742, 0.05f, 0.05f);   // the paper left
            return s;
        }

        static float[] Recovered()
        {
            var s = Buf(0.6f);
            Scratch(s, 0f, 0.4f, 900f, 2400f, 2f, 1f, 750, 0.01f, 0.18f);
            Drop(s, 0.1f, 0.4f, 300f, 600f, 0.5f, 0.02f, 0.2f);
            return s;
        }

        static float[] Anchor()
        {
            var s = Buf(0.5f);
            Drop(s, 0f, 0.12f, 140f, 60f, 1f, 0.001f, 0.04f);
            Click(s, 0f, 3200f, 0.5f, 760);
            Ring(s, 0f, 0.1f, 1800f, 0.4f, 0.03f, EnemySounds.Brass);
            Drop(s, 0.25f, 0.14f, 120f, 55f, 0.9f, 0.001f, 0.05f);          // and the seal pressed
            Smear(s, 0.25f, 0.1f, 700f, 300f, 0.5f, 761, 0.001f, 0.03f);
            return s;
        }

        static float[] Held() { var s = Buf(0.4f); Smear(s, 0f, 0.35f, 700f, 300f, 1f, 770, 0.08f, 0.1f); return s; }

        static float[] Released()
        {
            var s = Buf(0.5f);
            Scratch(s, 0f, 0.08f, 1200f, 2400f, 1.5f, 1f, 780, 0.004f, 0.03f);
            Smear(s, 0.08f, 0.4f, 500f, 250f, 0.6f, 781, 0.1f, 0.12f);
            return s;
        }

        static float[] Waypoint()
        {
            var s = Buf(0.2f);
            Click(s, 0f, 3600f, 0.7f, 790);
            Scratch(s, 0.02f, 0.1f, 2400f, 1600f, 3f, 1f, 791, 0.002f, 0.04f);
            Drop(s, 0.14f, 0.04f, 2600f, 2400f, 0.5f, 0.0005f, 0.012f);
            return s;
        }

        static float[] Travel()
        {
            var s = Buf(0.6f);
            for (int k = 0; k < 3; k++) Whoosh(s, k * 0.17f, 0.2f, 900f, 1500f, 1f, 1f - k * 0.2f, 800 + k, 0.4f);
            Crumple(s, 0f, 0.5f, 0.3f, 803);
            return s;
        }

        static float[] Redrawn()
        {
            var s = Buf(0.6f);
            for (int k = 0; k < 6; k++) Scratch(s, 0.02f + k * 0.08f, 0.05f, 1600f + k * 220f, 1300f + k * 220f, 4f, 0.8f, 810 + k, 0.002f, 0.02f);
            Click(s, 0.52f, 4200f, 0.6f, 816);
            Drop(s, 0.52f, 0.04f, 2600f, 2400f, 0.5f, 0.0005f, 0.012f);
            return s;
        }

        static float[] SealBreak()
        {
            var s = Buf(0.3f);
            Click(s, 0f, 4000f, 1f, 820);
            Click(s, 0.05f, 3200f, 0.8f, 821);
            Ring(s, 0.05f, 0.1f, 2200f, 0.5f, 0.03f, EnemySounds.Shell);
            Smear(s, 0.08f, 0.2f, 900f, 300f, 0.4f, 822, 0.005f, 0.06f);
            return s;
        }

        static float[] Fell()
        {
            var s = Buf(0.5f);
            Whoosh(s, 0f, 0.3f, 1800f, 600f, 1.2f, 1f, 830, 0.3f);
            Smear(s, 0.25f, 0.24f, 400f, 120f, 0.8f, 831, 0.01f, 0.08f);
            return s;
        }

        static float[] Barred()
        {
            var s = Buf(0.2f);
            Drop(s, 0f, 0.1f, 180f, 90f, 1f, 0.001f, 0.03f);
            Smear(s, 0f, 0.08f, 600f, 250f, 0.6f, 840, 0.001f, 0.025f);
            return s;
        }

        static float[] Offered()
        {
            var s = Buf(0.4f);
            Smear(s, 0f, 0.2f, 500f, 900f, 1f, 850, 0.02f, 0.08f);
            Ring(s, 0.12f, 0.28f, EnemySounds.CantorHz, 0.3f, 0.1f, EnemySounds.Handbell);
            return s;
        }

        static float[] PelletLand()
        {
            var s = Buf(0.12f);
            Drop(s, 0f, 0.05f, 400f, 150f, 1f, 0.001f, 0.02f);
            Smear(s, 0f, 0.08f, 900f, 300f, 0.6f, 860, 0.001f, 0.03f);
            return s;
        }

        // ---- her Instruments ----

        static float[] DartThrow() { var s = Buf(0.15f); Whoosh(s, 0f, 0.12f, 1500f, 3500f, 2f, 1f, 900, 0.3f); Click(s, 0f, 4000f, 0.5f, 901); return s; }

        static float[] PlumbDrop()
        {
            var s = Buf(0.3f);
            Drop(s, 0f, 0.22f, 900f, 200f, 0.6f, 0.004f, 0.15f);
            Drop(s, 0.22f, 0.07f, 120f, 70f, 1f, 0.001f, 0.025f);
            return s;
        }

        static float[] LensRaise() { var s = Buf(0.2f); Ring(s, 0f, 0.18f, 3200f, 1f, 0.06f, Glass); Whoosh(s, 0f, 0.1f, 1200f, 2400f, 1.5f, 0.3f, 910, 0.4f); return s; }

        static float[] LanternLight()
        {
            var s = Buf(0.4f);
            Smear(s, 0f, 0.15f, 2500f, 900f, 1f, 920, 0.002f, 0.05f);
            Grain(s, 0.03f, 0.3f, 60f, 0.006f, 3000f, 6000f, 2f, 0.5f, 921, 10f);
            Drop(s, 0.05f, 0.15f, 200f, 120f, 0.6f, 0.01f, 0.06f);
            return s;
        }

        static float[] HookCast()
        {
            var s = Buf(0.25f);
            Drop(s, 0f, 0.18f, 400f, 1800f, 0.7f, 0.004f, 0.15f);
            Scratch(s, 0f, 0.18f, 1400f, 3600f, 2f, 0.35f, 930, 0.004f, 0.1f);
            Click(s, 0.19f, 3400f, 0.8f, 931);
            return s;
        }

        static float[] TinctureDrink()
        {
            var s = Buf(0.3f);
            Drop(s, 0f, 0.08f, 600f, 250f, 1f, 0.002f, 0.03f);
            Smear(s, 0.06f, 0.22f, 500f, 200f, 0.7f, 940, 0.02f, 0.08f);
            return s;
        }

        static float[] SealSet()
        {
            var s = Buf(0.25f);
            Smear(s, 0f, 0.1f, 900f, 400f, 1f, 950, 0.005f, 0.03f);
            Drop(s, 0.04f, 0.12f, 150f, 80f, 0.9f, 0.002f, 0.04f);
            Click(s, 0.04f, 2800f, 0.4f, 951);
            return s;
        }

        static float[] Parry() { var s = Buf(0.25f); Click(s, 0f, 5000f, 0.6f, 960); Ring(s, 0f, 0.24f, 4200f, 1f, 0.07f, Glass); return s; }

        static float[] Charter() { var s = Buf(0.35f); Whoosh(s, 0f, 0.2f, 800f, 1800f, 1f, 1f, 970, 0.4f); Scratch(s, 0.15f, 0.18f, 2000f, 1200f, 2f, 0.6f, 971, 0.004f, 0.06f); return s; }

        static float[] ClarityGone() { var s = Buf(0.7f); Smear(s, 0f, 0.65f, 1500f, 4000f, 1f, 980, 0.1f, 0.3f); return s; }

        static float[] ClarityGrew()
        {
            var s = Buf(0.5f);
            Drop(s, 0f, 0.3f, 300f, 600f, 0.6f, 0.02f, 0.2f);
            Ring(s, 0.1f, 0.4f, EnemySounds.CantorHz * 2f, 0.4f, 0.12f, EnemySounds.Handbell);
            return s;
        }

        static float[] BeltTurn() { var s = Buf(0.05f); Click(s, 0f, 3400f, 1f, 990); Drop(s, 0f, 0.03f, 2000f, 1900f, 0.5f, 0.0005f, 0.01f); return s; }

        static float[] Prompt() { var s = Buf(0.04f); Click(s, 0f, 4500f, 1f, 995); Drop(s, 0f, 0.025f, 3000f, 2900f, 0.4f, 0.0005f, 0.008f); return s; }
    }
}
