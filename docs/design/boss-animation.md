# The Late Bosses Drawn (CHR-08, CHR-09, CHR-10, v1)

The twelve boss kits of `boss-kits.md` fought as placeholder blocks in their built rooms. Now every one of them
is drawn: the bosses' bodies on rigs in Blender like the coast's creatures (`enemy-animation.md`), and the pieces a
fight makes at run time (doves, rubble, feathers, stones, seals, ropes, the fist, the quill hand, the pools) as
sheets of their own that the boss carries into the room. With them come the two families the rows name: the
Smudges of every region and the Cantors beyond the forest.

## 1. The pipeline

```
tools/characters/bosses.py          the nine bodies (Hale and Voss on the Warden rig; the rest their own)
tools/characters/boss_parts.py      the twelve parts (no turnarounds: they are not characters)
tools/characters/families.py        four Smudges, the crow Cantor, the Choir's dove
python tools/characters/pack.py collapse gatekeeper hale voss fallenstar corrasdrawing archivist completesurvey halfcathedralbells \
    rubble surge choruslamp feather stone stonestrike starfist vossseal bellrope crayonsmall quillhand inkpool \
    smudge_ember smudge_leaf smudge_chalk memorysmudge cantor_crow choirdove
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.ProjectSetup.BuildBootstrapScene -quit
```

`MakeBoss` dresses a boss from `Art/Characters/<Type>/` as before (the body on a cell-sized quad centred on its
collider, `EnemyAnimator` asking `Boss.Clip` each frame). New: it also asks the boss which **part skins** it can
wear (`Boss.PartSkinNames`) and loads each one that exists into the boss's serialized `PartSkin` list (the name,
the cell, the clips). At run time, when the kit makes a piece, `Boss.Skin(part, name, clip)` dresses the
`BossPart` (`BossPart.Dress`: the block becomes an InkSprite quad with an `InkSheetPlayer`) and
`Boss.SkinProp(prop, name, clip)` does the same for a plain prop (a lamp, a stone). A kit with no skin keeps its
blocks, so the runtime arenas of the tests (`BossKits.Build`) look as they did. `BossPart.Show(prop, clip,
material)` and `BossPart.SetMaterial` keep the old material swaps as the fallback: a dressed piece plays a clip
instead.

Two kits are **bodiless** (`Boss.Bodiless`): the Choir is the song, and its doves are the drawing; the sprite the
setup gives it stays hidden. The Half-Cathedral Bells and the Complete Survey have bodies after all: the beam
over the nave with its four bells, and the Atlas itself open on the floor, its ink rising.

## 2. The bodies

| Boss | Drawing | Cell | Clips | What names them |
|---|---|---|---|---|
| The Collapse | the mine collapse as a smudge-beast: a mass of ink strokes with rubble in it, lamp-light for eyes, and an arm of ink drawn only in the reach | 5.0 | idle, rumble, shake, surge, reach, hurt, death | `Collapse.Clip`: the rubble's telegraph is `rumble` (the dust), the drop is `shake`, the surge's telegraph and run are `surge`; `reach` is sought by the window's progress, so the arm is at the lamp as it closes. Its sprite is still hidden while undrawn |
| The Gatekeeper | a flying-age statue of an eagle, roots for wings (branching cones in root-wood, moss on them), a hooked beak, talons | 5.0 | idle, perch, fly, rise, telegraph, sweep, shake, pass, land, recover, hurt, death | `Gatekeeper.Clip`: `idle` on the plinth, `perch` hanging from the roots once aloft, `fly` (badly) once the roots are torn; `rise` to the top of the gate; the feathers' telegraph is `shake`; `pass` across the gate; `land` heavy. `death` lies it down with its wings open |
| Surveyor Hale | the Warden rig as a godwit in a road-coat (windreach-arc.md; a bittern until 2026-10-02): a shorter neck, the long bill turned up, pink to an ink tip, no gorget, a quill for a lance, a lens on a strap, a satchel of pages | 2.8 | idle, move, sight, call, count, telegraph, quill, recover, hurt, death | `Hale.Clip`: `sight` raises the lens to his eye through the sighting (sought by its progress); `call` and `count` are Halvard's; the quill is a Warden's thrust |
| Guildmaster Voss | the Warden rig at 1.15, a great grey heron in full brass: plates, a long crest, the compass-rose shield on the far wing | 3.2 | idle, move, telegraph, thrust, lunge, guard, anchor, recover, hurt, death | `Voss.Clip`: `guard` brings the shield forward and walks behind it (looping); `anchor` plants the lance (the rose is drawn on the section) with the shield half up. `death` is a withdrawal: he lowers the lance and the shield and steps aside. Go |
| The Fallen Star | an iron golem: a lumpy core with the gold seam on top, two fists, two stub legs, ember cracks drawn only when it burns | 4.5 | idle, walk, telegraph, slam, raise, recover, burn, hurt, death | `FallenStar.Clip`: `telegraph` raises the near fist high; `slam` brings it down; `raise` lifts both arms for the walls; `burn` is its idle in phase 3 with the cracks showing |
| Corra's Drawing | a child's crayon drawing of her father: a round head with a long beak and three crest strokes, a blue coat, a yellow gorget, stick arms and legs, a crayon lance; crayon-grey line, thicker. Every clip twice: in crayon, and `_outline` with the washes gone to paper | 5.0 | idle, move, telegraph, swipe, lift, stomp, recover, hurt, death, and each `_outline` | `CorrasDrawing.Clip`: the stomp's telegraph is `lift` (the foot raised over the mark); the drawing wobbles in every frame (a child's drawing never holds still); once the crayon runs out every name gets `_outline`. Its sprite is still hidden while a limb is redrawn |
| The Archivist | an enormous half-drawn owl: the facial disc, gold eyes, ear tufts, a quill in the near wing, the keystone in the far talon; grey ink, washes toward paper | 4.0 | idle, telegraph, draw, swoop, recover, hold, hurt, death | `Archivist.Clip`: `telegraph` lifts the quill; `draw` scribbles (looping while his quill is on the page); `swoop` low with the wings flat; `hold` with the wings tight and the eyes nearly shut |
| The Complete Survey | the Great Atlas open on the Observatory floor, lines on its pages, a plume of ink rising from the gutter | 4.0 | idle, hurt, death | the base rule: `hurt` slams the pages; `death` closes the book |
| The Half-Cathedral Bells | the beam over the nave and four bells in faded brass, the great one larger; faded ink | 5.0 | idle, hurt, death | the base rule; the ropes below are parts |
| Reedmother's Brood | a giant reed-nest: a woven bowl with a fringe of reeds leaning out round the rim, a dark mouth at the top with three eggs and two chicks' heads in it (a prop), the Guild's fire on its east side (a prop) | 5.0 | idle, telegraph, thresh, call, open, burn, hurt, death, calm | `ReedmotherBrood.Clip`: `telegraph` draws the reeds in, `thresh` (24 fps) lashes them flat both sides; `call` opens the mouth over three frames, the heads on the last; `open` holds it open while the clutch is out; `burn` is its idle with the fire; `death` is the fire taking the nest; `calm` is the fire out and the reeds settling. The chicks that hop out are copies of a room's dormant reedling (CMB-09, enemy-animation.md §2f) |

Model sheets are `docs/art/<name>-turnaround.png` for all nine.

**Voss and Hale stand in rooms as NPCs too** (the Threshold's speech, the Observatory's statue, the surveyor at
dusk), and `MakeNpc` dresses an NPC from the sheets of its name, so the same drawings serve both, as Halvard's do:
their manifests carry `feetUnits` (−0.82, and −0.94 for Voss at 1.15) so the NPC stands on the floor while the
boss is drawn on its collider's centre. `NpcAnimator` asks them for idle, talk and walk; they have idle and move,
and the rest falls back to idle.

## 3. The pieces

| Part | Owner | Cell | Clips | How the fight plays them |
|---|---|---|---|---|
| ChorusLamp | the Collapse's four lamps | 1.0 | dark, lit | `Show(lamp, lit ? "lit" : "dark")` on every beat |
| Rubble | its blocks | 2.0 | idle | dressed as each lands |
| Surge | its ink along the floor | 2.0 | idle, move | drawn going east; flipped when it comes from the east |
| Feather | the Gatekeeper's stone feathers | 1.2 | idle | dressed as each drops |
| Stone | Hale's nine stones | 1.6 | bare, hale, wren | the owner's clip (the brass sighting mark; her ink scribble) |
| StoneStrike | the count's columns | 3.5 | erupt | once, rising |
| StarFist | the Fallen Star's fist on the mark | 2.0 | idle | dressed as it lands |
| VossSeal | Voss's seals | 5.5 | idle | the rose turns slowly |
| BellRope | the four ropes | 6.5 | idle, ring | `ring` sought by the bell's progress; `idle` when it tolls or is quiet |
| CrayonSmall | the small Voss | 1.6 | idle | the same drawing at 0.3 |
| QuillHand | the Archivist's hand | 1.2 | idle, draw | `draw` while his quill is on the page |
| WrenDrawing | her drawing | Wren's | Wren's own | the Archivist carries **Wren's** sheets under this name; her drawing stands with its feet on the floor, washed 0.5 and its line faded 0.6, and plays `idle`, `run` and `strike1` as it hunts her |
| InkPool | the Survey's pools | 1.6 | idle | drops rise and fall |
| CordLance | Halvard's second lance in flight (II, III) | 2.0 | fly | the cord ripples behind it; flipped when it comes back |
| FurnaceGrate | one of the cold furnace's six floor sections (6.5) | 3.0 | idle, warming, hot, dark | stood 0.35 under the floor and in front of its face, the plate on the floor line, the firebox below; `idle` cool iron with a fleck of ash lifting, `warming` the slots glowing and pulsing, `hot` the slots white and five flames licking up past her feet, `dark` sooted over for phase 3; Brann plays each section's state (`Brann.SectionClip`), and keeps the flat quads when the sheet is missing |
| BridgeSpan | a span of the Seven Bridges (II) | 3.0 | idle, fall | stood a unit under the floor, its deck on the floor line, grit trickling from under the rib at rest; `fall` when the count cuts it: it cracks, tips off its rib and drops out of the cell |
| ChoirDove | the Choir's three | 2.4 | idle, ring, hurt, death | `ring` restarted when a dove's bell starts and sought by its progress; `idle` when it tolls or is stopped |

## 4. The families

**Smudges (CHR-09).** A smudge is an ink-beast formed from something forgotten (bible 6), so each region's
carries the shape of what it forgot inside its scribble, on the coast's Smudge rig: the coast's keeps the boat's
ribs (`Smudge`); the mine's is ash-black with ember flecks and a miner's lamp (`Smudge_Ember`); the forest's is
green-black with leaves and a child's swing (`Smudge_Leaf`); the plateau's, the steppe's and the white's is chalk,
nearly gone, with a feather and a stone (`Smudge_Chalk`, grey ink); and the smudge of Wren's own death is
blue-black with her cowl and quill faint inside it (`MemorySmudge`). `SmudgeLook(roomId)` in the setup picks the
look by region for every recipe room; `MemoryDrops` carries the memory smudge's sheets from the persistent scene
and spawns it drawn (`ConfigureSheets`), with `EnemyAnimator` on it like any smudge. Same clips as the Smudge.

**Cantors (CHR-08).** Doves on the coast and in the forest (`Cantor`); crows with cracked bells in the mine, on the
plateau, the steppe and beyond (`Cantor_Crow`, `CantorLook(roomId)`); and the Choir's three doves in white wool
with a scarf and no flask (`ChoirDove`). Same clips as the Cantor; the Choir dove has no `move` or `recover`.

## 5. Reworking by hand

As for every character: replace a `<Name>_<clip>.png` strip at the same frame count and cell, or change the count
in the json, and rebuild. A body's poses are the clip functions in `bosses.py`; a part's in `boss_parts.py`; a
look's extras are a `Rig` subclass's `__init__` in `families.py`. A new part is a class, a clip list and a line in
`PARTS`, plus its name in the owner's `PartSkinNames` and a `Skin(...)` call where the kit makes it.

## 6. Verification

- `BossDrawingsTests` (edit mode, 5 tests): every body's manifest at 96 px/unit with every clip its `Clip`
  names, strips at frame count × cell, drawn rates, death and hurt once, model sheets; Corra's Drawing's outline
  clips match its crayon ones frame for frame; the parts' manifests and the ring's six frames; the families on
  their rigs' cells; each built boss room rests its boss on its idle strip, carries its parts' sheets (the
  Archivist carries Wren's), and runs the animator; the regions' smudges and Cantors wear their looks and the
  persistent scene carries the memory smudge's.
- `BossClipPlayTests` (play mode, 6 tests): Voss, the Gatekeeper, Hale, the Fallen Star and Corra's Drawing name
  their clips for their moves (the outline suffix once the crayon runs out); a kit carrying a skin dresses the
  pieces it makes (the Gatekeeper's feathers, Hale's stones changing hands, the Archivist's hand drawing and her
  drawing on its feet, the Choir's dove ringing through its frames); the memory smudge spawns drawn.

## 7. Open

- **The captures show the bosses at rest.** Their fights' parts appear only in play; the tests see them, the
  screenshots don't.
- **Brann's brass, the Star's burning and the Collapse's lit lamp are lights since ENV-10** (`Boss.Glow`,
  lighting.md §3) as well as drawn states; the other fights' glows (the Gatekeeper's roots, Corra's crayon) are
  still only drawn.
- **The Collapse's beast is one drawing moved from section to section.** The bible's "as wide as the floor" would
  be four drawings or one four sections wide; v1 draws it in the lit section only, as the rule says.
- **Her drawing uses three of Wren's clips.** Oriel's mirror reads the Charter; the Archivist's drawing of her
  does not yet, so it has no combo to show.
- **The runtime arenas (`BossKits.Build`) carry no skins:** the fight tests and any island or epilogue stand-in
  fight in blocks. The built rooms are the game.
- **Hale's stones and the Collapse's lamps swap materials when undressed** and clips when dressed; the two paths
  are both kept, which is one more than needed once every kit is drawn.
