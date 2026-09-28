# 01 — Engine Decision

**Decision: Unity 6 (6000.3 LTS), Universal Render Pipeline (Forward+), C#.**

*Hollow Knight* and *Silksong* are themselves Unity games. That is not the reason to pick Unity, but it is proof the target feel is reachable here.

## Why Unity 6

| Need | How Unity 6 meets it |
|---|---|
| Tight 2D action at 60 fps | 2D physics with a custom kinematic character controller (no Rigidbody-driven movement), fixed 60 Hz step, Input System with buffers. Same recipe as the reference games. |
| 2.5D: 2D gameplay plane, real depth behind it | Perspective camera over layered sprite/paper planes and light 3D props. Cinemachine 3 for follow, look-ahead, confiners. |
| Hand-drawn animation | Sprite-sheet import with 2D Animation for bone-rigged bosses; Aseprite/Krita pipeline. |
| The "ink" render state | Shader Graph: one `_Ink` parameter per material drives line weight, wash saturation, paper grain, and layer dropout. Forward+ lights for lamps and the Blank's lantern-radius. |
| Interconnected world without loading screens | Addressables + additive scene "rooms" streamed by a `WorldStreamer`; a region is a graph of rooms with transitions hidden behind doors and shafts. |
| Story: cryptic dialogue, flags, generated Act 3 | Yarn Spinner + a `WorldState` flag store + ScriptableObject data for Commissions, places, and islands. Timeline for the few cutscenes. |
| Already installed | Editor 6000.3.7f1 at `D:\Unity\6000.3.7f1` with Windows Standalone and WebGL modules. |

## Considered and rejected
- **Godot 4.7** (installed). Excellent 2D; weaker at mixing real 3D depth and post-processing with 2D gameplay; dialogue tooling community-maintained. Fallback if Unity licensing becomes a problem.
- **Unreal 5.** Paper2D is under-maintained; heavy for this scale.
- **Custom (MonoGame/Bevy).** Loses the scene, timeline, animation, and localization tooling the project needs.

## The 2.5D recipe
1. **Gameplay plane** at Z = 0. All colliders are 2D. Wren's controller is kinematic: raycast/boxcast against `Physics2D`, with explicit coyote time, jump buffering, apex hang, and dash i-frames.
2. **Camera**: perspective, FOV about 30°, 18 units from the plane, 4° downward tilt. Cinemachine 3 `CinemachineCamera` with `PositionComposer`, look-ahead, and a `Confiner2D` per room.
3. **Parallax**: layers at Z = -6 (foreground, blurred), -2, 0 (play), +3, +8, +16, +30 (sky). Parallax is free from the perspective camera; no scripted offset.
4. **Sprites**: quads with a custom `InkSprite` Shader Graph: alpha clip, two-step hand-drawn shadow ramp, `_Ink` state, paper-grain modulation. Receive Forward+ lights; cast shadows only on the play layer.
5. **Post**: URP Volume per region: colour grading, vignette, depth of field on the foreground layer only (via a render feature masking Z < -1), extreme DoF in the Greyfold.
6. **Rooms**: each sub-zone is a set of rooms (additive scenes). Transitions preload the neighbour. Bosses are their own room with a locked arena.
7. **Save**: JSON `WorldState` plus per-room deltas, atlas survey bitfield, Commissions state, Charter/Instrument loadout.

## Packages (installed by `Assets/Editor/Setup/PackageInstaller.cs`)

| Package | Purpose |
|---|---|
| `com.unity.render-pipelines.universal`, `com.unity.shadergraph` | Rendering, the `InkSprite` shader, Volumes |
| `com.unity.cinemachine` | Camera rigs, confiners, boss cameras |
| `com.unity.inputsystem` | Buffered, rebindable input; gamepad first |
| `com.unity.2d.sprite`, `com.unity.2d.animation`, `com.unity.2d.psdimporter` | Sprite editing, boss bone rigs, layered PSD/Krita import |
| `com.unity.timeline` | Cutscenes and boss intros |
| `com.unity.addressables` | Room streaming |
| `com.unity.localization` | String tables. Installed but not the pipeline as of PRG-19: dialogue uses Yarn Spinner's own CSV localisations, and UI text uses `Loc` CSV tables (`docs/design/localisation.md`). Kept for smart strings and asset tables if they're wanted |
| `com.unity.ai.navigation` | NPC pathing in hubs |
| `com.unity.splines` | Enemy patrol paths, Inkthread swing arcs, updraft curves |
| `com.unity.probuilder` | Greybox rooms |
| `dev.yarnspinner.unity` (OpenUPM) | Dialogue and Commissions scripting |
| `com.unity.test-framework` | Controller and WorldState tests |

## Conventions
- Assemblies: `OWSBG.Core`, `OWSBG.World`, `OWSBG.Narrative`, `OWSBG.UI`, `OWSBG.Editor`.
- Data-driven content: places, Commissions, Charters, Instruments, enemies, and bosses are ScriptableObjects.
- Linear colour, Forward+, new Input System only, 60 Hz fixed step.
- `Assets/_Project/` for project assets; `Assets/ThirdParty/` for everything else.
