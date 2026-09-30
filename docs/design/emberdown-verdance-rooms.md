# Emberdown and Verdance Rooms (DES-09, v1)

Room-by-room design for the two climbs (bible 4.2, 4.3; world-map.md §3). Same conventions as the coast
(saltmarrow-rooms.md): rooms 40 units wide, 17 high (19 for vertical rooms), floor at y = 0, exits at the
edges or through the floor and the ceiling. The act-1 player takes one climb and meets the other in Act 2;
both must stand on their own as a first region.

The plans are data: `RoomPlans` (Core) holds every room, its purpose, vantage, enemies, who stands there, and
its exits with their gates. The tables below are generated from the same source as that data, so they cannot
drift. `RoomPlanTests` hold the plans to the macro map (room and vantage counts per zone, every gate between
zones the same as the map's), to the boss sheets (every boss has one arena room, with a desk in it or next
door), to the cast (everyone who appears in a zone stands in one of its rooms), and to each other (the room
graph reaches exactly the zones the zone graph does, for every kit).

## 1. Emberdown (21 rooms, 8 vantages)

```
Bone Bridge ═E, W~═ Stair_1 ─up─ Stair_2 ─up─ Stair_3 (Brann, desk) ─E─ Rest_1 ─E─ Rest_2 (hub) ─E─ Rest_3 ─E─ Chimneys_1
Rest_2 ─up─ Bell_1 ─E─ Bell_2 (the bell; the lesson walk)
Rest_3 ═down, T + flag═ Hollow_1 ─down─ Hollow_2 ─down─ Hollow_3 (desk) ─down─ Hollow_4 (the Collapse)
Chimneys_1 ═up, T═ Chimneys_2 ═up, T═ Chimneys_3 ─E─ Chimneys_4 ═E, T═ Baths_1 ─E─ Baths_2 ─E─ Baths_3 ═E, T═ Overlook_1 ─E─ Overlook_2 ═E, T═ Seven Bridges
```
Each line reads west to east or down the page; `─` is an open way, `═` a gated one: `W~` a Wingbeat gap (soft), `T` Talonhold, `flag` `emberdown.hollowvein_opened`.

**The shape.** The stair is a climb, the town is flat, and everything past the town goes up by the wall or
down into the mine. Talonhold is learned at the foot of the first chimney and asked for in the very next room:
the region teaches the ability by making the way on need it at once. The mine is a column, four rooms
straight down, because the long roll-call is walked downward (bounds-walk.md: Hollowvein, 4 × 6 going down).

<!-- table:Emberdown -->
| Room | Zone | What it is for | Vantage | Enemies | Stands here | Exits |
|---|---|---|---|---|---|---|
| **Stair_1** The foot of the stair | FurnaceStair | The climb from the Bone Bridge ends in black basalt; the first ash falls. A Wingbeat gap is the way in (soft: a pogo off the bat crosses it). | — | cave-bat, salamander ×2 | — | W → Saltmarrow.BoneBridge [Wingbeat (soft)], up → Stair_2 |
| **Stair_2** The furnace landings | FurnaceStair | A vertical room of landings over live furnaces: the Furnace Stair Rescue set piece. The first vantage looks back at the coast. | Landing | cave-bat ×2, salamander | — | down → Stair_1, up → Stair_3 |
| **Stair_3** The cold furnace | FurnaceStair | A desk on the landing, then Cinder Warden Brann's arena: a furnace cooling in sections (6.5). | — | — | desk, arena: brann | down → Stair_2, E → Rest_1 |
| **Rest_1** The gate | KettilsRest | Kettil counts every arrival out loud; a stranger is a problem. Ash on the roofs, roosts cut in the cliff. | — | — | Kettil | W → Stair_3, E → Rest_2 |
| **Rest_2** The square | KettilsRest | The hub: desk, ledger, the smith's shop, Kettil's porch. The bell stair climbs from its east end. | Square | — | Kettil, desk | W → Rest_1, E → Rest_3, up → Bell_1 |
| **Rest_3** The pit-head | KettilsRest | The mine mouth, boarded; the chimneys start at the east wall. Hollowvein opens here once the families agree. | — | — | Runa | W → Rest_2, E → Chimneys_1, down → Hollow_1 [Talonhold, `emberdown.hollowvein_opened`] |
| **Bell_1** The bell stair | RollCallBell | Up from the square to the bell; bats roost in the stair. | — | cave-bat ×2 | — | down → Rest_2, E → Bell_2 |
| **Bell_2** The bell | RollCallBell | The nightly roll-call: Runa counts Wren in, and teaches the walk here (the lesson walk, bounds-walk.md). | Bell | — | Runa, Kettil, walk: kettils_rest | W → Bell_1 |
| **Chimneys_1** The first chimney | NineChimneys | Runa climbs the old way and Wren learns Talonhold at its foot; the shaft above needs it. | — | salamander | Runa | W → Rest_3, up → Chimneys_2 [Talonhold] |
| **Chimneys_2** The shafts | NineChimneys | Wall to wall up three chimneys at once; salamanders on the ledges. | Shaft | salamander ×2, cave-bat | — | down → Chimneys_1 [Talonhold], up → Chimneys_3 [Talonhold] |
| **Chimneys_3** The ninth chimney | NineChimneys | Nobody remembers building it. A Guild agent lives at the top (the Ninth Chimney commission). | Ninth | cave-bat | desk | down → Chimneys_2 [Talonhold], E → Chimneys_4 |
| **Chimneys_4** The flue road | NineChimneys | A tunnel of old flues east toward the baths; the heat rises through the floor. | — | salamander ×2 | — | W → Chimneys_3, E → Baths_1 [Talonhold] |
| **Baths_1** The steam walk | CinderBaths | Boardwalks over hot pools; steam hides the next hold. | — | salamander, smudge | — | W → Chimneys_4 [Talonhold], E → Baths_2 |
| **Baths_2** The baths | CinderBaths | The Cinder Bath Debate: Kettil and a Guild surveyor argue in real numbers, and Runa sings them back. | Baths | — | Kettil, Runa | W → Baths_1, E → Baths_3 |
| **Baths_3** The vents | CinderBaths | Vents that breathe on a rhythm; the climb out to the ridge is a wall. | — | cave-bat ×2 | — | W → Baths_2, E → Overlook_1 [Talonhold] |
| **Overlook_1** The ridge | Overlook | The highland's edge; the wind turns cold and the ash stops. | — | Warden (patrol, the road) | — | W → Baths_3 [Talonhold], E → Overlook_2 |
| **Overlook_2** The overlook | Overlook | First sight of the Greyfold from outside: bigger than it looks. The road down to the Plateau's bridges. | Overlook | — | Runa, desk | W → Overlook_1, E → Bridges_1 [Talonhold] |
| **Hollow_1** The adit | Hollowvein | Down from the pit-head behind the boards; the long roll-call starts at its first beam. | — | — | Runa, walk: hollowvein | up → Rest_3 [Talonhold, `emberdown.hollowvein_opened`], down → Hollow_2 |
| **Hollow_2** The first gallery | Hollowvein | Lamps on the walls, one for each name; the walk's second and third verses. | Gallery | smudge ×2 | walk: hollowvein | up → Hollow_1, down → Hollow_3 |
| **Hollow_3** The flooded gallery | Hollowvein | Black water to the knee; a desk the miners left, still dry. | — | smudge | desk, walk: hollowvein | up → Hollow_2, down → Hollow_4 |
| **Hollow_4** The bottom | Hollowvein | The collapse itself: the Collapse wakes on the fourth verse (6.4). The keystone is under it. | — | — | arena: collapse, walk: hollowvein | up → Hollow_3 |
<!-- /table -->

**Built (ENV-03).** All twenty-one rooms are scenes from recipes (`ProjectSetup.EmberdownRecipes`), on the
highland's kit (paper-kit.md §2b): the stair's iron landings over `Paper_Mid_Furnaces` with Brann's arena behind
two doors in Stair_3; the town on `Ground_Ash` with the desk, Kettil's ledger, her porch and the smith's anvil in
the square, Kettil at the gate, the square and the bell, the boarded mine mouth at the pit-head; the Bell with
Runa, Kettil, the Bell prop and the lesson walk (three verses of five bounds at three Emberdown beats, `<<walk
kettils_rest>>`); the chimneys as Talonhold shafts (two walls four units apart) with Ostry at the ninth; the
baths' boardwalks over pools with the debate's two speakers; the Overlook's Warden and the Greyfold white on its
horizon, Runa and a desk at the end; Hollowvein's four rooms straight down on timber, the Collapse's arena at the
bottom. The Bone Bridge (`Saltmarrow_BoneBridge`) joins the chapel to Stair_1 under the whale's bones. The
cave-bats and salamanders are drawn and placed as the table says (`CaveBat`, `Salamander`; enemy-animation.md
§2b): bats roost under the landings and in the shafts, salamanders crawl the ledges and the flue road. Not yet:
the Hollowvein walk (it crosses four rooms, §4); Overlook_2's road to the Plateau (ENV-05); Brann's and Lorne's
talks before their scenes.

## 2. The Verdance (19 rooms, 8 vantages)

```
Iris Fields ═E, W~═ Road_1 ─E─ Road_2 ─E─ Road_3 ─E─ House_1 ─E─ House_2 (hub) ─E─ House_3
House_2 ─down─ Chapel_1 ─down─ Chapel_2 ═E, I═ Grove_1 ─E─ Grove_2 (vigil) ─up─ Grove_3 ─E─ Grove_4 ═E, I═ Library_1 ─down─ Library_2 (Ansel)
House_3 ─E─ Aldermere_1 (desk) ─E─ Aldermere_2 (the Choir) ─E─ Aldermere_3 ═E, I═ Gate_1 (desk) ─E─ Gate_2 (Gatekeeper) ═E, I═ Paper Mills
```
`I` Inkthread: only a thread crosses, a gap wider than a Wingbeat with an anchor-point over it.

**The shape.** Flat and long, and quiet: the road runs east into the trees, the House sits in the roots, and
the region's two directions are down (into the roots, to the thread) and east (to Aldermere and the gate).
Aldermere is reachable before Inkthread on purpose: the last day happens whether or not Wren has the thread,
and the road out of it is the one the thread opens. The grove and the library are the thread's reward, above
and below.

<!-- table:Verdance -->
| Room | Zone | What it is for | Vantage | Enemies | Stands here | Exits |
|---|---|---|---|---|---|---|
| **Road_1** The iris gap | OldRoad | From the Pale Iris Fields over a Wingbeat gap (soft) onto a road the forest has half taken. | — | crab, skimmer | — | W → Saltmarrow.IrisFields [Wingbeat (soft)], E → Road_2 |
| **Road_2** The milestones | OldRoad | Flying-age milestones, every one giving the distance to a place that is gone. | Milestone | smudge, crab | — | W → Road_1, E → Road_3 |
| **Road_3** The first trees | OldRoad | The trees begin: trunks eighty wingspans tall, light in shafts, near silence. | — | Cantor | — | W → Road_2, E → House_1 |
| **House_1** The roots gate | QuietHouse | The Quiet House's door in the roots of one tree; the brothers bow and do not speak. | — | — | — | W → Road_3, E → House_2 |
| **House_2** The cloister | QuietHouse | The hub: desk, ledger, Teodor. The root stair goes down from its floor. | Cloister | — | Teodor, desk | W → House_1, E → House_3, down → Chapel_1 |
| **House_3** The east door | QuietHouse | The brothers' garden and the lane to Aldermere; ash on the path. | — | — | — | W → House_2, E → Aldermere_1 |
| **Chapel_1** The root stair | RootChapel | Down through the roots; lanterns hung from them. | — | smudge | — | up → House_2, down → Chapel_2 |
| **Chapel_2** The root chapel | RootChapel | Teodor teaches Inkthread: the solvent-line reversed. The grove is across a gap only a thread crosses. | Chapel | — | Teodor | up → Chapel_1, E → Grove_1 [Inkthread] |
| **Grove_1** The grove edge | LanternGrove | Anchor-points in the branches; the first thread gauntlet. | — | skimmer ×2 | — | W → Chapel_2 [Inkthread], E → Grove_2 |
| **Grove_2** The lanterns | LanternGrove | Eleven lanterns in a ring: the vigil with Teodor. No choices. | Lanterns | — | Teodor, desk | W → Grove_1, up → Grove_3 |
| **Grove_3** The canopy | LanternGrove | Up into the canopy by thread; the forest floor out of sight below. | Canopy | Cantor, skimmer | — | down → Grove_2, E → Grove_4 |
| **Grove_4** The high lanterns | LanternGrove | The grove's crown; a thread line east drops to the library's roof. | — | smudge | — | W → Grove_3, E → Library_1 [Inkthread] |
| **Library_1** The reading stair | SunkenLibrary | Down into a library the forest floor swallowed; anchored, and it shows: the dust does not move. | — | — | — | W → Grove_4 [Inkthread], down → Library_2 |
| **Library_2** The reading room | SunkenLibrary | Brother Ansel on page 214 for thirty-eight years. Teodor will not turn it for you. | Page | — | Teodor | up → Library_1 |
| **Aldermere_1** The lane | Aldermere | Aldermere on its last day: bunting, bread, a desk the inn keeps for travellers. | — | — | desk | W → House_3, E → Aldermere_2 |
| **Aldermere_2** The square | Aldermere | The last evening. Attend it, or try to stop it and the Choir sings over the square (6.6). | Square | — | Teodor, arena: choir | W → Aldermere_1, E → Aldermere_3 |
| **Aldermere_3** The ash field | Aldermere | Where the village is already paper; the canopy road starts over it by thread. | — | Cantor, smudge | — | W → Aldermere_2, E → Gate_1 [Inkthread] |
| **Gate_1** The approach | OvergrownGate | A desk under the roots, then the gate's roots as anchors up the wall. | — | skimmer | desk | W → Aldermere_3 [Inkthread], E → Gate_2 |
| **Gate_2** The Overgrown Gate | OvergrownGate | The Gatekeeper's arena (6.7); beyond it, the canopy road to the Paper Mills. | Gate | — | arena: gatekeeper | W → Gate_1, E → Mills_1 [Inkthread] |
<!-- /table -->

**Built (ENV-04).** All nineteen rooms are scenes from recipes (`ProjectSetup.VerdanceRecipes`), on the forest's kit
(paper-kit.md §2c): the road on `Ground_Moss` under the trunks, with three milestones and the mill's two speakers (Wend and
Tobin, `Road_Solvent`) in the first trees; the House on `Ground_Root` in `Paper_Mid_Roots` with the desk, Teodor's ledger,
Teodor and the tapestry in the cloister and the root stair down from its floor; the chapel with Teodor and a ten-unit gap
east that only a thread crosses, two anchor-points over it; the grove's gauntlet of three anchor-points, the vigil's eleven
lanterns in a ring over Teodor, the canopy by thread and the thread line east; the library on `Ground_Flag` under
`Paper_Mid_Shelves` with Ansel at his lectern and Teodor beside him; Aldermere's lane and square with the bunting, Hollin and
Teodor, and the Choir's arena, which waits for `verdance.aldermere.stopped` (`BossArena.RequiresFlag`) and erases the
square's three platforms; the ash field already paper (`Paper_Mid_Ash`); the gate with the Gatekeeper's arena, its roots as
its anchors, the inn's keeper (a Remnant) and the traveller who asks. Anchor-points are permanent `TetherAnchor`s (the
thread's component) under the kit's knot; the Wingbeat gap to the fields is in Road_1 with a skimmer over it. The coast
gained the Pale Iris Fields (`Saltmarrow_IrisFields`, `Paper_Mid_Irises`, the Irises vantage, seeds) between Reedmother's
crown and the road. Not yet: Gate_2's east road to the Paper Mills (ENV-05); the Reedmother's Brood (6.2); the brothers
who bow and do not speak; Aldermere's after-state (§4).

## 3. Rules these plans follow
- **Gates between zones are the map's.** An exit that crosses from one zone to another carries exactly the
  macro map's gate (ability, flag, soft). Inside a zone, only the ability the zone grants may gate a room.
- **A desk within fifteen seconds of every boss door** (combat doc §8): the arena room has a desk, or the room
  before it does.
- **Hubs have a desk.** Kettil's square, Teodor's cloister.
- **Enemies by answer** (combat doc §7): salamanders and bats in the mine country (pogo the salamanders, strike
  the bats as they dive), crabs and skimmers on the old road, Cantors in the Verdance (the Unwriters' country),
  smudges where something was forgotten (the galleries, the ash field). One Warden patrols the Overlook road:
  the Guild watches the way to the Plateau.
- **Vantages** sit where the view is the point: back at the coast from the landings, the bell over the town, the
  ninth chimney, the Greyfold from the Overlook; the milestone, the cloister, the canopy, Ansel's page.

## 4. Open
- Whether a bounds-walk may cross rooms (Hollowvein's does, four rooms deep); bounds-walk.md has the question.
- Emberdown's second vantage in the chimneys: the ninth chimney's top may belong to the Guild agent's commission
  and not be reachable until it is resolved.
- Aldermere after its last day (ENV-04 builds one state: the script releases the three rooms and they thin with their fade stage, but the square and the bunting stay): attended, the three rooms go to fade stage 4 and the ash field becomes the way
  (the square is gone); stopped, the rooms stay and the Choir's erasure is the scar. Both need a recipe variant.
- Runa at Merrow's End in Act 2 (character-bibles.md §3) needs a way back to the coast from Kettil's Rest
  that is not the whole climb: the Ferrymen, or fast travel only.
