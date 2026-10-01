# The Endings Runner and the Epilogue Walk (PRG-23, v1)

Bible 7.4: "A walk through Halden, then one outer region, then Marrow. Under thirty minutes." The endings are
written (NAR-13) and proven reachable (DES-12); this is the runtime that plays one out. `EndingsRunner` lives in the
persistent scene beside the other player systems.

## 1. How it starts
Each ending's last scene ends with `<<epilogue>>`: `Ending_Fixed`, `Ending_Open_After` (after 6.15), `Observatory_
Teodor_Unwritten`, and `Ending_Rest` at Corvin's chair. The command calls `EndingsRunner.Begin()`, which refuses if
no ending is chosen (`ending.chosen`), the walk has begun (`epilogue.started`), or walking is off. The runner then
waits for the scene that called it to finish before it moves.

## 2. The walk
1. **Voss's coda** (`Observatory_Voss`), at the frame's door, if `ending.voss_coda` is not yet set: the note or the
   statue. The Rest starts in the Blank, so its coda plays there, as a scene, before the walk to Halden.
2. For each stop in `Endings.EpilogueWalk` (its zone from the cast's placing of the scene, `Endings.EpilogueStops`):
   fade to white, go to the stop's room, fade in, play the stop's scene, stand a moment (`HoldSeconds`, 3 s), and on.
3. After the last stop: white, `game.finished`, and the title as a caption: "The Last Cartographer".

| Ending | Stops |
|---|---|
| Fixed | the Hall (Pell), the quay (Sable), the Hollow (Marrow) |
| Open | the Hall, Kettil's Rest (Runa), the Hollow |
| Unwritten | the Hall, the Quiet House (Teodor), the Hollow |
| Rest | the Hall (Isolde teaching), the Hollow (no chick) |

## 3. Where a stop plays
`EndingsRunner.SceneFor(zone)`: the zone's first built room (`WorldGraph.BuiltRoomScene`, "Greybox_&lt;place&gt;"), so
the Fixed World's Sable stop plays on the real quay; otherwise a stand-in from `EpilogueBuilder`, a second runtime
room generator beside the islands' (`RoomManager.Generators`), named `Epilogue_&lt;zone&gt;`: a floor, two walls, a
step, warm paper layers, and the stop's speaker standing on the stop's node. As the real rooms are built, the
stand-ins retire one by one, with no change to the runner.

## 4. Tests
`EndingsRunnerTests`: the Fixed World's walk from `Begin()` visits the built Hall, the built quay and the built Hollow
(Ilse's house, since ENV-08; the stand-ins remain for a zone with no room) in order, each a real room change, plays the coda first and Marrow's verdict last, and ends white on the
title; the Unwritten's own last scene starts the walk through `<<epilogue>>` and the runner waits for it to finish;
with walking off, the command does nothing (the route replays and the endings' arc tests walk by hand, and turn it
off). Every stop's zone is on the map (`EndingsTests`).

## 5. Open
- **A walk, not a wait.** v1 plays each stop's scene on arrival and moves on after a hold. The bible's walk is the
  player's: each stop should have a way on (a door, a road) that the player takes, with the scene where they stand.
  The stand-ins' exits and the built rooms' routes are that work, with the rooms (DES-08 to DES-11 built).
- Under thirty minutes: with the rooms, the walk's length is the rooms' length; the runner should skip a stop's
  travel if the player is already there.
- The title caption is the end; a credits roll and a return to the title screen are M4's.
- The Rest's coda plays at Corvin's chair, where Voss is not; its statue line should read as a report, or the
  coda should wait for the Hall.
