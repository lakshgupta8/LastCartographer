# UI Art (ENV-11, v1)

Everything on the screen is paper and ink (art-direction 6): the atlas is a book that opens across the screen, health
is a row of masks drawn as inked feathers, the Inkwell is a bottle that visibly fills, titles are a hand-cut serif and
body text a humanist sans. Version one draws every piece in Blender with the same pen as the characters and the kits,
loads them through Resources, and leaves the greybox look under any piece that is missing, so the hand pass can
replace them one at a time.

## 1. The pipeline

```
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/ui/ui_art.py      (all; or -- UI_Page UI_Mask*)
python tools/ui/pack.py
```

`ui_art.py` renders each piece at 2x into `tools/ui/.frames/` (not committed) with `ui.json` beside them: the size
at 1x, the nine-slice insets of the paper, where the Inkwell's fill runs. `pack.py` downsamples them with
premultiplied alpha into `Assets/_Project/Art/UI/Resources/UI/`, copies the manifest, and lays every piece on
`docs/art/ui-sheet.png`. `UiTextureImporter` imports them crisp (no mipmaps, uncompressed, clamped).

`InkArt` (UI) is the only reader: `Tex(name)` through `Resources.Load`, asked once; `PieceOf(name)` for the manifest;
`Paperize` / `Reskin` for the paper; `Glyph` / `SetGlyph` for a drawing in an element; `CharterIcon`, `InstrumentIcon`
cached per kind so the HUD's refresh allocates nothing (PRG-24). No setup pass is needed: a build, the editor and a
test see the same drawings.

## 2. The pieces

Units in the script are 100 px at 1x; the camera looks along +Y and smaller y is nearer. Objects in the `NoInk`
collection get no Freestyle line (washes, rules, highlights); everything else carries the kits' pen.

| Piece | Size | What | Where |
|---|---|---|---|
| `UI_Page` | 1024×512, sliced 96 | a deckled sheet, an ink rule set in, a fainter one inside it, corner ticks | every panel (`InkTheme.Panel`): dialogue, desk, ledger, shop, options, the journal alone |
| `UI_Strip` | 768×128, sliced 64/24 | a strip torn off the sheet, a short rule at each end | prompts and captions, the journal's toast, the roll-call strip (`InkTheme.Strip`) |
| `UI_Spread` | 1500×900, sliced 120/100 | two pages on a sewn spine, shade where they curl into it | the atlas (`InkTheme.Spread`): the region's map drawn by the pen on the left page (`atlas-map.md`) over a scrolling list that opens at where she stands, the journal on the right |
| `UI_Portrait` | 256×256, sliced 40 | a square sheet with a darker inner square and a rule | the speaker's face on the dialogue page |
| `UI_MaskFull`, `UI_MaskEmpty` | 96×96 | a feather: ink with paper barbs, or paper with ink barbs | the HUD's masks |
| `UI_Inkwell`, `UI_InkwellFill` | 112×144 | the bottle with its cork and highlights; the ink in it to the brim | the HUD's Inkwell: the fill is clipped from the bottom to the pips' share of its run (`fill.bottom`..`fill.top`), the nine pips are marks up the glass |
| `UI_Lantern` | 48×48 | her lantern, lit | beside the Clarity bar, shown with it |
| `UI_BossBar`, `UI_BossFill`, `UI_Tick` | 1024×48, 12×48 | a brush stroke's trough; the ink stroke, dry-brushed at its end; a phase mark | the boss bar: the stroke is clipped to the health left, the ticks sit at the phases |
| `UI_Marker` | 48×48 | a pen nib pointing right | the chosen row on every page (`InkTheme.Marker`) |
| `UI_Seed` | 40×40 | an iris seed pod | the HUD's purse |
| `UI_Rose` | 128×128 | the compass rose, Wren's clasp | before the atlas's title |
| `UI_VantageDrawn/Blank/Erased` | 40×40 | the pen's dot with a flick, a thin ring, a cross over a ghost | each vantage on the atlas (`AtlasView.VantageLine`) |
| `UI_Lamp`, `UI_Desk` | 40×40 | a lit lamp on its post; the drafting desk | the atlas's travel rows |
| `UI_Charter_<six>` | 96×96 | Wren head and shoulders in each Charter's cowl (`wren.py`'s colours and shapes) | the HUD's Charter line, the desk's Charter row |
| `UI_Instrument_<seven>` | 96×96 | the compass-dart, the plumb weight, the sighting lens, the field lantern, the tether hook, the iris tincture, the wax seal | the HUD's slots, the desk's slot rows, the shop |

## 3. The faces

`Art/UI/Resources/UI/Fonts/`: **IM Fell English** (titles: every page's title, the boss's name, a speaker's name, the
roll-call's called name, the death line) and **Alegreya Sans** Regular, Italic and Bold (body). Both are under the SIL
Open Font License, with the licences beside them. `InkTheme.Font` and `TitleFont` serve them; `Text()` uses the real
italic and bold cuts when it has them (nothing is slanted twice), `Title()` / `TitleText()` the serif. Alegreya Sans
covers Latin, Cyrillic and Greek; the CJK locales (PRG-19) fall back as they did with the engine's face.

## 4. High contrast (DES-14)

The drawn paper is the warm palette's. `InkArt.Drawn` is false under high-contrast ink: `UiRoot` calls
`InkArt.Reskin` after `InkTheme.Recolour`, every piece of paper (class `ink-paper`, its kind as a second class) goes
back to the opaque flat page with its firm rule, the atlas's halves meet at a faint rule instead of the spine, and the
glyphs (feathers, bottle, nib, icons) stay, being ink on nothing.

## 5. Verification

- `UiArtTests` (edit mode): every piece `InkArt.Required` names is packed at the size `ui.json` gives it and loads
  through Resources; the paper's slices and the Inkwell's fill are set; the import settings are crisp; the four fonts
  import with their licences and are the theme's faces; every Charter and Instrument has its icon; the sheet and this
  document exist.
- `UiArtPlayTests` (play mode): the HUD's masks are full feathers that empty one by one, the Inkwell is the bottle
  whose fill follows the pips from the brim to dry, the desk is a drawn page with the serif title, the nib marker and
  the Surveyor's cowl on its first row, and high contrast flattens the page and gives it back.
- `UiTests` as before: the pips stay nine children of `hud-ink`, the masks carry `filled`, the slots their `name` and
  `uses`. `SurveyPlayTests` reads the vantage line through `AtlasView.VantageText`.
- `UiScreenshotTests` and the marketing captures pick the drawings up as they are.

## 6. Open

- The atlas's map is drawn (`atlas-map.md`); lettering its region headings is the hand pass's.
- The atlas's status line still says ☼ and ▣ in text for the lamps and desks it lists; only the travel rows draw them.
- The paper stretches between its slices; a very tall page stretches the deckle's wobble. A tiled middle
  (`-unity-slice-type: tiled`) would keep it, once the project's UI Toolkit is confirmed to carry it.
- The roll-call strip's three miss marks are still diamonds, and the options page has no drawings but the marker.
- The atlas's place list scrolls behind the engine's default grey scroller; a drawn scroller (a ribbon bookmark) is the hand pass's.
- The Charter and Instrument icons are glyphs, not the 3D rigs' renders; a hand pass may want them from `wren.py`'s
  own cowls.
