# 02 — Production Plan

How the work is divided, in what order, and who owns what. Task IDs are stable; reference them in commits (`[CMB-03]`), branches, and the tracker. `SB-x.y` points to the story bible; `CMB-x` to `design/combat-and-movement.md`; `ART-x` (in the art doc) to `design/art-direction.md`.

## 1. Team shape

Assumes 3 to 6 people with overlapping roles. Solo: follow the order within each milestone; dependencies still hold.

| Role | Owns | Tools |
|---|---|---|
| **Creative Director / Lead Writer** | Story bible, Commissions, dialogue, bosses-as-characters, endings | Yarn Spinner, Markdown, Timeline |
| **Lead Programmer** | Architecture, character controller, save/streaming, dialogue runtime, tooling | Unity 6, C#, Addressables |
| **Combat Designer / Gameplay Programmer** | Wren's kit, enemies, bosses, tuning, Charters, Instruments | Unity, Cinemachine, Input System |
| **Art Director / Character Artist** | Ink style, Wren, enemies, bosses, portraits, hand-drawn animation | Krita/Aseprite, 2D Animation |
| **Environment Artist** | Paper kits per region, parallax layers, props, lighting | Krita, Blender (props), URP |
| **Technical Artist** | `InkSprite` shader, `_Ink` fade state, paper grain, VFX, post | Shader Graph, render features |
| **Audio Designer / Composer** | Score per region, roll-call leitmotif, combat SFX, ambience | DAW, Unity Audio Mixer |
| **Producer / QA** | Tracker, milestones, playtests, builds | Git, CI, this document |

## 2. Milestones

| # | Milestone | Exit criteria | Length |
|---|---|---|---|
| **M0** | **Pre-production** | Bible, combat doc, art direction signed off. Wren's controller feels right in a greybox. `InkSprite` shader proven. Room streaming proven. Yarn in-engine. | 8 weeks |
| **M1** | **Vertical slice** | Prologue + Saltmarrow's first third: final-quality art in two rooms, Wren's base kit + Wingbeat, three enemy types, the Lamp-Keeper boss, drafting desk, Commissions ledger, survey, save/load. External testers. | 12 weeks |
| **M2** | **First playable** | Act 1 complete (Saltmarrow, one climb region, Halden at greybox-plus). Inkwell, Charters (3), Instruments (4), Halvard recurring, fade stages, atlas UI. | 14 weeks |
| **M3** | **Alpha (content complete)** | All regions, bosses, Commissions, endings at greybox or better. All dialogue written. Finishable. | 22 weeks |
| **M4** | **Beta** | All art, animation, music, SFX final. Localization. Performance. Weekly full playthroughs. | 14 weeks |
| **M5** | **Release candidate** | Bug bar, accessibility, store builds, marketing. | 6 weeks |

Total about 76 weeks. Cut Commissions before bosses, optional bosses before regions, regions before endings.

## 3. Tasks

Status: `[ ]` todo, `[~]` in progress, `[x]` done. `after:` lists dependencies.

### 3.1 Narrative (NAR)

| ID | Task | M | Ref | after |
|---|---|---|---|---|
| NAR-01 `[x]` | Story bible v2 (Silksong-inspired framing, bird kingdom, bosses, abilities) | M0 | SB-all | — |
| NAR-02 `[x]` | Dialogue style guide incl. cryptic-register rules | M0 | SB-11 | NAR-01 |
| NAR-03 `[~]` | Prologue script (the Edge with Isolde) in Yarn; playable end to end in the greybox (arrive, survey, bind, seal, smudges, departure, shore); Isolde fighting beside Wren and the real Greyfold room are open | M0 | SB-7.0 | PRG-08 |
| NAR-04 `[~]` | Saltmarrow arc: Sable, Dotha, the Lamp-Keeper, Halvard's first hunt; Commissions (5); v1 written and staged (`Saltmarrow_*.yarn`, `docs/story/saltmarrow-arc.md`); the widow's own voice and the Ferrymen's second voices open | M1 | SB-4.1, 8.1 | NAR-02 |
| NAR-05 `[~]` | Recurring-character bibles: Pell, Sable, Runa, Teodor, Marrow (where they appear on the map, what they say each time); v1 in `docs/story/character-bibles.md` and as data (`Cast`, tested against the map and the Yarn project); Marrow's four words and Pell's species open | M1 | SB-6 | NAR-01 |
| NAR-06 `[ ]` | Boss character sheets: reason, arena, three phase lines, aftermath, for all 15 | M1 | SB-6 | NAR-01, CMB-09 |
| NAR-07 `[ ]` | Emberdown arc: Kettil, Runa, Hollowvein, the Cinder Bath Debate, Brann; Commissions | M2 | SB-4.2, 8.2 | NAR-05 |
| NAR-08 `[ ]` | Verdance arc: Teodor, Aldermere, the Sunken Library, the Gatekeeper; Commissions | M2 | SB-4.3, 8.3 | NAR-05 |
| NAR-09 `[ ]` | Halden arc: Orchard cache, Lowmarket strike, Interludes A and B, Oriel; Commissions | M2 | SB-4.4, 7.2 | NAR-04 |
| NAR-10 `[ ]` | Windreach arc: the moving camp, Idrenne's Fire, Hale, the Fallen Star | M3 | SB-4.5, 8.5 | NAR-05 |
| NAR-11 `[ ]` | The Threshold: Voss confrontation, all variants | M3 | SB-7.2, 6.11 | NAR-09 |
| NAR-12 `[ ]` | Act 3: Isolde's camp, Thessaly Hollow, Corra, Corvin, the Return | M3 | SB-7.3, 5.x | NAR-07..11 |
| NAR-13 `[ ]` | Endings and epilogues (4 + Voss); Marrow's word | M3 | SB-9 | NAR-12 |
| NAR-14 `[ ]` | Blank island dialogue for every unanchored place | M3 | SB-8.6 | NAR-07..10 |
| NAR-15 `[ ]` | Environmental storytelling pass: inscriptions, corpses, tapestries, the fledgling loops | M3 | SB-5.6 | ENV-06 |
| NAR-16 `[ ]` | Foreshadowing audit (three plants per secret), `#still` tagging | M4 | SB-11 | NAR-13 |
| NAR-17 `[ ]` | Item, Charter, Instrument, memory, and atlas flavour text | M4 | CMB-5, 6 | DES-05 |
| NAR-18 `[ ]` | Localization-ready pass | M4 | — | PRG-19 |

### 3.2 Design (DES)

| ID | Task | M | Ref | after |
|---|---|---|---|---|
| DES-01 `[x]` | Game design overview | M0 | GDD | NAR-01 |
| DES-02 `[~]` | Survey spec: vantage points, atlas inking animation, fast travel, erasure/re-survey; v1 in `docs/design/survey.md`, the page's inking animation and line-of-sight vantages open | M0 | SB-10 | DES-01 |
| DES-03 `[~]` | Anchor / hold / release spec; held-state rules; Warden patrols in anchored towns; v1 in `docs/design/anchoring.md`, bind requirement and licence rules open | M0 | SB-1.3, 10 | DES-02 |
| DES-04 `[~]` | Fade-stage spec (0–4, story-beat advancement, `_Ink` values per stage); v1 in `docs/design/fade-stages.md`, per-region curves open | M0 | SB-10, ART-3 | DES-01 |
| DES-05 `[~]` | Economy: iris seeds, vellum scraps, Instrument prices, mask/quill upgrades; v1 in `docs/design/economy.md` with seeds, drops, Sable's shop, desk mask and belt upgrades in the greybox; quill upgrades and caches open | M1 | CMB-6 | DES-01 |
| DES-06 `[~]` | Commissions system spec (ledger, states, rewards, Blank-island flags); v1 in `docs/design/commissions.md`, failure and expiry rules open | M1 | SB-8 | DES-01 |
| DES-07 `[~]` | World macro map: region graph, room counts, ability gates, sequence-break policy; v1 in `docs/design/world-map.md` and as data (`WorldGraph`, reachability tests); per-region layouts are DES-08–11 | M0 | SB-4.0 | DES-01 |
| DES-08 `[~]` | Saltmarrow room-by-room level design (paper maps, vantage points, gauntlet); v1 for the slice in `docs/design/saltmarrow-rooms.md`, built as recipe rooms in the greybox (16 rooms); the rest of the coast sketched | M0 | SB-4.1 | DES-07 |
| DES-09 `[ ]` | Emberdown and Verdance room designs | M2 | SB-4.2, 4.3 | DES-07 |
| DES-10 `[ ]` | Halden room designs incl. flyer-towers | M2 | SB-4.4 | DES-07 |
| DES-11 `[ ]` | Windreach, Greyfold, Blank room designs (drifting islands, lantern-radius platforms) | M3 | SB-4.5–4.7 | DES-07 |
| DES-12 `[ ]` | Ending requirement matrix as flag logic; every ending reachable | M3 | SB-9 | NAR-13 |
| DES-13 `[~]` | Bounds-walk rhythm spec and three authored walks; v1 in `docs/design/bounds-walk.md` (Merrow's End built, Kettil's Rest and Hollowvein designed) | M2 | CMB-10 | DES-03 |
| DES-14 `[ ]` | Accessibility: remap, hold/toggle, hitstop and shake sliders, high-contrast ink, no timed dialogue | M4 | — | PRG-05 |

### 3.3 Combat (CMB)

| ID | Task | M | Ref | after |
|---|---|---|---|---|
| CMB-01 `[~]` | Wren controller: kinematic 2D, buffers, coyote, variable jump, apex hang; tuned in greybox | M0 | CMB-1, 2 | PRG-05 |
| CMB-02 `[~]` | Quill strike (3 directions), combo, down-strike pogo, hitstop, hit reactions | M0 | CMB-2.1 | CMB-01 |
| CMB-03 `[~]` | Inkwell + Bind | M0 | CMB-2.2, 4 | CMB-02 |
| CMB-04 `[~]` | Wingbeat, Talonhold, Inkthread, Windmemory implementations with ability gating | M1–M2 | CMB-3 | CMB-01 |
| CMB-05 `[~]` | Flourishes: Crosshatch, Longstroke, Blot | M1 | CMB-4 | CMB-03 |
| CMB-06 `[~]` | Charters (3 base) with combo rewrites and silhouettes (greybox: combo, passives, default Flourish, tint; silhouettes await CHR art) | M2 | CMB-5 | CMB-05 |
| CMB-07 `[~]` | Instruments (7) and slot system (greybox effects for all 7; Compass-dart marks await Inkthread, tether-hook is a marker) | M2 | CMB-6 | CMB-03 |
| CMB-08 `[~]` | Enemy framework: state machine, telegraphs, "answer" tagging, families | M1 | CMB-7 | CMB-02 |
| CMB-09 `[~]` | Enemy roster: 12 for M1, 40 by M3 (greybox: Marsh Crab, Reed Skimmer, Smudge, Warden) | M1–M3 | CMB-7 | CMB-08 |
| CMB-10 `[~]` | Boss framework: phases, arena states, intro through the Timeline pipeline on first entry, retry loop under 8 s; outro cutscene pending | M1 | CMB-8 | CMB-08 |
| CMB-11 `[~]` | The Lamp-Keeper (6.1): greybox kit (beam, dive, double beam), three phases, rewards | M1 | SB-6.1 | CMB-10 |
| CMB-12 `[ ]` | Halvard recurring (6.3 × 3 kits) | M2 | SB-6.3 | CMB-10 |
| CMB-13 `[ ]` | The Collapse, Brann, the Gatekeeper, the Choir | M2–M3 | SB-6.4–6.7 | CMB-10 |
| CMB-14 `[ ]` | Oriel, Hale, the Fallen Star | M3 | SB-6.8–6.10 | CMB-10 |
| CMB-15 `[ ]` | Voss (arena-anchoring phase) | M3 | SB-6.11 | CMB-10 |
| CMB-16 `[ ]` | Bells, Corra's Drawing, the Archivist, the Complete Survey | M3 | SB-6.12–6.15 | CMB-10 |
| CMB-17 `[ ]` | Late Charters (Ferryman, Unwriter, Remnant) | M3 | CMB-5 | CMB-06 |
| CMB-18 `[ ]` | Traversal gauntlets (one per region) | M2–M3 | CMB-9 | CMB-04 |
| CMB-19 `[ ]` | Tuning pass: damage tables, telegraph frames per tier, Inkwell economy | M4 | CMB-all | all |

### 3.4 Programming (PRG)

| ID | Task | M | Ref | after |
|---|---|---|---|---|
| PRG-01 `[x]` | Unity 6 project, URP Forward+, packages, asmdefs, folders | M0 | Engine | — |
| PRG-02 `[x]` | Persistent scene + two greybox rooms: side-on Cinemachine rig, parallax layers, sun, post volume, Wren rig, dummy | M0 | Engine | PRG-01 |
| PRG-03 `[~]` | `InkSprite` shader (HLSL, not Shader Graph): alpha clip, two-step ramp, `_Ink` state, paper grain; done for the greybox, needs the art pass (line weight, wash) once sprites exist | M0 | ART-3 | PRG-02 |
| PRG-04 `[~]` | Foreground-only depth-of-field render feature; paper-grain overlay feature (both as full-screen passes on the renderer; tuning against real art pending) | M0 | ART-3 | PRG-03 |
| PRG-05 `[x]` | Input: `.inputactions`, buffered input service, rebinding | M0 | CMB-1 | PRG-01 |
| PRG-06 `[~]` | Cinemachine 3 rig: follow, look-ahead, `Confiner2D` per room, boss cameras (fixed arena camera per BossArena; cutscene shots; per-region tuning pending) | M0 | Engine | CMB-01 |
| PRG-07 `[~]` | Room system: additive scenes, transitions, neighbour preload, Addressables (rooms are addressables in one bundle each; neighbour bundles kept resident; Build Settings fallback; content build entry point, no CI yet) | M0 | Engine | PRG-01 |
| PRG-08 `[~]` | Dialogue runtime: Yarn Spinner, `<<flag>>`, `<<commission>>`, `<<fade>>`, `<<anchor>>`, dialogue UI | M0 | SB-11 | PRG-01 |
| PRG-09 `[~]` | `WorldState` + save/load (JSON, autosave at desks) | M0 | SB-10 | PRG-01 |
| PRG-10 `[~]` | Survey system: vantage points, atlas reveal, fast travel, erasure (Atlas store, atlas page as pause screen, Cantor bell erasure and recovery by re-survey, travel points at desks and lit lamps; page inking animation pending) | M1 | DES-02 | PRG-08 |
| PRG-11 `[~]` | Drafting desk: rest, respawn, Charter/Instrument swap (placeholder IMGUI desk menu), save | M1 | GDD 6 | PRG-09 |
| PRG-12 `[~]` | Commissions ledger runtime and journal (state machine in WorldState, tracker, ledger page, journal + toasts, Yarn command and functions, Saltmarrow greybox set; journal hosted on the atlas page) | M1 | DES-06 | PRG-09 |
| PRG-13 `[~]` | Anchor / hold / release runtime; held-state loops; Warden patrol spawner (Places store, HeldState grade lock + Wardens, desk place row, Yarn verbs; schedule loops and the locked hour in PRG-15) | M2 | DES-03 | PRG-10 |
| PRG-14 `[~]` | Fade-stage runtime: `_Ink` animation per place, layer dropout, story-beat advancement (FadeStages + FadeGroup per room, `<<fade>>`; the Remnant look and audio pending) | M2 | DES-04 | PRG-03 |
| PRG-15 `[~]` | NPC schedules and hub life (day clock with dawn/day/dusk/night, desk rest sleeps, per-phase posts and nodes, anchored hour lock and `#still` loops, hour tint; v1 in `docs/design/hub-life.md`; NavMesh / cross-room routes and shops open) | M2 | SB-5.1 | PRG-07 |
| PRG-16 `[~]` | Cutscene pipeline: Timeline + Cinemachine + Yarn hooks (Cutscene object, actor-move / paper-fade / dialogue-node clips, `<<cutscene>>` waits, shot camera; boss intros still coroutines) | M1 | — | PRG-06 |
| PRG-17 `[~]` | Death and smudge recovery | M1 | GDD 6 | PRG-09 |
| PRG-18 `[ ]` | Clarity meter and lantern-radius rendering in the Blank | M3 | SB-4.7 | PRG-03 |
| PRG-19 `[ ]` | Localization wiring | M2 | — | PRG-08 |
| PRG-20 `[ ]` | Blank island generator from `WorldState` | M3 | SB-8.6 | PRG-14 |
| PRG-21 `[ ]` | Moving camp (Windreach) and day-advance travel | M3 | SB-4.5 | PRG-15 |
| PRG-22 `[~]` | Bounds-walk rhythm runtime (BoundsWalk set piece, roll-call strip, `<<walk>>`, hold on completion; the desk defers Hold to it; music pending) | M2 | DES-13 | PRG-08 |
| PRG-23 `[ ]` | Endings runner and epilogue walk | M3 | SB-9 | DES-12 |
| PRG-24 `[ ]` | Performance: streaming budget, sprite batching, 60 fps lock | M4 | — | all |
| PRG-25 `[ ]` | Build pipeline: CI, Windows build, Steam packaging | M4 | — | PRG-01 |
| PRG-26 `[~]` | Tests: controller frame-data, WorldState, save round-trip, ending reachability | M3 | — | PRG-09 |

### 3.5 Character art and animation (CHR)

| ID | Task | M | Ref | after |
|---|---|---|---|---|
| CHR-01 `[x]` | Art direction doc | M0 | ART-all | NAR-01 |
| CHR-02 `[ ]` | Wren model sheet and turnaround; ink style test | M0 | ART-4 | CHR-01 |
| CHR-03 `[ ]` | Wren core animation: idle, run, jump, fall, land, strike ×3, pogo, Bind, survey, hurt, death | M0–M1 | CMB-2 | CHR-02 |
| CHR-04 `[ ]` | Wren ability animation: Wingbeat, Talonhold, Inkthread, Windmemory, Flourishes | M1–M2 | CMB-3, 4 | CHR-03 |
| CHR-05 `[ ]` | Charter silhouettes for Wren (cowl and grip variants) | M2 | CMB-5 | CHR-03 |
| CHR-06 `[ ]` | Saltmarrow enemies (6) and the Lamp-Keeper | M1 | SB-6.1 | CHR-02 |
| CHR-07 `[ ]` | Warden family (Halvard, Brann, Oriel, generic ×3) | M2 | SB-3.1 | CHR-02 |
| CHR-08 `[ ]` | Cantor family and the Choir | M2 | SB-3.3 | CHR-02 |
| CHR-09 `[ ]` | Smudge family (5) and the Collapse | M2 | SB-6 | CHR-02 |
| CHR-10 `[ ]` | Remaining bosses (Gatekeeper, Hale, Fallen Star, Voss, Bells, Corra's Drawing, Archivist, Complete Survey) | M3 | SB-6 | CHR-02 |
| CHR-11 `[ ]` | NPC cast: Isolde, Pell, Sable, Runa, Teodor, Kettil, Idrenne, Maren, Corvin, Ilse, Corra, Marrow (+ colour states) | M2–M3 | SB-6 | CHR-02 |
| CHR-12 `[ ]` | Generic townsfolk library (30) and Remnant (grey) variants | M3 | — | CHR-02 |
| CHR-13 `[ ]` | Portraits for dialogue | M3 | — | CHR-11 |
| CHR-14 `[ ]` | Fledgling background loops per region (glide distance per ability) | M3 | SB-5.6 | CHR-02 |

### 3.6 Environment art (ENV)

| ID | Task | M | Ref | after |
|---|---|---|---|---|
| ENV-01 `[ ]` | Paper-kit pipeline test: one Saltmarrow room from greybox to final, with parallax and `_Ink` fade | M0 | ART-5 | PRG-03 |
| ENV-02 `[ ]` | Saltmarrow kit and rooms (vertical slice: 2 rooms final, rest greybox) | M1 | SB-4.1 | ENV-01, DES-08 |
| ENV-03 `[ ]` | Emberdown kit and rooms | M2 | SB-4.2 | ENV-01 |
| ENV-04 `[ ]` | Verdance kit and rooms | M2 | SB-4.3 | ENV-01 |
| ENV-05 `[ ]` | Halden kit and rooms incl. flyer-towers and the Observatory | M2–M3 | SB-4.4 | ENV-01 |
| ENV-06 `[ ]` | Environmental storytelling props (inscriptions, tapestries, corpses) | M3 | NAR-15 | ENV-02..05 |
| ENV-07 `[ ]` | Windreach kit, grass system, updraft ink-swirls | M3 | SB-4.5 | ENV-01 |
| ENV-08 `[ ]` | Greyfold and Blank: white-out, lantern-radius, drifting islands | M3 | SB-4.6, 4.7 | PRG-18 |
| ENV-09 `[ ]` | Hubs dressing: drafting desks, ledgers, shops | M2 | — | ENV-02 |
| ENV-10 `[ ]` | Lighting and post per region | M4 | ART-5 | ENV-all |
| ENV-11 `[~]` | UI art: atlas book, masks, Inkwell, ledger, Charter/Instrument screens, fonts (UI Toolkit runtime layer in place: HUD, dialogue page, desk page, boss bar; art and fonts pending) | M1–M2 | ART-6 | CHR-01 |
| ENV-12 `[ ]` | VFX: ink splashes, Flourish scribbles, Bind redraw, erasure, Blank edge | M2–M3 | ART-7 | PRG-03 |
| ENV-13 `[ ]` | Key art, capsule, screenshots, trailer assets | M5 | — | all |

### 3.7 Audio (AUD)

| ID | Task | M | Ref | after |
|---|---|---|---|---|
| AUD-01 `[ ]` | Audio direction: instrumentation per region, the roll-call as leitmotif, combat mix rules | M0 | SB-3.4 | NAR-01 |
| AUD-02 `[ ]` | The roll-call song (Runa; the whale; the true-ending chorus) | M1 | SB-8.1, 9.2 | AUD-01 |
| AUD-03 `[ ]` | Wren SFX: strikes, pogo, dash, thread, Bind, survey; hit and kill layers | M1 | CMB-2 | CMB-02 |
| AUD-04 `[ ]` | Saltmarrow theme, Lamp-Keeper boss theme | M1 | SB-4.1 | AUD-01 |
| AUD-05 `[ ]` | Ambience per region with fade-stage filtering | M2 | SB-10 | PRG-14 |
| AUD-06 `[ ]` | Region themes: Emberdown, Verdance (near-silent), Halden, Windreach | M3 | SB-4 | AUD-01 |
| AUD-07 `[ ]` | Boss themes (Halvard, Brann, Voss, Archivist; shared motifs for optionals) | M3 | SB-6 | AUD-01 |
| AUD-08 `[ ]` | The Blank: reversed motifs, Remnant voices; endings and epilogue | M3 | SB-4.7, 9 | AUD-02 |
| AUD-09 `[ ]` | Mixer, ducking, snapshots (dialogue, combat, boss, Blank) | M4 | — | AUD-05 |

### 3.8 Production and QA (PRO)

| ID | Task | M | after |
|---|---|---|---|
| PRO-01 `[x]` | Repo, `.gitignore`, LFS attributes, folder layout | M0 | — |
| PRO-02 `[ ]` | Tracker with these IDs; weekly build cadence | M0 | PRO-01 |
| PRO-03 `[ ]` | Feel-test protocol for the controller (M0 gate) | M0 | CMB-01 |
| PRO-04 `[ ]` | External test round 1 (vertical slice) | M1 | M1 exit |
| PRO-05 `[ ]` | Full-playthrough matrix: each ending, each region order, sequence breaks | M3 | DES-12 |
| PRO-06 `[ ]` | Performance targets: 60 fps at 1080p on GTX 1060-class; room transition under 100 ms | M4 | PRG-24 |
| PRO-07 `[ ]` | Bug bar and triage | M4 | — |
| PRO-08 `[ ]` | Store page, trailer, press kit | M5 | ENV-13 |

## 4. Critical path

```
NAR-01 bible + CMB doc + ART doc
  └─ PRG-05 input ─ CMB-01 controller (feel gate, PRO-03)
       ├─ CMB-02 strike/pogo ─ CMB-03 Inkwell ─ CMB-08 enemies ─ CMB-10 boss framework ─ CMB-11 Lamp-Keeper
       ├─ PRG-03 InkSprite ─ ENV-01 pipeline test ─ ENV-02 Saltmarrow
       └─ PRG-07 rooms ─ PRG-08 dialogue ─ PRG-09 WorldState ─ PRG-10 survey ─ PRG-11 desk ─ PRG-12 ledger
            └─ M1 vertical slice
                 └─ CMB-04 abilities ─ regions (any order) ─ PRG-14 fade ─ PRG-20 islands ─ PRG-23 endings ─ M3
```

## 5. First sprint (two weeks)

1. **CMB-01** Wren's controller in the greybox room until it feels right. Nothing else matters until this does. Frame data from the combat doc; tune by hand.
2. **CMB-02** Strike and pogo on a dummy. Hitstop. Ink splash placeholder.
3. **PRG-03** `InkSprite` shader with `_Ink` slider; put the placeholder Wren on it; fade her to paper and back.
4. **CHR-02** Wren model sheet and ink style test (two poses, one strike frame).
5. **NAR-03** Prologue in Yarn (the Edge).
6. **PRG-07** Two rooms with a transition and neighbour preload.
7. **AUD-01/02** Audio direction and a first sketch of the roll-call.

Exit: Wren runs, jumps, pogos a dummy across two connected rooms, looks like an ink drawing, and can be faded to blank paper with one slider.

## 6. Cut list (in order)
1. Optional bosses 6.2, 6.10 (Brood, Fallen Star).
2. Late Charters (keep three).
3. Windreach moving camp (static camp).
4. Ending 9.4.
5. The Choir (6.6) — Aldermere resolves by dialogue only.
6. Halden Commissions 3 and 5.

Never cut: the four regional decisions, the Blank built from choices, Voss and the Archivist, the three main endings, the fledgling loops.
