# Bounds-Walk (DES-13, v1)

The Holdfast's way of holding a place (bible 3.4, 10; combat doc 10): a rhythm traversal with a chorus. It is
how **Hold** (anchoring.md) is actually performed. Runtime: `BoundsWalk` (World), `BoundsWalks` (Core),
`WalkView` (UI), `<<walk id>>` and `walked()` (Yarn).

## 1. Rules
- A walk is **verses**; a verse is **beats**; every beat names a **bound**: a spot in the room with a name the
  people use ("Dotha's stoop", "the tether-post"). The chorus calls the name half a beat ahead; when the beat
  lands Wren must be **standing in the bound** (a radius, 1.6 units). No button: the walk is where you are.
- A beat is seconds, not frames, and a whole number of the region's music beats (AUD-01,
  `docs/design/audio-direction.md`): 3.6 s at Merrow's End (four of Saltmarrow's), 2.4 s in Emberdown's (three),
  3.2 s in Hollowvein's (four). The call half a beat ahead is the roll-call's pickup.
  It is deliberately slow and warm; the game's alternative to the Guild's cold seal.
- **Three misses in a verse** and the verse starts again from its first beat, misses forgiven. Hits are never
  taken back. There is no time limit and no death: the only cost is the verse again.
- **Every verse walked and the place is held**: `BoundsWalks.Complete` sets `walk.<place>.done` and
  `Places.Hold`. A held place stops fading, keeps its days, and gets no Wardens and no lock (anchoring.md).
- A walk can only be started once Wren **knows it** (`holdfast.walk_learned`, taught at Kettil's Rest; or, on the
  coast, `saltmarrow.bone_bridge.heard`: the whale's song is Runa's roll-call, bible 8 [F 3.4]).
- The desk **refuses Hold** for a place that has not been walked ("Walk the bounds first"). Anchor and release
  remain the desk's.
- The chorus stands still for the walk (their schedules pause) and starts again after.

## 2. The three authored walks

| Walk | Place | Verses × beats | Chorus | What it holds |
|---|---|---|---|---|
| **Merrow's End** (greybox now) | `Saltmarrow_B` | 2 × 4: the stoop, the tether-post, the shaft's foot, the reed steps; back by the steps | Dotha alone, nine songs short | The village, the Holdfast way: Dotha's third choice (`saltmarrow.dotha.decided` = 3) |
| **Kettil's Rest** (built, ENV-03: `Emberdown_Bell_2`) | `Emberdown.KettilsRest` | 3 × 5 at 2.4 s (three Emberdown beats): the well-cap, the bell's foot, the rope post, the east rail, the stair head; Runa leads | Runa, Kettil, the families | Teaches the walk (`holdfast.walk_learned`); the town is already held, so this one only teaches: misses restart, nothing is lost |
| **Hollowvein** (the descent, boss 6.4) | `Emberdown.Hollowvein` | 4 × 6 going down; every bound is a dead miner's name | The families, one voice fewer each verse | Wakes the Collapse between verses 3 and 4; walked to the end, the dead are recovered and the keystone is free |

Later, unauthored: the Blank's last walk (bible 9.2, the true ending) is the whole cast singing every bound in
the game; it is a cutscene on this system, not a challenge.

## 3. Presentation
- The **roll-call strip** (WalkView, top centre): the called name large, verse and beat under it, three miss
  marks, a thin bar filling to the beat. Hits flash the name blue, misses ochre; "Held." lingers at the end.
- Bound markers in the room (posts) tint ochre when called, blue at rest, all ochre when held.
- Captions: the verse title when it starts, "Again, from the top of the verse." on a restart, "Walked. <place>
  is held." at the end.
- **Sung** (AUD-02, `docs/design/roll-call.md`): the chorus sings the call as each name is called, the name held
  to the next call; a miss is the chorus faltering (the name breaks off and slips flat); a verse's end is the
  answer in the region's own time. Merrow's End is Dotha alone.

## 4. Story hooks
```yarn
<<walk merrows_end>>          // starts once the talk ends (Wren must be free)
<<if walked("Saltmarrow_B")>>
```
Code: `BoundsWalk.Begin/Abort`, `BoundsWalk.Find(id)`, events `Started`, `NameCalled`, `BeatLanded`,
`VerseRestarted`, `VerseDone`, `Completed`; `BoundsWalks.IsWalked / IsLearned / Complete`.

## 5. WorldState
`walk.<place>.done` (flag), `place.<place>.fate` = held (Places), `holdfast.walk_learned`. Saves need nothing extra.

## 6. Greybox bindings
- Room B has the Merrow's End walk: four posts, Dotha's schedule paused while it runs. Her season talk gains
  "Walk the bounds with her" once the whale has been heard; finishing it decides Merrow's End as held.
- Sable's "held" line already answers a walked quay (Greybox_Sable_Again); Dotha has a walked line.

## 7. Open
- The chorus sings notes, not the bounds' names; the composer's recordings will (roll-call.md §6).
- Whether a walk may cross rooms (Hollowvein's descent wants to).
- Strangers: the bible says the Holdfast fails where a bird is not known. A walk in a place where Wren is a
  stranger could need a named NPC to vouch (a bound with a person in it).
- How the moving camp's walk (Windreach) uses the three sites.
