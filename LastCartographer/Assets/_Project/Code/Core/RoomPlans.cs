using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    public enum Side { West, East, Up, Down }

    /// <summary>A way out of a planned room: the side, where it leads (a room id, or a zone id "Region.Zone" outside the plan), and its gate.</summary>
    public sealed class RoomExit
    {
        public Side Side;
        public string To;
        public Ability Needs;
        public string Flag;
        public bool Soft;
        public bool IsExternal => RoomPlans.IsZoneId(To);
    }

    /// <summary>One room as designed on paper (DES-09): what it is for, what stands in it, and its exits. Built later from a recipe.</summary>
    public sealed class RoomPlan
    {
        public string Id;
        public string Zone;
        public string Name;
        public string Purpose;
        /// <summary>The vantage's short name ("Bell"); its atlas id is "&lt;Id&gt;/&lt;Vantage&gt;". Null if none.</summary>
        public string Vantage;
        public string Enemies;
        /// <summary>Cast ids (<see cref="Cast"/>) who stand in this room.</summary>
        public string[] Npcs;
        public bool Desk;
        /// <summary>A boss sheet id (<see cref="Bosses"/>) fought here, or null.</summary>
        public string Arena;
        /// <summary>A bounds-walk id that runs through this room, or null.</summary>
        public string Walk;
        public readonly List<RoomExit> Exits = new List<RoomExit>();

        public string VantageId => Vantage == null ? null : Id + "/" + Vantage;

        RoomPlan Add(Side side, string to, Ability needs, string flag, bool soft)
        {
            Exits.Add(new RoomExit { Side = side, To = to, Needs = needs, Flag = flag, Soft = soft });
            return this;
        }
        public RoomPlan West(string to, Ability needs = Ability.None, string flag = null, bool soft = false) => Add(Side.West, to, needs, flag, soft);
        public RoomPlan East(string to, Ability needs = Ability.None, string flag = null, bool soft = false) => Add(Side.East, to, needs, flag, soft);
        public RoomPlan Up(string to, Ability needs = Ability.None, string flag = null, bool soft = false) => Add(Side.Up, to, needs, flag, soft);
        public RoomPlan Down(string to, Ability needs = Ability.None, string flag = null, bool soft = false) => Add(Side.Down, to, needs, flag, soft);
    }

    /// <summary>
    /// Room-by-room plans for the regions not yet built (DES-09 Emberdown and the Verdance, DES-10 Halden Reach, DES-11 Windreach,
    /// the Greyfold and the Blank's fixed islands), as data so the spec
    /// (`docs/design/emberdown-verdance-rooms.md`, generated from the same source) can be checked against the macro map
    /// (<see cref="WorldGraph"/>), the boss sheets and the cast. Pure data.
    /// </summary>
    public static class RoomPlans
    {
        static readonly List<RoomPlan> _all = new List<RoomPlan>();
        static bool _built;

        public static IReadOnlyList<RoomPlan> All { get { EnsureDefaults(); return _all; } }
        public static RoomPlan Find(string id) { EnsureDefaults(); return _all.Find(r => r.Id == id); }
        public static List<RoomPlan> InZone(string zone) { EnsureDefaults(); return _all.FindAll(r => r.Zone == zone); }
        public static List<RoomPlan> InRegion(Region region) { EnsureDefaults(); var p = region + "."; return _all.FindAll(r => r.Zone.StartsWith(p)); }

        public static bool IsZoneId(string s) => !string.IsNullOrEmpty(s) && s.IndexOf('.') > 0;
        public static Side Opposite(Side s) => s == Side.West ? Side.East : s == Side.East ? Side.West : s == Side.Up ? Side.Down : Side.Up;

        /// <summary>The zone a target belongs to: itself for a zone id, the room's zone for a planned room.</summary>
        public static string ZoneOf(string target) => IsZoneId(target) ? target : Find(target)?.Zone;

        public static bool Passable(RoomExit e, Ability have, Func<string, bool> hasFlag, bool allowSoft)
        {
            if (e.Flag != null && (hasFlag == null || !hasFlag(e.Flag))) return false;
            if (e.Needs == Ability.None || (have & e.Needs) == e.Needs) return true;
            return allowSoft && e.Soft;
        }

        /// <summary>
        /// The planned rooms a kit can reach, entering from every outside zone the macro map says is reachable with it.
        /// The room graph and the zone graph should agree on which zones this touches.
        /// </summary>
        public static HashSet<string> ReachableRooms(Ability have, Func<string, bool> hasFlag = null, bool allowSoft = false)
        {
            EnsureDefaults();
            var zones = WorldGraph.Reachable(have, hasFlag, allowSoft);
            var seen = new HashSet<string>();
            var open = new Queue<string>();
            foreach (var r in _all)
                foreach (var e in r.Exits)
                    if (e.IsExternal && zones.Contains(e.To) && Passable(e, have, hasFlag, allowSoft) && seen.Add(r.Id)) open.Enqueue(r.Id);
            while (open.Count > 0)
            {
                var r = Find(open.Dequeue());
                foreach (var e in r.Exits)
                {
                    if (e.IsExternal || !Passable(e, have, hasFlag, allowSoft)) continue;
                    if (seen.Add(e.To)) open.Enqueue(e.To);
                }
            }
            return seen;
        }

        public static void Reset() { _all.Clear(); _built = false; }

        static RoomPlan R(string id, string zone, string name, string purpose, string vantage, string enemies, string[] npcs, bool desk, string arena, string walk)
        {
            var r = new RoomPlan { Id = id, Zone = zone, Name = name, Purpose = purpose, Vantage = vantage, Enemies = enemies, Npcs = npcs, Desk = desk, Arena = arena, Walk = walk };
            _all.Add(r);
            return r;
        }

        // Generated from the DES-09 source with the spec's tables; edit both together.
        public static void EnsureDefaults()
        {
            if (_built) return;
            _built = true;
            R("Emberdown_Stair_1", "Emberdown.FurnaceStair", "The foot of the stair", "The climb from the Bone Bridge ends in black basalt; the first ash falls. A Wingbeat gap is the way in (soft: a pogo off the bat crosses it).", null, "cave-bat, salamander ×2", new string[0], false, null, null)
                .West("Saltmarrow.BoneBridge", Ability.Wingbeat, null, true)
                .Up("Emberdown_Stair_2", Ability.None);
            R("Emberdown_Stair_2", "Emberdown.FurnaceStair", "The furnace landings", "A vertical room of landings over live furnaces: the Furnace Stair Rescue set piece. The first vantage looks back at the coast.", "Landing", "cave-bat ×2, salamander", new string[0], false, null, null)
                .Down("Emberdown_Stair_1", Ability.None)
                .Up("Emberdown_Stair_3", Ability.None);
            R("Emberdown_Stair_3", "Emberdown.FurnaceStair", "The cold furnace", "A desk on the landing, then Cinder Warden Brann's arena: a furnace cooling in sections (6.5).", null, "", new string[0], true, "brann", null)
                .Down("Emberdown_Stair_2", Ability.None)
                .East("Emberdown_Rest_1", Ability.None);
            R("Emberdown_Rest_1", "Emberdown.KettilsRest", "The gate", "Kettil counts every arrival out loud; a stranger is a problem. Ash on the roofs, roosts cut in the cliff.", null, "", new[] { "kettil" }, false, null, null)
                .West("Emberdown_Stair_3", Ability.None)
                .East("Emberdown_Rest_2", Ability.None);
            R("Emberdown_Rest_2", "Emberdown.KettilsRest", "The square", "The hub: desk, ledger, the smith's shop, Kettil's porch. The bell stair climbs from its east end.", "Square", "", new[] { "kettil" }, true, null, null)
                .West("Emberdown_Rest_1", Ability.None)
                .East("Emberdown_Rest_3", Ability.None)
                .Up("Emberdown_Bell_1", Ability.None);
            R("Emberdown_Rest_3", "Emberdown.KettilsRest", "The pit-head", "The mine mouth, boarded; the chimneys start at the east wall. Hollowvein opens here once the families agree.", null, "", new[] { "runa" }, false, null, null)
                .West("Emberdown_Rest_2", Ability.None)
                .East("Emberdown_Chimneys_1", Ability.None)
                .Down("Emberdown_Hollow_1", Ability.Talonhold, "emberdown.hollowvein_opened");
            R("Emberdown_Bell_1", "Emberdown.RollCallBell", "The bell stair", "Up from the square to the bell; bats roost in the stair.", null, "cave-bat ×2", new string[0], false, null, null)
                .Down("Emberdown_Rest_2", Ability.None)
                .East("Emberdown_Bell_2", Ability.None);
            R("Emberdown_Bell_2", "Emberdown.RollCallBell", "The bell", "The nightly roll-call: Runa counts Wren in, and teaches the walk here (the lesson walk, bounds-walk.md).", "Bell", "", new[] { "runa", "kettil" }, false, null, "kettils_rest")
                .West("Emberdown_Bell_1", Ability.None);
            R("Emberdown_Chimneys_1", "Emberdown.NineChimneys", "The first chimney", "Runa climbs the old way and Wren learns Talonhold at its foot; the shaft above needs it.", null, "salamander", new[] { "runa" }, false, null, null)
                .West("Emberdown_Rest_3", Ability.None)
                .Up("Emberdown_Chimneys_2", Ability.Talonhold);
            R("Emberdown_Chimneys_2", "Emberdown.NineChimneys", "The shafts", "Wall to wall up three chimneys at once; salamanders on the ledges.", "Shaft", "salamander ×2, cave-bat", new string[0], false, null, null)
                .Down("Emberdown_Chimneys_1", Ability.Talonhold)
                .Up("Emberdown_Chimneys_3", Ability.Talonhold);
            R("Emberdown_Chimneys_3", "Emberdown.NineChimneys", "The ninth chimney", "Nobody remembers building it. A Guild agent lives at the top (the Ninth Chimney commission).", "Ninth", "cave-bat", new string[0], true, null, null)
                .Down("Emberdown_Chimneys_2", Ability.Talonhold)
                .East("Emberdown_Chimneys_4", Ability.None);
            R("Emberdown_Chimneys_4", "Emberdown.NineChimneys", "The flue road", "A tunnel of old flues east toward the baths; the heat rises through the floor.", null, "salamander ×2, pulp-wasp", new string[0], false, null, null)
                .West("Emberdown_Chimneys_3", Ability.None)
                .East("Emberdown_Baths_1", Ability.Talonhold);
            R("Emberdown_Baths_1", "Emberdown.CinderBaths", "The steam walk", "Boardwalks over hot pools; steam hides the next hold.", null, "salamander, smudge", new string[0], false, null, null)
                .West("Emberdown_Chimneys_4", Ability.Talonhold)
                .East("Emberdown_Baths_2", Ability.None);
            R("Emberdown_Baths_2", "Emberdown.CinderBaths", "The baths", "The Cinder Bath Debate: Kettil and a Guild surveyor argue in real numbers, and Runa sings them back.", "Baths", "", new[] { "kettil", "runa" }, false, null, null)
                .West("Emberdown_Baths_1", Ability.None)
                .East("Emberdown_Baths_3", Ability.None);
            R("Emberdown_Baths_3", "Emberdown.CinderBaths", "The vents", "Vents that breathe on a rhythm; the climb out to the ridge is a wall.", null, "cave-bat ×2", new string[0], false, null, null)
                .West("Emberdown_Baths_2", Ability.None)
                .East("Emberdown_Overlook_1", Ability.Talonhold);
            R("Emberdown_Overlook_1", "Emberdown.Overlook", "The ridge", "The highland's edge; the wind turns cold and the ash stops.", null, "Warden (patrol, the road)", new string[0], false, null, null)
                .West("Emberdown_Baths_3", Ability.Talonhold)
                .East("Emberdown_Overlook_2", Ability.None);
            R("Emberdown_Overlook_2", "Emberdown.Overlook", "The overlook", "First sight of the Greyfold from outside: bigger than it looks. The road down to the Plateau's bridges.", "Overlook", "", new[] { "runa" }, true, null, null)
                .West("Emberdown_Overlook_1", Ability.None)
                .East("Halden_Bridges_1", Ability.Talonhold);
            R("Emberdown_Hollow_1", "Emberdown.Hollowvein", "The adit", "Down from the pit-head behind the boards; the long roll-call starts at its first beam: the first verse.", null, "", new[] { "runa" }, false, null, "hollowvein")
                .Up("Emberdown_Rest_3", Ability.Talonhold, "emberdown.hollowvein_opened")
                .Down("Emberdown_Hollow_2", Ability.None);
            R("Emberdown_Hollow_2", "Emberdown.Hollowvein", "The first gallery", "Lamps on the walls, one for each name; the walk's second verse.", "Gallery", "smudge ×2", new[] { "runa" }, false, null, "hollowvein")
                .Up("Emberdown_Hollow_1", Ability.None)
                .Down("Emberdown_Hollow_3", Ability.None);
            R("Emberdown_Hollow_3", "Emberdown.Hollowvein", "The flooded gallery", "Black water to the knee; a desk the miners left, still dry; the third verse.", null, "smudge", new[] { "runa" }, true, null, "hollowvein")
                .Up("Emberdown_Hollow_2", Ability.None)
                .Down("Emberdown_Hollow_4", Ability.None);
            R("Emberdown_Hollow_4", "Emberdown.Hollowvein", "The bottom", "The collapse itself: the Collapse is the fourth verse, and wakes once the three above are walked (6.4). The keystone is under it.", null, "", new[] { "runa" }, false, "collapse", "hollowvein")
                .Up("Emberdown_Hollow_3", Ability.None);
            R("Verdance_Road_1", "Verdance.OldRoad", "The iris gap", "From the Pale Iris Fields over a Wingbeat gap (soft) onto a road the forest has half taken.", null, "crab, skimmer, reedling ×3", new string[0], false, null, null)
                .West("Saltmarrow.IrisFields", Ability.Wingbeat, null, true)
                .East("Verdance_Road_2", Ability.None);
            R("Verdance_Road_2", "Verdance.OldRoad", "The milestones", "Flying-age milestones, every one giving the distance to a place that is gone.", "Milestone", "smudge, crab, pulp-wasp", new string[0], false, null, null)
                .West("Verdance_Road_1", Ability.None)
                .East("Verdance_Road_3", Ability.None);
            R("Verdance_Road_3", "Verdance.OldRoad", "The first trees", "The trees begin: trunks eighty wingspans tall, light in shafts, near silence.", null, "Cantor", new string[0], false, null, null)
                .West("Verdance_Road_2", Ability.None)
                .East("Verdance_House_1", Ability.None);
            R("Verdance_House_1", "Verdance.QuietHouse", "The roots gate", "The Quiet House's door in the roots of one tree; the brothers bow and do not speak.", null, "", new string[0], false, null, null)
                .West("Verdance_Road_3", Ability.None)
                .East("Verdance_House_2", Ability.None);
            R("Verdance_House_2", "Verdance.QuietHouse", "The cloister", "The hub: desk, ledger, Teodor. The root stair goes down from its floor.", "Cloister", "", new[] { "teodor" }, true, null, null)
                .West("Verdance_House_1", Ability.None)
                .East("Verdance_House_3", Ability.None)
                .Down("Verdance_Chapel_1", Ability.None);
            R("Verdance_House_3", "Verdance.QuietHouse", "The east door", "The brothers' garden and the lane to Aldermere; ash on the path.", null, "", new string[0], false, null, null)
                .West("Verdance_House_2", Ability.None)
                .East("Verdance_Aldermere_1", Ability.None);
            R("Verdance_Chapel_1", "Verdance.RootChapel", "The root stair", "Down through the roots; lanterns hung from them.", null, "smudge, moths", new string[0], false, null, null)
                .Up("Verdance_House_2", Ability.None)
                .Down("Verdance_Chapel_2", Ability.None);
            R("Verdance_Chapel_2", "Verdance.RootChapel", "The root chapel", "Teodor teaches Inkthread: the solvent-line reversed. The grove is across a gap only a thread crosses.", "Chapel", "", new[] { "teodor" }, false, null, null)
                .Up("Verdance_Chapel_1", Ability.None)
                .East("Verdance_Grove_1", Ability.Inkthread);
            R("Verdance_Grove_1", "Verdance.LanternGrove", "The grove edge", "Anchor-points in the branches; the first thread gauntlet.", null, "skimmer ×2", new string[0], false, null, null)
                .West("Verdance_Chapel_2", Ability.Inkthread)
                .East("Verdance_Grove_2", Ability.None);
            R("Verdance_Grove_2", "Verdance.LanternGrove", "The lanterns", "Eleven lanterns in a ring: the vigil with Teodor. No choices.", "Lanterns", "", new[] { "teodor" }, true, null, null)
                .West("Verdance_Grove_1", Ability.None)
                .Up("Verdance_Grove_3", Ability.None);
            R("Verdance_Grove_3", "Verdance.LanternGrove", "The canopy", "Up into the canopy by thread; the forest floor out of sight below.", "Canopy", "Cantor, skimmer, moths", new string[0], false, null, null)
                .Down("Verdance_Grove_2", Ability.None)
                .East("Verdance_Grove_4", Ability.None);
            R("Verdance_Grove_4", "Verdance.LanternGrove", "The high lanterns", "The grove's crown; a thread line east drops to the library's roof.", null, "smudge, moths ×2", new string[0], false, null, null)
                .West("Verdance_Grove_3", Ability.None)
                .East("Verdance_Library_1", Ability.Inkthread);
            R("Verdance_Library_1", "Verdance.SunkenLibrary", "The reading stair", "Down into a library the forest floor swallowed; anchored, and it shows: the dust does not move.", null, "", new string[0], false, null, null)
                .West("Verdance_Grove_4", Ability.Inkthread)
                .Down("Verdance_Library_2", Ability.None);
            R("Verdance_Library_2", "Verdance.SunkenLibrary", "The reading room", "Brother Ansel on page 214 for thirty-eight years. Teodor will not turn it for you.", "Page", "", new[] { "teodor" }, false, null, null)
                .Up("Verdance_Library_1", Ability.None);
            R("Verdance_Aldermere_1", "Verdance.Aldermere", "The lane", "Aldermere on its last day: bunting, bread, a desk the inn keeps for travellers.", null, "", new string[0], true, null, null)
                .West("Verdance_House_3", Ability.None)
                .East("Verdance_Aldermere_2", Ability.None);
            R("Verdance_Aldermere_2", "Verdance.Aldermere", "The square", "The last evening. Attend it, or try to stop it and the Choir sings over the square (6.6).", "Square", "", new[] { "teodor" }, false, "choir", null)
                .West("Verdance_Aldermere_1", Ability.None)
                .East("Verdance_Aldermere_3", Ability.None);
            R("Verdance_Aldermere_3", "Verdance.Aldermere", "The ash field", "Where the village is already paper; the canopy road starts over it by thread. Attended, the village stands here as paper and Teodor sits with them.", null, "Cantor, smudge, pulp-wasp", new[] { "teodor" }, false, null, null)
                .West("Verdance_Aldermere_2", Ability.None)
                .East("Verdance_Gate_1", Ability.Inkthread);
            R("Verdance_Gate_1", "Verdance.OvergrownGate", "The approach", "A desk under the roots, then the gate's roots as anchors up the wall.", null, "skimmer", new string[0], true, null, null)
                .West("Verdance_Aldermere_3", Ability.Inkthread)
                .East("Verdance_Gate_2", Ability.None);
            R("Verdance_Gate_2", "Verdance.OvergrownGate", "The Overgrown Gate", "The Gatekeeper's arena (6.7); beyond it, the canopy road to the Paper Mills.", "Gate", "", new string[0], false, "gatekeeper", null)
                .West("Verdance_Gate_1", Ability.None)
                .East("Halden_Mills_1", Ability.Inkthread);
            R("Halden_Bridges_1", "Halden.SevenBridges", "The first bridge", "Off the Overlook road onto the Plateau: stone, copper gone green, always late afternoon. A toll-keeper counts coins, not birds.", null, "Warden ×2", new string[0], false, null, null)
                .West("Emberdown_Overlook_2", Ability.Talonhold)
                .East("Halden_Bridges_2", Ability.None);
            R("Halden_Bridges_2", "Halden.SevenBridges", "The toll bridges", "Three bridges over the drop, tolled; the stair down to Lowmarket goes from the second.", "Tollhouse", "Warden, Cantor", new string[0], false, null, null)
                .West("Halden_Bridges_1", Ability.None)
                .East("Halden_Bridges_3", Ability.None)
                .Down("Halden_Lowmarket_1", Ability.None);
            R("Halden_Bridges_3", "Halden.SevenBridges", "The seventh bridge", "Under repair for forty years; a family is paid to stand on it (the Seventh Bridge). A desk in the repair hut.", "Seventh", "", new string[0], true, null, null)
                .West("Halden_Bridges_2", Ability.None)
                .East("Halden_Bridges_4", Ability.None);
            R("Halden_Bridges_4", "Halden.SevenBridges", "The last span", "Halvard's second hunt (6.3): he cuts the span section by section. The mills are below it.", null, "", new[] { "halvard" }, false, "halvard_2", null)
                .West("Halden_Bridges_3", Ability.None)
                .Down("Halden_Mills_2", Ability.None);
            R("Halden_Mills_1", "Halden.PaperMills", "The mill race", "Where the canopy road from the Overgrown Gate comes down: a mill race, wheels, wet paper in the air.", null, "Warden, smudge, pulp-wasp", new string[0], false, null, null)
                .West("Verdance_Gate_2", Ability.Inkthread)
                .East("Halden_Mills_2", Ability.None);
            R("Halden_Mills_2", "Halden.PaperMills", "The drying lofts", "Sheets of new vellum hung to dry, rooms deep; the Seven Bridges are overhead.", "Lofts", "smudge ×2, pulp-wasp ×2", new string[0], false, null, null)
                .Up("Halden_Bridges_4", Ability.None)
                .West("Halden_Mills_1", Ability.None)
                .East("Halden_Mills_3", Ability.None);
            R("Halden_Mills_3", "Halden.PaperMills", "The pulp yard", "The strike's picket line: the millworkers of Lowmarket have downed tools. The Hall steps are beyond.", null, "", new string[0], false, null, null)
                .West("Halden_Mills_2", Ability.None)
                .East("Halden_Hall_1", Ability.None);
            R("Halden_Lowmarket_1", "Halden.Lowmarket", "The stair down", "Below the walls. The paint is thinner here, and so is everything else.", null, "smudge, Sketch", new string[0], false, null, null)
                .Up("Halden_Bridges_2", Ability.None)
                .East("Halden_Lowmarket_2", Ability.None);
            R("Halden_Lowmarket_2", "Halden.Lowmarket", "Lowmarket", "The district below the walls, fading; its notice board reads 'survey scheduled'. The strike hall, where the decision is made.", "Market", "", new string[0], true, null, null)
                .West("Halden_Lowmarket_1", Ability.None)
                .East("Halden_Lowmarket_3", Ability.None);
            R("Halden_Lowmarket_3", "Halden.Lowmarket", "The south gate", "The south road to Windreach, barred until Act 2 opens it.", null, "Warden", new string[0], false, null, null)
                .West("Halden_Lowmarket_2", Ability.None)
                .East("Windreach_Stones_1", Ability.None, "act2.started");
            R("Halden_Hall_1", "Halden.JourneymansHall", "The Hall steps", "The Guild's steps. Unlicensed now, she comes in past the Wardens or not at all until Interlude A resolves.", null, "Warden ×2", new string[0], false, null, null)
                .West("Halden_Mills_3", Ability.None)
                .East("Halden_Hall_2", Ability.None);
            R("Halden_Hall_2", "Halden.JourneymansHall", "The Journeyman's Hall", "The hub: desk, ledger, Wren's old room. Pell. Tam, who sits his exam next spring, eleven years running.", "Hall", "", new[] { "pell" }, true, null, null)
                .West("Halden_Hall_1", Ability.None)
                .East("Halden_Hall_3", Ability.None);
            R("Halden_Hall_3", "Halden.JourneymansHall", "The exam rooms", "Rows of desks with the same papers on them (the Master's Exam, plant 5.1). The orchard door at the end.", null, "", new string[0], false, null, null)
                .West("Halden_Hall_2", Ability.None)
                .East("Halden_Orchard_1", Ability.None);
            R("Halden_Orchard_1", "Halden.OldOrchard", "The orchard wall", "The only place in Halden with fallen leaves. Somebody rakes them.", null, "", new string[0], false, null, null)
                .West("Halden_Hall_3", Ability.None)
                .East("Halden_Orchard_2", Ability.None);
            R("Halden_Orchard_2", "Halden.OldOrchard", "The Old Orchard", "Isolde's cache in the roots; the Orchard Keeper; a gravestone with a crest on it. The flyer-tower rises from its wall; the road to the Edge begins here.", "Leaves", "", new[] { "isolde" }, false, null, null)
                .West("Halden_Orchard_1", Ability.None)
                .East("Greyfold_EdgeCamp_1", Ability.None, "isolde.cache")
                .Up("Halden_Bastion_1", Ability.Talonhold | Ability.Inkthread);
            R("Halden_Bastion_1", "Halden.Bastion", "The flyer-tower", "A tower built for flyers: no stairs. Talonhold up the walls, Inkthread across the gaps. A desk on the top landing; the Crown's hall behind it (Interlude B).", null, "Warden", new[] { "maren" }, true, null, null)
                .Down("Halden_Orchard_2", Ability.Talonhold | Ability.Inkthread)
                .Up("Halden_Bastion_2", Ability.None);
            R("Halden_Bastion_2", "Halden.Bastion", "The drill-yard", "Oriel's arena if Pell's report was sent (6.8); otherwise an empty yard with chalk lines.", "Yard", "", new string[0], false, "oriel", null)
                .Down("Halden_Bastion_1", Ability.None)
                .East("Halden_Bastion_3", Ability.None);
            R("Halden_Bastion_3", "Halden.Bastion", "The Guildmaster's window", "The tower's top window opens into Voss's office in the Observatory wing: the chick's drawing, framed (plant 5.5). The dome itself is shut.", null, "", new[] { "pell" }, false, null, null)
                .West("Halden_Bastion_2", Ability.None)
                .East("Halden_Observatory_1", Ability.None, "act3.started")
                .Down("Halden_Vault_1", Ability.None, "halden.vault_opened");
            R("Halden_Observatory_1", "Halden.Observatory", "The dome stair", "Act 3: the dome opens. A desk under the stair.", null, "Warden ×2", new string[0], true, null, null)
                .West("Halden_Bastion_3", Ability.None, "act3.started")
                .East("Halden_Observatory_2", Ability.None);
            R("Halden_Observatory_2", "Halden.Observatory", "The frame", "The frame of the shattered Atlas; the keystone in it; the ending's choice and, in the true ending, the Complete Survey (6.15).", "Frame", "", new[] { "pell", "voss", "runa", "teodor" }, false, "complete_survey", null)
                .West("Halden_Observatory_1", Ability.None);
            R("Halden_Vault_1", "Halden.Vault", "The Vault", "Seven slots, reached from the Guildmaster's window; one empty (Pell counts them, plant 5.2).", null, "", new[] { "pell" }, false, null, null)
                .Up("Halden_Bastion_3", Ability.None, "halden.vault_opened");
            R("Windreach_Stones_1", "Windreach.NineStones", "The south road's end", "Out of Lowmarket's south gate onto the Steppe: grass to the horizon, sky most of the screen. The first standing stone, lichen on its north face.", "Waymark", "Warden (out of uniform: Hale's escort), tussock", new string[0], false, null, null)
                .West("Halden_Lowmarket_3", Ability.None, "act2.started")
                .East("Windreach_Stones_2", Ability.None);
            R("Windreach_Stones_2", "Windreach.NineStones", "The long walk", "Stones two to eight in the line the clans have walked since before the Guild; the stones are a map (the Nine Stones). Ink-swirl updrafts, too weak to ride yet.", "Fifth", "smudge ×2, moths, tussock", new string[0], false, null, null)
                .West("Windreach_Stones_1", Ability.None)
                .East("Windreach_Stones_3", Ability.None);
            R("Windreach_Stones_3", "Windreach.NineStones", "The ninth stone", "Where the route turns north. Surveyor Hale at dusk, sighting the stones one by one (6.9, optional); the camp's wagons are just east.", null, "", new[] { "hale" }, false, "hale", null)
                .West("Windreach_Stones_2", Ability.None)
                .East("Windreach_Camp_1", Ability.None);
            R("Windreach_Camp_1", "Windreach.LongGrassCamp", "The wagons", "The walking-wagons in a ring. One stays at the Long Grass wherever the camp has gone: the walkers' post, with the desk and the ledger.", "Wagons", "", new string[0], true, null, null)
                .West("Windreach_Stones_3", Ability.None)
                .East("Windreach_Camp_2", Ability.None);
            R("Windreach_Camp_2", "Windreach.LongGrassCamp", "The fire ring", "The camp's first night (the Moving Camp). Idrenne tells where she was standing when she learned each thing. The camp moves on to the riverbed, then the high grass.", null, "", new[] { "idrenne" }, false, null, null)
                .West("Windreach_Camp_1", Ability.None)
                .East("Windreach_River_1", Ability.Wingbeat, null, true);
            R("Windreach_River_1", "Windreach.DryRiver", "The far bank", "A river with no water, a Wingbeat wide at the camp's edge (soft: a pogo off the dead reed-heads crosses it).", null, "smudge, tussock", new string[0], false, null, null)
                .West("Windreach_Camp_2", Ability.Wingbeat, null, true)
                .East("Windreach_River_2", Ability.None);
            R("Windreach_River_2", "Windreach.DryRiver", "The riverbed", "Cracked mud, boats on their sides; the camp's second night pitches here. Smudges in the boats: things the river forgot it carried. Sable berths here when she rows Wren north (Boat).", "Bed", "smudge ×2, moths", new[] { "idrenne", "sable" }, false, null, null)
                .West("Windreach_River_1", Ability.None)
                .East("Windreach_River_3", Ability.None);
            R("Windreach_River_3", "Windreach.DryRiver", "The cut bank", "The river's old cliff. The Wind Gate is at the top, and only Talonhold climbs it.", null, "smudge", new string[0], false, null, null)
                .West("Windreach_River_2", Ability.None)
                .Up("Windreach_Gate_1", Ability.Talonhold);
            R("Windreach_Gate_1", "Windreach.WindGate", "The leap", "The fledgling-leap on the cliff's lip: the clan sings, the young jump. Nobody has lived through it in forty years until Wren. Windmemory.", "Gate", "", new[] { "idrenne" }, false, null, null)
                .Down("Windreach_River_3", Ability.Talonhold)
                .East("Windreach_Gate_2", Ability.Windmemory);
            R("Windreach_Gate_2", "Windreach.WindGate", "The updrafts", "Ink-swirls to ride: the first glide course. North, the high grass; down the far side, a long glide into the Greyfold's white at the Mirror Pool.", null, "", new string[0], false, null, null)
                .West("Windreach_Gate_1", Ability.Windmemory)
                .East("Windreach_Fire_1", Ability.Windmemory)
                .Down("Greyfold_Pool_1", Ability.Windmemory);
            R("Windreach_Fire_1", "Windreach.IdrennesFire", "The high grass", "Grass over Wren's head; the camp's third night pitches here, and the clan walks her in.", null, "", new[] { "idrenne" }, false, null, null)
                .West("Windreach_Gate_2", Ability.Windmemory)
                .East("Windreach_Fire_2", Ability.None);
            R("Windreach_Fire_2", "Windreach.IdrennesFire", "Idrenne's Fire", "The hearth; the keystone is its cooking-stone. Idrenne says plainly how the clans do it (plant 9.2) and gives it up laughing. Surveying Windreach at all is decided here.", "Hearth", "", new[] { "idrenne" }, false, null, null)
                .West("Windreach_Fire_1", Ability.None)
                .Down("Windreach_Star_1", Ability.Windmemory);
            R("Windreach_Star_1", "Windreach.FallenStar", "The crater rim", "A glide down from the hearth to the rim; the smiths' wagon keeps a desk.", "Rim", "", new[] { "idrenne" }, true, null, null)
                .Up("Windreach_Fire_2", Ability.Windmemory)
                .East("Windreach_Star_2", Ability.None);
            R("Windreach_Star_2", "Windreach.FallenStar", "The anvil-crater", "The Fallen Star, forty years the clans' anvil, the hearth's heat run into its iron. Lift the stone at the Fire and it wakes (6.10, optional).", null, "", new string[0], false, "fallen_star", null)
                .West("Windreach_Star_1", Ability.None);
            R("Greyfold_EdgeCamp_1", "Greyfold.EdgeCamp", "The orchard road's end", "The road from the Old Orchard stops at a Guild fence with no gate. Beyond it the paper is white; buildings show only at the edge of the eye.", null, "", new string[0], false, null, null)
                .West("Halden_Orchard_2", Ability.None, "isolde.cache")
                .East("Greyfold_EdgeCamp_2", Ability.None);
            R("Greyfold_EdgeCamp_2", "Greyfold.EdgeCamp", "The Edge Camp", "The abandoned Guild outpost: tether-posts, a ledger nobody posts to, Isolde's initials cut in a beam. The hub; the last place colour reaches by itself.", "Outpost", "", new string[0], true, null, null)
                .West("Greyfold_EdgeCamp_1", Ability.None)
                .East("Greyfold_Edge", Ability.None);
            R("Greyfold_Edge", "Greyfold.HalfCathedral", "The Edge", "The prologue's room, built (the greybox `Greyfold_Edge`): Isolde's desk; survey, bind, seal; then she walks in. Act 1 ends here too.", "HalfCathedral", "", new[] { "isolde" }, true, null, null)
                .West("Greyfold_EdgeCamp_2", Ability.None)
                .East("Greyfold_Cathedral_2", Ability.None);
            R("Greyfold_Cathedral_2", "Greyfold.HalfCathedral", "The nave", "Half a cathedral, white; the Road That Stops runs down its nave. Thirty steps in, a grey chick. With Clarity, the bells ring (6.12).", null, "lost Remnant ×2, moths", new[] { "marrow" }, false, "bells", null)
                .West("Greyfold_Edge", Ability.None)
                .East("Greyfold_Road_1", Ability.None);
            R("Greyfold_Road_1", "Greyfold.RoadThatStops", "The road in", "Cobbles that fade a stride at a time. Platforms are drawn only inside Wren's lantern-radius; outside it, outlines.", null, "smudge ×2, Sketch", new string[0], false, null, null)
                .West("Greyfold_Cathedral_2", Ability.None)
                .East("Greyfold_Road_2", Ability.None);
            R("Greyfold_Road_2", "Greyfold.RoadThatStops", "The mileposts", "Mileposts for a road nobody finished, each one nearer to nothing.", "Milepost", "smudge, lost Remnant, Sketch", new string[0], false, null, null)
                .West("Greyfold_Road_1", Ability.None)
                .East("Greyfold_Road_3", Ability.None);
            R("Greyfold_Road_3", "Greyfold.RoadThatStops", "Where it stops", "The road ends mid-stride. She steps off and stays herself: Clarity. Pell, sent to watch, sees her come back (the act break).", null, "", new[] { "pell" }, false, null, null)
                .West("Greyfold_Road_2", Ability.None)
                .East("Greyfold_Pool_1", Ability.Clarity);
            R("Greyfold_Pool_1", "Greyfold.MirrorPool", "The white shore", "A beach of white paper; the glide from the Wind Gate lands here from above. Colour only in her radius.", null, "lost Remnant, smudge, Sketch", new string[0], false, null, null)
                .Up("Windreach_Gate_2", Ability.Windmemory)
                .West("Greyfold_Road_3", Ability.Clarity)
                .East("Greyfold_Pool_2", Ability.None);
            R("Greyfold_Pool_2", "Greyfold.MirrorPool", "The Mirror Pool", "Water that shows what is not on the bank: a grey chick in the reflection, none beside her.", "Pool", "", new[] { "marrow" }, false, null, null)
                .West("Greyfold_Pool_1", Ability.None)
                .East("Greyfold_Threshold_1", Ability.Clarity, "act2.threshold");
            R("Greyfold_Threshold_1", "Greyfold.Threshold", "The Guild's line", "Tethers staked across the white, Wardens in a line, the Guild's field desk behind them. Halvard's third fight at the edge (6.3).", null, "Warden ×3", new[] { "halvard" }, true, "halvard_3", null)
                .West("Greyfold_Pool_2", Ability.Clarity, "act2.threshold")
                .East("Greyfold_Threshold_2", Ability.None);
            R("Greyfold_Threshold_2", "Greyfold.Threshold", "The Threshold", "The line itself. Voss, going in himself at last (6.11); Pell, if the report was kept. On the Return, Marrow echoes him.", null, "", new[] { "voss", "pell", "marrow" }, false, "voss", null)
                .West("Greyfold_Threshold_1", Ability.None)
                .East("Greyfold_LastCamp_1", Ability.None, "greyfold.crossed");
            R("Greyfold_LastCamp_1", "Greyfold.IsoldesLastCamp", "Isolde's Last Camp", "Just across the line: her tent, her lamp still lit, her complete atlas (reveal 5.2). Act 3 starts here; the Lantern leads on into the Blank.", "Atlas", "", new[] { "isolde" }, true, null, null)
                .West("Greyfold_Threshold_2", Ability.None, "greyfold.crossed")
                .East("Blank_Hollow_1", Ability.None, "act3.started");
            R("Blank_Hollow_1", "Blank.ThessalyHollow", "The Lantern", "Inside. White, and colour blooming round Wren as she walks; the first island drifts up under her feet. A grey chick starts following.", null, "lost Remnant", new[] { "marrow" }, false, null, null)
                .West("Greyfold_LastCamp_1", Ability.None, "act3.started")
                .East("Blank_Hollow_2", Ability.None);
            R("Blank_Hollow_2", "Blank.ThessalyHollow", "Thessaly Hollow", "Wren's birth village, grey. Ilse sees her first (reveal 5.3). Isolde, grey at the edges, cannot leave. The hub: a desk in Ilse's house.", null, "", new[] { "ilse", "isolde", "marrow" }, true, null, null)
                .West("Blank_Hollow_1", Ability.None)
                .East("Blank_Hollow_3", Ability.None);
            R("Blank_Hollow_3", "Blank.ThessalyHollow", "The drift", "The Hollow's far edge, where the islands of every place she left unanchored drift past (PRG-20). The capital lies east; Aury's light below.", null, "lost Remnant ×2", new string[0], false, null, null)
                .West("Blank_Hollow_2", Ability.None)
                .East("Blank_Capital_1", Ability.None)
                .Down("Blank_Aury_2", Ability.Clarity, "act3.started");
            R("Blank_Capital_1", "Blank.OldCapital", "The district's edge", "Streets of the old capital, half-drawn. A desk in the doorway of what was a Guild office.", null, "lost Remnant, Sketch", new string[0], true, null, null)
                .West("Blank_Hollow_3", Ability.None)
                .East("Blank_Capital_2", Ability.None);
            R("Blank_Capital_2", "Blank.OldCapital", "Corra's room", "A white room with a crayon floor. A child's drawing of her father, huge and wrong, keeps everyone out (6.13).", null, "", new[] { "corra" }, false, "corras_drawing", null)
                .West("Blank_Capital_1", Ability.None)
                .East("Blank_Capital_3", Ability.None);
            R("Blank_Capital_3", "Blank.OldCapital", "The mirror streets", "The district open: the capital's streets reversed, the Observatory's mirror-half at their end. A desk on its steps.", null, "lost Remnant ×2, Sketch", new string[0], true, null, null)
                .West("Blank_Capital_2", Ability.None)
                .East("Blank_Capital_4", Ability.None);
            R("Blank_Capital_4", "Blank.OldCapital", "The mirror-Observatory", "Corvin with the seventh keystone; he has drawn her a chair (reveals 5.4, 5.6). The choice laid out; the Archivist (6.14). Marrow echoes him.", null, "", new[] { "corvin", "marrow" }, false, "archivist", null)
                .West("Blank_Capital_3", Ability.None);
            R("Blank_Aury_1", "Blank.AurysLighthouse", "The tether's end", "From the Lantern Chain's third lighthouse by tether (Act 2): a causeway into the white, the light still turning.", null, "", new string[0], false, null, null)
                .West("Saltmarrow.LanternChain", Ability.None, "saltmarrow.tether")
                .East("Blank_Aury_2", Ability.None);
            R("Blank_Aury_2", "Blank.AurysLighthouse", "Aury's lamp room", "Aury, who asks if you've eaten, the keystone in his wings. In Act 3 his island drifts to the Hollow, and Sable sits with him.", null, "", new[] { "aury", "sable" }, false, null, null)
                .Up("Blank_Hollow_3", Ability.Clarity, "act3.started")
                .West("Blank_Aury_1", Ability.None);
        }
    }
}
