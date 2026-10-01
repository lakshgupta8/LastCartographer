# Traversal Gauntlets (CMB-18, v1)

Combat doc §9: each region has one gauntlet, a pure platforming sequence built around its ability, and a fall in one
returns Wren to the last solid ground for a mask, never a full death. Six gauntlets, one per region except the
Blank. The Blank's traversal is the drift between islands and Clarity (PRG-18), not a course.

The data is `Gauntlets` (Core). The greybox is `GauntletKits` (World), which builds each one to a recipe. The rooms
are `GauntletRooms` (Narrative), built at runtime as scenes named `Gauntlet_<id>` until each region is built; the
updrafts' is the first in its built room (ENV-07, `Windreach_Gate_2`): the course is the room's own lip, grass and
ledge, with the `Gauntlet`, the hazard and the goal added by the recipe (`Gauntlet(id, ...)`).
Crossing one writes `gauntlet.<id>.done` and pays one vellum scrap, the first time only.

| Gauntlet | Region, room | Built around | The course |
|---|---|---|---|
| **The lamp posts** | Saltmarrow, the Lantern Chain (a line off the built chain) | Wingbeat | Bank, one lamp post, far bank, each 8 units apart over the tide: a full jump carries about 6.5, a jump and a Wingbeat about 11.5 |
| **The furnace shafts** | Emberdown, the Nine Chimneys (`Emberdown_Chimneys_2`) | Talonhold | A chimney 4 wide and 13.5 tall, wall to wall; a hot vent on each wall, and a hand on one lets go |
| **The canopy threads** | Verdance, the Lantern Grove (`Verdance_Grove_1`) | Inkthread | 21 units of forest floor under three anchors, thorns below |
| **The flyer-tower** | Halden, the Bastion (`Halden_Bastion_1`) | Talonhold and Inkthread | A 9.5-unit tower with no stairs, then two anchors across a 13-unit gap down to the hall; the mill race below |
| **The updrafts** | Windreach, the Wind Gate (`Windreach_Gate_2`) | Windmemory | 22 units of long grass, three ink-swirl updrafts, and a far ledge 3 units up |
| **The Road That Stops** | Greyfold (`Greyfold_Road_1`) | Clarity | Four stretches of cobble, 3 apart over the white, that exist only in her lantern-radius |

## The rules

- **`Gauntlet`** remembers the last `SolidGround` she stood on. A `GauntletZone` hazard (tide, vent, thorns, mill
  race, long grass, the white) takes one mask, never her last, and puts her back there.
- **`LanternPlatform`** exists only when she carries Clarity and stands within her lantern-radius: solid and drawn,
  otherwise an outline she falls through. The radius is her Clarity meter's (PRG-18, `clarity.md`): 5 units at level
  1, narrowing as the meter runs down, wider with the Field lantern. The course was built at 3.5, and at any level
  the next cobble is in reach and the one after it is not.
- **The goal** is a zone over the far ground.

## The abilities it finished (CMB-04)

The gauntlets needed the two movement abilities the controller didn't have yet.

- **Inkthread.** A new Thread button: `U`, or the right trigger, which Dash no longer shares; Dash keeps the right
  shoulder, Shift and K. The thread goes to the nearest anchor within 9 units. Anchors ahead of her come first.
  Anchors are any `TetherAnchor` (the roots, a Tether-hook's point) and any enemy the Compass-dart has marked. It
  costs 2 pips, or the Charter's `InkthreadCost` (the Ferryman's is 1). It pulls her at 22 units/s. At the anchor
  she hops up and her Wingbeat refreshes. A knockback or a freeze cuts the thread. With no anchor in reach, or no
  ink, it is refused.
- **The Windmemory glide.** Holding jump on the way down with Windmemory, gravity drops to 60% and the fall is capped
  at 4 units/s. Updrafts (`Updraft`) lift her only with Windmemory, as before.

## Tests

`GauntletsTests` (EditMode, 3 tests):
- One gauntlet per region but the Blank.
- Each is in its region, on the map, and in its planned room.
- Each is built around the ability the combat doc names.

`GauntletTests` (PlayMode, 10 tests):
- **The thread:**
  - It does nothing without Inkthread, and is refused without ink.
  - It costs 2 pips, pulls her to the anchor, and hops her off it.
  - The Ferryman's costs 1.
  - An unmarked crab is not an anchor; a marked one is.
- **The glide:** without Windmemory she drops; with it she falls slowly while jump is held, and falls normally when
  it's released.
- **Falls:**
  - A fall costs a mask and returns her to the bank.
  - On her last mask it costs nothing.
- **The Road:** it is outlines without Clarity; with it, only the cobbles in reach are there.
- **The numbers:** every gap is wider than a full jump, every climb is taller, and the lamp posts' gap is within a
  jump and a Wingbeat.
- **A plain runner** (run right, jump at edges, Wingbeat on the way down, thread to anchors ahead) goes through the
  lamp posts, the canopy, the updrafts and the Road twice: without the ability she falls; with it she crosses
  without a fall, and the flag and the scrap are written.

`GauntletRoomsTests` (PlayMode, 1 test): each gauntlet's room loads through RoomManager with Wren at its start.

## Open

- **The two climbs have no runner.** A climbing policy for the shafts and the tower (wall to wall, clear of the
  vents) is next. The numbers test proves they need Talonhold, not that the vents leave a way up.
- **Thread aim.** The thread goes to the nearest anchor ahead. Aiming with the stick, and a reticle, come with the
  controls art; DES-14 v1 (`accessibility.md`) remaps the Thread button but doesn't aim it.
- **Real rooms.** Each gauntlet moves into its region's built room; the Lantern Chain's lamp posts could join the
  built chain now, between the second and third lighthouses.
