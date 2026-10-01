# Environmental storytelling props (ENV-06, v1)

The forty-two pieces of `docs/story/environment.md` drawn and stood in their rooms, and the two doors that ask. Version
one: every piece is a cut-out from the region's paper kit on an ink quad, the pieces that change with their place carry
a second drawing and swap it when the world does, and the pale blocks that stood for the readables are gone from every
room but the four where a bird asks. The hand pass is §5.

The catalog is `Dressing` (Core): each piece now names its drawing (`Prop`), its changed drawing (`PropAfter`) and what
changes it (`Change`, a `DressingChange`). The component is `DressingProp` (World). The builder's part is
`ProjectSetup.MakeDressing` and `RoomRecipe.Dress`; the readables' part is `PlacementSetup`. Tests: `DressingPropTests`
(edit, 6) and `DressingPropPlayTests` (play, 2).

## 1. The drawings

Thirty-one new drawings and eight changed states, in `tools/paperkit/props.py` under "the dressing (ENV-06)", rendered
like every prop (one cut-out at 96 px/unit, feet at the bottom edge, no depth wash) into the region's kit. Eleven pieces
were already drawn by the rooms' recipes (ENV-03..08) and are only named here. The thing speaks, so what is written on
it is marks, not letters: rows of upright strokes for a cut name, horizontal strokes for script, tally strokes for a
count. The hand pass letters them.

| Region | Piece | Drawing | Size | Changes to | When |
|---|---|---|---|---|---|
| Saltmarrow | the Shore's tether-posts | `Prop_TetherPosts` | 3 × 2 | | |
| | the Quay's price board | `Prop_PriceBoard` | 1.5 × 2 | | |
| | the stilt-roost's ladders (seen) | `Prop_Ladders` | 2 × 4 | | |
| | Merrow's lintels | `Prop_Lintels` | 3 × 2.5 | `Prop_Lintels_Chalk` | Saltmarrow_B held or anchored |
| | the Ferry's moorings (seen) | `Prop_Moorings` | 3 × 0.75 | | |
| | the first light's log | `Prop_Log` | 1 × 0.5 | | |
| | the faded keeper (seen) | `Prop_Keeper` | 2 × 3, at z 2.5 | | |
| | the chapel's tapestry | `Prop_Tapestry` | 2.5 × 3 | | |
| | the chapel's door (asks) | `Prop_ChapelDoor` | 1.5 × 2.5 | `Prop_ChapelDoor_Open` | `saltmarrow.chapel.door_open` |
| Emberdown | the roost lintel | `Prop_Lintel` | 2 × 4 | | |
| | Kettil's tally wall | `Prop_TallyWall` | 2.5 × 2 | | |
| | the pit-head's cups | `Prop_Cups` | 2.5 × 1.25 | `Prop_Cups_Up` | `emberdown.hollowvein.walked` |
| | the ninth chimney's foot | `Prop_ChimneyFoot` | 2 × 1.5 | | |
| | the ninth's door (asks) | `Prop_NinthDoor` | 1.5 × 2.5 | `Prop_NinthDoor_Open` | `emberdown.ninth.door_open` |
| | the gallery's lamps | `Prop_HookLamps` | 3 × 2 | `Prop_HookLamps_Lit` | `emberdown.hollowvein.walked` |
| | the bottom of Hollowvein (seen) | `Prop_Bottom` | 2.5 × 1.5 | `Prop_Bottom_Swept` | `emberdown.hollowvein.walked` |
| Verdance | the milestone | `Prop_Milestone` (ENV-04) | | | |
| | the cloister's wool map | `Prop_WoolMap` | 2.5 × 2.5 | `Prop_WoolMap_Open` | Verdance_Aldermere_2 released |
| | the library's dust (seen) | `Prop_Dust` | 4 × 3, no line | | |
| | the lectern | `Prop_Lectern` (ENV-04) | | | |
| | Aldermere's bunting (seen) | `Prop_Bunting` (ENV-04) | | | |
| | the gate's ledge (seen) | `Prop_Ledge` | 2 × 1 | | |
| | the gate's inscription (read, not catalogued) | `Prop_Inscription` | 2 × 3 | | |
| Halden | the toll board | `Prop_TollBoard` | 1.5 × 2.5 | | |
| | the drying sheets | `Prop_Sheets` | 3 × 2.5 | | |
| | the roll of Guildmasters | `Prop_Roll` | 2 × 2.5 | | |
| | the standing order | `Prop_Order` | 1 × 1.75 | | |
| | the exam papers | `Prop_ExamDesk` (ENV-05) | | | |
| | the orchard's leaves (seen) | `Prop_Leaves` | 2 × 1.25 | | |
| | Lowmarket's notice | `Prop_Notice` (ENV-05) | | `Prop_Notice_Complete` | Halden_Lowmarket_2 anchored |
| | the seventh bridge (seen) | `Prop_Scaffold` (ENV-05) | | | |
| | the Bastion's plaque | `Prop_Plaque` | 2 × 2.5 | | |
| | the office drawing | `Prop_Drawing` | 1.25 × 1.75 | | |
| Windreach | the stones' notches, the wagon-cloth, the river's boats, the Gate's lip | `Prop_Stone`, `Prop_Wagon`, `Prop_Hull`, `Prop_LipStone` (ENV-07) | | | |
| Greyfold | Isolde's beam, the mileposts, the footprints | `Prop_Beam`, `Prop_Milepost`, `Prop_Footprints` (ENV-08) | | | |
| | the outpost's tethers (seen) | `Prop_CutTether` | 2 × 1.25 | | |
| | the old tether at the Threshold (seen) | `Prop_OldTether` | 1.5 × 2.5 | | |
| Blank | the nameplate, the crayon floor | `Prop_Door`, `Prop_Crayon` (ENV-08) | | | |
| | Ilse's doorframe | `Prop_Doorframe` | 1.25 × 2.5 | | |

The three other readables that are not catalog pieces take the recipes' drawings: the gravestone, the Great Atlas's
frame, and Voss's notice over the dead ledger (`PlacementSetup.Extra`).

Render the lot with `blender -b -P tools/paperkit/props.py -- Prop_TetherPosts ...` (names, or `Region:Prop_X` for a
shared one); a drawing's details must sit nearer the camera than the box they lie on (a box is 0.4 deep, so y ≤ −0.25),
and pale strokes (chalk) must be thicker than the ink line round them or the line swallows them. Sizes are multiples
of 1/24 unit (a multiple of four pixels at 96 px/unit), so the textures compress.

## 2. How a piece stands

- **A read piece** comes with its trigger. `PlacementSetup.Place` (run by every build) makes `Read_<node>` with the
  talker and under it `Dressing_<prop>` with the drawing on a quad at z 0.4, from `Dressing.ByNode`, a door asker's
  `Prop` (`Offerings`), or `Extra`. Where the room's recipe already stands `Prop_<prop>` within 3.5 units of the trigger
  (the milestone, the standing stones, the exam desks, the beam, the mileposts, the office door) only the trigger is
  placed. Where nothing is drawn (the four birds who ask: the gannet, the traveller, Brek, Corvin), the ochre block
  stays: a person is a character's, not a prop's.
- **A seen piece** is the recipe's: `RoomRecipe.Dress("<piece id>", x, y, z)` stands the catalog's drawing(s) there.
- **Both** join the room's `FadeGroup` at dropout 5 (they thin with the place and never drop), and the placement prunes
  the group's missing layers before it re-places.

## 3. A piece that changes

`DressingChange` is a flag, or a place and the fates that change it; `IsMet(WorldState)` says whether the world has
changed the piece. `MakeDressing` stands both drawings under one `Dressing_<prop>` object with a `DressingProp` that
holds the pair and the change; the second starts inactive. The component binds to the world's `FlagChanged` (fate and
fade are flags too) and rebinds on `GameState.Loaded`, and `Apply` activates one drawing and deactivates the other, so
the fade group's own enabling never shows both. The Yarn scene that reads the piece branches on the same thing, and the
edit test holds them together: the flag or place the drawing changes on is in the scene's text.

Six catalog pieces change (the lintels, the cups, the lamps, the bottom, the wool map, the notice) and two doors open.
Reading changes nothing; only the world does (environment.md §1).

The capture takes a world: `OWSBG_SHOT_WORLD="emberdown.hollowvein.walked=1"` or `"fate:Halden_Lowmarket_2=Anchored"`
(pairs with `;`) sets it and applies every `DressingProp` before the render.

## 4. Verification

| Check | Where |
|---|---|
| every piece names a drawing in its region's kit (PNG and manifest), the six that change name a second and a change, the two doors shut and open, no bird is a prop | `DressingPropTests.EveryPieceHasADrawingInItsKit` |
| the change reads the world as the Yarn does: the walk, held/anchored/released, Aldermere released, Lowmarket anchored; empty never; and each changing scene branches on the same key | `TheChangeReadsTheWorldAsTheYarnDoes` |
| every piece stands in its built room (`Prop_*`, and `Read_*` where read), the six under a `Dressing_*` with a `DressingProp` | `EveryPieceStandsInItsBuiltRoom` |
| the doors are in their rooms shut and open on their flags | `TheDoorsAreDrawnShutAndOpen` |
| only the four birds' blocks remain in any room with a readable | `OnlyTheBirdsWhoAskAreStillBlocks` |
| the recipes' drawings are not drawn again under the trigger; Lowmarket's board is the readable's pair | `TheRecipesDrawingsAreNotDrawnTwice` |
| a pair swaps on its flag and its place's fate, and back for a new game | `DressingPropPlayTests.APieceChangesWithTheWorldAndBackForANewGame` |
| in the real chapel the door opens on the offering's flag, both drawings in the fade group, the tapestry static | `TheChapelDoorOpensOnItsFlagInItsRoom` |
| Wren stands at every readable and up reads it | `PlacementTests` (unchanged) |

Version one ran EditMode 297/297 and PlayMode 335/335 after the rebuild. Captures: every new piece in its room and the
eight changed states (`logs/env06-shot-*.png`, reviewed by eye), and the thirty-eight drawings on one sheet.

## 5. Rework by hand

- **Letter the marks.** Every inscription is strokes; the scenes say the words (`<Region>_Environment.yarn`). Draw the
  real lettering in the region's hand: chalk on the coast, cut basalt in Emberdown, brass in Halden.
- **Redraw any piece** as its own PNG of the same size (or change the size in the function and re-render); nothing
  reads the drawing but the quad.
- **The dust should move**: it parts round Wren (environment.md). Version one is flecks on a quad.
- **The keeper** stands between the walk and the strip at z 2.5; a hand pass may paint her into the faded tower strip.

## 6. Open

- **The four birds who ask are still blocks** (the gannet, the traveller, Brek, Corvin): they are characters, for the
  NPC drawing rows, not this one.
- **The doors open as drawings, not as doors**: the chapel's and the ninth's open state does not move a collider. The
  reliquary and the satchel are the drawing's.
- **Marks, not letters** (§5).
- **One state per flag.** Merrow's lintels have three Yarn readings (held, anchored, paper) and two drawings; held and
  anchored share the chalk. The fade's "doors to paper" is the fade group's wash.
- **The fledgling loops** leap now (CHR-14, `fledglings.md`).
