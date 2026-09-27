# The Endings (NAR-13, v1)

Bible 9 and 7.4 as written in Yarn, with what each ending needs as flag logic (`Endings`). Unlabelled in-game;
named here for production. Files: `Dialogue/Halden/Halden_Observatory_Endings.yarn` (the frame, three endings, Voss),
`Dialogue/Blank/Blank_Capital.yarn` (`Ending_Rest`), `Dialogue/Epilogue/Epilogue_Walk.yarn`. DES-12 builds the matrix
and proves each reachable on this logic; PRG-23 runs it.

## 1. What each needs (`Endings.IsOpen`)

| Ending | Where it is chosen | Needs (bible 9) | As flags |
|---|---|---|---|
| **The Fixed World** | the frame | seven keystones; Corvin's cooperation | six carried + the frame's own (`StonesAtTheFrame` = 7); `corvin.stance` = 1 |
| **The Open World** | the frame | ≥ 4 keystones; three of the five names allied; witnessed Idrenne's Fire and one of Aldermere or Hollowvein; Corvin persuaded; boss 6.15 | `Keystones.Count` ≥ 4; `AlliedCount` ≥ 3; `windreach.fire.witnessed` and (`verdance.aldermere.attended` or `emberdown.hollowvein.walked`); `corvin.stance` = 2; then `boss.complete_survey.defeated` |
| **The Unwritten** | the frame | Teodor allied; Aldermere attended without stopping it | `teodor.keystone_given`, `verdance.aldermere.attended`, not `.stopped` |
| **The Cartographer's Rest** | Corvin's chair, in the Blank | zero anchors, zero keystones; Corvin offers | `Keystones.Count` = 0, `Places.Count(Anchored)` = 0, `ending.rest_offered` |

The frame (`Observatory_Frame`) offers only what is open; dimmed otherwise. If nothing is, "Not yet." She can go and
earn one. Act 3's map is open. Corvin's chair can be come back to (`Capital_Corvin_After`): change his stance to
cooperation, argue again, or sit. So a stance is never final, and no path through Act 3 locks every ending.
`Endings.Choose` writes `ending.chosen` once.

**Allied** (`Endings.Allied`), because the bible names the five but not what allied means:

| Name | Allied if |
|---|---|
| Sable | she sold the tether and went with it (`sable.tether_sold`, NAR-14), or heard Wren walked Merrow's End (`saltmarrow.sable.walked`) |
| Runa | she counts Wren in by name (`runa.named_wren`), or they walked Hollowvein (`emberdown.hollowvein.walked`) |
| Teodor | the honest answer (`teodor.keystone_given`) |
| Idrenne | walked the three fires (`windreach.camp.walked`) |
| Pell | kept the report (`pell.report_kept`) |

Nobody of the five dies in v1, so "alive" holds for all.

## 2. The seventh keystone (settled)
The bible needs seven and places six. The seventh is **Isolde's**: the stone she stole from the Vault's sixth slot
to return to Corvin (5.2), still in her pack in Thessaly Hollow. She gives it (`Hollow_Isolde`) or keeps it to give
him herself; she asks again on each visit. Seven homes (`Keystones.Homes`): Aury, Hollowvein, the Quiet House,
Windreach, Isolde, Corvin, and the Observatory's own, which never leaves the frame and counts only there. Thessaly
Hollow is now a keystone zone on the map.

## 3. The four, and the walk after

| Ending | At the frame | Epilogue walk (`Endings.EpilogueWalk`) | Marrow's verdict |
|---|---|---|---|
| Fixed | "The seventh socket closes. The Atlas is whole." "Nobody flies." | Pell, **Sable** (the quay: "Prices are the same." #still), Marrow | echoes Ilse: "Same." Bright, beautiful, no new word |
| Open | Runa's chorus of everyone Wren has met, in order; 6.15; "Wren flies. Once. Briefly." (`<<grant Sky>>`) | Pell, **Runa** (forty-three, and teaching someone from Merrow's End), Marrow | laughs: **"Skywalk."** A word nobody has said before; it appears once in the whole script (tested) |
| Unwritten | Teodor names each place Wren carried a stone for, in the present tense: "Hollowvein is." "There. Now nothing is held." | Pell, **Teodor** (walks Aldermere's bounds, alone; it holds), Marrow | fades holding Wren's wing: **"Look."**, its third word repeated |
| Rest | Corvin: "Isolde can go." She sits and draws beside him | Pell (Isolde teaching a new apprentice, "Let the pen find the line", the prologue's words; Wren's atlas on the desk), Marrow | not there |

The outer region is the ending's own: the coast for the Fixed World, Kettil's Rest for the Open, the Quiet House for
the Unwritten. The Rest walks from Halden straight to the Hollow.

**Wren's last line** (bible 2.3: her voices "decide the last line of the epilogue"): the last page of her atlas, by
her most-used voice. Surveyor: "everything I saw, drawn true." Warden: "everyone I could, carried out." Drift: "left
blank. On purpose."

## 4. Voss (bible 9.5)
`Observatory_Voss`, after any ending. If he said her name at the Return (`voss.changed`): a note in his exact hand,
under the small drawing: "Gone in to find her. Hold nothing for me. A. V." If not: a statue at the Observatory door
that was not there before, a grey heron in brass facing the Greyfold. Nobody remembers commissioning it.

## 5. Open
- Sable's tether (`sable.tether_sold`) and Aury's stone (`keystone.aury`) are NAR-14's. Until then the Fixed World is
  one stone short in play (the logic is tested with flags), and Sable is allied only through the walk.
- The Complete Survey's fight (CMB-15) and the epilogue walk's runner (PRG-23).
- Marrow's naming at the Hollow; "it" to "they" as Wren's choice.
- DES-12: the full matrix, with every ending proven reachable from a new game on `WorldGraph` and these flags.
