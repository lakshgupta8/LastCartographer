# Offerings: memory as currency (DES-15, v1)

Bible 10: "Bound memories are inventory. Doors, birds, and keystones ask for specific memories. Offering one weakens
that place's anchor." This is that rule, and the first two things that ask. The code is `Offerings` (Core). The
Yarn is `<<offer asker memory>>`, `can_offer(asker, memory)` and `offered(asker)`. The tests are `OfferingsTests`
(edit mode) and `OfferingsPlayTests` (play mode).

## 1. The rule

- **An asker** is a door, a bird or a keystone in a room. It wants specific memories (or, with an empty list, any),
  takes one, once, and gives something for it: a flag (a door open), scraps of vellum, iris seed.
- **Only what she carries.** A memory lost to a death is in the smudge, and must be recovered before it can be given.
- **A given memory is gone for good.** It leaves her ink, so:
  - it isn't in the next smudge;
  - no scene can bind it again (`Memories.Bind` refuses);
  - it can't be the bind that anchors its place (`Memories.BoundFor`, anchoring §9).
- **Offering one weakens that place's anchor.** If the memory's place (`Memories.HomeOf`) is anchored, the seal
  loses its bind:
  - the place fades a stage even though it is anchored (`FadeStages.Loosen`), and `place.<id>.weakened` counts it;
  - the fate stays, because a fate is final;
  - a loosened seal never goes to blank: stage 3 at most;
  - the caption says so: "Given: nine songs, and which came first. Merrow's End's seal loosens."
- **A held place doesn't notice.** It's held by its people, not by ink.
- **A weakened anchor shows in the Blank.** Its people drift in as a **half-island** (`Islands.HalfPlaces`,
  `Island_Half`): grey, paler than the Remnant, half-remembered. The place itself stays where it is, anchored. "There
  was a place. It's still there. We're not. Somebody gave us away."
- **A still memory is only carried.** A memory bound from the Stillness (`Memories.IsStill`: Tam's "next spring") is
  true and the same every year. It binds ("Bound, and still: next spring. It holds nothing."), it drops and is
  recovered like any ink, but it is no bind to anchor with and nothing that asks will take it. Secret 5.1, in the hand.
- **An unwritten place loses its bind.** Nothing weakens, but the memory is no longer there to anchor it with.

So each memory is spent once, on one of three things: anchoring its place, opening something, or keeping it.
The bible's rule 4, "choices cost; they do not punish", is the shape of it. Nothing is lost that wasn't chosen, and
a door can always be left shut.

## 2. What asks, v1

| Asker | Kind | Room | Takes | Gives | Why |
|---|---|---|---|---|---|
| The Salt Chapel's door (`Chapel_Door`) | Door | Saltmarrow_Chapel | Dotha's songs | the reliquary: 2 scraps; `saltmarrow.chapel.door_open` | "Sing me in." Merrow's End's are the last songs on the coast |
| The gannet at the faded light (`Chain_Gannet`) | Bird | Saltmarrow_Chain_3 | Sable's count | 6 iris seed | A Remnant who watched the boats go out and asks who comes back. Only Sable counted |
| The ninth chimney's door (`Ninth_Door`) | Door | Emberdown_Chimneys_3 | Kettil's count | the builder's satchel: 2 scraps; `emberdown.ninth.door_open` | Nobody remembers cutting it, so it admits only the counted. "Counted, come in. Strangers, the stair." |
| The traveller at the one-night inn (`Inn_Traveller`) | Bird | Verdance_Gate_2 | Teodor's eleven | 8 iris seed | Remnant, from a village the Unwriters let go. She asks which villages still stand, and hers not among them is what she wanted to hear |
| Brek at the Gate (`Gate_Brek`) | Bird | Windreach_Gate_1 | where Idrenne stood | `windreach.brek.on_the_stone` | A fledgling with no place to stand on. Given Idrenne's, he stands on her fire's stone. Next spring, he says |
| Corvin (`Capital_Corvin_Argue`) | Bird | Blank_Capital_4 | Isolde's first sight of Wren | `corvin.saw_her`; his stance turns to persuaded | Only when the argument falls short and she carries it. "Let me see what she saw." Not a keystone trade: the seventh is given whatever she does |

- **The door warns you,** in its own way, when Merrow's End is anchored: "The songs are sealed into Merrow's End.
  The door doesn't care."
- **Without the memory,** the door only waits ("Nobody on this coast sings any more. Nearly nobody."), and the
  gannet's answer is dimmed.
- **Only after the story lets them:** the inn's traveller is there only the night the road is
  (`verdance.gate.inn_visited`); Brek stands near the stones only once Wren has jumped (`windreach.leap.done`).
- **Nothing inland can loosen.** Kettil's Rest is held by its people, the Quiet House is nobody's to seal, and
  Windreach is never anchored. Inland, a memory's cost is only itself: it can't open its door and anchor its place both.
  With the licence rule (anchoring §9), only the coast's two are ever both.
- **Isolde's memory has one asker, late.** It has no home, so giving it weakens nothing; it costs only itself, and it
  is the first thing ever bound. Corvin asks for it when the three-part argument (act3.md §2) falls short: giving it
  persuades him where the words didn't. A player who lost it to a death and never went back for it has only the words.

## 2a. Who gives what

| Memory | Given by | Where | Words |
|---|---|---|---|
| `dotha.nine_songs` | Dotha | "Write it as it was" (`Merrow_Dotha_Season`) | nine songs, and which came first |
| `sable.boats_back` | Sable | once the widow is decided (`Quay_Sable_Widow`) | the count of boats that came back |
| `kettil.count` | Runa | the night she is named at the bell (`Bell_Runa_Named`, after Hollowvein or a held Merrow's End) | the count at the bell, with you in it |
| `teodor.eleven_names` | Teodor | the Lantern Grove vigil (`Grove_Teodor_Vigil`) | eleven villages, in the order he sealed them |
| `idrenne.standing_place` | Idrenne | after the stone, at her fire (`Fire_Idrenne`) | where Idrenne stood when she learned it |
| `tam.next_spring` | Tam | his notes, "for luck" (`Hall_Tam`); a still memory | next spring, eleven years running |
| `isolde.first_sight` | Isolde | the prologue's lesson | the first time she saw you |

Each giving sits inside a branch its scene already runs once, so nothing standing changed and the `#still` audit
holds. Runa gives the count only when she names Wren, which needs a place held first: the count with you in it is
earned the way the town earns it.

## 3. What it costs, by route

| If Wren… | Then… |
|---|---|
| Anchors Merrow's End with Dotha's songs, then opens the door | Merrow's End stays anchored, but at fade stage 1: the held state over thinner ink |
| Opens the door first | She can't anchor Merrow's End herself. She can still hold it (the walk) or release it |
| Holds Merrow's End (the walk), then opens the door | Nothing else: the village holds itself |
| Tells the gannet Sable's count | The quay can't be anchored by Wren. If it already was, it loosens a stage |

## 4. Keystones

None asks yet. Every keystone's giving is already written, and none of them is a trade:
- Aury gives his freely ("Take it, will you? It keeps me up.");
- Idrenne gives hers laughing, at no cost, deliberately;
- Teodor's is a dialogue check;
- Hollowvein's is under a fight;
- the Observatory's never leaves the frame.

A keystone that asks for a memory would change an ending's route, so it belongs with the endings (DES-12) when a
memory worth that exists. The kind is in the enum for it.

## 5. Tests

`OfferingsTests` (EditMode, 3):
- **Every asker stands somewhere and asks in Yarn:** it stands in a built or planned room, its scene exists, and it
  gives something. Everything it wants is a real memory taken by an `<<offer>>` in its scene, and every `<<offer>>` in
  the Yarn is an asker taking what it wants.
- **A given memory is gone for good:**
  - the wrong memory is refused;
  - the door opens and pays;
  - the memory can't be bound again, given twice, or used to anchor;
  - a dropped memory can't be given until it is recovered, and a given one isn't in the next smudge.
- **Offering a place's memory weakens its anchor:** an anchored Merrow's End goes to stage 1 and stays anchored; a
  held one doesn't notice; an unwritten one isn't weakened; a loosened seal never reaches blank.

`OfferingsPlayTests` (PlayMode, 2):
- The door, played for real: with nothing to give it only waits; with Dotha's songs and an anchored Merrow's End it
  warns, opens and pays, and the village fades a stage; afterwards it's open and empty.
- The gannet: "I don't know" gives nothing. Once the widow is decided, Sable's count buys six seed, and the unanchored
  quay doesn't loosen.

## 6. Open

- **Halden gives a still memory** (decided 2026-09-29): Tam's binds and holds nothing, and no Halden door asks for
  anything. The Blank has no giver: Ilse's belongs to the Hollow's reveal (NAR-12) and should be written with it.
- **No keystone asks** (decided 2026-09-29): every keystone stays as written.
- **A half-island's room** is the generic Remnant island's, paler (`IslandBuilder`); its own dressing is PRG-20's.
- **Placement.** The askers stand in their rooms by `PlacementSetup`: the gannet on the faded light's rail as an ochre
  block (a bird is a character's drawing, not a prop's), the door past the chapel's altar as its drawing (ENV-06,
  `environment-props.md`): `Prop_ChapelDoor` shut, `Prop_ChapelDoor_Open` once `saltmarrow.chapel.door_open` is set,
  swapped by a `DressingProp`; the ninth chimney's door the same on its flag. The door opens as a drawing, not as a
  collider.
- **A weakened anchor in the Blank.** Whether an anchored place with a loosened seal should appear in Act 3 as a
  half-island (its people half-remembered) is for the Blank generator (PRG-20).
- **Held places and the walk.** A held place's walk might someday ask for a memory, not just a rhythm.
