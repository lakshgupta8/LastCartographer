using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>What a piece of environmental storytelling is (NAR-15): something to read, something woven, what's left of someone, or a sign of how a place lives.</summary>
    public enum DressingKind
    {
        /// <summary>Words cut, painted, chalked or pinned: a lintel, a board, a milestone, a notice.</summary>
        Inscription,
        /// <summary>A picture made to be kept: a tapestry, a wagon-cloth, a child's drawing.</summary>
        Tapestry,
        /// <summary>What's left of someone: a cut tether, a turned-down cup, a lamp on a hook. The faded are not dead, so never them.</summary>
        Remains,
        /// <summary>A sign of how a place lives: raked leaves, unfalling dust, one thumb in every sheet.</summary>
        Trace,
    }

    /// <summary>
    /// What changes a piece with its place (ENV-06): a flag set (the walk down Hollowvein), or the place's fate among
    /// some (Merrow's End held or anchored, Aldermere released, Lowmarket anchored). The Yarn scene branches on the
    /// same thing; <c>DressingProp</c> shows the piece's second drawing while it is met.
    /// </summary>
    [Serializable]
    public struct DressingChange
    {
        /// <summary>A flag that, set, changes the piece; or null.</summary>
        public string Flag;
        /// <summary>A place whose fate changes the piece; or null.</summary>
        public string Place;
        /// <summary>The fates that change it, lower-case and |-separated ("held|anchored").</summary>
        public string Fates;

        public bool IsEmpty => string.IsNullOrEmpty(Flag) && string.IsNullOrEmpty(Place);

        public static DressingChange OnFlag(string flag) => new DressingChange { Flag = flag };
        public static DressingChange OnFate(string place, params PlaceFate[] fates)
            => new DressingChange { Place = place, Fates = string.Join("|", fates.Select(Places.Describe)) };

        /// <summary>Whether the world has changed the piece.</summary>
        public bool IsMet(WorldState w)
        {
            if (w == null) return false;
            if (!string.IsNullOrEmpty(Flag) && w.Is(Flag)) return true;
            if (string.IsNullOrEmpty(Place)) return false;
            var fate = Places.Describe(Places.FateOf(w, Place));
            foreach (var f in (Fates ?? "").Split('|')) if (f == fate) return true;
            return false;
        }

        public override string ToString() => IsEmpty ? "never" : Flag != null ? "flag " + Flag : Place + " " + Fates;
    }

    /// <summary>One piece of the environmental pass: where it stands, what it is, and, if it can be read, the Yarn scene that reads it.</summary>
    public sealed class DressingPiece
    {
        public string Id;
        /// <summary>A planned room (<see cref="RoomPlans"/>) or a built Saltmarrow room ("Saltmarrow_Stilts" is the greybox scene "Greybox_Saltmarrow_Stilts").</summary>
        public string Room;
        public DressingKind Kind;
        /// <summary>The Yarn scene that reads it with up (the Talker's start node), or null for a piece that is only seen.</summary>
        public string Node;
        /// <summary>For the artist (ENV-06): what stands there, in a sentence or two.</summary>
        public string Brief;
        /// <summary>The secret or bible section it plants ("5.6", "9.2"), or null. A read piece's plant is tagged in its Yarn too.</summary>
        public string Plant;
        /// <summary>How it changes with the place's fate or fade, or null if it never does.</summary>
        public string Changes;
        /// <summary>The kit drawing that stands for it (ENV-06): "Prop_&lt;Prop&gt;" in its region's kit. Every piece has one.</summary>
        public string Prop;
        /// <summary>The drawing shown once <see cref="Change"/> is met, or null for a piece that never changes.</summary>
        public string PropAfter;
        /// <summary>What swaps <see cref="Prop"/> for <see cref="PropAfter"/>; empty for a piece that never changes.</summary>
        public DressingChange Change;
        public bool Readable => Node != null;
        public Region Region => Dressing.RegionOfRoom(Room);
    }

    /// <summary>
    /// The environmental storytelling pass (NAR-15, <c>docs/story/environment.md</c>): the inscriptions, tapestries,
    /// remains and traces in every region's rooms, as a catalog for the art pass (ENV-06). Environment first (style
    /// guide 6): what a lintel or a tapestry can carry, no NPC is asked to say. A readable piece is a Yarn scene in
    /// <c>&lt;Region&gt;_Environment.yarn</c> where the thing speaks, never a person, and reading it changes nothing.
    /// Five of them are the planned plants the foreshadowing audit was waiting on (NAR-16 §4).
    /// </summary>
    public static class Dressing
    {
        static readonly List<DressingPiece> _all = new List<DressingPiece>();

        public static IReadOnlyList<DressingPiece> All => _all;
        public static DressingPiece Find(string id) => _all.Find(p => p.Id == id);
        public static IEnumerable<DressingPiece> In(Region r) => _all.Where(p => p.Region == r);
        public static IEnumerable<DressingPiece> InRoom(string room) => _all.Where(p => p.Room == room);
        /// <summary>The piece a Yarn scene reads, or null.</summary>
        public static DressingPiece ByNode(string node) => node == null ? null : _all.Find(p => p.Node == node);

        /// <summary>The region a room id belongs to: its prefix ("Halden_Hall_3" is Halden's).</summary>
        public static Region RegionOfRoom(string room)
        {
            var head = (room ?? "").Split('_')[0];
            return Enum.TryParse<Region>(head, out var r) ? r : throw new ArgumentException("no region for room " + room);
        }

        static void P(string room, DressingKind kind, string id, string node, string plant, string brief, string changes = null)
            => _all.Add(new DressingPiece { Id = id, Room = room, Kind = kind, Node = node, Plant = plant, Brief = brief, Changes = changes });

        /// <summary>The drawings (ENV-06): what stands for a piece, and what stands for it once its place has changed.</summary>
        static void Draw(string id, string prop, string after = null, DressingChange change = default)
        {
            var piece = Find(id) ?? throw new ArgumentException("no piece " + id);
            piece.Prop = prop; piece.PropAfter = after; piece.Change = change;
        }

        const string Walked = "emberdown.hollowvein.walked";

        static void Drawings()
        {
            Draw("shore.tetherposts", "TetherPosts");
            Draw("quay.price_board", "PriceBoard");
            Draw("stilts.ladders", "Ladders");
            Draw("merrow.lintels", "Lintels", "Lintels_Chalk", DressingChange.OnFate("Saltmarrow_B", PlaceFate.Held, PlaceFate.Anchored));
            Draw("ferry.boats", "Moorings");
            Draw("chain.log", "Log");
            Draw("chain.faded_keeper", "Keeper");
            Draw("chapel.tapestry", "Tapestry");
            Draw("rest.lintel", "Lintel");
            Draw("rest.tally_wall", "TallyWall");
            Draw("pithead.cups", "Cups", "Cups_Up", DressingChange.OnFlag(Walked));
            Draw("chimneys.foot", "ChimneyFoot");
            Draw("hollow.lamps", "HookLamps", "HookLamps_Lit", DressingChange.OnFlag(Walked));
            Draw("hollow.bottom", "Bottom", "Bottom_Swept", DressingChange.OnFlag(Walked));
            Draw("road.milestone", "Milestone");
            Draw("cloister.tapestry", "WoolMap", "WoolMap_Open", DressingChange.OnFate("Verdance_Aldermere_2", PlaceFate.Released));
            Draw("library.dust", "Dust");
            Draw("library.lectern", "Lectern");
            Draw("aldermere.bunting", "Bunting");
            Draw("gate.ledge", "Ledge");
            Draw("bridges.toll_board", "TollBoard");
            Draw("mills.sheets", "Sheets");
            Draw("hall.roll", "Roll");
            Draw("hall.order", "Order");
            Draw("hall.exam_papers", "ExamDesk");
            Draw("orchard.leaves", "Leaves");
            Draw("lowmarket.notice", "Notice", "Notice_Complete", DressingChange.OnFate("Halden_Lowmarket_2", PlaceFate.Anchored));
            Draw("bridges.seventh", "Scaffold");
            Draw("bastion.plaque", "Plaque");
            Draw("office.drawing", "Drawing");
            Draw("stones.notches", "Stone");
            Draw("camp.wagon_cloth", "Wagon");
            Draw("river.boats", "Hull");
            Draw("gate.lip", "LipStone");
            Draw("edgecamp.beam", "Beam");
            Draw("edgecamp.tethers", "CutTether");
            Draw("road.mileposts", "Milepost");
            Draw("road.footprints", "Footprints");
            Draw("threshold.old_tether", "OldTether");
            Draw("capital.nameplate", "Door");
            Draw("capital.crayon_floor", "Crayon");
            Draw("hollow.doorframe", "Doorframe");
        }

        static Dressing()
        {
            // ---- Saltmarrow: the built coast ---------------------------------------------------------------------------
            P("Saltmarrow_Shore", DressingKind.Remains, "shore.tetherposts", "Shore_Tetherposts", null,
                "A row of tether-posts along the tideline, ropes running into the sea-fade. Most are cut a wingspan out; one has a bracelet knotted on it.");
            P("Saltmarrow_A", DressingKind.Inscription, "quay.price_board", "Quay_PriceBoard", null,
                "The Ferrymen's price board by the desk, chalk on slate; the return price is a blank, and someone has chalked under it.");
            P("Saltmarrow_Stilts", DressingKind.Trace, "stilts.ladders", null, "5.6",
                "Roost doors at the tops of the stilts with landing ledges and no steps; rope ladders lashed on after, a different colour. A line of wing-bindings drying between two roosts.");
            P("Saltmarrow_B", DressingKind.Inscription, "merrow.lintels", "Merrow_Lintels", null,
                "A song's name carved over every door in Merrow's End. Most doors below them are white paper.",
                "Held: fresh chalk in the letters, a different hand each day. Anchored: fresh chalk, the same strokes every day. Fading: the doors go to paper first; the carvings last.");
            P("Saltmarrow_Ferry", DressingKind.Remains, "ferry.boats", null, null,
                "Two boat-shaped patches of paper on the landing's water where the boats were moored, their painted names still floating on the white.");
            P("Saltmarrow_Chain_1", DressingKind.Inscription, "chain.log", "Chain_Log", "5.6",
                "The first lighthouse's log, open on the lamp-room sill, weighted with a shell. Forty years of the same entry.");
            P("Saltmarrow_Chain_3", DressingKind.Trace, "chain.faded_keeper", null, null,
                "The faded third lighthouse: in its lamp room, a grey keeper's silhouette at the lamp, seen from outside and never from within (Aury, blank-islands).");
            P("Saltmarrow_Chapel", DressingKind.Tapestry, "chapel.tapestry", "Chapel_Tapestry", "5.6",
                "A salt-stiffened tapestry behind the altar: a flock crossing a sky over the sea, the birds eaten out by salt so only their holes remain.");

            // ---- Emberdown --------------------------------------------------------------------------------------------
            P("Emberdown_Rest_1", DressingKind.Inscription, "rest.lintel", "Rest_Lintel", "5.6",
                "Roosts cut into the cliff over the gate, doors high up for flyers, words cut over one; ladders bolted up the basalt to each, rungs worn pale.");
            P("Emberdown_Rest_2", DressingKind.Inscription, "rest.tally_wall", "Rest_TallyWall", null,
                "Kettil's tally wall behind the porch: a year and a count cut for every year since the town began. Every number different; one drop of thirty-one.");
            P("Emberdown_Rest_3", DressingKind.Remains, "pithead.cups", "PitHead_Cups", null,
                "A trestle by the boarded mine mouth set with thirty-one cups, turned down, a name scratched in each.",
                "After the walk down Hollowvein: right side up and washed.");
            P("Emberdown_Chimneys_3", DressingKind.Inscription, "chimneys.foot", "Chimneys_Foot", null,
                "At the ninth chimney's foot, a bare stone where the other eight have their builders' names cut; fresh soot on it (the Ninth Chimney commission).");
            P("Emberdown_Hollow_2", DressingKind.Remains, "hollow.lamps", "Hollow_Lamps", null,
                "A miner's lamp on a hook for each name down the gallery walls, a brass plate under each; dark until the walk.",
                "After the walk: every lamp lit.");
            P("Emberdown_Hollow_4", DressingKind.Remains, "hollow.bottom", null, null,
                "Under the rubble where the Collapse lay: a boot, a lamp, a pick with a name on the haft. Kept small and out of the light.",
                "After the walk: the rubble cleared, the stone swept, thirty-one names chalked on the wall by the families.");

            // ---- the Verdance -----------------------------------------------------------------------------------------
            P("Verdance_Road_2", DressingKind.Inscription, "road.milestone", "Road_Milestone", "5.6",
                "Flying-age milestones, their letters cut large and high for eyes above the trees; a walker's chalk low down beside one.");
            P("Verdance_House_2", DressingKind.Tapestry, "cloister.tapestry", "Cloister_Tapestry", null,
                "An undyed wool map of the Verdance across the cloister wall, villages as knots; some knots unpicked, their threads left hanging.",
                "Aldermere released: its knot unpicked, its thread hanging with the others.");
            P("Verdance_Library_1", DressingKind.Trace, "library.dust", null, "5.1",
                "The reading stair's air full of dust that hangs where it is and does not fall; it parts round Wren and closes behind her.");
            P("Verdance_Library_2", DressingKind.Inscription, "library.lectern", "Library_Lectern", "5.1",
                "A reading list pinned to the lectern's post, one title on it; the pin rusted into the wood, the paper white.");
            P("Verdance_Aldermere_1", DressingKind.Trace, "aldermere.bunting", null, null,
                "Bunting across the lane and bread on every sill: the last day dressed as a festival. In the Blank, the same bunting (the festival never ended).");
            P("Verdance_Gate_2", DressingKind.Trace, "gate.ledge", null, "5.6",
                "The gate's landing ledge, worn into grooves by talons; none of the grooves is newer than forty years. The gate's inscription is Gate_Inscription.");

            // ---- Halden Reach ------------------------------------------------------------------------------------------
            P("Halden_Bridges_1", DressingKind.Inscription, "bridges.toll_board", "Bridges_TollBoard", "5.1",
                "The toll board at the first bridge: forty springs of revisions painted one under another, each row the same as the last.");
            P("Halden_Mills_2", DressingKind.Trace, "mills.sheets", "Mills_Sheets", "5.1",
                "Rooms of vellum hung to dry, every sheet with the same thumbprint in the same corner.");
            P("Halden_Hall_2", DressingKind.Inscription, "hall.roll", "Hall_Roll", "5.4",
                "The roll of Guildmasters cut in the Hall's wall: the ninth name chiselled out, its owl's-eye crest left; Voss's name tenth, smooth stone beneath.");
            P("Halden_Hall_1", DressingKind.Inscription, "hall.order", "Hall_Order", "5.5",
                "A standing order pinned inside the Hall's doors, the paper brown, the pins bright.");
            P("Halden_Hall_3", DressingKind.Inscription, "hall.exam_papers", "Hall_ExamPapers", "5.1",
                "Rows of desks, each with the same exam paper; the date at the top gone over in fresh ink.");
            P("Halden_Orchard_1", DressingKind.Trace, "orchard.leaves", null, "5.1",
                "Fallen leaves along the orchard wall, raked into one neat pile, a rake against the wall: the only fallen leaves in Halden.");
            P("Halden_Lowmarket_2", DressingKind.Inscription, "lowmarket.notice", "Lowmarket_Notice", null,
                "The notice board in Lowmarket's square: SURVEY SCHEDULED, pasted over older copies of itself, the lowest gone brown.",
                "Anchored after the strike: a fresh SURVEY COMPLETE pasted over them all.");
            P("Halden_Bridges_3", DressingKind.Trace, "bridges.seventh", null, "5.1",
                "The seventh bridge's scaffolding, the same planks in the same places for forty years, and the paid family standing in their chalked squares.");
            P("Halden_Bastion_1", DressingKind.Inscription, "bastion.plaque", "Bastion_Plaque", "5.6",
                "A brass plaque by a door cut crudely into the flyer-tower's foot; high above, the old landing door, and no stairs between.");
            P("Halden_Bastion_3", DressingKind.Tapestry, "office.drawing", "Office_Drawing", "5.5",
                "A child's crayon drawing in a Guild frame on Voss's wall, dusted: a tall heron with a compass, a city, a small heron waving from a window.");

            // ---- Windreach ---------------------------------------------------------------------------------------------
            P("Windreach_Stones_1", DressingKind.Inscription, "stones.notches", "Stones_Notches", "9.2",
                "The first standing stone: lichen on its north face, walkers' notches down its south face, thousands of them.");
            P("Windreach_Camp_1", DressingKind.Tapestry, "camp.wagon_cloth", "Camp_WagonCloth", "9.2",
                "The route woven on a walking-wagon's canvas: stones, river, gate, fire, and the clan walking at every stop.");
            P("Windreach_River_2", DressingKind.Remains, "river.boats", null, null,
                "Boats on their sides in the cracked mud, names painted on their bows; smudges nest in them (things the river forgot it carried).");
            P("Windreach_Gate_1", DressingKind.Inscription, "gate.lip", "Gate_Lip", null,
                "A ring of flat stones on the Wind Gate's lip, each carved with a place; worn hollow where the young have stood.");

            // ---- the Greyfold ------------------------------------------------------------------------------------------
            P("Greyfold_EdgeCamp_2", DressingKind.Inscription, "edgecamp.beam", "EdgeCamp_Beam", null,
                "Isolde's initials cut in a tent-post of the abandoned outpost, and under them a wren in four strokes.");
            P("Greyfold_EdgeCamp_2", DressingKind.Remains, "edgecamp.tethers", null, null,
                "Coiled tethers on their pegs, never used; one peg empty, its tether run out under the fence into the white and cut.");
            P("Greyfold_Road_2", DressingKind.Inscription, "road.mileposts", "Road_Mileposts", null,
                "Mileposts counting down to the capital, each one paler, the last blank.");
            P("Greyfold_Road_3", DressingKind.Trace, "road.footprints", null, null,
                "A line of footprints in the road's last dust, stopping mid-stride where the road does.");
            P("Greyfold_Threshold_1", DressingKind.Remains, "threshold.old_tether", null, null,
                "Among the Guild's new stakes, one old Ferrymen's tether staked long before, running taut into the white.");

            // ---- the Blank ---------------------------------------------------------------------------------------------
            P("Blank_Capital_1", DressingKind.Inscription, "capital.nameplate", "Capital_Nameplate", null,
                "A Guild office door in the grey capital, its nameplate polished bright at a chick's height.");
            P("Blank_Capital_2", DressingKind.Tapestry, "capital.crayon_floor", null, null,
                "Corra's room: a white floor drawn over in crayon, the same tall heron again and again, each with a compass, none with a face.");
            P("Blank_Hollow_2", DressingKind.Inscription, "hollow.doorframe", "Hollow_Doorframe", null,
                "Height marks knifed into the doorframe of Ilse's house, stopping at six years.");
            Drawings();
        }

        // ---- the fledgling loops -------------------------------------------------------------------------------------

        /// <summary>
        /// A region's fledgling loop (bible 10, "Flight is memory"; art direction; CHR-14 animates it): young birds
        /// leaping in the background of one room, from one perch, gliding a little further for every piece of the sky
        /// Wren has recovered.
        /// </summary>
        public sealed class FledglingLoop
        {
            public Region Region;
            public string Room;
            /// <summary>What they leap from.</summary>
            public string Perch;
            public string Brief;
            /// <summary>The place was anchored before the story began (Halden): its fledglings never get any further.</summary>
            public bool AnchoredFromTheStart;
            /// <summary>Faded birds (the Greyfold's outlines, the Remnant's chicks): a fade doesn't thin them, it made them.</summary>
            public bool Faded;
        }

        /// <summary>What a loop shows now.</summary>
        public struct LoopState
        {
            /// <summary>How many leap.</summary>
            public int Leaping;
            /// <summary>How many of them glide before landing; the rest drop as they always have.</summary>
            public int Gliding;
            /// <summary>The farthest glide, in wingspans: the newest glider's.</summary>
            public float Farthest;
            /// <summary>The true ending's epilogue: one of them doesn't come down.</summary>
            public bool OneFlies;
        }

        /// <summary>One leaper for each piece of the sky there is to recover.</summary>
        public const int Leapers = 6;
        /// <summary>How much further each new glider goes than the one before it, in wingspans.</summary>
        public const float GlideStep = 0.5f;

        public static readonly FledglingLoop[] Loops =
        {
            new FledglingLoop { Region = Region.Saltmarrow, Room = "Saltmarrow_Stilts", Perch = "the top stilt-roost's ledge, into the shallows",
                Brief = "Six young gulls and terns off the highest roost, splashing down in the shallows; a grandmother with bound wings watches from the ladder." },
            new FledglingLoop { Region = Region.Emberdown, Room = "Emberdown_Rest_1", Perch = "the cliff roosts over the gate, into the ash",
                Brief = "Young grouse off the roost doors into drifts of ash, counted aloud by whoever is nearest." },
            new FledglingLoop { Region = Region.Verdance, Room = "Verdance_Grove_3", Perch = "a canopy branch, into the moss far below",
                Brief = "Young doves between the high branches, falling into moss so deep they bounce; nobody sings, nobody stops them." },
            new FledglingLoop { Region = Region.Halden, Room = "Halden_Bridges_2", Perch = "a bridge parapet, onto a net strung under it",
                Brief = "Young pigeons off the toll bridge's parapet into a Crown net: the same leap from the same stone, every afternoon.",
                AnchoredFromTheStart = true },
            new FledglingLoop { Region = Region.Windreach, Room = "Windreach_Camp_1", Perch = "the wagon roofs, into the long grass",
                Brief = "Young cranes off the walking-wagons' roofs into the grass, practising for the Gate, and the clan cheering every landing." },
            new FledglingLoop { Region = Region.Greyfold, Room = "Greyfold_Cathedral_2", Perch = "the Half-Cathedral's broken tower, into the white",
                Brief = "Grey outlines of young birds leaping from the tower, seen only at the edge of the eye; gone when looked at.", Faded = true },
            new FledglingLoop { Region = Region.Blank, Room = "Blank_Hollow_2", Perch = "Thessaly Hollow's roofs, onto the drift",
                Brief = "The Remnant's grey chicks off the village roofs; they glide as the living ones do. The Blank remembers.", Faded = true },
        };

        public static FledglingLoop LoopOf(Region r) => Array.Find(Loops, l => l.Region == r);

        /// <summary>How many pieces of the sky Wren has.</summary>
        public static int SkyPieces(Ability have)
        {
            int n = 0;
            foreach (Ability a in Enum.GetValues(typeof(Ability)))
                if (a != Ability.None && (have & a) == a) n++;
            return n;
        }

        /// <summary>
        /// A loop as it plays: every piece of the sky Wren recovers, one more fledgling glides, a little further than
        /// the last. An anchored place's fledglings leap the same leap forever. A fade thins them: half at stage 2,
        /// none from stage 3. In the Fixed World nobody glides (the Atlas holds the sky); in the Open World they all
        /// do, and one flies.
        /// </summary>
        public static LoopState At(FledglingLoop loop, Ability have, PlaceFate fate = PlaceFate.Unwritten, int fadeStage = 0, Ending ending = Ending.None)
        {
            int leaping = Leapers;
            if (!loop.Faded)
                leaping = fadeStage >= 3 ? 0 : fadeStage == 2 ? Leapers / 2 : Leapers;
            int gliding;
            if (ending == Ending.Open) gliding = leaping;
            else if (ending == Ending.Fixed || loop.AnchoredFromTheStart || fate == PlaceFate.Anchored) gliding = 0;
            else gliding = Math.Min(leaping, SkyPieces(have));
            return new LoopState { Leaping = leaping, Gliding = gliding, Farthest = gliding * GlideStep, OneFlies = ending == Ending.Open && leaping > 0 };
        }
    }
}
