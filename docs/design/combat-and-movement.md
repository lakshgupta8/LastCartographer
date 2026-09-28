# Combat and Movement Design

The action layer. Target feel: *Silksong*-class responsiveness (input to animation under 3 frames, generous buffers, aerial commitment), with a distinct identity from Wren's needle-quill and the Inkwell. Every number here is a starting value for the vertical slice, to be tuned by feel.

## 1. Feel targets

| Property | Target |
|---|---|
| Frame rate | 60 fps locked, physics at 60 Hz fixed step |
| Input buffer | 6 frames for attack and jump; 4 for dash |
| Coyote time | 5 frames |
| Jump | Variable height: 2.0 tiles min, 4.5 tiles max; apex hang 3 frames |
| Ground speed | 9 units/s, instant accel, 4-frame decel |
| Attack | 3-frame startup, 4 active, 8 recovery; cancellable into dash from frame 10 |
| Hitstop | 2 frames on hit, 5 on kill, 8 on parry |
| Camera | Look-ahead 2 units in facing direction; vertical dead zone 1.5 units; hard shake only on boss slams |
| Damage model | Wren has **masks** of health (5 to start, up to 9). Most hits take 1; boss slams take 2 |

## 2. Base kit (available from the prologue)

### 2.1 Quill strike
- Directional slash with the needle-quill: forward, up, and **down-strike** (pogo) in the air.
- Reach is long and thin (2.2 units), narrow vertically. Rewards spacing.
- Combo: forward-forward-thrust. The thrust (3rd hit) has 3.0 reach and knocks back.
- Down-strike pogo bounces Wren 3 tiles; bounce is chainable; pogo on hazards (spikes, ink pools) is allowed and encouraged.
- **Ink on hit:** each landed strike fills the Inkwell by 1 pip (of 9).

### 2.2 Bind (heal)
- Hold to spend 3 pips and restore 1 mask over 0.6 s. Wren "draws herself back": a brief ink-line animation redraws her outline. Interruptible by damage. Can be done in the air after **Wingbeat** is learned (air Bind is a mid-game upgrade).

### 2.3 Jump, drop, ledge
- Variable-height jump. Down + jump drops through one-way platforms. No ledge-grab (Talonhold replaces it).

### 2.4 Survey stance
- Hold the survey button at a **vantage point** to ink in the map. Not usable in combat (Wren lowers her quill). Enemies within 8 units cancel it.

## 3. Movement abilities (progression)

| Ability | Input | Rules | Combat use |
|---|---|---|---|
| **Wingbeat** (air dash) | Dash | Horizontal, 5 units, 8 frames, i-frames on frames 2–5, one per airtime, resets on ground/pogo/wall | Repositioning; dash-cancel out of attack recovery |
| **Talonhold** (wall cling) | Toward wall | Cling 2.5 s, slow slide after; wall-jump arcs away 3 units | Reset Wingbeat; escape corners |
| **Inkthread** (grapple) | Thread toward an anchor-point | Pulls Wren to anchor rings (world) or to **marked** enemies (after a thrust hit); 2 pips per use | The signature aggressive tool: mark, thread, pogo |
| **Windmemory** (glide) | Hold jump in air | Glides at 60% fall speed; updrafts carry upward | Ride boss updrafts; aerial spacing |
| **Clarity** | Passive | Untethered Blank sections drain a clarity meter; hits from Remnant drain more (`clarity.md`) | Gates late areas |
| **The Sky** | Endgame | Brief true flight in scripted sections | The final approach |

## 4. Inkwell and Flourishes

The Inkwell has 9 pips. It fills by striking enemies, and slowly (1 pip / 10 s) while standing at a vantage point. It empties on death. Spend it on:

| Flourish | Cost | What it does |
|---|---|---|
| **Crosshatch** | 3 | Rapid 6-hit flurry in a cone; the quill scribbles. Best single-target damage |
| **Longstroke** | 3 | Piercing horizontal thrust across 6 units; passes through enemies and thin walls |
| **Blot** | 4 | Ink burst around Wren; knockback, 2 s slow on smudges; also extinguishes small fires and dissolves Cantor bells |
| **Bind** | 3 | Heal 1 mask (see 2.2) |
| **Inkthread** | 2 | Grapple (see 3) |

Design rule: the Inkwell is a **tempo** resource, not a bank. Full-well players should be tempted to spend on damage; low-mask players on healing. Never let the Inkwell exceed 9; excess ink drips visibly off the quill.

## 5. Charters (stances, like crests)

Wren carries one Charter at a time; swap at a drafting desk. Each rewrites her combo and default Flourish and changes silhouette (cowl shape, quill grip). They map to the three dialogue voices.

| Charter | Combo | Flourish default | Passive | Feel |
|---|---|---|---|---|
| **Surveyor's Charter** (start) | Slash, slash, thrust | Crosshatch | Ink fills 25% faster | Balanced, precise |
| **Warden's Charter** | Slow heavy sweep, shove, overhead | Blot | +1 mask; dash is shorter | Heavy, grounded, spacing |
| **Drifter's Charter** | Fast triple slash, no thrust | Longstroke | Second Wingbeat per airtime; masks 4 max | Aerial, glass |

Later Charters are found in the world: **Ferryman's Charter** (Inkthread costs 1, tether-swing attacks), **Unwriter's Charter** (attacks erase enemy projectiles; Bind costs 4), **Remnant Charter** (Act 3; strikes drain enemies' colour). Their combos, where each is found, and how the passives work are in `late-charters.md`.

## 6. Instruments (equipable tools)

Wren has 3 Instrument slots (4 with an upgrade). Instruments have limited uses restored at drafting desks. They are surveying tools used sideways.

| Instrument | Uses | Effect |
|---|---|---|
| **Compass-dart** | 12 | Thrown, homes gently, marks enemies for Inkthread |
| **Plumb weight** | 6 | Heavy lob; 3 damage; breaks weak floors |
| **Sighting lens** | ∞ (cooldown 3 s) | Parry: perfect-timed block reflects projectiles and staggers melee for 1 s |
| **Field lantern** | 4 | Reveals hidden platforms and Smudges for 10 s; in the Blank, extends lantern-radius |
| **Tether-hook** | 3 | Deploy a rope anchor to make a temporary Inkthread point anywhere |
| **Iris tincture** | 2 | Fills 5 pips instantly |
| **Wax seal** | 1 | Set a temporary respawn point (consumed on death) |

Buy and craft at hubs with **iris seeds** (currency) and **vellum scraps** (upgrade material from bosses and hidden caches).

## 7. Enemy design rules

- Every enemy is readable by silhouette at 1/6 screen height. Long-leg family (Wardens) telegraph with the lance; round family (townsfolk gone wrong, marsh fauna) telegraph with a hop; Smudges telegraph with a colour flare.
- Each enemy has exactly one "answer": something Wren does that is clearly correct (pogo the shelled ones, Longstroke the lined-up ones, Blot the swarms, parry the lancers). Bosses combine three or four answers.
- No enemy blocks the map. Every fight can be run past except bosses and arena locks.
- **Smudges** are the wildcard family: they flicker between drawn and undrawn; only drawn frames can be hit. Their rhythm is the region's music tempo.

## 8. Boss design rules

- Three phases, each introduced by a short line of dialogue or a change in the arena's ink state.
- The arena is part of the character (the Lamp-Keeper's beam sweeps, Voss freezing sections, Corvin drawing walls).
- No boss has more than four distinct attacks per phase. Every attack has a 12+ frame telegraph at Tier I, 8+ at Tier IV.
- Bosses drop **vellum scraps** and, where the bible says so, a keystone or ability. Optional bosses drop Charters or Instruments.
- Retry loop under 8 seconds from death to re-entering the arena. Drafting desk or wax seal within 15 seconds of every boss door.

## 9. Traversal challenges

- Each region has one **gauntlet**: a pure platforming sequence built around its ability (the Lantern Chain jumps, the Furnace Stair shafts, the canopy threads, the Windreach updrafts, the Road That Stops in the Greyfold where platforms exist only in your lantern-radius).
- Death in a gauntlet returns to the last solid ground and costs 1 mask, never a full death.

## 10. Bounds-walk (the non-combat set piece)
A rhythm traversal: NPCs sing the roll-call; each street name is a beat; Wren must be at the named spot on the beat. Fail three beats and the walk restarts from the last verse. Success holds the place without freezing it. It is deliberately slow and warm; it is the game's alternative to the Guild's cold anchor.

## 11. What we don't do
- No stamina. No weapon durability. No XP levels.
- No damage numbers on screen.
- No difficulty modes at launch; accessibility options instead (plan DES-14, `docs/design/accessibility.md`).
