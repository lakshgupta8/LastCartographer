# The Windreach Arc (NAR-10, v1)

The Steppe (bible 3.6, 4.5, 8.5, 6.9, 6.10) as written in Yarn: the proof that the true ending is possible. The
clans hold their places by walking them, and this arc is where the game says so out loud. The rooms are planned
(`docs/design/windreach-greyfold-blank-rooms.md`); every scene names its room. Files live in
`Assets/_Project/Dialogue/Windreach/`.

## 1. Beats

| Beat | Room | Scene (node) | Writes |
|---|---|---|---|
| The first fire | Camp_2 | `Camp_Idrenne`: "A map in the pack. A question." Sit, eat first; walk three days and ask at the last fire | `windreach.idrenne.met`, `windreach.camp.night` = 1 |
| The second fire | River_2 | `River_Idrenne_Night`: the stones are a map. Eight places, and the ninth is wherever we're standing | `windreach.stones.named`, night = 2 |
| The leap | Gate_1 | `Gate_Idrenne_Leap`: forty years of singing the young off the lip. The wind knows her (plant 5.3) | `windreach.leap.done`, `<<grant Windmemory>>` |
| The third fire | Fire_1 | `Grass_Idrenne_Night`: everyone says where they were standing when they learned something. So does Wren | `windreach.camp.walked`, night = 3 |
| Idrenne's Fire | Fire_2 | `Fire_Idrenne`: how the clans do it, plainly, rot and all (plant 9.2 ×2). The survey. The cooking-stone | `windreach.fire.witnessed`, `windreach.survey.decided`, `keystone.windreach`, `windreach.star.woken` |
| The cost | Star_1 | `Star_Idrenne`: "There it is. The cost. Mind the iron." After 6.10: "No cost. Well. One." | `windreach.star.met`, `windreach.star.cold` |
| The surveyor | Stones_3 | `Stones_Hale` → a duel (6.9), `Stones_Hale_Finish`, or after the duel `Stones_Hale_Pages` | `windreach.hale.decided`, `.challenged`, `.finished`, `.pages` (1 burned, 2 kept), `.lens` |

The walk has an order and every scene holds to it: the riverbed is cold ashes before the first night, the Gate is
"the third day's", the high grass is trampled and empty until she has jumped, and the Fire is said once, to
someone who has walked the three fires.

## 2. What Idrenne says at the Fire
Bible 9.2's requirement, in her voice, and the clans' rot (3.6) with it, because she does not pretend:

> We asked to be anchored. We were poor. They said the Steppe wasn't worth a stone.
> So we made a philosophy of it afterwards. I'll not pretend otherwise. It works anyway.
> A place holds when it's lived in. Walked. Every name in it said out loud.
> One town can't do it alone. It takes everyone you've met, singing the same walk.
> And the stones go back where they came from. You don't keep a place in a vault.

That is the true ending, line by line: the bounds of the Blank walked, everyone she has met singing, the keystones
given back. The ending matrix (DES-12) reads `Steppe.FireWitnessed`.

## 3. The region's decision (`Steppe`, new)
Surveying the Steppe would let the Guild anchor it (4.5). Two birds can finish that survey:
- **Hale**, if Wren lets him (`Finish it, then`) or helps him. The ninth stone is the uncarved one, wherever the clan
  is standing, so it cannot be sighted until somebody stands there. If Wren holds his staff, she is the somebody.
  Either way, the Steppe is on Guild paper, and at the Fire there is nothing left to decide.
- **Wren**, at the Fire: draw it (for them, not the Guild; "paper goes where paper goes"), leave it undrawn, or walk
  it every year with the clans. The last is offered dimmed until she knows a walk (Kettil's or the whale's).

`Steppe.OnTheGuildsMap` is true if Hale finished or Wren drew it; `Steppe.HeldByWalking` if she chose to walk it.
If she beats Hale, his eleven months of pages are hers to burn or keep; kept pages are hers, not the Guild's.

## 4. The keystone
The clans' cooking-stone. Idrenne gives it up laughing, with no condition: not the survey, not Hale, not a right
answer. "No cost. I mean it." That is deliberate (4.5), and the contrast with Teodor's and Kettil's is the point.
Lifting it wakes the Fallen Star in the crater: Idrenne's one cost (6.10). The fight is optional, and the stone is
hers either way.

## 5. Who says what
- **Idrenne** (crane, Speaker): amused, unhurried; answers with where she was standing when she learned the answer.
  Never says "always" or "never" (`SteppeTests` holds every one of her lines to it). Calls Wren "map-bird".
- **Hale** (godwit, Guild surveyor; added to the cast): professional courtesy, a colleague to her. Works alone and
  does not say "we", the opposite of Voss.
- **Ossa** (a child) and **Brek** (old; failed the leap forty-one years ago) at the third fire.

## 6. Commissions (the camp's ledger, `CommissionCatalog.Windreach()`)
| Commission | Posts after | Steps | Reward |
|---|---|---|---|
| The Moving Camp | arrival | three nights (`windreach.camp.night` ≥ 3) | 2 scraps |
| The Nine Stones | meeting Idrenne | learn what the stones are | 1 scrap |
| The Guild Surveyor (6.9) | the stones named | meet him at the ninth stone | 2 scraps |
| The Fallen Star (6.10) [A Windmemory] | the stone lifted | put it back to sleep | 2 scraps |
| Idrenne's Fire [F 9.2] [A Windmemory] | the three fires | ask her | 1 scrap |

No [B]: Windreach is not left to fade by any choice here. What happens to it is the Guild's paper or the clans'
walking.

## 7. Open
- The rooms; the leap as a set piece (a jump that must fall before Windmemory catches); Hale's duel (CMB-14) and the
  Fallen Star (CMB-14).
- The camp moving between sites is written into the scenes; the hub that moves with it is PRG-21.
- What Hale's lens does (an Instrument, NAR-17), and what a Wren who kept his pages can do with them.
- Whether Hale reappears at the Threshold among Voss's Wardens if he finished (NAR-11).
