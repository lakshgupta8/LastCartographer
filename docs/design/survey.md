# Survey (DES-02, v1)

How the atlas is drawn, wiped, and drawn again, and how Wren moves between the places on it. Runtime: `Atlas`
and `FadeStages.Erase/Recover` (Core), `VantagePoint`, `Cantor`, `TravelPoint`, `FastTravel` (World), `AtlasView` (UI),
`<<erase>>`, `erased()`, `drawn()` (Yarn).

## 1. Rules
- The atlas is Wren's map and the pause screen (GDD 5). Unsurveyed places are blank paper. A **vantage** is
  drawn by standing in it and **holding Survey** for 1.2 s; the hold is the Cornifer-free way of inking a place in.
- Vantage ids are `Place/Name`. The place is a room id (`Room.RoomId`) or a named sub-zone; the page groups
  vantages by place and places by region.
- A place is **drawn** when at least one of its vantages is on the page. Being drawn is what puts its desks and
  lamps within reach (below).
- **Erasure.** A Cantor's bell wipes every drawn vantage of the place it rings over and blanks the place's ink at
  once (stage 4, quick). The stage the place had is remembered. Story gates that read "ever surveyed" do not undo
  themselves; the page and the ink do. Anchored places cannot be erased by a field bell.
- **Recovery.** Re-surveying any vantage of an erased place brings the ink back to the stage it had, at the
  drawing pace. Nothing else recovers a place: not a desk, not time.
- **Waypoints.** Every drafting desk is a travel point; a lamp is one once it is lit (its vantage drawn, which for
  the fourth lighthouse is the Lamp-Keeper's beacon). Standing at one puts it on the page for good. A waypoint is
  a **destination** while it is known and its place is drawn; erasure takes it off the list until the re-survey.
- **Travel** happens only from the desk or lit lamp Wren stands at, through the atlas page: paper closes over
  the screen, the room changes, the paper lifts. It does not rest, save, or move the respawn point.

## 2. The page

| Element | Shows |
|---|---|
| Region heading | The Greyfold, The Saltmarrow, … (catalog order) |
| Place | Name, ink or dim by drawn; `· here` for the current room |
| Vantage marks | `●` drawn, `○` blank, `✕` erased |
| Status line | fade stage name (or `erased. Draw it again.`), fate once decided, known waypoints (`▣` desk, `☼` lamp; `(off the page)` when not a destination) |
| Travel | From the point Wren stands at: destinations with a cursor. Otherwise "Travel from a desk or a lit lamp." |
| Right page | The journal (commissions, scraps) |

M / Select opens and closes it; ↑↓ picks a destination; J / South travels; Esc / East closes.

## 3. The Cantor
Combat doc 7, Unwriter family. A dove with a bell that hovers 2.6 units over the floor and keeps 2.5 units
from Wren. Within 5.5 units it **rings** for 26 frames (the bell rises: the telegraph), then **tolls**: one mask to
anyone within 4 units, and the place is erased. Recover 40 frames, cooldown 4 s. A hit during the ring stops it.
Answer: **Longstroke** (the forward Flourish reaches where the quill does not). Health 3.

## 4. Story hooks
```yarn
<<erase Saltmarrow_B>>                 // a Cantor's bell in the script (no-op if erased or anchored)
<<if erased("Saltmarrow_B")>>
<<if drawn("Saltmarrow_A")>>
```
Code: `Atlas.Survey`, `Atlas.Erase`, `Atlas.IsDrawn`, `Atlas.Discover`, `Atlas.Destinations`, `FastTravel.Go`.

## 5. WorldState
`SurveyedVantages` (ever drawn), `ErasedVantages` (wiped, until re-drawn), `Waypoints` (known travel points),
`fade.<place>.erased` = stage before erasure + 1. `IsSurveyed` is drawn-and-not-erased; `IsEverSurveyed` is the
story's reading. Save version 3.

## 6. Greybox bindings
- Room B has the tether-post vantage and a Cantor over its east end. Its bell blanks Merrow's End (the reeds go
  to paper in under a second) and takes the tether-post off the page; draw it again and the reeds come back to
  whatever stage Dotha's decision left them at. Sable notices an erased Merrow's End.
- Desks in rooms A and the lighthouse are travel points; the lamp under the Lamp-Keeper's perch lights when she
  is beaten. Rest at the quay, beat her, stand under the lamp: the atlas travels between the three.
- Surveying prints `Drawn: <vantage>`; recovering prints `Drawn again: <place>`; the toll prints `Erased: <place>`.

## 7. Open
- The **inking animation** on the page (the drawing hand, ART-7): the page updates at once for now.
- Vantages with a **line of sight** requirement (sighting-lens) and the survey's ink cost, if any.
- Whether a bell can erase a **held** place (v1: yes; the bounds-walk, DES-13, may answer differently).
- The Choir (6.6) and the Half-Cathedral bells (6.12) erase the arena and the lantern-radius, not only the page.
- Travel cost: settled in PRG-21 (`moving-camp.md`). The road takes an hour a way on the macro map, and the day moves
  on by it.
