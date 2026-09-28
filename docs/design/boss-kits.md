# Boss Kits: every boss after the first act (CMB-13 to CMB-16, v1)

Twelve bosses from the boss sheets, built as greybox kits: the four mid-game fights (6.4 to 6.7, CMB-13), three
late ones (6.8 to 6.10, CMB-14), Voss at the Threshold (6.11, CMB-15) and the four endgame fights (6.12 to 6.15,
CMB-16). With the Lamp-Keeper and Halvard's first fight (built into their rooms), every boss sheet now has a kit
except Reedmother's Brood (6.2, optional) and Halvard's second and third (CMB-12). Each kit keeps to the sheet's
arena, its three phases and its answers. The sheet owns the reason and the words; the kit owns the frame data.
Telegraphs keep the tier's floor (11 frames at Tier II, 10 at Tier III, 8 at Tier IV), and no phase has more than
four attacks.

Code: `Collapse`, `Brann`, `Choir`, `Gatekeeper`, `Oriel`, `Hale`, `FallenStar`, `Voss`, `HalfCathedralBells`,
`CorrasDrawing`, `Archivist`, `CompleteSurvey` (World, `Code/World/Bosses/`), all on the `Boss` framework (CMB-10).
`BossKits.Build(id, parent, origin)` builds any of the twelve arenas: an 18-unit floor between two doors, the arena
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

## 6.11 Guildmaster Aurelian Voss, the Threshold

He will not let Wren cross. The floor is six sections. **Anchoring:** from phase 2 the colour grade locks
(`HeldState.LockGrade`, the same global an anchored place sets), and he seals the section she stands in: a compass
rose is drawn on it for 24 frames, then a brass seal rises over it. A section sealed with her inside holds her for a
beat (0.8 s, `WrenController.Frozen`). She is held again only when she walks back in after leaving, and not sooner
than 1.2 s after the last hold. A seal breaks to a strike on its edge from outside; from within, the quill only rings
on it. He keeps three seals at most, and a fourth replaces the oldest. When a seal would go where she already stands
sealed, he thrusts instead.

| Phase | Attacks | Answer |
|---|---|---|
| 1 | Thrust, guard (the compass-rose shield up for 60 frames, walking forward), lunge | Parry the lance (1 s stagger); over or behind the shield |
| 2 | Anchor, thrust, anchor, lunge | Break the seal by striking its edge; don't stand where the rose is drawn |
| 3 | The Blank eats the floor from the west, a section every 1.6 s, seals and all, until two sections are left; he fights from that island. Anchor, thrust, guard | Stay off the west: the white takes a mask at a time |

40 health, 3 scraps, `boss.voss.defeated` (which `Threshold_Voss` reads before it writes `greyfold.crossed`). The
grade comes free and any hold ends when he falls or on a retry.

## 6.12 The Half-Cathedral Bells, the nave

Environmental: the bells are the boss, and the four ropes are its health. Each finished ring (40 frames) shrinks
Wren's lantern-radius by 1.6 units, from 7 down to 1.2. Once it can't shrink further, each ring costs a mask. The
radius is published as a shader global, `_OWSBG_LanternRadius`, for the lantern-radius pass (PRG-18).

**She cuts a rope only when she can see it:** within her radius, or while the Field lantern shows everything (the
Bells are `IRevealable`). Standing still for a second at the vantage by the west door steadies the radius back to
full. The phases follow the ropes.

| Phase | Bells | Answer |
|---|---|---|
| 1 | One, mid-nave | Close enough to see it; cut it |
| 2 | Two, in canon (the second comes in 20 frames into the first) | The lantern for the far one, or walk to it |
| 3 | The great bell: its rope is in the white, shown only to a radius of 4 or more, or the lantern | Steady at the vantage, then cut it before the rings shrink her again |

4 ropes, no scraps, `boss.bells.defeated`. The bells' inscriptions are the phase lines. Clarity's growth and the
bound memory are Clarity's (PRG-18) and the prologue's.

## 6.13 Corra's Drawing, the crayon room

It redraws a limb whenever one is struck: for 0.6 s after a hit it is not drawn and can't be hit, so hits must be
spaced (strike the drawn frames). It walks toward her and attacks with a crayon-arm swipe in front and a stomp where
a red crayon mark shows.

| Phase | What changes | Answer |
|---|---|---|
| 1 | Swipe, stomp | Strike, wait for the line, strike |
| 2 | It draws a second Voss, small, beside it. Strike the small one and the drawing draws itself bigger (+2 health) and draws him back in 2 s | Don't strike the small one |
| 3 | The crayon runs out: outline, faster (half the pauses and wind-ups, never under the tier's floor, half the redraw), and the room's colour goes (`_OWSBG_Outline`) | The same, faster |

32 health, 2 scraps, `boss.corras_drawing.defeated`, which opens `Capital_Corra` and the carry.

## 6.14 The Archivist, the mirror-Observatory

Corvin, an enormous half-drawn owl at perch height. **He draws, and the drawings are real while his quill is on
them**: walls either side of her and a floor over her head (Ground colliders) for 3 s. Her drawing hunts her for
6 s: it walks to her and jabs, with its own wind-up. The quill passes through it: it is her. His quill hand can be
struck only while he draws. Struck, every drawing is unmade, he takes the hit and he staggers 0.8 s.

| Phase | Attacks | Answer |
|---|---|---|
| 1 | Draw (walls, floor), swoop (low across the room; contact hurts) | Strike his quill hand |
| 2 | Draw her, draw, swoop | Ignore her drawing; strike his hand |
| 3 | He draws the Atlas frame round the arena, stops drawing, and holds at the centre. The frame closes a wingspan (0.6) every 0.8 s, down to 4 units wide; outside it is the page, and the page hurts | Longstroke the frame's edge (it goes back 1.2); strike him |

40 health, 3 scraps, `boss.archivist.defeated`, which `Capital_Corvin_After` reads to offer the keystone and the
Rest.

## 6.15 The Complete Survey, the Observatory floor

The Great Atlas itself; it can't be struck, only its ink. The floor is six sections of page. The chorus keeps the
beat. Each beat names a section, shown a beat ahead. **When the beat lands, if she is anywhere else, the region's
ink fixes her where she stands:** held 0.6 s and a mask lost. Every third beat, ink pools where the named ground
was (three pools at most). Striking a pool wounds the Atlas. Every ten beats, the last two are a verse break:
nothing is named, time to Bind.

| Phase | Ink | Beat | The named ground walks |
|---|---|---|---|
| 1 | Saltmarrow's tide | 0.9 s | One section a beat |
| 2 | Emberdown's ash | 0.75 s | Two a beat |
| 3 | Halden's late afternoon | 0.6 s | Three a beat |

24 health (pools), no scraps, `boss.complete_survey.defeated`, and the Sky (the arena grants what the sheet
grants), which `Ending_Open_After` reads.

## Arena rooms

The regions are planned (`RoomPlans`), not built. `ArenaRooms` (Narrative) registers with
`RoomManager.Generators` and builds each fight's room at runtime. The scene is named after the planned room that
fights that boss: `Arena_Emberdown_Hollow_4`, `Arena_Emberdown_Stair_3`, `Arena_Verdance_Aldermere_2`,
`Arena_Verdance_Gate_2`, `Arena_Halden_Bastion_2`, `Arena_Windreach_Stones_3`, `Arena_Windreach_Star_2`,
`Arena_Greyfold_Threshold_2`, `Arena_Greyfold_Cathedral_2`, `Arena_Blank_Capital_2`, `Arena_Blank_Capital_4`,
`Arena_Halden_Observatory_2`. Each room has walls, paper layers, the kit, and a west spawn outside the doors. When a
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

`VossFightTests` (PlayMode, 5 tests): his sheet and patterns; the thrust lands after its wind-up, a real lens parry
staggers him, and the shield turns the quill from the front but not over it or from behind; phase 2 locks the grade,
seals her section and holds her for a beat, not again while she stands still, and again when she walks back in; a
seal ignores strikes from inside and breaks to one on its edge from outside; phase 3's white takes the western seal,
hurts her in the west, and stops at his two-section island; beating him sets the flag, pays three scraps and frees
the grade; a retry mid-hold frees her and unseals the arena.

`EndgameBossFightTests` (PlayMode, 9 tests):

- **The Bells:** a ring shrinks the radius and publishes it; at the smallest a ring takes a mask; the vantage
  steadies it. A rope out of sight, or of a bell not yet ringing, won't cut, and one in sight will. The lantern
  shows the far rope, and the great rope needs a wide radius even up close. Four cuts win.
- **Corra's Drawing:** a struck limb can't be struck again until redrawn. Striking the small one heals the drawing
  and he is drawn back. In outline it is faster and the room's colour goes, until it falls. The swipe and the stomp
  land.
- **The Archivist:** his hand is only a hand when he isn't drawing. The walls and floor are real, either side of
  her, until his hand is struck or his quill lifts. The swoop lands. Her drawing can't be struck and does land its
  jab, and is unmade by his hand. The frame closes a wingspan a beat, the page takes her, and a real Longstroke
  pushes the edge back.
- **The Complete Survey:** on the named ground she is safe; off it she is fixed and loses a mask. Walking the
  bounds, nothing fixes her. Between verses nothing is named. Pools wound it, the phases quicken the beat and
  lengthen the stride, and the win grants the Sky.

The arena-room tests cover all twelve rooms.

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
- **Voss's sheet has sections freeze mid-air and platforms lock.** v1 seals floor sections only; the Threshold's
  built room, with platforms to lock, can add them. His "shrinking island" is the floor's last two sections, not a
  platform yet. Halvard's third kit, fought before him at Threshold_1, is still CMB-12's.
- **The lantern-radius is a number, not yet a picture.** The Bells publish it and the ropes obey it, but the nave
  doesn't go white beyond it until PRG-18 draws it; the sheet's "outline in peripheral vision" is that pass's too.
  Cutting the ropes by Inkthread waits for the thread; v1 cuts them with any strike.
- **The Remnant Charter draining the Drawing's colour** is CMB-17's.
- **The Archivist's second phase draws her plainly.** Her drawing jabs; it doesn't yet use her Charter's combo the
  way Oriel's mirror does.
- **The Complete Survey keeps its own beat and names its own ground.** The bible's is the bounds-walk and Runa's
  chorus singing the roll-call (`BoundsWalk`); when the Observatory is built, the beat and the named ground should be
  the walk's, like the Collapse's. It fights only in the true ending; its room always holds it.
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
