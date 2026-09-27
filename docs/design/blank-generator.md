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

## 3. The room (v1 greybox)
The same recipe as the greybox rooms, at runtime (the editor's helpers mirrored in `IslandBuilder`): 40 by 17,
camera bounds, `Room` with `RoomId` = the scene name.
- **The island**: a grey slab, 24 wide, in the middle; white paper floor either side so there is nowhere to fall.
  Two platforms placed by a hash of the island's name, so no two islands are quite the same.
- **The white**: three paper layers in near-white at z 3, 8, 16.
- **Its people**: one `NpcTalker` on the island's node (`Island_Aldermere`, or `Island_Remnant`), prompt "Talk", a
  grey quad for a body. Generic islands' Remnant are paler.
- Spawns `Start`, `West`, `East`; transitions `To_West` and (except the last) `To_East`.

No `FadeGroup`: an island is grey by material, not by stage. No vantages: the Blank cannot be surveyed.

## 4. Tests
- `IslandsTests` (edit-mode): the drift's order and names; `Resolve` finds only what drifts.
- `IslandRoomsTests` (play-mode): three places released; the drift is three scenes; a transition builds the first
  (spawns, camera bounds, ground, the talker on Aldermere's node, exits to the drift room and the next island);
  walking east builds the next and unloads the last; the generic island speaks as the Remnant and ends the chain;
  and nothing is built for a scene that is not an island's or an island that does not drift in this world.

## 5. Open
- The lantern-radius look (art-direction.md; colour blooming round Wren) is a render feature, not the room's.
- The Hollow's drift room is planned, not built; until it is, the drift is entered by `RoomManager.Transition`.
- A generic island speaks one script. To say what it was ("This was the baths"), the builder should pass
  `Drift.Name` to Yarn: a `$island` variable, or a `<<island>>` command that sets a caption.
- Islands are rebuilt on every visit and never remember: an NPC talked to stays talked to (flags), but a dropped
  memory on an island is lost with the room. If drops matter here, the room needs a stable id in `MemoryDrops`.
- PRG-14's fade look for a released place, on the way in from the drift room, could replace the hard grey.
