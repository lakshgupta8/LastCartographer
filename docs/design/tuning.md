# Tuning Pass (CMB-19, v1)

The numbers that cut across the fights: what a hit takes, how long each tier's attacks are read, how much health
each boss has, what enemies take, and what the Inkwell buys. The rules sit in one table, `Tuning` (Core). The
kits declare their attacks from their own fields (`Boss.Kit()`), and the tuning tests hold every kit to the rules.
These are starting values, set by reasoning rather than playtest. They are ready for the feel-test (PRO-03) and the
first external round (PRO-04) to move.

Frames are fixed steps at 60 Hz (PRG-24 moved physics from 50 Hz). The kits had been written and tested at 50 Hz,
so every read in frames had become a sixth shorter in real time. This pass decides tier by tier where that stays.

## 1. Damage to Wren

| What | Masks | Rule |
|---|---|---|
| A hit: strikes, contact, projectiles, hazards | 1 (`Tuning.Hit`) | Combat doc 1: most hits take 1 |
| A **slam** | 2 (`Tuning.Slam`) | An attack that comes down on floor it marked first. It always brings the hard camera shake (0.35, scaled by the player's shake option) |

The slams are the Collapse's rubble, the Gatekeeper's landing, the Fallen Star's fist and Corra's Drawing's stomp.
The Lamp-Keeper's dive comes at Wren, not at a mark, so it is a strike. Before this pass every attack in the game took
1 mask, and only the Fallen Star shook the camera.

## 2. Telegraphs per tier

Each attack is one of four kinds (`AttackKind`):

- **Strike:** a telegraphed blow.
- **Slam:** a strike that comes down on marked floor.
- **Window:** a long read the fight is built round, such as a ring, a sighting, a beat or the heat's glow.
- **Shape:** changes the arena and hurts no one (walls, marks, a step).

| Tier | Floor | Typical (median of a boss's strikes) | Real time |
|---|---|---|---|
| I | 12 | 18 ± 2 | 300 ms |
| II | 11 | 16 ± 2 | 267 ms |
| III | 10 | 14 ± 2 | 233 ms |
| IV | 8 | 12 ± 2 | 200 ms |

- **The floor** (combat doc 8): nothing that is read goes under it. `Boss.Telegraph` already raised short reads to
  the floor; the audit now checks the declared reads as well.
- **The typical read** makes the tiers feel different. Before this pass the four tiers all sat around 15 frames:
  a Tier IV boss read as slowly as the first.
- **A slam reads at least the tier's typical telegraph.** Two masks come with a full read. When Corra's Drawing runs
  out of crayon and halves its wind-ups, the stomp stops at 12 frames, not 7.

## 3. Boss health

Health = the tier's base × access. The base is strikes of the Surveyor's quill, which does 1 damage a strike.

| Tier | I | II | III | IV |
|---|---|---|---|---|
| Base (strikes) | 30 | 34 | 38 | 42 |

| Access | Scale | Meaning |
|---|---|---|
| Open | 1.0 | The body can be struck whenever Wren reaches it |
| Windowed | 0.8 | Only in windows the fight opens, or the fight takes her time elsewhere |
| Paced | 0.6 | The fight hands out a strike at its own pace |
| Counted | — | Health is a count of things |

A windowed boss takes longer per strike, so it gets fewer strikes. The time a fight takes should still rise with
tier.

| Boss | Tier | Access | Health (was) | Why that access |
|---|---|---|---|---|
| The Lamp-Keeper | I | Windowed | **24** (24) | Open only while grounded after a dive |
| Reedmother's Brood | I | Windowed | 24 (—) | Not built: the nest opens between broods |
| Halvard (chapel) | I | Open | **30** (30) | |
| The Collapse | II | Windowed | **27** (28) | Drawn for 60% of a beat, in one section |
| Cinder Warden Brann | II | Open | **34** (36) | |
| The Choir | II | Windowed | **27** (24) | Only the doves |
| The Gatekeeper | II | Open | **34** (32) | |
| Halvard (bridges) | II | Open | 34 (—) | Not built |
| Warden-Captain Oriel | III | Open | **38** (30) | Her Bind takes a third back unless it is denied |
| Surveyor Hale | III | Windowed | **30** (30) | The stones take her time |
| The Fallen Star | III | Windowed | **30** (34) | Only the seam, from above |
| Voss | III | Open | **38** (40) | |
| Halvard (Threshold) | III | Open | 38 (—) | Not built |
| The Half-Cathedral Bells | III | Counted | **4** (4) | Four ropes |
| Corra's Drawing | IV | Windowed | **34** (32) | A 0.6 s redraw after every hit |
| The Archivist | IV | Windowed | **34** (40) | The hand, while he draws |
| The Complete Survey | IV | Paced | **25** (24) | A pool every third beat |

This removes three inversions. Halvard at Tier I (30) had more health than the Collapse and the Choir at Tier II.
Brann at Tier II (36) had more than every Tier IV fight. The Archivist (40, windowed) had more than Voss.

**Where it lives.** `Boss.ApplySheet` and `Boss.Awake` take health from the table for any boss standing on a sheet
the table knows. A test rig without a sheet keeps what it is given. `BossKits` no longer passes numbers, and
`ProjectSetup` writes the table's numbers into the Lamp-Keeper's and Halvard's rooms.

## 4. Enemies

| Family | Health (strikes) | Contact | Answer |
|---|---|---|---|
| Reed skimmer | **2** (3) | 1 | Fodder on the wing |
| Marsh crab | 3 | 1 | Pogo only |
| Smudge | 3 | 1 | Drawn frames only |
| Cantor | **4** (3) | 1 | Longstroke reaches it; a hit stops the ring |
| Warden | **5** (3) | 1 | Parry: the 1 s stagger is a combo and change |
| Lost Remnant | **5** (3) | 1 | The Blank's |
| Cave-bat | 2 | 1 | Emberdown's fodder on the wing: struck as it swoops |
| Salamander | 3 | 1 | Its back burns: pogo only |
| Moth cloud | 3 | 1 | The quill passes through it: Blot it, then strike |
| Pulp-wasp | 3 | 1 | Keeps the quill's reach away: Longstroke the line |
| Sketch | 4 | 1 | An outline past her lantern-radius; drawn inside it, any hit |
| Tussock | 3 | 1 | Shelled and under the grass: pogo it when it surfaces |
| Reedling | 2 | 1 | One is fodder; the clutch of three is a swarm: Blot it |

Before this pass every enemy in every scene was at the component's default: 3 health, 1 contact.

**Where it lives.** `Enemy.Awake` takes the family's numbers from the table, which outranks the scene. The scenes
were updated as well, so the inspector shows the same numbers. A family the table doesn't list, such as the
memory-holding smudge, keeps its own numbers.

## 5. Wren's damage

### The quill

Sustained damage from the whole combo swung without a pause (`CharterProfile.QuillDamagePerSecond`). The rule: no
Charter falls below 0.85× the Surveyor or rises above 1.75×.

| Charter | Combo damage | Frames | Damage/s | × Surveyor | Pays with |
|---|---|---|---|---|---|
| Surveyor | 1 · 1 · 1 | 50 | 3.6 | 1.00 | Ink 25% faster |
| Warden | 2 · 1 · 3 | 68 | 5.3 | 1.47 | Shorter dash, slow wind-ups |
| Drifter | 1 · 1 · 1 | 31 | 5.8 | 1.61 | Four masks at most |
| Ferryman | 1 · 1 · 2 | 62 | 3.9 | 1.08 | |
| Unwriter | 1 · 1 · 2 | 57 | 4.2 | 1.17 | Bind costs 4 |
| Remnant | 1 · 1 · 1 | 53 | 3.4 | 0.94 | Its drain is the damage |

### The Flourishes

| Flourish | Cost | Damage to one target | Per pip |
|---|---|---|---|
| Crosshatch | 3 | 6 × 1 | 2.0 |
| Longstroke | 3 | **3** (was 2), piercing | 1.0 |
| Blot | 4 | 1, round her, and 2 s slow | Utility |

The rule: every pip is a strike earned, so a damaging Flourish never gives back less than the strikes it cost.
Longstroke at 2 damage for 3 pips was a loss on one target, and it is now 3. Crosshatch stays the best single-target
damage (combat doc 4).

## 6. The Inkwell economy

| | Value | Reads as |
|---|---|---|
| Well | 9 | Three Binds |
| A strike | +1 pip (×1.25 Surveyor) | Three strikes a mask, 2.4 for the Surveyor |
| Bind | 3 (Unwriter 4) | One mask |
| Inkthread | 2 (Ferryman 1) | Cheap enough to use in a fight |
| Iris tincture | +5 | More than one Bind, less than a full well |
| At a vantage | 1 pip / 10 s | For between fights, not during them |

**The tempo check.** A fight pays in ink its health × 1 pip:

- the smallest fight (the Lamp-Keeper, 24) earns about eight Binds' worth;
- the Surveyor's ink comes a quarter faster (24 × 1.25 = 30 pips, ten Binds' worth).

Wren can't bank it: the well holds three Binds. The choice between damage (Crosshatch, 2 per pip) and a mask
(Bind, 3 pips) comes up every nine strikes, which is the tempo the combat doc asks for.

## 7. What changed

| | Before | After |
|---|---|---|
| Slams (Collapse, Gatekeeper, Fallen Star, Corra) | 1 mask; only the Fallen Star shook | 2 masks; all four shake |
| Lamp-Keeper beam, dive | 14, 16 frames | 18, 20 (back to 300 ms) |
| Halvard thrust, lunge, survey, count | 12, 14, 16, 18 | 14, 17, 19, 22 |
| Gatekeeper landing | 14 | 16 (a slam reads the tier's typical) |
| Corra swipe, stomp (outline) | 14, 16 (7→8, 8) | 12, 14 (6→8, 12) |
| Boss health | Hand-set per kit | Tier base × access (§3) |
| Enemy health | 3 for every family | Per family (§4) |
| Longstroke | 2 | 3 |

**Unchanged, and within the rules:** Brann, the Collapse's reads, Oriel, Hale, the Fallen Star's reads, Voss and the
Archivist. Oriel's mirrored combo is read from its first step: the Surveyor's thrust, 4 × 3 = 12 frames.

## 8. Tests

**`TuningTests`** (edit mode) checks:

- floors and typical reads fall with the tier;
- every sheet has a health, the table's tier is the sheet's, and health never falls with tier within an access;
- the enemy families' numbers;
- the Inkwell's trades;
- every Charter's quill damage against the Surveyor's;
- the Persistent scene's Wren, the Lamp-Keeper's lighthouse and Halvard's chapel carry the table's numbers.

**`TuningAuditTests`** (play mode) builds all twelve kits plus the Lamp-Keeper and Halvard rigs, then checks every
declared attack against the floor, the damage by kind, the slam read, the tier's typical median and four attacks a
phase. It logs the whole table. It also checks:

- the slams are exactly the four above;
- a slam takes two masks and shakes the camera, and a strike takes one and doesn't;
- enemies take their family's numbers.

The kit fight tests now expect two masks from the rubble, the fist and the stomp. They also count strikes to the next phase from the boss's health (`BossHits`), not from the old numbers, so the next retune doesn't break them.

## 9. Open

- **Feel.** All of this wants the feel-test (PRO-03) and the first external round (PRO-04). Three things in
  particular:
  - Oriel at 38 against her stand-down, which needs no mask lost (boss sheets: it may already be too hard);
  - the Warden's 5 strikes in the early rooms;
  - Tier IV reads at 12 frames.
- **Quill upgrades** (three, GDD 7; the Halden smith, DES-10) will raise the quill's damage. Tier III and IV health
  should then be re-based on the expected quill at that point in the route.
- **Hitstop** (2 on hit, 5 on kill, 8 on parry) and the i-frames after a slam are not part of this pass.
- **Seeds against health** (economy §2) are unchanged. The Warden gives 2 seeds for 5 strikes, where the Cantor
  gives 3 for 4.
