# Foreshadowing and `#still` (NAR-16, v1)

This covers two audits of the Yarn scripts, both run as tests.

- **The rule of three** (bible 11.1): every secret in bible 5 is planted three times, in three scenes, before its
  reveal. It is never "planted" after the reveal.
- **The `#still` rule** (bible 11.8, style guide 5): every line a player can hear again, unchanged, on a later visit
  is tagged. Nothing else is.

The code is `Foreshadowing` (Core: the secrets, their reveal scenes, and where each scene falls in the story) and
`YarnAudit` (Core: a reading of the scripts' shape, without running them). The tests are `ForeshadowingTests`, in
edit mode.

## 1. Tags

| Tag | On | Rule |
|---|---|---|
| `#plant:5.x` | A line that hints at secret 5.x | Before the reveal, on at least one Act 1 road. In the reveal's own scene it must come before the reveal line, and it doesn't count toward the three |
| `#reveal:5.x` | The one line that says it | Exactly one per secret, in the scene the bible names |
| `#callback:5.x` | A line that leans on the secret once it is known | After the reveal |
| `#still` | A line heard again, unchanged, on a later visit | §3 |

`#plant` also takes the bible's other sections (3.4 the whale, 4.1 Sable's brother, 4.6 the Greyfold creeping, 6.12
the bells, 9.2 Idrenne's Fire). Those plant sections that aren't secrets and aren't counted here.

## 2. The secrets

Each scene falls on a beat of the story (bible 7):

| Beat | Act | What it covers |
|---|---|---|
| Prologue | — | |
| Coast | 1 | |
| Climb | 1 | Emberdown or the Verdance, whichever she climbs first. The other falls in Act 2 |
| Hall | 1 | |
| Orchard | 1 | |
| Edge | 1 | |
| Act 2 | 2 | |
| Threshold | 2 | |
| Last Camp, Lantern, Hollow, Capital, Return, Observatory | 3 | |

A plant counts when its scene's beat comes before the reveal's. The test counts both roads: Emberdown first and the
Verdance first. **Road** in the tables means the plant is on the path every ending's route takes (`EndingRoutes`).
The test asks that every route reaching a reveal has passed at least one plant for it.

### 5.1 The Stillness: revealed in the Orchard (Act 1)

Revealed by Isolde's pages (`Orchard_Isolde_Cache`): "Anchoring stops the fade. It stops everything else too."

| Scene | Line | Beat | Counts for |
|---|---|---|---|
| `Chimneys_Ninth_Agent` | Ostry: "First winter's the hardest, they say. Nearly spring." | Climb (Emberdown) | Emberdown first |
| `Baths_Kettil_Debate` | Kettil: "And in forty years the same six hundred and twelve. Not one more." **New tag** | Climb (Emberdown) | Emberdown first; road |
| `Library_Teodor_Ansel` | Teodor: "I will not turn it for you…" | Climb (Verdance) | Verdance first |
| `QuietHouse_Teodor_Keystone` | Teodor: "They asked, and I made sure." | Climb (Verdance) | Verdance first; road |
| `Hall_Tam` | Tam: "The same, see? Every year I get it right." | Hall | Both |

- **Also:** the pages' "They fall here" comes before the reveal in the same scene. On the Hall_3 exam papers
  (`RoomPlans`) the plant is only planned (§4).
- **Three on each road:** Ostry, Kettil and Tam if she climbs Emberdown first; the Library, the Quiet House and Tam if
  she climbs the Verdance.
- **Callbacks, re-tagged from plants:** Maren's "He is nine" (Act 2) and Voss's "You told the Queen-Regent about the
  boy" (the Threshold). Both come after Act 1's reveal.

### 5.2 Why Isolde walked in: revealed at the Last Camp (Act 3)

Revealed by Isolde's pages (`LastCamp_Isolde`): "The Vault's sixth slot. I took it."

| Scene | Line | Beat |
|---|---|---|
| `Prologue_Edge_Departure` | Isolde: "Because I drew something I shouldn't have. And because someone in there is owed a visit." | Prologue |
| `Prologue_Shore_Wake` | Sable: "Nobody came out but you. Nobody ever comes out." | Prologue |
| `Quay_Sable_First` | Sable: "The Guild's declared Isolde Marr dead." | Coast (road) |
| `Lighthouse_Halvard_Hunt` | Halvard: "The Guildmaster gave it an hour." | Coast (road) |
| `QuietHouse_Teodor_First` | Teodor: "She is careless with one thing. You are standing in it." | Climb (Verdance) |
| `Vault_Pell_Slot` | Pell: "Six. Which, technically, means seven minus one." | Act 2 |

### 5.3 Where Wren is from: revealed in the Hollow (Act 3)

Revealed by Ilse (`Hollow_Ilse`): "You were born here. After." This line was tagged as a plant, but it is the reveal
itself.

| Scene | Line | Beat |
|---|---|---|
| `Prologue_Edge_Survey` | Isolde: "A child at the edge of the white, looking straight at me." | Prologue |
| `Prologue_Edge_Departure` | Isolde: "Look at me. Only at me. Then look beside the white." | Prologue |
| `Edge_Pell_Watch` | Pell: "It's the most frightening thing anyone's said to me." | Edge (road) |
| `Gate_Idrenne_Leap` | Idrenne: "It knows where you're from better than you do." | Act 2 (road) |
| `Aury_Lighthouse` | Aury: "Like a chick I saw once, carried past, out of the white." | Act 2 |

### 5.4 The Halloway debt: revealed in the Old Capital (Act 3)

Revealed by Corvin (`Capital_Corvin`): "When I— granddaughter."

| Scene | Line | Beat |
|---|---|---|
| `Library_Ansel` | Ansel: "A history of the Halloways…" and "The owl had a daughter." | Climb (Verdance) |
| `Orchard_Keeper` | The Keeper: "You're Isolde's." | Orchard |
| `Orchard_Gravestone` | "A second name beneath… The first is an I." | Orchard |
| `Threshold_Voss` | Voss: "Journeyman Halloway." | Threshold (road) |
| `Hollow_Ilse` | Ilse: "My father holds this place. He held on too hard, once." | Hollow (road) |

### 5.5 Voss's daughter: revealed in the Old Capital (Act 3)

Revealed by Corra (`Capital_Corra`, after 6.13): "He's the Guildmaster now."

| Scene | Line | Beat |
|---|---|---|
| `Prologue_Edge_AfterSmudges` | Isolde: "You hold it like Voss taught you." | Prologue |
| `Office_Pell_Drawing` | Pell: "He's never surveyed anything." | Act 2 |
| `Threshold_Voss` | Voss: "I have stood here since dawn. I cannot say why I have not." And "Tell her— no. Nothing." | Threshold (road) |

- **Also:** the framed chick's drawing in `Bastion_3` is a planned plant (§4).
- **Callback, re-tagged from a plant:** "Papa. And Aurelian, when he's in trouble." It comes after the reveal line in
  Corra's scene.
- **Exactly three.** 5.5 has the fewest plants, and the Office is off the road.

### 5.6 The Grounding: revealed in the Old Capital (Act 3)

Revealed by Corvin (`Capital_Corvin`): "Nobody lost their wings. They lost the memory of them."

| Scene | Line | Beat |
|---|---|---|
| `Quay_Sable_First` | Sable: "Nothing that still remembers is." | Coast (road) |
| `Lighthouse_Lamp` | "Lit for the flock, going south." | Coast |
| `Chimneys_Runa_Climb` | Runa: "The chimney remembers who climbed it." | Climb (Emberdown; road) |
| `RootChapel_Teodor_Thread` | Teodor: "Draw it the other way. It is the same line." | Climb (Verdance; road) |
| `Gate_Inscription` | "Passage for the winged. Land, and be welcome." | Climb (Verdance) |

## 3. `#still`

**The rule.** A line is **standing** when some way through its visit changes nothing. The player can take that way
again and hear the line again, unchanged, so QA should read a repeat as meant, not as a bug. The audit reads it like
this:

- **What counts as a change.** A command changes the next visit unless it is neutral: `voice`, `shop`, `tutorial`,
  `survey_hint` and `wait` are. A `<<flag>>` changes it only if some script's condition or the game's code reads that
  flag. A flag nothing reads is set again next time to the same value, and routes nothing.
- **Choices don't count.** Asking the same question gets the same answer.
- **Jumps.** A way through ends at `<<stop>>` or the node's end. At a `<<jump>>` it carries on into the next scene, and
  that scene must stand too.

`#still` goes on **every standing line of a scene that expects to be visited again**, and on no line that can't stand.
A scene expects to be visited again when it reads a flag it writes (its own "after" branch), or when such a scene
jumps into it. Standing lines elsewhere may carry `#still` without needing it: the night posts, the idle lines, the
anchored epilogue ("Prices are the same.").

**What the audit changed:**

| Change | Where |
|---|---|
| **+86** `#still` | 24 files. Kettil's hub lines, Sable's return visits, Idrenne's camp, and Halvard's lines before the fight at the Threshold, which play again on every retry. Also the Observatory frame before the dome opens, Runa at Hollowvein, and the Wind Gate |
| **−2** `#still` | Dotha's "Nine songs. Still nine.": every choice after it decides the village. Ansel's "Kind of you. Another time.": his scene checks on the next visit the flag it sets |

The style guide's sentence ("in an anchored town") now reads as this rule. Anchored towns are where it matters most,
but the tag means the same everywhere.

## 4. The rooms' plants (NAR-15)

The environmental pass (`docs/story/environment.md`, `Dressing`) dressed the plants this audit was waiting on. The
readable ones are Yarn scenes where the thing speaks, and they count like any other plant:

| Scene | Room | Line | Plants | Beat |
|---|---|---|---|---|
| `Chain_Log` | Saltmarrow_Chain_1 | "Lamp lit. No flock." | 5.6 | Coast |
| `Chapel_Tapestry` | Saltmarrow_Chapel | "The salt has eaten the birds. Their shapes are holes. The sky is whole." | 5.6 | Coast |
| `Rest_Lintel` | Emberdown_Rest_1 | "The ladder up to it is newer than the words." | 5.6 | Climb (Emberdown) |
| `Road_Milestone` | Verdance_Road_2 | "Nine on foot. Bring bread." | 5.6 | Climb (Verdance) |
| `Library_Lectern` | Verdance_Library_2 | "The pin has rusted into the wood. The list has not yellowed." | 5.1 | Climb (Verdance) |
| `Bridges_TollBoard` | Halden_Bridges_1 | "Every figure matches the one above." | 5.1 | Hall (Emberdown's way in) |
| `Mills_Sheets` | Halden_Mills_2 | "The whole loft has the same thumb." | 5.1 | Hall (the Verdance's way in) |
| `Hall_Roll` | Halden_Hall_2 | "The ninth is chiselled out. The owl's-eye crest beside it was left." | 5.4 | Hall |
| `Hall_Order` | Halden_Hall_1 | "No exceptions. No requests. The paper is forty years old. The pins are new." | 5.5 | Hall |
| `Hall_ExamPapers` | Halden_Hall_3 | "The date at the top is forty years old. The ink is wet." | 5.1 | Hall |
| `Bastion_Plaque` | Halden_Bastion_1 | "There are no stairs between." | 5.6 | Act 2 |
| `Office_Drawing` | Halden_Bastion_3 | "In one, high up, a small heron waves." | 5.5 | Act 2 |

The orchard's fallen leaves and the stilt-roosts' ladders are seen, not read, so they carry their plant in the
catalog only. The gravestone's crest was already `Orchard_Gravestone`.

## 5. Tests (`ForeshadowingTests`)

- **Reveals:** each secret is revealed on exactly one tagged line, in the scene `Foreshadowing.RevealNode` names. 5.1
  comes at Act 1's Orchard, and the rest in Act 3.
- **Placement:** every scene that plants, reveals or calls back a secret is placed on a beat, and every placed scene
  exists.
- **The rule of three:** three plants in three scenes before every reveal, on both Act 1 roads.
- **Order:** a plant comes before its reveal on some road, and in the reveal's scene before the reveal line. A
  callback comes after.
- **The road:** every ending's route that reaches a reveal has passed a plant for it, and every reveal is reached by
  some route.
- **`#still`:** every tagged line stands, and every standing line of a scene that is visited again is tagged.
- **The reader:** branches, options, jumps, flag reads and writes, neutral commands, and flags the code reads, checked
  on a small sample script.

## 6. Open

- **5.5 had exactly three plants.** The rooms added two: the Hall's standing order (Act 1, on the road, both climbs)
  and the framed drawing (Act 2). It now has five.
- **The reader reads, it doesn't run.** It treats every branch as possible. A line that stands only in a state the
  game never reaches would still be asked for `#still`; none has turned up.
- **Flags built in code** (`Keys.Of`) aren't seen when the audit looks for flags the code reads.
