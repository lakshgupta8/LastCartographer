# Paper Kit (ENV-01, ENV-02, v1)

How a region's backdrop and ground get drawn. ENV-01 proved the pipeline on the Quay (`Saltmarrow_A`);
ENV-02 finished the Saltmarrow kit, so all sixteen coast rooms wear it: backdrop strips, ground tiles, the
faded third lighthouse's paler set, the chapel's and the lighthouse's own strips, salt stone and the tide.
Every step is a file the team can rework by hand.

## 1. The pipeline

```
tools/paperkit/<region>.py   (Blender, headless)
        │  cut-out geometry in the region's palette, Freestyle ink lines, straight alpha
        ▼
Assets/_Project/Art/Environment/<Region>/Paper_<Layer>.png, Ground_<Tile>.png, kit.json
        │  EnvironmentTextureImporter: mipmaps, straight alpha; strips clamp, tiles repeat
        ▼
ProjectSetup.MakePaperLayer / SkinGround   (the greybox builder)
        │  a region with a kit layer of that name gets it on the InkSprite shader; otherwise the flat wash
        ▼
Greybox_<Room>.unity: Paper_* quads and planked ground blocks in the room's FadeGroup
```

Render the Saltmarrow kit (about eight minutes for fifteen layers), then rebuild the scenes:

```
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/paperkit/saltmarrow.py
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.ProjectSetup.BuildBootstrapScene -quit
```

Pass layer names after `--` to re-render only those (`-- Paper_Mid_Reeds`).

## 2. What a layer is

| Kind | Where it goes | Size | Density | Wrap |
|---|---|---|---|---|
| Strip (`Paper_*`) | one of the room's parallax quads, 80 units wide, at the greybox's height and depth | 80 × h units | 40 px/unit | clamp |
| Tile (`Ground_*`) | the material of a ground block itself, mapped in world space by face | 4 × 1 units | 96 px/unit | repeat, world-space |

The Saltmarrow strips, from the art-direction's camera (perspective, 18 units back):

| Layer | z | y range | Depth wash | Line | Rooms | What it is |
|---|---|---|---|---|---|---|
| `Paper_Fore_Reeds` | −4 | −0.8 … 0.8 | none, darker than ink-wash | 3.0 px | Quay | reeds in front of the walkway; blurred by the foreground pass; drops at stage 3 |
| `Paper_Mid_Reeds` | 3 | 0 … 6 | 0.15 | 2.0 px | Quay, Merrow's End, recipe rooms | the reed bank and its salt-flat foot |
| `Paper_Far_Roosts` | 8 | 2 … 12 | 0.45 | 1.4 px | same | eleven stilt-roosts, the jetty and its posts, the shallows |
| `Paper_Farther_Cliffs` | 16 | 6 … 22 | 0.6 | 1.3 px | same | two dune ridges and the sea line, nearly paper |
| `Paper_*_Faded` | as above | as above | +0.3 | ×0.6 | Chain_3 | the same three drawings, washed further, drawn thinner: the faded third from the start |
| `Paper_Mid_Salt` | 3 | 0 … 6 | 0.15 | 2.0 px | Chapel | salt crust, cracks, dead reeds, tether posts |
| `Paper_Far_Chapel` | 8 | 4 … 18 | 0.45 | 1.4 px | Chapel | the Salt Chapel: nave, gable, bell tower, buttresses, salt climbing the walls; three roosts west of it |
| `Paper_Far_Tower` | 8 | 4 … 18 | 0.45 | 1.4 px | Lighthouse | the fourth lighthouse over the lamp room, the keeper's hut, the third and fifth far off |
| `Paper_Farther_Sea` | 16 | 6 … 22 | 0.6 | 1.3 px | Lighthouse, Chapel | open sea to the horizon, wave lines, an island with a tower far to the west |

The tiles, 4 × 1 units at 96 px/unit, mapped in world space:

| Tile | Rooms | What it is |
|---|---|---|
| `Ground_Boardwalk` | Quay, Merrow's End, every recipe room but the faded third | quay planks, nails, grain, a beam below |
| `Ground_Boardwalk_Faded` | Chain_3 | the same planks washed and thinned |
| `Ground_Shallows` | the Boardwalk's two gaps | the tide over mud: pale water, wave lines, a darker bed, weed |
| `Ground_Stone` | Lighthouse, Chapel | salt-crusted stone in two staggered courses |
| `Ground_Boardwalk_Weak` | every weak floor | the planks rotten through: cracks, a missing board, a sagging beam (ENV-12) |
| `Ground_Boardwalk_Hidden` | every hidden platform | planks a Field lantern draws: a dotted outline, the boards barely washed in (ENV-12) |

A faded layer is the same geometry (the random seed comes from the name without `_Faded`) with the
depth wash raised by 0.3 and the line at 0.6 of its thickness.

`Paper_Mid_Bones` (z 3, 0 … 6, the Bone Bridge) is the coast's twelfth strip (ENV-03): the whale faded to its bones
across the channel, skull to the west, ribs into the flat, the flukes east.

### 2b. The Emberdown kit (ENV-03)

`tools/paperkit/emberdown.py`, on the plumbing both kits share (`tools/paperkit/kitlib.py`: materials, cut-out
helpers, the Freestyle render, the build loop). Smoke-grey paper (0.82, 0.80, 0.78), charcoal, sulphur, ember
orange, black ink (art-direction 5). The strips sit at the coast's depths (3 / 8 / 16) and the recipes pick three a
room (`EmberdownPapers(mid, far, farther)`).

| Layer | z | Rooms | What it is |
|---|---|---|---|
| `Paper_Fore_Slag` | −4 | the square | cinder and slag heaps in front of the walk, ember flecks in them |
| `Paper_Mid_Roosts` | 3 | the town, the bell, the chimneys, the Overlook | the cliff behind the town, roosts cut into it on ledges, ladders, ash on every sill |
| `Paper_Mid_Furnaces` | 3 | the stair | iron furnace fronts, glowing doors, pipes, a landing rail |
| `Paper_Mid_Springs` | 3 | the baths | sulphur pools in the rock, steam standing over them, boardwalk posts |
| `Paper_Mid_Gallery` | 3 | the flue road, Hollowvein | timber props and lintels, a lamp for each name, rubble at the foot |
| `Paper_Far_Chimneys` | 8 | the stair, the pit-head, the chimneys, the Overlook | the nine chimneys on the skyline, head-frames, mine mouths lit red, smoke going east |
| `Paper_Far_Bell` | 8 | the town, the bell | Kettil's Rest from below: ashed roofs, the bell tower over them |
| `Paper_Far_Dark` | 8 | the flue road, Hollowvein | the mine's dark: a charcoal wash, faint beams, a rope going down |
| `Paper_Farther_Ridge` | 16 | most rooms | the basalt highland, two ridges nearly paper, ash in the air |
| `Paper_Farther_White` | 16 | the Overlook | the Greyfold from outside: a white mass on the horizon, bigger than it looks |

Tiles: `Ground_Basalt` (black blocks, cracks lit faintly from below), `Ground_Iron` (riveted plates, a rust
bloom), `Ground_Timber` (rough planks over a beam), `Ground_Ash` (grey stone, ash drifted along the top). Props
(`props.py`, region `Emberdown`): the coast's desk, ledger, vantage stake, lamp and glow, seeds and bound stake
redrawn in the highland's colours, plus `Prop_Bell` (the Roll-Call Bell on its frame, 2.5 × 3.5), `Prop_Anvil` (the
smith's, 2 × 1.5), `Prop_Boards` (the pit-head's mine mouth, boarded, 3 × 2.5) and `Prop_Porch` (Kettil's, 3 × 2).
Render a shared drawing for one region with `-- Emberdown:Prop_Desk`.

### 2c. The Verdance kit (ENV-04)

`tools/paperkit/verdance.py`, on the same plumbing. Pale gold paper (0.94, 0.90, 0.72), deep green, moss, bone white,
sepia ink (art-direction 5). The strips sit at the coast's depths (3 / 8 / 16) and the recipes pick three a room
(`VerdancePapers(mid, far, farther)`); the road and the House's doors add the fern line in front.

| Layer | z | Rooms | What it is |
|---|---|---|---|
| `Paper_Fore_Ferns` | −4 | the road, the east door | ferns and grasses in front of the walk, dark, a thick line |
| `Paper_Mid_Trunks` | 3 | the road | trunks eighty wingspans tall leaving the top of the strip, buttress roots, moss on the north side |
| `Paper_Mid_Roots` | 3 | the House, the chapel | one tree's roots as a wall, doors and windows cut in them, lanterns hung from the roots |
| `Paper_Mid_Branches` | 3 | the grove | branches across the strip, lanterns on cords, the knots the threads catch, leaves |
| `Paper_Mid_Shelves` | 3 | the library | bookcases the floor swallowed, roots through them, dust that does not fall |
| `Paper_Mid_Village` | 3 | Aldermere's lane and square | low houses under deep roofs, bunting between them, bread on the sills |
| `Paper_Mid_Ash` | 3 | the ash field | the same houses already paper: outline only, bone and paper, the bunting still up |
| `Paper_Mid_Gate` | 3 | the gate | two stone gateposts, the landing ledge between them, the inscription's band, roots up the stone |
| `Paper_Far_Canopy` | 8 | most rooms | the trunks going on up, leaves in masses, light in shafts between them |
| `Paper_Far_Lanterns` | 8 | the chapel, the grove | lanterns hung in the canopy by birds who could reach it, some still lit |
| `Paper_Farther_Forest` | 16 | most rooms | the forest behind the forest, trunks nearly paper, the light through them |

Tiles: `Ground_Root` (bark and root in bands, moss in the joins), `Ground_Moss` (packed earth, moss along the top),
`Ground_Flag` (pale flagstones, a fern in a crack), `Ground_Lane` (Aldermere's lane: cart ruts, patches already paper).
Props (`props.py`, region `Verdance`): the coast's desk, ledger, vantage stake, lamp and glow, seeds and bound stake in the
forest's colours, plus `Prop_Milestone` (a flying-age milestone, 1.5 × 2.5), `Prop_Lantern` (a grove lantern on its cord,
1 × 2.5; eleven of them ring the vigil), `Prop_Lectern` (Ansel's, page 214 open on it, 1.5 × 1.5), `Prop_Bunting` (two poles
and a cord of flags, 4 × 2.5) and `Prop_Anchor` (an anchor-point: a knot of root round a bone ring, 1 × 1, drawn on every
placed `TetherAnchor`). Since ENV-04 a prop's material is per region (`M_Prop_Desk`, `M_Prop_Desk_Emberdown`,
`M_Prop_Desk_Verdance`): before, the last region built overwrote the coast's drawings with its own.

The coast's kit gained `Paper_Mid_Irises` for the Pale Iris Fields: irises to the horizon, pale, and the Reedmother's
reed-nest in the middle of them.

### 2d. The Halden kit (ENV-05)

`tools/paperkit/halden.py`, on the same plumbing. Cool cream paper (0.92, 0.92, 0.87), slate, verdigris, brass,
blue-black ink (art-direction 5). The strips sit at the coast's depths and the recipes pick up to three a room
(`HaldenPapers(mid, far, farther)`); an interior (the Hall, the tower, the dome, the Vault) drops the far ones.

| Layer | z | Rooms | What it is |
|---|---|---|---|
| `Paper_Fore_Balustrade` | −4 | the bridges | a stone balustrade in front of the walk, brass finials |
| `Paper_Mid_Bridges` | 3 | the Seven Bridges | spans over the drop, piers going down, the seventh's scaffolding, the far parapet |
| `Paper_Mid_Mills` | 3 | the Paper Mills | mill houses on the race, wheels, vellum hung to dry, the same thumbprint on every sheet |
| `Paper_Mid_Lowmarket` | 3 | Lowmarket | stalls and low houses under the wall, the paint thinner (a mid layer washed like a far one), the notice board |
| `Paper_Mid_Hall` | 3 | the Hall, Voss's office | panelled walls, pilasters, framed charts, the roll of names, brass lamps |
| `Paper_Mid_Orchard` | 3 | the Old Orchard | the orchard wall, old trees with sparse leaves, the raked pile, the flyer-tower's foot |
| `Paper_Mid_Tower` | 3 | the flyer-tower, the drill-yard, the Vault | stone courses with talon grooves, the old landing doors high up and no stairs, a door cut at the foot |
| `Paper_Mid_Dome` | 3 | the Observatory | the dome's ribs, the brass frame with its seven sockets (one full), instruments, a stair |
| `Paper_Far_Citadel` | 8 | most rooms | roofs of copper gone green, the walls, towers, the dome |
| `Paper_Far_Drop` | 8 | the bridges | the drop: cliff faces, the mills' roofs far below, mist |
| `Paper_Farther_Sky` | 16 | outdoors | always late afternoon: a long light low across the plateau, far towers nearly paper |

Tiles: `Ground_Granite` (long slabs, a copper strip gone green), `Ground_Boards` (the mills' pale boards, wet along the
top), `Ground_Parquet` (the Hall's herringbone, a brass inlay), `Ground_Cobble` (Lowmarket's and the orchard's, a leaf).
Props (`props.py`, region `Halden`): the shared furniture and the anchor-point in the Citadel's colours, plus
`Prop_Gravestone` (the orchard's, a crest cut in it, 1.5 × 2), `Prop_Wheel` (a mill wheel, 2.5 × 2.5), `Prop_Scaffold`
(the seventh bridge's repair, 3 × 3), `Prop_Frame` (the frame of the Great Atlas: seven sockets, one stone, 3.5 × 4),
`Prop_Slots` (the Vault's wall: seven niches, one empty, 5 × 2.5), `Prop_ExamDesk` (a desk with the same paper on it,
2.5 × 1.25) and `Prop_Notice` (survey scheduled, pasted over itself, 1.5 × 2). A piece that turns (a branch, a rib, a
spoke) is built at the origin and placed after (`rbox`): a box turned in place turns about the world origin.

### 2e. The Windreach kit (ENV-07)

`tools/paperkit/windreach.py`, on the same plumbing. Straw paper (0.93, 0.88, 0.70), sky blue, storm violet, gold,
grey-brown ink (art-direction 5). The wind is from the west: every blade in every layer leans east. The recipes pick up
to three a room (`WindreachPapers(mid, far, farther)`); the cliff and the crater take the rim for their far layer.
Layer names are unique across the kits (`WindreachRoomsTests` holds it): a material is named after its layer, and two
kits sharing a name would overwrite each other's drawing at build.

| Layer | z | Rooms | What it is |
|---|---|---|---|
| `Paper_Fore_Grass` | −4 | the high grass | long grass in front of the walk, dark, a thick line |
| `Paper_Mid_Stones` | 3 | the Nine Stones | standing stones in a line across the grass, lichen on their north faces, notches, long shadows east |
| `Paper_Mid_Camp` | 3 | the Long Grass Camp | walking-wagons in a ring, the fire, the route woven on one wagon's cloth, smoke going east |
| `Paper_Mid_River` | 3 | the Dry River | the far bank of cracked mud, boats on their sides, dead reeds, the cut bank rising at the east |
| `Paper_Mid_Cliff` | 3 | the cut bank | twelve units of cliff: strata, talon grooves, the lip's grass at the top |
| `Paper_Mid_WindGate` | 3 | the leap | the lip's flat carved stones, two tall stones leaning toward each other, the wind drawn as ink-swirls |
| `Paper_Mid_HighGrass` | 3 | the glide course, the high grass | grass over anyone's head, seed-heads, a trampled way through the middle |
| `Paper_Mid_Hearth` | 3 | Idrenne's Fire | the hearth in its ring, the cooking-stone flat on it, a pot on a tripod, wagons either side |
| `Paper_Mid_Crater` | 3 | the Fallen Star | the iron wall the Star threw up, the Star as the anvil, hammer-marks, sparks, the smiths' wagon |
| `Paper_Far_Steppe` | 8 | the walk | grass to the horizon in pale bands, the stones' line going away small, far wagons |
| `Paper_Far_Rim` | 8 | the cliff, the Gate, the crater | the crater's far rim and the heights, pale violet, grass along their tops |
| `Paper_Farther_Storm` | 16 | outdoors | sky that is most of the screen: storm violet banked on the horizon, a gold light under it, a flight going east |

Tiles: `Ground_Turf` (packed earth, grass along the top), `Ground_Cracked` (the riverbed's mud in plates),
`Ground_Lip` (the Gate's flat stones, carved, lichen in the joints), `Ground_Cinder` (the crater's floor, iron flecks, a
glow in the cracks). Props (`props.py`, region `Windreach`): the shared furniture in the Steppe's colours, plus
`Prop_Stone` (a standing stone, 1.5 × 3), `Prop_Wagon` (a walking-wagon, 4 × 3), `Prop_Fire` and `Prop_Ashes` (the
camp's fire, and its ring where the camp is not), `Prop_Bedroll`, `Prop_Hull` (a boat on its side, 3 × 1.5),
`Prop_Reeds`, `Prop_LipStone` (a flat stone carved with a place), `Prop_Swirl` (one turn of an updraft's ribbon, 2 × 2,
tileable top to bottom), `Prop_Hearth` (the ring, the cooking-stone, the pot on its tripod, 2.5 × 2), and the grass:
`Prop_Grass_A/B/C` (tufts, 1 × 1.25) and `Prop_Grass_Tall` (the high grass, 1.5 × 2.5).

**The grass** (`GrassField`, World): a recipe's `Grass(x0, x1, n, tall, z, y)` is a row of tufts, each the drawing on a
root at its feet; the field leans them with the wind (gusts travelling east along the row, the tips going east) and
parts them round Wren (a tuft within about a unit bends away from her, most when she stands in it, and springs back once
she has passed). The high grass stands in rows before and behind her at the third fire. One behaviour per field;
tufts cast no shadows. **The updrafts** (`InkSwirl`): `Updraft(x, bottom, height, speed)` is the lift in a column two
wide with the ribbon stacked up it, rising and wrapping from the top back to the foot, swaying a little; the Nine
Stones' are too weak to ride (three a second), the Gate's and the crater's carry her (nine).

### 2a. Props (ENV-09)

`tools/paperkit/props.py` draws the hubs' furniture with the same helpers: one cut-out each at 96 px/unit, feet at
the bottom edge, centred, no depth wash (they stand on the play plane), listed in `kit.json` as kind `prop` with
their size in units. `ProjectSetup.MakeProp` stands one on an InkSprite quad a little behind the play plane
(z 0.5 to 0.9), sized from the manifest, under whatever object asked for it; a helper whose prop is missing keeps
its greybox block, so a region without a kit still builds. The material is lit like the ground (shadows on,
`_Lighting` 0.7); the lamp's glow is flat, unshadowed and grainless. Every `Prop_*` renderer joins the room's fade
group at dropout 5: it thins with the place and never drops, so a desk stays a desk to the last.

| Prop | Size | Where | What it is |
|---|---|---|---|
| `Prop_Desk` | 2 × 1.5 | under every `DraftingDesk` | the drafting desk on trestles: slanted board, a sheet with a coast on it, inkwell and quill, a rolled chart below |
| `Prop_Ledger` | 1.5 × 2 | under every `CommissionLedger` | the commissions board: a post, a framed board, four pinned slips |
| `Prop_Dummy` | 1 × 1.625 | under the `TrainingDummy` | a sacking dummy bound with rope on a post, a painted ring; the behaviour rests it white and flashes it bright |
| `Prop_Stall` | 2.5 × 2 | the Quay, under the stilts | the Ferrymen's stall, where tethers are sold: crates, coils, a sign under an awning |
| `Prop_Vantage` | 0.75 × 2 | under every `VantagePoint` | a survey stake with a brass plate and a ribbon |
| `Prop_Lamp`, `Prop_LampGlow` | 1.5 × 2.5, 2 × 2 | under every `TravelPoint` lamp | a lantern on an iron stand; its light on the paper behind it, the renderer the travel point shows once lit |
| `Prop_Seeds` | 0.75 × 0.75 | under every placed `IrisSeed` | split pods on a stem (runtime drops keep the sphere) |
| `Prop_Bound` | 0.5 × 1.25 | every bound of a walk; the Ferry | a bound stake with a knotted cord, in the marker blue |
| `Prop_Nets` | 1.75 × 1.5 | the Quay, behind Sable's post | a drying rack with a net over it |
| `Prop_Stoop` | 2 × 1.25 | Merrow's End, behind Dotha | two stone steps, a bench, a pot with a dead reed |
| `Prop_Boat` | 3 × 1 | the Quay's west end, the Shore | a beached rowing boat, an oar across it |
| `Prop_Tether` | 1 × 3 | Merrow's End by the vantage, the Tetherline ×4, the Ferry ×2 | a tether-post, its rope running up and away into the white |
| `Prop_WetEdge` (the Greyfold kit's first layer: `Art/Environment/Greyfold/`) | 4 × 12, stretched to 30 tall | the Edge, over the first white sheet's start | where the paper is wet before it is white: a fibrous damp band, no ink line, outside the fade group (ENV-12, `ink-fx.md` §3) |

Depth wash is how far each colour is lerped toward the region's paper: aerial perspective as thinning ink
(art-direction 3). Ink lines thin the same way.

## 3. Shader

Every kit layer is on `OWSBG/InkSprite`, so the place's fade (`FadeGroup`, fade-stages.md) thins its lines
and washes it to paper with one `_Ink` value. Two properties were added for the kit:

- `_WorldUV` (0/1): tiles map in world space by face (front and back in the play plane, the top along it,
  the ends across it), `_BaseMap_ST.xy = 1 / tile size`, so planks run on across blocks, a platform's top
  reads under the camera's tilt, and every block of a room shares one material.
- `_Shadows` (0/1): the walkway takes Wren's shadow; a backdrop strip never does.
- `_Lighting` (0–1): how much of the scene light a drawing takes. Backdrops 0.3, so the sun does not bleach
  the wash; ground 0.7; birds 1.

Backdrops light flat (`_ShadowStep` 0). Ground keeps a shallow ramp (0.2) so the shadow reads as ink.
`ProjectSetup.RegionPaper` holds the six paper colours from art-direction 5.

## 4. Palette in the kit

Saltmarrow: paper (0.93, 0.89, 0.80), silver-grey (0.64, 0.66, 0.64), olive (0.50, 0.54, 0.36),
rust (0.60, 0.36, 0.24), ink (0.08, 0.10, 0.16). Materials are unlit emission so the PNG carries the exact
palette; Blender's view transform is Standard, not AgX.

## 5. Reworking by hand

- **Repaint a layer:** paint over the PNG at the same size (the manifest `kit.json` gives the pixels and
  units), keep straight alpha, rebuild the scenes. Nothing in Unity refers to the Blender file.
- **Reshape a layer:** edit the shapes in the script (reeds, ridges and roosts are a few lines each), or
  replace a function with an import of a hand-modelled `.blend`; the render settings stay.
- **A new region:** copy the script, change the palette and the layer list, keep the names the room builder
  uses (`Paper_<name>` for `MakePaperLayer`, `Ground_<name>` for `SkinGround`), and the builder picks them up
  for every room of that region with no code change.

## 6. Verification

- `PaperKitTests` (edit mode): the manifest's fifteen layers exist at the size their quads expect, every
  backdrop material is on the ink shader over its own drawing, every tile material tiles in world space, each
  of the sixteen coast scenes stands on the tile it should (planks, paler planks, the tide, stone), and the
  importer clamps strips and repeats tiles.
- `PaperKitPlayTests` (play mode): the Quay loads through Addressables with four inked backdrop layers and
  five planked blocks in its fade group; advancing the place to stage 2 thins the reeds and washes the planks; the
  foreground is the only layer set to drop before stage 4. The faded third, the chapel and the boardwalk load with
  every fade-group layer a kit drawing, the named strip and tile among them.

## 7. Open

- The weak floors' and hidden platforms' tiles are drawn (ENV-12); the Shore's sea-fade sheets and the rest of
  the Blank's look are ENV-08's.
- The Shore's sea-fade sheets stay white quads: the Blank's look is ENV-08's.
- The Greyfold Edge's three strips (`Mid_Edge`, `Far_Cathedral`, `Farther_Edge`) are ENV-08's.
- Per-region `_Ink` curves (fade-stages.md §7) can now be tuned against real layers.
- The props are one drawing each (§2a); a second state (the desk with Wren at it, the stall shuttered at night, the
  glow at half) would be a second file and a rule in the helper. Seeds dropped by enemies at run time are still the
  greybox sphere: `IrisSeed.Spawn` has no kit to read.
- A wind sway on the reeds (vertex offset in the shader) would sell the parallax; not in v1.
