# The Townsfolk Library (CHR-12, v1)

Thirty generic birds, six for each living region, so that a crowd, a watcher, a picket line, a minor named bird or a
bird who asks is a drawing and not a block. Built the way the returning cast was (`npc-animation.md`): the same
parametric townsfolk bird in Blender, a spec per look for the sizes, the colours and the marks that name its species,
clips as functions of time, the paper kits' Freestyle ink, sheets packed at 96 px/unit. The Remnant grey is not drawn:
`NpcInk` washes a look at run time, so the gannet on the faded rail is the gannet, greyed.

Drawings: `tools/characters/townsfolk.py`, packed by `pack.py` to `Art/Characters/Folk_<Look>/`; model sheets
`docs/art/folk_<look>-turnaround.png`. Data: `Townsfolk` (Core: the looks, the clips, who wears what). Builder:
`RoomRecipe.Folk(look, x, activity, face, y, remnant)` → `ProjectSetup.MakeFolk`; `ProjectSetup.DressPerson` dresses
anyone (an NPC, one of the crowd, an asker) from a character's sheets; `PlacementSetup` draws the birds who ask.
Tests: `TownsfolkTests` (edit, 4), `TownsfolkPlayTests` (play, 2).

## 1. The looks

One function, `marks(b, s)`, dresses every look from keys in its spec: a `cap` (a crown), `brow`, `disc` (a facial
disc or a pale face), `face` (a rook's bare bill-base), `stripe` (through the eye), `band` (the puffin's bill),
`comb`, `fan_crest`, `tuft` (a raven's throat), `bib`, `crescent`, `speckles` on the near flank, `tips` (dark
wingtips), `bars` across the wing, `shoulder`, `shawl`, `cord` (wings bound). Cells: 1.6 for the small (a sparrow, a
finch, a lark), 2.0 for most, 2.4 for a raven and a young crane, 2.6 for a goose and a bustard. Line 3.0, the cast's.

| Region | Looks (`Townsfolk.Looks`) | Who wears them in the greybox |
|---|---|---|
| Saltmarrow | Gull, Tern, Gannet, Puffin, Eider, Turnstone | the Tetherline's gull, the iris fields' tern, the Boardwalk's puffin and turnstone; the **gannet** at the faded light (an asker, grey); the **eider** on the Stilts' top roost, her wings bound, watching the leap |
| Emberdown | Chough, Raven, Ptarmigan, Dipper, Ouzel, Grouse | Hask (chough); the Rest's chough, raven at the anvil, dipper, ptarmigan; the Bell's ouzel; the **grouse** under the roosts, watching the leap (the counting voice, once AUD gives it one) |
| The Verdance | Thrush, Woodpecker, Finch, Jay, Nuthatch, Owlet | Wend (finch), Tobin (woodpecker), Hollin (jay), Ansel (owlet), the Innkeeper (nuthatch, a Remnant); the **traveller** at the one-night inn (an asker, grey); the Quiet House's brother; Aldermere's villagers on the last day |
| Halden | Pigeon, Starling, Rook, Sparrow, Goose, Magpie | Ostry and Anvers (rooks), Brisk (starling), Tam (sparrow), Arden (goose), the orchard's Keeper (magpie); the picket line at the pulp yard (two starlings, one cheering on the crate, a pigeon, a clerk watching), Lowmarket's pigeon, the Hall's sparrow, the geese of Arden's family |
| Windreach | Lark, Hoopoe, Kestrel, Bustard, Plover, Crane | **Brek** at the Gate (a young crane, an asker); the clan at the wagon (a hoopoe, a lark cheering, a plover watching the leap), the elder by the first fire, the walkers at the third, the singer and the elder at the Gate, a scout on the walk |

The Greyfold and the Blank have no living townsfolk: theirs are these looks as Remnant (`NpcInkState.Remnant`), and
the Blank's islands' people are still runtime grey quads (§5).

## 2. The clips

Six each at 12 fps, all looping: the hub's four (`idle`, `talk`, `walk`, `asleep`: `cast.py`'s, shared) and the
crowd's two: `watching` (the head up and tracking something overhead, a lean back, a blink) and `cheering` (wings up
and beating, the beak open, a bounce). `NpcAnimator` gained a standing **activity** (`_activity`, "watching the
leap", "cheering"), used when there is no schedule (or the schedule's post names nothing); as a post's, its first
word names the clip, so talk and walk still win and a clip the sheets lack idles.

## 3. Who stands how

- **One of the crowd** (`MakeFolk`) is `Folk_<Look>_<n>` at z 0.5, behind the walk and in front of the room's props:
  the quad on the look's sheets, `InkSheetPlayer`, `NpcAnimator` with its activity, `NpcInk` at its rest state
  (drawn, or Remnant when told). No talker, no collider: nobody to bump, nobody to talk to. Faces the way it was
  told (the quad mirrored).
- **A minor named bird** (`Townsfolk.Named`: Hask, Ostry, Wend, Tobin, Ansel, Hollin, Arden, Brisk, Anvers, Tam, the
  Keeper, the Innkeeper) keeps its own object, talker and material (`M_Npc_<Name>_Greybox`) and rests on its look's
  idle, the tint stood down to white, through `MakeNpc` → `DressPerson`.
- **A bird who asks** (`Offerings.Asker.Look`, `Remnant`, `FacesWest`): `PlacementSetup` dresses the `Read_<node>`
  trigger itself from the library, so the asker's talker is the person's. The gannet (Remnant, facing west), the
  traveller (Remnant), Brek (a young crane). Corvin's asker has no body: his own drawing already stands beside it
  (`Capital_Corvin` reaches the same argument), and the trigger waits beside him.
- **Colour** follows the place as for anyone (`npc-animation.md` §4): the clans' are never washed, a hub's thin as its
  place fades, the Remnant start grey.

## 4. Verification

| Check | Where |
|---|---|
| thirty looks, six per living region, each packed with the six clips at 96 px/unit at its own cell, 12 fps, looping, a model sheet each | `TownsfolkTests.TheLibraryIsSixLooksForEachLivingRegionPackedWithTheSixClips` |
| every look worn somewhere; every named bird's material on its look's idle with no tint | `EveryLookIsWornSomewhereAndTheNamedBirdsWearTheirs` |
| the three birds drawn (the Remnant ones resting grey), Corvin's asker beside his drawing, no block under any readable | `TheBirdsWhoAskAreDrawnAndNoBlockIsLeftUnderAnyReadable`, `DressingPropTests.NoBlockIsLeftStandingNowTheBirdsWhoAskAreDrawn` |
| the crowds where the brief put them with their activities; everyone animating and carrying a colour state | `TheCrowdsStandWhereTheBriefPutsThem` |
| in play: the clan cheers and watches, the rest idle, nobody to talk to; an activity changed is followed | `TownsfolkPlayTests.TheClanCheersAndWatchesAtTheCampAndTheRestIdle` |
| in play: the gannet is a grey person on the asker's trigger, facing west, no block | `TheGannetOnTheFadedRailIsAGreyPersonWhoAsks` |

A capture shows a Remnant in its own colours: `NpcInk` washes only in play (the play test reads the wash). Review sheet `logs/chr12-sheet.png` (every look's idle, watching,
cheering and talk); captures of the camp, the Stilts' roost, the picket, the gannet, the Gate and the strike hall.

## 5. Rework by hand

- **Redraw a look** as its own strips at the same clip names (any cell); nothing reads the drawing but the quad.
- **A new look** is a line in `LOOKS` (`townsfolk.py`) and a line in `Townsfolk.Looks`; the test keeps them equal.
- **The watchers' voices**: the counting under the Rest's roosts and the clan's song are AUD's.
- **The Blank's islands** (`IslandBuilder`, runtime) still stand grey quads: a runtime room has no `AssetDatabase`,
  so their people need the sheets in a loadable place (Resources or Addressables) before they can wear the library.
- **Who stands where** is the recipes' (`.Folk(...)`): the hubs built by hand (the Quay, Merrow's End) have no crowd yet.
