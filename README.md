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
In the editor menu bar choose **OWSBG → Play From Start** (Ctrl+Shift+P). It opens the persistent
scene plus the first room and enters Play mode. Then **click inside the Game view once** so it has
keyboard focus (Unity only sends input to a focused Game view). Walk off the right edge to transition
into room B, which has a Talonhold shaft.

Opening `Assets/_Project/Scenes/Persistent/Persistent.unity` by hand also auto-opens the first room,
and pressing Play inside any room scene bootstraps the persistent scene for you.

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD / arrows | left stick / d-pad |
| Jump (hold for height) | Space | South |
| Quill strike (up / down in air with stick) | J | West |
| Wingbeat dash | Shift or K | RB / RT |
| Bind (hold, spends 3 ink) | E | East |
| Flourish: neutral = Crosshatch, forward = Longstroke, up = Blot | L | LT / LB |
| Survey (hold at a vantage) | Q | North |

Wingbeat and Talonhold are pre-unlocked in the greybox for feel-testing (`AbilitySet` on the Wren object).

Room A also has the first world interactions. Stand in front of something and press **up** (W or stick up) to use it:
- the dark bird on the left is a stand-in Sable: a Yarn conversation with options (1-3 or arrows, J to confirm);
- the small table is a drafting desk: restores masks, sets the respawn point, saves to `saves/slot0.json`;
- the blue post on the right is a vantage point: hold **Q** on it to survey it (Sable notices afterwards).

Dialogue lives in `Assets/_Project/Dialogue/**/*.yarn`, compiled by `LastCartographer.yarnproject`. Custom commands: `<<flag key value>>`, `<<tutorial name>>`, `<<bind_prompt id>>`; functions: `flag("key")`, `has_flag("key")`, `surveyed("id")`.

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
