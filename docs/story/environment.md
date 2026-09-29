# Environmental storytelling (NAR-15, v1)

What the rooms say without anyone saying it: the inscriptions, tapestries, remains and traces in every region, and
the fledgling loops. This is the words and the brief. The art pass (ENV-06) builds the props, and CHR-14 animates the
fledglings.

The catalog is `Dressing` (Core). Each readable piece is a Yarn scene in `<Region>_Environment.yarn`. The tests are
`DressingTests` in edit mode and `DressingReadTests` in play mode.

## 1. Rules

- **Environment first** (style guide 6). If a lintel, a board or a tapestry can carry a fact, no NPC is asked to say
  it.
- **The thing speaks, never a person.** The speaker is the object: `Board:`, `Lintel:`, `Tapestry:`, `Log:`. The
  lines describe what's there and quote what's written on it. Wren doesn't narrate; her margin notes are NAR-17's.
- **Reading changes nothing.** No flags, no commands, no choices. Every line is `#still`, because it reads the same
  every time unless the place itself has changed.
- **A piece may change with its place.** The lines branch on `place_fate`, `fade_stage` or a story flag, never on
  having been read.
- **Twenty words a line**, like everyone else (style guide 2.1).
- **The faded are not dead** (bible 3.7). Remains are what's left of the dead: the Hollowvein miners, the tether-
  walkers who didn't come back. A grey bird in the Blank is never a corpse.

## 2. Kinds

| Kind | What | In this pass |
|---|---|---|
| **Inscription** | Words cut, painted, chalked or pinned: a lintel, a board, a milestone, a notice | 20 |
| **Tapestry** | A picture made to be kept: a tapestry, a wagon-cloth, a child's drawing | 5 |
| **Remains** | What's left of someone: a cut tether, a turned-down cup, a lamp on a hook | 8 |
| **Trace** | A sign of how a place lives: raked leaves, dust that doesn't fall, one thumb in every sheet | 9 |

There are 42 pieces, and 28 of them can be read. Each has a brief for the artist. Where a piece changes with its
place, it says how.

## 3. By region

Read pieces are shown as `Scene` and "the line"; seen-only pieces as a description. **P** marks a plant, with the
bible section it plants.

### Saltmarrow (the built coast)

| Room | Piece | What it carries | P |
|---|---|---|---|
| Shore | `Shore_Tetherposts` | Tether-posts cut a wingspan out. "Brin. Back by dark." Eleven notches. Then none | |
| A (the Quay) | `Quay_PriceBoard` | "Tether, and back again: ask." Chalked under it: "Nobody asks." The Ferrymen's rot (bible 3.5) | |
| Stilts | the ladders | Roost doors with landing ledges and no steps, ladders lashed on later, wing-bindings drying | 5.6 |
| B (Merrow's End) | `Merrow_Lintels` | A song's name over every door. The doors go white first. Held: fresh chalk. Anchored: fresh chalk, the same strokes every day | |
| Ferry | the boats | Boat-shaped patches of paper where the boats were moored | |
| Chain_1 | `Chain_Log` | "Flock south at dusk. Lamp kept." Then, every night for forty years: "Lamp lit. No flock." | 5.6 |
| Chain_3 | the faded keeper | A grey silhouette at the third lamp, seen from outside (Aury) | |
| Chapel | `Chapel_Tapestry` | A flock over the sea, the birds eaten out by salt: "Their shapes are holes. The sky is whole." | 5.6 |

### Emberdown

| Room | Piece | What it carries | P |
|---|---|---|---|
| Rest_1 | `Rest_Lintel` | "Land soft. Leave light." over a roost door; the ladder is newer than the words | 5.6 |
| Rest_2 | `Rest_TallyWall` | A count cut for every year, every number different: the Holdfast holding without freezing. One year it falls by thirty-one | |
| Rest_3 | `PitHead_Cups` | Thirty-one cups by the boards, turned down, a name in each. After the walk: right side up, and clean | |
| Chimneys_3 | `Chimneys_Foot` | Eight chimneys have builders' names; the ninth's stone is bare, the soot fresh | |
| Hollow_2 | `Hollow_Lamps` | A lamp on a hook for each miner. After the walk: lit, all thirty-one | |
| Hollow_4 | the bottom | A boot, a lamp, a pick with a name on it, kept small. After: swept, thirty-one names chalked | |

### The Verdance

| Room | Piece | What it carries | P |
|---|---|---|---|
| Road_2 | `Road_Milestone` | "HALDEN, ONE DAY'S FLIGHT", cut for eyes a long way up. Chalked low: "Nine on foot. Bring bread." | 5.6 |
| House_2 | `Cloister_Tapestry` | The Verdance in undyed wool, villages as knots, some unpicked, none cut. Aldermere released: its knot open | |
| Library_1 | the dust | Dust that hangs and doesn't fall, parting round Wren | 5.1 |
| Library_2 | `Library_Lectern` | A reading list with one title. "The pin has rusted into the wood. The list has not yellowed." | 5.1 |
| Aldermere_1 | the bunting | The last day dressed as a festival; the same bunting on the island in the Blank | |
| Gate_2 | the ledge | Talon grooves on the landing ledge, none newer than forty years (the inscription is `Gate_Inscription`) | 5.6 |

### Halden Reach

An anchored city. Its things say the same thing forever, and that is the point.

| Room | Piece | What it carries | P |
|---|---|---|---|
| Bridges_1 | `Bridges_TollBoard` | "Revised each spring." Forty springs below it, every figure matching the one above | 5.1 |
| Bridges_3 | the seventh bridge | The same planks for forty years, the paid family in their chalked squares | 5.1 |
| Mills_2 | `Mills_Sheets` | A thumbprint in the pulp. "The whole loft has the same thumb." | 5.1 |
| Hall_1 | `Hall_Order` | "No Warden crosses the Greyfold line. No tether is issued for the Old Capital." The paper is forty years old; the pins are new | 5.5 |
| Hall_2 | `Hall_Roll` | Nine Guildmasters and a tenth. "The ninth is chiselled out. The owl's-eye crest beside it was left." | 5.4 |
| Hall_3 | `Hall_ExamPapers` | The same paper on every desk. "The date at the top is forty years old. The ink is wet." | 5.1 |
| Orchard_1 | the leaves | The only fallen leaves in Halden, raked into one pile | 5.1 |
| Lowmarket_2 | `Lowmarket_Notice` | SURVEY SCHEDULED over older copies of itself. Anchored: SURVEY COMPLETE over them all | |
| Bastion_1 | `Bastion_Plaque` | The landing forty wingspans up; a door cut at the foot the year after the fall; "no stairs between" | 5.6 |
| Bastion_3 | `Office_Drawing` | Crayon, framed: a heron as tall as the page with a compass; in a high window, a small heron waves | 5.5 |

### Windreach

| Room | Piece | What it carries | P |
|---|---|---|---|
| Stones_1 | `Stones_Notches` | A notch for every walk, back past the Guild's first map; the newest is this year's | 9.2 |
| Camp_1 | `Camp_WagonCloth` | The route woven on canvas. "No walls anywhere on it. At every stop, the clan is woven walking." | 9.2 |
| River_2 | the boats | Boats on their sides in the mud, names on their bows, smudges in them | |
| Gate_1 | `Gate_Lip` | Flat stones on the lip, each carved with a place, not a name: what the clan sings (Idrenne, `Gate_Idrenne_Leap`) | |

### The Greyfold

| Room | Piece | What it carries | P |
|---|---|---|---|
| EdgeCamp_2 | `EdgeCamp_Beam` | "I. M." cut in a post, and under it a wren in four lines | |
| EdgeCamp_2 | the tethers | Coiled tethers, never used; one peg empty, its tether run under the fence and cut | |
| Road_2 | `Road_Mileposts` | "THE CAPITAL, 2." Then 1. Then blank, and so is the road | |
| Road_3 | the footprints | A line of footprints stopping mid-stride where the road does | |
| Threshold_1 | the old tether | One old Ferrymen's tether among the Guild's new stakes, running taut into the white | |

### The Blank

| Room | Piece | What it carries | P |
|---|---|---|---|
| Capital_1 | `Capital_Nameplate` | "A. VOSS, JOURNEYMAN. BACK BY DUSK." Polished where a small wing could reach | |
| Capital_2 | the crayon floor | The same tall heron over and over, each with a compass, none with a face | |
| Hollow_2 | `Hollow_Doorframe` | Height marks in Ilse's doorframe. They stop at six | |

The Blank's three all fall in the scene of a reveal (Corra, Ilse), after the player has walked through them. The
audit can't order scenes within a beat, so they are untagged. They read as plants the first time and as callbacks
on a return.

## 4. The plants

Fourteen read pieces plant a secret or a bible section, and their lines carry the tag, so the foreshadowing audit
(NAR-16) counts them. Twelve of them plant a 5.x secret, and those twelve scenes are placed on a beat in
`Foreshadowing`. Seen-
only pieces carry their plant in the catalog but don't count: nobody can prove a player looked.

What changed for the audit:

| Secret | New plants | Before its reveal, now |
|---|---|---|
| 5.1 The Stillness | toll board, sheets (one each road), exam papers, lectern | five or more on each road, three of them in the Hall |
| 5.4 The Halloway debt | the roll of Guildmasters (Hall, both roads) | one more on the road |
| 5.5 Voss's daughter | the Hall's standing order (both roads), the framed drawing (Act 2) | **five**, was three; the Hall's order puts one in Act 1 on the road |
| 5.6 The Grounding | the log, the tapestry (coast), the lintel, the milestone (one each climb), the plaque (Act 2) | eight or more |

The four plants the audit was waiting on (foreshadowing.md §4) are dressed:
- the Hall_3 exam papers, read (5.1);
- the Bastion_3 drawing, read (5.5);
- the orchard's leaves, seen (5.1);
- the towers without stairs, three times (5.6): the stilt-roosts, the Emberdown lintel, the Crown's Tower plaque.

The gravestone's crest was already `Orchard_Gravestone`.

**9.2** (Idrenne's Fire): the stones' notches and the wagon-cloth show the clans holding places by walking them,
before Idrenne says so.

## 5. The fledgling loops

Bible 10: "Fledglings leap in the background of every region; after each ability, one more of them glides a little
further." `Dressing.Loops` places one loop per region. `Dressing.At` says what it shows.

| Region | Room | They leap from | Note |
|---|---|---|---|
| Saltmarrow | Stilts | the top roost's ledge, into the shallows | a grandmother with bound wings watches (bible 1.2) |
| Emberdown | Rest_1 | the cliff roosts, into the ash | counted aloud by whoever is nearest |
| The Verdance | Grove_3 | a canopy branch, into deep moss | nobody sings, nobody stops them |
| Halden | Bridges_2 | a bridge parapet, into a Crown net | anchored from the start |
| Windreach | Camp_1 | the wagon roofs, into the grass | practising for the Gate |
| The Greyfold | Cathedral_2 | the broken tower, into the white | grey outlines at the edge of the eye |
| The Blank | Hollow_2 | the Hollow's roofs, onto the drift | the Remnant's chicks glide as the living do |

**The rule** (`Dressing.At`):
- **Six leap**, one for each piece of the sky.
- **One more glides for each ability Wren has**, and the newest goes furthest, half a wingspan more each time. The
  rest drop as they always have.
- **Anchored places leap the same leap forever:** nobody glides. Halden has been anchored since before the story,
  so its fledglings never get any further. Held places live on.
- **A fade thins them:** half at stage 2, none from stage 3. The Greyfold's and the Blank's are already faded, and a
  fade doesn't touch them.
- **The endings:**
  - The Fixed World: nobody glides, because the Atlas holds the sky for them (bible 9.1).
  - The Open World: they all glide, even in Halden, and one doesn't come down (bible 9.2).
  - The Unwritten and the Rest keep what Wren remembered.

## 6. Tests

`DressingTests` (EditMode, 4):
- **Every piece stands in a room that exists** (planned or built). Every region is dressed, every kind has at least
  three pieces, and most pieces can be read.
- **Every readable piece is a Yarn scene in its region's file where the thing speaks:**
  - no cast member speaks, and there are no options;
  - lines are twenty words or fewer, and every one is `#still`;
  - it writes nothing and runs no commands;
  - its plant matches the catalog's, and a piece that changes has a conditional line.
  Every scene in an environment file is catalogued.
- **The plants the audit waited on are dressed.** 5.5 has four or more plants before its reveal on both roads, the
  Hall's order among them. Every secret the rooms can carry is planted by a room.
- **The fledglings:**
  - every region has a loop in a real room;
  - one more glides per ability, each further than the last;
  - anchored places and Halden never glide, but still leap;
  - a fade thins them, and the faded aren't thinned;
  - the Fixed World grounds them, the Open World flies one, and the Unwritten keeps them.

`DressingReadTests` (PlayMode, 1): every readable piece is run through the shipped Yarn project, once in a new game
and once with its places decided (Merrow's End held, Aldermere released, Lowmarket anchored, Hollowvein walked). Each
one ends with nothing to choose, and the world is byte for byte what it was.

## 7. Open

- **The coast's pieces are placed; the rest wait for their rooms.** `PlacementSetup` (OWSBG → Place the Coast's
  Readables) puts the five read pieces in built Saltmarrow rooms as a trigger with an `NpcTalker` on the node, plus a
  parchment-coloured block to find it by. `PlacementTests` stands Wren at each one and checks up reads it. The art
  (ENV-06) swaps the block for the prop. Pieces in planned rooms are placed when the rooms are built.
- **The fledgling loop is data, not animation.** CHR-14 reads `Dressing.At` for the room's place and Wren's
  abilities.
- **An anchored place freezes at zero,** not at whatever the fledglings had reached when it was anchored. Recording
  that would need the ability count at the seal. Zero reads as the Stillness and is simpler; revisit if a player
  notices.
- **Speaker names** (`Board`, `Lintel`, …) go through the dialogue presenter as written; they'll want Loc keys with
  the rest of the speaker names (NAR-18).
