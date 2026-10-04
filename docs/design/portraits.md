# Portraits for Dialogue (CHR-13, v1)

Every bird who speaks has a face beside their lines. A portrait is not a new drawing. It is the body the speaker is met
in, framed on the head from three-quarters in front and turned toward the line. That body is the cast's own sheets,
the Warden rig, or the look a minor named bird wears. Things that speak, places that speak, the Lantern's narration
and Marrow have no portrait. The page shows the line alone.

Drawings: `tools/characters/portraits.py` (Blender) renders each speaker in five moods, each at rest and mid-word.
`tools/characters/portraits_pack.py` packs them to `Art/Portraits/Portrait_<Speaker>.png` with `portraits.json`.
The contact sheet is `docs/art/portraits.png`, and every speaker's five moods side by side are
`docs/art/portrait-moods.png`. The page as it plays, Sable drawn and Corvin a Remnant, is
`docs/art/portraits-page.png`.
Data: `Portraits` (Core) holds who has a face and what body it is drawn from, who speaks faceless, who is a
Remnant at rest, the moods, and which mood a line is said with (`MoodOf`).
Page: `DialogueView` (UI) holds the strips and is wired by `ProjectSetup.LoadPortraits`.
Tests: `PortraitTests` (edit, 9), `DialoguePortraitTests` (play, 5).

## 1. Who has a face

| Speakers | Drawn from |
|---|---|
| Sable, Dotha, Isolde, Pell, Runa, Kettil, Teodor, Idrenne, Maren, Corvin, Ilse, Corra, Aury | their own bird in `cast.py` |
| Halvard, Brann, Oriel, the Warden at the Bastion | the Warden rig in `wardens.py` (the generic one is CHR-06's) |
| Hale, Voss | the Warden rig as `bosses.py` dresses them |
| Hask, Tobin, Ansel, Arden, Brisk, Tam, the Keeper, the Innkeeper | the look each wears (`Townsfolk.Named`) |
| Ostry, Anvers, Hollin, Wend | their own drawings, the nightjar, the heron, the thrush and the dove their arcs name (`Townsfolk.OwnDrawn`, townsfolk.md §3) |
| the Gannet, the Traveller, Brek | the asker's look (`Offerings.Asker.Look`, the Crane for Brek) |
| Ossa, Lorne, Garrow, Brask | the species their arc gives them, from the library (Plover, Crane, Crane, Chough); none of them is stood up in a room yet |

**Faceless** (`Portraits.Faceless`) covers:
- the things and places that speak (the ashes, a door, the pages, the milestones, the Mine, the Hollow);
- the Lantern, which narrates;
- the islands' unnamed `Remnant` speakers, whose bodies wait on a Resources pass (CHR-12);
- Marrow, who has no portrait and no name plate until named (character-bibles.md §5).

Every Yarn speaker must be on one list or the other, never both, and never neither. A new speaker is a decision
someone makes, and `PortraitTests` holds the lists to the project.

Two looks are shared: Hask and Brask are choughs; Brek, Lorne and Garrow are cranes. In each pair one speaker has a touch of
their own in the portrait (`TOUCHES` in `portraits.py`, decided 2026-10-02), taken from their arc:
- **Brask**, a Hollowvein miner buried with his shift, has a leather helmet with its lamp.
- **Lorne**, the Guild's careful surveyor, is a grown crane (grey, the red crown) where Brek is the young tawny one,
  and wears spectacles on a brass wire.
- **Garrow**, who failed the leap forty-one years ago, is the crane grown old: ash-pale, stooped, the crown faded,
  the eye milky. He is not Brek. The story had one Brek at both the third fire (old) and the Gate (a fledgling), so
  the old one was renamed (decided 2026-10-02).

None of Brask, Lorne and Garrow stands in a room yet, so the touches are only in the portraits for now. Ostry, Anvers,
Hollin and Wend first shared looks too (two rooks, a jay, a finch). They are now drawn as the species their arcs
give them, so their portraits and their rooms show the same bird. Sable and Aury are the same bird, since he is "her
brother, the same bird in a keeper's coat"; his coat still sets the two portraits apart, and in play his is grey.

## 2. The drawing

`portraits.py` builds each speaker's rig as its own script does, at rest. It frames the head mesh's world bounds:
- **Span:** 2.7 heads wide.
- **Height:** centred a fifth of a head below the head, which takes in the beak, the neck and the top of the shoulders.
- **Across:** a little left of centre, so the beak has room toward the line.

The camera turns 48° toward the face. The Freestyle line is 3.0 px at 512 and packs at 256, so the weight matches
the sheets at the portrait's size.

Every mood has two poses: `rest`, and `talk`, the jaw open and the head lifted 5°. The Warden rig has one bill, so
the portrait script gives it a lower half, hinged at the base, to open.

### The moods

A mood is what a bird's head, beak and eye can say without a new drawing (`MOODS` in `portraits.py`):

| Mood | The face | Jaw at rest / mid-word |
|---|---|---|
| `plain` | as the bird stands | 0° / 18° |
| `bright` | the head up 14°, the neck lifted, the eye wide (×1.28) | 6° / 24° |
| `grave` | the head down 12°, the neck forward, the lid half down and drooping away from the beak | 0° / 12° |
| `wary` | the head drawn back and the chin down, the lid a third down and dropping toward the beak, a frown | 0° / 10° |
| `asking` | the head tilted 22° and turned toward the reader, the eye wide (×1.18) | 3° / 16° |

The lid is a cap of the head's own colour over the eye, cut straight or slanted, parented to the eye so it takes the
eye's size and shape on every rig (the cast's `eye_m`, the Warden's `eye`, a boss's `eye0`). Under a lid the glint
is hidden; it sits proud of the eye and would show through. The frame does not move with the mood, so a bird that
lifts its head lifts it in the frame. On a dark bird (Sable, the choughs) the lid is quieter than on a pale one; the
head's pitch carries those.

`portraits_pack.py` downsamples with premultiplied alpha, as `pack.py` does. It then makes each mood's two Remnant frames:
InkSprite's `ColourState` at `_Wash` 1 and `_LineFade` 1, worked on the pixels in linear light. A Remnant's portrait
is therefore the drawing the player sees standing in the room: the same shapes with the ink removed.

A sheet is a row per mood, top to bottom `plain`, `bright`, `grave`, `wary`, `asking`. Each row is four 256-px frames:
`rest`, `talk`, `rest_remnant`, `talk_remnant`. That makes 1024 × 1280 px. The importer treats `Art/Portraits/` like
the character sheets: no mipmaps, bilinear, uncompressed. That is 5 MB a speaker and about 215 MB for the 41, all
carried by the persistent page. It is the same open question as the sheets' (performance.md: BC7 at the hand pass).

## 3. On the page

`DialogueView` puts a 168-px square of darker paper, framed in faint ink, to the left of the speaker's name and line.
- **Shown:** while a line is spoken by someone with a face.
- **Hidden:** for a faceless speaker, and for Wren's choices (she has no portrait of her own).
- **Mood:** the line's mood picks the sheet's row (`Portraits.MoodOf`, below).
- **Beak:** while the line is new, the beak opens and shuts at 8 frames a second, inside the mood. A line is new for 0.04 s per
  letter, at least 0.5 s and at most 3.5 s. Then the portrait rests on its `rest` frame.
- **Grey:** the grey frames lie over the drawn ones at an opacity, the speaker's wash (`DialogueView.WashOf`). The
  wash comes from the first of these that applies:
  1. The bird in the room: the talker if it is the speaker, otherwise any `NpcInk` on an object of the speaker's
     name (`Sable`, `Sable_Greybox`, `Npc_Sable`). Its wash is 0 when drawn, up to 0.7 while its place fades, and
     1 for a Remnant.
  2. With no bird in the room, the speaker's rest state: 1 for those in `Portraits.RemnantAtRest` (Ilse, Corra,
     Aury, Corvin, the Innkeeper, the Gannet, the Traveller, Brask), otherwise 0.

### Which mood a line is said with

A line's `#face:<mood>` hashtag sets it. The presenter hands the line's Yarn metadata to the page
(`IDialogueView.ShowLine`). An untagged line takes its mood from its punctuation:

1. ending in `?`: `asking`;
2. any `!`: `bright`;
3. trailing off, with `...`, `…` or a dash: `grave`;
4. anything else: `plain`.

Writing the tags (the style rule):
- **Tag a line only when it wants something its punctuation would not give it.** A plain statement said plainly
  needs no tag; a question that is really a challenge does (Corvin's "Do you want your mother to fade?" is
  `#face:wary`).
- **One tag a line, said by someone with a face.** Wren's options have no portrait, and neither does a faceless
  speaker; `PortraitTests` holds every `#face:` to a drawn mood on a speaker with a face.
- **The mood is the line's, not the scene's.** It does not carry to the next line; a speaker who stays grave is
  tagged on each line they are grave on.
- **The tag goes before `#line:`,** with the line's other tags. It changes neither the text nor the line's id, so
  the localisation is untouched.

The overlay is a blend of two drawings, not the shader. The fills match the shader; while a person fades, the line
greys a little faster than in the room, about 0.6 against 0.35 of the way at full wash. A shader on the
UI element would match exactly; that is for the hand pass if it matters.

## 4. Redrawing

- **One portrait:** replace `Portrait_<Speaker>.png` with a sheet of the same five rows of four 256-px frames.
- **A new mood:** add a row to `MOODS` in `portraits.py` and its name to `Portraits.Moods`, in the same place.
- **Re-render everything:** run `blender -b -P tools/characters/portraits.py`, then
  `python tools/characters/portraits_pack.py`. The 41 speakers' 410 frames render in about 4 minutes.
- **Re-wire the page:** rebuild with **OWSBG → Build Bootstrap Scene**.

## 5. Verification

| Check | Where |
|---|---|
| Every Yarn speaker has a face or is faceless, never both, never neither; Marrow has none; every Remnant at rest has a face | `PortraitTests.EverySpeakerHasAFaceOrIsFacelessAndNeverBoth` |
| A face is drawn from the body the speaker is met in: the named birds' looks, the cast's own sheets | `PortraitTests.EachFaceIsDrawnFromTheBodyTheSpeakerIsMetIn` |
| The pack and the table list the same speakers and moods; a row of four 256-px frames per mood; import settings | `PortraitTests.EveryFaceIsPackedAsARowOfFourFramesPerMood` |
| In every mood the beak opens; the grey is the same outline with less colour, nearer the paper | `PortraitTests.TheBeakOpensAndTheRemnantIsTheSameDrawingGreyed` |
| Every mood's face differs from the plain one over a fiftieth of what either covers, for every speaker | `PortraitTests.EachMoodIsADifferentFace` |
| A tag wins; a question asks, an exclamation is bright, a line that trails off is grave; an unknown mood is plain | `PortraitTests.ALineIsSaidWithItsTagOrWhatItsPunctuationSays` |
| Every `#face:` in the Yarn project is a drawn mood, on a line said by someone with a face, never on an option | `PortraitTests.EveryFaceTagIsAMoodOnALineWithAFace` |
| The choughs and the cranes each differ over a twentieth of what either covers | `PortraitTests.TheBirdsWhoShareALookStillReadApart` |
| The persistent page carries every strip; the contact sheet exists | `PortraitTests.ThePersistentPageCarriesEveryPortrait` |
| Sable's line shows her face, talking and then resting | `DialoguePortraitTests.ASpeakerWithAFaceShowsItTalkingThenResting` |
| The ashes show no face; Wren's choices show none | `DialoguePortraitTests.AThingThatSpeaksShowsNoFace` |
| Corvin, with no Corvin in the room, speaks in grey | `DialoguePortraitTests.ARemnantSpeaksInGrey` |
| Corvin's tagged question is wary, not asking; the page shows the wary row, talking then resting in it | `DialoguePortraitTests.ALineIsSaidInItsMood` |
| A Sable in the room, fading and then a Remnant, sets her portrait's grey | `DialoguePortraitTests.TheBirdInTheRoomDecidesHowGrey` |
