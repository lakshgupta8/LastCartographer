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
- **An unwritten place loses its bind.** Nothing weakens, but the memory is no longer there to anchor it with.

So each memory is spent once, on one of three things: anchoring its place, opening something, or keeping it.
The bible's rule 4, "choices cost; they do not punish", is the shape of it. Nothing is lost that wasn't chosen, and
a door can always be left shut.

## 2. What asks, v1

| Asker | Kind | Room | Takes | Gives | Why |
|---|---|---|---|---|---|
| The Salt Chapel's door (`Chapel_Door`) | Door | Saltmarrow_Chapel | Dotha's songs | the reliquary: 2 scraps; `saltmarrow.chapel.door_open` | "Sing me in." Merrow's End's are the last songs on the coast |
| The gannet at the faded light (`Chain_Gannet`) | Bird | Saltmarrow_Chain_3 | Sable's count | 6 iris seed | A Remnant who watched the boats go out and asks who comes back. Only Sable counted |

- **The door warns you,** in its own way, when Merrow's End is anchored: "The songs are sealed into Merrow's End.
  The door doesn't care."
- **Without the memory,** the door only waits ("Nobody on this coast sings any more. Nearly nobody."), and the
  gannet's answer is dimmed.
- **Isolde's memory has no asker yet.** It has no home, so giving it would weaken nothing. It would cost only itself.
  That's the kind of choice a late asker should offer.

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

- **More memories, more askers.** Three memories exist, all on the coast. Every region's people should give one:
  Kettil's count, Runa's roll-call, Teodor's eleven villages, Idrenne's standing place, Tam's exam, Ilse's. Each region
  wants a door or a bird that asks for one.
- **Placement.** Neither asker stands in its room yet. As with the environmental pieces, a prop with an `NpcTalker` on
  the node is ENV-06's. The chapel door's flag is for the door prop to read.
- **A weakened anchor in the Blank.** Whether an anchored place with a loosened seal should appear in Act 3 as a
  half-island (its people half-remembered) is for the Blank generator (PRG-20).
- **Held places and the walk.** A held place's walk might someday ask for a memory, not just a rhythm.
