# Windreach, the Greyfold and the Blank: Rooms (DES-11, v1)

Room-by-room design for the last three regions (bible 4.5–4.7; world-map.md §3), and the end of the paper map:
with these, every authored room outside Saltmarrow is planned (96), and with Saltmarrow's 27 the map's 123. Same
conventions as the other regions: rooms 40 units wide, 17 high (19 for vertical rooms), floor at y = 0, exits at
the edges or through the floor and ceiling.

The plans are data in `RoomPlans` with the other regions'; the tables below are generated from the same source.
`RoomPlanTests` hold all six regions to the macro map, the boss sheets and the cast, and walk the story's order
through them: Act 1's end, Act 2 with and without each late ability, the climax, the crossing, Act 3.

## 1. The map

```
Halden Lowmarket_3 ═E, act2.started═ Stones_1 ─E─ Stones_2 ─E─ Stones_3 (Hale) ─E─ Camp_1 (desk) ─E─ Camp_2 (Idrenne)
Camp_2 ═E, Wingbeat (soft)═ River_1 ─E─ River_2 ─E─ River_3 ═up, T═ Gate_1 (the leap: Windmemory)
Gate_1 ═E, W═ Gate_2 ═E, W═ Fire_1 ─E─ Fire_2 (Idrenne; the keystone) ═down, W═ Star_1 (desk) ─E─ Star_2 (the Fallen Star)
Gate_2 ═down, W═ Greyfold Pool_1                                       (the glide into the white)

Halden Orchard_2 ═E, isolde.cache═ EdgeCamp_1 ─E─ EdgeCamp_2 (hub, desk) ─E─ Edge (the prologue; desk) ─E─ Cathedral_2 (the bells)
Cathedral_2 ─E─ Road_1 ─E─ Road_2 ─E─ Road_3 (Clarity; Pell watches) ═E, C═ Pool_1 ─E─ Pool_2 (Marrow in the water)
Pool_2 ═E, C, act2.threshold═ Threshold_1 (Halvard; desk) ─E─ Threshold_2 (Voss)
Threshold_2 ═E, greyfold.crossed═ LastCamp_1 (Isolde's atlas; desk) ═E, act3.started═ the Blank

LastCamp_1 ═E═ Hollow_1 (the Lantern) ─E─ Hollow_2 (hub: Ilse, Isolde; desk) ─E─ Hollow_3 (the drift)
Hollow_3 ─E─ Capital_1 (desk) ─E─ Capital_2 (Corra's Drawing) ─E─ Capital_3 (desk) ─E─ Capital_4 (the Archivist)
Hollow_3 ═down, C, act3.started═ Aury_2 (Aury, Sable) ─W─ Aury_1 ═W, saltmarrow.tether═ Saltmarrow's Lantern Chain
```
`T` Talonhold, `W` Windmemory, `C` Clarity.

**Windreach** is one long walk east, the clans' route. The stones, the camp, the dry river, the cliff; then up, and
from the Wind Gate everything is by air: the glide north to the high grass and Idrenne's Fire, down into the
crater, and the long glide south-east into the Greyfold at the Mirror Pool (the map's "Greyfold approach").

**The Greyfold** is one road that runs out. The orchard road ends at the Edge Camp; the Edge is the prologue's
room; the nave, the road in, the place it stops. Clarity is learned where the road ends, and everything past that
wants it. The Threshold is two rooms, the Guild's line and the line itself.

**The Blank's fixed islands** hang off the Hollow: the capital east, Aury's light below. Every other island is
generated from what she left unanchored (PRG-20) and drifts past the Hollow's far edge.

## 2. Three changes to the macro map
- **Isolde's Last Camp is across the Threshold, not beside the Edge Camp.** The map had it one Clarity gate from
  the Edge Camp, which Act 2 has, and its scene starts Act 3 (`act3.started`, which also opens the Observatory).
  An Act 2 player could have skipped the climax. Bible 7.3 puts the camp first after the crossing, so it is: the
  Threshold leads to the camp (`greyfold.crossed`), and the camp to the Blank (`act3.started`). The Lantern (7.3
  step 2) is the Blank's first room.
- **Aury's island reaches the Hollow only in Act 3.** Aury's lighthouse is reached by tether in Act 2
  (`saltmarrow.tether`); the map joined it to Thessaly Hollow with Clarity alone, a second back door into Act 3.
  The join now also wants `act3.started`: in Act 2 his island is a dead end with a keystone in it.
- **Voss's scene writes `greyfold.crossed`.** It wrote `act2.threshold`, which is the gate *to* the Threshold, so
  the climax could never open. `act2.threshold` is Act 2 calling its climax; what writes it (the Edge Camp, once
  she chooses to go) is NAR-11's.

<!-- table:Windreach -->
| Room | Zone | What it is for | Vantage | Enemies | Stands here | Exits |
|---|---|---|---|---|---|---|
| **Stones_1** The south road's end | NineStones | Out of Lowmarket's south gate onto the Steppe: grass to the horizon, sky most of the screen. The first standing stone, lichen on its north face. | Waymark | Warden (out of uniform: Hale's escort) | — | W → Lowmarket_3 [`act2.started`], E → Stones_2 |
| **Stones_2** The long walk | NineStones | Stones two to eight in the line the clans have walked since before the Guild; the stones are a map (the Nine Stones). Ink-swirl updrafts, too weak to ride yet. | Fifth | smudge ×2 | — | W → Stones_1, E → Stones_3 |
| **Stones_3** The ninth stone | NineStones | Where the route turns north. Surveyor Hale at dusk, sighting the stones one by one (6.9, optional); the camp's wagons are just east. | — | — | Hale, arena: hale | W → Stones_2, E → Camp_1 |
| **Camp_1** The wagons | LongGrassCamp | The walking-wagons in a ring. One stays at the Long Grass wherever the camp has gone: the walkers' post, with the desk and the ledger. | Wagons | — | desk | W → Stones_3, E → Camp_2 |
| **Camp_2** The fire ring | LongGrassCamp | The camp's first night (the Moving Camp). Idrenne tells where she was standing when she learned each thing. The camp moves on to the riverbed, then the high grass. | — | — | Idrenne | W → Camp_1, E → River_1 [Wingbeat (soft)] |
| **River_1** The far bank | DryRiver | A river with no water, a Wingbeat wide at the camp's edge (soft: a pogo off the dead reed-heads crosses it). | — | smudge | — | W → Camp_2 [Wingbeat (soft)], E → River_2 |
| **River_2** The riverbed | DryRiver | Cracked mud, boats on their sides; the camp's second night pitches here. Smudges in the boats: things the river forgot it carried. | Bed | smudge ×2 | Idrenne | W → River_1, E → River_3 |
| **River_3** The cut bank | DryRiver | The river's old cliff. The Wind Gate is at the top, and only Talonhold climbs it. | — | smudge | — | W → River_2, up → Gate_1 [Talonhold] |
| **Gate_1** The leap | WindGate | The fledgling-leap on the cliff's lip: the clan sings, the young jump. Nobody has lived through it in forty years until Wren. Windmemory. | Gate | — | Idrenne | down → River_3 [Talonhold], E → Gate_2 [Windmemory] |
| **Gate_2** The updrafts | WindGate | Ink-swirls to ride: the first glide course. North, the high grass; down the far side, a long glide into the Greyfold's white at the Mirror Pool. | — | — | — | W → Gate_1 [Windmemory], E → Fire_1 [Windmemory], down → Pool_1 [Windmemory] |
| **Fire_1** The high grass | IdrennesFire | Grass over Wren's head; the camp's third night pitches here, and the clan walks her in. | — | — | Idrenne | W → Gate_2 [Windmemory], E → Fire_2 |
| **Fire_2** Idrenne's Fire | IdrennesFire | The hearth; the keystone is its cooking-stone. Idrenne says plainly how the clans do it (plant 9.2) and gives it up laughing. Surveying Windreach at all is decided here. | Hearth | — | Idrenne | W → Fire_1, down → Star_1 [Windmemory] |
| **Star_1** The crater rim | FallenStar | A glide down from the hearth to the rim; the smiths' wagon keeps a desk. | Rim | — | Idrenne, desk | up → Fire_2 [Windmemory], E → Star_2 |
| **Star_2** The anvil-crater | FallenStar | The Fallen Star, forty years the clans' anvil, the hearth's heat run into its iron. Lift the stone at the Fire and it wakes (6.10, optional). | — | — | arena: fallen_star | W → Star_1 |
<!-- /table -->

<!-- table:Greyfold -->
| Room | Zone | What it is for | Vantage | Enemies | Stands here | Exits |
|---|---|---|---|---|---|---|
| **EdgeCamp_1** The orchard road's end | EdgeCamp | The road from the Old Orchard stops at a Guild fence with no gate. Beyond it the paper is white; buildings show only at the edge of the eye. | — | — | — | W → Orchard_2 [`isolde.cache`], E → EdgeCamp_2 |
| **EdgeCamp_2** The Edge Camp | EdgeCamp | The abandoned Guild outpost: tether-posts, a ledger nobody posts to, Isolde's initials cut in a beam. The hub; the last place colour reaches by itself. | Outpost | — | desk | W → EdgeCamp_1, E → Edge |
| **Edge** The Edge | HalfCathedral | The prologue's room, built (the greybox `Greyfold_Edge`): Isolde's desk; survey, bind, seal; then she walks in. Act 1 ends here too. | HalfCathedral | — | Isolde, desk | W → EdgeCamp_2, E → Cathedral_2 |
| **Cathedral_2** The nave | HalfCathedral | Half a cathedral, white; the Road That Stops runs down its nave. Thirty steps in, a grey chick. With Clarity, the bells ring (6.12). | — | lost Remnant ×2 | Marrow, arena: bells | W → Edge, E → Road_1 |
| **Road_1** The road in | RoadThatStops | Cobbles that fade a stride at a time. Platforms are drawn only inside Wren's lantern-radius; outside it, outlines. | — | smudge ×2 | — | W → Cathedral_2, E → Road_2 |
| **Road_2** The mileposts | RoadThatStops | Mileposts for a road nobody finished, each one nearer to nothing. | Milepost | smudge, lost Remnant | — | W → Road_1, E → Road_3 |
| **Road_3** Where it stops | RoadThatStops | The road ends mid-stride. She steps off and stays herself: Clarity. Pell, sent to watch, sees her come back (the act break). | — | — | Pell | W → Road_2, E → Pool_1 [Clarity] |
| **Pool_1** The white shore | MirrorPool | A beach of white paper; the glide from the Wind Gate lands here from above. Colour only in her radius. | — | lost Remnant, smudge | — | up → Gate_2 [Windmemory], W → Road_3 [Clarity], E → Pool_2 |
| **Pool_2** The Mirror Pool | MirrorPool | Water that shows what is not on the bank: a grey chick in the reflection, none beside her. | Pool | — | Marrow | W → Pool_1, E → Threshold_1 [Clarity, `act2.threshold`] |
| **Threshold_1** The Guild's line | Threshold | Tethers staked across the white, Wardens in a line, the Guild's field desk behind them. Halvard's third fight at the edge (6.3). | — | Warden ×3 | Halvard, desk, arena: halvard_3 | W → Pool_2 [Clarity, `act2.threshold`], E → Threshold_2 |
| **Threshold_2** The Threshold | Threshold | The line itself. Voss, going in himself at last (6.11); Pell, if the report was kept. On the Return, Marrow echoes him. | — | — | Voss, Pell, Marrow, arena: voss | W → Threshold_1, E → LastCamp_1 [`greyfold.crossed`] |
| **LastCamp_1** Isolde's Last Camp | IsoldesLastCamp | Just across the line: her tent, her lamp still lit, her complete atlas (reveal 5.2). Act 3 starts here; the Lantern leads on into the Blank. | Atlas | — | Isolde, desk | W → Threshold_2 [`greyfold.crossed`], E → Hollow_1 [`act3.started`] |
<!-- /table -->

<!-- table:Blank -->
| Room | Zone | What it is for | Vantage | Enemies | Stands here | Exits |
|---|---|---|---|---|---|---|
| **Hollow_1** The Lantern | ThessalyHollow | Inside. White, and colour blooming round Wren as she walks; the first island drifts up under her feet. A grey chick starts following. | — | lost Remnant | Marrow | W → LastCamp_1 [`act3.started`], E → Hollow_2 |
| **Hollow_2** Thessaly Hollow | ThessalyHollow | Wren's birth village, grey. Ilse sees her first (reveal 5.3). Isolde, grey at the edges, cannot leave. The hub: a desk in Ilse's house. | — | — | Ilse, Isolde, desk | W → Hollow_1, E → Hollow_3 |
| **Hollow_3** The drift | ThessalyHollow | The Hollow's far edge, where the islands of every place she left unanchored drift past (PRG-20). The capital lies east; Aury's light below. | — | lost Remnant ×2 | — | W → Hollow_2, E → Capital_1, down → Aury_2 [Clarity, `act3.started`] |
| **Capital_1** The district's edge | OldCapital | Streets of the old capital, half-drawn. A desk in the doorway of what was a Guild office. | — | lost Remnant | desk | W → Hollow_3, E → Capital_2 |
| **Capital_2** Corra's room | OldCapital | A white room with a crayon floor. A child's drawing of her father, huge and wrong, keeps everyone out (6.13). | — | — | arena: corras_drawing | W → Capital_1, E → Capital_3 |
| **Capital_3** The mirror streets | OldCapital | The district open: the capital's streets reversed, the Observatory's mirror-half at their end. A desk on its steps. | — | lost Remnant ×2 | desk | W → Capital_2, E → Capital_4 |
| **Capital_4** The mirror-Observatory | OldCapital | Corvin with the seventh keystone; he has drawn her a chair (reveals 5.4, 5.6). The choice laid out; the Archivist (6.14). Marrow echoes him. | — | — | Corvin, Marrow, arena: archivist | W → Capital_3 |
| **Aury_1** The tether's end | AurysLighthouse | From the Lantern Chain's third lighthouse by tether (Act 2): a causeway into the white, the light still turning. | — | — | — | W → Saltmarrow.LanternChain [`saltmarrow.tether`], E → Aury_2 |
| **Aury_2** Aury's lamp room | AurysLighthouse | Aury, who asks if you've eaten, the keystone in his wings. In Act 3 his island drifts to the Hollow, and Sable sits with him. | — | — | Aury, Sable | up → Hollow_3 [Clarity, `act3.started`], W → Aury_1 |
<!-- /table -->

## 3. Rules these plans follow
- The map's gates, exactly, between zones (tested). Inside a zone only its own ability gates a room: the Wind
  Gate's updrafts want Windmemory, which the leap in the same zone teaches. The Road That Stops has no gate inside
  it, because Clarity is learned at its end.
- A desk within fifteen seconds of every boss door: the camp's wagons for Hale, the smiths' wagon for the Star,
  the Edge's desk for the bells, the Guild's field desk at the line for Halvard and Voss, the capital's two for
  Corra's Drawing and the Archivist.
- **Windreach is not anchored.** Its hour moves (hub-life.md) and its hub moves with it: the camp's first night is
  at the fire ring, the second in the riverbed (`River_2`), the third in the high grass (`Fire_1`), PRG-21. One
  wagon stays at the Long Grass as the walkers' post, so the hub's desk is always where the map says. No Wardens
  in uniform: Hale's escort walks the Steppe out of it.
- **Surveying Windreach is the region's decision.** Seven vantages stand here as everywhere, and surveying the Steppe
  would let the Guild anchor it (bible 4.5). Hale is finishing that survey in secret; Idrenne's Fire is where
  Wren decides whether to finish it herself.
- **The Greyfold is drawn round her lantern.** Colour and platforms exist inside Wren's lantern-radius (about six
  units); outside it, outlines at the edge of the eye (art-direction.md). A room here is designed twice: the
  platforms, and what can be seen of them from each one. The bells (6.12) shrink the radius; Clarity grows it.
- **The Blank cannot be surveyed.** No vantages; its islands are held or lost by what she did outside. Its enemies
  are the lost, the few Remnant who are hostile; most of the grey are not.
- **The built prologue room is its own plan.** `Greyfold_Edge` (the greybox) is the Half-Cathedral's first room
  and keeps its id and its vantage (`Greyfold_Edge/HalfCathedral`).

## 4. Open
- The Nine Stones as a map: which stone is which place (NAR-10), and whether surveying all nine is a vantage set
  or a commission step.
- Hale's escort and the lost Remnant are enemy families without kits (CMB).
- The fledgling-leap as a set piece: a jump that has to fail before Windmemory arrives (the Lamp-Keeper's pattern).
- What the Edge Camp's ledger posts: the Greyfold's commissions are not in the bible.
- The Blank's generated islands: one scene per released place, from the saved `PlaceFate` (PRG-20; NAR-14 writes
  them).
