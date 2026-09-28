# Boss Kits: the mid-game and late bosses (CMB-13, CMB-14, v1)

Seven bosses from the boss sheets, built as greybox kits: the four mid-game fights (6.4 to 6.7, CMB-13) and three
late ones (6.8 to 6.10, CMB-14). Each kit keeps to the sheet's arena, its three phases and its answers. The sheet
owns the reason and the words; the kit owns the frame data. Telegraphs keep the tier's floor (11 frames at Tier II,
10 at Tier III), and no phase has more than four attacks.

Code: `Collapse`, `Brann`, `Choir`, `Gatekeeper`, `Oriel`, `Hale`, `FallenStar` (World, `Code/World/Bosses/`), all on
the `Boss` framework (CMB-10). `BossKits.Build(id, parent, origin)` builds any of the seven arenas: an 18-unit floor between two doors, the arena
zone, the props, and the boss with the name, tier, lines, reward and scraps from its sheet. The test rigs and the
game's rooms use the same recipe.

## Shared pieces

- **`Boss.ApplySheet`** sets name, tier and the three lines from `Bosses` at runtime (the editor-built Lamp-Keeper
  and Halvard still set them through serialized properties).
- **`Boss.HitWren` / `HitWrenInCircle`** check one attack box against Wren. A parryable attack meets the Sighting
  lens first. The result is Landed only when a mask was actually taken, so an attack keeps looking through her
  i-frames.
- **`BossPart`** is a strikeable piece that is not the boss's body: a dove, a rubble block, an ink surge, a stone
  feather. It hands each hit to its owner, who decides whether it lands. A landed down-strike on a part pogoes
  Wren like any other hit.
- **`Boss.AdvanceToPhase`** moves a boss on by something other than health (Hale's stones). **`Enemy.Heal`** gives
  health back (Oriel's Bind).
- **`Updraft`** (World) is rising air that lifts Wren while she has Windmemory (`WrenController.Lift`) and does
  nothing without it. The Fallen Star's heat makes these; the Steppe's ink-swirls can use the same component.
- **The answers as hit rules.** `Boss.IsLongstroke(hit)` means the forward Flourish in the frame it strikes.
  `IsDownStrike` means the pogo and `IsUpStrike` means the belly. The kits use these three tests to ask for the
  sheet's answers.

## 6.4 The Collapse, at the bottom of Hollowvein

Four chorus lamps over four sections of the floor. The beat runs every 0.8 s and never stops for hurtstun. Each
beat lights the next lamp, and the Collapse is drawn in that section for the first 60% of the beat. That window is
the only time and place it can be struck (the Smudge rule). A beat whose lamp is out, or has rubble under it, is
lost and stays dark. Touching the Collapse does no harm: it is the mine.

| Phase | Attacks | Answer |
|---|---|---|
| 1 | Rubble: dust rises under her, then a block comes down the column and stays on the floor | Step off the dust; pogo the block to free the lamp |
| 2 | Surge (ink along the floor from the far end, toward her), rubble | Jump the surge or Longstroke it; the quill does nothing to it |
| 3 | Reach (1.4 s at the next lamp; unstruck, the lamp goes out), rubble, surge | Strike it while it reaches; it never takes the last lamp |

28 health. The arena sets `boss.collapse.defeated` and gives 2 scraps. The keystone and the count are the Runa
scene's.

## 6.5 Cinder Warden Brann, the cold furnace

Six floor sections. A hot section costs a mask while she stands on it, spaced by her i-frames. Every 3.2 s the
pattern shifts, and the sections about to heat glow orange 0.8 s before.

| Phase | Floor | Attacks | Answer |
|---|---|---|---|
| 1 | 2 of 6 cool, the pair walking round | Thrust, charge (one lance) | Parry (a parry staggers him 1 s) |
| 2 | Half cool, alternating | Cross-cut (both lances, both sides, low), hold, thrust | Jump the cross-cut and pogo him; only a Longstroke or a pogo gets through the hold, and a Longstroke breaks it (1.2 s stagger) |
| 3 | Dark: nothing burns | Hold, cross-cut, thrust, charge | Read the glow: his brass is dim (0.25) at rest and full (1.0) on every telegraph |

36 health, 3 scraps, `boss.brann.defeated`.

## 6.6 The Choir, over Aldermere's square

Three doves in a line at 2.6 units, one health between them (24). Only the doves can be struck; a hit on the Choir
itself is refused. A dove's ring lasts 32 frames. When it finishes, it hurts within 3.2 units of the bell, erases
the next of the square's four platforms, and erases Aldermere (`Verdance_Aldermere_2`) from the atlas. The square's
vantage stands by the west door, so she can re-survey between verses and the next bell erases it again. A hit on a
ringing dove stops that bell (the Cantor rule).

| Phase | Doves | Song |
|---|---|---|
| 1 | Three | In turn: one bell at a time, 0.9 s between |
| 2 | Two (the east dove leaves) | In canon: the second comes in halfway through the first |
| 3 | One (the middle dove) | Alone, 0.55 s between |

1 scrap, `boss.choir.defeated`. The Unwriter's Charter is CMB-17's.

## 6.7 The Gatekeeper, at the Overgrown Gate

Three roots are Inkthread anchors (`TetherAnchor`, which lasts for ever here): two at the gate's sides and one high
in the middle.

| Phase | Where | Attacks | Answer |
|---|---|---|---|
| 1 | On its plinth | Wing sweep along the floor; stone feathers (three shadows round her, then three feathers fall) | Jump the sweep; pogo the feathers |
| 2 | Rises to the top of the gate (7 units) | Feathers, the sweep at perch height | Thread up the roots |
| 3 | The roots tear free; it flies low (2.4 units) | Passes the width of the gate, heavy landings | Strike the belly: in the air only an up-strike lands |

32 health, 2 scraps, `boss.gatekeeper.defeated`. A retry puts it back on its plinth with its roots.

## 6.8 Warden-Captain Oriel, the Bastion's drill-yard

She has read Pell's report and fights to see whether it is accurate, so she mirrors Wren. When the fight begins she
reads Wren's equipped Charter (`Equipment.Charter`). She fights with that Charter's combo, reversed (the Surveyor's
slash, slash, thrust becomes thrust, slash, slash), and with its default Flourish. Each step's wind-up is three
times its startup, never under the tier's floor. The late Charters mirror as the Surveyor until they exist.

| Phase | Attacks | Answer |
|---|---|---|
| 1 | The mirrored combo, a step (clear of her, or into reach) | Parry the mirror: every combo step can be parried, and a parry staggers her 1 s and ends the combo |
| 2 | Combo, Wren's own Flourish (Crosshatch in front, Longstroke six units, or Blot round her), step | Whatever the Charter asks |
| 3 | At a third she steps clear and Binds, once: 60 frames, then a third of her health back. Then Flourish, combo, step, combo | Deny the Bind: a hit while she binds stops it, staggers her, and the Bind is spent |

30 health, 3 scraps, `boss.oriel.defeated`. **The stand-down:** she counts every mask Wren loses in the attempt.
Beaten with none lost, she writes `halden.oriel.stood_down`. A retry starts the count again and gives her Bind back.

**A rule changed with it.** `Licence` used to let Pell's report outrank Oriel. But she only fights once the report
has been sent, so her stand-down could never have counted. Her word now comes after the report and outranks it.
`WardensHostile` is false once she has stood them down, and otherwise true when the report is sent or Wren is
unlicensed. `LicenceTests` and `anchoring.md` now say so.

## 6.9 Surveyor Hale, the Nine Stones

The only duel. Nine stones stand two units apart across the floor. Hale sights the bare stone nearest him: his lens
is up for 36 frames, then the stone is his. Wren draws a bare stone by standing still at it for 0.8 s. A stone she
has drawn is hers, and if he was sighting it, his sighting fails. When he calls the count, a column of light rises
over each of his stones and hurts whoever stands in it. Her stones stay quiet. **The phases follow the stones, not
the wounds:** phase 2 when five stones have been drawn by either of them, phase 3 at eight. Health only decides
the end.

| Phase | Attacks | Answer |
|---|---|---|
| 1 | Sight, quill (a jab), sight | Survey faster than him; parry the quill (1 s stagger) |
| 2 | Sight, the count, quill | Stand on bare stones or her own; pogo the strikes (a down-strike on a column lands and bounces her) |
| 3 | Count, quill, sight, count | The same, with most of the floor claimed |

30 health, 2 scraps, `boss.hale.defeated`. When he has nothing left to sight, he calls the count instead; with no
stones to call, he uses the quill. **Hale's lens:** once he is beaten the sighting lens cools down in half the time
(`InstrumentBelt.CooldownOf`). His pages stay with `Stones_Hale_Pages`.

## 6.10 The Fallen Star, the anvil-crater

Magnetic iron everywhere but the seam on top where the keystone sat. Side strikes and up-strikes drift off it
(counted, refused); only a strike from above lands. It hurts to touch.

| Phase | Attacks | Answer |
|---|---|---|
| 1 | Walk; slam (a mark where she stands, then the fist comes down; the fist stays 45 frames) | Pogo the fist up onto its back and strike the seam |
| 2 | Iron walls (marked, then risen 3.6 units high either side of her, two at a time), slam, walk | Keep out of the pen, or climb |
| 3 | It burns: the walls grow to 5.4 units, past any jump, and heat rises on her side of each | Windmemory: an updraft carries her over the wall; without it the heat is only heat |

34 health, 2 scraps, `boss.fallen_star.defeated`. The keystone and the cold anvil belong to the Fire scene.

## Arena rooms

The regions are planned (`RoomPlans`), not built. `ArenaRooms` (Narrative) registers with
`RoomManager.Generators` and builds each fight's room at runtime. The scene is named after the planned room that
fights that boss: `Arena_Emberdown_Hollow_4`, `Arena_Emberdown_Stair_3`, `Arena_Verdance_Aldermere_2`,
`Arena_Verdance_Gate_2`, `Arena_Halden_Bastion_2`, `Arena_Windreach_Stones_3`, `Arena_Windreach_Star_2`. Each room has walls, paper layers, the kit, and a west spawn outside the doors. When a
region's rooms are built, its arena moves into them and that generator entry retires.

## Tests

`BossKitFightTests` (PlayMode, 13 tests) build each kit in a rig and fight it to its answers:

- **Every kit:** it stands on its sheet: name, tier, lines, scraps and phase count.
- **The Collapse:** lamps light in order; it takes hits only while drawn; rubble falls where she stood and darkens
  its lamp until pogoed; the surge hurts, the quill does nothing to it, and a real Longstroke cuts it; a reach puts
  a lamp out and a strike stops it; it never takes the last lamp.
- **Brann:** cool iron is safe and hot burns; a section glows before it heats; the cross-cut lands; the hold
  refuses the quill, takes the pogo, and breaks to a real Longstroke; in the dark nothing burns and the glow is the
  telegraph.
- **The Choir:** doves ring in turn; each toll erases a platform and the page; a re-survey recovers the page and
  the next toll erases it again; a strike stops a bell; two sing in canon, then one alone.
- **The Gatekeeper:** the roots stand as anchors; the sweep and the feathers land; feathers pogo; it rises, tears
  its roots, and then takes only up-strikes; a pass crosses the gate; a retry restores it.
- **Every kit:** each one defeated sets its flag, gives its sheet's scraps and opens its doors.

`LateBossKitFightTests` (PlayMode, 10 tests) do the same for the late three:

- **Oriel:** she mirrors the Warden's Charter, reversed (overhead, shove, sweep) with Blot; every step shows its
  wind-up and the combo lands; a real lens parry staggers her; her Flourish lands in phase 2; at a third she queues
  the step and the Bind; the Bind heals a third, once; a retry gives it back, and a hit denies it. Beaten clean,
  she stands the Wardens down even after the report; with a mask lost, she doesn't.
- **Hale:** his sighting takes a stone; her survey beats his to the next one; his count spares her stones, hits
  her on his, and its columns can be pogoed; 25 wounds leave him in phase 1 while five and eight stones move him
  on; beating him halves the lens's cooldown.
- **The Fallen Star:** side and up strikes drift and the seam takes a down-strike; the slam lands and the fist
  pogoes; walls rise either side of her, then grow when it burns, with heat beside each; the heat lifts her over
  the wall only once she has Windmemory; the walls go when it falls.

The arena-room tests cover all seven rooms.

`ArenaRoomsTests` (PlayMode, 2 tests) check that each kit has a planned room in its sheet's zone. They also travel
to every arena room in the real game and walk in to start each fight.

## Open

- **Hale can't win the survey yet.** Nothing happens if he sights all nine; the bible's stake (nine stones and the
  Guild can anchor Windreach) belongs to the fight's outcome with the region's room, where it could write
  `windreach.hale.finished`.
- **The Fallen Star's magnetism** is v1 as a hit rule (side strikes drift). It doesn't yet pull Wren or bend the
  quill's path.
- **Oriel's arena** always holds her; gating it on `pell.report_sent` belongs with the Bastion's built room, like
  the Choir's gate.
- **Inkthread doesn't exist as a movement yet** (CMB-04). The Gatekeeper's phase 2 is reachable in the tests by
  placing Wren; in play it needs the thread. The roots are already the anchors the ability will look for.
- **The Collapse keeps its own beat.** The bible's fight is the bounds-walk's last verse, so it should share the
  walk's clock (`BoundsWalk`) and Runa's chorus. When the Hollowvein walk is staged into the room, the beat becomes
  the walk's.
- **The Choir fights only if Wren tries to stop Aldermere's last day.** The arena room always holds it; gating it
  on `verdance.aldermere.stopped` belongs with the region's built room.
- **No intro cutscenes or arena cameras yet.** The arenas use the short intro wait; PRG-16 staging follows the
  art.
- **Numbers are first passes** (health, beat, shift clock, ring length) for CMB-19's tuning pass.
