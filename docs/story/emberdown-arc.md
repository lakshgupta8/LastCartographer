# The Emberdown Arc (NAR-07, v1)

Act 1's first climb (bible 4.2, 7.1 step 4, 8.2) as written in Yarn, with the state each scene reads and writes.
It is also Act 2's second region for a player who climbed the Verdance first; nothing here assumes the order.
The rooms are planned, not built (`docs/design/emberdown-verdance-rooms.md`); every scene names the room it
belongs to. Files live in `Assets/_Project/Dialogue/Emberdown/`.

## 1. Beats

| Beat | Room | Scene (node) | Writes |
|---|---|---|---|
| The rescue | Stair_2 | `Stair_Rescue`: Hask, pulled off a landing over a live furnace (after the set piece) | `emberdown.rescue.done` |
| Brann | Stair_3 | `Stair_Brann`: the survey is already written; he only needs to seal it. Then his arena (6.5) | `emberdown.brann.met`, `boss.brann.defeated` |
| The gate | Rest_1 | `Rest_Kettil_First`: "One, two, and a stranger." A stranger is a bird nobody remembers | `emberdown.kettil.met` |
| Counted in | Bell_2 | `Bell_Runa_Count`: "Forty-two, stranger, one." The walk is taught here | `emberdown.runa.counted`, `holdfast.walk_learned` |
| Talonhold | Chimneys_1 | `Chimneys_Runa_Climb`: talon, talon, breathe. Runa will not climb with a stranger: she must be counted first | `emberdown.runa.climbed`, `<<grant Talonhold>>` |
| The ninth chimney | Chimneys_3 | `Chimneys_Ninth_Agent`: Ostry, a season into his posting, twenty-two years ago | `emberdown.ninth.agent_met` |
| The debate | Baths_2 | `Baths_Kettil_Debate` → `Baths_Runa_Debate`: six hundred and twelve, in numbers and in names | `emberdown.debate.heard`, `.sided` (1 Lorne's numbers, 2 Merrow's End, 3 both right) |
| The boards | Rest_2 | `Rest_Kettil_Hollowvein`: once the baths have argued and she knows the walk | `emberdown.hollowvein_opened` (the map's gate) |
| The Long Roll-Call | Rest_3 | `Hollowvein_Runa_Walk`: Runa asks, the first time. Walk it down, or leave it buried | `.chosen` and `<<walk hollowvein>>`, or `.buried` and `.decided` = 2 |
| The bottom | Hollow_4 | The Collapse (6.4); `Hollowvein_Runa_After`: "Thirty-one." She says the number | `.walked`, `.decided` = 1 |
| The Overlook | Overlook_2 | `Overlook_Runa`: the white, counted from outside | `emberdown.overlook.seen` |
| Named | Bell_2 (Act 2) | `Bell_Runa_Named`: once Wren has held a place by walking it, "Forty-two, Wren" | `runa.named_wren` |

After Brann, Kettil counts him out ("One, gone."). After Hollowvein, both Kettil and Runa speak to it every visit.

## 2. Who says what
- **Kettil** (capercaillie, the Holdfast). Image: the count, in numbers. Six hundred and eleven at supper; twelve
  with Wren. Spits at the Guild's name (the stage direction is in the line; there is no emote system yet). Orders
  and laughter; she never pleads.
- **Runa** (character-bibles.md §3). Image: the count, sung, in names. She never says "stranger" after the first
  night and never says "I don't know how".
- **Surveyor Lorne** (crane, the Guild). Careful, correct, and a little lonely. He is right that nobody has gone
  missing on Kettil's count because it has never been tested; Hollowvein is the test.
- **Brann** (6.5). Believes the anchoring is a kindness and has seen a roll-call fail when one voice went.
- **Ostry** (nightjar, Guild agent). The Stillness from inside: an anchored chimney, and a posting that never
  ends. Plant 5.1.
- **Hask** (stair crew). Nine and Wren.

Plants: 5.6 (the chimney remembers who climbed it), 4.6 (the Overlook), 5.1 (Ostry). The keystone under
Hollowvein is the thing that "broke the mine" and "hums": Kettil withholds, never lies.

## 3. Commissions (Kettil's ledger, `CommissionCatalog.Emberdown()`)
| Commission | Posts after | Steps | Reward |
|---|---|---|---|
| The Long Roll-Call [B] | Runa counts her | decide Hollowvein | 3 scraps; island `Hollowvein` |
| The Ninth Chimney [F 5.1] [A Talonhold] | she can climb | survey `Chimneys_3/Ninth`; meet Ostry | 2 scraps |
| The Cinder Bath Debate | Kettil meets her | hear it | 1 scrap |
| Furnace Stair Rescue | Kettil meets her | pull Hask off the ledge | 2 scraps, the plumb weight |
| The Overlook [F 4.6] [A Talonhold] | she can climb | survey `Overlook_2/Overlook` | 1 scrap |

Posting moved to Core (`Commissions.PostAvailable`), so a ledger's rules can be checked without its room.
`EmberdownLedgerTests` also check every ledger's flags against the scripts: a step waiting on a flag no script
or system writes fails the build.

## 4. Engine hooks this arc added
- `<<grant Ability>>` unlocks an ability on Wren (Runa's Talonhold; Teodor's Inkthread next).
- Abilities persist: `AbilitySet.Unlock` writes `ability.<name>`, and the set restores from those flags on enable
  and on load. Before this, a learned ability was not in the save.

## 5. Open
- The rooms: none of these scenes has its room built yet; the tests run them from the persistent scene.
- The Kettil's Rest lesson walk and the Hollowvein walk as `BoundsWalk` objects (bounds-walk.md designs both;
  Hollowvein's crosses four rooms).
- Epilogue lines (`Epilogue_Runa`) wait for NAR-13.
- Merrow's End's hold way: Runa coming to the coast in Act 2 (`emberdown.runa.asked_for_merrow`) is unwritten.
