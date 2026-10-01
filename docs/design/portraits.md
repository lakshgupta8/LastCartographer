# Portraits for Dialogue (CHR-13, v1)

Every bird who speaks has a face beside their lines. A portrait is not a new drawing. It is the body the speaker is met
in, framed on the head from three-quarters in front and turned toward the line. That body is the cast's own sheets,
the Warden rig, or the look a minor named bird wears. Things that speak, places that speak, the Lantern's narration
and Marrow have no portrait. The page shows the line alone.

Drawings: `tools/characters/portraits.py` (Blender) renders each speaker at rest and mid-word.
`tools/characters/portraits_pack.py` packs them to `Art/Portraits/Portrait_<Speaker>.png` with `portraits.json`.
The contact sheet is `docs/art/portraits.png`. The page as it plays, Sable drawn and Corvin a Remnant, is
`docs/art/portraits-page.png`.
Data: `Portraits` (Core) holds who has a face and what body it is drawn from, who speaks faceless, and who is a
Remnant at rest.
Page: `DialogueView` (UI) holds the strips and is wired by `ProjectSetup.LoadPortraits`.
Tests: `PortraitTests` (edit, 5), `DialoguePortraitTests` (play, 4).

## 1. Who has a face

| Speakers | Drawn from |
|---|---|
| Sable, Dotha, Isolde, Pell, Runa, Kettil, Teodor, Idrenne, Maren, Corvin, Ilse, Corra, Aury | their own bird in `cast.py` |
| Halvard, Brann, Oriel, the Warden at the Bastion | the Warden rig in `wardens.py` (the generic one is CHR-06's) |
| Hale, Voss | the Warden rig as `bosses.py` dresses them |
| Hask, Ostry, Wend, Tobin, Ansel, Hollin, Arden, Brisk, Anvers, Tam, the Keeper, the Innkeeper | the look each wears (`Townsfolk.Named`) |
| the Gannet, the Traveller, Brek | the asker's look (`Offerings.Asker.Look`, the Crane for Brek) |
| Ossa, Lorne, Brask | the species their arc gives them, from the library (Plover, Crane, Chough); none of them is stood up in a room yet |

**Faceless** (`Portraits.Faceless`) covers:
- the things and places that speak (the ashes, a door, the pages, the milestones, the Mine, the Hollow);
- the Lantern, which narrates;
- the islands' unnamed `Remnant` speakers, whose bodies wait on a Resources pass (CHR-12);
- Marrow, who has no portrait and no name plate until named (character-bibles.md §5).

Every Yarn speaker must be on one list or the other, never both, and never neither. A new speaker is a decision
someone makes, and `PortraitTests` holds the lists to the project.

Some looks are shared. Ostry and Anvers are both rooks, Hask and Brask choughs, Brek and Lorne cranes. Sable and Aury
are the same bird, since he is "her brother, the same bird in a keeper's coat", so their portraits read alike. In
play his is grey.

## 2. The drawing

`portraits.py` builds each speaker's rig as its own script does, at rest. It frames the head mesh's world bounds:
- **Span:** 2.7 heads wide.
- **Height:** centred a fifth of a head below the head, which takes in the beak, the neck and the top of the shoulders.
- **Across:** a little left of centre, so the beak has room toward the line.

The camera turns 48° toward the face. The Freestyle line is 3.0 px at 512 and packs at 256, so the weight matches
the sheets at the portrait's size.

There are two poses:
- `rest`.
- `talk`: the jaw open 18° and the head lifted 5°. The Warden rig has one bill, so the portrait script gives it a
  lower half, hinged at the base, to open.

`portraits_pack.py` downsamples with premultiplied alpha, as `pack.py` does. It then makes the Remnant's two frames:
InkSprite's `ColourState` at `_Wash` 1 and `_LineFade` 1, worked on the pixels in linear light. A Remnant's portrait
is therefore the drawing the player sees standing in the room: the same shapes with the ink removed.

A strip is four 256-px frames: `rest`, `talk`, `rest_remnant`, `talk_remnant`. The importer treats `Art/Portraits/`
like the character sheets: no mipmaps, bilinear, uncompressed.

## 3. On the page

`DialogueView` puts a 168-px square of darker paper, framed in faint ink, to the left of the speaker's name and line.
- **Shown:** while a line is spoken by someone with a face.
- **Hidden:** for a faceless speaker, and for Wren's choices (she has no portrait of her own).
- **Beak:** while the line is new, the beak opens and shuts at 8 frames a second. A line is new for 0.04 s per
  letter, at least 0.5 s and at most 3.5 s. Then the portrait rests on its `rest` frame.
- **Grey:** the grey frames lie over the drawn ones at an opacity, the speaker's wash (`DialogueView.WashOf`). The
  wash comes from the first of these that applies:
  1. The bird in the room: the talker if it is the speaker, otherwise any `NpcInk` on an object of the speaker's
     name (`Sable`, `Sable_Greybox`, `Npc_Sable`). Its wash is 0 when drawn, up to 0.7 while its place fades, and
     1 for a Remnant.
  2. With no bird in the room, the speaker's rest state: 1 for those in `Portraits.RemnantAtRest` (Ilse, Corra,
     Aury, Corvin, the Innkeeper, the Gannet, the Traveller, Brask), otherwise 0.

The overlay is a blend of two drawings, not the shader. The fills match the shader; while a person fades, the line
greys a little faster than in the room, about 0.6 against 0.35 of the way at full wash. A shader on the
UI element would match exactly; that is for the hand pass if it matters.

## 4. Redrawing

- **One portrait:** replace `Portrait_<Speaker>.png` with a strip of the same four 256-px frames. A hand-drawn
  portrait can have any number of expressions later; the page only asks for rest and talk.
- **Re-render everything:** run `blender -b -P tools/characters/portraits.py`, then
  `python tools/characters/portraits_pack.py`. The 37 render in about 40 s.
- **Re-wire the page:** rebuild with **OWSBG → Build Bootstrap Scene**.

## 5. Verification

| Check | Where |
|---|---|
| Every Yarn speaker has a face or is faceless, never both, never neither; Marrow has none; every Remnant at rest has a face | `PortraitTests.EverySpeakerHasAFaceOrIsFacelessAndNeverBoth` |
| A face is drawn from the body the speaker is met in: the named birds' looks, the cast's own sheets | `PortraitTests.EachFaceIsDrawnFromTheBodyTheSpeakerIsMetIn` |
| The pack and the table list the same speakers; four 256-px frames each; import settings | `PortraitTests.EveryFaceIsPackedAsAStripOfFourFrames` |
| The beak opens; the grey is the same outline with less colour, nearer the paper | `PortraitTests.TheBeakOpensAndTheRemnantIsTheSameDrawingGreyed` |
| The persistent page carries every strip; the contact sheet exists | `PortraitTests.ThePersistentPageCarriesEveryPortrait` |
| Sable's line shows her face, talking and then resting | `DialoguePortraitTests.ASpeakerWithAFaceShowsItTalkingThenResting` |
| The ashes show no face; Wren's choices show none | `DialoguePortraitTests.AThingThatSpeaksShowsNoFace` |
| Corvin, with no Corvin in the room, speaks in grey | `DialoguePortraitTests.ARemnantSpeaksInGrey` |
| A Sable in the room, fading and then a Remnant, sets her portrait's grey | `DialoguePortraitTests.TheBirdInTheRoomDecidesHowGrey` |
