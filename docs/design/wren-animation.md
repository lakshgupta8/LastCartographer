# Wren's Model and Animation (CHR-02, CHR-03, CHR-04, CHR-05, v1)

How Wren is built and moved for version one, and how the team replaces her frame by frame later.
The model sheet is `docs/art/wren-turnaround.png`; her six Charters side by side are `docs/art/wren-charters.png`.

## 1. The pipeline

```
tools/characters/wren.py         (Blender, headless)
        │  parts on a hierarchy of empties; every clip is a function of time; Freestyle ink; 2x density
        ▼
tools/characters/.frames/wren/   (intermediate, not committed)
        │  python tools/characters/pack.py wren: premultiplied downsample to 96 px/unit, one strip per clip
        ▼
Assets/_Project/Art/Characters/Wren/Wren_<clip>.png + wren.json;  docs/art/wren-turnaround.png
        │  CharacterTextureImporter: straight alpha, no mipmaps, clamped, uncompressed
        ▼
ProjectSetup.BuildWren: InkSheetPlayer (the clips) + WrenAnimator (which clip when) on the persistent Wren
```

```
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/wren.py
python tools/characters/pack.py wren
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.ProjectSetup.BuildBootstrapScene -quit
```

Clip names after `--` render only those; `-- turnaround` re-renders the model sheet.

## 2. The model

Art-direction 4, as parts: a brown-grey body with a cream breast, an ink-blue cowl (a cone over the
shoulders) with a brass clasp, a round head with a beak, a dark eye with a paper glint and a crest, two
wings, a tail, two thin legs with feet, and the needle-quill (1.1 units, nearly her height) pivoting at the
near wing's tip. She stands 1.25 units. Every part hangs from an empty (root, hips, body, head, wing_near,
wing_far, quill, tail, leg_l, leg_r), so a pose is a few rotations and offsets from rest; there is no
armature and nothing is baked.

Colours are flat emission washes; the ink line is Freestyle (silhouette, border, creases over 110°) with a
pen's noise and a calligraphic nib, as in the paper kits, so she and the world are one drawing.

## 3. The frames

Rendered side-on at 192 px/unit (2x), packed at 96 px/unit into 192 × 192 px cells (two units square: room
for the quill and the jumps), her feet at the cell's bottom centre. The quad on Wren is 2 × 2 units at
(0, 1); `WrenView` still flips it for facing and squashes it on landing.

| Clip | fps | Frames | Loops | When (`WrenAnimator`, top priority first) |
|---|---|---|---|---|
| death | 12 | 8 | no, holds the last | the last mask |
| hurt | 12 | 3 | no | 0.25 s after a hit |
| strike1 / strike2 / strike3 | 24 | 6 | no | a forward swing, by combo step; the frame follows `QuillStrike.Progress` |
| strike_up | 24 | 6 | no | an up-swing |
| pogo | 24 | 4 | no | the down-strike |
| crosshatch | 24 | 12 | no | the Crosshatch: the quill scribbling in a cone, six strokes; the frame follows `Flourishes.Progress` |
| longstroke | 24 | 6 | no | the Longstroke: the wind-up, the long thrust with the whole body behind it; follows `Progress` |
| blot | 24 | 6 | no | the Blot: the quill stabbed down at her feet, the crouch, the burst; follows `Progress` |
| thread_cast | 24 | 2 | no | the Inkthread flung at the anchor: the first 0.09 s of a thread (restarted on `Threaded`) |
| thread | 24 | 4 | yes | the pull: stretched along the line |
| thread_catch | 24 | 3 | no | the hop at the anchor, 0.15 s after `ThreadArrived`, unless she lands, dashes or threads again |
| dash | 24 | 4 | no | the Wingbeat (restarted on the event): wings snapped back, the streak, thrown open to brake |
| bind | 12 | 8 | yes | the bind held: the quill circles her |
| survey | 12 | 6 | yes | the survey held at a vantage (`VantagePoint.Surveying`) |
| walljump | 24 | 3 | no | the push off a wall, 0.15 s after `WallJumped` (fires after `Jumped` when the jump was a wall's) |
| cling | 12 | 4 | yes | the Talonhold: gripping, a breath, a glance up |
| slide | 12 | 3 | yes | the hold spent (`IsSliding`): dragged down, the talons scraping |
| glide_rise | 12 | 4 | yes | carried up an updraft (`IsLifted`, within three steps of a `Lift`): wings cupped higher, the head up |
| glide | 12 | 6 | yes | in the air, gliding: wings wide and flat, a slow bob |
| jump | 12 | 4 | no | rising (restarted on the jump) |
| fall | 12 | 4 | yes | falling |
| land | 12 | 3 | no | 0.25 s after landing |
| run | 12 | 8 | yes | grounded and moving |
| idle | 12 | 8 | yes | otherwise |

The strike's frames are not on the clock: startup, active and recovery (3, 4, 8 game frames) map onto the
six drawn frames through the swing's progress, so the drawing lands when the hitbox does.

## 4. In the engine

`InkSheetPlayer` windows the strip through the renderer's property block (`_BaseMap` and `_BaseMap_ST`),
so Wren keeps one material and the ink state (`_Ink`, the Remnant Charter's tint) stays where it was. A
looping clip cycles; a one-shot holds its last frame; `Seek` shows a fraction. `WrenAnimator` reads the
controller, the strike, the flourishes, the vitals and the vantage; the events for jump, dash, land and hurt
restart their clips so the first frame always shows.

## 5. Reworking by hand

- **Redraw a clip:** replace `Wren_<clip>.png` with a strip of the same frame count and cell (or change
  the count in `wren.json`), then rebuild the scenes. The animator does not care where the pixels came from.
- **Change a pose or timing:** the clip functions in `wren.py` are a dozen lines each; frame counts and
  rates sit in `CLIPS`.
- **Swap the model:** any Blender rig that exposes the same empties (or a Rigify rig with the same
  names on its bones) drops into the same render loop.
- **A new character:** copy `wren.py`, keep the `CLIPS` shape and the output naming; `pack.py` and
  `ProjectSetup.LoadSheets` work by name.

## 6. Verification

- `WrenSheetTests` (edit mode): every clip the animator plays is packed at 96 px/unit in 192-px cells,
  locomotion at 12 fps and the quill's moves at 24, one-shots and loops as listed, the swings six frames,
  the model sheet in the docs, the importer as specified, her material on the idle sheet and the persistent
  scene carrying the player, the animator and every sheet.
- `WrenAnimatorTests` (play mode): on a synthetic Wren with the real controller, strike and vitals: idle,
  run, jump, fall, land and back; the first swing's frames advance with its phases and the property block
  windows the frame; the down-strike is the pogo; the hit, then the death holding its last frame.

## 7. Open

- CHR-04 gave the four abilities and the three flourishes their own frames (the table above); the hand-drawn
  pass may still want the thread's line drawn from the quill's nib rather than the controller's.
- A sheet without the new clips (a hand pass mid-way) falls back: a flourish to the rising slash, the slide to the
  cling, the rise to the glide, the cast and the catch to the pull and the air.
- Her shadow on the walkway is the quad's; a drawn contact shadow would sit better.
- The ink line's weight does not yet thicken at the bottom of forms (art-direction 4).
- The Charters' grips keep the Surveyor's swings: each clip turns the quill the same number of degrees from the
  Charter's rest angle, so the Warden's sweep starts from her shoulder and the Drifter's thrust runs low. Each
  Charter's own combo is left to the hand pass (decided 2026-10-02): the Warden's sweep, shove and overhead stay
  the Surveyor's three swings in her cowl until then, and a redrawn strike clip drops into its set by name.
- The Surveyor's and the Unwriter's quills reach the cell's edge in the strike poses, so the tip can be cut off.
  The cell stays 2 × 2 units (decided 2026-10-02): the hand pass redraws those frames and can widen the cell then
  (`CELL` in `wren.py`, the quad's size in `BuildWren`).

## 8. The Charter silhouettes (CHR-05)

Each Charter changes her silhouette (combat doc 5): the cowl's shape and colour, and how she holds the quill.
`wren.py` builds her with a Charter (`Wren(charter)`, the table `CHARTERS`) and renders every clip again in it;
the Surveyor's are the base sheets, the other five are sets beside them.

| Charter | Cowl | Grip | Sheets |
|---|---|---|---|
| Surveyor | the ink-blue cape-cowl, brass clasp | the needle-quill up and forward, held a third along | `Wren/` |
| Warden | a broad stiff mantle in Warden slate, a high collar, a riveted shoulder | the heavy quill (1.7 thick) laid back over the shoulder, held behind her | `Wren_Warden/` |
| Drifter | a short ochre hood and two scarf-tails streaming back | held short and low, like a knife | `Wren_Drifter/` |
| Ferryman | a sea-grey cape-cowl and a boatman's wide brim | upright like a punt-pole, held before her beak, the cord wound round it and its loop hanging | `Wren_Ferryman/` |
| Unwriter | the Choir's pale wool lumped round her shoulders and over the crown | turned round: the nib tucked behind the hand, a pale crumb of wool leading | `Wren_Unwriter/` |
| Remnant | gone grey, the hood pulled up with its torn point drooping behind, the hem in long tatters, the clasp lost | broken short, no nib, carried low | `Wren_Remnant/` |

Every set has all of her clips at the base sheets' frame counts, rates and loops, so the animator never asks
which Charter she wears. In the engine:

- `InkSheetPlayer` carries the other drawings as named `SheetSet`s. `UseSet(name)` swaps the strips under
  whatever is playing without restarting it (the run carries on at the same frame in the new cowl); a set
  missing a clip draws it from the base, so a hand pass can redraw a Charter clip by clip.
- `CharterSet.Apply` wears `SheetSetOf(kind)` (null for the Surveyor). While a drawing exists the tint stands
  down to white; a Charter with no set yet keeps the base sheets under its old greybox tint.
- `ProjectSetup.BuildWren` loads `Art/Characters/Wren_<Charter>/` for each Charter and hands the sets to the
  player on the persistent Wren.

To redraw: render one Charter (`wren.py -- Warden`, or a few clips: `-- Warden idle run`), pack it
(`pack.py wren_warden`) and rebuild; `charters_sheet.py` redraws the side-by-side sheet. Each Charter has its own
model sheet, `docs/art/wren_<charter>-turnaround.png`.

Verification: `WrenCharterSheetTests` (edit mode: five sets, every clip frame for frame with the base, every pair
of the six differing over a tenth of their outline at rest, every strip wired on the persistent Wren, the model
sheets in the docs) and `CharterSilhouetteTests` (play mode: each Charter worn in its own drawing with no tint
over it, a swap mid-run carrying the clip on at its frame, an undrawn Charter on the base under its tint).
