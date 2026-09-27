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
    /// Room-by-room plans for the regions not yet built (DES-09 Emberdown and the Verdance, DES-10 Halden Reach), as data so the spec
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
            R("Emberdown_Chimneys_4", "Emberdown.NineChimneys", "The flue road", "A tunnel of old flues east toward the baths; the heat rises through the floor.", null, "salamander ×2", new string[0], false, null, null)
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
            R("Emberdown_Hollow_1", "Emberdown.Hollowvein", "The adit", "Down from the pit-head behind the boards; the long roll-call starts at its first beam.", null, "", new[] { "runa" }, false, null, "hollowvein")
                .Up("Emberdown_Rest_3", Ability.Talonhold, "emberdown.hollowvein_opened")
                .Down("Emberdown_Hollow_2", Ability.None);
            R("Emberdown_Hollow_2", "Emberdown.Hollowvein", "The first gallery", "Lamps on the walls, one for each name; the walk's second and third verses.", "Gallery", "smudge ×2", new string[0], false, null, "hollowvein")
                .Up("Emberdown_Hollow_1", Ability.None)
                .Down("Emberdown_Hollow_3", Ability.None);
            R("Emberdown_Hollow_3", "Emberdown.Hollowvein", "The flooded gallery", "Black water to the knee; a desk the miners left, still dry.", null, "smudge", new string[0], true, null, "hollowvein")
                .Up("Emberdown_Hollow_2", Ability.None)
                .Down("Emberdown_Hollow_4", Ability.None);
            R("Emberdown_Hollow_4", "Emberdown.Hollowvein", "The bottom", "The collapse itself: the Collapse wakes on the fourth verse (6.4). The keystone is under it.", null, "", new string[0], false, "collapse", "hollowvein")
                .Up("Emberdown_Hollow_3", Ability.None);
            R("Verdance_Road_1", "Verdance.OldRoad", "The iris gap", "From the Pale Iris Fields over a Wingbeat gap (soft) onto a road the forest has half taken.", null, "crab, skimmer", new string[0], false, null, null)
                .West("Saltmarrow.IrisFields", Ability.Wingbeat, null, true)
                .East("Verdance_Road_2", Ability.None);
            R("Verdance_Road_2", "Verdance.OldRoad", "The milestones", "Flying-age milestones, every one giving the distance to a place that is gone.", "Milestone", "smudge, crab", new string[0], false, null, null)
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
            R("Verdance_Chapel_1", "Verdance.RootChapel", "The root stair", "Down through the roots; lanterns hung from them.", null, "smudge", new string[0], false, null, null)
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
            R("Verdance_Grove_3", "Verdance.LanternGrove", "The canopy", "Up into the canopy by thread; the forest floor out of sight below.", "Canopy", "Cantor, skimmer", new string[0], false, null, null)
                .Down("Verdance_Grove_2", Ability.None)
                .East("Verdance_Grove_4", Ability.None);
            R("Verdance_Grove_4", "Verdance.LanternGrove", "The high lanterns", "The grove's crown; a thread line east drops to the library's roof.", null, "smudge", new string[0], false, null, null)
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
            R("Verdance_Aldermere_3", "Verdance.Aldermere", "The ash field", "Where the village is already paper; the canopy road starts over it by thread.", null, "Cantor, smudge", new string[0], false, null, null)
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
            R("Halden_Mills_1", "Halden.PaperMills", "The mill race", "Where the canopy road from the Overgrown Gate comes down: a mill race, wheels, wet paper in the air.", null, "Warden, smudge", new string[0], false, null, null)
                .West("Verdance_Gate_2", Ability.Inkthread)
                .East("Halden_Mills_2", Ability.None);
            R("Halden_Mills_2", "Halden.PaperMills", "The drying lofts", "Sheets of new vellum hung to dry, rooms deep; the Seven Bridges are overhead.", "Lofts", "smudge ×2", new string[0], false, null, null)
                .Up("Halden_Bridges_4", Ability.None)
                .West("Halden_Mills_1", Ability.None)
                .East("Halden_Mills_3", Ability.None);
            R("Halden_Mills_3", "Halden.PaperMills", "The pulp yard", "The strike's picket line: the millworkers of Lowmarket have downed tools. The Hall steps are beyond.", null, "", new string[0], false, null, null)
                .West("Halden_Mills_2", Ability.None)
                .East("Halden_Hall_1", Ability.None);
            R("Halden_Lowmarket_1", "Halden.Lowmarket", "The stair down", "Below the walls. The paint is thinner here, and so is everything else.", null, "smudge", new string[0], false, null, null)
                .Up("Halden_Bridges_2", Ability.None)
                .East("Halden_Lowmarket_2", Ability.None);
            R("Halden_Lowmarket_2", "Halden.Lowmarket", "Lowmarket", "The district below the walls, fading; its notice board reads 'survey scheduled'. The strike hall, where the decision is made.", "Market", "", new string[0], true, null, null)
                .West("Halden_Lowmarket_1", Ability.None)
                .East("Halden_Lowmarket_3", Ability.None);
            R("Halden_Lowmarket_3", "Halden.Lowmarket", "The south gate", "The south road to Windreach, barred until Act 2 opens it.", null, "Warden", new string[0], false, null, null)
                .West("Halden_Lowmarket_2", Ability.None)
                .East("Windreach.NineStones", Ability.None, "act2.started");
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
                .East("Greyfold.EdgeCamp", Ability.None, "isolde.cache")
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
        }
    }
}
