# Anchor, Hold, Release (DES-03, v1)

The regional decision (bible 1.3, 3.1, 3.4, 10; GDD 2): what Wren does about a place once she has walked it.
Runtime: `Places` (Core), `HeldState` and `Warden` (World), `<<anchor>>` / `<<hold>>` / `<<release>>` (Yarn), the
desk's place row (UI).

## 1. The three fates

| Fate | Who | Fading | What the player sees afterwards |
|---|---|---|---|
| **Anchored** | The Guild's way: survey, bind, seal | stops | The **held state**: the colour grade locks (cool, brass-blue), schedules loop (`#still` lines), stock never varies, **Wardens patrol**. |
| **Held** | The Holdfast's way: the people hold it by ritual (bounds-walk, roll-call) | stops | Life goes on. No Wardens, no lock. Only works for places small enough that everyone knows everyone. |
| **Released** | Let go | continues on story beats | The place becomes an island in the Blank (PRG-20) and its people react to how Wren left them. |

A fourth state, **Unwritten**, is every place before a decision: fading on story beats, nothing else.

## 2. Rules
- A fate is **decided once and final**. The story may comment on it forever; it never changes.
- Deciding is a **story beat**: written from Yarn in the scene where it happens, or from the drafting desk for
  places Wren seals herself. The desk requires every vantage in the place surveyed; the bind (a true memory from
  a resident) is a story requirement the desk does not yet check (open).
- Anchored and Held places **stop fading** (`FadeStages.Advance` refuses). Released places fade on.
- Wardens hunt unlicensed surveyors: from the end of Act 1 they are hostile to Wren in every anchored town.
- Endings count fates: the Cartographer's Rest needs zero anchors; the Unwritten ending's world shrinks to what is
  held.

## 3. The held look
`HeldState` on a room animates a global `_OWSBG_Held` 0→1 over 2 s when its place is anchored (and back to 0 when
the room unloads). The paper-grain pass reads it: 35% desaturation and a cool brass-blue cast. Real art will add
the locked time of day and looping props (ENV-09).

## 4. Wardens
`Warden : Enemy`, long-leg family. Patrols, turns at walls and edges. Wren in front within 3.2 units: lowers the
lance (14 frames), thrusts a 2.4-unit box for 6 frames, recovers 30, then 1.4 s before the next. Body contact
also hurts. 5 health. Answer: Parry (the sighting lens). Placed in the room inactive; `HeldState` switches them on.

## 5. Story hooks
```yarn
<<anchor Saltmarrow_A>>   <<hold Emberdown_Rest>>   <<release Verdance_Aldermere>>
<<if place_fate("Saltmarrow_A") == "anchored">>
```
Code: `Places.Anchor / Hold / Release / Decide`, `Places.FateOf`, `Places.Count(world, fate)`.

## 6. WorldState
`place.<id>.fate` = 0 unwritten, 1 anchored, 2 held, 3 released. `WorldState.AnchoredPlaces` mirrors the anchored
set. Saves need nothing extra.

## 7. Greybox
- Room A and B carry a `HeldState` with two Wardens each, inactive until anchored.
- The desk's **Place** row proposes anchor / hold / release for the current room and seals on J once every vantage
  in the room is surveyed. Sable notices an anchored Saltmarrow.

## 8. Open
- The bind requirement at the desk (a memory from a resident), and whether Wren can anchor without a licence.
- Hold is performed by the bounds-walk (`docs/design/bounds-walk.md`, DES-13): the desk refuses Hold until the place is walked.
- Wardens before Act 1's end: neutral, or absent.
- Whether Release should advance the fade one stage on the spot (the story says it continues, not that it jumps).
