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
| NAR-06 `[~]` | Boss character sheets: reason, arena, three phase lines, aftermath, for all 15; v1 in `docs/story/boss-sheets.md` and as data (`Bosses`, the twelve-word rule and the map tested; the Lamp-Keeper built from her sheet); kits are sketches for CMB-12..16 | M1 | SB-6 | NAR-01, CMB-09 |
| NAR-07 `[~]` | Emberdown arc: Kettil, Runa, Hollowvein, the Cinder Bath Debate, Brann; Commissions; v1 written (`Emberdown_*.yarn`, `docs/story/emberdown-arc.md`, Kettil's ledger of five), run through the Yarn project by tests; the rooms and the two walks are unbuilt | M2 | SB-4.2, 8.2 | NAR-05 |
| NAR-08 `[~]` | Verdance arc: Teodor, Aldermere, the Sunken Library, the Gatekeeper; Commissions; v1 written (`Verdance_*.yarn`, `docs/story/verdance-arc.md`, the Quiet House's ledger of five, Teodor's keystone check), run through the Yarn project by tests; the rooms are unbuilt | M2 | SB-4.3, 8.3 | NAR-05 |
| NAR-09 `[~]` | Halden arc: Orchard cache, Lowmarket strike, Interludes A and B, Oriel; Commissions; v1 written (`Halden_*.yarn`, `docs/story/halden-arc.md`, the Hall's ledger of five), with the voice tally (`Voices`, `<<voice>>`) that Pell's report weighs; the rooms are unbuilt | M2 | SB-4.4, 7.2 | NAR-04 |
| NAR-10 `[~]` | Windreach arc: the moving camp, Idrenne's Fire, Hale, the Fallen Star; v1 written (`Windreach_*.yarn`, `docs/story/windreach-arc.md`, the camp's ledger of five), with the region's decision as `Steppe` (on the Guild's map, walked, or neither) and Hale in the cast; the rooms, the leap set piece and both fights are unbuilt | M3 | SB-4.5, 8.5 | NAR-05 |
| NAR-11 `[~]` | The Threshold: Voss confrontation, all variants; v1 written (`Greyfold_*.yarn`, `docs/story/threshold.md`): the act break (Clarity, `act2.started`), the Edge Camp notice (`act2.threshold`), Marrow's echo, Halvard's third, Voss's one speech read from the report, the audience, the stones (`Keystones`) and the Steppe, Pell at the line; every story gate on the map before Act 3 is now opened by a script. The Return is NAR-12's | M3 | SB-7.2, 6.11 | NAR-09 |
| NAR-12 `[~]` | Act 3: Isolde's camp, Thessaly Hollow, Corra, Corvin, the Return; v1 written (`Greyfold_LastCamp_Isolde`, `Blank_Hollow`, `Blank_Capital`, `Greyfold_Return`, `Halden_Observatory_Pell`; `docs/story/act3.md`): reveals 5.2–5.6 placed, Corvin's stance (cooperate / persuaded / not) from a three-part argument, Corra carried out or not, Voss changed or not, Marrow's first three words, Pell's last list by dominant voice; Corra joins the cast | M3 | SB-7.3, 5.x | NAR-07..11 |
| NAR-13 `[~]` | Endings and epilogues (4 + Voss); Marrow's word; v1 written (`Halden_Observatory_Endings`, `Epilogue_Walk`, `Ending_Rest`; `docs/story/endings.md`): the frame offers only what is earned (`Endings`, bible 9 as flag logic), Runa's chorus, Teodor's naming, Voss's note or statue, the walk by ending, Marrow's last word ("Skywalk" / "Look." / none), Wren's last line by voice; the seventh keystone is Isolde's | M3 | SB-9 | NAR-12 |
| NAR-14 `[~]` | Blank island dialogue for every unanchored place; v1 written (`Blank_Islands`, `Blank_Aury`, `Saltmarrow_Chain_Sable`; `docs/story/blank-islands.md`): five authored islands, one per [B] commission, present by the outcome that sent them (`Islands`), and a generic Remnant island for any other released place; Aury in Act 2 (the keystone, plant 5.3) and Act 3 with Sable; Sable's tether. Every carried keystone and every story gate on the map is now written | M3 | SB-8.6 | NAR-07..10 |
| NAR-15 `[ ]` | Environmental storytelling pass: inscriptions, corpses, tapestries, the fledgling loops | M3 | SB-5.6 | ENV-06 |
| NAR-16 `[~]` | Foreshadowing audit (three plants per secret), `#still` tagging; v1 in `docs/story/foreshadowing.md` (`Foreshadowing`, `YarnAudit`): every secret revealed on one `#reveal` line and planted three times before it on both Act 1 roads, callbacks re-tagged, one plant found untagged; `#still` defined as "heard again, unchanged" and held both ways (+86, −2); the room plants wait for NAR-15 | M4 | SB-11 | NAR-13 |
| NAR-17 `[~]` | Item, Charter, Instrument, memory, and atlas flavour text; v1 in `docs/story/flavour-text.md` (`Flavour`): 84 lines in Wren's hand for every region and zone, Charter, Instrument (and Hale's lens), keystone, ability, memory and purse; on the desk, in the atlas margin as places are drawn, and in the journal's new Carried section; keystones named | M4 | CMB-5, 6 | DES-05 |
| NAR-18 `[~]` | Localization-ready pass; v1 in `docs/design/localisation.md`: every catalog string (places, vantages, waypoints, commissions, bosses and their lines, memories, gauntlets, pitches, walks, abilities, Instruments, Charters) keyed by id with its English as the fallback and listed by `DataText`/`WorldText` into `ui.en.csv` (486 keys); plurals by CLDR rules (`Loc.P`) and lists (`Loc.List`); the pseudo-locale audit opens every page and finds no English; fonts beyond Latin-1, RTL and translations themselves are not done | M4 | — | PRG-19 |

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
| DES-09 `[~]` | Emberdown and Verdance room designs; v1 in `docs/design/emberdown-verdance-rooms.md` and as data (`RoomPlans`, 40 rooms tested against the macro map, the boss sheets and the cast); recipes and Aldermere's after-state open | M2 | SB-4.2, 4.3 | DES-07 |
| DES-10 `[~]` | Halden room designs incl. flyer-towers; v1 in `docs/design/halden-rooms.md` and as data (`RoomPlans`, 21 rooms, tested with the climbs); the Vault and Voss's office moved under the flyer-tower so Act 2 can reach them; the Crown hall and Lowmarket's faded variant open | M2 | SB-4.4 | DES-07 |
| DES-11 `[~]` | Windreach, Greyfold, Blank room designs (drifting islands, lantern-radius platforms); v1 in `docs/design/windreach-greyfold-blank-rooms.md` and as data (`RoomPlans`, 35 rooms; 96 planned, the map complete with Saltmarrow's), tested through the story's order; Isolde's Last Camp moved across the Threshold and Aury's island joined to the Hollow only in Act 3, closing two back doors into Act 3 | M3 | SB-4.5–4.7 | DES-07 |
| DES-12 `[~]` | Ending requirement matrix as flag logic; every ending reachable; v1 in `docs/design/ending-matrix.md` and as data (`EndingRoutes`): one route per ending replayed through the Yarn project with every zone proven reachable on the map first (`EndingRoutesTests`), and what each decision closes (`EndingMatrixTests`); fights are flags until their kits exist | M3 | SB-9 | NAR-13 |
| DES-13 `[~]` | Bounds-walk rhythm spec and three authored walks; v1 in `docs/design/bounds-walk.md` (Merrow's End built, Kettil's Rest and Hollowvein designed) | M2 | CMB-10 | DES-03 |
| DES-14 `[~]` | Accessibility: remap, hold/toggle, hitstop and shake sliders, high-contrast ink, no timed dialogue; v1 in `docs/design/accessibility.md`: the options page (Esc / Start) pauses the world and holds the language, hitstop and shake at 0–100%, captions as written / ×2 / ×3 / until dismissed, high-contrast ink (UI palette at 7:1 and a paper-pass mode), hold or toggle for Bind, Survey and Glide, and a controls page (`Controls`: remaps kept in PlayerPrefs, a key in use swaps); dialogue never advances by itself; menu keys, the mouse and thread aim are not done | M4 | — | PRG-05 |

### 3.3 Combat (CMB)

| ID | Task | M | Ref | after |
|---|---|---|---|---|
| CMB-01 `[~]` | Wren controller: kinematic 2D, buffers, coyote, variable jump, apex hang; tuned in greybox | M0 | CMB-1, 2 | PRG-05 |
| CMB-02 `[~]` | Quill strike (3 directions), combo, down-strike pogo, hitstop, hit reactions | M0 | CMB-2.1 | CMB-01 |
| CMB-03 `[~]` | Inkwell + Bind | M0 | CMB-2.2, 4 | CMB-02 |
| CMB-04 `[~]` | Wingbeat, Talonhold, Inkthread, Windmemory implementations with ability gating; all four now in the controller (Inkthread to anchors and marked enemies on its own Thread button, the Charter's pip cost; the glide at 60% gravity, capped at 4 units/s; updrafts) with CMB-18 (`docs/design/gauntlets.md`); stick aim for the thread is open | M1–M2 | CMB-3 | CMB-01 |
| CMB-05 `[~]` | Flourishes: Crosshatch, Longstroke, Blot | M1 | CMB-4 | CMB-03 |
| CMB-06 `[~]` | Charters (3 base) with combo rewrites and silhouettes (greybox: combo, passives, default Flourish, tint; silhouettes await CHR art) | M2 | CMB-5 | CMB-05 |
| CMB-07 `[~]` | Instruments (7) and slot system (greybox effects for all 7; Compass-dart marks await Inkthread, tether-hook is a marker) | M2 | CMB-6 | CMB-03 |
| CMB-08 `[~]` | Enemy framework: state machine, telegraphs, "answer" tagging, families | M1 | CMB-7 | CMB-02 |
| CMB-09 `[~]` | Enemy roster: 12 for M1, 40 by M3 (greybox: Marsh Crab, Reed Skimmer, Smudge, Warden) | M1–M3 | CMB-7 | CMB-08 |
| CMB-10 `[~]` | Boss framework: phases, arena states, intro through the Timeline pipeline on first entry, retry loop under 8 s; outro cutscene pending | M1 | CMB-8 | CMB-08 |
| CMB-11 `[~]` | The Lamp-Keeper (6.1): greybox kit (beam, dive, double beam), three phases, rewards | M1 | SB-6.1 | CMB-10 |
| CMB-12 `[~]` | Halvard recurring (6.3 × 3 kits); the first kit built in the Salt Chapel (thrust, lunge, the survey's marks and the count, phase 3's floor; parry staggers him; he withdraws at zero); the second and third kits open | M2 | SB-6.3 | CMB-10 |
| CMB-13 `[~]` | The Collapse, Brann, the Gatekeeper, the Choir; v1: four greybox kits built from their sheets by `BossKits` (the Collapse struck only where and when a lamp is lit, rubble to pogo, a surge to Longstroke, lamps it reaches for; Brann's hot floor, both lances, a hold only a Longstroke breaks, the dark; the Choir's bells that erase platforms and the page, stopped by a strike, three then two in canon then one; the Gatekeeper's sweeps, feathers, rise, torn roots and belly), each in a runtime arena room (`ArenaRooms`), in `docs/design/boss-kits.md`; Inkthread, the walk's shared beat and the Choir's gate are open | M2–M3 | SB-6.4–6.7 | CMB-10 |
| CMB-14 `[~]` | Oriel, Hale, the Fallen Star; v1: three greybox kits in `BossKits` and runtime arena rooms (Oriel mirrors Wren's equipped Charter reversed, adds its Flourish, Binds once at a third, and stands the Wardens down if beaten without a mask lost, which now outranks Pell's report; Hale's duel over nine stones, phases by stones drawn, his lens halving the parry's cooldown; the Fallen Star struck only on the seam, its fist to pogo, iron walls and, burning, `Updraft`s that lift her with Windmemory), in `docs/design/boss-kits.md`; Hale's winning survey, magnetism as a pull, and Oriel's gate are open | M3 | SB-6.8–6.10 | CMB-10 |
| CMB-15 `[~]` | Voss (arena-anchoring phase); v1: a greybox kit in `BossKits` and a runtime arena room at the Threshold (lance, lunge and a shield that turns the quill from the front; from phase 2 the grade locks and he seals the section she stands in, holding her a beat, breakable at its edge from outside; in phase 3 the Blank eats the floor from the west down to his two-section island, and the white hurts), in `docs/design/boss-kits.md`; frozen platforms mid-air and Halvard's third kit are open | M3 | SB-6.11 | CMB-10 |
| CMB-16 `[~]` | Bells, Corra's Drawing, the Archivist, the Complete Survey; v1: four greybox kits in `BossKits` and runtime arena rooms (the Bells' rings shrink her lantern-radius and a rope cuts only when she can see it, by radius, the Field lantern or the vantage's steadying; Corra's Drawing struck only on drawn frames, the small Voss not to be struck, the outline phase; the Archivist's drawings real while his quill is on them, unmade by a strike on his hand, her drawing, the closing frame a Longstroke pushes back; the Complete Survey's named ground on the beat, ink pools that wound it, the Sky), in `docs/design/boss-kits.md`; the lantern-radius picture (PRG-18), the Remnant Charter and the walk's shared beat are open | M3 | SB-6.12–6.15 | CMB-10 |
| CMB-17 `[~]` | Late Charters (Ferryman, Unwriter, Remnant); v1: three `CharterProfile`s with combos, Flourishes and passives (the Ferryman's reel pulls and its thread costs 1; the Unwriter's strikes unwrite `EnemyProjectile`s, Hale's new flick among them, and Bind costs 4; the Remnant drains colour to grey and slow, and a grey Corra's Drawing cannot redraw), handed over by Sable with the cord, the Choir's arena and Ilse (`<<charter>>`), in `docs/design/late-charters.md`; silhouettes and the thread itself are open | M3 | CMB-5 | CMB-06 |
| CMB-18 `[~]` | Traversal gauntlets (one per region); v1: six gauntlets as data (`Gauntlets`), greybox recipes (`GauntletKits`) and runtime rooms (`GauntletRooms`): the lamp posts (Wingbeat), the furnace shafts (Talonhold), the canopy threads (Inkthread), the flyer-tower (both), the updrafts (Windmemory), the Road That Stops (lantern-radius cobbles, Clarity); a fall costs a mask and returns her to solid ground, never her last; with Inkthread and the glide finished in the controller (CMB-04), in `docs/design/gauntlets.md`; runners for the two climbs are open | M2–M3 | CMB-9 | CMB-04 |
| CMB-19 `[~]` | Tuning pass: damage tables, telegraph frames per tier, Inkwell economy; v1 in `docs/design/tuning.md` (`Tuning`: slams take 2 and shake, a floor and a typical read per tier, boss health from tier and access, enemy families, Longstroke 3; every kit declares its attacks and the audit holds them to the rules); the feel-test and playtest pass open | M4 | CMB-all | all |

### 3.4 Programming (PRG)

| ID | Task | M | Ref | after |
|---|---|---|---|---|
| PRG-01 `[x]` | Unity 6 project, URP Forward+, packages, asmdefs, folders | M0 | Engine | — |
| PRG-02 `[x]` | Persistent scene + two greybox rooms: side-on Cinemachine rig, parallax layers, sun, post volume, Wren rig, dummy | M0 | Engine | PRG-01 |
| PRG-03 `[~]` | `InkSprite` shader (HLSL, not Shader Graph): alpha clip, two-step ramp, `_Ink` state, paper grain; done for the greybox, needs the art pass (line weight, wash) once sprites exist | M0 | ART-3 | PRG-02 |
| PRG-04 `[~]` | Foreground-only depth-of-field render feature; paper-grain overlay feature (both as full-screen passes on the renderer; tuning against real art pending) | M0 | ART-3 | PRG-03 |
| PRG-05 `[x]` | Input: `.inputactions`, buffered input service, rebinding | M0 | CMB-1 | PRG-01 |
| PRG-06 `[~]` | Cinemachine 3 rig: follow, look-ahead, `Confiner2D` per room, boss cameras (fixed arena camera per BossArena; cutscene shots; per-region tuning pending) | M0 | Engine | CMB-01 |
| PRG-07 `[~]` | Room system: additive scenes, transitions, neighbour preload, Addressables (rooms are addressables in one bundle each; neighbour bundles kept resident; Build Settings fallback; content build entry point; CI in PRG-25) | M0 | Engine | PRG-01 |
| PRG-08 `[~]` | Dialogue runtime: Yarn Spinner, `<<flag>>`, `<<commission>>`, `<<fade>>`, `<<anchor>>`, dialogue UI | M0 | SB-11 | PRG-01 |
| PRG-09 `[~]` | `WorldState` + save/load (JSON, autosave at desks) | M0 | SB-10 | PRG-01 |
| PRG-10 `[~]` | Survey system: vantage points, atlas reveal, fast travel, erasure (Atlas store, atlas page as pause screen, Cantor bell erasure and recovery by re-survey, travel points at desks and lit lamps; page inking animation pending) | M1 | DES-02 | PRG-08 |
| PRG-11 `[~]` | Drafting desk: rest, respawn, Charter/Instrument swap (placeholder IMGUI desk menu), save | M1 | GDD 6 | PRG-09 |
| PRG-12 `[~]` | Commissions ledger runtime and journal (state machine in WorldState, tracker, ledger page, journal + toasts, Yarn command and functions, Saltmarrow greybox set; journal hosted on the atlas page) | M1 | DES-06 | PRG-09 |
| PRG-13 `[~]` | Anchor / hold / release runtime; held-state loops; Warden patrol spawner (Places store, HeldState grade lock + Wardens, desk place row, Yarn verbs; Wardens measure a journeyman and hunt an unlicensed cartographer via `Licence`; schedule loops and the locked hour in PRG-15; the bind requirement open) | M2 | DES-03 | PRG-10 |
| PRG-14 `[~]` | Fade-stage runtime: `_Ink` animation per place, layer dropout, story-beat advancement (FadeStages + FadeGroup per room, `<<fade>>`; the Remnant look and audio pending) | M2 | DES-04 | PRG-03 |
| PRG-15 `[~]` | NPC schedules and hub life (day clock with dawn/day/dusk/night, desk rest sleeps, per-phase posts and nodes, anchored hour lock and `#still` loops, hour tint; v1 in `docs/design/hub-life.md`; NavMesh / cross-room routes and shops open) | M2 | SB-5.1 | PRG-07 |
| PRG-16 `[~]` | Cutscene pipeline: Timeline + Cinemachine + Yarn hooks (Cutscene object, actor-move / paper-fade / dialogue-node clips, `<<cutscene>>` waits, shot camera; boss intros still coroutines) | M1 | — | PRG-06 |
| PRG-17 `[~]` | Death and smudge recovery (return to desk or wax seal; the drop of bound memories as a `MemorySmudge` rebuilt in its room, recovered by striking it down, folded forward on a second death; save v4; v1 in `docs/design/death-and-retry.md`; the smudge's look pending ENV-12) | M1 | GDD 6 | PRG-09 |
| PRG-18 `[~]` | Clarity meter and lantern-radius rendering in the Blank; v1: `Clarity` (levels 0–3 from the grant, the bells and Act 3: seconds untethered and radius), `ClarityMeter` on every Wren (drains in `UntetheredZone`s and the drift, empty draws her back to held ground for a mask, never the last; the gate without Clarity; the lost Remnant's touch takes more; HUD bar), `Lantern` (the radius narrows below half the meter, the Field lantern widens it, the Bells hold it; the Road's cobbles follow it) and a stage in the paper pass: white beyond the radius in the Greyfold and the Blank, outlines at the edge of the eye (`docs/design/clarity.md`); the drift room, the white patches and the lost Remnant's kit are unbuilt | M3 | SB-4.7 | PRG-03 |
| PRG-19 `[~]` | Localization wiring; v1: `Loc` (the locale, saved; UI and captions through `Loc.T/F` and `InkTheme.Say`, CSV tables per locale, English as the fallback), every Yarn line and option given a `#line:` id (1,087), the dialogue runner following the locale, the pseudo-locale `en-XA` for UI and dialogue, `LocalizationSetup.Refresh` to tag lines and rebuild the tables, tests that keep all of it in step (`docs/design/localisation.md`); data catalogs (places, Instruments, commissions, bosses), fonts beyond Latin-1, plurals and a language picker are open | M2 | — | PRG-08 |
| PRG-20 `[~]` | Blank island generator from `WorldState`; v1: `Islands.Drifting` orders the drift, `IslandBuilder` makes each island's room in a runtime scene when `RoomManager` asks for it (`RoomManager.Generator`), with its people on its node and exits along the chain (`docs/design/blank-generator.md`); the lantern-radius look and the drift room are not built | M3 | SB-8.6 | PRG-14 |
| PRG-21 `[~]` | Moving camp (Windreach) and day-advance travel; v1: `Camp` (three sites; it walks on at first light once its fire is had, one site a day, by sleep, play or the road past midnight; walking with it through the bedroll, `<<camp walk>>`, costs a day and makes camp at dusk at the next fire), `CampSite` (the camp or the ashes that say where it went), the walkers' post keeps the desk, stand-in rooms (`CampRooms`) until Windreach is built; `Travel`: fast travel takes an hour a way on the macro map and moves the day on (`docs/design/moving-camp.md`); the real rooms and the walk as a set piece are open | M3 | SB-4.5 | PRG-15 |
| PRG-22 `[~]` | Bounds-walk rhythm runtime (BoundsWalk set piece, roll-call strip, `<<walk>>`, hold on completion; the desk defers Hold to it; music pending) | M2 | DES-13 | PRG-08 |
| PRG-23 `[~]` | Endings runner and epilogue walk; v1: an ending's last scene says `<<epilogue>>` and `EndingsRunner` (persistent scene) plays Voss's coda, then walks the ending's stops through white (built room or an `EpilogueBuilder` stand-in), plays each scene, and ends on the title (`docs/design/endings-runner.md`); the walk is timed, not walked, and the stops' real rooms are unbuilt | M3 | SB-4.7, SB-9 | DES-12 |
| PRG-24 `[~]` | Performance: streaming budget, sprite batching, 60 fps lock; v1 in `docs/design/performance.md`: physics moved to the combat doc's 60 Hz (it was 50) and rendering locked at 60 (`FrameRate`), budgets in `PerfBudget` (p95 16.7 ms, transitions 100 ms, quiet frames under 64 B and no collection), and `PerfProbe` (`-perf`, `tools/perf.ps1`) walks a built player through six rooms: 1.1–1.2 ms a frame, 9–16 ms transitions, 21–51 batches on an RTX 3050 laptop; quiet-frame garbage cut from 333–538 B to 25–29 B (world keys made once, `Keys`); the GTX 1060 run and the painted rooms' budgets are open | M4 | — | all |
| PRG-25 `[~]` | Build pipeline: CI, Windows build, Steam packaging; v1 in `docs/design/build-pipeline.md`: `GameBuild` builds the Addressables content and a stamped 64-bit Windows player (`tools/build.ps1`; release 101 MB), a built player passes `-smoke` (two rooms through Addressables, the stamp, a conversation), `SteamDepot` writes SteamPipe scripts (`tools/steam-upload.ps1`), and `.github/workflows/ci.yml` (GameCI) tests, builds, smoke-runs and uploads tags to Steam; CI has not run (secrets to set), and there is no Steamworks SDK in the game | M4 | — | PRG-01 |
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
| AUD-09 `[~]` | Mixer, ducking, snapshots (dialogue, combat, boss, Blank); v1 in `docs/design/audio-mix.md`: six buses and six snapshots (paused, boss, dialogue, Blank, combat, explore) as gains and low-pass cutoffs with eased moves, four ducks (a line, an impact, a hurt, a Bind) with envelopes, the fade stage's filter on the ambience, and the player's four volumes on top (`Mix`, `Mixer`, `Options.Volume`); `MixDriver` wakes with the game with a source per bus and follows the state; a code mixer, no `AudioMixer` asset, and no clips yet | M4 | — | AUD-05 |

### 3.8 Production and QA (PRO)

| ID | Task | M | after |
|---|---|---|---|
| PRO-01 `[x]` | Repo, `.gitignore`, LFS attributes, folder layout | M0 | — |
| PRO-02 `[ ]` | Tracker with these IDs; weekly build cadence | M0 | PRO-01 |
| PRO-03 `[ ]` | Feel-test protocol for the controller (M0 gate) | M0 | CMB-01 |
| PRO-04 `[ ]` | External test round 1 (vertical slice) | M1 | M1 exit |
| PRO-05 `[ ]` | Full-playthrough matrix: each ending, each region order, sequence breaks | M3 | DES-12 |
| PRO-06 `[ ]` | Performance targets: 60 fps at 1080p on GTX 1060-class; room transition under 100 ms | M4 | PRG-24 |
| PRO-07 `[~]` | Bug bar and triage; v1 in `docs/design/bug-bar.md`: five severities defined by what a bug does to a player, a floor from the report's symptom and a priority from its reach (`BugBar`), a bar per milestone (beta 0/3, RC 0/0/10, release 0/0/0/25 listed), triage weekly then daily with a label for each state and every fix carrying its test; F12 in the game writes a report folder (report.md, the save, the last log lines, a screenshot: `BugReport`, `LogTail`, `BugReporter`) and a built player writes one itself on the first exception; the issue form (`.github/ISSUE_TEMPLATE/bug_report.yml`) and `tools/triage.ps1` (labels; the count against the bar) use the code's words and are held to it by test; the script hasn't run (no `gh`, nothing pushed) | M4 | — |
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
