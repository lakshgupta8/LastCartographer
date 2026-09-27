# The Blank's Islands (NAR-14, v1)

Bible 4.7, 8.6 and 10 ("the Blank is built from your choices") as written in Yarn: every place Wren left to the
Blank drifts in as an island, and its people react to how she left it. Plus Aury's Lighthouse, the fixed island
reached by Sable's tether in Act 2. Files: `Dialogue/Blank/Blank_Islands.yarn`, `Dialogue/Blank/Blank_Aury.yarn`,
`Dialogue/Saltmarrow/Saltmarrow_Chain_Sable.yarn`. The generator that builds the islands' rooms is PRG-20; it reads
`Islands`.

## 1. Which islands, and why (`Islands`)

| Island | Node | Drifts in when | Who, and what they say to how she left it |
|---|---|---|---|
| Merrow's End | `Island_Merrow_Dotha` | Merrow's End released, or Dotha let it go (`saltmarrow.dotha.decided` = 2) | Dotha has all eleven songs here; "You let it go. Kind." Does not ask twice |
| Hollowvein | `Island_Hollowvein` | left buried (`emberdown.hollowvein.buried`) | Brask and thirty others, standing, listening for the roll-call. If Wren knows the walk she calls it, and they answer (`blank.hollowvein.called`) |
| Aldermere | `Island_Aldermere` | released (the last day attended) | Hollin: the festival is still tonight; "We would rather be remembered than kept. And you remembered." |
| The inn | `Island_Inn` | the Overgrown Gate's road followed (`verdance.gate.inn_visited`) | The innkeeper: the same soup; a regular now |
| Lowmarket | `Island_Lowmarket` | released (the strike broke) | Brisk: no owners, no wages, no survey; "Nobody's owed anything here. I thought I'd like it more." |
| any other released place | `Island_Remnant` | its fate is Released and no island above covers it (`Islands.GenericPlaces`) | A Remnant: "That's where unanchored goes. It's not bad. It's quiet." |

One authored island per [B] commission (tested). An island's presence is its **outcome**, not the commission:
the ledger's `blank.island.*` marks that a [B] commission closed, and Lowmarket's closes whichever way the strike
went. Only a broken strike puts Lowmarket in the Blank.

Remnant voice everywhere: half-sentences, present tense, polite. Everyone asks whether she has eaten (Dotha, who
was never polite, is the exception). Nobody says "I'm dead" (tested).

## 2. Aury, and the tether
- **The tether** (`Chain_Sable_Tether`, the Lantern Chain's tether-post, Act 2, once Wren has asked who keeps the
  third lighthouse): "Tether's fifteen. My rowing's free. Don't read anything into that." Writes `sable.tether_sold`
  and `saltmarrow.tether`, which opens the causeway on the map, and makes Sable allied (endings.md §1).
- **Aury, Act 2** (`Aury_Lighthouse`): a cormorant keeping a lamp that faded forty-one years ago, and he does not
  know. **Plant 5.3**, the bible's third: "Like a chick I saw once, carried past, out of the white." The keystone is
  in the lamp's heart; he offers it. She can take it or leave it with him (the Rest needs zero), and take it later.
- **Aury and Sable, Act 3** (`Aury_Sable`): she talks prices, and does not tell him. Wren can: "Aury. You faded."
  "…Oh. That's why the oil's late." Sable says nothing for the first time in the game, then "Boat's leaving."
  (`sable.aury_told`, `blank.aury.knows`.)

## 3. What this closes
- **Every keystone Wren can carry is handed over by a script**: Aury (here), Hollowvein (Runa), the Quiet House
  (Teodor), Windreach (Idrenne), Isolde's, and Corvin's. The Fixed World is reachable in play.
- **Every story gate on the map is opened by a script**, `saltmarrow.tether` included (tested by walking the map's links).
- **Every cast scene with a stage is written.** The one left is `BoneBridge_Sable`, whose room is neither built nor
  planned (Saltmarrow's Bone Bridge).

## 4. Open
- The islands' rooms (PRG-20): one generated scene per released place, from `Islands.Present` and
  `Islands.GenericPlaces`.
- Generic islands speak one script. A place-aware line ("This was the Cinder Baths") needs the island's name
  passed in as a Yarn variable, which the generator can set.
- The Bone Bridge crossing with Sable (Act 1): when the room exists.
