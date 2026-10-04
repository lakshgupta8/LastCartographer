# The Blank Island Generator (PRG-20, v1)

Bible 4.7, 8.6 and 10: "the Blank is built from the player's choices." An island room for every place Wren left to
the white, made at runtime from `WorldState` when it is asked for, never saved. `Islands` (Core) says which islands
drift and why (NAR-14, `docs/story/blank-islands.md`); `IslandBuilder` (Narrative) builds their rooms;
`RoomManager` asks it before Addressables.

## 1. How a room is asked for
`RoomManager.Generator` is a static hook: given a scene name, return the `Room` you built in a new scene, or null.
`IslandBuilder` registers itself at load (`RuntimeInitializeOnLoadMethod`) and owns every scene named
`Island_<id>`. A transition to one of those, from a `RoomTransition` or `RoomManager.Transition`, builds the island
in `SceneManager.CreateScene`, and the manager places Wren, points the camera confiner and unloads the room behind,
exactly as for a loaded scene. Unloading is the ordinary scene unload; the next visit rebuilds it.

## 2. The drift (`Islands.Drifting`)
The islands in the Blank now, in the order they pass the Hollow's far edge (`Blank_Hollow_3`, the drift room):
the five authored islands in their order (Merrow's End, Hollowvein, Aldermere, the inn, Lowmarket), each present
only by the outcome that sent it, then every other released place, sorted, as a generic Remnant island. Scene names:
`Island_Aldermere`, `Island_Emberdown_Baths_2`. A generic island is named for what the place was (its plan's name,
its atlas name, or its id read aloud): "The baths".

The chain: the first island's west exit leads back to the drift room; each island's east exit leads to the next;
the last has no east exit. Nothing holds the chain still: release another place and it joins the end.

## 3. The room
The same recipe as the greybox rooms, at runtime (the editor's helpers mirrored in `RuntimeRooms`): 40 by 17,
camera bounds, `Room` with `RoomId` = the scene name. An island is **a piece of the place it was, torn out of its
own region's kit and greyed, standing in the Blank's white** (`IslandLooks`, Core; `IslandDrawing`, Narrative).

- **The island**: a slab 24 wide in the middle, on the place's own floor tile, with the Blank's white floor either
  side so there is nowhere to fall. Two platforms on the same tile, placed by a hash of the island's name, so no two
  islands are quite the same.
- **Behind it**: the place's strip at z 3 and its far strip at z 8, greyed toward the paper as far as the island's
  wash (`_Wash`, the line half as far, through a property block on the shared kit material). Then the Blank's
  `Far_Islands` at z 12 and `Farther_Grey` at z 16.
- **The white**: two sheets of the Blank's paper at each of the place's strips, z 2.8 and 7.8, torn edges facing in
  at x ±12 and ±16. Between the edges is the place; outside them is the Blank. The sheet is made once at runtime
  from a fixed seed (`IslandDrawing.TornSheet`): paper, a ragged edge that wanders and catches on fibres, and an
  inked line along it. It is cut out by the ink shader's alpha clip, as everything else is.
- **The props**: the place's own drawings on its floor, greyed with it.
- **Its people**: one `NpcTalker` on the island's node (`Island_Aldermere`, or `Island_Remnant`), prompt "Talk", in
  the body they were met in: Dotha's own sheets, Brask in the chough look, Hollin as himself, the innkeeper a
  nuthatch, Brisk a starling. These are the bodies their portraits are drawn from. Round them stands a crowd of the
  region's townsfolk, doing what the island is doing. Every one of them is a Remnant (`NpcInk`; an island makes
  anyone one).
- Spawns `Start`, `West`, `East`; transitions `To_West` and (except the last) `To_East`.

| Island | Strip, far strip | Floor | Props | The speaker, and the crowd |
|---|---|---|---|---|
| Merrow's End | Mid_Reeds, Far_Roosts | Boardwalk | the stoop, the nets, the moorings | Dotha; three of the village, watching (the eleventh song) |
| Hollowvein | Mid_Gallery, Far_Dark | Timber | the hook lamps, the tally wall | Brask; eight miners of the thirty-one, standing, listening |
| Aldermere | Mid_Village, Far_Lanterns | Lane | the bunting, two lanterns | Hollin; five of the village, cheering (the festival, tonight all night) |
| The inn | Mid_Gate, Far_Canopy | Flag | a table, a lantern | the innkeeper; two travellers asleep over the soup |
| Lowmarket | Mid_Lowmarket, Far_Citadel | Cobble | the mill's wheel, the notice | Brisk; three of the market, idle (nobody's owed) |
| a released place | its region's strip, or its own where its name says (the baths draw the springs) | its region's | two of its region's, by its name | one of its region's townsfolk, two more standing, the same ones every visit |
| a half-island | the Blank's Mid_Drift, Far_Islands | the Blank's | a doorframe, a chair | its place's people, paler; the place is still standing somewhere else |

The wash is how far the place has gone: the authored islands 0.4–0.55 (Aldermere's festival the least, the
miners and the inn the most), a generic island 0.65, a half-island none (it is drawn in the Blank's own grey).

**Loading.** Nothing of an island is in a scene. Every texture and material under Art is in the Addressables group
`Art` at its own path (PRG-24), so `AddressableArt` (World) loads the kit's materials by name and a body's sheets
from `Art/Characters/<C>/<C>_<clip>.png`. A sheet is a strip of square cells: frames = width / height, 96 px a
unit, 12 frames a second. The handles live on the room (`AddressableArt.Held`) and are released when its scene
unloads. If the art is not there (the strip or the floor will not load), the island keeps the greybox: grey
slab, near-white paper, grey quads.

No `FadeGroup`: an island is grey by its wash, not by stage. No vantages: the Blank cannot be surveyed.

## 4. Tests
- `IslandsTests` (edit-mode): the drift's order and names; `Resolve` finds only what drifts.
- `IslandLooksTests` (edit-mode): every material an island asks for is drawn and addressable at its path; every
  body has idle and talk; each authored island is its own region's kit, its speaker in their portrait's body; a
  generic island is its place's region, the same person every visit; a half-island is the white.
- `IslandDrawingTests` (play-mode): all five authored islands, a generic one and a half-island are walked. Each has
  its place's strip, greyed as far as its wash, the Blank above, four torn sheets, its floor and props, the
  speaker drawn from their sheets and a Remnant, the grey quad gone, and the crowd standing round, not on the
  speaker. The room holds its art, and leaving lets it go. Each island's picture goes to `logs/islands/`.
- `IslandRoomsTests` (play-mode): three places released; the drift is three scenes; a transition builds the first
  (spawns, camera bounds, ground, the talker on Aldermere's node, exits to the drift room and the next island);
  walking east builds the next and unloads the last; the generic island speaks as the Remnant and ends the chain;
  and nothing is built for a scene that is not an island's or an island that does not drift in this world.

## 5. Open
- The lantern-radius look is built (PRG-18, `clarity.md`): every island is drawn round her lantern, and the white
  between the drift's edge and the island is untethered, so the Clarity meter runs there.
- The Hollow's drift room is built (ENV-08, `Greybox_Blank_Hollow_3`, which `DriftEntryScene` names): a `DriftField`
  shows as many of the kit's islands going past as `Islands.Drifting` lists, and a `DriftCrossing` over its top platform
  steps her onto the first of them (a caption when nothing drifts). The islands' own rooms are drawn (§3).
- A generic island speaks one script. To say what it was ("This was the baths"), the builder should pass
  `Drift.Name` to Yarn: a `$island` variable, or a `<<island>>` command that sets a caption.
- Islands are rebuilt on every visit and never remember: an NPC talked to stays talked to (flags), but a dropped
  memory on an island is lost with the room. If drops matter here, the room needs a stable id in `MemoryDrops`.
- PRG-14's fade look for a released place, on the way in from the drift room, could replace the hard grey.
- The islands' unnamed Remnant still speak without a portrait. Their bodies are townsfolk looks, which have no
  portraits; a portrait per look (thirty) would give each its face.
- A hand pass: the torn edge drawn rather than generated, an island's underside, and the drift's islands going past
  behind it drawn as the places they are.
