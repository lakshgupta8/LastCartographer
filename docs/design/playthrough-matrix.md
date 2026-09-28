# Full-playthrough matrix (PRO-05, v1)

This covers every way through the game the map allows, as data and as a test: each ending in each region order,
and each sequence break the soft gaps permit. It is built on the ending routes (DES-12, `docs/design/ending-matrix.md`)
and the macro map (`WorldGraph`), and replayed through the shipped scripts. The point is to find a lock before a
player does: a scene that dims the option the story needs because something was done in another order.

## The legs

A route (`EndingRoutes`) is one order of the story. `Playthroughs.LegsOf` cuts it into legs by the story's marks,
not by geography:

| Leg | What | Cut by |
|---|---|---|
| Shore | Sable, the Lamp-Keeper, Halvard's first hunt, Sable again | from the start |
| Emberdown | Kettil's Rest, the Bell, the Chimneys (Talonhold), the Baths, Hollowvein | zone `Emberdown.*` before the act break |
| Verdance | the Quiet House, the Root Chapel (Inkthread), Aldermere | zone `Verdance.*` before the act break |
| ActBreak | Isolde's cache in the Orchard, the Edge (Clarity, `act2.started`) | `Orchard_Isolde_Cache`, `Edge_Pell_Watch` |
| Halden | the Hall, the Seven Bridges, the Hall (Pell keeps the report) | `Halden.*` after the act break |
| Windreach | the three fires, the Wind Gate (Windmemory), Idrenne's Fire | `Windreach.*` |
| Threshold | Voss's notice, Halvard's third, Voss, the last camp (`act3.started`) | `EdgeCamp_Notice` to `LastCamp_Isolde` |
| End | the Blank, the Observatory, the epilogue | after the last camp |

The tests hold that every route cuts into these in the story's order, each leg one run, each in its region.

## The orders

The map's spine is Wingbeat → (Talonhold or Inkthread) → the Plateau → the other → Windmemory → Clarity → the
Sky (`docs/design/world-map.md`). Two of its joints are free:
- **Act 1:** either climb first. Emberdown then Verdance, or Verdance then Emberdown.
- **Act 2:** Halden's business or Windreach's first. Windreach opens from Lowmarket on `act2.started`, which the
  Edge sets, so both are open at the same time.

`Playthroughs.Build(ending, climb, act2)` puts the legs in the asked order; the result is the route's steps, every
one of them, reordered. Nothing else moves: the shore is first, the act break sits between the acts, the
Threshold follows Act 2, the Blank and the Observatory follow the last camp.

| Ending | Orders | Why |
|---|---|---|
| The Fixed World | 4 | both climbs, both Act 2 legs |
| The Open World | 4 | the same |
| The Unwritten | 2 | no Windreach leg: its route needs no Windmemory |
| The Cartographer's Rest | 2 | the same |

## The sequence breaks

Only Wingbeat gaps are soft (world-map §2): a skilled pogo crosses the lighthouse gap, the climb to Emberdown and
the iris gap without the ability. So a player can be up either climb before the Lamp-Keeper. The matrix plays
that:

| Break | Steps |
|---|---|
| EmberdownBeforeTheLamp | the whole Emberdown leg first, by the Bone Bridge's gap; then the shore and the rest |
| VerdanceBeforeTheLamp | the whole Verdance leg first, by the iris gap; then the shore and the rest |

On a break the replay lets the map's soft gaps count as crossed, and holds that the first step is **only**
reachable that way, so a break is a break. Talonhold, Inkthread, the Plateau and every story flag stay hard; a
break can reach a climb's hub and its first scenes, and the route still earns its ability there before the
shafts and anchors beyond.

Two breaks for each of the four endings: eight. With the twelve orders, **twenty playthroughs**, each named
(`Fixed/VerdanceFirst/WindreachFirst`, `Rest/EmberdownBeforeTheLamp`), each once.

## The replay

`RouteReplay` (PlayMode tests) is the harness the ending routes already used, pulled out so the matrix shares it.
From a new game booted past the prologue, for each step:
1. the step's zone is on the map and reachable with what the playthrough has earned so far (soft gaps only on a
   break);
2. a scene runs to its end through the shipped Yarn project, taking exactly the route's choices; an option the
   scene dims, or a choice the route doesn't list, fails the playthrough by name and step;
3. a fight is recorded as its sheet says, where the sheet says, granting what the sheet says.
At the end the ending's flags choose that ending and its epilogue has walked.

## What the matrix found

All twenty reach their ending through the scripts as shipped, with the map's reachability holding at every step:
no scene dims the option a route needs because another leg came first, and no leg is out of reach in either
order. Two things worth knowing came out of it:
- **On the Emberdown break the Collapse is the first boss.** Up the Bone Bridge's gap before the Lamp-Keeper, a
  player reaches Kettil's Rest, the Bell and the Chimneys (Talonhold) with no Wingbeat, and the route goes on to
  Hollowvein, so the Collapse (tier II, 27 health, `docs/design/tuning.md`) is met before the tier I lamp. The
  kit holds: its rubble asks for the down-strike, which is base kit, and its phase-two surges for a jump or a
  Longstroke. The tuning assumed the lamp first; the feel-test (PRO-03) should try this order once.
- **The Verdance break stops at the Root Chapel's business.** Inkthread is taught there and the road beyond is
  anchors, so the break reaches the Quiet House, the chapel and Aldermere, which is the whole leg; nothing further
  is out of order.
The matrix takes about a hundred seconds in the editor, five seconds a playthrough.

## Tests

`PlaythroughsTests` (EditMode, 4):
- every route cuts into legs in the story's order, each leg one run, each in its region;
- every order is a permutation of its route, with the climbs and Act 2 in the asked order;
- a sequence break enters by a soft gap and nothing else, the whole climb before the lamp, the lamp still the
  first fight;
- the matrix names each playthrough once: four orders where Windreach is on the route, two where it isn't, two
  breaks each, twenty in all.

`PlaythroughMatrixTests` (PlayMode, 20): one case per playthrough, by name, replayed to its ending.

`EndingRoutesTests` (PlayMode, 5) is unchanged in what it proves; it now runs on `RouteReplay`.

## Open

- **A fourth Act 2 order.** Windreach by sea from the Bone Bridge (world-map §6) would add an order; it needs the
  Ferrymen's way on the map first.
- **The reward for a noticed break** exists now (`SequenceBreaks`): a line, a scrap and a flag on the first room
  past a soft gap, and the test holds that each break playthrough starts in a zone the game would notice. The
  replays run scenes, not rooms, so the reward is tested on its own; a `#still`-free variant from the zone's
  people on the flag is still to write.
- **Breaks inside Act 2.** The Dry River's gap is soft too, but it is inside Windreach, past the story gate, so
  it changes nothing the matrix can see.
- **Partial orders inside a leg.** The matrix moves whole legs. Within Emberdown the route's order (the Bell before
  the Chimneys, the debate before Hollowvein) is the one the scripts write; a player who takes the Chimneys first
  is not yet a row.
- **Real fights.** As in the ending routes, a boss is a flag. When the late fights exist, the Boss steps should
  play them; the harness has the place for it.
