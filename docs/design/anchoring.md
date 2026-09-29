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
  places Wren seals herself. The desk requires every vantage in the place surveyed. To **anchor**, it also asks
  for the bind: a true memory from someone who lives there, carried now (§9). To **hold**, it asks for the bounds
  walked. **Release** asks for nothing but the survey.
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

**Licence** (`Licence`, Core). Until Halvard's count at the lit lamp (`act1.unlicensed`, written by
`Lighthouse_Halvard_Hunt`), a Warden **measures** a journeyman: stops in front of her for 40 frames, looks away,
and does not measure again for 4 s; no lance, and touching him does not hurt (the first time, a caption). From
the count on he is hostile everywhere. Pell's report, if sent (`pell.report_sent`), sets them on her too. Oriel,
who fights her only once she has read it, stands them down if beaten without a mask lost
(`halden.oriel.stood_down`, boss 6.8), and her word outranks the report. Striking a Warden
provokes that one for the rest of the fight, papers or no. Yarn: `unlicensed()`.

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
- ~~Whether Wren can anchor without a licence.~~ She can't (decided 2026-09-29): the Guild's seal needs the Guild's
  licence (§9).
- ~~Whether sealing should spend the memory.~~ It doesn't: she keeps it, to lose to a death and recover, or to give
  to a door or a bird, which loosens the seal (`docs/design/offerings.md`).
- Hold is performed by the bounds-walk (`docs/design/bounds-walk.md`, DES-13): the desk refuses Hold until the place is walked.
- Wardens before Act 1's end measure and look away (above). Whether some should be absent instead (a hub with
  none until the town is anchored) is a per-town choice for DES-09 to DES-11.
- Whether Release should advance the fade one stage on the spot (the story says it continues, not that it jumps).

## 9. The bind

Anchoring is survey, bind, seal (bible 1.3). The desk checks the middle step with `Memories.BoundFor(world, place)`:
a memory Wren carries whose giver lives in the place (`Memories.HomeOf`).

| Memory | Given by | Home | Where it's given |
|---|---|---|---|
| `dotha.nine_songs` | Dotha | `Saltmarrow_B`, Merrow's End | "Write it as it was" (`Merrow_Dotha_Season`) |
| `sable.boats_back` | Sable | `Saltmarrow_A`, the Drowned Quay | once the widow is decided (`Quay_Sable_Widow`) |
| `isolde.first_sight` | Isolde | none: she lives nowhere now | the prologue's lesson |

- **No memory, no anchor.** The desk refuses, and the blurb says "Bind a true memory from someone who lives here
  first." With one, it names the memory it binds.
- **Carried means carried.** A memory lost to a death is in the smudge, not in her ink. The desk refuses until she
  has struck the smudge down.
- **The licence.** An unlicensed cartographer can't anchor at the desk (`Licence.MayAnchor`): "Unlicensed. The
  Guild's seal needs the Guild's licence. Hold it, or let it go." Halvard's count at the lit lamp revokes her, so Wren
  anchors only on the coast before the fourth light. After that, anchoring is the Guild's own (Lowmarket) and hers is
  the holding or the letting go.
- **Sealing doesn't spend it.** Giving it away later does, and loosens the seal a stage (`docs/design/offerings.md`).
- **A place with nobody to give one can't be anchored by Wren:** the lighthouses, the Salt Chapel, the Edge. She can
  still release them.
- **Story anchors don't ask.** The Guild anchors Lowmarket in the strike scene itself (`<<anchor>>`), and the bind is
  the Guild's business.
- Tests: `MemoriesTests.AMemoryBelongsToWhereItsGiverLives` (edit), and
  `AnchoringTests.DeskAnchorsOnlyWithAMemoryFromThePlace` (play).
