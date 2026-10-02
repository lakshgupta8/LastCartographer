# The Store Page, the Trailer and the Press Kit (PRO-08, v1)

The last of the plan's first pass: what the store shows, cut and written from what the game is. Nothing here is
published. It is the draft the team edits, then pastes into Steamworks and hosts where they choose.

Data: `StorePage` (Core) holds the copy, the tags, the requirements and the factsheet, and renders the two pages.
Writer: `StorePageSetup.Write` (**OWSBG → Marketing → Write the Store Page and Press Kit**) puts them in
`docs/marketing/`. Trailer: `tools/marketing/cut_trailer.py`. Tests: `StorePageTests` (edit, 4).

| File | What |
|---|---|
| `docs/marketing/store-page.md` | every Steamworks field in order: short description, about, features, tags, requirements, the assets, the factsheet |
| `docs/marketing/press-kit/index.html` | one page, paper and ink, no scripts: factsheet, description, history, features, the trailer and its clips, every screenshot and capsule, team, contact |
| `docs/marketing/trailer/the_last_cartographer_trailer.mp4` | the rough cut |

## 1. The copy

- **Short description** (Steam's 300 characters): what it is, the world's rule, who Wren is, what she does, and
  the choice. 296 characters.
- **About** in four paragraphs: the kingdom and its rule; the loop (survey, fight, the Inkwell, abilities as
  memory); the regional choice and the generated last act; how it is told and how it looks.
- **Features** as eight lines, each true of the game as it stands.
- **Tags**, fourteen of Steam's twenty, Metroidvania first: the first tags weigh most in Steam's search.

Every claim comes from a design doc or the story bible. There is no "soundtrack by", no "voice acting", no
language but English, and no release date or price. Where the team has a decision to make, the factsheet says
**TBA**, and the test refuses an invented address or link.

## 2. Requirements

The minimum card is the project's target: a GTX 1060-class card at 1080p and 60 fps (`docs/design/performance.md`,
PRO-06). The page says, in a note, that the 60 fps on that card is not yet measured. The rest is Unity 6's floor
with headroom: Windows 10 64-bit, a quad core, 8 GB, DirectX 11, 1 GB of storage (the build is 101 MB).

## 3. The trailer

`cut_trailer.py` follows the beat sheet in `marketing-assets.md` §4, in ten segments at 1920x1080 and 30 fps:

| Beat | From | Seconds |
|---|---|---|
| the white | the Blank's capital, a slow push | 4 |
| a cartographer | `run_the_boardwalk` | 4 |
| the people | `sable_speaks` | 3 |
| the quill | `quill_strikes` | 3 |
| the regions | the chimneys, Aldermere, Lowmarket, the library, 1.6 s each | 6.4 |
| the sky she has | `the_leap` | 5 |
| title | the key art, held | 3.5 |

Cross-fades of 0.4 s join them; the first fades in from paper and the last out to it. Under it, Saltmarrow's
theme: the `bed`, `pulse`, `lead` and `voices` stems the score renders (`docs/audio/music/`, AUD), looped, mixed
and faded out over the title. About 25 seconds.

It is a rough cut: greybox footage, no voice, no card but the title, placeholder music. The team re-edits it
from the clips, which the press kit also links one by one.

## 4. The press kit

One HTML page, the paper's colours and a serif, no scripts and no external resources, so it can be hosted anywhere
or opened from the folder. Every image is linked at full size; the trailer plays in the page. The team section
names the roles from the production plan and no people; the contact is TBA.

## 5. Verification

| Check | Where |
|---|---|
| The short description within 300 and over 150 characters; about, features and tags filled; tags unique and within 20, Metroidvania first; the target card named in the minimum | `StorePageTests.TheCopyFitsSteamsFields` |
| Developer, date, price, website and contact say TBA; no address, link or script in the kit | `TheFactsheetSaysTbaRatherThanInventing` |
| Every capsule, screenshot and clip linked from the kit, and every link resolves to a file | `ThePressKitLinksEveryAsset` |
| The written pages are what the data renders | `TheWrittenPagesAreTheDatas` |

## 6. Open

- **Everything marked TBA.** The developer's name, the release date, the price, the website and the contact.
- **The trailer's edit, voice and music** are the team's. The cut proves the footage holds a trailer's shape.
- **The 60 fps claim** on the minimum card waits on a run on that hardware (performance.md §PRO-06).
- **Steamworks itself**: an app id, the depot and the upload are in `build-pipeline.md` and `steam-upload.ps1`;
  the page's fields are filled by hand from `store-page.md`.
