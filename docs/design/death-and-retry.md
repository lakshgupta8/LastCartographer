# Death and Retry (GDD 6, PRG-17, v1)

What happens when Wren's masks run out, and what it costs. Runtime: `PlayerRespawn` (the return),
`MemoryDrops` and `MemorySmudge` (the smudge), `Memories` (Core, the rules), `WrenVitals` (the ink empties).

## 1. The loop
1. The last mask goes. The Inkwell empties. Wren freezes for 0.9 s.
2. **The drop.** Everything she has bound (`WorldState.BoundMemories`) goes into the world's drop at the spot
   where she fell: `DroppedMemories`, `DropRoom`, `DropX/Y`. A **smudge of her own** stands there. If she had
   nothing bound and no earlier drop, nothing is left behind and there is no smudge.
3. **The return.** She wakes at the last drafting desk (or the wax seal, once), masks full, ink empty. Enemies
   are back; bosses are not.
4. **The walk back.** The smudge is rebuilt whenever its room is entered and dissolves with the room. It flickers
   like any smudge and lunges like one; only drawn frames can be hit. Three hits while drawn and the memories
   are hers again ("Recovered: …").
5. **A second death** before the recovery folds the old drop into the new one at the new spot. Memories are never
   lost, only out of reach: the ink remembers. The stake is that doors, birds and keystones that ask for a memory
   cannot be answered while it lies in the smudge, and the desk's bind cannot offer it.

## 2. Why it is shaped like this
Silksong's shade run, with narrative currency instead of money. Losing a bound memory for good would break the
scenes that ask for it later (bible §10), so the cost is distance and danger, not loss. The smudge is a real
enemy of the Smudge family so the Field lantern reveals it and Blot slows it; it drops no seeds.

## 3. Numbers
| | |
|---|---|
| Freeze before the return | 0.9 s |
| Smudge health | 3 |
| Smudge flicker | 0.9 s drawn, 0.7 s undrawn (the family's) |
| Where it stands | 0.6 above the spot she fell |

## 4. Save
Save version 4 adds `droppedMemories`, `dropRoom`, `dropX`, `dropY`. Loading a save with a drop in the current
room rebuilds the smudge.

## 5. Open
- The smudge's look: for now a dark sphere on the fade shader. It should be a smear of her own silhouette (ENV-12).
- A journal line for what the smudge holds, and the atlas marking its room.
- Whether a smudge left for a whole act should drift toward the region's hub (a kindness) or into the Blank (a
  story: a memory that faded because nobody went back for it, recovered on its island in Act 3).
- Gauntlet deaths (combat doc §9) cost a mask and return to solid ground; they do not drop.
