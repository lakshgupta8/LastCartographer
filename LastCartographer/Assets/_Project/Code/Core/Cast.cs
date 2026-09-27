using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>A recurring character: who they are and the one image they own (NAR-05, style guide §4, §6).</summary>
    public sealed class CastMember
    {
        public string Id;
        public string Name;
        public string Species;
        public string Faction;
        /// <summary>The recurring image every scene of theirs must carry (Sable: prices; Runa: counting people).</summary>
        public string Image;
        public string Tell;
        public string NeverSays;
        /// <summary>Speaks plainly rather than in the cryptic register (Isolde and Pell).</summary>
        public bool Plain;
    }

    /// <summary>One place on the map where a character is met: an act, a zone, a scene, and (when written) its Yarn node.</summary>
    public sealed class Appearance
    {
        public string Character;
        /// <summary>0 prologue, 1..3 the acts, 4 the epilogue. Outer regions list the earliest act they can be reached in.</summary>
        public int Act;
        /// <summary>A <see cref="WorldGraph"/> zone id, "Region.Zone".</summary>
        public string Zone;
        public string Scene;
        /// <summary>The scene's entry node in the Yarn project, or null when the scene has no lines (a glimpse).</summary>
        public string Node;
        /// <summary>True once the node exists in the project; the tests hold data and scripts to each other through this.</summary>
        public bool Staged;
        public string Writes;
    }

    /// <summary>
    /// The returning cast as data (NAR-05): every member, and every appearance by act and zone, so the character bibles
    /// (`docs/story/character-bibles.md`) and the map (<see cref="WorldGraph"/>) can be checked against each other and
    /// against the Yarn project. Pure data; no Unity types.
    /// </summary>
    public static class Cast
    {
        public const int Prologue = 0, Act1 = 1, Act2 = 2, Act3 = 3, Epilogue = 4;

        /// <summary>The five NAR-05 asks for by name.</summary>
        public static readonly string[] Recurring = { "pell", "sable", "runa", "teodor", "marrow" };

        static readonly List<CastMember> _members = new List<CastMember>();
        static readonly List<Appearance> _appearances = new List<Appearance>();
        static bool _built;

        public static IReadOnlyList<CastMember> Members { get { EnsureDefaults(); return _members; } }
        public static IReadOnlyList<Appearance> Appearances { get { EnsureDefaults(); return _appearances; } }

        public static CastMember Find(string id) { EnsureDefaults(); return _members.Find(m => m.Id == id); }
        public static List<Appearance> AppearancesOf(string id) { EnsureDefaults(); return _appearances.FindAll(a => a.Character == id); }

        /// <summary>The distinct acts a character is met in, in order.</summary>
        public static SortedSet<int> ActsOf(string id)
        {
            var acts = new SortedSet<int>();
            foreach (var a in AppearancesOf(id)) acts.Add(a.Act);
            return acts;
        }

        /// <summary>Met in at least two acts.</summary>
        public static bool IsRecurring(string id) => ActsOf(id).Count >= 2;

        /// <summary>Who is met in a zone, in any act.</summary>
        public static List<Appearance> InZone(string zoneId) { EnsureDefaults(); return _appearances.FindAll(a => a.Zone == zoneId); }

        public static void Reset() { _members.Clear(); _appearances.Clear(); _built = false; }

        static CastMember Member(string id, string name, string species, string faction, string image, string tell, string neverSays, bool plain = false)
        {
            var m = new CastMember { Id = id, Name = name, Species = species, Faction = faction, Image = image, Tell = tell, NeverSays = neverSays, Plain = plain };
            _members.Add(m);
            return m;
        }

        static void At(string character, int act, string zone, string scene, string node = null, bool staged = false, string writes = null)
        {
            _appearances.Add(new Appearance { Character = character, Act = act, Zone = zone, Scene = scene, Node = node, Staged = staged, Writes = writes });
        }

        public static void EnsureDefaults()
        {
            if (_built) return;
            _built = true;

            // ---- The five (character-bibles.md §1–5) ------------------------------------------------------------------
            Member("pell", "Pell", "jackdaw", "Meridian Guild", "lists, shortening", "\"which, technically, means...\"", "anything unkind", plain: true);
            At("pell", Act1, "Greyfold.RoadThatStops", "the act break: sent to watch, sees her step in and come back", "Edge_Pell_Watch", writes: "pell.saw_her_cross");
            At("pell", Act2, "Halden.JourneymansHall", "the minder; Interlude A, the report", "Hall_Pell_Minder", staged: true, writes: "pell.report_read, pell.report_sent");
            At("pell", Act2, "Halden.Vault", "the empty slot (plant 5.2)", "Vault_Pell_Slot", staged: true, writes: "halden.vault.pell_counted");
            At("pell", Act2, "Halden.Bastion", "Voss's office by the flyer-tower: the drawing (plant 5.5)", "Office_Pell_Drawing", staged: true, writes: "halden.office.pell_ledgers");
            At("pell", Act2, "Greyfold.Threshold", "if the report was not sent: comes to see her cross", "Threshold_Pell_Cross", writes: "pell.at_threshold");
            At("pell", Act3, "Halden.Observatory", "the Return: holds the frame's door; the last list", "Observatory_Pell_Return", writes: "pell.last_list");
            At("pell", Epilogue, "Halden.JourneymansHall", "the epilogue walk, by ending", "Epilogue_Pell");

            Member("sable", "Sable", "cormorant", "the Ferrymen", "prices", "ends the conversation first", "\"I hope\"");
            At("sable", Act1, "Saltmarrow.Quay", "the quay: the Guild's haste, the board, the reed, the whale, the widow, the shop, Halvard, Aury", "Quay_Sable", staged: true, writes: "saltmarrow.sable.*, saltmarrow.bone_bridge.heard, saltmarrow.widow.decided");
            At("sable", Act1, "Saltmarrow.BoneBridge", "the whale's step: she rows Wren under and does not sing along", "BoneBridge_Sable", writes: "saltmarrow.bone_bridge.rowed");
            At("sable", Act2, "Saltmarrow.LanternChain", "the tether to the third lighthouse; she rows it herself", "Chain_Sable_Tether", writes: "sable.tether_sold, saltmarrow.tether");
            At("sable", Act3, "Blank.AurysLighthouse", "sits with Aury; talks prices; does not tell him", "Aury_Sable", writes: "sable.aury_told, blank.aury.knows");
            At("sable", Epilogue, "Saltmarrow.Quay", "prices, by ending", "Epilogue_Sable");

            Member("runa", "Runa", "capercaillie", "the Holdfast", "counting people out loud, sung", "\"we\" for her town even alone", "\"I don't know how\"");
            At("runa", Act1, "Emberdown.RollCallBell", "counted in: \"stranger, one\"", "Bell_Runa_Count", staged: true, writes: "emberdown.runa.counted");
            At("runa", Act1, "Emberdown.NineChimneys", "Talonhold: the old way up", "Chimneys_Runa_Climb", staged: true, writes: "emberdown.runa.climbed");
            At("runa", Act1, "Emberdown.Overlook", "the Greyfold from the north (plant 4.6)", "Overlook_Runa", staged: true, writes: "emberdown.overlook.seen");
            At("runa", Act1, "Emberdown.CinderBaths", "the debate: she sings the surveyor's numbers back", "Baths_Runa_Debate", staged: true, writes: "emberdown.debate.heard");
            At("runa", Act1, "Emberdown.Hollowvein", "the long roll-call down, or leave it buried", "Hollowvein_Runa_Walk", staged: true, writes: "emberdown.hollowvein.walked / .buried");
            At("runa", Act2, "Emberdown.RollCallBell", "named: Wren in the roll-call once she has held a place", "Bell_Runa_Named", staged: true, writes: "runa.named_wren");
            At("runa", Act3, "Halden.Observatory", "the true ending's chorus: she leads the roll-call round the Blank", "Observatory_Runa_Chorus", writes: "ending.chorus_led");
            At("runa", Epilogue, "Emberdown.KettilsRest", "the epilogue, by ending", "Epilogue_Runa");

            Member("teodor", "Brother Teodor Ashe", "mourning dove", "the Unwriters", "the faded in the present tense", "asks permission before a hard sentence", "a raised voice");
            At("teodor", Act1, "Verdance.QuietHouse", "first meeting: the reeds have told him; the vigil's invitation", "QuietHouse_Teodor", staged: true, writes: "verdance.teodor.met");
            At("teodor", Act1, "Verdance.RootChapel", "Inkthread: the solvent-line reversed", "RootChapel_Teodor_Thread", staged: true, writes: "verdance.teodor.thread");
            At("teodor", Act1, "Verdance.LanternGrove", "the vigil: eleven names, no choices", "Grove_Teodor_Vigil", staged: true, writes: "verdance.grove.vigil");
            At("teodor", Act1, "Verdance.SunkenLibrary", "Ansel's page: he will not turn it for you", "Library_Teodor_Ansel", staged: true, writes: "verdance.library.teodor_asked");
            At("teodor", Act1, "Verdance.Aldermere", "the last day: attend, or stop it", "Aldermere_Teodor", staged: true, writes: "verdance.aldermere.attended / .stopped");
            At("teodor", Act2, "Verdance.QuietHouse", "the keystone: say why", "QuietHouse_Teodor_Keystone", staged: true, writes: "teodor.keystone_given / teodor.refused");
            At("teodor", Act3, "Halden.Observatory", "the Unwritten: he dissolves the keystones, naming their places", "Observatory_Teodor_Unwritten", writes: "ending.unwritten");
            At("teodor", Epilogue, "Verdance.QuietHouse", "the epilogue, by ending", "Epilogue_Teodor");

            Member("marrow", "Marrow", "grey chick, unreadable", "none", "echoes", "one original word per island, four in all", "a word that is not the scene's");
            At("marrow", Prologue, "Greyfold.HalfCathedral", "glimpsed in the white at the thirtieth step, silent");
            At("marrow", Act2, "Greyfold.MirrorPool", "in the pool's reflection and not on the bank; one echo", "MirrorPool_Marrow", writes: "marrow.seen_in_pool");
            At("marrow", Act3, "Blank.ThessalyHollow", "following, from the Lantern on; Ilse does not know it; first word", "Blank_Marrow_Follow", writes: "marrow.following, marrow.words=1");
            At("marrow", Act3, "Blank.OldCapital", "echoes Corvin; second word", "Capital_Marrow_Word", writes: "marrow.words=2");
            At("marrow", Act3, "Greyfold.Threshold", "the Return: echoes Voss; third word", "Threshold_Marrow", writes: "marrow.words=3");
            At("marrow", Epilogue, "Blank.ThessalyHollow", "the verdict: its last original word, or none", "Epilogue_Marrow", writes: "marrow.words=4");

            // ---- The rest of the returning cast (§8), enough to place them ---------------------------------------------
            Member("isolde", "Isolde Marr", "(Wren's mentor)", "Meridian Guild", "questions instead of answers", "\"journeyman\" when proud, \"Wren\" when scared", "\"I'm sorry\"", plain: true);
            At("isolde", Prologue, "Greyfold.HalfCathedral", "the Edge: survey, bind, seal; she walks in", "Prologue_Edge_Arrive", staged: true, writes: "prologue.*");
            At("isolde", Act1, "Halden.OldOrchard", "her cache: the Stillness measured, five names", "Orchard_Isolde_Cache", staged: true, writes: "isolde.cache");
            At("isolde", Act3, "Greyfold.IsoldesLastCamp", "her complete atlas (reveal 5.2)", "LastCamp_Isolde", writes: "act3.started");
            At("isolde", Act3, "Blank.ThessalyHollow", "alive, grey at the edges; cannot leave under her own power", "Hollow_Isolde");

            Member("halvard", "Warden-Sergeant Halvard", "heron", "Meridian Guild", "paces and counts", "enters silence as a plea", "her first name");
            At("halvard", Act1, "Saltmarrow.LanternChain", "the first hunt: three paces, the count, unlicensed (staged in the fourth lighthouse; the bible's Salt Chapel)", "Lighthouse_Halvard_Hunt", staged: true, writes: "act1.halvard_met, act1.unlicensed");
            At("halvard", Act2, "Halden.SevenBridges", "the second hunt, new kit", "Bridges_Halvard_Hunt", staged: true, writes: "act2.halvard_second");
            At("halvard", Act2, "Greyfold.Threshold", "the third, beside Voss", "Threshold_Halvard", writes: "act2.halvard_third");

            Member("dotha", "Dotha", "(last elder of Merrow's End)", "Merrow's End", "the songs, counted down", "never asks twice", "\"please\"");
            At("dotha", Act1, "Saltmarrow.MerrowsEnd", "her last season: nine songs of eleven; three ways", "Merrow_Dotha", staged: true, writes: "saltmarrow.dotha.*");

            Member("maren", "Queen-Regent Maren Ostrell", "swan", "the Crown of Halden", "permanence", "rules for a nephew who is nine forever", "\"change\"");
            At("maren", Act2, "Halden.Bastion", "Interlude B: the audience; make Halden permanent", "Bastion_Maren_Audience", staged: true, writes: "halden.maren.decided");

            Member("kettil", "Old Kettil", "capercaillie", "the Holdfast", "counting people", "proverbs, orders, laughter", "\"Guild\" without spitting");
            At("kettil", Act1, "Emberdown.KettilsRest", "town-mother; the roll-call is hers by right and Runa's by voice", "Rest_Kettil", staged: true);
            At("kettil", Act1, "Emberdown.CinderBaths", "the debate, with real numbers", "Baths_Kettil_Debate", staged: true);

            Member("idrenne", "Speaker Idrenne", "crane", "the Windreach Clans", "where she was standing when she learned it", "amused, unhurried", "\"always\" or \"never\"");
            At("idrenne", Act2, "Windreach.LongGrassCamp", "the moving camp, the first night: the fire ring", "Camp_Idrenne", staged: true, writes: "windreach.camp.night=1");
            At("idrenne", Act2, "Windreach.DryRiver", "the second night, in the riverbed: the stones are a map", "River_Idrenne_Night", staged: true, writes: "windreach.stones.named");
            At("idrenne", Act2, "Windreach.WindGate", "the fledgling-leap: forty years, and then Wren", "Gate_Idrenne_Leap", staged: true, writes: "windreach.leap.done, ability.windmemory");
            At("idrenne", Act2, "Windreach.IdrennesFire", "the third night, in the high grass: where each was standing", "Grass_Idrenne_Night", staged: true, writes: "windreach.camp.walked");
            At("idrenne", Act2, "Windreach.IdrennesFire", "how the clans do it, plainly; the survey; the keystone, laughing", "Fire_Idrenne", staged: true, writes: "windreach.fire.witnessed, windreach.survey.decided, keystone.windreach");
            At("idrenne", Act2, "Windreach.FallenStar", "the one cost", "Star_Idrenne", staged: true, writes: "windreach.star.cold");

            Member("hale", "Surveyor Hale", "godwit", "Meridian Guild", "the finished page", "\"professional courtesy\"", "\"we\"");
            At("hale", Act2, "Windreach.NineStones", "the ninth stone at dusk: stop him, let him finish, or hold the staff; boss 6.9", "Stones_Hale", staged: true, writes: "windreach.hale.decided, windreach.hale.finished / windreach.hale.pages");

            Member("voss", "Guildmaster Aurelian Voss", "grey heron", "Meridian Guild", "titles; \"we\" for the Guild", "flinches at \"Halloway\"", "\"Corra\"");
            At("voss", Act2, "Greyfold.Threshold", "the one speech; boss 6.11", "Threshold_Voss", writes: "greyfold.crossed");
            At("voss", Act3, "Halden.Observatory", "changed, or a statue", "Observatory_Voss");

            Member("corvin", "Corvin Halloway, the Archivist", "great owl", "the Remnant", "\"When I—\"", "self-correcting", "\"it wasn't my fault\"");
            At("corvin", Act3, "Blank.OldCapital", "the mirror-Observatory; reveals 5.4 and 5.6; boss 6.14", "Capital_Corvin");

            Member("aury", "Aury", "cormorant", "the Remnant", "asks whether you've eaten", "does not know the difference", "\"I'm dead\"");
            At("aury", Act2, "Blank.AurysLighthouse", "the third lighthouse by tether; the keystone in his wings", "Aury_Lighthouse");
            At("aury", Act3, "Blank.AurysLighthouse", "his island; Sable beside him", "Aury_Island");

            Member("ilse", "Ilse", "(Wren's mother, grey)", "the Remnant", "letting go", "sees Wren before Wren sees her", "\"stay\"");
            At("ilse", Act3, "Blank.ThessalyHollow", "reveal 5.3", "Hollow_Ilse");
        }
    }
}
