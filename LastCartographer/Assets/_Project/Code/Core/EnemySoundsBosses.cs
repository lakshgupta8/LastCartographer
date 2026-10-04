using System;

namespace OWSBG.Core
{
    public static partial class EnemySounds
    {
        /// <summary>
        /// The bosses' own voices (AUD-15, docs/design/enemy-sounds.md §2a). A boss is heard doing what it does, never
        /// reading: a move's cue sits on the clip of the move itself (the sweep, the slam, the stomp), never on a clip the
        /// fight telegraphs with, because the tell is the read's sound (audio-direction 4). A loop is the body going on (a
        /// stone wingbeat, iron feet, a pen writing a Bind). What is no clip of the body's (a bell tolled, a rope cut, a
        /// limb redrawn, the furnace floor shifting) is an event: a count the boss already keeps, heard each time it goes
        /// up (<see cref="Voice.Events"/>). A boss's phase may have its own take of an event's cue (<c>bells_toll_p3</c>,
        /// the great bell), and a few die their own way.
        /// </summary>
        static void Bosses()
        {
            Family("LampKeeper", Material.Wing, "a Remnant gannet fused to the lighthouse's lamp: the lamp humming while its beam turns, the dive tearing the air, the lamp's glass rattling on the boards when it is grounded, wingbeats climbing back",
                moves: Map("dive", "lamp_dive", "grounded", "lamp_grounded", "return", "lamp_return"), loops: Map("beam", "lamp_beam"));
            Family("Halvard", Material.Brass, "the Guild's sergeant: a Warden's lens and lance, the lunge's long air, three brass clicks as he counts the paces, the lance thrown on its cord and reeled home",
                moves: Map("measure", "warden_measure", "thrust", "warden_thrust", "lunge", "halvard_lunge", "count", "halvard_count", "throw", "halvard_throw", "recall", "halvard_recall"));
            Family("Brann", Material.Brass, "the Cinder Warden: iron feet charging across the grates, two lances crossing with a ring, the furnace roaring through his brass while he holds, and the grates clanking as the heat shifts",
                moves: Map("thrust", "warden_thrust", "charge", "brann_charge", "crosscut", "brann_crosscut"), loops: Map("hold", "brann_hold"), events: Map("Shifts", "brann_shift"));
            Family("Oriel", Material.Brass, "the Watch's best, in Wren's own strokes: clean cuts of air and chalk, the Flourish's long sweep, a scuff as she steps away, a pen scratching while she Binds and snapping when the word is broken",
                moves: Map("strike1", "oriel_strike", "strike2", "oriel_strike", "strike3", "oriel_strike", "flourish", "oriel_flourish", "step", "oriel_step"),
                loops: Map("bind", "oriel_bind"), events: Map("BindsDenied", "oriel_denied"));
            Family("Gatekeeper", Material.Earth, "a stone eagle with roots for wings: roots creaking as it rises, a stone wing grinding along the floor, heavy stone wingbeats when it flies, a pass's air and a landing that shakes the gate",
                moves: Map("rise", "gate_rise", "sweep", "gate_sweep", "pass", "gate_pass", "land", "gate_land"), loops: Map("fly", "gate_fly", "pass", "gate_fly"));
            Family("Choir", Material.Bell, "three Cantor doves: a dove's toll as its ring lands, and a bell choked off by a strike",
                events: Map("Tolls", "cantor_toll", "Cancelled", "bell_choke"));
            Family("HalfCathedralBells", Material.Bell, "the nave's bells: a tower bell tolling as each ring takes the light (the great bell an octave under), a rope cut and the bell swinging to rest, and the last hum let go",
                events: Map("Rings", "bells_toll", "Cuts", "bells_cut"), death: "bells_death");
            Family("Hale", Material.Earth, "a Guild surveyor with a quill of his own: a dry stroke of it, and the stones knocked as he counts them",
                moves: Map("quill", "hale_quill", "count", "hale_count"));
            Family("Collapse", Material.Earth, "the mine's collapse itself: rubble coming down as it shakes the roof",
                moves: Map("shake", "collapse_fall"));
            Family("FallenStar", Material.Ember, "an iron meteorite-golem: iron feet, a slam that rings the ground, iron walls grinding up, a roar of burning in the last phase, and its fall: iron striking the ground and cooling, ticking",
                moves: Map("slam", "star_slam"), loops: Map("walk", "star_walk", "burn", "star_burn"), events: Map("WallRaisings", "star_walls"), death: "star_fall");
            Family("Voss", Material.Paper, "the Guildmaster, the Unwriter: a Warden's lance and lunge, the compass-rose shield ringing deep as he guards, and dry paper when he is struck",
                moves: Map("thrust", "warden_thrust", "lunge", "halvard_lunge", "guard", "voss_guard"));
            Family("CorrasDrawing", Material.Graphite, "a child's crayon drawing of her father: wax scribbling as it walks, dragged hard in a swipe, a stomp of paper, a limb scribbled back in after a hit and the small one drawn back quick; in outline, a thin pencil line",
                moves: Map("swipe", "corra_swipe", "stomp", "corra_stomp", "swipe_outline", "corra_line", "stomp_outline", "corra_stomp"),
                loops: Map("move", "corra_scribble", "move_outline", "corra_scribble"), events: Map("LimbsRedrawn", "corra_redraw", "SmallStruck", "corra_small"));
            Family("Archivist", Material.Graphite, "Corvin, a half-drawn owl: a quill drawing in long strokes, and an owl's swoop, almost silent",
                moves: Map("swoop", "archivist_swoop"), loops: Map("draw", "archivist_draw"));
            Family("CompleteSurvey", Material.Graphite, "the Great Atlas: the chorus is its voice (the music keeps its beat); the page itself is only ink");
            Family("ReedmotherBrood", Material.Wing, "a great bird over its nest: the reeds thrashing, the nest's reeds parting as it opens, the reeds burning",
                moves: Map("thresh", "brood_thresh", "open", "brood_open"), loops: Map("burn", "ember_crackle"));
            BossParts();
        }

        /// <summary>The bosses' cues, into <see cref="InkSounds"/>' table: moves, loops and events as world one-shots, a death of its own as an enemy death.</summary>
        static void RegisterBosses()
        {
            void One(string id, string what, Func<float[]> render, float gain = 1f, bool loop = false, bool death = false) =>
                InkSounds.Add(id, InkSounds.Kind.Enemy, what, render, gain, loop, null, death ? AudioDirection.Voice.Enemy : AudioDirection.Voice.World);
            One("lamp_beam", "the lamp humming as its beam turns, its glass singing", LampBeam, 0.35f, true);
            One("lamp_dive", "the gannet's dive: air torn, falling", LampDive, 0.9f);
            One("lamp_grounded", "the lamp on the boards: a thud and its glass rattling", LampGrounded, 0.9f);
            One("lamp_return", "three wingbeats climbing back to the lamp", LampReturn, 0.7f);
            One("halvard_lunge", "the lunge: a long draw of air and a clink at full reach", HalvardLunge, 0.9f);
            One("halvard_count", "three brass clicks: \"Three paces. I measured them.\"", HalvardCount, 0.7f);
            One("halvard_throw", "the lance thrown, its cord paying out", HalvardThrow, 0.9f);
            One("halvard_recall", "the cord reeled in and the lance home with a clink", HalvardRecall, 0.7f);
            One("brann_charge", "iron feet charging across the grates", BrannCharge, 0.9f);
            One("brann_crosscut", "two lances crossing: two airs and a ring", BrannCrosscut, 0.9f);
            One("brann_hold", "the furnace roaring through his brass while he holds", BrannHold, 0.4f, true);
            One("brann_shift", "the grates clanking as the heat shifts, and the heat's breath", BrannShift, 0.8f);
            One("oriel_strike", "a clean cut of air and a scrape of chalk", OrielStrike, 0.8f);
            One("oriel_flourish", "the Flourish: one long sweep", OrielFlourish, 0.9f);
            One("oriel_step", "a scuff of chalk as she steps away", OrielStep, 0.5f);
            One("oriel_bind", "a pen writing, fast: her Bind", OrielBind, 0.35f, true);
            One("oriel_denied", "the word broken off: the nib snapping on the page", OrielDenied, 0.9f);
            One("gate_rise", "roots creaking and stone grinding as it climbs the gate", GateRise, 0.9f);
            One("gate_sweep", "a stone wing grinding along the floor", GateSweep, 1f);
            One("gate_pass", "the air of a heavy pass", GatePass, 0.9f);
            One("gate_land", "a stone landing: the gate shaken and grit falling", GateLand, 1f);
            One("gate_fly", "stone wings beating, heavily", GateFly, 0.45f, true);
            One("bell_choke", "a bell choked off: struck and stopped", BellChoke, 0.8f);
            One("bells_toll", "a tower bell tolling in D, with its hum", BellsToll, 1f);
            One("bells_toll_p3", "the great bell tolling, an octave under", BellsTollGreat, 1f);
            One("bells_cut", "a rope cut: its snap, and the bell swinging to rest", BellsCut, 0.9f);
            One("bells_death", "the last hum let go, sagging into the white", BellsDeath, 1f, death: true);
            One("hale_quill", "a dry stroke of a quill", HaleQuill, 0.8f);
            One("hale_count", "stones knocked, counted", HaleCount, 0.7f);
            One("collapse_fall", "rubble coming down from the roof", CollapseFall, 1f);
            One("star_slam", "iron slamming the ground, ringing", StarSlam, 1f);
            One("star_walk", "iron feet", StarWalk, 0.45f, true);
            One("star_burn", "the iron burning: a roar and a crackle", StarBurn, 0.4f, true);
            One("star_fall", "the star's fall: iron striking the ground, a long hiss and the metal ticking as it cools", StarFall, 1f, death: true);
            One("voss_guard", "the compass-rose shield ringing deep", VossGuard, 0.8f);
            One("corra_swipe", "a crayon dragged hard across the page", CorraSwipe, 0.9f);
            One("corra_line", "a thin pencil line, fast", CorraLine, 0.7f);
            One("corra_stomp", "a stomp of paper", CorraStomp, 0.9f);
            One("corra_scribble", "a crayon scribbling as it walks", CorraScribble, 0.35f, true);
            One("corra_redraw", "a limb scribbled back in", CorraRedraw, 0.8f);
            One("corra_small", "the small one drawn back, quick and light", CorraSmall, 0.6f);
            One("archivist_draw", "a quill drawing in long strokes", ArchivistDraw, 0.4f, true);
            One("archivist_swoop", "an owl's swoop: almost nothing", ArchivistSwoop, 0.4f);
            One("brood_thresh", "the reeds thrashing", BroodThresh, 0.9f);
            One("brood_open", "the nest's reeds parting", BroodOpen, 0.8f);
            RegisterParts();
        }

        /// <summary>A phase's own take of a cue, if it has one (<c>bells_toll_p3</c>: the great bell), else the cue.</summary>
        public static string PhaseCue(string cue, int phase) => cue != null && InkSounds.Has(cue + "_p" + phase) ? cue + "_p" + phase : cue;

        /// <summary>The partials of struck iron: inharmonic, a little dull.</summary>
        internal static readonly (float, float)[] Iron = { (1f, 1f), (2.32f, 0.6f), (4.25f, 0.35f), (6.63f, 0.2f) };
        /// <summary>The Half-Cathedral's tower bell, in the Greyfold's D: two octaves under the Cantor's handbell.</summary>
        public const float NaveBellHz = CantorHz / 4f;

        // ---- the Lamp-Keeper ----

        static float[] LampBeam()
        {
            var s = Buf(1f + Ov);
            Buzz(s, 0f, 1f + Ov, 110f, 1f, 2f, 0.01f, 600f, 600);           // 110 cycles and 2 turns a second: the loop meets itself
            Smear(s, 0f, 1f + Ov, 2400f, 2400f, 0.15f, 601, 0.05f, 10f);     // the glass singing
            return Seamless(s, Ov);
        }

        static float[] LampDive()
        {
            var s = Buf(0.55f);
            Whoosh(s, 0f, 0.48f, 2600f, 600f, 1.2f, 1f, 602, 0.6f);
            Drop(s, 0.3f, 0.2f, 300f, 120f, 0.3f, 0.01f, 0.08f);
            return s;
        }

        static float[] LampGrounded()
        {
            var s = Buf(0.45f);
            Drop(s, 0f, 0.18f, 120f, 60f, 1f, 0.001f, 0.06f);
            Grain(s, 0f, 0.35f, 60f, 0.006f, 4000f, 7000f, 3f, 0.6f, 603, 10f);
            Ring(s, 0.01f, 0.2f, 3100f, 0.4f, 0.05f, Shell);
            return s;
        }

        static float[] LampReturn()
        {
            var s = Buf(0.6f);
            for (int k = 0; k < 3; k++) Whoosh(s, k * 0.17f, 0.15f, 600f + 200f * k, 1400f + 300f * k, 1f, 1f - k * 0.15f, 604 + k, 0.4f);
            return s;
        }

        // ---- the Wardens ----

        static float[] HalvardLunge()
        {
            var s = Buf(0.4f);
            Whoosh(s, 0f, 0.28f, 700f, 2400f, 1.5f, 1f, 610, 0.3f);
            Ring(s, 0.24f, 0.12f, 3000f, 0.5f, 0.03f, Brass);
            return s;
        }

        static float[] HalvardCount()
        {
            var s = Buf(0.66f);
            float[] hz = { 2600f, 2600f, 3100f };
            for (int k = 0; k < 3; k++) { Click(s, k * 0.25f, hz[k], 0.6f, 611 + k); Ring(s, k * 0.25f, 0.12f, hz[k], 0.6f, 0.03f, Brass); }
            return s;
        }

        static float[] HalvardThrow()
        {
            var s = Buf(0.45f);
            Whoosh(s, 0f, 0.2f, 900f, 2600f, 1.5f, 1f, 614, 0.3f);
            Grain(s, 0.08f, 0.32f, 70f, 0.008f, 1200f, 2000f, 1f, 0.5f, 615, 30f, true);
            return s;
        }

        static float[] HalvardRecall()
        {
            var s = Buf(0.6f);
            Grain(s, 0f, 0.4f, 25f, 0.006f, 3000f, 4000f, 3f, 0.7f, 616, 45f);
            Click(s, 0.44f, 3200f, 0.6f, 617);
            Ring(s, 0.44f, 0.12f, 3200f, 0.7f, 0.03f, Brass);
            return s;
        }

        static float[] BrannCharge()
        {
            var s = Buf(0.6f);
            for (int k = 0; k < 4; k++)
            {
                Drop(s, k * 0.12f, 0.08f, 150f, 70f, 1f, 0.001f, 0.03f);
                Ring(s, k * 0.12f, 0.06f, 900f + 60f * k, 0.3f, 0.02f, Iron);
            }
            Smear(s, 0f, 0.55f, 300f, 300f, 0.4f, 618, 0.02f, 0.2f);
            return s;
        }

        static float[] BrannCrosscut()
        {
            var s = Buf(0.42f);
            Whoosh(s, 0f, 0.2f, 800f, 2600f, 1.5f, 1f, 619, 0.4f);
            Whoosh(s, 0.04f, 0.2f, 2600f, 800f, 1.5f, 0.8f, 620, 0.4f);
            Ring(s, 0.15f, 0.25f, 2800f, 0.6f, 0.06f, Brass);
            Ring(s, 0.15f, 0.25f, 3300f, 0.5f, 0.05f, Brass);
            return s;
        }

        static float[] BrannHold()
        {
            var s = Buf(1f + Ov);
            Smear(s, 0f, 1f + Ov, 260f, 260f, 1f, 621, 0.05f, 10f);                // the furnace's roar
            Grain(s, 0f, 1f + Ov, 18f, 0.006f, 3000f, 6000f, 2f, 0.35f, 622);        // and its sparks
            return Seamless(s, Ov);
        }

        static float[] BrannShift()
        {
            var s = Buf(0.55f);
            for (int k = 0; k < 3; k++) Ring(s, k * 0.07f, 0.18f, 700f + 140f * k, 0.6f, 0.05f, Iron);
            Smear(s, 0.05f, 0.45f, 200f, 500f, 0.7f, 623, 0.05f, 0.12f);           // the heat's breath
            return s;
        }

        static float[] OrielStrike()
        {
            var s = Buf(0.18f);
            Whoosh(s, 0f, 0.12f, 1500f, 3500f, 1.5f, 1f, 630, 0.3f);
            Scratch(s, 0.05f, 0.06f, 3000f, 2200f, 1.5f, 0.5f, 631, 0.002f, 0.02f);
            Ring(s, 0.09f, 0.06f, 3600f, 0.25f, 0.015f, Brass);
            return s;
        }

        static float[] OrielFlourish()
        {
            var s = Buf(0.45f);
            Whoosh(s, 0f, 0.36f, 900f, 3800f, 1.2f, 1f, 632, 0.6f);
            Scratch(s, 0.05f, 0.3f, 2000f, 3400f, 2f, 0.4f, 633, 0.01f, 0.1f);
            return s;
        }

        static float[] OrielStep()
        {
            var s = Buf(0.14f);
            Scratch(s, 0f, 0.1f, 1800f, 1200f, 1.5f, 1f, 634, 0.002f, 0.04f);
            Click(s, 0f, 2400f, 0.4f, 635);
            return s;
        }

        static float[] OrielBind()
        {
            var s = Buf(0.8f + Ov);
            Grain(s, 0f, 0.8f + Ov, 22f, 0.01f, 2000f, 3200f, 2.5f, 1f, 636);
            Smear(s, 0f, 0.8f + Ov, 1400f, 1400f, 0.2f, 637, 0.05f, 10f);
            return Seamless(s, Ov);
        }

        static float[] OrielDenied()
        {
            var s = Buf(0.2f);
            Click(s, 0f, 4200f, 1f, 638);
            Scratch(s, 0f, 0.08f, 3500f, 1000f, 2f, 0.8f, 639, 0.001f, 0.03f);
            Drop(s, 0.01f, 0.06f, 260f, 140f, 0.4f, 0.001f, 0.02f);
            return s;
        }

        // ---- the Gatekeeper ----

        static float[] GateRise()
        {
            var s = Buf(0.75f);
            Scratch(s, 0f, 0.6f, 300f, 700f, 6f, 1f, 640, 0.05f, 0.3f);              // the roots creaking
            Smear(s, 0f, 0.65f, 160f, 220f, 0.7f, 641, 0.05f, 0.25f);                // stone grinding
            Grain(s, 0.1f, 0.5f, 30f, 0.01f, 1500f, 3000f, 2f, 0.4f, 642, 10f);
            return s;
        }

        static float[] GateSweep()
        {
            var s = Buf(0.6f);
            Smear(s, 0f, 0.5f, 400f, 200f, 1f, 643, 0.02f, 0.2f);
            Grain(s, 0f, 0.5f, 70f, 0.01f, 1200f, 2800f, 2f, 0.5f, 644);
            Whoosh(s, 0f, 0.45f, 300f, 900f, 1f, 0.6f, 645, 0.5f);
            return s;
        }

        static float[] GatePass()
        {
            var s = Buf(0.7f);
            Whoosh(s, 0f, 0.6f, 300f, 1200f, 0.8f, 1f, 646, 0.5f);
            Drop(s, 0.15f, 0.35f, 90f, 60f, 0.5f, 0.05f, 0.15f);
            return s;
        }

        static float[] GateLand()
        {
            var s = Buf(0.8f);
            Drop(s, 0f, 0.4f, 90f, 40f, 1f, 0.001f, 0.15f);
            Smear(s, 0f, 0.4f, 400f, 120f, 0.6f, 647, 0.002f, 0.12f);
            Grain(s, 0.05f, 0.65f, 60f, 0.01f, 1500f, 3500f, 2f, 0.5f, 648, 6f);
            return s;
        }

        static float[] GateFly()
        {
            var s = Buf(1.2f + Ov);
            for (int k = 0; k < 2; k++)
            {
                Whoosh(s, k * 0.6f, 0.45f, 250f, 700f, 0.8f, 1f, 649 + k, 0.35f);
                Scratch(s, k * 0.6f + 0.05f, 0.3f, 400f, 550f, 6f, 0.2f, 651 + k, 0.03f, 0.1f);   // stone creaking at the shoulder
            }
            return Seamless(s, Ov);
        }

        // ---- bells ----

        static float[] BellChoke()
        {
            var s = Buf(0.16f);
            Ring(s, 0f, 0.07f, CantorHz, 1f, 0.03f, Handbell);
            Drop(s, 0.02f, 0.05f, 300f, 160f, 0.5f, 0.001f, 0.02f);
            Smear(s, 0.02f, 0.08f, 900f, 400f, 0.3f, 660, 0.002f, 0.03f);
            return s;
        }

        static float[] BellsToll()
        {
            var s = Buf(2.6f);
            Ring(s, 0f, 2.5f, NaveBellHz, 1f, 0.8f, TowerBell);
            return s;
        }

        static float[] BellsTollGreat()
        {
            var s = Buf(3.6f);
            Ring(s, 0f, 3.5f, NaveBellHz / 2f, 1f, 1.2f, TowerBell);
            return s;
        }

        static float[] BellsCut()
        {
            var s = Buf(0.8f);
            Click(s, 0f, 2500f, 1f, 661);
            Scratch(s, 0f, 0.06f, 1800f, 600f, 2f, 0.8f, 662, 0.001f, 0.02f);         // the rope's snap
            Smear(s, 0.05f, 0.4f, 700f, 300f, 0.3f, 663, 0.05f, 0.15f);              // the bell swinging
            Ring(s, 0.25f, 0.4f, NaveBellHz, 0.3f, 0.15f, TowerBell);                 // the clapper's last two knocks, soft
            Ring(s, 0.5f, 0.28f, NaveBellHz, 0.15f, 0.1f, TowerBell);
            return s;
        }

        static float[] BellsDeath()
        {
            var s = Buf(3.1f);
            Ring(s, 0f, 3f, NaveBellHz / 2f, 1f, 1f, TowerBell, NaveBellHz / 2f * 0.94f);
            return s;
        }

        // ---- Hale, the Collapse, the Star, Voss ----

        static float[] HaleQuill()
        {
            var s = Buf(0.22f);
            Scratch(s, 0f, 0.18f, 2600f, 1500f, 2f, 1f, 670, 0.003f, 0.06f);
            Click(s, 0f, 3600f, 0.4f, 671);
            return s;
        }

        static float[] HaleCount()
        {
            var s = Buf(0.55f);
            for (int k = 0; k < 3; k++) { Click(s, k * 0.18f, 1600f, 0.7f, 672 + k); Drop(s, k * 0.18f, 0.06f, 320f, 220f, 0.8f, 0.001f, 0.02f); }
            return s;
        }

        static float[] CollapseFall()
        {
            var s = Buf(1f);
            Smear(s, 0f, 0.9f, 140f, 80f, 1f, 680, 0.03f, 0.3f);
            Grain(s, 0f, 0.8f, 30f, 0.02f, 400f, 1200f, 1.5f, 0.8f, 681, 6f);
            for (int k = 0; k < 3; k++) Drop(s, 0.1f + k * 0.22f, 0.12f, 130f - 15f * k, 60f, 0.7f - 0.15f * k, 0.001f, 0.05f);
            return s;
        }

        static float[] StarSlam()
        {
            var s = Buf(0.9f);
            Drop(s, 0f, 0.5f, 70f, 35f, 1f, 0.001f, 0.2f);
            Ring(s, 0f, 0.7f, 420f, 0.5f, 0.3f, Iron);
            Grain(s, 0.02f, 0.4f, 60f, 0.01f, 1500f, 3500f, 2f, 0.4f, 690, 8f);
            return s;
        }

        static float[] StarWalk()
        {
            var s = Buf(1.6f + Ov);
            for (int k = 0; k < 2; k++)
            {
                Drop(s, k * 0.8f, 0.15f, 110f, 60f, 1f, 0.001f, 0.05f);
                Ring(s, k * 0.8f, 0.2f, 600f, 0.3f, 0.06f, Iron);
            }
            return Seamless(s, Ov);
        }

        static float[] StarBurn()
        {
            var s = Buf(1f + Ov);
            Smear(s, 0f, 1f + Ov, 600f, 600f, 1f, 691, 0.05f, 10f);
            Grain(s, 0f, 1f + Ov, 30f, 0.006f, 3000f, 6500f, 2f, 0.6f, 692);
            return Seamless(s, Ov);
        }

        static float[] StarFall()
        {
            var s = Buf(1.7f);
            Drop(s, 0f, 0.8f, 80f, 30f, 1f, 0.001f, 0.3f);
            Ring(s, 0f, 1f, 300f, 0.6f, 0.5f, Iron);
            Smear(s, 0.1f, 1.2f, 3000f, 1500f, 0.5f, 693, 0.05f, 0.5f);               // the long hiss
            Grain(s, 0.6f, 1f, 6f, 0.004f, 4000f, 6000f, 3f, 0.4f, 694, 2f);          // the metal ticking as it cools
            return s;
        }

        static float[] VossGuard()
        {
            var s = Buf(0.6f);
            Click(s, 0f, 2000f, 0.5f, 695);
            Ring(s, 0f, 0.55f, 700f, 1f, 0.25f, Brass);
            return s;
        }

        // ---- Corra's Drawing, the Archivist, the Brood ----

        static float[] CorraSwipe()
        {
            var s = Buf(0.36f);
            Scratch(s, 0f, 0.3f, 900f, 600f, 0.8f, 1f, 700, 0.005f, 0.1f);            // wax, dragged
            Grain(s, 0f, 0.28f, 50f, 0.008f, 1500f, 2500f, 2f, 0.5f, 701);            // skipping on the paper's tooth
            return s;
        }

        static float[] CorraLine()
        {
            var s = Buf(0.15f);
            Scratch(s, 0f, 0.12f, 2400f, 1800f, 3f, 1f, 702, 0.002f, 0.04f);
            return s;
        }

        static float[] CorraStomp()
        {
            var s = Buf(0.3f);
            Drop(s, 0f, 0.15f, 150f, 70f, 1f, 0.001f, 0.05f);
            Smear(s, 0f, 0.15f, 1000f, 400f, 0.5f, 703, 0.002f, 0.05f);
            Grain(s, 0f, 0.12f, 90f, 0.008f, 2200f, 4200f, 2f, 0.4f, 704);
            return s;
        }

        static float[] CorraScribble()
        {
            var s = Buf(1.2f + Ov);
            for (int k = 0; k < 10; k++)
                Scratch(s, k * 0.12f, 0.11f, k % 2 == 0 ? 1000f : 1400f, k % 2 == 0 ? 1400f : 1000f, 1f, 1f, 705 + k, 0.01f, 0.04f);
            return Seamless(s, Ov);
        }

        static float[] CorraRedraw()
        {
            var s = Buf(0.32f);
            for (int k = 0; k < 6; k++)
                Scratch(s, k * 0.045f, 0.04f, k % 2 == 0 ? 1000f : 1500f, k % 2 == 0 ? 1500f : 1000f, 1f, 1f, 715 + k, 0.002f, 0.015f);
            return s;
        }

        static float[] CorraSmall()
        {
            var s = Buf(0.18f);
            for (int k = 0; k < 4; k++)
                Scratch(s, k * 0.035f, 0.03f, k % 2 == 0 ? 2000f : 2600f, k % 2 == 0 ? 2600f : 2000f, 1.5f, 1f, 721 + k, 0.002f, 0.012f);
            return s;
        }

        static float[] ArchivistDraw()
        {
            var s = Buf(1f + Ov);
            for (int k = 0; k < 2; k++)
            {
                Scratch(s, k * 0.5f, 0.42f, 2200f, 1600f, 2f, 1f, 725 + k, 0.02f, 0.15f);
                Click(s, k * 0.5f, 3400f, 0.3f, 727 + k);
            }
            return Seamless(s, Ov);
        }

        static float[] ArchivistSwoop()
        {
            var s = Buf(0.55f);
            Whoosh(s, 0f, 0.5f, 800f, 1400f, 0.7f, 1f, 730, 0.5f);
            return s;
        }

        static float[] BroodThresh()
        {
            var s = Buf(0.5f);
            Grain(s, 0f, 0.42f, 120f, 0.012f, 1500f, 3500f, 1f, 1f, 735, 40f, true);
            Whoosh(s, 0f, 0.4f, 1000f, 2500f, 1f, 0.6f, 736, 0.4f);
            return s;
        }

        static float[] BroodOpen()
        {
            var s = Buf(0.65f);
            Scratch(s, 0f, 0.5f, 600f, 1500f, 1f, 0.8f, 737, 0.05f, 0.2f);
            Grain(s, 0f, 0.55f, 60f, 0.012f, 1500f, 3000f, 1f, 0.6f, 738, 15f, true);
            return s;
        }
    }
}
