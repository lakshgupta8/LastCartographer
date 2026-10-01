# The fledglings (CHR-14, v1)

Bible 10, "Flight is memory": fledglings leap in the background of every region; after each ability Wren learns, one
more of them glides a little further. NAR-15 wrote the rule as data (`Dressing.Loops`, `Dressing.At`,
`docs/story/environment.md` §5); this row draws the birds and makes them leap. Version one: one species per region
built on the townsfolk bird at a chick's size, five clips each, and a loop component in the catalog's room that leaps
six of them in turn along flights it computes from the world.

Runtime: `FledglingLoop` (World). Drawings: `tools/characters/fledglings.py`, packed by `pack.py` to
`Art/Characters/Fledgling_<Look>/`. Builder: `RoomRecipe.Fledglings(perchX, perchY, landingY, direction)` and
`ProjectSetup.MakeFledglings`. Tests: `FledglingLoopTests` (edit, 2), `FledglingLoopPlayTests` (play, 2); the rule
itself is `DressingTests`'.

## 1. The birds

| Region | Look (`FledglingLoop.Look`) | Room, perch | They come down |
|---|---|---|---|
| Saltmarrow | `Gull`: white, grey wingtips, a yellow beak | Stilts, the top roost's ledge (1.5, 12.3), east | the floor at 0 (the shallows are the Boardwalk's; v1 lands on the boards) |
| Emberdown | `Grouse`: brown, a crest | Rest_1, the roost doors in the paper (8, 6), east | the ash at 0 |
| The Verdance | `Dove`: grey-pink | Grove_3, the branch at (4, 6.3), east | the moss at 0 |
| Halden | `Pigeon`: blue-grey, a teal breast | Bridges_2, the parapet at (12, 3.3), west | the net at 0 (v1: the bridge floor) |
| Windreach | `Crane`: tawny, long legs and neck | Camp_1, the wagon's roof (8, 3), east | the grass at 0 |
| The Greyfold | `Outline`: paper on paper, the line grey | Cathedral_2, the broken tower in the paper (−8, 9), east | the white at 0 |
| The Blank | `Grey`: the Remnant's chick, Marrow's grey | Hollow_2, a roof (6, 2.5), east | the street at 0 |

One parametric chick (`CHICK` on `cast.Townsfolk`: a big head on a small round body, stubby wings, short legs, a big
eye) in a 1.25-unit cell at 96 px per unit, line 2.6 (2.0 for the outline, 2.4 for the grey). Five clips at 12 fps:
`idle` on the perch (a shuffle, the near wing stretched once, a blink; six frames, loops), `leap` (crouch and spring,
four, once), `glide` (wings out and flat, a slow bob, legs trailing; six, loops), `drop` (half-open wings, legs
flailing; four, loops), `land` (the splat and the shake; four, once). Review sheet: `logs/chr14-sheet.png`.

## 2. The loop

Six birds (`Dressing.Leapers`, one for each piece of the sky) on one perch, each on its own turn, a turn every 1.4 s
(`Interval`), so the whole loop is 8.4 s (`Period`). A bird's turn: the leap at the perch (0.33 s), the flight, the
landing (0.33 s), then gone (climbing back, out of sight) until it stands on the perch 1.4 s before its next turn.
`Evaluate(bird, time)` is pure and gives the position, the phase and whether it shows; `Update` applies it and plays
the phase's clip; `Preview` is what the saved scene and a capture show, every leaping bird along its own flight.

What the world says (`Dressing.At`, read on enable, on every flag and on load; abilities, fates and fades are flags):
- **How many leap**: six, or three at fade stage 2, none from 3; the faded (the Greyfold's, the Blank's) are never
  thinned.
- **How many glide**: one per ability Wren has (`Dressing.SkyPieces`); none in an anchored place, and never in Halden,
  anchored before the story began. In the Fixed World none; in the Open World all.
- **How far**: a dropper's leap is 1.5 wingspans (`DropReach`, a wingspan 0.8 units) and sags at once (a square
  fall); the k-th glider's is half a wingspan more per glider before it (`Dressing.GlideStep`), the newest furthest,
  and bends down gently with a little lift at the start. A glide takes 0.9 s plus 0.3 s per wingspan past the drop; a
  drop 0.7 s.
- **One flies** (the Open World's epilogue): the newest glider's arc never bends down; it goes up and away and does
  not land.

The birds stand at z 2, behind the walk and in front of the room's mid paper, outside the fade group (the rule thins
them itself). The drawing faces the way it leaps.

## 3. Verification

| Check | Where |
|---|---|
| seven looks, one per region, each with the five clips at a chick's cell, leap and land once, the rest looping; a material per look | `FledglingLoopTests.EveryLookHasItsSheetsWithTheFiveClips` |
| every loop in the catalog's room with one perch and six birds on the look's sheets, reading its own place; seven loops in all the scenes | `EveryLoopStandsInItsRoomWithSixBirdsOnTheirSheets` |
| the rule in a built room: nobody glides at first, one more per ability and further, the rest drop, a fade thins, anchored never, the Open World flies the newest up and away | `FledglingLoopPlayTests.TheLoopReadsTheWorld` |
| the turns: the leap at the perch, the drop between, the landing at the reach, gone, back on the perch; the second waits, the third is still climbing; in play the clips and positions follow, and a fade empties the perch at once | `TheyLeapInTurnAlongTheirFlightsAndAreGoneUntilTheirNextTurn` |
| the rule itself | `DressingTests.TheFledglingsGlideFurtherForEveryPieceOfTheSky` |

Version one ran EditMode 305/305 and PlayMode 341/341 (the full suite, then the loop's two again after their in-play
assertions were made frame-proof: a batch frame can be longer than a leap). Captures: each loop in its room
(`logs/chr14-shot-*.png`), the coast with no sky and with three pieces, the Hollow in the Open World, reviewed by eye.

## 4. Rework by hand

- **Redraw a species** as its own sheets of the same clip names (any cell); nothing reads the drawing but the quad.
- **The climb back** is unseen: a bird lands, is gone, and is on the perch again. A hand pass may draw it hopping
  back up the roost.
- **The landings**: the coast's chicks should splash in the shallows and the plateau's hit a net; v1 lands both on
  the floor.

## 5. Open

- **The watchers**: the grandmother with bound wings on the coast, the counting voice in Emberdown, the clan
  cheering on the Steppe (environment.md §5) are not drawn or heard; CHR-12's townsfolk and AUD's.
- **The outlines at the edge of the eye**: the Greyfold's are drawn pale, not hidden when looked at.
- **Brek's leap** at the Wind Gate (offerings.md) is a separate asker, not this loop.
