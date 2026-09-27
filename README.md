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
In the editor menu bar choose **OWSBG → Play From Start (the Edge)** (Ctrl+Shift+P). It opens the persistent
scene plus the prologue room and enters Play mode. **OWSBG → Play From Saltmarrow** skips the prologue and starts in room A. Then **click inside the Game view once** so it has
keyboard focus (Unity only sends input to a focused Game view). Walk off the right edge to transition
into room B, which has a Talonhold shaft.

**The prologue (the Edge).** A new game opens at the Greyfold's edge with Isolde: the paper thins, she asks what you see,
then sends you to the marker behind you (hold Q). Surveying it brings the binding and the seal (a wax seal is set where you stand),
then three smudges come out at dusk; killing them brings her last lesson and she walks into the white. Follow her past
where the wall was: the screen goes white and you wake on the Saltmarrow shore with Sable standing over you, and Act 1 begins.
Every beat is driven by flags (`prologue.*`), so reloading lands you in the right one. The scenes are `Greybox_Greyfold_Edge`
and room A's `Cutscene_shore_wake`; the script is `Dialogue/Greyfold/Prologue_Edge_Isolde.yarn`.

Past room B's east edge is the fourth lighthouse: a drafting desk, then two doors and **the Lamp-Keeper**,
the first boss (Tier I). Her beam sweeps the floor (jump it) and she dives at you (step aside); she is only
hittable while grounded after a dive. Three phases on health thirds. Beating her grants Wingbeat, a vellum
scrap, and turns her lamp into a beacon. Dying puts you back at the desk with the doors open; walk in again. The first entry plays a short intro on the perch
(a cutscene on the pipeline below) and a fixed arena camera holds the whole room until the fight ends either way.

Opening `Assets/_Project/Scenes/Persistent/Persistent.unity` by hand also auto-opens the first room,
and pressing Play inside any room scene bootstraps the persistent scene for you.

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD / arrows | left stick / d-pad |
| Jump (hold for height) | Space | South |
| Quill strike (up / down in air with stick) | J | West |
| Wingbeat dash | Shift or K | RB / RT |
| Bind (hold, spends 3 ink) | E | East |
| Flourish: neutral = Charter default, forward = Longstroke, up = Blot | L | LT / LB |
| Use Instrument (selected slot) | I | right stick press |
| Select next Instrument slot | Tab | left stick press |
| Survey (hold at a vantage) | Q | North |
| Journal (commissions, scraps) | M | Select |

Forward strikes chain into the Charter's three-hit combo when pressed within 18 frames of the last swing.
Every base Charter and every Instrument is owned from the start in the greybox; the desk menu swaps them.

Wingbeat and Talonhold are pre-unlocked in the greybox for feel-testing (`AbilitySet` on the Wren object).

Room A also has the first world interactions. Stand in front of something and press **up** (W or stick up) to use it:
- the dark bird on the left is a stand-in Sable: a Yarn conversation on the paper page (1-3 or arrows, J to confirm);
- the paper sheet on a post between Sable and the desk is the **Commissions ledger**: the coast's side-quests. Read it (up) to see what is posted, J takes one, and once the journal (M) shows it fulfilled, J at the board turns it in for vellum scraps. Three post from the start; Dotha's posts after you meet Sable and the Tether-Widows after the Lamp-Keeper;
- the small table is a drafting desk: restores masks and Instrument uses, sets the respawn point, saves to `saves/slot0.json`, and opens the desk menu (row 0 swaps the Charter with ◂ ▸ or 1-3; the rows below swap what sits in each Instrument slot; J leaves);
- the light brown slab up on the left wall is a weak floor (a plumb weight breaks it; the quill only scratches it) and the faint slab on the far right is a hidden platform (a Field lantern draws it for ten seconds);
- the blue post on the right is a vantage point: hold **Q** on it to survey it (Sable notices afterwards).

Room B's west end has Dotha, the last elder of Merrow's End. She only talks properly once her commission is taken; her decision seeds an island in the Blank (`blank.island.Merrows_End`). The design is in `docs/design/commissions.md`.

The look (art-direction doc): `OWSBG/InkSprite` is the lit, alpha-clipped sprite shader with the two-step shadow ramp and the
`_Ink` state; two full-screen passes sit on the URP renderer (`Settings/Rendering/URP_Renderer.asset`): **ForegroundBlur**
(before post-processing; anything nearer than the gameplay plane blurs with distance, read from depth) and **PaperGrain**
(after post-processing; static grain, stronger on light paper than on ink). Far layers blur through the volume's Gaussian DoF.
Materials `Art/Materials/M_FS_*.mat` hold the knobs.

Rooms are Addressables (`Assets/AddressableAssetsData`, group **Rooms**, one bundle per room, address = scene name);
only the persistent scene is a built-in scene. `RoomManager` loads a room by address, keeps the bundles of its neighbours
(the targets of its transitions) resident, and releases the rest. Play mode reads from the AssetDatabase, so nothing has to be
built to press Play; a player build needs `-executeMethod OWSBG.Setup.ProjectSetup.BuildAddressables` first.

Cutscenes (`Assets/_Project/Data/Cutscenes/*.playable`, built by the setup script) are Timeline assets on a `Cutscene` object:
`ActorMoveClip` walks an actor, `PaperFadeClip` drives the screen fade, `DialogueNodeClip` starts a Yarn node and holds the
timeline until it ends; an optional Cinemachine shot takes over while it plays and Wren is frozen throughout.

The HUD, dialogue page, desk page and boss bar are one UI Toolkit document (`UI` object in the persistent
scene; `Assets/_Project/Code/UI`), built in code on the paper-and-ink palette until the UI art (ENV-11) lands.

Dialogue lives in `Assets/_Project/Dialogue/**/*.yarn`, compiled by `LastCartographer.yarnproject`. Custom commands: `<<flag key value>>`, `<<tutorial name>>`, `<<bind_prompt id>>`, `<<commission id post|take|fulfil|close|fail>>`, `<<cutscene id>>` (waits for it); functions: `flag("key")`, `has_flag("key")`, `surveyed("id")`, `commission_state("id")`, `commission_is("id", "taken")`.

## Verifying headless
```
Unity.exe -batchmode -projectPath LastCartographer -runTests -testPlatform PlayMode -testResults logs/playmode-results.xml
```
The play-mode tests check the combat doc's frame data on the real controller, the enemies, Flourishes, Charters, Instruments, the boss loop, dialogue, commissions, cutscenes, the prologue end to end, room streaming, and the UI. `UiScreenshotTests` also writes `logs/ui-hud.png`, `ui-dialogue.png`, `ui-desk.png`, `ui-ledger.png` and `ui-journal.png` with the UI composited over the camera, for a headless visual check.

## Tooling
- **Unity 6 / URP Forward+**, Cinemachine 3, Input System, Addressables, Yarn Spinner 3.
- **Krita / Aseprite** for hand-drawn frames; **Unity 2D Animation** for boss rigs.
- **Blender 5.2** for light 3D props behind the play plane.
- **Git + LFS**.
