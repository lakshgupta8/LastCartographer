# The Halden Arc (NAR-09, v1)

The Plateau (bible 4.4, 7.1 steps 5–6, 7.2, 8.4) as written in Yarn: Act 1's end in the Old Orchard, and Act 2's
hub with both interludes, the strike, and Pell. The rooms are planned (`docs/design/halden-rooms.md`); every
scene names its room. Files live in `Assets/_Project/Dialogue/Halden/`.

## 1. Beats

| Beat | Room | Scene (node) | Writes |
|---|---|---|---|
| The cache | Orchard_2 | `Orchard_Isolde_Cache`: her pages. The leaves, the stalls, the king who is nine. Five names. Reveal 5.1 | `isolde.cache` (the map's gate to the Edge), `halden.orchard.cache_read` |
| The keeper | Orchard_2 | `Orchard_Keeper` (the style guide's own sample, made real); `Orchard_Gravestone` | `halden.orchard.keeper_met` |
| The second hunt | Bridges_4 | `Bridges_Halvard_Hunt`: four paces now; then his second kit (6.3) | `act2.halvard_second` |
| The minder | Hall_2 | `Hall_Pell_Minder`: three things, practised. Pell walks with her and reports where she went | `halden.hall.pell_minder` |
| Interlude A | Hall_2 | `Hall_Pell_Report` → `Hall_Pell_Read` / `Hall_Pell_Send` | `pell.report_read`, `pell.report_sent` or `pell.report_kept`, `pell.report_decided` |
| The strike | Lowmarket_2 | `Lowmarket_Strike` → mediate, the walk, or stay out | `halden.strike.decided` 1 anchored / 2 walk taught / 3 released |
| Interlude B | Bastion_1 | `Bastion_Maren_Audience`: make Halden permanent. Refuse, accept, or tell her about the cygnet (only after the cache) | `halden.maren.decided` 2 / 1 / 3 |
| Oriel | Bastion_2 | `Bastion_Oriel`: only if the report went. "Pell writes well." Then 6.8 | `halden.oriel.met` |
| The office | Bastion_3 | `Office_Pell_Drawing`: a chick's drawing of a heron; he has never surveyed anything (5.5); Pell takes the Vault key | `halden.office.pell_ledgers`, `halden.vault_opened` |
| The Vault | Vault_1 | `Vault_Pell_Slot`: six. Seven minus one. The dust in the empty one is younger (5.2) | `halden.vault.pell_counted` |
| The exam | Hall_3 | `Hall_Tam`: eleven identical books (5.1) | `halden.exam.notes_read` |
| The seventh bridge | Bridges_3 | `Bridges_Family`: "Under repair. We are the repair." | `halden.bridge.family_met` |

## 2. The five names
Bible 7.2 calls Act 2 "The Five Names" and the true ending needs "three of the five names alive and allied" (9.2),
but the bible never lists them. Isolde's pages do, and this arc fixes them: **Sable** (who crosses), **Runa**
(who walks), **Teodor** (who lets go), **Idrenne** (who never needed any of it), and **Pell** (if Pell will). One
per region, each a way of holding or releasing a place that is not the Guild's. The ending matrix (DES-12) should
use this list; the bible should adopt it or replace it.

## 3. Interlude A: how Pell decides
The rule in character-bibles.md §1, now running: if Wren **reads** the report before Pell decides, and she has
answered in the **Warden's** voice at least twice **in Halden**, Pell keeps it ("You'd have carried me out."). If she
reads it and has not, or never reads it, Pell sends it, and says so to her face. Sent: `pell.report_sent`, which
`Licence` already reads: Wardens hunt her in every anchored town, and Oriel receives her in the drill-yard. Beaten
there without a mask lost, Oriel stands them down, and that outranks the report (CMB-14). Kept: the yard is closed.

This needed a tally of Wren's voices, which the bible promises (2.3: "they colour NPC replies and decide the last
line of the epilogue") and nothing counted until now.

## 4. Wren's voices (`Voices`, new)
A script marks a choice with `<<voice surveyor|warden|drift scope>>`; the tally is kept per game and per scope
(`voice.warden`, `voice.halden.warden`). Yarn reads it with `voice("warden")` and `voice_in("halden", "warden")`;
`Voices.Dominant` gives the most-chosen, which is what the epilogue's last line will use (NAR-13). Every Halden
choice set is marked; the earlier regions' scripts are not yet (an open item, and a small one: one line per choice).

## 5. The strike
Lowmarket wants a survey; the owners will not pay. **Mediate** and the owners pay: anchored, the same forever, and
Brisk is not sure he is glad. **Teach the walk** (offered dimmed until Wren knows it): the workers hold it
themselves; the place is held when the walk is walked, not by a verb. **Stay out** and the strike breaks: the three
rooms are released and Lowmarket is an island in the Blank (`Lowmarket`, the commission's [B]).

## 6. Commissions (the Hall's ledger, `CommissionCatalog.Halden()`)
| Commission | Posts after | Steps | Reward |
|---|---|---|---|
| The Paper Mill Strike [B] | the minder | settle it | 3 scraps; island `Lowmarket` |
| The Master's Exam [F 5.1] | the minder | Tam's notes | 1 scrap |
| The Seventh Bridge | arrival | survey `Bridges_3/Seventh`; speak to the repair | 1 scrap |
| The Orchard Keeper [F 5.4] | arrival | speak to her | 2 scraps |
| Voss's Office [F 5.5] [A Inkthread] | the minder | the Guildmaster's window with Pell | 2 scraps |

## 7. Who says what
- **Pell** (character-bibles.md §1): three things, then one. Never unkind; never apologises for the report.
- **Isolde's pages**: plain, questions instead of answers. "Don't anchor what I've drawn. Come find why."
- **Maren** (swan, Queen-Regent; added to the cast): gracious, tired, certain. Image: permanence. The cygnet line
  breaks her composure for three lines.
- **Brisk** (starling, foreman) and **Anvers** (heron, Guild clerk); **Tam**; the **Arden** family; the **Orchard
  Keeper**; **Oriel**, briefly, before her fight.

## 8. Open
- The rooms; the flyer-tower's climb; Oriel's fight (CMB-14) and Halvard's second kit (CMB-12).
- Oriel's stand-down (`halden.oriel.stood_down`) is written by her fight (CMB-14, `docs/design/boss-kits.md`) when
  she is beaten without a mask lost; the fight stands in a runtime arena room until Bastion_2 is built.
- Voice marks for the Saltmarrow, Emberdown and Verdance choices.
- Pell at the act break (`Edge_Pell_Watch`, the Greyfold) and at the Threshold: NAR-11.
