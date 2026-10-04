# The Enemies' Voices (AUD-10, AUD-15, v1)

Everything that fights her is a drawing, and version one makes each of them sound like what it is drawn as: a shell
is a hard tick, a wing is air, a smudge is wet ink, a Cantor is a handbell, a Warden is brass, a Remnant is dry
paper, a salamander is embers, a moth cloud is many small wings, a pulp-wasp is a hum and a wet pop, a Sketch is
graphite, a tussock is earth, a reedling is fluff and a peep. Wren's quill stays ink and paper (AUD-03); this is the
other side of every fight, and the direction's rule that too much at once drops from the bottom (audio-direction 4).

## 1. A voice per family

`EnemySounds` (Core) is the table: a **Voice** per family, which is its **material** (the hurt and death every
drawing of that stuff shares, and what a strike it turns away sounds like, if anything) and the cues its own moves
make. The moves are named by the clips the animator already asks for (`Enemy.Clip`, `docs/design/enemy-animation.md`):
a one-shot as a clip begins, or a loop while a clip runs. A family the table does not know (a boss's part, a test's
target) is wet ink.

| Family | Material | Turned away | Moves (clip → cue) | Loops (clip → cue) |
|---|---|---|---|---|
| Marsh crab | shell | `shell_block`, a dry clack | hop → `crab_hop`, a flick off the paper | move → `crab_scuttle`, claws ticking |
| Reed skimmer | wing | — | rise → `skimmer_rise`, a thin whistle; dive → `skimmer_dive`, air torn | — |
| Smudge, Memory smudge | ink | silence (undrawn is nothing to hit) | — | — |
| Cantor | bell | — | recover → `cantor_toll`, the toll in D with its hum | — |
| Warden | brass | — | measure → `warden_measure`, the lens turned twice; telegraph → `warden_draw`, the lance drawn back; thrust → `warden_thrust`, its air and a clink | — |
| Lost Remnant | paper | — | — | move → `remnant_drift`, dry paper breathing |
| Cave-bat | wing | — | unfurl → `bat_unfurl`; swoop → `bat_swoop`, air falling and climbing | — |
| Salamander | ember | `ember_block`, a hiss | flare → `salamander_flare`, the crackle; rush → `salamander_rush`, a dry scrape | flare, rush → `ember_crackle` |
| Lantern-moth cloud | moth | `moth_pass`, almost nothing | flare → `moth_flare`, every wing open; dart → `moth_dart`, a hiss | idle, move, gather → `moth_flutter` |
| Pulp-wasp | pulp | — | spit → `wasp_spit`, a wet pop and a whizz | idle, move, spit → `wasp_buzz`, 150 Hz |
| Sketch | graphite | silence (the fill left out) | fill → `sketch_fill`, fast hatching; lunge → `sketch_lunge`, a dry drag | — |
| Tussock | earth | `shell_block`, only while it is up (breach, idle) | heave → `tussock_heave`; breach → `tussock_breach`, grit bursting; burrow → `tussock_burrow`, grit falling | ridge → `tussock_ridge`, a rumble |
| Reedling | fluff | — | move → `reedling_scurry`; peck → `reedling_peck`, two taps; lunge → `reedling_peep` | — |
| The bosses | (section 2a) | — | (section 2a) | (section 2a) |

**The materials** (`<material>_hurt`, `<material>_death`): a shell ticks and cracks (two ticks and grit running
out); a wing puffs and falls as a flutter slowing; ink smears wet and short (shorter than her own smudge, so it is
never her) and dissolves with two drops leaving it; the handbell rings **D6** (1175 Hz, the coast's tonic four
octaves above its drone) and cracks with its note sagging into shards; brass clinks and comes off in three clinks and a thud;
paper crumples quietly and is torn through; embers hiss and pop and are put out in a long hiss as the crackle goes;
moths puff and come down as dust; pulp pops and the sac bursts into a patter; graphite snaps and is rubbed out in
four strokes of the eraser; earth thuds and the mound goes down in a rumble; fluff peeps and the last peep falls.

**The tells that were missing** (wren-sounds.md §5): the crab tells as it hops, the skimmer as it rises, a smudge as
it lunges (all strikes); a Cantor's ring is a **window** and chimes as it starts, and so do every ring of the
Choir's doves and the Half-Cathedral's bells, and every ground the Complete Survey names a beat ahead.

## 2. How they play

`EnemyVoice` (World) is put on every enemy as it wakes (`Enemy.Awake`), bosses and runtime spawns included. Each
frame it reads the enemy's clip: a clip that just began plays its move's cue; a clip with a loop keeps the voice's own
`AudioSource` running (a wasp hums through its spit; a dead thing is quiet). A landed hit (`Enemy.WasHit`) is the
material's hurt unless the hit killed, a death (`Enemy.Died`) is the material's death under the kill layer the bank
already plays, and a strike turned away (`Enemy.HitBlocked`, new) is the material's block, if it has one.

**Where it stands.** Every enemy sound is played from the enemy's place: the bank pans it toward where it is
(`InkSounds.Pan`: up to 0.7 at the screen's edge, never hard to one side) and lets the distance take its gain
(`InkSounds.Falloff`: whole on screen, half a screen past the edge, nothing three screens out, where it is not
played at all). The screen's half-width comes from the camera at the play plane; the listener is the camera's.

**The voice limit** (audio-direction 4). The bank now plays every one-shot on one of **24 sources**
(`AudioDirection.VoiceLimit`), each cue carrying its rank in the direction's order (`AudioDirection.Voice`: tells,
Wren hurt, dialogue, Wren's strikes and Bind, enemy hits and deaths, world one-shots, ambience, music). When all 24
are taken, the newcomer either takes the place of the sounding voice that matters least (the oldest of the lowest
rank) or, if everything sounding matters more, is the one dropped (`InkSoundBank.Dropped`). The enemies' hurts and
deaths rank as enemy hits, their moves and loops as world one-shots; the loops are the voices' own sources and do
not count against the 24.

## 2a. The bosses' own voices (AUD-15)

**A boss is heard doing what it does, never reading.** A move's cue sits on the clip of the move itself (the sweep,
the slam, the stomp), never on a clip the fight telegraphs with: the read already has its sound, the tell, and a
second sound there would say something the telegraph does not show (audio-direction 4). So the Gatekeeper's sweep
grinds along the floor after the sweep's tell, not with it. A **loop** is the body going on (stone wings, iron feet, a
pen writing a Bind) and may run through a read, as the Gatekeeper's wingbeats run through the pass it reads with.
What is no clip of the body's is an **event** (`Voice.Events`): a count the boss already keeps (`Rings`, `Cuts`,
`Shifts`, `LimbsRedrawn`), heard each time it goes up, so nothing in the fights changed to be heard. An event's cue
may have a phase's own take, `<cue>_p<phase>` (`EnemySounds.PhaseCue`): the great bell is `bells_toll_p3`. Three die
their own way (`Voice.OwnDeath`).

| Boss | Material | Moves (clip → cue) | Loops | Events (count → cue) | Death |
|---|---|---|---|---|---|
| The Lamp-Keeper | wing (a gannet; was brass) | dive → `lamp_dive`, air torn; grounded → `lamp_grounded`, a thud and the lamp's glass rattling; return → `lamp_return`, three wingbeats climbing | beam → `lamp_beam`, the lamp humming, its glass singing | — | the wing's |
| Halvard | brass | measure, thrust (the Warden's); lunge → `halvard_lunge`; count → `halvard_count`, three brass clicks ("Three paces. I measured them."); throw → `halvard_throw`, the cord paying out; recall → `halvard_recall`, reeled home with a clink | — | — | brass |
| Cinder Warden Brann | brass | thrust (the Warden's); charge → `brann_charge`, iron feet on the grates; crosscut → `brann_crosscut`, two lances crossing with a ring | hold → `brann_hold`, the furnace roaring through his brass | `Shifts` → `brann_shift`, the grates clanking as the heat moves | brass |
| Warden-Captain Oriel | brass | strike1–3 → `oriel_strike`, a clean cut and chalk; flourish → `oriel_flourish`, one long sweep; step → `oriel_step`, a scuff | bind → `oriel_bind`, a pen writing fast | `BindsDenied` → `oriel_denied`, the nib snapping | brass |
| The Gatekeeper | earth (stone; was brass) | rise → `gate_rise`, roots creaking; sweep → `gate_sweep`, a stone wing grinding; pass → `gate_pass`; land → `gate_land`, the gate shaken | fly, pass → `gate_fly`, stone wingbeats | — | earth |
| The Choir | bell | — | — | `Tolls` → `cantor_toll`; `Cancelled` → `bell_choke`, a bell stopped by a strike | bell |
| The Half-Cathedral Bells | bell | — | — | `Rings` → `bells_toll`, a tower bell in D (`NaveBellHz`, 294 Hz, two octaves under the Cantor's), and in phase 3 `bells_toll_p3`, the great bell an octave under; `Cuts` → `bells_cut`, the rope's snap and the bell swinging to rest | `bells_death`, the last hum let go |
| Surveyor Hale | earth | quill → `hale_quill`; count → `hale_count`, stones knocked | — | — | earth |
| The Collapse | earth | shake → `collapse_fall`, rubble from the roof (its shake is the fall, not a read) | — | — | earth |
| The Fallen Star | ember | slam → `star_slam`, iron ringing the ground | walk → `star_walk`, iron feet; burn → `star_burn` | — | `star_fall`: iron striking the ground, a long hiss, the metal ticking as it cools |
| Guildmaster Voss | paper | thrust (the Warden's); lunge → `halvard_lunge`; guard → `voss_guard`, the compass-rose shield ringing deep | — | — | paper |
| Corra's Drawing | graphite | swipe → `corra_swipe`, a crayon dragged hard; stomp, stomp_outline → `corra_stomp`; swipe_outline → `corra_line`, a thin pencil line | move, move_outline → `corra_scribble` | `LimbsRedrawn` → `corra_redraw`; `SmallStruck` → `corra_small`, drawn back quick | graphite |
| The Archivist | graphite | swoop → `archivist_swoop`, an owl: almost nothing | draw → `archivist_draw`, a quill's long strokes | — | graphite |
| The Complete Survey | graphite | — (the chorus is its voice; the music keeps its beat) | — | — | graphite |
| The Reedmother's Brood | wing | thresh → `brood_thresh`, reeds thrashing; open → `brood_open`, reeds parting | burn → `ember_crackle` | — | wing |

**The Bells' score strikes no bell** (music.md §3e), and here is why it need not: the bells are heard in the nave
itself, a toll as each ring lands and takes the light, after the ring's chime has read it. A cut rope is heard twice,
its snap and the bell's last ping (the material's hurt, as the cut is the blow).

## 3. The pipeline

```
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.InkSoundsExport.Render -quit
```

(or **OWSBG → Render Wren's Sounds**) now also writes `docs/audio/sfx/enemy_sfx_<cue>.wav`, 48 kHz 24-bit mono, one
per cue above, for the sound designer to hear and replace. The six loops are rendered seamless (their tails
crossfaded onto their heads); the wasp's hum is a whole number of cycles a second so it meets itself exactly.

## 4. Verification

- `EnemySoundsTests` (EditMode, 4): every concrete enemy and boss class in the game has a voice of its own and every
  voice's cues exist with the right rank (hurts and deaths as enemy hits, moves and loops as world one-shots); each
  material sounds like itself (a shell short and high, ink low and shorter than her smudge, the handbell in D at the
  coast's tonic, brass bright, paper quiet with nothing low, earth low, a reedling's peep high, the wasp at 150 Hz,
  every loop meeting itself); the bank's order is the direction's line for line and every cue knows its place in it;
  pan and falloff by the screen's width.
- `EnemyVoiceTests` (PlayMode, 6): a crab wakes with a voice, ticks as it walks and tells before it hops; a smudge's
  hit is wet ink and its death its own under the kill layer, a crab's shell clacks at a side strike and ticks at a
  pogo; the wasp hums while it lives and not after; a Cantor's ring is a window and its toll a bell; thirty strokes
  keep to 24 and a tell is always heard, while nothing lower gets in among 24 tells; a sound stands where its enemy
  is, half as loud a screen out and not played three screens out.
- AUD-15: `BossSoundsTests` (EditMode, 2): every boss is heard doing something but the Survey, and no move sits on a
  clip that boss reads with; every event names a count the boss keeps, played as a world one-shot; a boss's own death
  is still an enemy death; its cues are its own but for the Warden's lance and lens, the Cantor's toll and the embers.
  The Gatekeeper is stone and the Lamp-Keeper a wing; the toll is strongest at D, the great bell an octave under and
  longer, the last hum longer still; a choked bell is shorter than a struck one; the heavy things (a stone landing,
  an iron slam, the Star's fall, the roof's rubble) are low and a pencil line has nothing low. `BossVoiceTests`
  (PlayMode, 3): the Bells toll after the ring's chime and toll the great bell in phase 3; Brann's floor clanks as the
  heat shifts; the Gatekeeper is silent through the sweep's read but for its tell and grinds after it.
- `InkSoundsTests` and `WrenSoundsTests` take the new cues in: a loop's join is no bigger a step than any inside it,
  and the target under her strike now answers in its own voice.

## 5. Open

- **They are sketches.** A band-passed tick is a shell by suggestion; the sound designer's recordings are the point.
- **Bosses' parts** (the Gatekeeper's stone feathers falling and pogoed, the Collapse's rubble striking, the Star's
  walls of iron) are their own objects, not the boss's clips, and are silent but for their tells.
- **The Complete Survey** has no voice of its own beyond the chorus; its ink pools are silent when struck but for the hit.
- **Projectiles** (the wasp's pellet in flight and landing) and the eraser the Cantor's toll drags are silent.
- **Steps on the beat.** A Warden's walk and a crab's scuttle are loops, not footfalls on the clip's frames.
- **Captions** for the enemies' tells wait on the captions setting (DES-14), as Wren's do.
- **Hit-stop and shake** still do not read the cue, and the hit layer does not scale with the strike's strength.
