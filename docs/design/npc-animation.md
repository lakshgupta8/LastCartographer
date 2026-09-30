# The Cast Drawn (CHR-11, v1)

The returning cast built and animated the way Wren and the coast's creatures are (`wren-animation.md`,
`enemy-animation.md`): one parametric townsfolk bird in Blender, a spec per species, clips as functions of time,
the paper kits' Freestyle ink, sheets packed at 96 px/unit. Model sheets are `docs/art/<name>-turnaround.png`.
Colour states (a person fading with their place, the Remnant grey) are the shader's, applied at run time, so every
member is rendered once in their own colours.

## 1. The pipeline

```
tools/characters/cast.py                     (Blender, headless; inklib.py shared with wren.py and the enemies)
python tools/characters/pack.py sable dotha isolde pell runa kettil teodor idrenne maren corvin ilse corra marrow aury
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.ProjectSetup.BuildBootstrapScene -quit
```

`MakeNpc` dresses a townsfolk with its sheets when `Art/Characters/<Character>/<character>.json` exists, the
character being the object's name without its `_Greybox` suffix (Sable_Greybox → Sable): the quad becomes a
cell-sized frame window with the feet at the transform, the stand-in tint stands down to white, and the object
gets `InkSheetPlayer`, `NpcAnimator` and `NpcInk`. Without sheets the tinted block stays (Halvard, a Warden:
CHR-07). Every drawing faces right; `NpcSchedule` and `NpcTalker` flip the root to face left.

## 2. The bird

Townsfolk are ovals (art-direction 4). `Townsfolk(spec, extras)` is a body on two legs, a neck, a head with a
beak whose lower half opens to talk, an eye that blinks, two wings and a tail; the spec sets every size and colour,
and `extras` adds what names the species and the person. Props a clip needs (a net, a ledger, a page) are hidden
at rest and shown by the clip (`Rig.prop`, `Rig.show`).

| Character | Species (bible) | Cell | What names them |
|---|---|---|---|
| Sable | cormorant | 2.0 | black gone brown at the edges, sits low; the oilskin; the net and the ledger as props |
| Aury | cormorant | 2.0 | Sable's bird in a keeper's coat, a lamp at his feet |
| Dotha | oystercatcher (v1's choice; the bible says "last elder") | 2.0 | black over white, the long red bill, the shawl, a stoop |
| Isolde Marr | curlew (v1's choice; the bible says "Wren's mentor") | 2.2 | streaked brown, the down-curved bill, a Guild cowl in faded blue with the brass clasp, a satchel of charts |
| Pell | jackdaw | 2.0 | black, the grey nape, the pale eye; a satchel and three rolled charts (three more things than hands) |
| Runa | capercaillie | 2.0 | slate-dark, green breast, the red comb, the fan tail; a chalk bag |
| Old Kettil | capercaillie | 2.0 | Runa's bird gone grey and stooped, a stick |
| Brother Teodor Ashe | mourning dove | 2.0 | soft grey-brown, small head, pointed tail; the hood |
| Speaker Idrenne | crane | 2.6 | tall: the long neck and legs, the red crown, a wind-cloak |
| Queen-Regent Maren Ostrell | swan | 2.6 | white, the S of the neck, the orange bill with its knob, a thin circlet |
| Corvin Halloway | great owl | 2.0 | the broad oval, ear tufts, the big eyes, spectacles; a stone in his wing |
| Ilse | wren | 1.6 | Wren's palette without the cowl, a shawl; her daughter's crest |
| Corra | heron chick | 1.4 | grey-blue fluff, the spiky crest, big feet; a page and charcoal |
| Marrow | grey chick, unreadable | 1.2 | greys, a stub of a beak, down that names no species; the line half gone (`GREY_INK`) |

Blender writes sRGB, so a wash given as 0.05 prints near 0.25: the blacks are set dark for it.

## 3. The clips

Every member has the four the hub asks for, at 12 fps, all looping (a person's state holds while it holds):

| Clip | Frames | What |
|---|---|---|
| idle | 8 | breath, a head turn, a blink on the last frame |
| talk | 6 | the beak opening and closing, the head nodding, a wing gesture |
| walk | 8 | the legs alternating, a bob, a lean |
| asleep | 4 | the head tucked back, the eye shut, the body low |

And their own: **Sable** `mending` (sat low over the net, the near wing pulling) and `reading` (the ledger up in
her wing, head down); **Dotha** `singing` (head up, beak wide, a sway); **Corra** `drawing` (crouched over the
page, the charcoal moving).

`NpcAnimator` picks each frame, top first: **talk** while `NpcTalker.Talking` is this talker (set on the press,
cleared when the dialogue completes); **walk** while the schedule is between posts, or whenever the transform moves
faster than 0.3 units/s (a cutscene's walk into the white: Isolde); the post's **activity** when the sheets have a
clip named by its first word (`"mending nets"` → mending, `"asleep under the stilts"` → asleep, `"reading the
ledger"` → reading); else **idle**. A clip the sheets lack falls through, so Dotha "on her stoop" idles and every
night post sleeps.

## 4. Colour states (`NpcInk`)

People fade by their own rules (`fade-stages.md` §: characters are not in the room's fade group). The shader gained
two properties: `_Wash` moves the fills toward paper and leaves the line (the line is what is darker than a wash),
`_LineFade` moves the line toward grey.

| State | When | Wash | Line |
|---|---|---|---|
| **Drawn** | the place stands, stage 0; or Held (life goes on) | 0 | 0 |
| **Fading** | the place's fade stage is above 0 (anchored places hold at their stage's colour) | 0.7 × stage / 4, never 1: a fading person keeps a little colour while the place stands | 0 |
| **Remnant** | the place is Released; or the room is an island in the Blank (`Island_*`); or the setup says so (Ilse, Corra, Aury, Corvin, Marrow: the Remnant faction) | 1 | 1 (≈ the `GREY_INK` of the drawn Remnant enemies) |

`NpcInk.Resolve` and `NpcInk.Amounts` are pure and tested; the component reads `Places.FateOf` and
`FadeStages.Get` for its room (or a set `PlaceId`) and writes the two floats through the renderer's property
block, so the sheet player's frame window is untouched.

## 5. Reworking by hand

Replace a `<Character>_<clip>.png` strip at the same frame count and cell, or change the count in the json, and
rebuild. A new pose is a clip function in `cast.py`; a new member is a spec dict, an extras function and a line in
`CAST`; the setup finds the sheets by the NPC's name. The species chosen here for Isolde and Dotha are v1's and
are one line each to change.

## 6. Verification

- `CastSheetTests` (edit mode): fourteen manifests at 96 px/unit with idle, talk, walk, asleep and each one's own
  clips, strips at frame count × cell, 12 fps, looping, a model sheet each; the sizes (the crane and the swan tall,
  the chicks small, Ilse a wren, Aury Sable's size); every drawn member in the cast data; Sable's, Dotha's and
  Isolde's materials on their idle strips with a white tint and Halvard's still tinted; the Quay, Merrow's End and
  the Edge carry the animator, Merrow's End the colour state; the shader's two properties; the state rules.
- `NpcAnimatorTests` (play mode): a scheduled bird at its day post shows `mending`, `talk` while named the talker,
  `walk` between posts, idle at a post without a clip; and its colour: drawn, then washed by stage two, then the
  ink removed when the place is released.

## 7. Open

- Only Sable, Dotha and Isolde stand in built rooms; the other eleven have sheets and no room yet (Halden,
  Emberdown, Windreach, the Blank's islands are unbuilt). Their idles are what the room builders will place.
- Halvard is the tinted stand-in until CHR-07 draws the Warden family.
- Sable's `reading` at dusk and `asleep` at night, Dotha's `singing`, are on the schedule's activity strings; a
  post renamed loses its clip silently (it idles).
- The talk clip runs for the whole conversation, including while Wren's own lines print; a per-line speaker from
  Yarn would let it rest between her lines.
- A very dark wash (Sable's black) reads as line to the shader, so it lightens less under Fading than a pale
  bird's fills do; the Remnant state greys everything alike.
- Sable's and Aury's `talk` opens a hooked bill; the hand-drawn pass should decide whether cormorants talk with
  the throat instead.
