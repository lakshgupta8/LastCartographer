using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>
    /// Flavour text (NAR-17, <c>docs/story/flavour-text.md</c>): the margins of Wren's atlas and the words on what she
    /// carries. One line each, in her hand, for every region and zone on the map, every Charter and Instrument, the
    /// seven keystones, the six abilities, the bound memories and the two purses. The blurbs say what a thing does;
    /// the flavour says what it is. Keyed "flavour.&lt;kind&gt;.&lt;id&gt;" for the translators (NAR-18).
    /// Rules the tests hold: twenty words or fewer, no numerals (numbers are the blurbs'), no modern idiom.
    /// </summary>
    public static class Flavour
    {
        static readonly Dictionary<string, string> _english = new Dictionary<string, string>();

        /// <summary>Every flavour line by key, in English.</summary>
        public static IReadOnlyDictionary<string, string> English => _english;

        public static string Key(string kind, string id) => Keys.Of("flavour." + kind + ".", id);

        static string T(string kind, string id) => _english.TryGetValue(Key(kind, id), out var e) ? Loc.T(Key(kind, id), e) : "";

        public static string ForRegion(Region r) => T("region", r.ToString());
        /// <summary>A zone's margin note by its <see cref="WorldGraph"/> id ("Saltmarrow.Quay").</summary>
        public static string ForZone(string zoneId) => T("zone", zoneId);
        public static string ForCharter(CharterKind k) => T("charter", k.ToString());
        public static string ForInstrument(InstrumentKind k) => k == InstrumentKind.None ? "" : T("instrument", k.ToString());
        /// <summary>An Instrument as Wren has it now: the Sighting lens is Hale's way once he is beaten (boss-kits 6.9).</summary>
        public static string ForInstrument(InstrumentKind k, WorldState w)
            => k == InstrumentKind.SightingLens && w != null && w.Is(Bosses.FlagKey("hale")) ? HalesLens : ForInstrument(k);
        /// <summary>The Sighting lens as Wren holds it after the Nine Stones (Hale's lens, windreach-arc).</summary>
        public static string HalesLens => T("instrument", "HalesLens");
        public static string ForKeystone(string home) => T("keystone", home);
        public static string ForAbility(Ability a) => a == Ability.None ? "" : T("ability", a.ToString());
        public static string ForMemory(string id) => T("memory", id);
        public static string ForCurrency(string id) => T("currency", id);

        public const string IrisSeed = "iris_seed", VellumScrap = "vellum_scrap";

        static void Add(string kind, string id, string english) => _english[Key(kind, id)] = english;

        static Flavour()
        {
            // ---- the regions: the heading of each page ---------------------------------------------------------------
            Add("region", "Saltmarrow", "The map's edge. Paper shows through the reeds here, and the sea goes white before it ends.");
            Add("region", "Emberdown", "Basalt, and ash like slow snow. They count each other aloud every night, and nobody is missed.");
            Add("region", "Verdance", "Trees eighty wingspans tall. Quiet enough to hear the moss. Everyone speaks as if someone is asleep.");
            Add("region", "Halden", "Home. Always late afternoon. Nobody has moved house in forty years; I never thought to ask why.");
            Add("region", "Windreach", "Grass, sky, and a camp that will not stay drawn. They hold it by walking it.");
            Add("region", "Greyfold", "The largest fade. Outlines at the corner of the eye, gone when I look. Colour only near the lantern.");
            Add("region", "Blank", "White. Then colour where I stand. The islands drift; I draw them where they are today.");

            // ---- the Saltmarrow ------------------------------------------------------------------------------------
            Add("zone", "Saltmarrow.Shore", "Where I woke. Wet sand, three torn pages, no map. The tide had my footprints before I stood.");
            Add("zone", "Saltmarrow.Quay", "A Ferrymen port on a town half gone. Everything here has a price, and Sable says it first.");
            Add("zone", "Saltmarrow.Reedmother", "Roots as thick as streets. Crabs in the bole, and something at the crown that watches back.");
            Add("zone", "Saltmarrow.IrisFields", "Pale irises to the horizon: the Ferrymen's purse. Something nests in the middle and dislikes visitors.");
            Add("zone", "Saltmarrow.MerrowsEnd", "Forty roofs once. Nine songs left, and Dotha keeping them. The water listens here.");
            Add("zone", "Saltmarrow.LanternChain", "Seven lighthouses on a string of rock. Three have faded. The fourth still burns for someone.");
            Add("zone", "Saltmarrow.SaltChapel", "A chapel crusted with salt. Halvard waited for me in it, counting paces.");
            Add("zone", "Saltmarrow.BoneBridge", "A whale, faded to its bones, lying across the channel. It sings, if a Ferryman rows you under.");

            // ---- Emberdown -----------------------------------------------------------------------------------------
            Add("zone", "Emberdown.FurnaceStair", "Stairs cut through a cold furnace. The heat comes back in patches, and a Warden keeps it.");
            Add("zone", "Emberdown.KettilsRest", "The holdfast. Six hundred and eleven at supper, counted aloud. Six hundred and twelve with me.");
            Add("zone", "Emberdown.RollCallBell", "The bell rings at dusk and every name is sung. A name nobody answers is sung twice.");
            Add("zone", "Emberdown.NineChimneys", "Nine shafts. Nobody remembers building the ninth. Runa climbs them the old way, talons first.");
            Add("zone", "Emberdown.CinderBaths", "Hot water in cold air. A Guild crane does sums in the steam, and Kettil lets him.");
            Add("zone", "Emberdown.Overlook", "The Greyfold from outside, for the first time. White to the horizon. It looked smaller from within.");
            Add("zone", "Emberdown.Hollowvein", "The mine that fell in. The boards are nailed from the outside. The families still set places.");

            // ---- the Verdance --------------------------------------------------------------------------------------
            Add("zone", "Verdance.OldRoad", "A road into the trees, older than the Guild. The mills along it grind the same flour.");
            Add("zone", "Verdance.QuietHouse", "A monastery grown into one tree's roots. Teodor speaks of the faded as if they were next door.");
            Add("zone", "Verdance.RootChapel", "Lines drawn on the bark in solvent. Read backwards, they are a way up.");
            Add("zone", "Verdance.LanternGrove", "Lanterns hung in the canopy by birds who could reach it. Some are still lit. Nobody climbs to fill them.");
            Add("zone", "Verdance.SunkenLibrary", "An anchored library. One monk, one page, thirty-eight years. The dust has learned to wait.");
            Add("zone", "Verdance.Aldermere", "A village that asked to be let go. I came on its last day.");
            Add("zone", "Verdance.OvergrownGate", "A gate for flyers: a landing ledge and no path. Roots have it now, and something stone.");

            // ---- Halden Reach --------------------------------------------------------------------------------------
            Add("zone", "Halden.SevenBridges", "Seven bridges over the drop. The seventh has been under repair for forty years.");
            Add("zone", "Halden.PaperMills", "Where the Guild's vellum is made. Wet paper in the air, and every sheet the same.");
            Add("zone", "Halden.Lowmarket", "Below the walls. The paint is thinner here. The notice says 'survey scheduled', and has for years.");
            Add("zone", "Halden.JourneymansHall", "My old room, as I left it. So is everything else. Tam sits his exam next spring.");
            Add("zone", "Halden.OldOrchard", "The only place in Halden where leaves fall. Somebody rakes them.");
            Add("zone", "Halden.Bastion", "A tower for birds who flew. No stairs. What the Guild keeps from me is at the top.");
            Add("zone", "Halden.Observatory", "The brass dome. The Great Atlas was made here, and broken here. The dome is shut.");
            Add("zone", "Halden.Vault", "Seven slots for seven stones. I was never meant to count them.");

            // ---- Windreach -----------------------------------------------------------------------------------------
            Add("zone", "Windreach.NineStones", "Standing stones across the grass. A Guild surveyor draws them, and pretends I can't see him.");
            Add("zone", "Windreach.LongGrassCamp", "Wagons, a fire, and soup that has not noticed the move. The camp is never where I left it.");
            Add("zone", "Windreach.DryRiver", "A riverbed with boats in it, and no river. The clan camps here some nights.");
            Add("zone", "Windreach.WindGate", "A lip over nothing. For forty years they sang their fledglings off it. Then me.");
            Add("zone", "Windreach.IdrennesFire", "Idrenne's hearth. The cooking-stone is older than the clan, and so is the joke about it.");
            Add("zone", "Windreach.FallenStar", "Iron from the sky, used as an anvil. The smiths say it hums when nobody is hitting it.");

            // ---- the Greyfold --------------------------------------------------------------------------------------
            Add("zone", "Greyfold.EdgeCamp", "A Guild outpost, left in a hurry. The ledger was never posted. The tethers are still coiled.");
            Add("zone", "Greyfold.HalfCathedral", "Half a cathedral at the edge of the white. The bells ring some nights. Nobody is inside to ring them.");
            Add("zone", "Greyfold.IsoldesLastCamp", "Her camp. Her fire. Her atlas, every page.");
            Add("zone", "Greyfold.RoadThatStops", "A road that stops mid-stride, a step from the white. I stepped in and stayed myself.");
            Add("zone", "Greyfold.MirrorPool", "Still water that shows the bank I am not standing on. Something small looks back.");
            Add("zone", "Greyfold.Threshold", "Where the map goes no further. The Wardens' line, and the Guildmaster at the front of it.");

            // ---- the Blank -----------------------------------------------------------------------------------------
            Add("zone", "Blank.ThessalyHollow", "A village in the white, grey and quiet. I knew the way to the well.");
            Add("zone", "Blank.OldCapital", "The old capital, drawn in the white. A mirror of the Observatory, and someone still working in it.");
            Add("zone", "Blank.AurysLighthouse", "The third lighthouse, faded. Its keeper hums to the lamp. Sable's brother, keeping it still.");

            // ---- Charters: the words on each charter ---------------------------------------------------------------
            Add("charter", "Surveyor", "Issued by the Guild to its journeymen. 'Measure first. Draw what is there.'");
            Add("charter", "Warden", "A Warden's charter with the seal cut out. Someone kept the grip and threw away the oath.");
            Add("charter", "Drifter", "Unsigned. Written in pencil, then gone over in ink, as if the writer took a while to decide.");
            Add("charter", "Ferryman", "Tied to a hook with a Ferryman's knot. 'Everything crosses. Mind the price.'");
            Add("charter", "Unwriter", "Written in solvent, so it reads only at an angle. The Cantors sing it rather than sign it.");
            Add("charter", "Remnant", "Grey ink on grey paper. It is easier to read than it should be.");

            // ---- Instruments ---------------------------------------------------------------------------------------
            Add("instrument", "CompassDart", "Guild issue. The needle finds the nearest thing that moves, which is not always north.");
            Add("instrument", "PlumbWeight", "For true verticals. Dropped from high enough, it answers other questions.");
            Add("instrument", "SightingLens", "The Guild's lens, ground to catch a line across a valley. Close up, it catches blows.");
            Add("instrument", "FieldLantern", "Honest light. The Guild's, before it was Sable's. Hidden things cannot stand it.");
            Add("instrument", "TetherHook", "A Ferryman's hook. The rope is new. The knot is older than the rope.");
            Add("instrument", "IrisTincture", "Pressed from the pale iris. It tastes of the fields, and of whoever lost them.");
            Add("instrument", "WaxSeal", "Guild wax, stamped with a compass rose. Press it anywhere and the page remembers you were there.");
            Add("instrument", "HalesLens", "The same lens. Since the Nine Stones I hold it the way Hale did, and it comes back quicker.");

            // ---- the keystones -------------------------------------------------------------------------------------
            Add("keystone", "aury", "Taken from a faded keeper's wings. It hums, as he did.");
            Add("keystone", "hollowvein", "From the bottom of Hollowvein. Still warm. Kettil kept it buried because it broke the mine.");
            Add("keystone", "quiet_house", "Teodor carried it eleven years. It is heavier than it looks. He told me so.");
            Add("keystone", "windreach", "Idrenne's cooking-stone, forty years of soup in its grain. She laughed when she gave it up.");
            Add("keystone", "isolde", "The Vault's sixth. Isolde carried it into the white. I carry it out.");
            Add("keystone", "archivist", "The seventh, from Corvin's talons. He held it forty-one years. My hand is smaller.");
            Add("keystone", "observatory", "The one stone that never left the frame. The Observatory's own.");

            // ---- abilities: what came back -------------------------------------------------------------------------
            Add("ability", "Wingbeat", "One beat, over a gap I could not have jumped. My shoulders knew it before I did.");
            Add("ability", "Talonhold", "Runa showed me once. My feet had known how all along.");
            Add("ability", "Inkthread", "Teodor's solvent-lines, drawn the other way. The line pulls, and so do I.");
            Add("ability", "Windmemory", "The Wind Gate. I leapt, and the air remembered me before I remembered it.");
            Add("ability", "Clarity", "I stepped into the white and stayed myself. The lantern draws a little of the world around me.");
            Add("ability", "Sky", "Not a beat, not a glide. Up.");

            // ---- bound memories and the purses ---------------------------------------------------------------------
            Add("memory", "isolde.first_sight", "Isolde's, bound at the edge: the first time she saw me. It is warmer than my own.");
            Add("memory", "dotha.nine_songs", "Dotha's songs, weather first. Everything true is short.");
            Add("memory", "sable.boats_back", "Sable's count of the boats that came back. She never says the other number.");
            Add("currency", IrisSeed, "The coast's small change. Everyone takes it; nobody knows who planted the first field.");
            Add("currency", VellumScrap, "Offcuts of good vellum. The Guild counts every sheet; these are the ones it lost.");
        }
    }
}
