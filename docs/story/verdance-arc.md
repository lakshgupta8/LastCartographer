# The Verdance Arc (NAR-08, v1)

Act 1's second climb (bible 4.3, 7.1 step 4, 8.3) as written in Yarn, with the state each scene reads and writes.
Like Emberdown, it stands on its own as either climb. The rooms are planned (`docs/design/emberdown-verdance-rooms.md`);
every scene names its room. Files live in `Assets/_Project/Dialogue/Verdance/`.

## 1. Beats

| Beat | Room | Scene (node) | Writes |
|---|---|---|---|
| The mill | Road_3 | `Road_Solvent`: Sister Wend lets Ferrow's Mill go; Miller Tobin never asked; his mother did | `verdance.solvent.decided` (1 let it go, 2 stopped, 3 asked her with him there) |
| The House | House_2 | `QuietHouse_Teodor_First`: "You are Isolde's. May I say a thing you will not like?" | `verdance.teodor.met` |
| Inkthread | Chapel_2 | `RootChapel_Teodor_Thread`: the solvent-line drawn the other way. Only after the House | `verdance.teodor.thread`, `<<grant Inkthread>>` |
| The vigil | Grove_2 | `Grove_Teodor_Vigil`: eleven villages in the present tense. No choices | `verdance.grove.vigil` |
| The library | Library_1–2 | `Library_Teodor_Ansel` (he will not turn it); `Library_Ansel` → `Library_Ansel_Turn` | `verdance.library.page_turned`, `.decided` (1 turned, 2 left) |
| The last day | Aldermere_2 | `Aldermere_Teodor` (Teodor) or `Aldermere_Hollin` (Hollin) → `Aldermere_Attend` or `Aldermere_Stop` (then the Choir, 6.6) | attended: `<<release>>` the three rooms, `.decided` = 1; stopped: `.decided` = 2 |
| After, attended | Aldermere_1–3 | The lane and the square stand empty; the village is paper in the ash field under its bunting (released, so its people are Remnants), and Teodor sits with them at dusk in his own colours (`Aldermere_Ash_Teodor`): "Hollin cuts it too thick. She always does." | `verdance.aldermere.ash_sat` |
| After, stopped | Aldermere_2 | Teodor has gone. With the Choir answered, Hollin (`Aldermere_Hollin`): the bells stopped halfway and nobody knows how the song ends; the bread went stale; they will vote again. Then "We are still here." | `verdance.aldermere.after_heard` |
| The keystone | House_2 (Act 2) | `QuietHouse_Teodor_Keystone`: "Why do I let places go?" | `teodor.keystone_given` + `keystone.quiet_house`, or `teodor.refused` |
| The gate | Gate_2 | `Gate_Inscription`; after the Gatekeeper (6.7) and a survey, `Gate_Inn`: the road for one night | `verdance.gate.inn_visited` |

## 2. The keystone check (character-bibles.md §4)
Not a stat. Teodor raises it the first time Wren visits after Aldermere is decided. If she stopped the last day,
he refuses before asking: the Choir's defeat is not an argument. If she attended, he asks why he does what he
does, and offers three answers: that he is brave (no: he has been called that and it was never true), that he
grieves (no: grief is what a reason leaves), and that **they asked, and he made sure** (yes). The right answer is
the only one that names the thing he did, not the thing he feels. It is also the first time he says, aloud, that
the tenth village was his mother's and he was not sure. A refusal is final and kind; the House keeps its door open.

The keystone is a flag (`keystone.quiet_house`) until keystones are a system (world-map.md §6 has the open count).

## 3. Who says what
- **Teodor** (character-bibles.md §4). Never raises his voice, asks permission before hard sentences, calls her
  "cartographer", speaks of the faded in the present tense. His one sorrow in the square: that she must answer
  the Choir.
- **Mayor Hollin** (thrush, Aldermere). "We would rather be remembered than kept." Offers bread.
- **Brother Ansel** (the Sunken Library). Page 214 of a history of the Halloways; "I am coming to the part about his
  daughter" (5.4). Turned, he forgets thirty-eight years and asks who let the dust in.
- **Sister Wend** (dove, Cantor) and **Miller Tobin**. The Solvent's question is Teodor's own from the other side:
  a mother asked, a son did not.
- **The innkeeper** (Remnant). Half-sentences, present tense, asks whether you have eaten, never "I'm dead".

Plants: 5.2 (Isolde's carelessness), 5.6 (the thread, the gate's inscription), 5.1 (Ansel; the keystone answer),
5.4 (Ansel's history and the owl's daughter).

## 4. Commissions (the Quiet House's ledger, `CommissionCatalog.Verdance()`)
| Commission | Posts after | Steps | Reward |
|---|---|---|---|
| What the Ash Remembers [B] | meeting Teodor | be in the square on the last day | 3 scraps; island `Aldermere` |
| The Solvent | meeting Teodor | settle the mill | 2 scraps |
| The Sunken Library [F 5.1] [A Inkthread] | the thread | survey `Library_2/Page`; turn the page or leave it | 2 scraps |
| The Overgrown Gate [B] [A Inkthread] | the thread | survey `Gate_2/Gate`; follow the road to the inn | 2 scraps; island `Overgrown_Inn` |
| The Lantern Grove Vigil | the thread | sit it | 1 scrap |

## 5. Style check
This arc added `DialogueStyleTests`: every Yarn file is held to the style guide's hard limits (twenty words a
line, eight a choice, three choices, known tags, `#plant` with a bible section). Writing it caught twelve lines
over the limit across the Verdance, Emberdown and Saltmarrow files; all were cut.

## 6. Open
- ~~The rooms.~~ Built (ENV-07). ~~Aldermere's after-state.~~ Written and standing (§1): paper in the ash field for
  attended, half a song for stopped. The Choir's scar on the square as a drawing is art's (no piece yet).
- ~~The Choir's fight and the Gatekeeper's.~~ Both kits stand (CMB-13), in runtime arena rooms.
- ~~Keystones as a system.~~ `Keystones` (threshold.md §4).
- ~~Teodor's epilogue.~~ `Epilogue_Teodor` (endings.md).
