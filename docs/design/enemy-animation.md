# The Coast's Creatures (CHR-06, v1)

The six Saltmarrow enemies and the Lamp-Keeper, built and animated the way Wren is
(`docs/design/wren-animation.md`): parts on a rig of empties in Blender, clips as functions of time, the
paper kits' Freestyle ink, sheets packed at 96 px/unit. Model sheets are `docs/art/<name>-turnaround.png`.

## 1. The pipeline

```
tools/characters/saltmarrow_enemies.py     (Blender, headless; inklib.py holds what wren.py shares)
tools/characters/emberdown_enemies.py      (the highland's two, §2b)
python tools/characters/pack.py marshcrab reedskimmer smudge cantor warden lostremnant lampkeeper cavebat salamander
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.ProjectSetup.BuildBootstrapScene -quit
```

`MakeEnemy` and `MakeBoss` dress a creature with its sheets when `Art/Characters/<Type>/<type>.json` exists:
the quad becomes a cell-sized frame window centred on the collider, mirrored at rest because enemies start
facing left and every drawing faces right (`Enemy.Face` flips it). Without sheets the placeholder block stays.

## 2. Who asks for what

`EnemyAnimator` asks the enemy for its clip every frame (`Enemy.Clip`) and, for a move that follows its own
frame data, how far (`Enemy.ClipProgress`). The base rule is death, hurt, then moving or still; each family
adds its moves. A clip the sheets lack falls back to idle.

| Creature | Silhouette (art-direction 4) | Cell | Clips | What names them |
|---|---|---|---|---|
| Marsh crab | a round; pogo it | 1.6 | idle, move, hop, hurt, death | off the ground → hop |
| Reed skimmer | a round on the wing | 1.6 | idle, move, rise, dive, hurt, death | its state: Rise (drawn flaring yellow, the telegraph), Dive, Return → move |
| Smudge | a scribble | 2.0 | idle, move, hurt, death | velocity; the ink state is its own (`_Ink` follows drawn/undrawn) |
| Cantor | a teardrop with a bell | 2.4 | idle, move, ring, recover, hurt, death | Ring (sought by `RingProgress`: the bell rises through the six frames), Recover |
| Warden | a tall line: a heron with a lance and a brass gorget | 2.8 | idle, move, measure, telegraph, thrust, recover, hurt, death | Measure, Telegraph, Thrust (24 fps), Recover |
| Lost Remnant | a townsfolk oval with the ink gone | 2.0 | idle, move, hurt, death | velocity |
| The Lamp-Keeper | a Remnant gannet whose wings are the lamp's shutters, fused to the lamp | 3.6 | idle, telegraph, beam, dive, grounded, return, hurt, death | Perch → idle, Telegraph, Beam (shutters wide), Dive (24 fps), Grounded (open to hits), Return |

Locomotion and holds run at 12 fps; the thrust and the dives at 24, as Wren's strikes do.

## 2a. The Warden family (CHR-07)

`tools/characters/wardens.py` builds the rest of the family on the Warden rig above: the same heron, re-geared and
re-coloured, so every Warden reads as one silhouette (a tall line with a lance) and each is told apart by what
they carry. Packed as `Halvard`, `Brann`, `Oriel`, `Warden_B`, `Warden_C`; model sheets in `docs/art/`.

| Warden | Bird, gear | Clips | What names them |
|---|---|---|---|
| Warden-Sergeant Halvard | a heron; a sergeant's sash, a plume, the long sighting-lance with count notches | idle, move, walk, talk, measure, telegraph, thrust, lunge, survey, call, count, recover, hurt, death | `Halvard.Clip`: Approach → move; the telegraph is `survey` (the lance planted) for a survey and `call` (raised high) for a count, else `telegraph`; Thrust, Lunge, Count, Recover. `death` is his withdrawal: he straightens, lowers the lance and steps back. In the lighthouse he is an NPC: `NpcAnimator` asks for idle, talk and walk |
| Cinder Warden Brann | a crane in furnace-blackened brass, a red crown, twin lances (the far one rests behind the shoulder) | idle, move, telegraph, thrust, charge, crosscut, hold, recover, hurt, death | `Brann.Clip`: Charge, CrossCut (both lances sweeping), Hold (walking behind both, looping), the rest as Halvard |
| Warden-Captain Oriel | an egret, white, a captain's cloak, a short quill-lance with a nib | idle, move, telegraph, strike1–3, flourish, step, bind, recover, hurt, death | `Oriel.Clip`: the mirrored combo's steps in turn (`strike1..3`), Flourish (she turns through a Longstroke), Step, Bind (kneeling, the quill planted, looping until denied) |
| Warden B, Warden C | the patrols: B in a road-cloak with an iron gorget, C in a helm with a plume | the Warden's | the same `Warden` behaviour; `MakeHeldState` gives the anchored towns' patrols the three looks in turn |

Halvard's manifest carries `feetUnits` (−0.82: where his feet are from the cell's centre) because he is drawn on his
collider's centre like every enemy but must also stand on the lighthouse floor as an NPC; `MakeSpriteQuad` uses it
for the feet-at-origin placement, so one set of sheets serves the fight and the hunt. Brann and Oriel have no room
yet (Emberdown's Furnace Stair and Halden's Bastion are unbuilt): the sheets and the clip names wait for them.

## 2b. The highland's creatures (CMB-09, with ENV-03)

`tools/characters/emberdown_enemies.py`, on the same plumbing: the mine country's two (rooms doc §3, "pogo the
salamanders, strike the bats as they dive"), in the highland's palette (charcoal fur, basalt, ember). Both are
rounds. Placed by the room recipes (`Bat(x, y)` at its roost, `Salamander(x)` on its ledge) in every room the
plan lists them for: nine bats and nine salamanders across the stair, the bell stair, the chimneys, the flue
road and the baths; `EmberdownRoomsTests` holds each room to the plan's count.

| Creature | Silhouette | Cell | Clips | What names them |
|---|---|---|---|---|
| Cave-bat | a hung cloak that opens into a scalloped wing; ember eyes | 1.6 | idle, unfurl, swoop, move, hurt, death | `CaveBat.State`: Roost → idle (hanging, head down, wings folded); Unfurl (the telegraph, 0.4 s, sought by its progress; a Strike tell on its first frame); Swoop (24 fps: one arc from the roost through where Wren stood and up the far side, 0.8 s); Return → move (flapping back to the roost at 6 u/s). Health 2, any hit: fodder |
| Salamander | a low black length, embers down its back, a long tail | 1.6 | idle, move, flare, rush, cool, hurt, death | `Salamander.State`: Crawl (1.6 u/s along its ledge, turning at edges and walls) → idle/move; Flare (the telegraph, 0.35 s: the embers stand up, sought by its progress; a Strike tell); Rush (24 fps: 8 u/s along the ground for 0.45 s, stopping at an edge or a wall); Cool (0.8 s, the embers down, then 2 s before the next). Health 3; its back burns: only a down-strike lands (`AcceptsHit`), the answer is Pogo |

The bat's swoop is a quadratic arc: from the roost, through Wren's centre at its midpoint, to the mirror point
at the roost's height on her far side, so the pogo off it (the Wingbeat gap at the foot of the stair: "a pogo
off the bat crosses it") comes as it passes under. The bat in Chimneys_3 roosts over the ninth chimney's door,
more than eight units from the vantage, so it does not cancel the survey.

## 2c. The families and the late bosses (CHR-08, CHR-09, CHR-10)

`tools/characters/families.py` builds the rest of the Smudge and Cantor families on the two rigs above: four more
Smudges (the mine's with a lamp, the forest's with a swing, the plateau's in chalk, the smudge of Wren's own death
with her cowl inside it) and two more Cantors (a crow with a cracked bell, and the Choir's dove in wool). The
setup picks a look by region (`SmudgeLook`, `CantorLook`). `bosses.py` and `boss_parts.py` draw the nine late
bosses and the pieces their fights make. All of it is in `docs/design/boss-animation.md`.

## 2d. The roster's later families (CMB-09, second batch)

`tools/characters/roster_enemies.py`, on the same plumbing: three families for the answers the combat rules name
and no creature yet had (combat doc §7: "Blot the swarms, Longstroke the lined-up ones"; the Greyfold's rule that
the picture exists only inside her lantern-radius), one each for the Verdance, Halden and the Greyfold. Placed by
the room recipes (`Moths(x, y)`, `Wasp(x, y)`, `Sketch(x)`) where the room plans now list them: four clouds in the
root chapel and the Lantern Grove, three wasps over the mills, three Sketches on the Road That Stops and the white
shore. Numbers in `tuning.md` §4.

| Creature | Silhouette | Cell | Clips | What names them |
|---|---|---|---|---|
| Lantern-moth cloud | a ring of five small rounds, cream with an amber eye-spot on each wing | 1.8 | idle, move, flare, dart, gather, hurt, death | `Mothcloud.State`: Drift → idle/move (toward her lantern within 7 units, else home); Flare (the telegraph, 0.3 s: the wings thrown wide, the eye-spots shown, sought by its progress; a Strike tell); Dart (24 fps: 0.4 s at 7 u/s through where she stood); `gather` while a Blot's slow holds it (the cloud pulled tight). Health 3; the quill passes through a cloud (`AcceptsHit` only while gathered): the answer is Blot, then the strikes |
| Pulp-wasp | a round in paper-buff with ink-brown bands, hanging legs, the pulp sac under the jaw | 1.6 | idle, move, spit, hurt, death | `Pulpwasp.State`: Hover → idle/move (holds a stand-off of 4.5 units along the floor, backing from her and closing as she goes, 1.6 units over her); Spit (the telegraph, 0.4 s: the sac swells, sought by its progress; a Strike tell) then an `EnemyProjectile` pellet at 9 u/s toward her, every 2.4 s. Health 3; the quill's reach is short of the stand-off and the Longstroke's is not: a line of them is one stroke |
| Sketch | a townsfolk oval with the fill left out: a long neck carried low, legs too long, a crest of loose strokes; the line in grey ink | 2.0 | idle, move, fill, lunge, hurt, death | `Sketch.State`: Pace → idle/move (toward her, turning at edges and walls, with gravity); Fill (the telegraph, 0.3 s: the neck comes up, the bird the hand meant, sought by its progress; a Strike tell); Lunge (24 fps: 0.35 s at 9 u/s along the ground, stopping at an edge). Outside her lantern-radius (`ClarityMeter.Radius`, else 3.5) it is an outline: `_Ink` falls to 0.18, it cannot be struck and cannot hurt, and it does not lunge. Health 4; inside the radius, any hit |

The cloud's `gather` and the Sketch's outline are states the shader shows (`_Ink`), as the Smudge's flicker is;
the drawings are the same frames. The pellet is still the projectile's plain ink round (as Hale's flick is).

## 2e. Their regions' looks (CMB-09, third batch)

`tools/characters/roster_looks.py` builds each of the three in two more regions on the same rigs, as `families.py`
builds the Smudges: re-coloured, with a piece of the region on them. The setup picks the look by the room's region
(`MothLook`, `WaspLook`, `SketchLook`; the family's own sheets where no look is drawn), so the recipes still say
`Moths`, `Wasp` and `Sketch` and the plans still say moths, pulp-wasp and Sketch.

| Look | Region, rooms | What marks it |
|---|---|---|
| `Mothcloud_Ash` | the Greyfold and the Blank: the Half-Cathedral's nave | grey moths, the eye-spots gone white, ash on the wings; the line lighter |
| `Mothcloud_Dust` | Windreach: the long walk's stones, the riverbed's boats | tawny moths of the long grass, two carrying a grass seed |
| `Pulpwasp_Cinder` | Emberdown: the flue road | soot-black bands on ash, ember flecks, the sac an ember |
| `Pulpwasp_Gall` | the Verdance: the milestones, the ash field | green-brown, the oak gall it was born in on its back |
| `Sketch_Chalk` | the Blank: the capital's streets | chalk on white, the shade blue, the line nearly white |
| `Sketch_Thin` | Halden: Lowmarket's stair ("the paint is thinner here") | a paler fill under a thinner line |

Eleven families in twenty-five looks. `EnemySheetTests.TheRostersLooksWearTheirRegions` holds each look to its
family's cell and clips, its material to its own idle strip, and the rooms above to the look.

## 3. What changes when a creature wears sheets

- **Death:** the placeholder shrank to nothing in a quarter second. A drawing dies as its ink leaves: the
  fade sets `_Ink` from 1 to 0 over the death clip's length (`Enemy.DeathSeconds`, set by the animator), the
  clip holding its last frame under it.
- **Tint:** the placeholder's family colour stands down to white, so the sheet's own colours show; the
  grey of drained colour, the mark's pulse and the hit flash still apply (the flash goes bright rather than
  white, since white is the drawing's rest).
- **Stretches:** the Cantor's bell-rise and the Warden's lean were scale tricks on the block; with sheets
  they are frames, and the tricks stand down.
- **The Remnant's ink is greyer** (`GREY_INK`), and the Lamp-Keeper's too: half-drawn things.

## 4. Reworking by hand

As for Wren: replace a `<Type>_<clip>.png` strip at the same frame count and cell, or change the count in
the json, and rebuild; edit a creature's class or clip functions in `saltmarrow_enemies.py` (or `emberdown_enemies.py`) for a new pose;
a new creature is a `Rig` subclass, a clip list and a line in `CREATURES`, and the setup finds its sheets by
the enemy type's name.

## 5. Verification

- `EnemySheetTests` (edit mode): every creature's manifest at 96 px/unit with every clip its moves can name,
  strips at frame count × cell, drawn rates, death and hurt once, idle looping, a model sheet each; the
  families' sizes (tall Warden, wide Lamp-Keeper, small rounds); the rooms' materials rest on the idle strips
  and the Quay, Merrow's End and the Lighthouse carry the animator.
- `EnemyAnimatorTests` (play mode): a real marsh crab on a floor scuttles, hops when thrown, is hurt by a
  down-strike, and on the killing blow shows the death clip while its ink leaves over the clip's length with
  no shrink, then goes.

## 6. Open

- The Lost Remnant is placed by the Greyfold's and the Blank's rooms now (ENV-08); the Blank's islands still use tinted NPC blocks.
- Halvard's marks on the chapel floor are still dark blocks; his second kit (the thrown lance, the Seven Bridges)
  and third (the Threshold) have no frames yet.
- Brann's phase-3 glow (his telegraphs are the brass) is a tint the arena will need to drive on the sheet.
- The skimmer's rise is a second, flared render of the same bird; a tint on the sheet would do the same in
  one render once the shader takes a flare colour.
- The Lamp-Keeper's beams are still the arena's quads; her lamp glass could light them.
- The Warden's lance hitbox and the thrust frame's reach are tuned apart; the drawing should be checked
  against `Warden` reach in play.
- Death for a boss: the Lamp-Keeper's eight frames run before she is set inactive; the arena's own defeat
  staging (the beams hidden) still leads.
