# Art Direction — Ink on Paper

The visual identity, and how 2.5D is meant here.

## 1. The look in one sentence
Hand-drawn birds in ink and wash, living in layered paper dioramas with real depth, on a world that is literally a drawing, so that when a place is forgotten you can watch the ink leave it.

## 2. Why it is not Silksong's look
Silksong is painted, gothic, high-contrast, saturated pools of colour in darkness. Ours is **ink-wash on warm paper**: visible paper grain, thin confident line, wet-in-wet washes, limited palette per region, and white as a *presence* rather than an absence. Where Silksong's world is deep and dark, ours is bright, airy, and eroding at the edges. Both are hand-drawn; the pen is different.

## 3. 2.5D, defined
- **Gameplay plane:** a single 2D plane. Wren, enemies, platforms, and interactables live on Z = 0. Physics is 2D.
- **Depth:** the world behind and in front of the plane is built from **layered paper cutouts** and light 3D props, rendered by a perspective camera so parallax is free and true. Foreground layers are blurred and desaturated; far layers are washed toward the paper colour (aerial perspective as "the ink getting thinner").
- **Camera:** perspective, FOV about 30°, 18 units back from the plane, looking straight on with a slight (4°) downward tilt so tops of platforms read. Cinemachine follow with look-ahead; region confiners.
- **Lighting:** URP Forward+ with real lights for lamps, lava, and the Blank's lantern-radius. Sprites use a custom lit shader with a **hand-drawn shadow ramp** (two-step, not smooth) so light reads as ink rather than 3D shading.
- **Fading as a render state:** every material has a `_Ink` value (1 = fully drawn, 0 = blank paper). Fade stages animate it: line weight thins, wash desaturates, paper grain rises, edges soften, then the layer drops out of the parallax stack entirely. This one shader parameter *is* the game's central visual metaphor.

## 4. Characters
- **Line:** a single confident ink line, variable weight (thicker at the bottom of forms). No outlines on far background elements.
- **Wren:** small, round, brown-grey with warm cream breast; ink-blue cowl with a brass compass-rose clasp; the needle-quill is nearly her own height. Read at a glance: dot body, long line.
- **Silhouette families:** Wardens are tall vertical lines (herons, cranes); townsfolk are ovals; Cantors are teardrops with bells; Smudges are scribbles; Remnant are the same shapes with the ink removed (grey outline, no wash, paper showing through).
- **Animation:** hand-drawn frame animation at 12 fps for characters (24 for Wren's attacks and dashes), authored in Aseprite or Krita at 2x target size, imported as sprite sheets. Bosses may use Unity 2D Animation bone rigs for large limbs with hand-drawn overlays for faces and feathers.
  Version one draws Wren from a Blender model posed in script and rendered at 2x with the same Freestyle ink as the paper kits (`docs/design/wren-animation.md`, CHR-02/03); the sheets are the hand-off for the hand-drawn pass.
  The coast's creatures (`enemy-animation.md`, CHR-06) and the returning cast (`npc-animation.md`, CHR-11) follow: townsfolk as one parametric oval with a spec per species, the Remnant grey and a place's fading applied by the shader at run time rather than drawn.
- **Scale:** Wren is 1.2 units tall; a tile is 1 unit; sprite authored at 96 px per unit.

## 5. Environments
- **Modular paper kits per region:** platforms, walls, and props as cut-paper layers with hand-inked edges. Assembled in Unity with ProBuilder greybox first, then swapped for art.
  The pipeline that draws them, tested on the Quay, is `docs/design/paper-kit.md` (ENV-01): cut-out geometry rendered with Freestyle ink in headless Blender, on the ink shader.
- **Paper grain** is a full-screen overlay that also modulates by `_Ink`; unpainted areas show more grain.
- **Region palettes** (max five colours plus ink and paper):

| Region | Paper | Wash 1 | Wash 2 | Accent | Ink |
|---|---|---|---|---|---|
| Saltmarrow | warm cream | silver-grey | olive | rust | blue-black |
| Emberdown | smoke-grey | charcoal | sulphur | ember orange | black |
| Verdance | pale gold | deep green | moss | bone white | sepia |
| Halden | cool cream | slate | verdigris | brass | blue-black |
| Windreach | straw | sky blue | storm violet | gold | grey-brown |
| Greyfold / Blank | white | none | none | Wren's blue and lantern gold | ghost-grey |

- **Lighting per region:** always art-directed time of day; no real-time clock.

## 6. UI
- Everything is paper and ink. The map is Wren's atlas: a book that opens across the screen; unsurveyed areas are blank pages; surveying animates the pen drawing it.
- Health is a row of **masks** drawn as small inked feathers; the Inkwell is an ink bottle that visibly fills.
- Fonts: a hand-cut serif for titles; a clean humanist sans for body. No pixel fonts.

## 7. VFX
- Ink is the VFX language: strikes leave brief ink splashes that soak into the paper; Flourishes are pen scribbles; Bind redraws Wren's outline; erasure (Cantor bells) rubs the image out with a visible eraser texture; the Blank's edge is wet paper.
  Version one draws each as a one-shot sheet clip from geometry in Blender and spawns it through `InkFx` (`docs/design/ink-fx.md`, ENV-12); the strips are the hand-off for the hand-drawn pass.
- Fledglings leap in the background of every region; after each ability Wren learns, they glide a little further. This is a hand-animated background loop, not a system.

## 8. Reference board (for the team, not for tracing)
- *Hollow Knight: Silksong* (readability, silhouette, boss staging).
- *Ori and the Will of the Wisps* (2.5D parallax and camera).
- Chinese and Japanese ink-wash landscape painting (aerial perspective as thinning ink).
- Quentin Blake and Ronald Searle (nervous, confident line).
- *Gris* (colour as narrative state).

## 9. Acceptance test for any new asset
1. Does it read as a single ink drawing at 1/6 screen height?
2. Does it still read when `_Ink` is at 0.4?
3. Does it use only the region's five colours plus ink and paper?
4. Could a player tell which family it belongs to from the silhouette alone?
