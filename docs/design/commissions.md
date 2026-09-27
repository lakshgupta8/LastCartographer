# Commissions (DES-06, v1)

The side-quest system. A **ledger** at every hub; people write what they want and cannot say aloud. Silksong's
wishes board in the game's own vocabulary (bible 8). This spec covers the ledger, the states, rewards, and the
flags the Blank generator (PRG-20) reads. The Saltmarrow set is in bible 8.1 and, greybox-sized, in
`CommissionCatalog.Saltmarrow()`.

## 1. Rules
- Commissions are **posted** on a hub's ledger, **taken** there, **fulfilled** out in the world, and **closed**
  (turned in) back at the same ledger. Rewards land on close, never on fulfilment: the walk back is the point.
- A commission can also **fail** (Posted or Taken). Failure is a story decision, written from Yarn; nothing fails
  on a clock (fade rule, bible 10).
- Nothing moves backwards. Nothing is hidden once posted: a closed or failed commission stays on the board, dimmed.
- No objective markers in the world (GDD 8). The journal says where in words; the atlas shows where it was taken.
- Tags from the bible: **[F]** foreshadows a secret, **[B]** seeds an island in the Blank, **[A]** needs a later
  ability. [F] and [A] are descriptive only (the journal shows "needs Wingbeat"); [B] writes a flag on close.

## 2. States

```
Unknown ──post──▶ Posted ──take──▶ Taken ──(steps all met)──▶ Fulfilled ──close──▶ Closed
                     │                 │
                     └────fail─────────┴──────────▶ Failed
```

| State | Where | Player sees |
|---|---|---|
| Unknown | not on any board | nothing |
| Posted | ledger | brief and poster; J takes it |
| Taken | journal + ledger | journal text, steps with progress |
| Fulfilled | journal + ledger | "fulfilled — turn in" |
| Closed | ledger (dim), journal "Closed" | aftermath line |
| Failed | ledger (dim), journal "Closed" | "It came to nothing." |

A commission posts when Wren reads the ledger and its `PostAfterFlag` (if any) is set. Posting is lazy so a board
never changes while Wren is standing at it.

## 3. Steps
A commission has one or more steps; all must be met to fulfil. Step kinds:

| Kind | Key | Met when |
|---|---|---|
| Flag | flag key | `WorldState.Flags[key] >= Target` (set from Yarn with `<<flag>>`) |
| Vantage | vantage id | `WorldState.SurveyedVantages` contains it |
| Count | counter name | `Numbers["commission.<id>.step<n>"] >= Target` |

Counters are bumped by the `CommissionTracker`: an enemy death bumps `kill.<Family>` and `kill.any` for every
**taken** commission with a matching Count step (kills before taking do not count). Other counters can be added the
same way (`Commissions.Bump(world, "name")`). Taking a commission whose steps are already met (a vantage drawn
earlier) fulfils it at once.

## 4. Rewards (on close)
- **Vellum scraps** (`Numbers["$vellum_scraps"]`), the currency for masks and quill upgrades (DES-05).
- An **Instrument** added to the owned set (Sable sells others; DES-05).
- A **flag** (`RewardFlag`), for content that unlocks on a turn-in.
- **[B] Blank island**: `blank.island.<name> = 1`. The Act 3 generator (PRG-20) reads these; how Wren left the
  place is in the commission's own decision flags (for Dotha: `saltmarrow.dotha.decided` 1 remembered, 2 let fade).

## 5. WorldState keys
| Key | Type | Meaning |
|---|---|---|
| `commission.<id>` | flag (int) | `CommissionState` |
| `commission.<id>.step<n>` | number | Count-step progress |
| `blank.island.<name>` | flag | island seeded |
| `$vellum_scraps` | number | scraps |

Saves need nothing beyond flags and numbers (PRG-09).

## 6. Yarn API
```yarn
<<commission saltmarrow.dotha take>>          // verbs: post, take, fulfil, close, fail
<<if commission_is("saltmarrow.dotha", "taken")>>
<<if commission_state("saltmarrow.dotha") == "closed">>
```
Most commissions never need the command: they post from the ledger, fulfil from flags the conversation sets, and
close at the board. The command is for story-driven ones (a character hands Wren a commission directly, or fails one).

## 7. UI
- **Ledger page** (opens on the board, `LedgerView`): rows of title and state; below, the brief with poster, or the
  journal text with step progress, or the aftermath; the reward line. ↑↓ choose, J take / turn in, Esc leave.
- **Journal** (`JournalView`, M or Select; the atlas's right page until the atlas exists): open commissions with
  steps, then closed ones, then the scrap count.
- **Toast**: a paper caption under the HUD for taken / fulfilled / closed / failed.

## 8. Saltmarrow set (greybox)
| Id | Title | Posts | Steps | Reward | Tags |
|---|---|---|---|---|---|
| `saltmarrow.lantern_chain` | Lantern Chain | at once | Lamp beacon surveyed (the Lamp-Keeper) | 2 scraps | (7 lamps in NAR-04; the last is [A: Clarity]) |
| `saltmarrow.bone_bridge` | The Bone Bridge | at once | Reedmother surveyed; ask Sable | 1 scrap | [F 3.4] |
| `saltmarrow.iris_harvest` | The Iris Harvest | at once | 3 marsh crabs (stand-ins for Guild agents) | 1 scrap, Iris tincture | |
| `saltmarrow.dotha` | Dotha's Last Season | after meeting Sable | Dotha's decision (room B) | 2 scraps | [B Merrows_End] |
| `saltmarrow.tether_widows` | The Tether-Widows | after the Lamp-Keeper | the widow's answer, through Sable | 2 scraps | |

## 9. Open
- Failure and expiry: which Saltmarrow commissions can fail, and whether anchoring a place fails its open commissions (DES-03).
- Multiple ledgers per region (Windreach's moving camp carries its own).
- Commission text in the atlas's map page once it exists (where it was taken, no markers).
