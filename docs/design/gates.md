# The gates pass (v1)

The macro map's gates (world-map.md §2) read by the built rooms, which until now ignored them: every transition
carries the gate the plans and the map put on its way, a shut way has a bar in it, the Threshold's Wardens lower
their lances on Halvard's word, and a gap with nothing under it is a fall and not a death. No new row: this is the
rooms' part of DES-07 and PRG-07, the Wardens' part of PRG-13 and the fall's part of PRG-17.

Runtime: `Gate` and `Gates` (Core, pure), `RoomTransition` and `GateBar`, `FallCatch`, `Warden.StandDownFlag`
(World). Builder: `ProjectSetup.MakeTransition` and `MakeGateBar`, `MakeRoom`, `RoomRecipe.Warden(x, standDown)`.
Tests: `GatesTests` (edit, 6) and `GatesPlayTests` (play, 4).

## 1. The rule

A gate is an ability, a story flag, or both, and whether skill may cross it without the ability (`Gate`):

| | Open when | Bars while shut |
|---|---|---|
| **Soft ability gate** (every Wingbeat gap) | always: a skilled pogo crosses it, and the game notices (`SequenceBreaks`) | never |
| **Hard ability gate** (Talonhold shafts, Inkthread anchors, Windmemory updrafts, Clarity fades) | she has the ability | yes |
| **Story flag** | the flag is set | yes; hard, never soft |
| **Both** (Hollowvein: Talonhold and `emberdown.hollowvein_opened`) | both | yes |

A gate applies in both directions, so the same gate stands at both ends of a way. Where a way's gate comes from
(`Gates.Between(from, to)`): a planned room's exit (`RoomPlans`), matched by the target's id or, for an exit onto a
zone, the target's zone; the coast's rooms, built before the plans, take the macro map's link between their zones
(`WorldGraph`); two rooms in one zone with no planned gate have none. The Edge's two doors are shut while the prologue
plays (`prologue.crossed`, one way: back into the Edge from the camp is always open, since she returns at Act 1's end).

## 2. What stands in a shut way

The builder puts the gate on the transition (`Needs`, `Flag`, `Soft`) and, where the gate bars, a `Bar` under it:
solid ground the size of the trigger, pale as unwritten paper. At a side door it is a wall; over a climb a slab; in a
drop, the floor the gap is missing. The transition binds to the world's `FlagChanged` (abilities are flags too, so a
lesson opens a shaft the moment it is learned) and rebinds on load; `Apply` stands or removes the bar. Reaching a shut
trigger, or pressing on the bar (`GateBar` watches for her, since her body is kinematic and reports no collisions),
says one line: `Not without Talonhold.` for an ability she lacks, else `The way is shut. Not yet.`; once each time she
comes against it. A soft gap has no bar and no line.

Seventy-odd planned ways carry a gate and forty-odd bar. The story flags at doors: `act2.started` (Lowmarket's south
road), `isolde.cache` (the orchard road to the Edge), `act2.threshold` and `greyfold.crossed` (the Threshold),
`act3.started` (the Lantern, the Observatory, Aury's climb), `halden.vault_opened`, `emberdown.hollowvein_opened`,
`saltmarrow.tether` (Aury's causeway from the faded third), `prologue.crossed` (the Edge). Every story flag on the map
is set by a script (threshold.md §1); none is left to a door.

## 3. The Threshold's line

Threshold_1's three Wardens carry `StandDownFlag = act2.halvard_third`, the flag `Threshold_Halvard` sets after his
third fight ("Wardens. Lances down. This is a survey now. Nobody counts her."). Stood down, a Warden is not hostile
whatever the Guild's stance, counts nobody, and stands where he is, turning to watch her pass. Struck, he still
answers: provoked outranks stood down. Oriel's stand-down (`halden.oriel.stood_down`) is the Guild's and separate.

## 4. A gap with nothing under it

Every built room has a `FallCatch` four units below its camera bounds, wider than the room. Wren falling into it loses
a mask, never the last, and is put back on the last ground she stood on in this room, or, before she has stood
anywhere, the nearest spawn (`Back to solid ground.`, the gauntlet's line). It is not a death: nothing drops, no return
to the desk. A Down exit, the shallows, a white patch and a gauntlet's own hazard all sit above it and take her first.

## 5. Verification

| Check | Where |
|---|---|
| the rule reads a planned exit both ways, an external exit by zone, the coast's rooms by the map's link, nothing inside a zone, the Edge's prologue door one way; open or shut by kit and flags | `GatesTests.TheRuleReadsThePlanAndTheMap` |
| every planned way's flag and ability are on its scene's transition; one bar per shut way, none for a soft gap, every bar knows its gate | `EveryPlannedWayCarriesItsGateAndOnlyAShutWayHasABar` |
| the coast: the whale's gap and the climb soft and unbarred, the Quay open, the Edge's two doors on the prologue | `TheCoastsWaysTakeTheMapsLinks` |
| every story flag on the macro map stands at a built door, at both ends | `EveryStoryFlagOnTheMapStandsAtADoor` |
| the Threshold's three carry Halvard's flag, nobody else does, the scene sets it | `TheThresholdsWardensReadHalvardsWord` |
| one catch under every one of the 114 rooms | `EveryRoomHasACatchUnderIt` |
| Lowmarket's south road bars and says so, opens on `act2.started`, the Steppe's side is the same gate | `GatesPlayTests.AStoryGateBarsTheWayAndSaysSoUntilItsFlag` |
| the first chimney's shaft opens the moment Talonhold is learned; the whale's gap has no bar | `AnAbilityGateOpensWhenSheLearnsTheAbilityAndASoftGapNeverBars` |
| the line hunts her unlicensed and stands down on the flag, standing still | `TheThresholdsWardensLowerTheirLancesOnHalvardsWord` |
| a fall past the Bone Bridge's bottom costs a mask and returns her, no death | `AFallPastTheRoomsBottomCostsAMaskAndReturnsHer` |

Version one ran EditMode 303/303 and PlayMode 339/339 after the rebuild. Captures reviewed by eye: Lowmarket's south
road shut and open, the mine mouth plugged and open, the first chimney's slab, the Edge's west door shut, the
Threshold's line stood down (`logs/gates-shot-*.png`). `OWSBG_SHOT_WORLD="act2.started=1"` on a capture applies the
gates as it does the dressing.

## 6. Open

- **The bar is a greybox block.** The hand pass draws what shuts each way: the Guild's fence, boards over the mine
  mouth, the Vault's door; a shaft without Talonhold needs no drawing, only the shaft.
- **The door askers** (the chapel's, the ninth's) open as drawings, not as doors (environment-props.md §6), by decision.
- **The runtime rooms** (arenas, islands) have no catch under them and no gates; their exits are the drift's and the
  fights'.
- **Hale among the line**, if he finished (threshold.md §5).
