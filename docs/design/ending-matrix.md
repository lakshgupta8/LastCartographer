# Ending Requirement Matrix (DES-12, v1)

Bible 9 as flag logic (`Endings`, written in NAR-13), and the proof that every ending is reachable from a new game:
one route to each, as data (`EndingRoutes`, generated from the same source as this page), replayed through the
shipped Yarn project by `EndingRoutesTests`. Before every step the test proves the step's zone reachable on the macro
map (`WorldGraph.Reachable`) with only the abilities the route has earned so far and the flags the game has actually
written. Fights are won by flag (the late fights have no kits yet); everything else is the real script.

## 1. The matrix

| Ending | Stones | Corvin | The five names | Witnessed | Other |
|---|---|---|---|---|---|
| **Fixed World** (9.1) | 7 at the frame: six carried + the Observatory's own | cooperates (`corvin.stance` = 1) | — | — | — |
| **Open World** (9.2) | ≥ 4 carried | persuaded (`corvin.stance` = 2) | ≥ 3 allied | Idrenne's Fire, and Aldermere attended or Hollowvein walked | then 6.15 |
| **Unwritten** (9.3) | — | — | Teodor (his stone) | — | Aldermere attended, not stopped |
| **Rest** (9.4) | 0 carried | offered the chair (`ending.rest_offered`) | — | — | 0 anchored places |

Allied (endings.md §1): Sable by the tether or the walk; Runa by the name or Hollowvein; Teodor by his stone; Idrenne
by the three fires; Pell by keeping the report.

## 2. What closes what

From a state where every ending's requirements are met at once (impossible in play, since Corvin has one stance,
but useful as the matrix's origin), flipping one decision:

| Decision | Closes | Leaves |
|---|---|---|
| Corvin cooperates (stance 1) | Open | Fixed (with seven), Unwritten, Rest |
| Corvin persuaded (stance 2) | Fixed | Open, Unwritten, Rest |
| Corvin unpersuaded (stance 3) | Fixed, Open | Unwritten, Rest |
| Stop Aldermere's last day | Unwritten; Open unless Hollowvein was walked | Fixed, Rest |
| Skip Idrenne's Fire | Open | the rest |
| Teodor refuses the stone | Unwritten; Fixed (six is not seven) | Open (with four others), Rest |
| Pell sends the report | nothing by itself (Pell is one of five) | all |
| Anchor any place | Rest | all others |
| Carry any stone | Rest | all others |
| Hale finishes the Steppe | nothing | all (the Steppe's fate is the Blank's, not the frame's) |

`EndingMatrixTests` holds this table.

## 3. The routes

Choices are option indices, in the order the scene offers them; a step with several plays them in sequence. The
test takes the option named and fails if it is dimmed, so a route also proves its choices are open when it reaches
them. Bold flags are keystones.

### The Fixed World
Both climbs, Windreach, the Threshold, the Hollow, Corvin's cooperation, Aury by tether: all six carried stones.
<!-- route:Fixed -->
| # | Step | Where | Choices | Does |
|---|---|---|---|---|
| 1 | `Quay_Sable` | Saltmarrow.Quay | 1 | Sable: the Guild's haste; `saltmarrow.sable.talked` |
| 2 | **fight** lamp_keeper | Saltmarrow.LanternChain | — | 6.1 The Lamp-Keeper → *Wingbeat* |
| 3 | `Lighthouse_Halvard_Hunt` | Saltmarrow.LanternChain | 1, 0 | Halvard's first hunt: unlicensed |
| 4 | `Quay_Sable` | Saltmarrow.Quay | 1 | "The third lighthouse. Who keeps it?" `saltmarrow.sable.aury` |
| 5 | `Rest_Kettil` | Emberdown.KettilsRest | 0 | Kettil counts her in; `emberdown.kettil.met` |
| 6 | `Bell_Runa_Count` | Emberdown.RollCallBell | 0, 0 | counted; "Teach me.": the walk (`holdfast.walk_learned`) |
| 7 | `Chimneys_Runa_Climb` | Emberdown.NineChimneys | 1 | Talonhold; `emberdown.runa.climbed` → *Talonhold* |
| 8 | `Baths_Kettil_Debate` | Emberdown.CinderBaths | 0 | the debate; `emberdown.debate.heard` |
| 9 | `Rest_Kettil` | Emberdown.KettilsRest | 0 | "Then let's bring them up.": `emberdown.hollowvein_opened` |
| 10 | `Hollowvein_Runa_Walk` | Emberdown.Hollowvein | 0 | "Then we walk it down.": the long roll-call |
| 11 | **fight** collapse | Emberdown.Hollowvein | — | 6.4 The Collapse, on the fourth verse |
| 12 | `Hollowvein_Runa_After` | Emberdown.Hollowvein | — | thirty-one; `emberdown.hollowvein.walked`, **keystone.hollowvein** |
| 13 | `QuietHouse_Teodor` | Verdance.QuietHouse | 0 | Teodor; `verdance.teodor.met` |
| 14 | `RootChapel_Teodor_Thread` | Verdance.RootChapel | 0 | Inkthread; `verdance.teodor.thread` → *Inkthread* |
| 15 | `Aldermere_Teodor` | Verdance.Aldermere | 0 | "Then I'll stay to the end.": attended, the square released |
| 16 | `QuietHouse_Teodor` | Verdance.QuietHouse | 1 | "Because they asked. And you made sure.": **keystone.quiet_house** |
| 17 | `Orchard_Isolde_Cache` | Halden.OldOrchard | 1 | the five names; `isolde.cache`: the road to the Edge |
| 18 | `Edge_Pell_Watch` | Greyfold.RoadThatStops | 1 | she steps in and stays herself: Clarity, `act2.started` → *Clarity* |
| 19 | `Hall_Pell_Minder` | Halden.JourneymansHall | 1 | the minder; "Then don't send it." (Warden) |
| 20 | `Bridges_Halvard_Hunt` | Halden.SevenBridges | 1 | the second hunt; "Stand aside." (Warden) |
| 21 | `Hall_Pell_Minder` | Halden.JourneymansHall | 0 | Interlude A: "Let me read it." Two Warden voices: **Pell keeps it** |
| 22 | `Camp_Idrenne` | Windreach.LongGrassCamp | 0 | the first fire; `windreach.idrenne.met` |
| 23 | `River_Idrenne_Night` | Windreach.DryRiver | 0 | the second fire: the stones are a map |
| 24 | `Gate_Idrenne_Leap` | Windreach.WindGate | 0 | the leap: Windmemory → *Windmemory* |
| 25 | `Grass_Idrenne_Night` | Windreach.IdrennesFire | 0 | the third fire: known; `windreach.camp.walked` |
| 26 | `Fire_Idrenne` | Windreach.IdrennesFire | 0, 1 | Idrenne's Fire, witnessed; "I'll leave it undrawn."; **keystone.windreach** |
| 27 | `EdgeCamp_Notice` | Greyfold.EdgeCamp | 0 | Voss's notice: `act2.threshold` |
| 28 | **fight** halvard_3 | Greyfold.Threshold | — | 6.3 Halvard's third |
| 29 | `Threshold_Halvard` | Greyfold.Threshold | — | "I'll enter it as a survey." |
| 30 | `Threshold_Voss` | Greyfold.Threshold | 1 | the one speech |
| 31 | **fight** voss | Greyfold.Threshold | — | 6.11 Voss |
| 32 | `Threshold_Voss` | Greyfold.Threshold | — | "Take it in. Go.": `greyfold.crossed` |
| 33 | `LastCamp_Isolde` | Greyfold.IsoldesLastCamp | 1 | her atlas (5.2): `act3.started` |
| 34 | `Blank_Marrow_Follow` | Blank.ThessalyHollow | — | the Lantern: Marrow follows |
| 35 | `Hollow_Ilse` | Blank.ThessalyHollow | 0 | Ilse (5.3); `blank.ilse.heard` |
| 36 | `Hollow_Isolde` | Blank.ThessalyHollow | 0, 0 | Isolde; "I'll carry it.": **keystone.isolde** |
| 37 | **fight** corras_drawing | Blank.OldCapital | — | 6.13 Corra's Drawing |
| 38 | `Capital_Corra` | Blank.OldCapital | 0 | the small drawing, carried out |
| 39 | `Capital_Corvin` | Blank.OldCapital | 0 | "Finish the Survey with you.": `corvin.stance` = 1 |
| 40 | **fight** archivist | Blank.OldCapital | — | 6.14 The Archivist |
| 41 | `Capital_Corvin` | Blank.OldCapital | — | "Then draw it better than I did.": **keystone.archivist** |
| 42 | `Chain_Sable_Tether` | Saltmarrow.LanternChain | 0 | Sable's tether: `saltmarrow.tether` |
| 43 | `Aury_Lighthouse` | Blank.AurysLighthouse | 0, 0 | Aury (5.3); "I'll take it.": **keystone.aury** |
| 44 | `Observatory_Frame` | Halden.Observatory | 0 | "Remake it. All seven.": **the Fixed World** |
| 45 | `Epilogue_Pell` | Halden.JourneymansHall | — | the epilogue walk |
| 46 | `Epilogue_Sable` | Saltmarrow.Quay | — | the epilogue walk |
| 47 | `Epilogue_Marrow` | Blank.ThessalyHollow | — | the epilogue walk |
<!-- /route -->

### The Open World
The same road, with Corvin argued round (5.3, 5.4, 5.6) and no need of Aury: five stones, four allies, the Fire and
Hollowvein and Aldermere both.
<!-- route:Open -->
| # | Step | Where | Choices | Does |
|---|---|---|---|---|
| 1 | `Quay_Sable` | Saltmarrow.Quay | 1 | Sable: the Guild's haste; `saltmarrow.sable.talked` |
| 2 | **fight** lamp_keeper | Saltmarrow.LanternChain | — | 6.1 The Lamp-Keeper → *Wingbeat* |
| 3 | `Lighthouse_Halvard_Hunt` | Saltmarrow.LanternChain | 1, 0 | Halvard's first hunt: unlicensed |
| 4 | `Quay_Sable` | Saltmarrow.Quay | 1 | "The third lighthouse. Who keeps it?" `saltmarrow.sable.aury` |
| 5 | `Rest_Kettil` | Emberdown.KettilsRest | 0 | Kettil counts her in; `emberdown.kettil.met` |
| 6 | `Bell_Runa_Count` | Emberdown.RollCallBell | 0, 0 | counted; "Teach me.": the walk (`holdfast.walk_learned`) |
| 7 | `Chimneys_Runa_Climb` | Emberdown.NineChimneys | 1 | Talonhold; `emberdown.runa.climbed` → *Talonhold* |
| 8 | `Baths_Kettil_Debate` | Emberdown.CinderBaths | 0 | the debate; `emberdown.debate.heard` |
| 9 | `Rest_Kettil` | Emberdown.KettilsRest | 0 | "Then let's bring them up.": `emberdown.hollowvein_opened` |
| 10 | `Hollowvein_Runa_Walk` | Emberdown.Hollowvein | 0 | "Then we walk it down.": the long roll-call |
| 11 | **fight** collapse | Emberdown.Hollowvein | — | 6.4 The Collapse, on the fourth verse |
| 12 | `Hollowvein_Runa_After` | Emberdown.Hollowvein | — | thirty-one; `emberdown.hollowvein.walked`, **keystone.hollowvein** |
| 13 | `QuietHouse_Teodor` | Verdance.QuietHouse | 0 | Teodor; `verdance.teodor.met` |
| 14 | `RootChapel_Teodor_Thread` | Verdance.RootChapel | 0 | Inkthread; `verdance.teodor.thread` → *Inkthread* |
| 15 | `Aldermere_Teodor` | Verdance.Aldermere | 0 | "Then I'll stay to the end.": attended, the square released |
| 16 | `QuietHouse_Teodor` | Verdance.QuietHouse | 1 | "Because they asked. And you made sure.": **keystone.quiet_house** |
| 17 | `Orchard_Isolde_Cache` | Halden.OldOrchard | 1 | the five names; `isolde.cache`: the road to the Edge |
| 18 | `Edge_Pell_Watch` | Greyfold.RoadThatStops | 1 | she steps in and stays herself: Clarity, `act2.started` → *Clarity* |
| 19 | `Hall_Pell_Minder` | Halden.JourneymansHall | 1 | the minder; "Then don't send it." (Warden) |
| 20 | `Bridges_Halvard_Hunt` | Halden.SevenBridges | 1 | the second hunt; "Stand aside." (Warden) |
| 21 | `Hall_Pell_Minder` | Halden.JourneymansHall | 0 | Interlude A: "Let me read it." Two Warden voices: **Pell keeps it** |
| 22 | `Camp_Idrenne` | Windreach.LongGrassCamp | 0 | the first fire; `windreach.idrenne.met` |
| 23 | `River_Idrenne_Night` | Windreach.DryRiver | 0 | the second fire: the stones are a map |
| 24 | `Gate_Idrenne_Leap` | Windreach.WindGate | 0 | the leap: Windmemory → *Windmemory* |
| 25 | `Grass_Idrenne_Night` | Windreach.IdrennesFire | 0 | the third fire: known; `windreach.camp.walked` |
| 26 | `Fire_Idrenne` | Windreach.IdrennesFire | 0, 1 | Idrenne's Fire, witnessed; "I'll leave it undrawn."; **keystone.windreach** |
| 27 | `EdgeCamp_Notice` | Greyfold.EdgeCamp | 0 | Voss's notice: `act2.threshold` |
| 28 | **fight** halvard_3 | Greyfold.Threshold | — | 6.3 Halvard's third |
| 29 | `Threshold_Halvard` | Greyfold.Threshold | — | "I'll enter it as a survey." |
| 30 | `Threshold_Voss` | Greyfold.Threshold | 1 | the one speech |
| 31 | **fight** voss | Greyfold.Threshold | — | 6.11 Voss |
| 32 | `Threshold_Voss` | Greyfold.Threshold | — | "Take it in. Go.": `greyfold.crossed` |
| 33 | `LastCamp_Isolde` | Greyfold.IsoldesLastCamp | 1 | her atlas (5.2): `act3.started` |
| 34 | `Blank_Marrow_Follow` | Blank.ThessalyHollow | — | the Lantern: Marrow follows |
| 35 | `Hollow_Ilse` | Blank.ThessalyHollow | 0 | Ilse (5.3); `blank.ilse.heard` |
| 36 | `Hollow_Isolde` | Blank.ThessalyHollow | 0, 0 | Isolde; "I'll carry it.": **keystone.isolde** |
| 37 | **fight** corras_drawing | Blank.OldCapital | — | 6.13 Corra's Drawing |
| 38 | `Capital_Corra` | Blank.OldCapital | 0 | the small drawing, carried out |
| 39 | `Capital_Corvin` | Blank.OldCapital | 1, 0, 0, 0 | "Give them back." Ilse, alone, the sky: `corvin.stance` = 2 |
| 40 | **fight** archivist | Blank.OldCapital | — | 6.14 The Archivist |
| 41 | `Capital_Corvin` | Blank.OldCapital | — | "Then draw it better than I did.": **keystone.archivist** |
| 42 | `Observatory_Frame` | Halden.Observatory | 1 | "Break it. Give them back.": Runa's chorus |
| 43 | **fight** complete_survey | Halden.Observatory | — | 6.15 The Complete Survey → *Sky* |
| 44 | `Ending_Open_After` | Halden.Observatory | — | the frame breaks; she flies once: **the Open World** |
| 45 | `Epilogue_Pell` | Halden.JourneymansHall | — | the epilogue walk |
| 46 | `Epilogue_Runa` | Emberdown.KettilsRest | — | the epilogue walk |
| 47 | `Epilogue_Marrow` | Blank.ThessalyHollow | — | the epilogue walk |
<!-- /route -->

### The Unwritten
The shortest: Emberdown only for Talonhold (the flyer-tower wants both climbs' abilities), the Verdance for Teodor
and Aldermere, Halden, the Threshold, and straight to the frame. No Windreach, no Hollow.
<!-- route:Unwritten -->
| # | Step | Where | Choices | Does |
|---|---|---|---|---|
| 1 | `Quay_Sable` | Saltmarrow.Quay | 1 | Sable: the Guild's haste; `saltmarrow.sable.talked` |
| 2 | **fight** lamp_keeper | Saltmarrow.LanternChain | — | 6.1 The Lamp-Keeper → *Wingbeat* |
| 3 | `Rest_Kettil` | Emberdown.KettilsRest | 0 | Kettil counts her in; `emberdown.kettil.met` |
| 4 | `Bell_Runa_Count` | Emberdown.RollCallBell | 0, 1 | counted; "Another night." |
| 5 | `Chimneys_Runa_Climb` | Emberdown.NineChimneys | 1 | Talonhold → *Talonhold* |
| 6 | `QuietHouse_Teodor` | Verdance.QuietHouse | 0 | Teodor; `verdance.teodor.met` |
| 7 | `RootChapel_Teodor_Thread` | Verdance.RootChapel | 0 | Inkthread; `verdance.teodor.thread` → *Inkthread* |
| 8 | `Aldermere_Teodor` | Verdance.Aldermere | 0 | "Then I'll stay to the end.": attended, the square released |
| 9 | `QuietHouse_Teodor` | Verdance.QuietHouse | 1 | "Because they asked. And you made sure.": **keystone.quiet_house** |
| 10 | `Orchard_Isolde_Cache` | Halden.OldOrchard | 1 | the five names; `isolde.cache`: the road to the Edge |
| 11 | `Edge_Pell_Watch` | Greyfold.RoadThatStops | 1 | she steps in and stays herself: Clarity, `act2.started` → *Clarity* |
| 12 | `Hall_Pell_Minder` | Halden.JourneymansHall | 1 | the minder; "Then don't send it." (Warden) |
| 13 | `Bridges_Halvard_Hunt` | Halden.SevenBridges | 1 | the second hunt; "Stand aside." (Warden) |
| 14 | `Hall_Pell_Minder` | Halden.JourneymansHall | 0 | Interlude A: "Let me read it." Two Warden voices: **Pell keeps it** |
| 15 | `EdgeCamp_Notice` | Greyfold.EdgeCamp | 0 | Voss's notice: `act2.threshold` |
| 16 | **fight** halvard_3 | Greyfold.Threshold | — | 6.3 Halvard's third |
| 17 | `Threshold_Halvard` | Greyfold.Threshold | — | "I'll enter it as a survey." |
| 18 | `Threshold_Voss` | Greyfold.Threshold | 1 | the one speech |
| 19 | **fight** voss | Greyfold.Threshold | — | 6.11 Voss |
| 20 | `Threshold_Voss` | Greyfold.Threshold | — | "Take it in. Go.": `greyfold.crossed` |
| 21 | `LastCamp_Isolde` | Greyfold.IsoldesLastCamp | 1 | her atlas (5.2): `act3.started` |
| 22 | `Observatory_Frame` | Halden.Observatory | 2 | "Teodor. They're yours to let go.": **the Unwritten** |
| 23 | `Epilogue_Pell` | Halden.JourneymansHall | — | the epilogue walk |
| 24 | `Epilogue_Teodor` | Verdance.QuietHouse | — | the epilogue walk |
| 25 | `Epilogue_Marrow` | Blank.ThessalyHollow | — | the epilogue walk |
<!-- /route -->

### The Cartographer's Rest
Nothing carried, nothing anchored: the climbs for their abilities, the Threshold, and Corvin's chair.
<!-- route:Rest -->
| # | Step | Where | Choices | Does |
|---|---|---|---|---|
| 1 | `Quay_Sable` | Saltmarrow.Quay | 1 | Sable: the Guild's haste; `saltmarrow.sable.talked` |
| 2 | **fight** lamp_keeper | Saltmarrow.LanternChain | — | 6.1 The Lamp-Keeper → *Wingbeat* |
| 3 | `Rest_Kettil` | Emberdown.KettilsRest | 0 | Kettil counts her in; `emberdown.kettil.met` |
| 4 | `Bell_Runa_Count` | Emberdown.RollCallBell | 0, 1 | counted; "Another night." |
| 5 | `Chimneys_Runa_Climb` | Emberdown.NineChimneys | 1 | Talonhold → *Talonhold* |
| 6 | `QuietHouse_Teodor` | Verdance.QuietHouse | 0 | Teodor; `verdance.teodor.met` |
| 7 | `RootChapel_Teodor_Thread` | Verdance.RootChapel | 0 | Inkthread; `verdance.teodor.thread` → *Inkthread* |
| 8 | `Orchard_Isolde_Cache` | Halden.OldOrchard | 1 | the five names; `isolde.cache`: the road to the Edge |
| 9 | `Edge_Pell_Watch` | Greyfold.RoadThatStops | 1 | she steps in and stays herself: Clarity, `act2.started` → *Clarity* |
| 10 | `Hall_Pell_Minder` | Halden.JourneymansHall | 1 | the minder; "Then don't send it." (Warden) |
| 11 | `Bridges_Halvard_Hunt` | Halden.SevenBridges | 1 | the second hunt; "Stand aside." (Warden) |
| 12 | `Hall_Pell_Minder` | Halden.JourneymansHall | 0 | Interlude A: "Let me read it." Two Warden voices: **Pell keeps it** |
| 13 | `EdgeCamp_Notice` | Greyfold.EdgeCamp | 0 | Voss's notice: `act2.threshold` |
| 14 | **fight** halvard_3 | Greyfold.Threshold | — | 6.3 Halvard's third |
| 15 | `Threshold_Halvard` | Greyfold.Threshold | — | "I'll enter it as a survey." |
| 16 | `Threshold_Voss` | Greyfold.Threshold | 1 | the one speech |
| 17 | **fight** voss | Greyfold.Threshold | — | 6.11 Voss |
| 18 | `Threshold_Voss` | Greyfold.Threshold | — | "Take it in. Go.": `greyfold.crossed` |
| 19 | `LastCamp_Isolde` | Greyfold.IsoldesLastCamp | 1 | her atlas (5.2): `act3.started` |
| 20 | `Capital_Corvin` | Blank.OldCapital | 2 | "...": no stance |
| 21 | **fight** archivist | Blank.OldCapital | — | 6.14 The Archivist |
| 22 | `Capital_Corvin` | Blank.OldCapital | 0 | no stones: the chair is offered; "Keep your stone." |
| 23 | `Capital_Corvin` | Blank.OldCapital | 2 | "I'll sit. I'll draw with you.": **the Cartographer's Rest** |
| 24 | `Epilogue_Pell` | Halden.JourneymansHall | — | the epilogue walk |
| 25 | `Epilogue_Marrow` | Blank.ThessalyHollow | — | the epilogue walk |
<!-- /route -->

## 4. What this proves, and does not
- Each ending has at least one path from the shore that the map allows and the scripts write. The order of the
  regions on a route is one order; the map allows others (RoomPlanTests, WorldGraphTests).
- Every choice a route takes is open when it is taken (no dimmed option is needed).
- It does not prove the routes are the only ways, or that a player who has done something else is not locked out.
  The frame reads flags, not history, and Corvin's chair can be come back to (endings.md §1), so the known locks
  are the ones in §2: stopping Aldermere (the Unwritten) and Hale's survey (nothing).
- Fights are flags. When the late fights exist (CMB-14, CMB-15), the routes' `Boss` steps should play them.

## 5. Open
- ~~PRO-05's full-playthrough matrix.~~ Built (`docs/design/playthrough-matrix.md`): the routes' legs in every
  order the map allows, and the soft gaps' breaks, replayed.
- A route that sends the report, to prove Oriel's branch (6.8) does not lock the Threshold.
- The Rest's "zero anchors" counts `Places`. A player who seals the quay at its desk in Act 1 has closed the Rest
  before they know it exists. Bible 9.4 wants that; the design should say so somewhere the player can find.
