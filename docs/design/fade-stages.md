# Fade Stages (DES-04, v1)

How a place loses its ink. The one shader parameter that is the game's central image (art-direction 3), and the
rules for when it moves. Runtime: `FadeStages` (Core), `FadeGroup` (World), `<<fade>>` (Yarn).

## 1. Rules
- Every **place** has a fade stage 0–4. A place is a room (`Room.RoomId`) or a named sub-zone inside one.
- Stages **advance only on story beats**: a conversation, a decision, a keystone, an act break. Never on a clock,
  never on distance, never while the player is looking at it unless the scene is about that.
- **Anchored places do not fade.** Anchoring (DES-03) freezes the stage where it is.
- Stages do not go back. The one exception is **restore** after **erasure** (a Cantor's bell wiping a surveyed
  area, DES-02): re-surveying brings the ink back to the stage it had.
- A stage change while the room is loaded animates over about 2.5 s; a room that loads mid-fade shows the stage at once.

## 2. The stages

| Stage | Name | `_Ink` | What the player sees |
|---|---|---|---|
| 0 | drawn | 1.00 | Everything as drawn. |
| 1 | thinning | 0.80 | Line weight thins (alpha cutoff rises), a little grain. |
| 2 | washing | 0.55 | Colour desaturates toward paper; thin lines gone. |
| 3 | softening | 0.30 | Grain up, edges soft; **foreground layers drop out** of the parallax stack. |
| 4 | blank | 0.00 | All paper layers drop; ground remains, washed to paper. White as a presence. |

Lit (non-ink) materials wash toward the region's paper colour by the same curve instead of thinning.

## 3. Layers and dropout
A `FadeGroup` lists the renderers that belong to a place with a **dropout stage** each:

| Layer kind | Dropout |
|---|---|
| Foreground paper (`Paper_Fore*`) | 3 |
| Mid and far paper | 4 |
| Ground, platforms, walls | never (5): the floor stays walkable, washed to paper |
| Characters, enemies, interactables | not in the group: people fade by their own rules (the Remnant, NAR-14) |

Dropout happens once the ink has finished leaving, so a layer never pops mid-animation.

## 4. Story hooks
```yarn
<<fade Saltmarrow_B 2>>            // advance (no-op if lower or anchored)
<<if fade_stage("Saltmarrow_B") >= 2>>
```
Code: `FadeStages.Advance(world, place, stage)`, `FadeStages.Restore(world, place, stage)`, `FadeStages.Get`.

## 5. WorldState
`fade.<place>` = stage (int flag). Anchoring lives in `WorldState.AnchoredPlaces`. Saves need nothing extra.

## 6. Greybox bindings
- Every greybox room has a `FadeGroup` over its paper layers and ground, place id = room id.
- Dotha's "let it fade" advances `Saltmarrow_B` to 2: Merrow's End begins to go the moment Wren chooses.

## 7. Open
- Audio: each stage thins the region's ambience (AUD-01).
- Per-region `_Ink` curves once real paper kits exist (ENV-01): the numbers above are for the greybox.
- How Remnant birds and their places share a stage (NAR-14, PRG-20).
- Whether a place can fade while Wren stands in it (the Blank's edge says yes, on the story's terms).
