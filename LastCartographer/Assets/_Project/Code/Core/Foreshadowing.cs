using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>Where a scene falls in the story's order (bible 7). Act 1's climb is whichever region she climbs first.</summary>
    public enum Beat
    {
        Prologue = 0,
        /// <summary>Act 1: Saltmarrow, the Drowned Quay to the Lantern Chain.</summary>
        Coast = 10,
        /// <summary>Act 1: Emberdown or the Verdance, whichever she climbs first (bible 7.1.4). The other is Act 2's.</summary>
        Climb = 20,
        /// <summary>Act 1: Halden, the mills and the Journeyman's Hall on the way to the orchard.</summary>
        Hall = 30,
        /// <summary>Act 1: the Old Orchard and Isolde's cache (reveal 5.1).</summary>
        Orchard = 32,
        /// <summary>Act 1's end: the Edge, again; Pell watches.</summary>
        Edge = 34,
        /// <summary>Act 2: the four regions, the Halden interludes, the islands the tether reaches.</summary>
        Act2 = 40,
        Threshold = 50,
        LastCamp = 60, Lantern = 61, Hollow = 62, Capital = 63, Return = 64, Observatory = 65,
        Epilogue = 70,
    }

    /// <summary>Which climb Act 1 took (bible 7.1.4). A scene in the other region falls in Act 2.</summary>
    public enum ClimbRoute { Either, Emberdown, Verdance }

    /// <summary>
    /// The foreshadowing audit's catalog (NAR-16, <c>docs/story/foreshadowing.md</c>): bible 5's six secrets, the scene
    /// each is revealed in, and where every scene that plants one falls. The lines are tagged in Yarn: <c>#plant:5.x</c>
    /// before the reveal, <c>#reveal:5.x</c> on the line that says it, <c>#callback:5.x</c> after. The rule of three
    /// (bible 11.1) is that every secret has three plants in three scenes before its reveal, whichever climb Act 1 took,
    /// and at least one of them on the story's own road (<see cref="EndingRoutes"/>).
    /// </summary>
    public static class Foreshadowing
    {
        public static readonly string[] Secrets = { "5.1", "5.2", "5.3", "5.4", "5.5", "5.6" };

        public static string Title(string secret) => secret switch
        {
            "5.1" => "The Stillness",
            "5.2" => "Why Isolde walked in",
            "5.3" => "Where Wren is from",
            "5.4" => "The Halloway debt",
            "5.5" => "Voss's daughter",
            "5.6" => "The Grounding",
            _ => "",
        };

        /// <summary>The scene each secret is said in (bible 5 and 7).</summary>
        public static readonly IReadOnlyDictionary<string, string> RevealNode = new Dictionary<string, string>
        {
            { "5.1", "Orchard_Isolde_Cache" },   // Isolde's pages: anchoring stops everything
            { "5.2", "LastCamp_Isolde" },        // the sixth slot, taken back to Corvin
            { "5.3", "Hollow_Ilse" },            // born here, after
            { "5.4", "Capital_Corvin" },         // "When I— granddaughter."
            { "5.5", "Capital_Corra" },          // "He's the Guildmaster now."
            { "5.6", "Capital_Corvin" },         // they lost the memory of their wings
        };

        static readonly Dictionary<string, (Beat beat, ClimbRoute climb)> _nodes = new Dictionary<string, (Beat, ClimbRoute)>
        {
            // The prologue: the Edge with Isolde, and waking on the shore.
            { "Prologue_Edge_Arrive", (Beat.Prologue, ClimbRoute.Either) },
            { "Prologue_Edge_Survey", (Beat.Prologue, ClimbRoute.Either) },
            { "Prologue_Edge_AfterSmudges", (Beat.Prologue, ClimbRoute.Either) },
            { "Prologue_Edge_Departure", (Beat.Prologue, ClimbRoute.Either) },
            { "Prologue_Shore_Wake", (Beat.Prologue, ClimbRoute.Either) },
            // Act 1, the coast.
            { "Quay_Sable_First", (Beat.Coast, ClimbRoute.Either) },
            { "Quay_Sable_Again", (Beat.Coast, ClimbRoute.Either) },
            { "Lighthouse_Lamp", (Beat.Coast, ClimbRoute.Either) },
            { "Lighthouse_Halvard_Hunt", (Beat.Coast, ClimbRoute.Either) },
            { "Merrow_Dotha_Season", (Beat.Coast, ClimbRoute.Either) },
            { "Chain_Log", (Beat.Coast, ClimbRoute.Either) },            // the rooms' own plants (NAR-15, Dressing)
            { "Chapel_Tapestry", (Beat.Coast, ClimbRoute.Either) },
            // Act 1's climb: Emberdown.
            { "Overlook_Runa", (Beat.Climb, ClimbRoute.Emberdown) },
            { "Chimneys_Runa_Climb", (Beat.Climb, ClimbRoute.Emberdown) },
            { "Chimneys_Ninth_Agent", (Beat.Climb, ClimbRoute.Emberdown) },
            { "Baths_Kettil_Debate", (Beat.Climb, ClimbRoute.Emberdown) },
            { "Rest_Lintel", (Beat.Climb, ClimbRoute.Emberdown) },
            // Act 1's climb: the Verdance.
            { "Gate_Inscription", (Beat.Climb, ClimbRoute.Verdance) },
            { "RootChapel_Teodor_Thread", (Beat.Climb, ClimbRoute.Verdance) },
            { "QuietHouse_Teodor_First", (Beat.Climb, ClimbRoute.Verdance) },
            { "QuietHouse_Teodor_Keystone", (Beat.Climb, ClimbRoute.Verdance) },
            { "Library_Ansel", (Beat.Climb, ClimbRoute.Verdance) },
            { "Library_Teodor_Ansel", (Beat.Climb, ClimbRoute.Verdance) },
            { "Library_Lectern", (Beat.Climb, ClimbRoute.Verdance) },
            { "Road_Milestone", (Beat.Climb, ClimbRoute.Verdance) },
            // Act 1, Halden: the Hall on the way to the orchard, the orchard, the Edge.
            { "Hall_Tam", (Beat.Hall, ClimbRoute.Either) },
            { "Bridges_TollBoard", (Beat.Hall, ClimbRoute.Emberdown) },   // the bridges are the Overlook road's way in
            { "Mills_Sheets", (Beat.Hall, ClimbRoute.Verdance) },         // the mills, the canopy road's
            { "Hall_Roll", (Beat.Hall, ClimbRoute.Either) },
            { "Hall_Order", (Beat.Hall, ClimbRoute.Either) },
            { "Hall_ExamPapers", (Beat.Hall, ClimbRoute.Either) },
            { "Orchard_Isolde_Cache", (Beat.Orchard, ClimbRoute.Either) },
            { "Orchard_Keeper", (Beat.Orchard, ClimbRoute.Either) },
            { "Orchard_Gravestone", (Beat.Orchard, ClimbRoute.Either) },
            { "Edge_Pell_Watch", (Beat.Edge, ClimbRoute.Either) },
            // Act 2.
            { "Bastion_Maren_Audience", (Beat.Act2, ClimbRoute.Either) },
            { "Office_Pell_Drawing", (Beat.Act2, ClimbRoute.Either) },
            { "Vault_Pell_Slot", (Beat.Act2, ClimbRoute.Either) },
            { "Office_Drawing", (Beat.Act2, ClimbRoute.Either) },
            { "Bastion_Plaque", (Beat.Act2, ClimbRoute.Either) },
            { "Gate_Idrenne_Leap", (Beat.Act2, ClimbRoute.Either) },
            { "Fire_Idrenne", (Beat.Act2, ClimbRoute.Either) },
            { "Aury_Lighthouse", (Beat.Act2, ClimbRoute.Either) },   // by tether, in Act 2 (blank-islands.md)
            { "Threshold_Voss", (Beat.Threshold, ClimbRoute.Either) },
            // Act 3.
            { "LastCamp_Isolde", (Beat.LastCamp, ClimbRoute.Either) },
            { "Hollow_Ilse", (Beat.Hollow, ClimbRoute.Either) },
            { "Capital_Corra", (Beat.Capital, ClimbRoute.Either) },
            { "Capital_Corvin", (Beat.Capital, ClimbRoute.Either) },
        };

        /// <summary>Every scene the audit places: each one that plants, reveals or calls back a secret.</summary>
        public static IEnumerable<string> PlacedNodes => _nodes.Keys;
        public static bool IsPlaced(string node) => _nodes.ContainsKey(node ?? "");

        /// <summary>Where a scene falls for a player whose Act 1 climb was <paramref name="first"/>.</summary>
        public static Beat BeatOf(string node, ClimbRoute first)
        {
            var (beat, climb) = _nodes[node];
            return climb != ClimbRoute.Either && first != ClimbRoute.Either && climb != first ? Beat.Act2 : beat;
        }

        public static Beat RevealBeat(string secret) => _nodes[RevealNode[secret]].beat;

        /// <summary>
        /// The scenes that plant a secret before its reveal, for a player whose Act 1 climb was <paramref name="first"/>.
        /// A plant in the reveal's own scene doesn't count: it is part of the reveal.
        /// </summary>
        public static List<string> PlantsBefore(string secret, ClimbRoute first, IEnumerable<(string node, string secret)> plants)
        {
            var reveal = RevealBeat(secret);
            return plants.Where(p => p.secret == secret && p.node != RevealNode[secret] && IsPlaced(p.node) && BeatOf(p.node, first) < reveal)
                         .Select(p => p.node).Distinct().ToList();
        }
    }
}
