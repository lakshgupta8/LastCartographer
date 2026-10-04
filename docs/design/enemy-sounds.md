# The Enemies' Voices (AUD-10, v1)

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
| Halvard, Brann, Oriel, the Gatekeeper, the Lamp-Keeper | brass | — | (their tells carry the fight) | — |
| The Choir, the Half-Cathedral Bells | bell | — | — | — |
| Hale, the Collapse | earth | — | — | — |
| The Fallen Star | ember | — | — | — |
| The Archivist, Corra's Drawing, the Complete Survey | graphite | — | — | — |
| Voss | paper | — | — | — |
| The Reedmother's Brood | wing | — | — | — |

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
- `InkSoundsTests` and `WrenSoundsTests` take the new cues in: a loop's join is no bigger a step than any inside it,
  and the target under her strike now answers in its own voice.

## 5. Open

- **They are sketches.** A band-passed tick is a shell by suggestion; the sound designer's recordings are the point.
- **The bosses' own voices.** They are only their materials here: Brann's furnace, the Star's fall, the Bells'
  ropes, Corra's pencil want cues on their clips as the plain enemies have (`EnemySounds.Family(...)` takes them).
- **Projectiles** (the wasp's pellet in flight and landing) and the eraser the Cantor's toll drags are silent.
- **Steps on the beat.** A Warden's walk and a crab's scuttle are loops, not footfalls on the clip's frames.
- **Captions** for the enemies' tells wait on the captions setting (DES-14), as Wren's do.
- **Hit-stop and shake** still do not read the cue, and the hit layer does not scale with the strike's strength.
