# The Saltmarrow Arc (NAR-04, v1)

Act 1's first push (bible 7.1 steps 1–3) as it is written in Yarn, with the state each scene reads and
writes. Style guide rules apply throughout: twenty words a line, three choices, the cryptic register, one
image per speaker. Files live in `Assets/_Project/Dialogue/Saltmarrow/`.

## 1. Beats

| Beat | Where | Scene | Writes |
|---|---|---|---|
| Wakes on the shore | Quay (A), the shore wake cutscene | `Prologue_Shore_Wake` (Greyfold file; prologue's last node) | `saltmarrow.met_sable`, `prologue.woke_on_shore`, `act1.started`, dawn |
| The Drowned Quay | Sable at the quay | `Quay_Sable_First`: the Guild has declared Isolde dead and a journeyman missing, the same day | `saltmarrow.sable.talked` |
| The board | Sable, any visit | `Quay_Sable_Again`: the ledger, the reed, the whale (Bone Bridge step 2), the widow, the shop, the third lighthouse | `saltmarrow.sable.ledger / reed / aury`, `saltmarrow.bone_bridge.heard`, `saltmarrow.widow.decided` |
| Merrow's End | Dotha at her stoop | `Merrow_Dotha` → `Merrow_Dotha_Season`: nine songs of eleven; write it as it was, let it go, or walk it | `saltmarrow.dotha.met / songs / remembered / decided` (1, 2, 3), `<<fade Saltmarrow_B 2>>`, `<<walk merrows_end>>` |
| The Lantern Chain | The fourth lighthouse | The Lamp-Keeper's three lines (boss); the lamp's inscription `Lighthouse_Lamp` | `boss.lamp_keeper.defeated`, the beacon vantage |
| Halvard | The fourth lighthouse, the moment the lamp is lit | `Lighthouse_Halvard_Hunt`: three paces; missing carries no licence; surrender the pages at Halden or be a hunt | `act1.halvard_met`, `act1.unlicensed` |

After Halvard, Sable's return line changes ("Prices are the same. That's the one thing the Guild can't fix.").
Wardens in anchored towns become hostile to an unlicensed cartographer (anchoring.md; the flag is
`act1.unlicensed`, wiring is PRG-13's open item).

## 2. Who says what

- **Sable** (cormorant, Ferrymen). Image: prices. She names a cost in nearly every exchange and ends the talk.
  Plants: the Guild's haste (5.2, twice in `First`, once through Halvard), the lamp remembering (5.6), Aury
  ("Keeps it. He doesn't know the difference.", 4.1), the whale's names (3.4). She never says "I hope".
- **Dotha** (last elder). Image: the songs, counted. Eleven, nine, "decide before it's eight". She never asks
  twice. Her three ways are the regional decision in miniature: anchor (write it as it was, then seal at the
  desk), release (let it go; the reeds start washing at once), hold (the walk, once the whale has been heard).
- **The Lamp-Keeper** (Remnant gannet). Three lines, under twelve words: "The light stays." / "I remember the
  light. I remember nothing else." / "If it goes out, I go with it."
- **Halvard** (Warden-Sergeant, heron). Image: paces and counts. Formal, exact; enters silence as a plea. He
  never raises the lance in this scene; the first of three meetings (6.3), each with new kit.
- **The lamp's inscription** carries the Grounding plant (5.6) instead of a person: "Lit for the flock, going
  south."

## 3. Commissions (the five, copy in `Commissions.cs`)
Lantern Chain (the Guild, by proxy), The Bone Bridge (unsigned; the whale, Sable's second step), The Iris
Harvest (a grower; prices follow), Dotha's Last Season (Merrow's End, what is left of it; posts after the shore),
The Tether-Widows (the tether-post; posts after the Lamp-Keeper; answered through Sable).

## 4. Open
- Sable in the Blank at Aury's lighthouse (8.6) and the widow's own lines: she is only ever spoken for here.
- The Ferrymen as a place: two more voices on the quay (a tether-seller, the boy who counts boats).
- The Salt Chapel and the Bone Bridge scenes beyond the slice (DES-08 §3).
- Localization keys (NAR-18): lines are still literal.
