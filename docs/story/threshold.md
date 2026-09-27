# The Threshold (NAR-11, v1)

The Greyfold as the story crosses it before Act 3 (bible 7.1 step 6, 7.2's climax, 6.3, 6.11): the act break at the
road's end, the notice that calls the climax, Marrow's one echo, Halvard's third, Voss's one speech, and Pell at the
line. The rooms are planned (`docs/design/windreach-greyfold-blank-rooms.md`). Files live in
`Assets/_Project/Dialogue/Greyfold/`. The Return (the Threshold from inside, Act 3) is NAR-12's.

## 1. Beats

| Beat | Room | Scene (node) | Writes |
|---|---|---|---|
| The act break | Road_3 | `Edge_Pell_Watch`: she steps off the road's end without a tether and stays herself. Pell's five things; "Wren. How?" | `<<grant Clarity>>`, `pell.saw_her_cross`, `act2.started` |
| The call | EdgeCamp_2 | `EdgeCamp_Notice`: once Interlude A is decided, Voss's notice over the dead ledger. The Guild goes in at dawn. If Pell kept the report, a note under it | `act2.threshold` |
| The pool | Pool_2 | `MirrorPool_Marrow`: a grey chick in the reflection, not on the bank. It says back her own line | `marrow.seen_in_pool` |
| Halvard's third | Threshold_1 | `Threshold_Halvard`: "The count ends here." After 6.3: "Go. I'll enter it as a survey." He stands the Wardens down | `threshold.halvard.spoken`, `act2.halvard_third` |
| Voss | Threshold_2 | `Threshold_Voss`: the one speech. After 6.11: "Take it in. Tell her— no. Nothing. Go." | `threshold.voss.spoken`, `greyfold.crossed` |
| Pell at the line | Threshold_2 | `Threshold_Pell_Cross`: only if the report was kept, and only after Voss. Three things | `pell.at_threshold` |

With these, **every story gate on the map before Act 3 is opened by a script** (`EmberdownLedgerTests` walks the
map's links): `act2.started` by the act break, `act2.threshold` by the notice, `greyfold.crossed` by Voss.
The Aury tether (`saltmarrow.tether`) is the exception, left to the Lantern Chain's tethers (NAR-14).

## 2. What calls the climax
Bible 7.2 says only that Wren goes back "with whatever keystones she holds". The notice needs **Interlude A
decided** (`pell.report_decided`) and nothing else: the report is the one Act 2 hinge the Threshold reads without
exception (Pell there or not, the Wardens hostile or not). No keystone count, no region count. Voss is going in at
dawn whether she is ready or not.

## 3. Voss's one speech, and what it reads
The style guide gives Voss one speech (bible 11.6). It has a fixed core and a front that reads what she has done:

| If | He says |
|---|---|
| (always) | "Journeyman Halloway. This is as far as the map goes." (plant 5.4: he says the name he flinches at) |
| the report sent / kept | "Your minder writes well. I have read the report four times." / "Your minder filed nothing on you. Pell has not once filed late. We noticed." |
| Maren accepted / refused / told | "…make Halden permanent. Then help us hold it." / "…you refused her. So have we, often." / "…about the boy. That was not yours to tell." (5.1) |
| stones carried: 0 / 1–2 / 3+ | "…nothing to take from you." / "…belong in the Vault, in the slots we keep." / "…emptied half of Aurenne into your pack." |
| Hale finished / Hale beaten | "Hale's survey of the Steppe is on my desk." / "Hale came home without his lens." |

The core, every time: forty-one years holding the line; "I brought tethers today. Forty. I meant to go in myself,
this morning"; "I have stood here since dawn. I cannot say why I have not" (5.5). It is the only place he says "I"
for long: the Guild's "we" breaks where his daughter is. He does not say her name.

Wren's three answers (come in with me; who's in there you won't see; stand aside) do not change the fight. They are
the voice tally's, and the Return (NAR-12) will read what she said here.

## 4. Keystones (`Keystones`, new)
Voss counts them, so they needed counting. Each is a flag `keystone.<home>` for the six homes the map names: Aury,
Hollowvein, the Quiet House, Windreach, the Observatory, Corvin. Hollowvein's is now written where Runa hands it
over ("It's yours, if you want it"). `Keystones.OpenWorldNeeds` is bible 9.2's four. Aury's, the Observatory's and
Corvin's are written in NAR-12 and NAR-14.

## 5. Who says what
- **Pell**: five things at the Edge, three at the Threshold. Lists shorten as Pell grows surer. First plain question
  in the game: "Wren. How?"
- **Halvard**: counts paces (three at the lighthouse, four on the bridges, none here), then stops counting. "Survey",
  not "count", is the change.
- **Voss**: formal, exact, "we" for the Guild until the core of the speech.
- **Marrow**: echo only (`#echo:wren`), her chosen words, including silence.

## 6. Open
- Both fights (CMB-14); the Threshold rooms; the Blank eating Halvard's arena.
- Marrow's name plate reads "…" until Wren names it (character-bibles.md §5); the Yarn speaker is `Marrow`, and the
  UI needs a display-name override.
- Whether Hale, if he finished, stands among Voss's Wardens (a line for Halvard's scene).
