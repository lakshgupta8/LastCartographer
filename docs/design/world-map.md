# World Macro Map (DES-07, v1)

The region graph, the ways between sub-zones and what they ask, the room and vantage counts, and the
sequence-break policy. The same graph lives in code as `WorldGraph` (Core) and `WorldGraphTests` prove the spine
from the data, so this page and the tests read the same numbers. Bible 4.x is the source for names and holdings.

## 1. The spine
```
Prologue: the Edge (Greyfold.HalfCathedral) → the Blank moves her → Saltmarrow.Shore

Act 1   Shore → Quay → Reedmother / Merrow's End → Lantern Chain [Wingbeat]
        → Salt Chapel (Halvard) → Bone Bridge → Emberdown [Talonhold]  ─┐
        → Iris Fields → the Old Road → the Verdance [Inkthread]        ─┴→ Halden (the Plateau)
        → the Old Orchard (Isolde's cache) → the Edge → she steps in [Clarity]   ACT BREAK

Act 2   the other climb; Windreach [Windmemory] (from Lowmarket, act2.started); the flyer-towers
        (Talonhold + Inkthread); keystones; the Threshold (act2.threshold)

Act 3   across the line: Isolde's Last Camp (act3.started) → the Blank: Thessaly Hollow, the Old Capital;
        Aury's lighthouse (by tether from Act 2, joined to the Hollow in Act 3); generated islands
```
Ability gates in order: **Wingbeat → (Talonhold or Inkthread) → the Plateau → the other → Windmemory → Clarity
→ the Sky.** Either climb reaches Halden; the flyer-towers need both. Windreach is Act 2 only.

## 2. Gates

| Kind | Examples | Hard or soft |
|---|---|---|
| **Wingbeat** gap | lighthouse gap, the climb to Emberdown, the iris gap, the Dry River | **soft**: a skilled pogo or wall trick crosses it; the game notices (bible: "quietly rewarded") |
| **Talonhold** shaft | the chimneys, the Wind Gate, the road to the Plateau | hard |
| **Inkthread** anchor | the grove, the Overgrown Gate, the canopy road | hard |
| **Windmemory** updraft | Idrenne's Fire, the Greyfold approach | hard |
| **Clarity** fade | past the Road That Stops, the Threshold, Aury's island to the Hollow | hard |
| **Story flag** | `isolde.cache`, `act2.started`, `act2.threshold`, `greyfold.crossed`, `saltmarrow.tether`, `emberdown.hollowvein_opened`, `act3.started`, `halden.vault_opened` | hard, never soft |

Policy: only Wingbeat gaps are soft. Nothing that changes the story's order (the Plateau, the Edge, Windreach,
the Threshold) can be skipped by movement. Every way is two-way; a gate applies in both directions.

## 3. The regions

Rooms are targets for DES-08 through DES-11; vantages are the atlas's marks (48 in scope).

| Zone | Rooms | Vantages | Holds |
|---|---|---|---|
| **Saltmarrow** (27 rooms, 11 vantages) | | | |
| The Shore | 1 | 1 | start |
| The Drowned Quay | 3 | 1 | **hub**: desk, ledger, Sable, the Ferrymen |
| Reedmother's Roots | 4 | 2 | |
| The Pale Iris Fields | 3 | 1 | the iris gap to the Verdance |
| Merrow's End | 3 | 1 | Dotha; **decision** anchor / hold (needs Emberdown) / release |
| The Lantern Chain | 7 | 3 | **6.1 The Lamp-Keeper**; **Wingbeat**; the third lighthouse by tether (`saltmarrow.tether`) |
| The Salt Chapel | 3 | 1 | 6.3 Halvard's first hunt |
| The Bone Bridge | 3 | 1 | the whale; the climb to Emberdown |
| **Emberdown** (21, 8) | | | |
| The Furnace Stair | 3 | 1 | |
| Kettil's Rest | 3 | 1 | **hub** |
| The Roll-Call Bell | 2 | 1 | the bounds-walk |
| The Nine Chimneys | 4 | 2 | **Talonhold** (Runa) |
| The Cinder Baths | 3 | 1 | the debate |
| The Overlook | 2 | 1 | first sight of the Greyfold; the road to the Plateau |
| Hollowvein | 4 | 1 | 6.4; **keystone**; **decision** (`emberdown.hollowvein_opened`) |
| **The Verdance** (19, 8) | | | |
| The Old Road | 3 | 1 | |
| The Quiet House | 3 | 1 | **hub**: Teodor; **keystone** (his to give) |
| The Root Chapel | 2 | 1 | **Inkthread** |
| The Lantern Grove | 4 | 2 | |
| The Sunken Library | 2 | 1 | the monk and his page |
| Aldermere | 3 | 1 | 6.6 The Choir (optional); **decision** |
| The Overgrown Gate | 2 | 1 | the canopy road to the Plateau |
| **Halden Reach** (21, 8) | | | |
| The Seven Bridges | 4 | 2 | from the Overlook |
| The Paper Mills | 3 | 1 | from the Overgrown Gate |
| Lowmarket | 3 | 1 | **decision**: the strike; the south road to Windreach (`act2.started`) |
| The Journeyman's Hall | 3 | 1 | **hub**: Wren's old room |
| The Old Orchard | 2 | 1 | Isolde's cache (`isolde.cache`) → the Edge |
| The Bastion | 3 | 1 | flyer-towers (Talonhold + Inkthread); 6.8 |
| The Observatory | 2 | 1 | Act 3 (`act3.started`); 6.15; **keystone** |
| The Vault | 1 | 0 | `halden.vault_opened`; below the Guildmaster's window in the Bastion (Act 2), not the dome (DES-10) |
| **Windreach** (14, 7) | | | |
| The Nine Stones | 3 | 2 | |
| The Long Grass Camp | 2 | 1 | **hub** (moves between three sites, PRG-21) |
| The Dry River | 3 | 1 | |
| The Wind Gate | 2 | 1 | **Windmemory**; the Greyfold approach |
| Idrenne's Fire | 2 | 1 | **keystone**; **decision**: survey at all |
| The Fallen Star | 2 | 1 | 6.10 (optional) |
| **The Greyfold** (12, 5) | | | |
| The Edge Camp | 2 | 1 | **hub** |
| The Half-Cathedral | 2 | 1 | the prologue; 6.12 |
| Isolde's Last Camp | 1 | 1 | Act 3's first scene; across the Threshold (`greyfold.crossed`), not beside the Edge Camp (DES-11) |
| The Road That Stops | 3 | 1 | **Clarity** (Act 1's end) |
| The Mirror Pool | 2 | 1 | |
| The Threshold | 2 | 0 | 6.11 Voss (`act2.threshold`, then `greyfold.crossed`) |
| **The Blank** (9 fixed) | | | |
| Thessaly Hollow | 3 | 0 | **hub**: Ilse, Isolde |
| The Old Capital District | 4 | 0 | 6.13, 6.14; **keystone** |
| Aury's Lighthouse | 2 | 0 | **keystone** |
| generated islands | — | 0 | every unanchored place (PRG-20) |

Totals: 123 authored rooms, 47 vantages, 36 sub-zones outside the Blank, six keystones with a home (Aury's,
Hollowvein, Teodor's, the Observatory, Idrenne's, and Corvin's in the Old Capital), one hub per region.

## 4. Room budget
A room is one additive scene and one Addressables bundle (PRG-07). 123 rooms at the greybox's 40-unit width is
the whole map; the Blank adds one generated scene per released place. Vertical slice (M1) is Saltmarrow's first
third: Shore, Quay, Reedmother's Roots, Merrow's End, the Lantern Chain to the fourth lighthouse (about 14 rooms,
7 vantages). The greybox today stands in for four of them.

## 5. Greybox bindings
| Greybox room | Zone |
|---|---|
| `Greyfold_Edge` | Greyfold.HalfCathedral |
| `Saltmarrow_A` | Saltmarrow.Quay |
| `Saltmarrow_B` | Saltmarrow.MerrowsEnd |
| `Saltmarrow_Lighthouse` | Saltmarrow.LanternChain |
| `Saltmarrow_Chapel` | Saltmarrow.SaltChapel |

`WorldGraph.Reachable(abilities, flags, allowSoft)` is the same question the ending matrix (DES-12) and the
full-playthrough matrix (PRO-05) will ask; both should be built on it.

## 6. Open
- ~~The seventh keystone.~~ Settled in NAR-13 (`docs/story/endings.md` §2): the seventh is the stone Isolde stole
  from the Vault's sixth slot (5.2) and still carries in Thessaly Hollow. Seven homes: Aury, Hollowvein, the Quiet
  House, Windreach, Isolde, Corvin, and the Observatory's own, which never leaves the frame.
- Whether Windreach should also open from the Bone Bridge by sea (the Ferrymen) for a fourth Act 2 order.
- ~~The reward for a noticed sequence break.~~ Noticed (`SequenceBreaks`, `BreakWatcher`): a zone she stands in
  that no hard way could have brought her to, only a soft gap, gets one line ("Nobody comes this way on foot…"),
  a scrap of vellum and a flag (`break.noticed.<zone>`), once each. The zone's people say so once, in their first
  scene: Kettil ("Up the stair and no wings to speak of. Ha! I'll count you twice, then.") for the Furnace Stair,
  Teodor for the iris gap onto the Old Road (`GivenMemoriesTests`).
- ~~How the moving camp (PRG-21) changes Windreach's links between its three sites.~~ It doesn't. The sites are rooms
  in three zones, the camp is what stands in them, and the hub's desk stays at the post (`moving-camp.md`).
