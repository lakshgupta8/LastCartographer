# Ink Effects (ENV-12, v1)

Ink is the VFX language (art-direction 7): strikes leave splashes that soak into the paper, Flourishes are pen
scribbles, the Bind redraws Wren's outline, erasure rubs the image out with a visible eraser, the Blank's edge is
wet paper. Version one draws each as a one-shot sheet clip in Blender and spawns it from one place.

## 1. The pipeline

```
tools/characters/fx.py            (Blender, headless; each frame built fresh from geometry, the kits' ink line)
python tools/characters/pack.py fx
Unity.exe ... -executeMethod OWSBG.Setup.ProjectSetup.BuildBootstrapScene -quit
```

`BuildPersistent` puts `InkFx` in the persistent scene with every clip from `Art/Characters/Fx/fx.json` on one
flat ink material (`M_Fx`: unlit, unshadowed, no grain). Anything in the world calls `InkFx.Spawn(clip, point,
angle, scale, follow)`; the clip plays on a pooled quad at z −0.3, just in front of the play plane, turned to the
angle, and the quad goes back to the pool when the clip ends. `InkFx.Mark` is a looping clip the caller owns.
A clip that is not loaded returns null and the old placeholder geometry stands in, so scenes and tests without
the persistent scene behave as before.

## 2. The clips

Every clip renders into one 3-unit cell at 24 fps (ink moves fast, dries slow: the last frames hold the soak) and
faces +X where it has a direction.

| Clip | Frames | What | Who spawns it |
|---|---|---|---|
| `splash` | 6 | drops fly forward and out, then soak in where they land | `StrikeVisual` on a landed hit and for every `Burst` (parry, lantern); `WrenFx` when she is hurt |
| `slash` | 4 | a calligraphic arc drawing itself along the swing | `StrikeVisual` on every swing |
| `crosshatch` | 6 | pen ticks laid one way then the other, spray at the end | `Flourishes`, once on the first tick of the Crosshatch |
| `longstroke` | 5 | one long line out from the origin, dry-brushed at its end | `Flourishes`, the Longstroke |
| `blot` | 6 | a round spreading, drips running, spatter, soaking pale | `Flourishes`, the Blot |
| `redraw` | 8 | a pen line traced round Wren's outline, flaring, settling | `WrenFx` on `WrenVitals.Bound`, following her |
| `eraser` | 6 | an eraser dragged across, shavings curling, paper behind | `Cantor.Toll`: three sweeps out from the bell (the room's ink drop is the fade group's) |
| `crumble` | 6 | plank pieces dropping and turning, ink dust settling | `WeakFloor` on breaking, sized to the floor |
| `mark` | 2, loops | a scribbled square, the ink wet | `Halvard.AddMark`, one per surveyed square, his to destroy |
| `erupt` | 6 | a column of ink rising from a mark, falling as drops | `Halvard` when the count erupts, one per mark |

## 3. What else changed

- **The weak floor** stands on `Ground_Boardwalk_Weak` (rotten planks: cracks, a missing board, a sagging beam)
  and **the hidden platform** on `Ground_Boardwalk_Hidden` (a dotted outline, the boards barely washed in) when
  the kit has them (`paper-kit.md` §2). Both keep their colours: the flash goes bright and the lantern's reveal is
  the ink alone. A drawn weak floor vanishes as the crumble plays instead of shrinking.
- **Halvard's marks** are scribbled squares; the block-stretch of the eruption stands down when drawn.
- **The Blank's edge** is `Prop_WetEdge`, the Greyfold kit's first layer (`props.py` writes it to `Art/Environment/Greyfold/`, since the Edge room reads that region's kit), a 4 × 12 cut-out stretched to the room's height over the first white
  sheet's start in the Edge room: a fibrous damp band, the white bleeding in from the right, no ink line. It does
  not fade with the place.

## 4. Reworking by hand

Replace a `Fx_<clip>.png` strip at the same frame count in a 288-px cell, or change the count in `fx.json`, and
rebuild the persistent scene. A new effect is a build function and a line in `CLIPS`; whatever spawns it names it.
The frame builders in `fx.py` are plain geometry (blobs, strokes, drops) so a new look is a new function, not a rig.

## 5. Verification

- `InkFxSheetTests` (edit mode): the manifest (one cell, 24 fps, one-shots but the mark), the material, InkFx and
  WrenFx in the persistent scene, the two tiles and the wet edge in place.
- `InkFxPlayTests` (play mode): an effect plays at its point, angle and scale in front of the plane and is pooled
  when its clip ends; a following effect keeps its owner's offset; a mark stays until destroyed; an unloaded clip
  is a no-op; a drawn weak floor breaks as a crumble with the planks gone at once; a hit splashes on Wren.

## 6. Open

- The MemorySmudge (PRG-17) is still the placeholder: it is spawned at run time and `IrisSeed.Spawn`, `MemoryDrops`
  and the boss kits' runtime props have no kit to read. A runtime sheet library (the Smudge's clips beside the Fx's)
  is the next step for all of them.
- The Cantor's erasure plays three sweeps at the bell; the page-wide rub-out (the fade group's `_ink` drop) has no
  eraser texture of its own yet.
- The Bind's redraw traces a wren-sized oval, not Wren's actual silhouette.
- The wet edge is one drawing; the Blank's islands (ENV-08) will want it on every side.
- Hit-stop and screen shake are unchanged; the effects do not yet read the strike's strength.
