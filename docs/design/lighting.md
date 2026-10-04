# Lighting and Post per Region (ENV-10)

How each region is lit, what its post does to the picture, where the real lights are, and how the day sits over
it. Art-direction 3 and 5: URP Forward+, real lights for lamps, lava and the Blank's lantern-radius; an
art-directed time of day per region that never moves; the two-step ramp so light reads as ink.

Version one is the table in `RegionLight` (Core), the blender `RegionLighting` (World) in the persistent scene,
the lights the setup stands in the rooms, and the lamp loop in the ink shader. The persistent scene used to carry
one sun and one post for every room; now it carries one of each per region, and the room Wren is in picks.

## 1. The table (`RegionLight`)

One row per region. The sun is a direction and a colour; the ambient is a trilight (sky, horizon, ground); the
paper is what the camera clears to and what the grain pass tints toward; the post is bloom, vignette, saturation,
contrast, a colour filter and a warmth; the lamp is what anything that burns in the region casts.

| Region | Hour | Sun | Where from | Ambient | Paper | Post | Lamp |
|---|---|---|---|---|---|---|---|
| Saltmarrow | morning, the sea's light | (1, 0.95, 0.86) × 1.4, soft shadows | 40° up, 20° west | the old shared one: sky (0.70, 0.72, 0.78), horizon (0.55, 0.52, 0.48), ground (0.32, 0.30, 0.27) | warm cream | bloom 0.25 over 1.1, vignette 0.22 in ink; no grade | gold |
| Emberdown | no sky: lamplight and the furnaces | (1, 0.74, 0.52) × 0.55 | 62° up, a shaft | dark: sky (0.30, 0.26, 0.26), ground (0.20, 0.14, 0.10) | smoke-grey | bloom 0.6 over 0.9 (the embers bloom), vignette 0.38 near black, −8 saturation, +8 contrast, warm filter, −5 temperature; tint toward (1, 0.90, 0.82) at a quarter | ember (1, 0.52, 0.20) |
| Verdance | noon under the canopy, the light in shafts | (1, 0.97, 0.80) × 1.25 | 62° up | green: sky (0.62, 0.70, 0.52), ground (0.30, 0.32, 0.22) | pale gold | bloom 0.35 over 1.0, vignette 0.24 in green-black, +6 saturation, a green-gold filter | bone white |
| Halden | always late afternoon, a long light low across the plateau | (1, 0.86, 0.66) × 1.5 | 22° up, 55° west | sky (0.66, 0.70, 0.78), horizon (0.62, 0.56, 0.48) | cool cream | bloom 0.3, vignette 0.26, −4 saturation, +4 contrast, +12 temperature (warm); tint (1, 0.94, 0.84) at a fifth | brass |
| Windreach | storm light: gold under a violet sky | (1, 0.90, 0.68) × 1.3 | 28° up, 40° east | violet sky (0.46, 0.42, 0.60) over a straw horizon | straw | bloom 0.3, vignette 0.30 in violet-black, +4 saturation, +6 contrast, a cool filter | gold |
| Greyfold | no hour: a white that is everywhere at once | white × 1.0, **no shadows** | 20° up, from the front | near white all round | white | bloom 0.1 over 1.3, **no vignette**, −20 saturation, −6 contrast | lantern gold (1, 0.88, 0.60) |
| Blank | no hour: the lantern's colour blooming in the white | white × 0.95, no shadows | 15° up, from the front | near white | white | **bloom 0.55 over 0.95** (the colour blooms), no vignette, −10 saturation, −8 contrast | lantern blue (0.85, 0.88, 1) |

The sun's azimuth is its turn about the up axis, 0 from in front of the paper; negative is from the west (screen
left). The white's sun comes from the front and low so every quad is fully lit and the ramp never steps: no form,
no shadow, only the drawing. Halden's sun is low and from the west so a platform's top catches it and its far side
falls into the ramp's shadow step: the long light.

The three "cream" complaints of ENV-08 (the Greyfold reading cream under the shared sun) are answered here: the
Greyfold's and the Blank's rows clear to (0.98, 0.98, 0.97), light white and grade the colour down.

## 2. The blender (`RegionLighting`)

In the persistent scene: the sun, the camera and seven global volumes (`Volume_<Region>`, priority 1 over the
base `Global Volume`, each with `PP_<Region>.asset` written from the table by the build). Every frame it reads
`Room.Current`, finds its region (`Mix.RegionOf`, which knows the room plans, the islands and the epilogue's
stand-ins) and, when it differs from the last, blends from the light that is applied now to the new region's
over `BlendSeconds` (1.2 s, unscaled, so a transition's freeze does not stall it). What it writes each frame:

- the sun's colour, intensity, rotation and shadows (soft, or none in the white);
- `RenderSettings.ambient*` (trilight);
- the camera's clear colour (the region's paper, so a gap in the strips shows paper and not cream in the white);
- the global `_OWSBG_RegionTint` (rgb and amount) the grain pass multiplies in after its own tint, before the held
  grade and the hour;
- the volumes' weights: the entering region's rises as the leaving one's falls; a volume at zero is switched off.

Discrete parts (shadows on or off; whether the hour applies) follow the nearer side of the blend.

**The hour (hub-life 2).** `RegionLight.AtHour` takes `DayCycle.Dusk` and `DayCycle.Night`: night dims the sun to
35 %, cools it halfway to a blue-grey, halves the ambient and darkens the paper; dusk warms the sun without
dimming it. The grain pass's own dusk and night casts stay on top. Rows with `FollowsHour` false are left alone:
the Greyfold and the Blank (no hour), and Halden (anchored, its hour locked). Tests and the capture set
`NightOverride` / `DuskOverride` instead of waiting for the clock.

`Snap(region)` applies without a blend: the capture (`OWSBG_SHOT_HOUR=dusk|night` lights it at that hour), the
build (the persistent scene is saved at the coast's light), and tests.

## 3. The lights

Point lights, no shadows (the 1060 budget: Forward+ clusters them, so a room may carry a dozen without a second
pass). Every one is stood by the setup, named `Light_*`:

| Where | What | Colour, range, intensity |
|---|---|---|
| every travel lamp (`MakeLamp`: the fourth lighthouse) | `Light` under the `TravelPoint`, wired to `_light`, **off until the lamp is lit** (its vantage drawn), with the glow | the region's lamp, 5, 1.8 |
| a hub's `Prop("Lamp")` (her last camp, the capital's doorway) | `Light_Lamp` 1.6 above its feet, always on | the region's lamp, 5, 1.8 |
| `Prop("Hearth")` (Idrenne's Fire) | `Light_Hearth` | ember (1, 0.60, 0.25), 6, 2.4 |
| the camp's fire (`MakeCampSite`, every site) | `Light_Fire` | ember, 5, 2.2 |
| `Paper_Mid_Furnaces` (the Furnace Stair) | three `Light_Furnace` along the strip, 1.2 units in front of it so the ground takes them too | ember, 6, 2.2 |
| `Paper_Mid_Springs` (the baths) | two `Light_Spring` | sulphur (0.75, 0.90, 0.50), 5, 1.0 |
| `Paper_Mid_Lantern` (the Lantern, the Blank's first island) | `Light_Lantern` blue over the middle and `Light_Lantern_Gold` beside it | lantern blue 9 × 2.0; gold (1, 0.85, 0.55) 6 × 1.4 |
| `Paper_Mid_LampRoom` (Aury's lighthouse) | `Light_LampRoom` high in the room | warm white, 10, 2.4 |
| Wren | `LanternLight`: a point light on her that comes up with the lantern-radius (`ClarityMeter.LanternStrength`) and reaches the radius plus one; off on the coast | lantern gold (1, 0.90, 0.70), intensity 1.8 × strength |
| Brann | `Glow` under the boss (`Boss.Glow`/`SetGlow`): 0.4 + 2.6 × his brass's glow (a quarter at rest, all of it as he telegraphs); in the dark it is the only light | (1, 0.55, 0.22), 7 |
| the Fallen Star | the same, 3.2 while it burns (phase 3), nothing before | (1, 0.45, 0.15), 9 |
| the Collapse | the same, moved to the lit lamp each section, 2.4; out when the lamp is | (1, 0.80, 0.42), 8 |

A boss's light is made the first time the fight asks for it and put out when the fight resets, so the perch
is dark.

**The ink shader's lamp loop.** `OWSBG/InkSprite` now compiles `_ADDITIONAL_LIGHTS` and `_CLUSTER_LIGHT_LOOP`
and sums every other light as a pool on the paper: the light's attenuation stepped in two (full above 0.45, half
above 0.12, nothing below), with no facing term (a lantern lights the paper, not a form), added to the sun's ramp
before `_Lighting` scales it. So a backdrop at `_Lighting` 0.3 takes a third of a lamp, the ground 0.7, a bird
all of it (paper-kit 3). The `_WorldUV` ground tiles light the same way.

## 4. The pipeline asset

Per-pixel additional lights were already on, four per object, no additional-light shadows; nothing changed in
`URP_Pipeline.asset`. The renderer stays Forward+. The base `PP_Default` volume keeps what every region shares
(the depth of field, ACES); its own bloom and vignette are the coast's values, under the coast's volume.

**Found on the way:** `PP_Default.asset` had carried four null components since PRG-04. `VolumeProfile.Add`
makes a component in memory, and saving the profile without adding it to the asset writes a null; so the base
volume had never applied its depth of field, bloom, vignette or ACES tonemapping, and every capture before this
row was the raw render under the two full-screen passes. `ProjectSetup.PersistComponents` now adds each
component to its profile's asset (`AssetDatabase.AddObjectToAsset`), for the base and the seven regions, and
the build repairs the base profile in place. ACES applies for the first time from this row.

## 5. Reworking by hand

Change a row in `RegionLight.Table` and rebuild (`BuildBootstrapScene` rewrites the seven profile assets from
the table and stands the lights again). To light a new prop or strip, add a case to `MakePropLight` or
`MakeLayerLights` in `ProjectSetup`. A boss that should glow calls `Glow(colour, range)` once and `SetGlow(k)`
each tick. Editing a `PP_<Region>.asset` by hand is overwritten by the next build: the table is the source.

## 6. Verification

- EditMode `RegionLightingTests`: every region has its own row, hour and sun; the coast keeps the old shared sun;
  the mine is dim with ember lamps; Halden low, warm and locked; the white is white, unshadowed, unvignetted and
  desaturated, the Blank blooming; night dims the coast to under half and leaves the white and Halden alone; the
  blend meets both ends; rooms light by their region; the seven profile assets carry the table's post and leave
  the depth of field to the base; the persistent scene carries the rig, the seven volumes with their profiles,
  and her lantern; the rooms that burn carry lights (the lighthouse's wired to its travel point, the stair's
  three, the baths' two, the camp's fire, the hearth, her last camp's lamp, the Lantern, the capital's doorway,
  Aury's) and the Road That Stops and the stilts carry none; the ink shader compiles with the lamp keywords and
  the pipeline lights per pixel without lamp shadows.
- PlayMode `RegionLightingPlayTests`: the room's region lights the sun, volume, paper and sky, and the next
  region blends in over a breath with the two volumes sharing the weight; night dims and cools the coast and
  leaves the white and Halden; **a lamp is a pool on the paper in a real render** (white paper on the ink shader
  with no sun: the patch under a point light brightens by more than 0.15, the paper beyond its range does not,
  and 1.5 units out is still lit); her lantern is a light in the white that reaches the radius and goes out on
  the coast; a travel lamp's light comes on when its vantage is drawn; Brann's brass is a light as bright as his
  glow and out on reset; the Star is cold until phase 3 and then an ember light.
- Captures (`logs/env10-shot-<room>.png`, tiled in `env10-review.png`; `env10-before-after.png` against the
  CHR-10 captures): the Greyfold's Threshold reads white where it read cream (mean 243 to 251 of 255); the
  Verdance pale gold-green where it read cream; the mine dark with three ember pools on the furnace strip and the
  door glowing; the baths' sulphur on the ground; Halden cool cream under a low warm sun; the steppe straw with the
  fire's pool on the ground; her last camp's lamp a pool in the white; the Lantern's gold blooming in the Blank;
  Aury's lamp room lit; the coast, the mine and the camp at night cool, dim and lit by what burns.

## 7. Open

- **The tells of the hour are coarse.** Night is one multiplier and one colour; a region could want its own
  night (the mine does not get darker, the steppe's storm could). The table has the room for it.
- **The sun does not move within a region**, by design; but the blend between regions is the same 1.2 s whatever
  the door, and a long walk from the forest's gold to the plateau's brass might want longer.
- **The pools read as discs on a flat strip.** The two-step ramp is a full disc and a half disc round each lamp,
  which on the furnace strip and in Aury's lamp room reads as drawn circles rather than light on paper; it is the
  rule (light as ink) taken literally. A soft third step, or a drawn glow in the kit where a light sits, is the
  hand pass.
- **Lamps light the backdrops only a third** (`_Lighting` 0.3). The furnace strip's doors glow in the drawing
  and the lights in front of it light the ground more than the strip; a second `_LampLighting` property would
  let a strip take lamps fully while keeping the sun off its wash.
- **No light casts a shadow** but the sun, and no light is baked. The lantern-radius pass and the real lantern
  light are two things that agree by tuning, not by sharing a radius value in the shader.
- **The edit-mode scene view shows the coast's light** in every room: `RegionLighting` runs in play and on
  capture, not always. A designer opening the Greyfold in the editor sees cream until they press play.
- **Everything here is a hand pass away**: the numbers are a first reading of art-direction 5's table, not a
  colourist's.
