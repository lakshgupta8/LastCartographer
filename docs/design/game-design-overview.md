# Game Design Overview — The Last Cartographer

Companion to the story bible. The bible says *what the world is*; this says *what the player does*. Combat detail lives in `combat-and-movement.md`; the look lives in `art-direction.md`.

## 1. Pillars
1. **Exploration draws the world.** Surveying from vantage points inks in the map. Blank paper is the invitation.
2. **Fast, aerial, needle-sharp action.** Pogo, dash, thread, strike. The Inkwell makes every fight a tempo decision between damage and healing.
3. **Every choice populates the ending.** Nothing done to a place is forgotten; Act 3 is generated from what the player left behind.
4. **Told quietly.** Sparse cryptic NPCs, environment, bosses that are characters. The game never summarises itself.

## 2. Core loop
```
Reach a new area (ability-gated)
  → Survey from vantage points; the atlas inks in
    → Fight through; learn the region's enemy "answers"
      → Hub: drafting desk, Commissions ledger, NPCs, Charter/Instrument swap
        → Region's story arc → boss → ability or keystone
          → Regional decision (anchor / hold / release) written to WorldState
            → New ability reopens every earlier region (backtracking rewards)
```
Minute-to-minute: move, strike, pogo, dash, thread, survey. Every 20–40 minutes: a boss or a set piece.

## 3. Structure
- **Prologue** (30 min, linear). **Act 1** (5 h, semi-linear: coast → one of two climbs → Plateau). **Act 2** (15 h, open: four regions, two Halden interludes). **Act 3** (5 h, the Blank). **Epilogue** (30 min).
- Critical path about 25 h; completion about 40 h.
- Ability gates: Wingbeat → (Talonhold or Inkthread) → Plateau → the other → Windmemory → Clarity → the Sky. Sequence-breaking with skilled pogo is allowed and quietly rewarded.

## 4. Systems summary

| System | One line | Detail |
|---|---|---|
| Survey | Hold at a vantage point; the map draws itself; unlocks fast travel at lamps/desks | Bible 10 |
| Anchor / hold / release | The regional decision; sets a place's fate and its Act 3 island | Bible 4, 10 |
| Inkwell | 9-pip resource from hits; spend on Bind, Flourishes, Inkthread | Combat 4 |
| Charters | One equipped stance rewrites the combo; three at start, three found | Combat 5 |
| Instruments | 3–4 equipable tools with limited uses | Combat 6 |
| Commissions | Ledger at each hub; the side-quest system; some create Blank islands | Bible 8 |
| Fade stages | 0–4 per place, advanced on story beats only | Bible 10 |
| Clarity | Meter for untethered Blank sections; a late-game gate everywhere | Combat 3 |
| Bounds-walk | Rhythm traversal with an NPC chorus; the non-combat set piece | Combat 10 |
| Memories | Inventory of bound memories; keys for doors, birds, keystones | Bible 10 |
| WorldState | Flag store behind all of the above; drives the Act 3 generator and endings | Code |

## 5. Progression
No XP. Wren grows through: masks (5→9, from vellum caches), Inkwell size (9 fixed; regen upgrades), quill upgrades (three, at the Halden smith, cost keystone-adjacent vellum), Charters, Instruments, abilities, and **knowledge** (dialogue options appear once a fact is known).

## 6. Death and retry
Death drops Wren's bound memories at the spot as a **smudge**; recover it to get them back (a Silksong-style corpse run, but the cost is narrative currency, not money). Respawn at the last drafting desk or wax seal. Enemies respawn; bosses do not.

## 7. Camera and presentation
Side-on perspective camera, layered paper parallax, art-directed lighting, 60 fps. Internal render at native resolution (hand-drawn, not pixel art). Depth of field on foreground layers; extreme in the Greyfold.

## 8. UI
The atlas is the pause screen: map page left, journal/memories/keystones right. Masks and Inkwell top-left, small. Instruments bottom-left. No objective markers in the world, ever; the atlas shows where Commissions were taken.

## 9. Scope targets

| Thing | Count |
|---|---|
| Regions | 6 + the Blank |
| Sub-zones | ~34 |
| Vantage points | ~48 |
| Bosses | 15 (5 optional) |
| Enemy types | ~40 |
| Charters | 6 |
| Instruments | 7 |
| Named NPCs | ~55 |
| Commissions and hidden stories | ~30 |
| Endings | 4 + Voss branch |
| Dialogue | ~60k words (sparser than v1 by design) |

## 10. Onboarding
The prologue teaches strike, pogo, survey, bind, and seal with Isolde as the voice. Saltmarrow teaches the Inkwell, Charters, and Commissions diegetically (Sable sells the first Instrument; the Lamp-Keeper gives Wingbeat). No tutorial pop-ups after the prologue.
