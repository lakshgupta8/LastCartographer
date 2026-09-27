# Economy (DES-05, v1)

What Wren's growth costs and where the money comes from. No XP (GDD 7). Runtime: `Economy` (Core), `IrisSeed`,
`IrisSeedDrops`, `Shops` (World), `ShopView` and the desk's Masks and Belt rows (UI), `<<shop hub>>` and `seeds()` (Yarn).

## 1. Two currencies

| | Iris seeds `✿` | Vellum scraps |
|---|---|---|
| **From** | enemies (below), seeds lying in the fields and on high places | bosses, Commissions, hidden caches |
| **Spent on** | Instruments at hub shops | masks and the fourth belt slot, at any desk; quill upgrades at the Halden smith (later) |
| **Where kept** | `Numbers["$iris_seeds"]` | `Numbers["$vellum_scraps"]` (Commissions) |
| **Feel** | small, frequent, spent freely | rare, counted, each one a decision |

Bound memories are a third currency in the story's sense (bible 10), not a number: DES-06 and NAR-13 own them.

## 2. Seeds
| Family | Seeds |
|---|---|
| Marsh crab, reed skimmer | 1 |
| Smudge, Warden | 2 |
| Cantor | 3 |
| The Lamp-Keeper (and bosses generally) | 10 |
| Placed in rooms | 2–5 per cache; the Pale Iris Fields are the coast's purse (DES-08 §3) |

Seeds drop where the enemy fell and bob until walked through. They belong to the room's scene and go with it.
Enemies respawn with rooms (GDD 7), so seeds are farmable by design; the numbers above are small enough that
farming is slower than playing.

## 3. Shops
Hubs sell Instruments for seeds. Sable's table at the Drowned Quay:

| Instrument | Price | Pitch |
|---|---|---|
| Field lantern | 12 | Honest light. The Guild's, before it was mine. |
| Iris tincture | 8 | Pressed from the pale iris. Don't ask whose fields. |
| Wax seal | 6 | Guild wax. They'd hang me for having it. |
| Tether-hook | 15 | A Ferryman's hook. It has held people over worse than a drop. |

Wren starts with the Compass-dart, the Plumb weight and the Sighting lens. Bought Instruments are owned for
good; uses still come back only at desks. **Ferrymen prices follow the Iris Harvest** (bible 6.2, 8): if the
fields burn (`saltmarrow.iris_burned`, or the Iris Harvest commission failed) everything Sable sells costs half
again, rounded up. Later hubs sell what their region makes (Kettil's Rest: plumb weights and tether-hooks; the
Quiet House: tincture; Halden: nothing, the Guild licenses it).

Yarn: `<<shop Saltmarrow>>` in a conversation; the page opens when the talk ends. `seeds()` reads the purse.

## 4. Upgrades at the desk
| Row | Cost | Rule |
|---|---|---|
| **Masks** | 3 scraps each | 5 → 9, four times. A bought mask is full at once. Charter passives apply on top (Warden +1, Drifter capped at 4). |
| **Belt** | 4 scraps | The fourth Instrument slot, once. |

Quill upgrades (three, GDD 7) are the Halden smith's and cost keystone-adjacent vellum: DES-10.

## 5. WorldState
`$iris_seeds`, `$mask_upgrades`, `$vellum_scraps`; `Equipment.FourthSlotUnlocked`, `Equipment.OwnedInstruments`.
Saves need nothing extra.

## 6. Greybox bindings
- Sable's "What are you selling?" opens her table. The lantern, tincture, seal and hook are hers; the hidden
  platform in room A needs the lantern now, so the first purchase has a use.
- Seeds lie on the quay's high platform, the shore, the boardwalk's shallows, the faded lighthouse, and at the
  crown of Reedmother's Roots.
- The desk's Masks and Belt rows sit under the Place row; J buys.

## 7. Open
- Iris Harvest as a real scene (NAR-04): stopping the burning keeps prices down; the Brood (6.2) guards the fields.
- Whether seeds should be lost on death (v1: no; the smudge holds memories, not money, GDD 7).
- Hidden vellum caches per region (DES-08 §4 onward) and their count against the four mask upgrades.
- Quill upgrade costs and the smith (DES-10).
