# Halden Rooms (DES-10, v1)

Room-by-room design for Halden Reach, the Plateau (bible 4.4; world-map.md §3). Same conventions as the other
regions: rooms 40 units wide, 17 high (19 for vertical rooms), floor at y = 0, exits at the edges or through the
floor and ceiling. Halden is reached mid-game from either climb, and it is Act 2's hub; its rooms have to work
three times: first arrival (unlicensed, Act 1's end), the open act (the interludes, the strike, the towers), and
Act 3 (the dome opens).

The plans are data in `RoomPlans` alongside Emberdown's and the Verdance's; the table below is generated from the
same source. `RoomPlanTests` hold all three regions to the macro map, the boss sheets and the cast.

## 1. The map

```
Emberdown Overlook ═W, T═ Bridges_1 ─E─ Bridges_2 ─E─ Bridges_3 (desk) ─E─ Bridges_4 (Halvard, second hunt)
Bridges_2 ─down─ Lowmarket_1 ─E─ Lowmarket_2 (the strike; desk) ─E─ Lowmarket_3 ═E, act2.started═ Windreach
Verdance Gate ═W, I═ Mills_1 ─E─ Mills_2 ─E─ Mills_3 ─E─ Hall_1 ─E─ Hall_2 (hub) ─E─ Hall_3 ─E─ Orchard_1 ─E─ Orchard_2 (the cache)
Bridges_4 ─down─ Mills_2
Orchard_2 ═E, isolde.cache═ the Edge Camp
Orchard_2 ═up, T + I═ Bastion_1 (flyer-tower; desk) ─up─ Bastion_2 (Oriel) ─E─ Bastion_3 (the Guildmaster's window)
Bastion_3 ═E, act3.started═ Observatory_1 (desk) ─E─ Observatory_2 (the frame; the Complete Survey)
Bastion_3 ═down, halden.vault_opened═ Vault_1
```
`T` Talonhold, `I` Inkthread, `T + I` both: the flyer-towers were built for birds who flew, and only a bird with
both climbs' abilities can get up one.

**The shape.** Two ways in (the bridges from the north, the mills from the east) meet at the mills, and the city
runs east from there along one street: mills, Hall, orchard. Lowmarket hangs below the bridges, as it hangs below
the walls. Everything the Guild keeps from her is up: the flyer-tower from the orchard wall, the Guildmaster's
window at its top, the Vault below that window, and the dome, shut until Act 3.

## 2. Two changes to the macro map
- **The Vault opens from the Guildmaster's window, not the dome.** The bible needs Pell to notice the empty slot in
  Act 2 (plant 5.2) before Act 3's reveal. The map had the Vault behind the Observatory, which is Act 3 only;
  `WorldGraph` now joins the Vault to the Bastion, still behind `halden.vault_opened`.
- **Voss's office is reached from the Bastion's flyer-tower.** Bible 8.4 puts the office behind a flyer-tower in
  Act 2; the Observatory is Act 3. The office is the tower's top room (`Bastion_3`), a window into the Observatory's
  office wing; the cast's `Office_Pell_Drawing` scene moves to `Halden.Bastion`.

<!-- table:Halden -->
| Room | Zone | What it is for | Vantage | Enemies | Stands here | Exits |
|---|---|---|---|---|---|---|
| **Bridges_1** The first bridge | SevenBridges | Off the Overlook road onto the Plateau: stone, copper gone green, always late afternoon. A toll-keeper counts coins, not birds. | — | Warden ×2 | — | W → Overlook_2 [Talonhold], E → Bridges_2 |
| **Bridges_2** The toll bridges | SevenBridges | Three bridges over the drop, tolled; the stair down to Lowmarket goes from the second. | Tollhouse | Warden, Cantor | — | W → Bridges_1, E → Bridges_3, down → Lowmarket_1 |
| **Bridges_3** The seventh bridge | SevenBridges | Under repair for forty years; a family is paid to stand on it (the Seventh Bridge). A desk in the repair hut. | Seventh | — | desk | W → Bridges_2, E → Bridges_4 |
| **Bridges_4** The last span | SevenBridges | Halvard's second hunt (6.3): he cuts the span section by section. The mills are below it. | — | — | Halvard, arena: halvard_2 | W → Bridges_3, down → Mills_2 |
| **Mills_1** The mill race | PaperMills | Where the canopy road from the Overgrown Gate comes down: a mill race, wheels, wet paper in the air. | — | Warden, smudge | — | W → Gate_2 [Inkthread], E → Mills_2 |
| **Mills_2** The drying lofts | PaperMills | Sheets of new vellum hung to dry, rooms deep; the Seven Bridges are overhead. | Lofts | smudge ×2 | — | up → Bridges_4, W → Mills_1, E → Mills_3 |
| **Mills_3** The pulp yard | PaperMills | The strike's picket line: the millworkers of Lowmarket have downed tools. The Hall steps are beyond. | — | — | — | W → Mills_2, E → Hall_1 |
| **Lowmarket_1** The stair down | Lowmarket | Below the walls. The paint is thinner here, and so is everything else. | — | smudge | — | up → Bridges_2, E → Lowmarket_2 |
| **Lowmarket_2** Lowmarket | Lowmarket | The district below the walls, fading; its notice board reads 'survey scheduled'. The strike hall, where the decision is made. | Market | — | desk | W → Lowmarket_1, E → Lowmarket_3 |
| **Lowmarket_3** The south gate | Lowmarket | The south road to Windreach, barred until Act 2 opens it. | — | Warden | — | W → Lowmarket_2, E → Stones_1 [`act2.started`] |
| **Hall_1** The Hall steps | JourneymansHall | The Guild's steps. Unlicensed now, she comes in past the Wardens or not at all until Interlude A resolves. | — | Warden ×2 | — | W → Mills_3, E → Hall_2 |
| **Hall_2** The Journeyman's Hall | JourneymansHall | The hub: desk, ledger, Wren's old room. Pell. Tam, who sits his exam next spring, eleven years running. | Hall | — | Pell, desk | W → Hall_1, E → Hall_3 |
| **Hall_3** The exam rooms | JourneymansHall | Rows of desks with the same papers on them (the Master's Exam, plant 5.1). The orchard door at the end. | — | — | — | W → Hall_2, E → Orchard_1 |
| **Orchard_1** The orchard wall | OldOrchard | The only place in Halden with fallen leaves. Somebody rakes them. | — | — | — | W → Hall_3, E → Orchard_2 |
| **Orchard_2** The Old Orchard | OldOrchard | Isolde's cache in the roots; the Orchard Keeper; a gravestone with a crest on it. The flyer-tower rises from its wall; the road to the Edge begins here. | Leaves | — | Isolde | W → Orchard_1, E → EdgeCamp_1 [`isolde.cache`], up → Bastion_1 [Talonhold + Inkthread] |
| **Bastion_1** The flyer-tower | Bastion | A tower built for flyers: no stairs. Talonhold up the walls, Inkthread across the gaps. A desk on the top landing; the Crown's hall behind it (Interlude B). | — | Warden | Maren, desk | down → Orchard_2 [Talonhold + Inkthread], up → Bastion_2 |
| **Bastion_2** The drill-yard | Bastion | Oriel's arena if Pell's report was sent (6.8); otherwise an empty yard with chalk lines. | Yard | — | arena: oriel | down → Bastion_1, E → Bastion_3 |
| **Bastion_3** The Guildmaster's window | Bastion | The tower's top window opens into Voss's office in the Observatory wing: the chick's drawing, framed (plant 5.5). The dome itself is shut. | — | — | Pell | W → Bastion_2, E → Observatory_1 [`act3.started`], down → Vault_1 [`halden.vault_opened`] |
| **Observatory_1** The dome stair | Observatory | Act 3: the dome opens. A desk under the stair. | — | Warden ×2 | desk | W → Bastion_3 [`act3.started`], E → Observatory_2 |
| **Observatory_2** The frame | Observatory | The frame of the shattered Atlas; the keystone in it; the ending's choice and, in the true ending, the Complete Survey (6.15). | Frame | — | Pell, Voss, Runa, Teodor, arena: complete_survey | W → Observatory_1 |
| **Vault_1** The Vault | Vault | Seven slots, reached from the Guildmaster's window; one empty (Pell counts them, plant 5.2). | — | — | Pell | up → Bastion_3 [`halden.vault_opened`] |
<!-- /table -->

**Built (ENV-05).** All twenty-one rooms are scenes from recipes (`ProjectSetup.HaldenRecipes`), on the Plateau's kit
(paper-kit.md §2d): the bridges on `Ground_Granite` under the balustrade, spans a jump apart over `Paper_Far_Drop`, the
toll-keeper's Wardens awake (Halden is anchored, so no `HeldState`: every Warden patrols), the seventh's scaffold and
the Arden family, Halvard before the last span and his second hunt's arena behind two doors (the same `Halvard` kit as
the chapel's; the bridge-cutting is CMB's), waiting for `act2.started`; the mills on `Ground_Boards` with wheels and
sheets, the lofts' climb up to the span; Lowmarket on `Ground_Cobble` with its board, Brisk and Anvers at the strike;
the Hall on `Ground_Parquet` with the desk, the Guild's ledger, Pell, the roll, the exam desks and Tam; the orchard with
Isolde's cache (her pages, a stand-in: she is not drawn), the Keeper and the gravestone, and the flyer-tower rising from
its east wall as two Talonhold walls and a thread point; the Bastion: the tower's inside (walls, two anchor-points, a
desk on the top landing, Maren at its foot), Oriel at the yard's edge and her arena waiting for `pell.report_sent`, the
window into Voss's office with Pell; the dome stair and the frame with Pell, Voss, Runa and Teodor around it, the
Complete Survey's arena waiting for `ending.chorus_led`; the Vault with its seven slots and Pell counting. Both climbs
come down onto it: Overlook_2 → Bridges_1, Gate_2 → Mills_1. The epilogue walk now goes to Pell's Hall. The south
gate's road onto the Steppe is built (ENV-07): Lowmarket_3 → Windreach_Stones_1. Not yet: the orchard's road to the Edge
Camp (ENV-08); Lowmarket's faded variant; the
Crown hall as a room; the chalk lines of an empty yard. The map's doors read their flags now (`gates.md`).

## 3. Rules these plans follow
- The map's gates, exactly, between zones (tested). Only the zone's own ability gates rooms inside it; Halden
  grants none, so every gate in the city is between zones.
- A desk within fifteen seconds of every boss door: the seventh bridge's hut before Halvard's span, the flyer-tower's
  landing before Oriel's yard, the dome stair before the frame.
- **Unlicensed.** From Act 1's end Wren is unlicensed (`Licence`), and Halden is anchored: every Warden here is
  hostile until Oriel stands them down (6.8) or the report is kept. Wardens stand on the bridges, the mill race,
  the Hall steps, the south gate and the dome stair; the Hall itself (the hub) has none.
- **Always late afternoon.** Halden is anchored, so its hour is locked (hub-life.md): the whole region holds one
  phase of the day, and its people loop.

## 4. Open
- Interlude B's Crown hall is described behind the flyer-tower's landing but is not its own room; if the audience
  needs a stage, the Bastion grows to four rooms.
- The Seventh Bridge's family and the Orchard Keeper are not in the cast yet (NAR-09 will add them if they speak).
- Lowmarket after a failed strike goes to the Blank (bible 4.4): the three rooms need a faded variant.
- Where Tam's room is: the exam rooms (`Hall_3`) or Wren's old corridor in the Hall.
