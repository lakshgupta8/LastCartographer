# The Store's Assets (ENV-13, v1)

The key art, the Steam capsules, the screenshots and the trailer's clips, all made from the game itself:
- the screenshots and clips are captured as it plays;
- the key art and capsules are composed from a capture of the Wind Gate, Wren rendered large, and the title.

Nothing here is published. The files sit in `docs/marketing/` for the team, and PRO-08 (the store page) takes them
from there.

Data: `Marketing` (Core) holds the shots, the clips and the capsule sizes.
Capture: `MarketingCaptureTests` (play mode) runs only when Unity is started with `-captureMarketing`. Without it,
each test ignores itself, so the suite never rewrites `docs/marketing/`. The batch runner does not honour `[Explicit]`
alone.
Tools in `tools/marketing/`:
- `wren_hero.py` renders Wren large in Blender;
- `capsules.py` composes the key art and capsules;
- `encode_clips.sh` encodes the clips with ffmpeg;
- `fonts/` holds the title's font and its licence.

Tests: `MarketingAssetTests` (edit, 4).

## 1. Making them again

1. Capture: run the play-mode tests with `-testFilter MarketingCaptureTests -captureMarketing` (see §5 for the full
   command). It writes:
   - the screenshots to `docs/marketing/screenshots/`;
   - each clip's frames to `logs/capture/<clip>/`;
   - the key art's ground (the Wind Gate at 3840x2160, Wren hidden) to `logs/capture/key_art_ground.png`.
2. Render Wren: `blender -b -P tools/marketing/wren_hero.py` writes `logs/capture/hero_glide.png` and `hero_survey.png`.
3. Compose: `python tools/marketing/capsules.py` writes `docs/marketing/capsules/`.
4. Encode: `bash tools/marketing/encode_clips.sh` writes `docs/marketing/trailer/<clip>.mp4`.

`logs/` is not committed. The capture rebuilds what it holds.

## 2. The capsules

| File | Size | What is on it |
|---|---|---|
| `key_art.png` | 3840x2160 | the Wind Gate, the clan on its lip, Wren gliding over the updraft, the title in the sky |
| `header_capsule.png` | 920x430 | the same, cropped wide |
| `main_capsule.png` | 1232x706 | the same |
| `small_capsule.png` | 462x174 | the title large, Wren a mark beside it (read at a glance on a list) |
| `vertical_capsule.png` | 748x896 | Wren standing large, turned toward us, the lens up; the title above |
| `library_capsule.png` | 600x900 | the same, narrower |
| `library_hero.png` | 3840x1240 | the Gate and Wren gliding, **no text** (Steam lays the logo over it) |
| `library_logo.png` | 1280x720 | the title alone, on transparency |
| `page_background.png` | 1438x810 | the Gate washed thin toward paper, nobody in it, **no text** (it sits behind the store's text) |

The rules the composition follows:
- **The ground** is washed a little toward paper, so the title and Wren stand forward of it.
- **Wren** is the same rig, colours and ink as her sheets, rendered at 2400 px with the line at about the sheets'
  weight (`LINE = 6.0`; Freestyle's nib and taper do not scale in step with resolution).
- **The title** is set in **IM Fell English**:
  - a 17th-century type, and a stand-in for the hand-cut title the art direction asks for (ENV-11);
  - licensed under SIL OFL 1.1 (`tools/marketing/fonts/OFL.txt`), which allows use in marketing and requires the
    licence to travel with the font;
  - set as "The Last" small over "Cartographer" large, ink on paper, with a rule under it as on a map's cartouche,
    and a wash of paper behind the letters so they read over the drawing.

## 3. The screenshots

At 1920x1080, from the greybox as it plays (`Marketing.Shots`), one or more per region:
- **Saltmarrow:** the stilts, the boardwalk, the lighthouse.
- **Emberdown:** the ninth chimney, the baths.
- **The Verdance:** Aldermere's bunting, the library.
- **Halden:** Lowmarket, the Observatory.
- **Windreach:** the Gate, the camp.
- **The Blank:** the capital.

Two show the HUD (the boardwalk, Lowmarket), and one shows the dialogue page with Sable's portrait. The rest are
clean: no HUD, and no captions the room says on entry.

The capture handles the rooms as they are:
- It stops whatever dialogue a room opens on entry, such as the white's voice.
- It keeps Wren topped up while the camera settles, so a crab nearby does not leave the masks empty in the picture.
- The Edge, Thessaly Hollow and the Greyfold road are near-white by design and made empty pictures, so the Blank's
  capital stands for the white.

## 4. The trailer's clips

Thirty frames a second at 1920x1080, H.264, played by the real controller from a script of inputs
(`Marketing.Clips`):

| Clip | Room | What happens |
|---|---|---|
| `run_the_boardwalk` | Saltmarrow Boardwalk | she runs the boardwalk and jumps the gap |
| `quill_strikes` | Saltmarrow Boardwalk | the quill's three swings, then a dash |
| `the_leap` | Windreach Gate | the leap at the Wind Gate, gliding with Windmemory |
| `sable_speaks` | Saltmarrow (the start) | Sable's line, her portrait talking |

A trailer to cut from them:

| Beat | Clip or still | Seconds |
|---|---|---|
| The world is being forgotten | the Blank's capital, then the Gate washed to paper | 4 |
| A cartographer | `run_the_boardwalk` | 4 |
| The people in it | `sable_speaks` | 3 |
| The quill | `quill_strikes` | 3 |
| The regions | Emberdown, Aldermere, Lowmarket, the library (stills, a slow push) | 6 |
| The sky she has | `the_leap` | 5 |
| Title | `key_art.png`, the title held | 3 |

The edit, the music under it (the score's main theme) and the voice are left to the team. These clips are v1 footage
of the greybox.

## 5. Verification

| Check | Where |
|---|---|
| Nine capsules at Steam's sizes; the logo on transparency with the title on it; the hero untitled | `MarketingAssetTests.EveryCapsuleIsTheSizeSteamAsksFor` |
| The screenshots are exactly the list, each 1920x1080 from a room that exists, each captioned; at least ten | `MarketingAssetTests.EveryScreenshotIsOfARoomThatExists` |
| Every clip encoded, over three seconds, from a room that exists | `MarketingAssetTests.EveryTrailerClipIsEncoded` |
| The font carries its OFL licence | `MarketingAssetTests.TheTitlesFontCarriesItsLicence` |
| The capture itself | `MarketingCaptureTests` (with `-captureMarketing` only; skipped in the suite) |

To capture, run:

```
Unity.exe -batchmode -projectPath LastCartographer -runTests -testPlatform PlayMode -assemblyNames OWSBG.Tests.PlayMode -testFilter MarketingCaptureTests -captureMarketing
```

Leave out `-nographics`: the capture renders.
