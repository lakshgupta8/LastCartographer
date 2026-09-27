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
| `docs/story/character-bibles.md` | The returning cast (Pell, Sable, Runa, Teodor, Marrow): image, voice, where they stand act by act, what they say each time |
| `docs/story/saltmarrow-arc.md` | Act 1's first push as written: beats, flags, who says what |
| `docs/story/emberdown-arc.md` | The Emberdown climb as written: Kettil, Runa, the debate, Hollowvein, Brann; Kettil's ledger |
| `docs/story/verdance-arc.md` | The Verdance climb as written: Teodor, Aldermere's last day, Ansel's page, the keystone check; the Quiet House's ledger |
| `docs/story/halden-arc.md` | The Plateau as written: Isolde's cache and the five names, the strike, Pell's report, the audience, Oriel; the Hall's ledger; Wren's voices |
| `docs/story/windreach-arc.md` | The Steppe as written: the three fires, the leap, Idrenne's Fire said plainly, Hale at the ninth stone, the Fallen Star; the camp's ledger |
| `docs/story/threshold.md` | The Greyfold as written for Acts 1–2: the act break, the notice, Marrow in the pool, Halvard's third, Voss's one speech in all its variants, Pell at the line |
| `docs/story/act3.md` | Act 3 as written: Isolde's atlas, the Hollow and Ilse, Corra's small drawing, Corvin's argument, the Return, Marrow's three words, Pell's last list |
| `docs/story/endings.md` | The frame, the four endings and what each needs (`Endings`), Voss's coda, the epilogue walk, Marrow's verdict, Wren's last line; the seventh keystone settled |
| `docs/story/blank-islands.md` | The Blank's islands as written: one per [B] commission, a generic Remnant island for any other released place (`Islands`), Aury by tether and with Sable, the tether itself |
| `docs/story/boss-sheets.md` | The fifteen bosses: reason, arena by phase, three lines, answers, aftermath |
| `docs/design/game-design-overview.md` | Pillars, loop, systems, scope |
| `docs/design/combat-and-movement.md` | Wren's kit, Inkwell, Flourishes, Charters, Instruments, enemy and boss rules |
| `docs/design/art-direction.md` | Ink-on-paper look, 2.5D definition, palettes, the `_Ink` fade state |
| `docs/design/emberdown-verdance-rooms.md` | The two climbs room by room: 40 rooms, their exits and gates, generated from the same source as `RoomPlans` |
| `docs/design/halden-rooms.md` | Halden Reach room by room: 21 rooms, the flyer-tower, the Vault and the dome, generated with `RoomPlans` |
| `docs/design/windreach-greyfold-blank-rooms.md` | The last three regions room by room: Windreach's walk and glide, the Greyfold's road that runs out, the Blank's fixed islands (35 rooms; the paper map complete) |
| `docs/design/death-and-retry.md` | Death, the return to the desk, and the smudge that holds bound memories |
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
into the stilt-roosts, and on through the boardwalk to Merrow's End (room B), which has a Talonhold shaft.

The greybox is now the vertical slice's seventeen rooms (`docs/design/saltmarrow-rooms.md`): the shore and its sea-fade to the
west of the quay; the stilt-roosts and the boardwalk east of it; Merrow's End, the tether-line and the ferry landing;
the first, second and faded third lighthouses before the fourth; and, up from the stilt-roosts, the four rooms of
Reedmother's Roots with vantages at the bole and the crown. Twelve of them are built from recipes in the setup script
(geometry, exits, vantages and enemies as data); the four hand-built rooms keep their bespoke content.

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
| Atlas (map, journal, travel) | M | Select |

Forward strikes chain into the Charter's three-hit combo when pressed within 18 frames of the last swing.
Every base Charter is owned from the start in the greybox; the desk menu swaps them. Wren starts with three Instruments (compass-dart, plumb weight, sighting lens) and buys the rest.

The economy (`docs/design/economy.md`): enemies drop **iris seeds** (✿ on the HUD) and a few lie about the rooms; ask Sable
what she is selling and her table opens (↑↓, J buys, Esc leaves): the field lantern, iris tincture, wax seal and tether-hook,
half again dearer if the iris fields burn. **Vellum scraps** (from commissions and the Lamp-Keeper) buy masks and the fourth
belt slot at any desk: the desk's Masks and Belt rows, J to buy.

Wingbeat and Talonhold are pre-unlocked in the greybox for feel-testing (`AbilitySet` on the Wren object).

Room A also has the first world interactions. Stand in front of something and press **up** (W or stick up) to use it:
- the dark bird on the left is Sable, captain of the Ferrymen: a Yarn conversation on the paper page (1-3 or arrows, J to confirm). She reads the coast's state: what you have drawn, what the board says, who has died and who has been declared so;
- the paper sheet on a post between Sable and the desk is the **Commissions ledger**: the coast's side-quests. Read it (up) to see what is posted, J takes one, and once the journal (M) shows it fulfilled, J at the board turns it in for vellum scraps. Three post from the start; Dotha's posts after you meet Sable and the Tether-Widows after the Lamp-Keeper;
- dying (`docs/design/death-and-retry.md`) returns you to the last desk with full masks and an empty Inkwell. Whatever you had **bound** (the prologue's memory, for one) stays where you fell as a smudge of your own; walk back and strike it down while it is drawn to take the memories back. Dying again moves the drop, it never loses it;
- the small table is a drafting desk: restores masks and Instrument uses, sets the respawn point, sleeps to the next dawn, saves to `saves/slot0.json`, and opens the desk menu (row 0 swaps the Charter with ◂ ▸ or 1-3; the rows below swap what sits in each Instrument slot; J leaves);
- the light brown slab up on the left wall is a weak floor (a plumb weight breaks it; the quill only scratches it) and the faint slab on the far right is a hidden platform (a Field lantern draws it for ten seconds; Sable sells the lantern);
- the blue post on the right is a vantage point: hold **Q** on it to survey it (Sable notices afterwards, and the atlas page marks it drawn).

Room B's west end has Dotha, the last elder of Merrow's End. She only talks properly once her commission is taken; she offers two ways, and a third once you have heard the whale; her decision seeds an island in the Blank (`blank.island.Merrows_End`). The design is in `docs/design/commissions.md`. In the fourth lighthouse, the moment the Lamp-Keeper falls, Warden-Sergeant Halvard walks in and measures you: the Guild has declared Isolde dead and you missing, and from that talk on you are unlicensed (`act1.unlicensed`). The lamp's housing can be read. East of the lamp, past a tide gap a jump and a Wingbeat wide (falling in costs a mask and puts you back on the bank), the Salt Chapel holds his first fight: thrusts to parry, lunges to jump, and a survey that marks the floor under you until he calls the count and every mark erupts. The arc's beat sheet is `docs/story/saltmarrow-arc.md`. The returning cast is in `docs/story/character-bibles.md` and as data in `Cast` (Core): every member and every appearance by act and map zone, with the scenes that exist marked staged; the tests hold the data, the map and the Yarn project to each other. The bosses are in `docs/story/boss-sheets.md` and as `Bosses` (Core): the staged Lamp-Keeper takes her name, tier and three lines from her sheet when the room is built, and every fight's zone on the macro map names it.
Room B also has the tether-post vantage and, over its east end, a **Cantor**: a pale dove with a bell. Its bell rises (the telegraph), then tolls: a mask if you are under it, and Merrow's End is **erased**, the reeds going to paper and the tether-post leaving your atlas until you draw it again. A hit during the ring stops the bell; the forward Flourish reaches it.

Hub life (`docs/design/hub-life.md`): the world has a day of fifteen minutes' play (dawn, day, dusk, night; the paper warms at dusk and cools at night), paused while you talk or read a page; resting at a desk sleeps to the next dawn. Sable and Dotha keep posts by the hour and walk between them: Sable mends nets at the quay, reads the ledger at dusk and sleeps under the stilts at night; Dotha sings to the water by the tether-post at dusk. What they say depends on where they stand. An anchored place keeps the hour it was sealed at, and its people loop their posts on a fixed period, the same lines every time (`#still`). The atlas page prints the day and hour.

The macro map (`docs/design/world-map.md`) is also data: `WorldGraph` in Core lists every region's sub-zones, the ways between them and their gates (abilities, story flags, soft Wingbeat gaps), and `Reachable()` answers what a given kit and set of flags can reach; the edit-mode tests prove the spine from it. The greybox rooms are pinned to their zones.

The bounds-walk (`docs/design/bounds-walk.md`): how a place is **held** rather than sealed. Once you have heard the whale
(the Bone Bridge commission through Sable), Dotha offers to walk Merrow's End's bounds with you. She calls a bound half a
beat ahead and you must be standing on it when the beat lands (the posts in room B light up as they are called; the strip at
the top shows the name, the beat and your misses). Three misses restart the verse; two verses walked and Merrow's End is held.
The desk refuses to seal Hold on a place that has not been walked. Yarn: `<<walk id>>`, `walked("place")`, `walk_known()`.

The survey loop (`docs/design/survey.md`): **M** opens the atlas, the pause screen. The left page is the map: each place with its vantages drawn (●), blank (○) or erased (✕), its fade stage and fate, and the desks and lamps you have stood at. Standing at a desk or a lit lamp, the page lists where you can **travel** (↑↓ then J): any known desk or lamp whose place is drawn. The lamp under the Lamp-Keeper's perch lights when she is beaten. The right page is the journal.

The look (art-direction doc): `OWSBG/InkSprite` is the lit, alpha-clipped sprite shader with the two-step shadow ramp and the
`_Ink` state; two full-screen passes sit on the URP renderer (`Settings/Rendering/URP_Renderer.asset`): **ForegroundBlur**
(before post-processing; anything nearer than the gameplay plane blurs with distance, read from depth) and **PaperGrain**
(after post-processing; static grain, stronger on light paper than on ink). Far layers blur through the volume's Gaussian DoF.
Materials `Art/Materials/M_FS_*.mat` hold the knobs.

The regional decision (`docs/design/anchoring.md`): at the desk, once every vantage in the room is surveyed, the **Place**
row proposes anchor / hold / release and J seals it, once and for all. Anchoring locks the room's colour grade and wakes the
Guild's **Wardens** (long legs, a lance that lowers before it thrusts; the sighting lens parries it). Until Halvard has counted you at the lit lamp they only measure a journeyman and look away; from then on, unlicensed, you are hunted in every anchored town, and a Warden you strike hunts you regardless. Holding stops the fade
and nothing else; releasing lets it fade on. Yarn: `<<anchor place>>`, `<<hold place>>`, `<<release place>>`, `place_fate("id")`.

Fading (`docs/design/fade-stages.md`): each room has a `FadeGroup` over its paper layers and ground, driven by the place's
stage 0–4 in `WorldState` (`fade.<place>`). Stages advance only from story beats (`<<fade place stage>>`); anchored places hold.
In the greybox, telling Dotha to let Merrow's End fade thins room B: the reeds wash toward paper as you walk back through.

Rooms are Addressables (`Assets/AddressableAssetsData`, group **Rooms**, one bundle per room, address = scene name);
only the persistent scene is a built-in scene. `RoomManager` loads a room by address, keeps the bundles of its neighbours
(the targets of its transitions) resident, and releases the rest. Play mode reads from the AssetDatabase, so nothing has to be
built to press Play; a player build needs `-executeMethod OWSBG.Setup.ProjectSetup.BuildAddressables` first.

Cutscenes (`Assets/_Project/Data/Cutscenes/*.playable`, built by the setup script) are Timeline assets on a `Cutscene` object:
`ActorMoveClip` walks an actor, `PaperFadeClip` drives the screen fade, `DialogueNodeClip` starts a Yarn node and holds the
timeline until it ends; an optional Cinemachine shot takes over while it plays and Wren is frozen throughout.

The HUD, dialogue page, desk page, atlas spread and boss bar are one UI Toolkit document (`UI` object in the persistent
scene; `Assets/_Project/Code/UI`), built in code on the paper-and-ink palette until the UI art (ENV-11) lands.

Dialogue lives in `Assets/_Project/Dialogue/**/*.yarn`, compiled by `LastCartographer.yarnproject`. Custom commands: `<<flag key value>>`, `<<tutorial name>>`, `<<bind_prompt id>>`, `<<commission id post|take|fulfil|close|fail>>`, `<<cutscene id>>` (waits for it), `<<fade place stage>>`, `<<anchor|hold|release place>>`, `<<erase place>>`, `<<clock dawn|day|dusk|night>>`, `<<sleep>>`, `<<shop hub>>`, `<<walk id>>`; functions: `flag("key")`, `has_flag("key")`, `surveyed("id")`, `commission_state("id")`, `commission_is("id", "taken")`, `fade_stage("place")`, `place_fate("place")`, `erased("place")`, `drawn("place")`, `phase()`, `phase_in("place")`, `day()`, `seeds()`, `walked("place")`, `walk_known()`.

## Verifying headless
```
Unity.exe -batchmode -projectPath LastCartographer -runTests -testPlatform PlayMode -testResults logs/playmode-results.xml
```
The play-mode tests check the combat doc's frame data on the real controller, the enemies, Flourishes, Charters, Instruments, the boss loop, dialogue, commissions, cutscenes, the prologue end to end, room streaming, the survey loop and fast travel, hub schedules, the economy, the bounds-walk, and the UI. `UiScreenshotTests` also writes `logs/ui-hud.png`, `ui-dialogue.png`, `ui-desk.png`, `ui-ledger.png` and `ui-atlas.png` with the UI composited over the camera, for a headless visual check.

## Tooling
- **Unity 6 / URP Forward+**, Cinemachine 3, Input System, Addressables, Yarn Spinner 3.
- **Krita / Aseprite** for hand-drawn frames; **Unity 2D Animation** for boss rigs.
- **Blender 5.2** for light 3D props behind the play plane.
- **Git + LFS**.
