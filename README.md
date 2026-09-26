# The Last Cartographer (code name: OWSBG)

A 2.5D action metroidvania in the spirit of *Hollow Knight: Silksong*, built in **Unity 6 (6000.3 LTS)**.
Hand-drawn ink-and-wash birds on layered paper dioramas with real depth. A kingdom that forgot
how to fly, a map that is forgetting itself, and a small cartographer with a needle-quill.

> *She fell off the edge of the map and drew her way back.*

## Repository layout

| Path | What it is |
|---|---|
| `docs/01-engine-decision.md` | Why Unity 6, what was rejected, the 2.5D side-scrolling recipe |
| `docs/02-production-plan.md` | Roles, milestones, task IDs (NAR/DES/CMB/PRG/CHR/ENV/AUD/PRO), first sprint, cut list |
| `docs/story/story-bible.md` | Narrative bible: kingdom, factions, map and ability gates, secrets, 15 bosses, plot, Commissions, endings |
| `docs/story/dialogue-style-guide.md` | Voice, cryptic register, Yarn conventions |
| `docs/design/game-design-overview.md` | Pillars, loop, systems, scope |
| `docs/design/combat-and-movement.md` | Wren's kit, Inkwell, Flourishes, Charters, Instruments, enemy and boss rules |
| `docs/design/art-direction.md` | Ink-on-paper look, 2.5D definition, palettes, the `_Ink` fade state |
| `LastCartographer/` | The Unity 6 project (editor 6000.3.7f1) |
| `logs/` | Batch-mode editor logs and the greybox screenshot |

## Getting started
1. Unity Hub → add editor **6000.3.7f1** (on the origin machine: `D:\Unity\6000.3.7f1`).
2. Open `LastCartographer/`. The greybox room is `Assets/_Project/Scenes/Greybox/Greybox_Saltmarrow.unity`.
3. Read the story bible, then the combat doc, then the plan. Sprint 1 starts with Wren's controller.

## Playing the greybox
Open `Assets/_Project/Scenes/Persistent/Persistent.unity` and press Play. The RoomManager loads
`Greybox_Saltmarrow_A`; walk off the right edge to transition into room B (a Talonhold shaft is there).

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD / arrows | left stick / d-pad |
| Jump (hold for height) | Space | South |
| Quill strike (up / down in air with stick) | J | West |
| Wingbeat dash | Shift or K | RB / RT |
| Bind (hold, spends 3 ink) | E | East |
| Survey (hold at a vantage) | Q | North |

Wingbeat and Talonhold are pre-unlocked in the greybox for feel-testing (`AbilitySet` on the Wren object).

## Verifying headless
```
Unity.exe -batchmode -projectPath LastCartographer -runTests -testPlatform PlayMode -testResults logs/playmode-results.xml
```
The play-mode tests check the combat doc's frame data on the real controller: landing, held and tapped jump heights, coyote time, dash distance and gating, and the down-strike pogo.

## Tooling
- **Unity 6 / URP Forward+**, Cinemachine 3, Input System, Addressables, Yarn Spinner 3.
- **Krita / Aseprite** for hand-drawn frames; **Unity 2D Animation** for boss rigs.
- **Blender 5.2** for light 3D props behind the play plane.
- **Git + LFS**.
