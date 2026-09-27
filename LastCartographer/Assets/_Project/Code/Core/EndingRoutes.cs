using System.Collections.Generic;

namespace OWSBG.Core
{
    public enum RouteStepKind { Talk, Boss }

    /// <summary>One move on a route: a scene played with these choices, or a boss beaten. Where it happens, and what it grants.</summary>
    public sealed class RouteStep
    {
        public RouteStepKind Kind;
        /// <summary>A Yarn node, or a boss sheet id.</summary>
        public string Key;
        public string Zone;
        public int[] Choices;
        public Ability Grants;
        public string Note;
    }

    public sealed class EndingRoute
    {
        public Ending Ending;
        public RouteStep[] Steps;
    }

    /// <summary>
    /// One way to each ending from a new game (DES-12), as data: the scenes in order with the choices taken, and the
    /// fights won. EndingRoutesTests replays each through the shipped Yarn project, and before every step proves the
    /// step's zone reachable on the macro map with only what the route has earned. Generated with the matrix
    /// (`docs/design/ending-matrix.md`); edit both together.
    /// </summary>
    public static class EndingRoutes
    {
        static readonly List<EndingRoute> _all = new List<EndingRoute>();
        static bool _built;

        public static IReadOnlyList<EndingRoute> All { get { EnsureDefaults(); return _all; } }
        public static EndingRoute For(Ending e) { EnsureDefaults(); return _all.Find(r => r.Ending == e); }

        static RouteStep Talk(string node, string zone, int[] choices, Ability grants, string note)
            => new RouteStep { Kind = RouteStepKind.Talk, Key = node, Zone = zone, Choices = choices, Grants = grants, Note = note };
        static RouteStep Boss(string id, string zone, int[] choices, Ability grants, string note)
            => new RouteStep { Kind = RouteStepKind.Boss, Key = id, Zone = zone, Choices = choices, Grants = grants, Note = note };
        static void Route(Ending e, params RouteStep[] steps) => _all.Add(new EndingRoute { Ending = e, Steps = steps });

        public static void EnsureDefaults()
        {
            if (_built) return;
            _built = true;
            Route(Ending.Fixed,
                Talk("Quay_Sable", "Saltmarrow.Quay", new[] { 1 }, Ability.None, "Sable: the Guild's haste; `saltmarrow.sable.talked`"),
                Boss("lamp_keeper", "Saltmarrow.LanternChain", new int[0], Ability.Wingbeat, "6.1 The Lamp-Keeper"),
                Talk("Lighthouse_Halvard_Hunt", "Saltmarrow.LanternChain", new[] { 1, 0 }, Ability.None, "Halvard's first hunt: unlicensed"),
                Talk("Quay_Sable", "Saltmarrow.Quay", new[] { 1 }, Ability.None, "\"The third lighthouse. Who keeps it?\" `saltmarrow.sable.aury`"),
                Talk("Rest_Kettil", "Emberdown.KettilsRest", new[] { 0 }, Ability.None, "Kettil counts her in; `emberdown.kettil.met`"),
                Talk("Bell_Runa_Count", "Emberdown.RollCallBell", new[] { 0, 0 }, Ability.None, "counted; \"Teach me.\": the walk (`holdfast.walk_learned`)"),
                Talk("Chimneys_Runa_Climb", "Emberdown.NineChimneys", new[] { 1 }, Ability.Talonhold, "Talonhold; `emberdown.runa.climbed`"),
                Talk("Baths_Kettil_Debate", "Emberdown.CinderBaths", new[] { 0 }, Ability.None, "the debate; `emberdown.debate.heard`"),
                Talk("Rest_Kettil", "Emberdown.KettilsRest", new[] { 0 }, Ability.None, "\"Then let's bring them up.\": `emberdown.hollowvein_opened`"),
                Talk("Hollowvein_Runa_Walk", "Emberdown.Hollowvein", new[] { 0 }, Ability.None, "\"Then we walk it down.\": the long roll-call"),
                Boss("collapse", "Emberdown.Hollowvein", new int[0], Ability.None, "6.4 The Collapse, on the fourth verse"),
                Talk("Hollowvein_Runa_After", "Emberdown.Hollowvein", new int[0], Ability.None, "thirty-one; `emberdown.hollowvein.walked`, **keystone.hollowvein**"),
                Talk("QuietHouse_Teodor", "Verdance.QuietHouse", new[] { 0 }, Ability.None, "Teodor; `verdance.teodor.met`"),
                Talk("RootChapel_Teodor_Thread", "Verdance.RootChapel", new[] { 0 }, Ability.Inkthread, "Inkthread; `verdance.teodor.thread`"),
                Talk("Aldermere_Teodor", "Verdance.Aldermere", new[] { 0 }, Ability.None, "\"Then I'll stay to the end.\": attended, the square released"),
                Talk("QuietHouse_Teodor", "Verdance.QuietHouse", new[] { 1 }, Ability.None, "\"Because they asked. And you made sure.\": **keystone.quiet_house**"),
                Talk("Orchard_Isolde_Cache", "Halden.OldOrchard", new[] { 1 }, Ability.None, "the five names; `isolde.cache`: the road to the Edge"),
                Talk("Edge_Pell_Watch", "Greyfold.RoadThatStops", new[] { 1 }, Ability.Clarity, "she steps in and stays herself: Clarity, `act2.started`"),
                Talk("Hall_Pell_Minder", "Halden.JourneymansHall", new[] { 1 }, Ability.None, "the minder; \"Then don't send it.\" (Warden)"),
                Talk("Bridges_Halvard_Hunt", "Halden.SevenBridges", new[] { 1 }, Ability.None, "the second hunt; \"Stand aside.\" (Warden)"),
                Talk("Hall_Pell_Minder", "Halden.JourneymansHall", new[] { 0 }, Ability.None, "Interlude A: \"Let me read it.\" Two Warden voices: **Pell keeps it**"),
                Talk("Camp_Idrenne", "Windreach.LongGrassCamp", new[] { 0 }, Ability.None, "the first fire; `windreach.idrenne.met`"),
                Talk("River_Idrenne_Night", "Windreach.DryRiver", new[] { 0 }, Ability.None, "the second fire: the stones are a map"),
                Talk("Gate_Idrenne_Leap", "Windreach.WindGate", new[] { 0 }, Ability.Windmemory, "the leap: Windmemory"),
                Talk("Grass_Idrenne_Night", "Windreach.IdrennesFire", new[] { 0 }, Ability.None, "the third fire: known; `windreach.camp.walked`"),
                Talk("Fire_Idrenne", "Windreach.IdrennesFire", new[] { 0, 1 }, Ability.None, "Idrenne's Fire, witnessed; \"I'll leave it undrawn.\"; **keystone.windreach**"),
                Talk("EdgeCamp_Notice", "Greyfold.EdgeCamp", new[] { 0 }, Ability.None, "Voss's notice: `act2.threshold`"),
                Boss("halvard_3", "Greyfold.Threshold", new int[0], Ability.None, "6.3 Halvard's third"),
                Talk("Threshold_Halvard", "Greyfold.Threshold", new int[0], Ability.None, "\"I'll enter it as a survey.\""),
                Talk("Threshold_Voss", "Greyfold.Threshold", new[] { 1 }, Ability.None, "the one speech"),
                Boss("voss", "Greyfold.Threshold", new int[0], Ability.None, "6.11 Voss"),
                Talk("Threshold_Voss", "Greyfold.Threshold", new int[0], Ability.None, "\"Take it in. Go.\": `greyfold.crossed`"),
                Talk("LastCamp_Isolde", "Greyfold.IsoldesLastCamp", new[] { 1 }, Ability.None, "her atlas (5.2): `act3.started`"),
                Talk("Blank_Marrow_Follow", "Blank.ThessalyHollow", new int[0], Ability.None, "the Lantern: Marrow follows"),
                Talk("Hollow_Ilse", "Blank.ThessalyHollow", new[] { 0 }, Ability.None, "Ilse (5.3); `blank.ilse.heard`"),
                Talk("Hollow_Isolde", "Blank.ThessalyHollow", new[] { 0, 0 }, Ability.None, "Isolde; \"I'll carry it.\": **keystone.isolde**"),
                Boss("corras_drawing", "Blank.OldCapital", new int[0], Ability.None, "6.13 Corra's Drawing"),
                Talk("Capital_Corra", "Blank.OldCapital", new[] { 0 }, Ability.None, "the small drawing, carried out"),
                Talk("Capital_Corvin", "Blank.OldCapital", new[] { 0 }, Ability.None, "\"Finish the Survey with you.\": `corvin.stance` = 1"),
                Boss("archivist", "Blank.OldCapital", new int[0], Ability.None, "6.14 The Archivist"),
                Talk("Capital_Corvin", "Blank.OldCapital", new int[0], Ability.None, "\"Then draw it better than I did.\": **keystone.archivist**"),
                Talk("Chain_Sable_Tether", "Saltmarrow.LanternChain", new[] { 0 }, Ability.None, "Sable's tether: `saltmarrow.tether`"),
                Talk("Aury_Lighthouse", "Blank.AurysLighthouse", new[] { 0, 0 }, Ability.None, "Aury (5.3); \"I'll take it.\": **keystone.aury**"),
                Talk("Observatory_Frame", "Halden.Observatory", new[] { 0 }, Ability.None, "\"Remake it. All seven.\": **the Fixed World**"),
                Talk("Epilogue_Pell", "Halden.JourneymansHall", new int[0], Ability.None, "the epilogue walk"),
                Talk("Epilogue_Sable", "Saltmarrow.Quay", new int[0], Ability.None, "the epilogue walk"),
                Talk("Epilogue_Marrow", "Blank.ThessalyHollow", new int[0], Ability.None, "the epilogue walk")
            );
            Route(Ending.Open,
                Talk("Quay_Sable", "Saltmarrow.Quay", new[] { 1 }, Ability.None, "Sable: the Guild's haste; `saltmarrow.sable.talked`"),
                Boss("lamp_keeper", "Saltmarrow.LanternChain", new int[0], Ability.Wingbeat, "6.1 The Lamp-Keeper"),
                Talk("Lighthouse_Halvard_Hunt", "Saltmarrow.LanternChain", new[] { 1, 0 }, Ability.None, "Halvard's first hunt: unlicensed"),
                Talk("Quay_Sable", "Saltmarrow.Quay", new[] { 1 }, Ability.None, "\"The third lighthouse. Who keeps it?\" `saltmarrow.sable.aury`"),
                Talk("Rest_Kettil", "Emberdown.KettilsRest", new[] { 0 }, Ability.None, "Kettil counts her in; `emberdown.kettil.met`"),
                Talk("Bell_Runa_Count", "Emberdown.RollCallBell", new[] { 0, 0 }, Ability.None, "counted; \"Teach me.\": the walk (`holdfast.walk_learned`)"),
                Talk("Chimneys_Runa_Climb", "Emberdown.NineChimneys", new[] { 1 }, Ability.Talonhold, "Talonhold; `emberdown.runa.climbed`"),
                Talk("Baths_Kettil_Debate", "Emberdown.CinderBaths", new[] { 0 }, Ability.None, "the debate; `emberdown.debate.heard`"),
                Talk("Rest_Kettil", "Emberdown.KettilsRest", new[] { 0 }, Ability.None, "\"Then let's bring them up.\": `emberdown.hollowvein_opened`"),
                Talk("Hollowvein_Runa_Walk", "Emberdown.Hollowvein", new[] { 0 }, Ability.None, "\"Then we walk it down.\": the long roll-call"),
                Boss("collapse", "Emberdown.Hollowvein", new int[0], Ability.None, "6.4 The Collapse, on the fourth verse"),
                Talk("Hollowvein_Runa_After", "Emberdown.Hollowvein", new int[0], Ability.None, "thirty-one; `emberdown.hollowvein.walked`, **keystone.hollowvein**"),
                Talk("QuietHouse_Teodor", "Verdance.QuietHouse", new[] { 0 }, Ability.None, "Teodor; `verdance.teodor.met`"),
                Talk("RootChapel_Teodor_Thread", "Verdance.RootChapel", new[] { 0 }, Ability.Inkthread, "Inkthread; `verdance.teodor.thread`"),
                Talk("Aldermere_Teodor", "Verdance.Aldermere", new[] { 0 }, Ability.None, "\"Then I'll stay to the end.\": attended, the square released"),
                Talk("QuietHouse_Teodor", "Verdance.QuietHouse", new[] { 1 }, Ability.None, "\"Because they asked. And you made sure.\": **keystone.quiet_house**"),
                Talk("Orchard_Isolde_Cache", "Halden.OldOrchard", new[] { 1 }, Ability.None, "the five names; `isolde.cache`: the road to the Edge"),
                Talk("Edge_Pell_Watch", "Greyfold.RoadThatStops", new[] { 1 }, Ability.Clarity, "she steps in and stays herself: Clarity, `act2.started`"),
                Talk("Hall_Pell_Minder", "Halden.JourneymansHall", new[] { 1 }, Ability.None, "the minder; \"Then don't send it.\" (Warden)"),
                Talk("Bridges_Halvard_Hunt", "Halden.SevenBridges", new[] { 1 }, Ability.None, "the second hunt; \"Stand aside.\" (Warden)"),
                Talk("Hall_Pell_Minder", "Halden.JourneymansHall", new[] { 0 }, Ability.None, "Interlude A: \"Let me read it.\" Two Warden voices: **Pell keeps it**"),
                Talk("Camp_Idrenne", "Windreach.LongGrassCamp", new[] { 0 }, Ability.None, "the first fire; `windreach.idrenne.met`"),
                Talk("River_Idrenne_Night", "Windreach.DryRiver", new[] { 0 }, Ability.None, "the second fire: the stones are a map"),
                Talk("Gate_Idrenne_Leap", "Windreach.WindGate", new[] { 0 }, Ability.Windmemory, "the leap: Windmemory"),
                Talk("Grass_Idrenne_Night", "Windreach.IdrennesFire", new[] { 0 }, Ability.None, "the third fire: known; `windreach.camp.walked`"),
                Talk("Fire_Idrenne", "Windreach.IdrennesFire", new[] { 0, 1 }, Ability.None, "Idrenne's Fire, witnessed; \"I'll leave it undrawn.\"; **keystone.windreach**"),
                Talk("EdgeCamp_Notice", "Greyfold.EdgeCamp", new[] { 0 }, Ability.None, "Voss's notice: `act2.threshold`"),
                Boss("halvard_3", "Greyfold.Threshold", new int[0], Ability.None, "6.3 Halvard's third"),
                Talk("Threshold_Halvard", "Greyfold.Threshold", new int[0], Ability.None, "\"I'll enter it as a survey.\""),
                Talk("Threshold_Voss", "Greyfold.Threshold", new[] { 1 }, Ability.None, "the one speech"),
                Boss("voss", "Greyfold.Threshold", new int[0], Ability.None, "6.11 Voss"),
                Talk("Threshold_Voss", "Greyfold.Threshold", new int[0], Ability.None, "\"Take it in. Go.\": `greyfold.crossed`"),
                Talk("LastCamp_Isolde", "Greyfold.IsoldesLastCamp", new[] { 1 }, Ability.None, "her atlas (5.2): `act3.started`"),
                Talk("Blank_Marrow_Follow", "Blank.ThessalyHollow", new int[0], Ability.None, "the Lantern: Marrow follows"),
                Talk("Hollow_Ilse", "Blank.ThessalyHollow", new[] { 0 }, Ability.None, "Ilse (5.3); `blank.ilse.heard`"),
                Talk("Hollow_Isolde", "Blank.ThessalyHollow", new[] { 0, 0 }, Ability.None, "Isolde; \"I'll carry it.\": **keystone.isolde**"),
                Boss("corras_drawing", "Blank.OldCapital", new int[0], Ability.None, "6.13 Corra's Drawing"),
                Talk("Capital_Corra", "Blank.OldCapital", new[] { 0 }, Ability.None, "the small drawing, carried out"),
                Talk("Capital_Corvin", "Blank.OldCapital", new[] { 1, 0, 0, 0 }, Ability.None, "\"Give them back.\" Ilse, alone, the sky: `corvin.stance` = 2"),
                Boss("archivist", "Blank.OldCapital", new int[0], Ability.None, "6.14 The Archivist"),
                Talk("Capital_Corvin", "Blank.OldCapital", new int[0], Ability.None, "\"Then draw it better than I did.\": **keystone.archivist**"),
                Talk("Observatory_Frame", "Halden.Observatory", new[] { 1 }, Ability.None, "\"Break it. Give them back.\": Runa's chorus"),
                Boss("complete_survey", "Halden.Observatory", new int[0], Ability.Sky, "6.15 The Complete Survey"),
                Talk("Ending_Open_After", "Halden.Observatory", new int[0], Ability.None, "the frame breaks; she flies once: **the Open World**"),
                Talk("Epilogue_Pell", "Halden.JourneymansHall", new int[0], Ability.None, "the epilogue walk"),
                Talk("Epilogue_Runa", "Emberdown.KettilsRest", new int[0], Ability.None, "the epilogue walk"),
                Talk("Epilogue_Marrow", "Blank.ThessalyHollow", new int[0], Ability.None, "the epilogue walk")
            );
            Route(Ending.Unwritten,
                Talk("Quay_Sable", "Saltmarrow.Quay", new[] { 1 }, Ability.None, "Sable: the Guild's haste; `saltmarrow.sable.talked`"),
                Boss("lamp_keeper", "Saltmarrow.LanternChain", new int[0], Ability.Wingbeat, "6.1 The Lamp-Keeper"),
                Talk("Rest_Kettil", "Emberdown.KettilsRest", new[] { 0 }, Ability.None, "Kettil counts her in; `emberdown.kettil.met`"),
                Talk("Bell_Runa_Count", "Emberdown.RollCallBell", new[] { 0, 1 }, Ability.None, "counted; \"Another night.\""),
                Talk("Chimneys_Runa_Climb", "Emberdown.NineChimneys", new[] { 1 }, Ability.Talonhold, "Talonhold"),
                Talk("QuietHouse_Teodor", "Verdance.QuietHouse", new[] { 0 }, Ability.None, "Teodor; `verdance.teodor.met`"),
                Talk("RootChapel_Teodor_Thread", "Verdance.RootChapel", new[] { 0 }, Ability.Inkthread, "Inkthread; `verdance.teodor.thread`"),
                Talk("Aldermere_Teodor", "Verdance.Aldermere", new[] { 0 }, Ability.None, "\"Then I'll stay to the end.\": attended, the square released"),
                Talk("QuietHouse_Teodor", "Verdance.QuietHouse", new[] { 1 }, Ability.None, "\"Because they asked. And you made sure.\": **keystone.quiet_house**"),
                Talk("Orchard_Isolde_Cache", "Halden.OldOrchard", new[] { 1 }, Ability.None, "the five names; `isolde.cache`: the road to the Edge"),
                Talk("Edge_Pell_Watch", "Greyfold.RoadThatStops", new[] { 1 }, Ability.Clarity, "she steps in and stays herself: Clarity, `act2.started`"),
                Talk("Hall_Pell_Minder", "Halden.JourneymansHall", new[] { 1 }, Ability.None, "the minder; \"Then don't send it.\" (Warden)"),
                Talk("Bridges_Halvard_Hunt", "Halden.SevenBridges", new[] { 1 }, Ability.None, "the second hunt; \"Stand aside.\" (Warden)"),
                Talk("Hall_Pell_Minder", "Halden.JourneymansHall", new[] { 0 }, Ability.None, "Interlude A: \"Let me read it.\" Two Warden voices: **Pell keeps it**"),
                Talk("EdgeCamp_Notice", "Greyfold.EdgeCamp", new[] { 0 }, Ability.None, "Voss's notice: `act2.threshold`"),
                Boss("halvard_3", "Greyfold.Threshold", new int[0], Ability.None, "6.3 Halvard's third"),
                Talk("Threshold_Halvard", "Greyfold.Threshold", new int[0], Ability.None, "\"I'll enter it as a survey.\""),
                Talk("Threshold_Voss", "Greyfold.Threshold", new[] { 1 }, Ability.None, "the one speech"),
                Boss("voss", "Greyfold.Threshold", new int[0], Ability.None, "6.11 Voss"),
                Talk("Threshold_Voss", "Greyfold.Threshold", new int[0], Ability.None, "\"Take it in. Go.\": `greyfold.crossed`"),
                Talk("LastCamp_Isolde", "Greyfold.IsoldesLastCamp", new[] { 1 }, Ability.None, "her atlas (5.2): `act3.started`"),
                Talk("Observatory_Frame", "Halden.Observatory", new[] { 2 }, Ability.None, "\"Teodor. They're yours to let go.\": **the Unwritten**"),
                Talk("Epilogue_Pell", "Halden.JourneymansHall", new int[0], Ability.None, "the epilogue walk"),
                Talk("Epilogue_Teodor", "Verdance.QuietHouse", new int[0], Ability.None, "the epilogue walk"),
                Talk("Epilogue_Marrow", "Blank.ThessalyHollow", new int[0], Ability.None, "the epilogue walk")
            );
            Route(Ending.Rest,
                Talk("Quay_Sable", "Saltmarrow.Quay", new[] { 1 }, Ability.None, "Sable: the Guild's haste; `saltmarrow.sable.talked`"),
                Boss("lamp_keeper", "Saltmarrow.LanternChain", new int[0], Ability.Wingbeat, "6.1 The Lamp-Keeper"),
                Talk("Rest_Kettil", "Emberdown.KettilsRest", new[] { 0 }, Ability.None, "Kettil counts her in; `emberdown.kettil.met`"),
                Talk("Bell_Runa_Count", "Emberdown.RollCallBell", new[] { 0, 1 }, Ability.None, "counted; \"Another night.\""),
                Talk("Chimneys_Runa_Climb", "Emberdown.NineChimneys", new[] { 1 }, Ability.Talonhold, "Talonhold"),
                Talk("QuietHouse_Teodor", "Verdance.QuietHouse", new[] { 0 }, Ability.None, "Teodor; `verdance.teodor.met`"),
                Talk("RootChapel_Teodor_Thread", "Verdance.RootChapel", new[] { 0 }, Ability.Inkthread, "Inkthread; `verdance.teodor.thread`"),
                Talk("Orchard_Isolde_Cache", "Halden.OldOrchard", new[] { 1 }, Ability.None, "the five names; `isolde.cache`: the road to the Edge"),
                Talk("Edge_Pell_Watch", "Greyfold.RoadThatStops", new[] { 1 }, Ability.Clarity, "she steps in and stays herself: Clarity, `act2.started`"),
                Talk("Hall_Pell_Minder", "Halden.JourneymansHall", new[] { 1 }, Ability.None, "the minder; \"Then don't send it.\" (Warden)"),
                Talk("Bridges_Halvard_Hunt", "Halden.SevenBridges", new[] { 1 }, Ability.None, "the second hunt; \"Stand aside.\" (Warden)"),
                Talk("Hall_Pell_Minder", "Halden.JourneymansHall", new[] { 0 }, Ability.None, "Interlude A: \"Let me read it.\" Two Warden voices: **Pell keeps it**"),
                Talk("EdgeCamp_Notice", "Greyfold.EdgeCamp", new[] { 0 }, Ability.None, "Voss's notice: `act2.threshold`"),
                Boss("halvard_3", "Greyfold.Threshold", new int[0], Ability.None, "6.3 Halvard's third"),
                Talk("Threshold_Halvard", "Greyfold.Threshold", new int[0], Ability.None, "\"I'll enter it as a survey.\""),
                Talk("Threshold_Voss", "Greyfold.Threshold", new[] { 1 }, Ability.None, "the one speech"),
                Boss("voss", "Greyfold.Threshold", new int[0], Ability.None, "6.11 Voss"),
                Talk("Threshold_Voss", "Greyfold.Threshold", new int[0], Ability.None, "\"Take it in. Go.\": `greyfold.crossed`"),
                Talk("LastCamp_Isolde", "Greyfold.IsoldesLastCamp", new[] { 1 }, Ability.None, "her atlas (5.2): `act3.started`"),
                Talk("Capital_Corvin", "Blank.OldCapital", new[] { 2 }, Ability.None, "\"...\": no stance"),
                Boss("archivist", "Blank.OldCapital", new int[0], Ability.None, "6.14 The Archivist"),
                Talk("Capital_Corvin", "Blank.OldCapital", new[] { 0 }, Ability.None, "no stones: the chair is offered; \"Keep your stone.\""),
                Talk("Capital_Corvin", "Blank.OldCapital", new[] { 2 }, Ability.None, "\"I'll sit. I'll draw with you.\": **the Cartographer's Rest**"),
                Talk("Epilogue_Pell", "Halden.JourneymansHall", new int[0], Ability.None, "the epilogue walk"),
                Talk("Epilogue_Marrow", "Blank.ThessalyHollow", new int[0], Ability.None, "the epilogue walk")
            );
        }
    }
}
