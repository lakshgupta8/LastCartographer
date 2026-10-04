# The World's Sounds and the Pages' (AUD-11, v1)

The last silent side of the game was everything that is not a fight: the rooms, the lamps, the desk, the seeds,
the ledger, the decisions, and every page of the atlas. Version one gives each of them a sound made the same way as
Wren's and the enemies' (`docs/design/wren-sounds.md`, `docs/design/enemy-sounds.md`): ink and paper, from a few
lines of noise, filters and envelopes, never a recording.

## 1. The cues

`WorldSounds` (Core) is the table, registered into `InkSounds` like the others. Three kinds:

**The pages** (`ui_sfx_*`, the **Ui bus**: heard while the game is paused and the room is not).

| Cue | When | What | Gain |
|---|---|---|---|
| `ui_open` | a page opens: options, desk, atlas, shop, ledger, the journal alone, her choices laid out | paper lifted | 0.5 |
| `ui_close` | a page closes | paper set down | 0.5 |
| `ui_back` | back a page in the options, or out of listening for a key | a soft flap | 0.4 |
| `ui_move` | the cursor moves to another row, place or choice | the nib's tick | 0.35 |
| `ui_tick` | a setting changes (a volume, a switch, a Charter, a slot, a proposed fate) | a tick with a note in it; on a volume row pitched by the level, an octave across the slider (`WorldSounds.TickPitch`) | 0.45 |
| `ui_select` / `ui_denied` | a choice taken, or refused (unaffordable, no licence, nowhere to go) | a short stroke / a dry dot | 0.5 / 0.4 |
| `ui_line` | a line of dialogue shown | a quick scratch, never in the way of the words | 0.25 |
| `ui_toast` | a note in the journal, a tutorial prompt | two small taps | 0.35 |

**The world** (`world_sfx_*`, the Sfx bus, ranked as world one-shots: kept after the fight's sounds).

| Cue | On | What |
|---|---|---|
| `page_turn` | `RoomManager.Transitioned` | a room changes: the page turned, slowly |
| `lamp_lit` | `TravelPoint.Lit` (new: a lamp lit while the room is live, not one already lit when it woke) | a whump and a ring of glass, from the lamp |
| `desk_rest` | `DraftingDesk.Rested` | the pen set down, the chair, a long breath |
| `seed` | `IrisSeed.Collected` | a drop, from the seed, pitched up a little for each seed it is worth (`WorldSounds.SeedPitch`, up to a fifth) |
| `memories_back` | `Memories.Recovered` | the ink refilling |
| `buy` | `Economy.Bought`, `MaskBought`, `SlotBought` | seeds counted out |
| `ledger_take`, `commission_done`, `stamp`, `commission_failed` | `Commissions.Changed` to taken, fulfilled, closed, failed (posting is silent) | the pen's tick and a stroke; a long stroke and a tap; a stamp; the line struck through |
| `fade_step` | `FadeStages.Changed` | a place fades a stage: one soft pass of the eraser |
| `erased` / `recovered` | `FadeStages.Erased` / `Recovered` (the stage change that comes with them is not sounded twice) | the eraser dragged twice and the paper left / ink drawn back |
| `anchor`, `held`, `released` | `Places.FateChanged` | a stake driven and the Guild's seal pressed; a hand laid on the page; the pen lifted and a breath |
| `waypoint`, `travel` | `Atlas.WaypointFound`, `FastTravel.Arrived` | a mark made on the map; the map folding in three flaps |
| `redrawn`, `seal_break` | `PlayerRespawn.AnyRespawned`, `AnyWaxSealUsed` (new static mirrors) | the pen drawing her again; the wax cracked |
| `fell` | `FallCatch.AnyFell` (new) | a page's flutter and a smudge |
| `barred` | `RoomTransition.Bumped` (new, once per two seconds like the caption) | a dull knock, from the way |
| `offered` | `Offerings.Offered` | a wet stroke and the handbell, far |
| `pellet_land` | `EnemyProjectile.Spent` (new) | a wet pat, where it lands |

**Hers** (`wren_sfx_*`, with her strikes): the Instruments (`dart_throw`, `plumb_drop`, `lens_raise`, `lantern_light`,
`hook_cast`, `tincture_drink`, `seal_set`, from `InstrumentBelt.Used`; a refused use is her dry dot), `parry` (the
lens rings, full gain: it is the answer to a tell), `belt_turn`, `charter` (a Charter put on, not the one she woke
in), `clarity_gone` and `clarity_grew`, and `prompt` (the smallest tick when something to read comes into reach).
`WrenSounds` also plays the world's `ability` (`AbilitySet.Unlocked`: a flourish rising to the bell), delivered as
`world_sfx_ability.wav`.

## 2. How they play

`WorldSoundHooks` (World) wakes with the game beside the bank and answers the world's events; `WrenSounds` answers
hers; `UiSounds` (UI) is called by each view at the moment it does the thing. Everything goes through `InkSoundBank`,
which now plays a cue on its bus (`InkSounds.Cue.Bus`: the pages on the Ui bus's gain, the rest on the Sfx bus's) and
at a pitch. A sound with a place in the room (a lamp, a seed, a shut way, a pellet) plays from it, panned and quieter
past the screen as the enemies' do. A purchase or a decision at a page sounds twice on purpose: the page's
select, then the thing itself from the world.

## 3. The pipeline

The same render command as Wren's (**OWSBG → Render Wren's Sounds**, `InkSoundsExport.Render`) writes
`docs/audio/sfx/world_sfx_*.wav`, `ui_sfx_*.wav` and the Instruments as `wren_sfx_*.wav`.

## 4. Verification

- `WorldSoundsTests` (EditMode, 4): every Instrument, decision and commission step has its cue; the pages ride the Ui
  bus and the world the Sfx bus, all ranked as world one-shots, the pages at half a strike's gain or less, and the
  paused mix keeps the Ui bus whole while the Sfx bus is silent; each sounds like what it is; a slider's tick and a
  seed's drop carry their values in their pitch.
- `WorldSoundsPlayTests` (PlayMode, 6): a lamp lit while she is there whumps once and one already lit is silent; a
  seed drops pitched by its worth; three decisions, a fade, an erasure (one sound, not two) and a commission's
  steps in order; a shut way knocks once from where it is, a pellet lands, an ability is a flourish; the options page
  opens, moves, ticks the music slider at its level and closes; paused, the pages are heard and the room is not.
- `InkSoundsTests` renders and checks every new cue with the rest (peak, determinism, no click at the end) and the
  deliverables.

## 5. Open

- **They are sketches.** The sound designer's paper, ink and wax are the point of the delivery files.
- **Steps.** Wren's feet on each region's ground (boards, ash, moss, stone, grass) are still one pen-set-down.
- **Doors and props** that move (the lift, the bridges, the bell rope outside a fight) have no sound of their own.
- **The title, the save and the credits** pages are not hooked; the endings runner's pages neither.
