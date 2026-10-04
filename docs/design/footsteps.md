# Footsteps (AUD-12, v1)

Wren's feet on the world's ground. Every ground block already wears a paper-kit tile that says what it is drawn as
(`docs/design/paper-kit.md`), so it also says what a step on it sounds like. Nothing in the builder changes: the block's
material is named for its tile (`M_Ground_Boardwalk`), and a step reads it from the block under her feet.

## 1. The grounds

`FootstepSounds` (Core) has ten surfaces, each three takes of a step and a landing, in ink and paper like the rest.

| Surface | Tiles | A step | Gain |
|---|---|---|---|
| Wood | Boardwalk (and its faded, weak and hidden boards), Timber, Boards, Parquet | a hollow knock | 0.3 |
| Stone | Stone, Basalt, Granite, Flag, Cobble, Cobbles, Lip, Grey, Street, Causeway | a hard click | 0.3 |
| Iron | Iron | a short clank | 0.4 |
| Ash | Ash, Cinder | a crunch | 0.3 |
| Moss | Moss, Root | almost nothing, soft | 0.2 |
| Earth | Lane, Cracked, Chalk | a dull pat and a little grit | 0.3 |
| Grass | Turf | a swish | 0.25 |
| Sand | WhiteSand | a shuffle | 0.25 |
| Water | Shallows | a splash, a bubble rising | 0.4 |
| Paper | Line, Crayon, and anywhere nothing says | a dry tick, hardly that | 0.2 |

Where a block wears no tile (the greybox's grey), the region says what the ground is: the coast boards, Emberdown
ash, the Verdance moss, Halden stone, Windreach grass, the Greyfold earth, the Blank paper.

## 2. How they play

`Footsteps` (World) goes on Wren with `WrenSounds`. While she runs on the ground a footfall comes every **stride**:
the run clip's loop holds two footfalls, so the stride is her run speed times the loop's length over two
(`FootstepSounds.Stride`); with no run clip it is 2.6 units, about three and a half steps a second at a full run.
Starting off, the first foot comes down half a stride in. A dash, a glide, a cling, the air or a frozen Wren makes
no steps. The three takes turn in order, so no two steps in a row are alike. A **landing** after at least 0.08 s in
the air is the ground's landing under her pen set down, at half its gain after a hop and whole after a second's fall
(`FootstepSounds.LandGain`); stepping off a lip is not a landing. Steps play from her place and rank as world
one-shots: the first sounds dropped when too much sounds at once.

The surface under her is found by a short ray down from her feet on the ground mask; each block's answer is kept.

## 3. The pipeline

The same render command as Wren's (**OWSBG → Render Wren's Sounds**, `InkSoundsExport.Render`) writes
`docs/audio/sfx/wren_sfx_step_<surface>_<take>.wav` and `wren_sfx_landing_<surface>.wav`, 40 files.

## 4. Verification

- `FootstepSoundsTests` (EditMode, 4): every `Ground_*` tile in the seven kits' folders says what it is, and every
  region has a ground; every surface has three takes that turn and a landing, quiet, hers and ranked to be dropped
  first, the takes unalike, a landing longer than a step; moss dull and quietest, stone and ash high, wood low, iron in
  a plate's register, a splash longer than a click; the stride and the landing's loudness.
- `FootstepsTests` (PlayMode, 3): running on a block wearing the iron tile steps on iron a stride apart with the
  takes turning, and stops when she does; a bare block in no region is paper, and a jump lands on its landing under
  the pen; a dash makes no steps and the steps come back after it.

## 5. Open

- **They are sketches.** Recorded feet on real boards, ash and moss are the point of the delivery files.
- **The run clip's own frames.** Steps keep time by distance at the clip's stride, not on the frames where a foot
  touches; a hand-drawn run with an uneven gait wants its contact frames named.
- **A fading ground** sounds the same until it is gone; thinning steps with the fade stage is a feel question.
- **Wall clings and slides** (the Talonhold) and other birds' feet are silent.
