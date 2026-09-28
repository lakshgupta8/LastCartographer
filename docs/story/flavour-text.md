# Flavour Text (NAR-17, v1)

The margins of Wren's atlas, and the words on what she carries. There are 84 lines, one for each of:

- every region (7) and zone (45) on the map;
- every Charter (6) and Instrument (7, plus Hale's lens);
- the seven keystones and the six abilities;
- the three bound memories and the two purses.

The **blurbs** say what a thing does ("Slash, slash, thrust. Ink fills a quarter faster."). The **flavour** says what it
is. The code is `Flavour` (Core); the keys are "flavour.<kind>.<id>", harvested for the translators (NAR-18).

## 1. Voice

- **Wren's hand.** First person, as she would write in the margin of her own atlas: a surveyor's note, plain words and
  specific nouns (style guide 1).
- **Quoted inscriptions.** The Charters are documents, so theirs is the line written on the charter.
- **Twenty words or fewer** (style guide 2.1), one or two sentences.
- **No numerals.** Numbers belong to the blurb. "Six hundred and eleven at supper" is a count, not a statistic.
- **No modern idiom, and nobody says the theme.**
- **Nothing ahead of its reveal.** A margin note can lean toward a secret, the way a plant does: Halden's "Nobody has
  moved house in forty years", the Wind Gate's "Then me", the Lantern Grove's lanterns nobody climbs to fill. It
  never says one. The Act 3 lines (Isolde's camp, Thessaly Hollow, the Vault's sixth) can only be seen in Act 3.

## 2. Where it shows

| Page | What | When |
|---|---|---|
| The desk (`DeskMenu`) | The Charter row: its charter's words, under the blurb. A belt slot: the Instrument's line. The Sighting lens reads as Hale's once he is beaten (`Flavour.ForInstrument(kind, world)`) | Always |
| The atlas (`AtlasView`) | Each region's heading note, and each zone's margin note under the first drawn place in it | Once a place there is drawn: the margin fills in as she surveys |
| The journal (`JournalView`) | **Carried**, a new section: abilities, keystones (named now, `Keystones.NameOf`), bound memories, and both purses, each with its line | Always; only what she has |
| The shop (`ShopView`) | Unchanged. The pitch is the seller's own voice, Sable's | — |

## 3. The lines

### Regions (the head of each page)

| Region | Line |
|---|---|
| Saltmarrow | The map's edge. Paper shows through the reeds here, and the sea goes white before it ends. |
| Emberdown | Basalt, and ash like slow snow. They count each other aloud every night, and nobody is missed. |
| Verdance | Trees eighty wingspans tall. Quiet enough to hear the moss. Everyone speaks as if someone is asleep. |
| Halden | Home. Always late afternoon. Nobody has moved house in forty years; I never thought to ask why. |
| Windreach | Grass, sky, and a camp that will not stay drawn. They hold it by walking it. |
| Greyfold | The largest fade. Outlines at the corner of the eye, gone when I look. Colour only near the lantern. |
| Blank | White. Then colour where I stand. The islands drift; I draw them where they are today. |

### Zones (the margin, once a place in it is drawn)

| Zone | Line |
|---|---|
| Saltmarrow.Shore | Where I woke. Wet sand, three torn pages, no map. The tide had my footprints before I stood. |
| Saltmarrow.Quay | A Ferrymen port on a town half gone. Everything here has a price, and Sable says it first. |
| Saltmarrow.Reedmother | Roots as thick as streets. Crabs in the bole, and something at the crown that watches back. |
| Saltmarrow.IrisFields | Pale irises to the horizon: the Ferrymen's purse. Something nests in the middle and dislikes visitors. |
| Saltmarrow.MerrowsEnd | Forty roofs once. Nine songs left, and Dotha keeping them. The water listens here. |
| Saltmarrow.LanternChain | Seven lighthouses on a string of rock. Three have faded. The fourth still burns for someone. |
| Saltmarrow.SaltChapel | A chapel crusted with salt. Halvard waited for me in it, counting paces. |
| Saltmarrow.BoneBridge | A whale, faded to its bones, lying across the channel. It sings, if a Ferryman rows you under. |
| Emberdown.FurnaceStair | Stairs cut through a cold furnace. The heat comes back in patches, and a Warden keeps it. |
| Emberdown.KettilsRest | The holdfast. Six hundred and eleven at supper, counted aloud. Six hundred and twelve with me. |
| Emberdown.RollCallBell | The bell rings at dusk and every name is sung. A name nobody answers is sung twice. |
| Emberdown.NineChimneys | Nine shafts. Nobody remembers building the ninth. Runa climbs them the old way, talons first. |
| Emberdown.CinderBaths | Hot water in cold air. A Guild crane does sums in the steam, and Kettil lets him. |
| Emberdown.Overlook | The Greyfold from outside, for the first time. White to the horizon. It looked smaller from within. |
| Emberdown.Hollowvein | The mine that fell in. The boards are nailed from the outside. The families still set places. |
| Verdance.OldRoad | A road into the trees, older than the Guild. The mills along it grind the same flour. |
| Verdance.QuietHouse | A monastery grown into one tree's roots. Teodor speaks of the faded as if they were next door. |
| Verdance.RootChapel | Lines drawn on the bark in solvent. Read backwards, they are a way up. |
| Verdance.LanternGrove | Lanterns hung in the canopy by birds who could reach it. Some are still lit. Nobody climbs to fill them. |
| Verdance.SunkenLibrary | An anchored library. One monk, one page, thirty-eight years. The dust has learned to wait. |
| Verdance.Aldermere | A village that asked to be let go. I came on its last day. |
| Verdance.OvergrownGate | A gate for flyers: a landing ledge and no path. Roots have it now, and something stone. |
| Halden.SevenBridges | Seven bridges over the drop. The seventh has been under repair for forty years. |
| Halden.PaperMills | Where the Guild's vellum is made. Wet paper in the air, and every sheet the same. |
| Halden.Lowmarket | Below the walls. The paint is thinner here. The notice says 'survey scheduled', and has for years. |
| Halden.JourneymansHall | My old room, as I left it. So is everything else. Tam sits his exam next spring. |
| Halden.OldOrchard | The only place in Halden where leaves fall. Somebody rakes them. |
| Halden.Bastion | A tower for birds who flew. No stairs. What the Guild keeps from me is at the top. |
| Halden.Observatory | The brass dome. The Great Atlas was made here, and broken here. The dome is shut. |
| Halden.Vault | Seven slots for seven stones. I was never meant to count them. |
| Windreach.NineStones | Standing stones across the grass. A Guild surveyor draws them, and pretends I can't see him. |
| Windreach.LongGrassCamp | Wagons, a fire, and soup that has not noticed the move. The camp is never where I left it. |
| Windreach.DryRiver | A riverbed with boats in it, and no river. The clan camps here some nights. |
| Windreach.WindGate | A lip over nothing. For forty years they sang their fledglings off it. Then me. |
| Windreach.IdrennesFire | Idrenne's hearth. The cooking-stone is older than the clan, and so is the joke about it. |
| Windreach.FallenStar | Iron from the sky, used as an anvil. The smiths say it hums when nobody is hitting it. |
| Greyfold.EdgeCamp | A Guild outpost, left in a hurry. The ledger was never posted. The tethers are still coiled. |
| Greyfold.HalfCathedral | Half a cathedral at the edge of the white. The bells ring some nights. Nobody is inside to ring them. |
| Greyfold.IsoldesLastCamp | Her camp. Her fire. Her atlas, every page. |
| Greyfold.RoadThatStops | A road that stops mid-stride, a step from the white. I stepped in and stayed myself. |
| Greyfold.MirrorPool | Still water that shows the bank I am not standing on. Something small looks back. |
| Greyfold.Threshold | Where the map goes no further. The Wardens' line, and the Guildmaster at the front of it. |
| Blank.ThessalyHollow | A village in the white, grey and quiet. I knew the way to the well. |
| Blank.OldCapital | The old capital, drawn in the white. A mirror of the Observatory, and someone still working in it. |
| Blank.AurysLighthouse | The third lighthouse, faded. Its keeper hums to the lamp. Sable's brother, keeping it still. |

### Charters (the words on each charter)

| Charter | Line |
|---|---|
| Surveyor | Issued by the Guild to its journeymen. 'Measure first. Draw what is there.' |
| Warden | A Warden's charter with the seal cut out. Someone kept the grip and threw away the oath. |
| Drifter | Unsigned. Written in pencil, then gone over in ink, as if the writer took a while to decide. |
| Ferryman | Tied to a hook with a Ferryman's knot. 'Everything crosses. Mind the price.' |
| Unwriter | Written in solvent, so it reads only at an angle. The Cantors sing it rather than sign it. |
| Remnant | Grey ink on grey paper. It is easier to read than it should be. |

### Instruments

| Instrument | Line |
|---|---|
| CompassDart | Guild issue. The needle finds the nearest thing that moves, which is not always north. |
| PlumbWeight | For true verticals. Dropped from high enough, it answers other questions. |
| SightingLens | The Guild's lens, ground to catch a line across a valley. Close up, it catches blows. |
| FieldLantern | Honest light. The Guild's, before it was Sable's. Hidden things cannot stand it. |
| TetherHook | A Ferryman's hook. The rope is new. The knot is older than the rope. |
| IrisTincture | Pressed from the pale iris. It tastes of the fields, and of whoever lost them. |
| WaxSeal | Guild wax, stamped with a compass rose. Press it anywhere and the page remembers you were there. |
| HalesLens | The same lens. Since the Nine Stones I hold it the way Hale did, and it comes back quicker. |

### Keystones

| Stone | Line |
|---|---|
| Aury's stone | Taken from a faded keeper's wings. It hums, as he did. |
| The Hollowvein stone | From the bottom of Hollowvein. Still warm. Kettil kept it buried because it broke the mine. |
| The Quiet House's stone | Teodor carried it eleven years. It is heavier than it looks. He told me so. |
| Idrenne's cooking-stone | Idrenne's cooking-stone, forty years of soup in its grain. She laughed when she gave it up. |
| The Vault's sixth | The Vault's sixth. Isolde carried it into the white. I carry it out. |
| The seventh stone | The seventh, from Corvin's talons. He held it forty-one years. My hand is smaller. |
| The Observatory's stone | The one stone that never left the frame. The Observatory's own. |

### Abilities (what came back)

| Ability | Line |
|---|---|
| Wingbeat | One beat, over a gap I could not have jumped. My shoulders knew it before I did. |
| Talonhold | Runa showed me once. My feet had known how all along. |
| Inkthread | Teodor's solvent-lines, drawn the other way. The line pulls, and so do I. |
| Windmemory | The Wind Gate. I leapt, and the air remembered me before I remembered it. |
| Clarity | I stepped into the white and stayed myself. The lantern draws a little of the world around me. |
| Sky | Not a beat, not a glide. Up. |

### Bound memories

| Memory | Line |
|---|---|
| isolde.first_sight | Isolde's, bound at the edge: the first time she saw me. It is warmer than my own. |
| dotha.eleven_songs | Dotha's songs, weather first. Everything true is short. |
| sable.boats_back | Sable's count of the boats that came back. She never says the other number. |

### Purses

| Purse | Line |
|---|---|
| Iris seed | The coast's small change. Everyone takes it; nobody knows who planted the first field. |
| Vellum scrap | Offcuts of good vellum. The Guild counts every sheet; these are the ones it lost. |

## 4. Tests

**`FlavourTests`** (edit mode) checks:

- every region, zone, Charter, Instrument, keystone, ability, memory and purse has a line;
- no line is kept for a thing the game doesn't have;
- every line keeps the style guide: twenty words, no numerals, no idiom, a whole sentence, none said twice;
- Hale's lens only after the Nine Stones;
- the translators get every line and every keystone's name.

**`FlavourPlayTests`** (play mode) checks the pages:

- the desk shows the Charter's and the lens's line, and Hale's once he is beaten;
- the atlas margin is empty until the quay is drawn, then shows the quay's note and the coast's heading, and still
  nothing for Merrow's End;
- the journal lists an ability, a keystone, a memory and the purses, each with its line, and only the stones she has.

## 5. Open

- **Bound memories:** only Isolde's can be bound so far. Dotha's songs and Sable's count have lines waiting for their
  scenes (DES-06, NAR-13).
- **Zones with no rooms yet** have their notes ready. The atlas shows them once those places are on its page.
- **Quill upgrades** (DES-10) and **Charter silhouettes** (CHR-05) will want lines when they exist.
- **Item pickups:** the hidden vellum caches and seed caches say nothing when found. A caption with the purse's line
  may be enough.
- **The environmental plants** (NAR-15) are room dressing, not text, and aren't in this pass.
