# Development notes

The long-form technical README: repository layout, how to play the greybox, headless verification, build, and every content pipeline. The project front page is the [README](../README.md).

Original title: The Last Cartographer (code name: OWSBG).

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
| `docs/story/foreshadowing.md` | The foreshadowing audit: bible 5's secrets, each revealed on one `#reveal` line and planted three times before it on both Act 1 roads (`Foreshadowing`); `#still` as "heard again, unchanged", read from the scripts' shape (`YarnAudit`) and held both ways |
| `docs/story/environment.md` | The environmental storytelling pass (`Dressing`): the inscriptions, tapestries, remains and traces in every region's rooms, the readable ones as Yarn where the thing speaks and nothing changes, the plants they carry, and the fledgling loops and their glide rule |
| `docs/story/flavour-text.md` | Flavour text (`Flavour`): the atlas margins for every region and zone, the words on each Charter and Instrument, the keystones, abilities, memories and purses, in Wren's hand; where each shows (the desk, the atlas as it is drawn, the journal's Carried) and the rules the tests hold |
| `docs/design/game-design-overview.md` | Pillars, loop, systems, scope |
| `docs/design/combat-and-movement.md` | Wren's kit, Inkwell, Flourishes, Charters, Instruments, enemy and boss rules |
| `docs/design/art-direction.md` | Ink-on-paper look, 2.5D definition, palettes, the `_Ink` fade state |
| `docs/design/emberdown-verdance-rooms.md` | The two climbs room by room: 40 rooms, their exits and gates, generated from the same source as `RoomPlans` |
| `docs/design/playthrough-matrix.md` | The full-playthrough matrix (`Playthroughs`): each ending's route cut into legs and put in every order the map allows, plus the sequence breaks the soft gaps permit; twenty playthroughs replayed through the scripts (`RouteReplay`) |
| `docs/design/ending-matrix.md` | The endings as flag logic, what each decision closes, and one route to each ending from a new game (`EndingRoutes`), replayed through the scripts and proven on the map |
| `docs/design/boss-kits.md` | Thirteen bosses as greybox kits built from their sheets (`BossKits`): Reedmother's Brood (the nest in the iris fields, open while it calls its clutch; the fire in the last phase, and what she strikes deciding whether the field burns), the Collapse, Brann, the Choir, the Gatekeeper, Oriel (Wren's own Charter mirrored, one Bind, the stand-down), Hale (the nine-stone duel), the Fallen Star (iron, walls, the heat that lifts her over), Voss (lance and shield, seals that hold her and lock the grade, the Blank eating the arena to his island), the Half-Cathedral Bells (ropes cut only when she can see them), Corra's Drawing (drawn frames, the small one), the Archivist (drawings real while his quill is on them, the closing frame) and the Complete Survey (the named ground on the beat, the Sky); each fought to its answers in a runtime arena room (`ArenaRooms`) |
| `docs/design/gauntlets.md` | The six traversal gauntlets, one per region but the Blank, each around its ability; falls back to solid ground; the Road That Stops' lantern-radius cobbles; Inkthread (the Thread button) and the Windmemory glide |
| `docs/design/moving-camp.md` | Windreach's hub on the move (`Camp`): three fires at three sites, the camp walking on at first light, the bedroll that walks her with it for a day, ashes where it isn't; and travel that takes an hour a way on the macro map (`Travel`) |
| `docs/design/localisation.md` | The player's language: `Loc` for UI and captions, Yarn line ids and strings CSVs for dialogue, the catalogs keyed by id (`DataText`, `WorldText`), plurals and lists, the pseudo-locale and its audit of every page, `LocalizationSetup.Refresh`, and what is still open |
| `docs/design/offerings.md` | Memory as currency (`Offerings`): what asks for a memory, what it gives, a given memory gone for good, and the anchor it weakens |
| `docs/design/performance.md` | The 60 fps lock and 60 Hz physics (`FrameRate`), the budgets (`PerfBudget`), the `-perf` probe of a built player (`PerfProbe`, `tools/perf.ps1`), the first measurements, and the per-frame garbage it found and fixed; PRO-06's target: cards classed against a GTX 1060 (`PerfTarget`), the gate that reads testers' runs (`tools/perf-gate.ps1`), and the CPU-side run in CI |
| `docs/design/feel-test.md` | The controller feel-test (`FeelTest`), the M0 gate: the seven-station course (`FeelCourseRooms`, `-feel`), the twelve questions, what the game records while a tester runs (`FeelRecorder`) and the bar it is held to |
| `docs/design/tuning.md` | The tuning pass (`Tuning`): a hit takes one mask and a slam two, telegraph floors and each tier's typical read, boss health from tier and access, enemy families, the Charters' quill damage, the Flourishes' ink trades and the Inkwell's tempo; every kit declares its attacks (`Boss.Kit()`) and the audit holds them to the rules |
| `docs/design/tracker.md` | The plan as data (`Plan`) and the tracker made from it (`tools/tracker.ps1`: milestones, labels, one issue per row), and the weekly build on CI's schedule |
| `docs/design/bug-bar.md` | The bug bar (`BugBar`): severity by what a bug does to a player, the floor from its symptom, priority from reach, each milestone's bar and the triage states; F12's report folder (`BugReport`, `LogTail`, `BugReporter`), the issue form and `tools/triage.ps1` |
| `docs/design/build-pipeline.md` | The Windows build (`GameBuild`, `tools/build.ps1`), its version stamp (`BuildInfo`), the `-smoke` run of a built player (`SmokeTest`), CI on GitHub Actions with GameCI, and Steam's depot scripts and upload |
| `docs/design/audio-direction.md` | The audio brief (`AudioDirection`): each region's beat, mode, instruments and silence, Runa's roll-call as the one tune and its forms, a tell for every attack and the mix rules that keep them, fading as subtraction, delivery specs; the game's rhythms sit on the region beats |
| `docs/design/roll-call.md` | The roll-call sung (`RollCallSong`, `RollCallSinger`): a voice per bird, the tune's forms, every bounds-walk sung live, the whale, Runa's bell, the ending's chorus of whoever was met; `RollCallExport` renders the WAV and MIDI deliverables to `docs/audio/rollcall/` |
| `docs/design/wren-sounds.md` | Wren's sounds and the tells (`InkSounds`, `InkSoundBank`, `WrenSounds`): every cue made from ink and paper in code, the hit and kill layers, the survey's hatching, a tell per attack kind on every telegraph's first frame; `InkSoundsExport` renders `docs/audio/sfx/` |
| `docs/design/music.md` | The score (`Score`, `MusicDriver`): instruments and themes as notes in code, stems rendered on a worker and played in step under the Music bus, the five regions' themes with their rests and combat drives, the bar-line handover between regions, the Lamp-Keeper's arriving with her first telegraph and adding a stem a phase on the bar line, the resolution on the roll-call's answer; `ScoreExport` renders `docs/audio/music/` |
| `docs/design/ambience.md` | The ambience (`Ambience`, `AmbienceDriver`): every region's layers from recipes in code, loops of coprime lengths, played under the Ambience bus and taken away last first as the place fades; `AmbienceExport` renders `docs/audio/ambience/` |
| `docs/design/audio-mix.md` | The mix (`Mix`, `Mixer`): six buses, a snapshot per state (paused, boss, dialogue, Blank, combat, explore), ducks for a line, an impact, a hurt and a Bind, the fade stage's low-pass on the ambience, the player's four volumes; `MixDriver` runs it on a source per bus, ready for the clips |
| `docs/design/accessibility.md` | The options page (Esc / Start): remapping with swaps (`Controls`), hold or toggle for Bind, Survey and Glide, hitstop and shake sliders (`Shake`), high-contrast ink in the UI and the paper pass, captions that wait, and no dialogue that moves on by itself (`Options`) |
| `docs/design/clarity.md` | Clarity as a meter and a gate (`ClarityMeter`): how long she lasts untethered, growing with the story; empty, the white gives her back; the lantern-radius it draws, white paper beyond it in the Greyfold and the Blank |
| `docs/design/late-charters.md` | The Ferryman's, Unwriter's and Remnant Charters: combos, the reel, unwriting thrown things (`EnemyProjectile`), the dearer Bind, drained colour; handed over by Sable, the Choir and Ilse (`<<charter>>`) |
| `docs/design/endings-runner.md` | The epilogue walk at runtime (`EndingsRunner`, `<<epilogue>>`): Voss's coda, Halden, the ending's region, the Hollow, the title; stand-in rooms for unbuilt stops |
| `docs/design/blank-generator.md` | The Blank's islands built at runtime from `WorldState` (`IslandBuilder`, `RoomManager.Generator`): the drift's order, the room recipe, the tests |
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
scene plus the prologue room and enters Play mode. **OWSBG → Play From Saltmarrow** skips the prologue and starts in room A. **OWSBG → Play the Feel Course** goes straight to the controller feel-test's course (`docs/design/feel-test.md`); a build does the same with `-feel`. Then **click inside the Game view once** so it has
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

**Doors read the map.** Every transition carries its way's gate (`docs/design/gates.md`): a story flag or an ability
the way needs. A shut way has a pale bar in it that says why (`Not without Talonhold.`, `The way is shut. Not yet.`)
and goes the moment the flag is set or the ability learned; a Wingbeat gap never bars, since skill may cross it.
A fall past a room's bottom costs a mask and puts you back on the last ground, not at the desk.

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
| Options (language, remapping, comfort) | Esc | Start |

Every action but Move can be remapped on the options page's Controls page; a key already in use swaps places. The
page also holds the language, hitstop and shake sliders, how long captions stay, high-contrast ink, and whether Bind,
Survey and Glide are held or toggled (`docs/design/accessibility.md`).

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
In the Greyfold and the Blank the grain pass first whitens everything beyond Wren's lantern-radius, keeping outlines only
toward the frame's edge; the radius is her Clarity meter's (`docs/design/clarity.md`).
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
built to press Play; a player build builds them first (`GameBuild`, below).

Cutscenes (`Assets/_Project/Data/Cutscenes/*.playable`, built by the setup script) are Timeline assets on a `Cutscene` object:
`ActorMoveClip` walks an actor, `PaperFadeClip` drives the screen fade, `DialogueNodeClip` starts a Yarn node and holds the
timeline until it ends; an optional Cinemachine shot takes over while it plays and Wren is frozen throughout.

The HUD, dialogue page, desk page, atlas spread and boss bar are one UI Toolkit document (`UI` object in the persistent
scene; `Assets/_Project/Code/UI`), built in code on the paper-and-ink palette until the UI art (ENV-11) lands.

Dialogue lives in `Assets/_Project/Dialogue/**/*.yarn`, compiled by `LastCartographer.yarnproject`. Custom commands: `<<flag key value>>`, `<<tutorial name>>`, `<<bind_prompt id>>` (the prologue's lesson), `<<bind id>>` (a memory someone gives her, captioned), `<<commission id post|take|fulfil|close|fail>>`, `<<cutscene id>>` (waits for it), `<<fade place stage>>`, `<<anchor|hold|release place>>`, `<<erase place>>`, `<<clock dawn|day|dusk|night>>`, `<<sleep>>`, `<<shop hub>>`, `<<walk id>>`, `<<camp walk>>`; functions: `camp_ready()`, `camp_site()`, `flag("key")`, `has_flag("key")`, `surveyed("id")`, `commission_state("id")`, `commission_is("id", "taken")`, `fade_stage("place")`, `place_fate("place")`, `erased("place")`, `drawn("place")`, `phase()`, `phase_in("place")`, `day()`, `seeds()`, `walked("place")`, `walk_known()`. Every line and option carries a `#line:` id for its translations; after writing dialogue or UI text run `-executeMethod OWSBG.Setup.LocalizationSetup.Refresh` (`docs/design/localisation.md`).

## Verifying headless
```
Unity.exe -batchmode -projectPath LastCartographer -runTests -testPlatform PlayMode -testResults logs/playmode-results.xml
```
The play-mode tests check the combat doc's frame data on the real controller, the enemies, Flourishes, Charters, Instruments, the boss loop, dialogue, commissions, cutscenes, the prologue end to end, room streaming, the survey loop and fast travel, hub schedules, the economy, the bounds-walk, and the UI. `UiScreenshotTests` also writes `logs/ui-hud.png`, `ui-dialogue.png`, `ui-desk.png`, `ui-ledger.png` and `ui-atlas.png` with the UI composited over the camera, for a headless visual check; `LanternRenderTests` writes `logs/lantern-*.png`, the lantern-radius through the real renderer.

## Building
```
pwsh tools/build.ps1                  # Windows release build to LastCartographer/Builds/Windows, then a smoke run
pwsh tools/build.ps1 -Development     # a development build
```
`OWSBG.Build.GameBuild` builds the Addressables content and the 64-bit Windows player, stamped with its version (from the
git tag) and commit, shown at the foot of the options page. `-smoke` makes a build boot, walk two rooms and start a
conversation, then quit with 0 or an error code (`tools/smoke.ps1`); `-perf` walks six rooms and measures frames,
transitions and garbage against the budgets (`tools/perf.ps1`, `docs/design/performance.md`). Testers on the target card
(a GTX 1060) run `pwsh tools/perf.ps1 -Out logs/perf/<machine>.json`, and `pwsh tools/perf-gate.ps1` says whether PRO-06 is
met. CI (`.github/workflows/ci.yml`, GameCI) runs both test
suites, the Windows build, its smoke run and a CPU-side probe (transitions and garbage; the runners have no GPU) on every push, and uploads to Steam from a `v*` tag; the secrets it needs,
and Steam's depot scripts (`tools/steam-upload.ps1`), are in `docs/design/build-pipeline.md`.

## Reporting a bug
**F12** in the game writes a folder under `%USERPROFILE%\AppData\LocalLow\<company>\LastCartographer\BugReports\`: `report.md` (the build, the room, the first exception, the last log lines), the save, `log.txt` and a screenshot. A built player writes one by itself after its first exception. Attach it to a **Bug report** issue; the form's severities and areas are the bar's (`docs/design/bug-bar.md`). `pwsh tools/triage.ps1 -Labels` makes the tracker's labels and `pwsh tools/triage.ps1 -Milestone rc` counts the open bugs against the bar.

## What can be read
**OWSBG → Place the Coast's Readables** (`PlacementSetup`, run by every build) puts the environmental pieces and the memory askers into their rooms: a trigger with the piece's drawing from the region's kit under it (`docs/design/environment-props.md`), or an ochre block where a bird asks. Stand at one and press up. A piece that changes with its place (the cups after the walk, Merrow's chalk, Lowmarket's COMPLETE, a door that asks) carries two drawings and `DressingProp` shows the world's; `OWSBG_SHOT_WORLD="flag=1;fate:Place=Anchored"` captures the changed one. Re-running it replaces only what it placed (`docs/story/environment.md`, `docs/design/offerings.md`).

## The paper kit
A region's backdrop and ground are drawn by `tools/paperkit/<region>.py` (`saltmarrow.py`, `emberdown.py`, `verdance.py`, `halden.py`, `windreach.py`, `greyfold.py`, `blank.py`, on the shared `kitlib.py`) in headless Blender: cut-out geometry in the region's palette with Freestyle ink lines, written as PNG strips and tiles to `Assets/_Project/Art/Environment/<Region>/` with a `kit.json`. The greybox builder puts any layer the kit has on the ink shader (`Paper_*` strips on the parallax quads, `Ground_*` tiles as world-tiled skins on the ground blocks), so the place's fade thins the drawing. Render, then rebuild: `blender -b -P tools/paperkit/saltmarrow.py` and **OWSBG → Build Bootstrap Scene** (`docs/design/paper-kit.md`).
`tools/paperkit/props.py` draws the hubs' furniture the same way (the desk, the ledger, the dummy, Sable's stall, the survey stakes, the lamps, seeds, bound stakes, nets, Dotha's stoop, boats and tether-posts); the builder stands each under the behaviour it dresses and keeps the greybox block where a region has no drawing (paper-kit.md §2a).

## The light
Each region is lit by its own row in `RegionLight` (Core): an hour that never moves, as a sun, an ambient, the paper the camera clears to and the post it grades through (`docs/design/lighting.md`). `RegionLighting` in the persistent scene blends them as Wren crosses from one region to the next, and the day (`DayCycle`) dims the ones that have an hour. The build writes the seven `PP_<Region>.asset` profiles from the table and stands the real lights: travel lamps that come on when lit, the hubs' lamps, hearths and fires, the furnace strip, the springs, the Lantern and Aury's lamp room; her lantern is a light in the white, and Brann, the Star and the Collapse light their own fights. The ink shader takes every lamp as a two-step pool on the paper. `OWSBG_SHOT_HOUR=night` captures a room at night.

## Wren's sheets
`tools/characters/wren.py` builds Wren from parts in headless Blender and renders every clip side-on in the paper kit's ink; `python tools/characters/pack.py wren` packs the frames into one strip per clip under `Assets/_Project/Art/Characters/Wren/` and writes the model sheet to `docs/art/`. The bootstrap build puts `InkSheetPlayer` and `WrenAnimator` on her (`docs/design/wren-animation.md`). Her abilities and flourishes have their own frames: the Wingbeat, the Talonhold's hold, slide and push-off, the thread's cast, pull and catch, the glide and the rise up an updraft, and crosshatch, longstroke and blot following the flourish's own frames. Each Charter changes her silhouette: `wren.py -- charters` draws every clip again in the Warden's, the Drifter's, the Ferryman's, the Unwriter's and the Remnant's cowl and grip (`Art/Characters/Wren_<Charter>/`), the sheet player wears the set for the Charter she has on, and `tools/characters/charters_sheet.py` lays the six side by side in `docs/art/wren-charters.png`.
`tools/characters/saltmarrow_enemies.py` does the same for the coast's six enemies and the Lamp-Keeper (`docs/design/enemy-animation.md`), and `emberdown_enemies.py` for the highland's cave-bat and salamander (§2b), `roster_enemies.py` for the roster's later batches (the Verdance's lantern-moth cloud, Halden's pulp-wasp, the Greyfold's Sketch, §2d; the steppe's tussock and the Reedmother's reedlings, §2f) and `roster_looks.py` for their looks in two more regions each (§2e); `tools/characters/wardens.py` builds the rest of the Warden family on that rig (Halvard, Brann, Oriel, two more patrol looks; enemy-animation.md §2a); `bosses.py` draws the nine late bosses, `boss_parts.py` the pieces their fights make (doves, rubble, feathers, stones, seals, ropes, the fist, the quill hand, the pools) and `families.py` the rest of the Smudge and Cantor families (`docs/design/boss-animation.md`); `pack.py all` packs every rendered character.

## The ink
`tools/characters/fx.py` draws the effects as one-shot clips (a splash, the slash, the three Flourishes' scribbles, the Bind's redraw, the eraser, a crumble, Halvard's marks and their eruption) and `pack.py fx` packs them; the persistent scene's `InkFx` spawns them wherever the game hits, swings, binds, erases or breaks (`docs/design/ink-fx.md`).

## The roll-call
The game's one tune is sung by a small in-engine synth: `RollCallSong` (Core) turns the direction's notes into a bird's voice, and `RollCallSinger` sings every bounds-walk live (the call on each beat, a falter on a miss, the answer at a verse's end), the whale under the Bone Bridge when the Reedmother is drawn, Runa at the Bell, Dotha to the water and the ending's chorus of everyone met (`<<sing use>>` in a script). `Unity.exe -batchmode -executeMethod OWSBG.Setup.RollCallExport.Render` (or **OWSBG → Render the Roll-Call**) writes every use as WAV and MIDI to `docs/audio/rollcall/` for the composer to replace (`docs/design/roll-call.md`).

## The score
`Score` (Core) writes the music as data: each region's band as timbres and its theme (Saltmarrow, Emberdown, the Verdance, Halden, Windreach) and the Lamp-Keeper's as stems of scale degrees looping on whole bars at the region's beat, with the direction's silence share as bars of rest. `MusicDriver` wakes with the game, renders a theme's stems on a worker thread the first time a room asks for it, and plays them in step under the Music bus; a region's drive comes in with combat, a walk into another region hands over on the bar line and crosses over, and a boss's theme arrives with its first telegraph, adds or changes a stem a phase on the bar line and resolves on the roll-call's answer in its key: the Lamp-Keeper's, Halvard's in the three keys he is fought in, Brann's, Voss's and the Archivist's on the Guild's shared motif or the roll-call inverted, and every other boss fighting to its region's own motif. The rhythm bosses keep time with it: the Collapse's theme and the Complete Survey's three (an ink each, in its region's key and beat) have a pulse on every beat and the chorus calling a name on it, and the driver holds them on the fight's own clock, so the music is a timing aid; the Reedmother's Brood chants "Ours. Ours. Ours.". The Blank's theme remembers only what she drew: a region with nothing on her page is a rest where its tune would be (AUD-18). With AUD-14 every main boss has a theme of its own: the Gatekeeper's voice tries to fly, Oriel's brass plays the Guild's motif backwards, the Bells' score strikes no bell (every ring is the fight's tell), and Corra's Drawing draws her father twice his size. The Blank remembers every region's tune backwards with the Remnant singing the roll-call reversed, and each ending has a coda the epilogue plays out over the title. `Unity.exe -batchmode -executeMethod OWSBG.Setup.ScoreExport.Render` (or **OWSBG → Render the Score**) writes stems, mixes and MIDI to `docs/audio/music/` (`docs/design/music.md`).

## The rooms' beds
`Ambience` (Core) makes every layer the audio direction names for every region (the tide on the pilings, the reeds, rain on the boardwalk, gulls, a bell buoy; the furnaces; the canopy wind; Halden's clockwork on its beat; footsteps too close in the Greyfold; drift in the Blank) from a recipe in code, each its own loop. `AmbienceDriver` plays the room's region's layers under the Ambience bus and takes them away, last first, as the place's fade stage climbs; the mix dulls what is left. `Unity.exe -batchmode -executeMethod OWSBG.Setup.AmbienceExport.Render` (or **OWSBG → Render the Ambience**) writes the 28 loops to `docs/audio/ambience/` (`docs/design/ambience.md`).

## Her sounds
Wren's quill is ink and paper: `InkSounds` (Core) makes every cue from a few lines of noise, filters and envelopes (a stroke, the hit and kill layers, the nib's tap, a page turned fast, the thread, a word being written for the Bind, the survey's hatching, a smudge for hurt) and the four telegraph tells; `InkSoundBank` plays them at the Sfx bus's gain and `WrenSounds` on Wren hooks her events. `Unity.exe -batchmode -executeMethod OWSBG.Setup.InkSoundsExport.Render` (or **OWSBG → Render Wren's Sounds**) writes them to `docs/audio/sfx/` for the sound designer to replace (`docs/design/wren-sounds.md`).

## Their voices
Every enemy is a drawing and sounds like what it is drawn as (`docs/design/enemy-sounds.md`): `EnemySounds` (Core) gives each family a material (a shell's tick, wet ink, the Cantor's handbell in D, a Warden's brass, a Remnant's dry paper, embers, moths, pulp, graphite, earth, fluff) for its hurt, death and turned-away strike, and cues on the clips the animator asks for (the crab's hop and scuttle, the skimmer's rise and dive, the wasp's hum and spit, the tussock's rumble and heave, a reedling's peep); `EnemyVoice` goes on every enemy as it wakes and plays them from where it stands, panned and quieter past the screen. Since AUD-15 the bosses have their own: each is heard doing what it does and never as it reads (the tell is the read's sound), and what is no clip of its body (a bell tolled, a rope cut, the furnace floor shifting) is heard from a count the boss already keeps. Its loose pieces too (AUD-16): every part a boss makes is heard as it appears, is struck, goes, or while it is there (a feather shattering on the floor, rubble crashing where it lands, the surge rushing, the Brood's fire burning). The crab, the skimmer, a smudge, the Cantors, the Choir, the Bells and the Survey now tell like every other attack, and `InkSoundBank` keeps to the direction's 24 voices in its order, dropping from the bottom. The same render command as Wren's writes `docs/audio/sfx/enemy_sfx_*.wav`.

## The world's sounds
The rest of the game sounds too (`docs/design/world-sounds.md`): `WorldSounds` (Core) adds the pages (paper lifted and set down, the nib's tick for the cursor, a tick pitched by a slider's level, a stroke for a choice and a dry dot for a refusal, a quick scratch for each line), which ride the Ui bus so the options page is heard while paused; the world's events through `WorldSoundHooks` (a room's page turned, a lamp lit while she is there, the desk's long breath, a seed's drop pitched by its worth, the ledger's tick and stamp, the eraser for a fade or an erasure, a stake for an anchoring, a knock at a shut way); and hers through `WrenSounds` (the Instruments, a parry's ring, an ability's flourish). The same render command writes `docs/audio/sfx/world_sfx_*.wav` and `ui_sfx_*.wav`.

## Her footsteps
Wren's feet sound on whatever the ground is drawn as (`docs/design/footsteps.md`): `Footsteps` reads the paper-kit tile the block under her wears (boards knock, stone clicks, iron clanks, ash crunches, moss is nearly nothing, the shallows splash; the region's ground where a block wears no tile), steps every stride of the run clip with three takes turning, and lands on the ground's own landing, louder after a long fall. On a wall her talons catch, scrape as the hold gives and scuff as she pushes off; in a fading place her feet thin a stage at a time, and from stage 3 she steps on the page whatever the ground is drawn as (AUD-17). `docs/audio/sfx/wren_sfx_step_*.wav` and `wren_sfx_landing_*.wav` come from the same render command.

## The cast
`tools/characters/cast.py` draws the returning cast (Sable, Dotha, Isolde, Pell, Runa, Kettil, Teodor, Idrenne, Maren, Corvin, Ilse, Corra, Marrow, Aury) from one parametric townsfolk bird: idle, talk, walk and asleep for everyone, plus each one's own (Sable mends and reads, Dotha sings, Corra draws). The bootstrap build dresses any NPC whose name has sheets (`Sable_Greybox` → `Sable`) with `InkSheetPlayer`, `NpcAnimator` (the post's activity, the talk, the walk) and `NpcInk`, whose colour state follows the place: drawn, washing toward paper as the place fades, the ink removed once it is let go or on an island in the Blank (`docs/design/npc-animation.md`).

## The townsfolk
`tools/characters/townsfolk.py` draws a library of thirty generic birds, six for each living region, on the same bird
as the cast, with the hub's four clips and the crowd's two (`watching`, `cheering`): a crowd, a watcher, a picket line,
a minor named bird or a bird who asks wears one of them instead of standing as a block, and the Remnant grey is
`NpcInk`'s state, not a drawing. The recipes' `.Folk(look, x, activity, face)` stands them (`docs/design/townsfolk.md`).

## Portraits
Every bird who speaks has a face beside their lines (`docs/design/portraits.md`): `tools/characters/portraits.py`
frames the body they are met in, head and shoulders, at rest and mid-word, and `portraits_pack.py` adds the Remnant's
grey. The dialogue page moves the beak while a line is new and greys a speaker as far as the bird in the room has
faded. Things that speak, the Lantern and Marrow speak without one (`Portraits.Faceless`).

## The fledglings
In one room of every region young birds leap from a perch in the background (`docs/design/fledglings.md`): six in turn,
and for each ability Wren has one more of them glides, a little further each. Anchored places never glide, a fade thins
them, and in the Open World one doesn't come down. Drawn by `tools/characters/fledglings.py` (seven species), stood by
the recipes' `.Fledglings(...)`, leapt by `FledglingLoop`.

## The UI's drawings
The atlas UI is drawn by the same pen as the world (`docs/design/ui-art.md`): `tools/ui/ui_art.py` renders the paper the
pages are made of (the page, the strip, the atlas's spread, the portrait's frame), the masks as feathers, the Inkwell as a
bottle that fills, the boss bar as a brush stroke and the glyphs the pages point with (the nib, the rose, the vantage
marks, the Charters' cowls, the Instruments), `tools/ui/pack.py` packs them under `Art/UI/Resources/UI/` with `ui.json`,
and `InkArt` loads them through Resources with no setup pass. Titles are IM Fell English and body text Alegreya Sans
(both OFL, beside their licences). Under high-contrast ink the paper goes back to the flat opaque page and the glyphs stay.

## The store's assets
`docs/marketing/` holds the key art, the nine Steam capsules, thirteen screenshots and four trailer clips, all made from the
game: `MarketingCaptureTests` (play mode, with `-captureMarketing`) captures the shots and clips, `tools/marketing/` renders Wren
large, composes the capsules and encodes the clips (`docs/design/marketing-assets.md`). Nothing there is published.

## The first test round
The slice's external round is ready to run (`docs/design/playtest-round1.md`): `LastCartographer.exe -playtest -tester <name>`
records a session as anyone plays (rooms, deaths, the Lamp-Keeper, how long they were lost), `-continue` picks up from the
last desk after a crash, and `pwsh tools/playtest-gate.ps1` reads the answers and sessions in `logs/playtest/` against the
round's bar. The tester brief and the facilitator's sheet are in `docs/playtest/round1/`. No round has been run yet.

## The store page and press kit
`docs/marketing/store-page.md` and `docs/marketing/press-kit/index.html` are written from `StorePage` (Core) by **OWSBG →
Marketing → Write the Store Page and Press Kit**; `tools/marketing/cut_trailer.py` cuts a rough trailer from the clips and
stills under Saltmarrow's theme (`docs/design/store-page.md`). Everything is a local draft: the name, date, price and contact
say TBA.

## The feel-test
**OWSBG → Play the Feel Course** or `LastCartographer.exe -feel -tester <name>` runs the controller's seven-station course and writes the session as JSON on quit. Put the testers' answers in `logs/feel/answers.csv` and run `pwsh tools/feel-gate.ps1` for the M0 gate's verdict (`docs/design/feel-test.md`).

## Tooling
- **Unity 6 / URP Forward+**, Cinemachine 3, Input System, Addressables, Yarn Spinner 3.
- **Krita / Aseprite** for hand-drawn frames; **Unity 2D Animation** for boss rigs.
- **Blender 5.2** for the paper kits (`tools/paperkit/`) and light 3D props behind the play plane.
- **Git + LFS**.
