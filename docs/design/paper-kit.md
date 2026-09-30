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

A faded layer is the same geometry (the random seed comes from the name without `_Faded`) with the
depth wash raised by 0.3 and the line at 0.6 of its thickness.

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

- Weak floors and hidden platforms keep their greybox materials: they carry behaviours and need their own
  drawings (a cracked plank, a lantern-drawn outline) with the VFX pass (ENV-12).
- The Shore's sea-fade sheets stay white quads: the Blank's look is ENV-08's.
- The Greyfold Edge's three strips (`Mid_Edge`, `Far_Cathedral`, `Farther_Edge`) are ENV-08's.
- Per-region `_Ink` curves (fade-stages.md §7) can now be tuned against real layers.
- The props are one drawing each (§2a); a second state (the desk with Wren at it, the stall shuttered at night, the
  glow at half) would be a second file and a rule in the helper. Seeds dropped by enemies at run time are still the
  greybox sphere: `IrisSeed.Spawn` has no kit to read.
- A wind sway on the reeds (vertex offset in the shader) would sell the parallax; not in v1.
