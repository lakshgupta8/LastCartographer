# The Coast's Creatures (CHR-06, v1)

The six Saltmarrow enemies and the Lamp-Keeper, built and animated the way Wren is
(`docs/design/wren-animation.md`): parts on a rig of empties in Blender, clips as functions of time, the
paper kits' Freestyle ink, sheets packed at 96 px/unit. Model sheets are `docs/art/<name>-turnaround.png`.

## 1. The pipeline

```
tools/characters/saltmarrow_enemies.py     (Blender, headless; inklib.py holds what wren.py shares)
python tools/characters/pack.py marshcrab reedskimmer smudge cantor warden lostremnant lampkeeper
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
the json, and rebuild; edit a creature's class or clip functions in `saltmarrow_enemies.py` for a new pose;
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

- The Lost Remnant is drawn but the greybox places none yet; the Blank's islands still use tinted NPC blocks.
- The skimmer's rise is a second, flared render of the same bird; a tint on the sheet would do the same in
  one render once the shader takes a flare colour.
- The Lamp-Keeper's beams are still the arena's quads; her lamp glass could light them.
- The Warden's lance hitbox and the thrust frame's reach are tuned apart; the drawing should be checked
  against `Warden` reach in play.
- Death for a boss: the Lamp-Keeper's eight frames run before she is set inactive; the arena's own defeat
  staging (the beams hidden) still leads.
