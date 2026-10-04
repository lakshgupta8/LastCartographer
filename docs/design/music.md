# The Score: the regions, the bosses, the Blank and the endings (AUD-04, AUD-06, AUD-07, AUD-08, v1)

The direction (audio-direction 2, 4, 6) asks for music as stems looping on whole bars at the region's beat, with
the region's silence share as a target, a boss theme that arrives with the first telegraph, adds or changes a layer
on the next bar line and resolves on the roll-call's answer in the boss's region's key. Version one builds that
system and scores the coast's two themes for it, from notes and instruments in code.

## 1. The system

`Score` (Core) holds **instruments** (a harmonic recipe and an envelope), **themes** (stems of notes as scale
degrees at beat times) and the **renderer** (every stem to one loop of samples, exactly the theme's bars long, a
note's release wrapping to the loop's start so the join is seamless). `MusicDriver` (Narrative) wakes with the game:

- **The theme** is decided every frame: a boss whose fight has told (`Enemy.Telegraphed` from a `Boss` with its
  fight on), else the room's region, else nothing. The room's ambience carries the approach; the theme arrives with
  the first telegraph.
- **Rendering** happens once per theme on a worker thread (`Task.Run`), the stems becoming clips on the main thread
  when done. Nothing is loaded from disk.
- **Playing** is one `AudioSource` a stem, all `PlayScheduled` on the same DSP time, each at its level times the
  Music bus's gain and under an `AudioLowPassFilter` at the bus's cutoff (so the snapshots, the Blank's filter and
  the player's Music volume all apply; paused, the theme goes on low and dull, through a wall).
- **Layers** fade over 0.6 s from their bar line, never cut: a region theme's combat drive comes in while the mix is
  in combat; a boss theme's stems enter by phase, a phase change waiting for the next bar line, and a stem may
  leave at a phase too (`Stem.Until`: the direction's "adds or changes a layer").
- **Defeat** stops the stems and plays the resolution: the roll-call's answer on the fiddle over the drone, in the
  boss's region's key; nothing else plays until it has rung. A reset returns the room's theme.
- **Region to region is a handover** (AUD-06): the next theme's stems are scheduled on the playing theme's next bar
  line, and there the two cross over for two seconds, the old going out as the new comes in, so a walk across the
  Bone Bridge ends a coast bar and starts a highland one. Into or out of a boss's theme the change is now (the
  fight's theme arrives with the telegraph), the old stems going out over the short fade. Nothing is cut.

## 2. Saltmarrow

*The tide keeps the time: a drone that swells and draws back, and a fiddle that waits for it.* D Dorian, 67 bpm,
bars of four. **Ten bars a loop: eight sound, two rest**, so a fifth of the loop is the room alone: the direction's
20% silence, by design and measured.

| Stem | Instrument | What |
|---|---|---|
| `bed` | drone (two octaves down, low-passed, paired reeds) | the tonic and the fifth in two swells of four bars, the tide in and out; drawn back a beat early so its release is done as the rest begins |
| `pulse` | tongue drum | on one and three, a ghost before every other bar turns |
| `lead` | hardanger fiddle | waits two bars, then a long phrase; the second phrase lifts into the fifth and quotes the roll-call's answer with its third bent into the mode (A F E D), home and held |
| `voices` | low whistle | under the second phrase, a slow line down to the fifth below |
| `drive` (combat) | bowed psaltery | eighths on the tonic, fifth and octave: the fight's pulse, only while the mix is in combat |

## 3. The Lamp-Keeper

*The lamp turns; the beam sweeps low and slow; she dives through it.* D Dorian at the coast's beat, eight bars, no
rest. The stems enter by phase:

| Phase | Stem | Instrument | What |
|---|---|---|---|
| 1 | `bed` | drone | the tonic under the fourth, then the fifth: the lamp turning |
| 1 | `pulse` | tongue drum | driving: one, two, three-and, four |
| 1 | `lead` | fiddle | the beam's sweep, a rising figure over a bar, again a step higher, then the dive falling through it |
| 2 | `voices` | low whistle | the lamp splits: a second line in thirds under the first |
| 3 | `bells` | bell | the lamp gutters: a high toll on every bar |

Beaten, it resolves on A F E D over the drone. The Boss snapshot's slow release (2.5 s) is the aftermath.

## 3a. The other regions (AUD-06)

Each to its row of the direction's table (audio-direction 2): the mode, the beat, the band, the brief, the silence
share as near as whole bars come. Every theme has the four stems the direction asks for and a combat drive.

**Emberdown.** *A work-song for many voices: everyone sings, nobody solos, the anvil keeps the count.* G Mixolydian,
75 bpm. Nine bars and one of rest (10%).

| Stem | Instrument | What |
|---|---|---|
| `bed` | hurdy-gurdy (the wheel's bourdon) | tonic and fifth under everything, drawn back a beat before the rest |
| `pulse` | frame drum | one, two-and, three, four |
| `count` | anvil | on two and four of every bar |
| `lead` | work-song chorus | the call and its answer, twice, the second higher and up to the flat seventh; a tag home |
| `voices` | tuba | the roots |
| `drive` (combat) | hurdy-gurdy (the trompette) | the buzz in eighths |

**The Verdance.** *Mostly nothing. When music comes it is one bowed voice in a very large room, and it stops before
it resolves.* E Phrygian, 50 bpm. Three bars and seven of rest (70%).

| Stem | Instrument | What |
|---|---|---|
| `bed` | the root-chapel's organ pedal | the tonic, under the phrase only |
| `pulse` | Cantor handbell | two bells in three bars |
| `lead` | viola da gamba (harmonics) | one phrase, stopping on the flat second, unresolved |
| `voices` | bowed glass | a high tone, held |
| `drive` (combat) | handbells | a bell a beat, tonic and second |

**Halden.** *A handsome clockwork piece that loops a bar it never finishes: the Stillness, heard.* C major, 100
bpm. Seventeen bars and three of rest (15%): four phrases of I IV V vi, the fifth going to the sixth every time and
never home, then the first bar again, cut off after two beats.

| Stem | Instrument | What |
|---|---|---|
| `bed` | string quartet (the cello) | the root of each chord |
| `pulse` | music box | a tick on every beat |
| `lead` | harpsichord | the broken chords |
| `voices` | string quartet (the violin) | the top tone of each chord |
| `drive` (combat) | Guild brass | root and fifth in eighths |

**Windreach.** *Open air: a long flute and a plucked string over the wind, and the camp's drum when the fire is
lit.* A pentatonic, 60 bpm. Five bars and three of rest (37.5%, the direction's 35% as near as whole bars come).

| Stem | Instrument | What |
|---|---|---|
| `bed` | wind harp | tonic, fifth and octave, swelling |
| `pulse` | hand drum | soft, on one and three |
| `lead` | long flute | a wide slow line, home at the end |
| `voices` | overtone voice | the drone an octave under, its fifth and octave partials loud |
| `drive` (combat) | cittern | picked in eighths |

Each region's resolution (the answer on the fiddle over the drone, in its key) is rendered too, for its bosses.

## 3b. The bosses (AUD-07)

**The Wardens share the Guild's motif** (`Score.GuildMotif`: paces counted, 0 0 4 4 | 5 4 2 0) on the Guild's
brass, each in the key of the region he is fought in; the drone, drum, whistle and bell under Halvard's are the
Guild's travelling band. **Every other boss fights to its region's motif** (`Score.SharedThemeOf`): the region's
own theme with its rests gone, the bed, pulse and drive from the first telegraph, the lead in the second phase, the
voices in the third. That covers the optionals (the Choir, Hale, the Fallen Star). The rhythm bosses and the Brood
have their own since AUD-13 (section 3d), and the Gatekeeper, Oriel, the Bells and Corra's Drawing since AUD-14
(section 3e).

| Boss | Key, beat | Loop | Phase 1 | Phase 2 | Phase 3 |
|---|---|---|---|---|---|
| Halvard (three fights: `halvard`, `halvard-halden`, `halvard-greyfold`) | the chapel's D Dorian at 67; Halden's C at 100; the Threshold's D at 40 | 8 bars | drone; drum, three paces and a rest a bar; brass, the motif | whistle: the survey's marks, one a bar | bell: the count, tolled on every beat |
| Cinder Warden Brann | G Mixolydian, 75 | 8 bars | hurdy-gurdy, the furnace's roar; frame drum; anvil on one and three ("I am the schedule"); brass, the motif | tuba: both lances, cross-cuts under | the roar **leaves** (the furnace is dark); bell: only his brass glows |
| Guildmaster Voss | the Greyfold's held tone on D, 40 | 6 bars | a held tone that frays; a bowed cymbal a bar; brass, the motif, slow and formal | low choir, all at once: "Hold. Everything holds." | the tone **leaves** (the white); the Half-Cathedral's bells on every beat |
| The Archivist | the Blank's D at 33 | 4 bars | celesta on the off-beats (the clockwork, mirrored); the Remnant's voices; reversed piano: the roll-call inverted about its reciting tone | celesta: her drawing sings the answer the right way up, above | reversed piano chords rising a beat: the frame closes |

The resolution is rendered in every key a boss falls in, the Greyfold's and the Blank's too. `<<sing archivist>>`
now sounds in Corvin's scene (`Capital_Corvin`), the roll-call pulled the other way, before the choice.

## 3d. The rhythm bosses and the Brood (AUD-13)

**The Collapse and the Complete Survey are scored to the beat the fight keeps** (audio-direction 4): the music is the
timing aid a hearing player gets, and the lamps and the named ground are the one everyone gets. Each has a pulse on
every beat and nothing between, and the chorus calls a name on the beat with its lift half a beat ahead, exactly as a
bounds-walk calls (`Score.CallDegrees`). Their themes keep the fight's own clock (`Theme.KeepsBeat`, `IKeepsBeat`):
the driver puts the loop where the fight's beats say it should be (`MusicDriver.KeepBeat`). The first time is as the
theme begins, under its fade-in; after that only something that stops the fight and not the music (a pause, a
stalled frame) moves them apart by more than 50 ms, and then every stem is put back at once. **Hitstop does not stop
the chorus:** the two fights add back what a hit froze (`Hitstop.FrozenSeconds`), so their beats keep real time with
the music through every blow.

| Boss | Key, beat | Loop | Phase 1 | Phase 2 | Phase 3 |
|---|---|---|---|---|---|
| The Collapse (`collapse`) | G Mixolydian, 75: the beat the lamps are lit on | 4 bars: a bar is one round of the four lamps | frame drum on every beat, the round's first the strongest; the mine's hurdy-gurdy drone; the choir calling a name a beat for three rounds and the answer in the fourth | tuba walking under the surge; the anvil on two and four | the drone and the full chorus **leave**; the chorus comes back a voice fewer, the last beat of each round unsung (it puts the lamps out) |
| The Complete Survey (`survey-tide`, `survey-ash`, `survey-afternoon`) | one theme a phase (`Theme.ForPhase`): D Dorian at 67, G Mixolydian at 75, C major at 100, the three inks' keys and beats | 5 bars: two verses of eight beats and a break of two | the ink's own drum, frame drum or harpsichord on every beat, softer in the break; its drone, hurdy-gurdy or strings; the chorus calling a name on each beat of a verse and singing the answer in the break, where it breathes; the region's own lead under it, so the ink is heard | (the next ink's theme) | (the next ink's theme) |
| Reedmother's Brood (`brood`) | D Dorian, 67 | 8 bars | the tongue drum in quick pairs, their feet; the concertina's chant, three on the fifth every bar: "Ours. Ours. Ours." | the nest opens and the smoke comes: the drone, and the fiddle frantic over it | the chant **leaves**; it comes back asking, "...ours?", rising at its end; the psaltery's tremolo creeping up: the fire |

`Score.ThemeOfBoss(family, region, phase)` picks the Survey's ink by phase; a boss with one theme keeps it in every
phase. The resolution rings in the key of the theme playing when it falls: Halden's, for the Survey's last ink.

## 3e. The last four main bosses (AUD-14)

**Every main boss now has a theme of its own**; only the optionals (the Choir, Hale, the Fallen Star) fight to their
region's motif. Each of the four is in the key and at the beat of the region it is fought in, opens with its bed,
pulse and lead at the first telegraph, and adds or changes a layer a phase (`Score.ComposeMainBosses`).

| Boss | Key, beat | Loop | Phase 1 | Phase 2 | Phase 3 |
|---|---|---|---|---|---|
| The Gatekeeper (`gatekeeper`) | the Verdance's E Phrygian, 50 | 6 bars | the root-chapel's organ pedal, the roots in the ground; a Cantor handbell on one and three, the stone wings; the gamba, slow as stone, "Passage is for the winged", stopping on the flat second unresolved | the glass held high, a step higher every bar: it rises up the gate and the fight goes up the threads | the pedal and the standing voice **leave** (the roots tear free); the gamba tries to fly, a climb a bar coming down hard, each climb lower |
| Warden-Captain Oriel (`oriel`) | Halden's C major, 100 | 8 bars | the music box on every beat, the drill-yard's count; Halden's I IV V vi on the strings; the Guild's brass playing the Guild's motif **backwards** (`Score.MirroredMotif`), as she plays Wren's combo reversed | the motif the right way up on the strings, an octave above her mirror ("That's my stance"); the harpsichord's run up an octave and back every other bar, Wren's Flourish | the clockwork **leaves**; the cello holds the dominant and the violin its leading tone under everything: the Bind she is writing, the cadence never let home |
| The Half-Cathedral Bells (`bells`) | the Greyfold's held tone on D, 40 | 4 bars | the held tone; a bowed cymbal a bar; a second held tone rising a step a bar, "For the flock, going north" (the first bell's hum) | the second bell's hum, in canon a bar behind the first and a fourth under: "For the ones who stayed" | the held tone **leaves** (the white); the great bell's hum sings the roll-call's answer under everything, a bar a note: "We did not forget you" |
| Corra's Drawing (`corra`) | the Blank's D, 33 | 4 bars | the Remnant's voices, the crayon's colour; a celesta counting on every beat (on it, where the Blank's clock is off it); her song, a child's sing-song on the fifth and the third that does not come home | her father's motif, the Guild's, drawn bigger: twice its length, low on the reversed piano ("He was bigger"); the small one beside it at its own size, high on the celesta, the two starting together | the colour and her song **leave** (the crayon runs out); the song comes back as its outline, only the notes on the beat, at twice the pace |

**The Bells' music strikes no bell.** Every ring in the nave is the fight's own tell, and a bell in the score would be
a tell that the telegraph doesn't show (audio-direction 4); what the score has is the hum a bell leaves, on the
Greyfold's held sine, and the bowed cymbal. The Gatekeeper's theme keeps the Verdance's rule and never quotes the
roll-call; the Bells' great bell and every boss's resolution are the only places the answer comes in the Greyfold.

## 3c. The Blank and the endings (AUD-08)

**The Blank** (`blank`): *everything the player has heard, remembered wrong.* The Blank's D at 33 bpm, three bars
and two of rest (40%). Its lead is the reversed piano playing each region's lead as it opens, backwards, in the
Blank's own mode (so Windreach's pentatonic degrees land on Dorian steps: the tune remembered wrong); the Remnant's
voices hold under it and sing the roll-call the wrong way round above; a celesta ticks off the beat; in combat it
ticks every half-beat. The Blank's islands are runtime rooms (`Island_*`) and now read as the Blank's
(`Mix.RegionOf`), so they get this theme and the Blank's ambience; the epilogue's stand-ins (`Epilogue_<zone>`)
read as their zones' regions. `<<sing blank>>` sounds on the Remnant's island (`Island_Remnant`).

**The codas** (`Score.CodaOf(Ending)`): the endings runner, at the last fade to white, tells the music to begin
the chosen ending's coda (`MusicDriver.BeginCoda`); it hands over on the bar line like a region and plays over
everything, whatever room, until a new game.

| Ending | Key | What |
|---|---|---|
| The Fixed World (`coda-fixed`) | Halden's C, 100 | the clockwork with the bar finished at last: I IV V I, twice, the last chord held; nothing will ever fade |
| The Open World (`coda-open`) | the coast's D, 67 | the chorus sings the roll-call, twice, over the drone and the drum; the fiddle answers above; the answer comes home and holds |
| The Unwritten (`coda-unwritten`) | the Verdance's E, 50 | the gamba's one phrase, resolving to the tonic at last and holding, the glass over it |
| The Cartographer's Rest (`coda-rest`) | the Blank's D, 33 | the celesta plays the roll-call the right way round, the clock on the beat now, the Remnant under |

## 4. The pipeline

```
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.ScoreExport.Render -quit
```

(or **OWSBG → Render the Score**) writes `docs/audio/music/`: every stem as `<region>_music_<stem>_<bpm>.wav` and
`<region>_music_<boss>-<stem>_<bpm>.wav` and `<region>_music_shared-<stem>_<bpm>.wav` (48 kHz 24-bit mono, the
stems together peaking at −1 dBTP), an `all-stems` mix of each to listen to, each region's resolution, and each
theme as MIDI (the tempo, a track a stem with its instrument's program, the notes at their times) for a real
arrangement.

## 5. Reworking by hand

- **Recorded stems** replace the render per theme: the driver caches clips by theme and stem id (`_clips`), so a
  loader that fills that cache from files is the whole change. Stems must be exactly the theme's loop long.
- **A theme** is a `Theme` in `Score.Compose`: bars, rest bars, stems with notes as degrees. A new region's theme is
  one more, and `ThemeOf(region)` finds it; a boss's names its family.
- **An instrument** is a row: harmonics, envelope, vibrato, noise, detune, inharmonicity, a low-pass, a transpose.

## 6. Verification

- `ScoreTests` (EditMode, 11): both coast themes have the stems the direction asks for, at bars of four at the coast's
  beat, every note in D Dorian and before the rest, the instruments the coast's band, the silence shares 20% and 0;
  the lead waits two bars, quotes the answer in the mode and comes home; the stems are exactly the loop, the mix at
  the ceiling, the measured silence the designed one, the join quiet, a render the same every time, the fiddle's
  first note the fifth; the boss theme's three phases and its resolution starting on the fifth and ending on the
  tonic; the file names and MIDI header, and the deliverables rendered. AUD-06: the four other themes are in their
  modes at their beats from their bands with their silence shares (within a bar), each keeps its brief (the anvil
  on two and four, the gamba stopping on the flat second, Halden's seventeen bars and V never going to I, the
  flute's long notes), each renders to its loop at the ceiling with its silence measured, and its files are there.
  AUD-07: the four boss themes never rest, open with bed, pulse and lead, add a layer a phase and render to their
  loops; the Wardens open on the Guild's motif on brass; Halvard's theme is picked by the region he is fought in;
  Brann's roar and Voss's tone leave at phase 3; the Archivist's lead is the inverted roll-call and her drawing
  answers it upright; every region's shared theme is its own stems, fought in, by phase; the files are there.
  AUD-08: the Blank's theme rests two bars in five, opens on the coast's fiddle backwards, has the Remnant sing the
  roll-call reversed; each coda is in its ending's key, is never the region's theme, resolves its way and renders.
- `MusicDriverTests` (PlayMode, 7): the coast's stems play in step within 50 ms at their levels under the bus, the
  drive waiting; a blow brings the drive in over its fade and the fight's end takes it away; a boss fight's start is
  not the theme's start, its first telegraph is, phase 2 waits for the bar line and enters, phase 3 does not, and
  its death stops the stems and resolves; paused, the theme plays on at the snapshot's gain and cutoff; a walk from
  the coast into the highland schedules the highland's stems on the coast's bar line, the coast still going until
  then, and crosses over there; Brann's third phase takes the roar away on the bar line and brings the glow, and
  Hale, with no theme of her own, fights to Windreach's motif with its drive in and its flute waiting; an island
  plays the Blank, a coda takes over from it and outlasts any room until a new game. `EndingsRunnerTests`: the
  Fixed World's walk ends with its coda begun.
- AUD-13: `RhythmScoreTests` (EditMode, 4): the Collapse's theme is at the fight's beat with a round of lamps a bar,
  a pulse on every beat and nothing between, a name called on each beat with its lift ahead, the answer in the
  fourth round and a voice fewer in phase 3; the Survey's three themes are their inks' keys and beats, whole verses
  a loop, calling on the verse's beats and breathing in the break; the Brood's chant is one word three times a bar
  and asks, rising, in the last phase; the phase lookup falls back to a boss's own theme. `RhythmMusicTests`
  (PlayMode, 3): the Collapse's beat keeps real time through eight hitstops; its theme lands within a tenth of a
  second of the fight's beat and is put back on it after a pause; the Survey's theme changes ink with its phase
  and keeps the new beat.
- AUD-14: `MainBossScoreTests` (EditMode, 6): the four themes are in their regions' keys at their beats, never
  rest, open with bed, pulse and lead, add a layer a phase and change one in the last, render to their loops at the
  ceiling, and are found under their bosses' class names in every phase; every main boss on the sheets has a theme
  of its own and the three optionals none. The Gatekeeper's gamba stops on the flat second, its glass climbs, and
  its flight comes down hard and lower; Oriel's brass is the Guild's motif backwards, her strings it forwards, and
  the Bind holds the dominant and never the tonic; the Bells' theme plays no struck instrument, its second bell is
  the first a bar behind and a fourth under, and the great bell sings the answer; Corra's Drawing draws the motif
  at twice its length with the small one at its own size beside it, and its outline is her song's on-beat notes at
  twice the pace.

## 7. Open

- **It is a synth.** The fiddle is a sawtooth with vibrato and bow noise; the composer's recordings are the point of
  the delivery files, and the MIDI is the hand-off.
- **Loudness** is by peak (−1 dBTP across the stems), not the spec's −18 LUFS.
- **The handover is on the old theme's bar line**, not on a shared one: the beats differ by region, so the new
  theme starts a bar on the old bar's line and keeps its own beat from there.
- **The Blank remembers all five regions** whether or not the player has heard them; the direction's "everything
  the player has heard" would take the lead from the regions drawn, a theme variant per set.
- Windreach's "drum when the fire is lit" is its pulse, always soft: the camp's fire is no state yet.
- **The codas loop.** The title holds for eight seconds and the game stops there (M4's credits); a coda that ends
  rather than loops wants that roll to end with.
- **The rhythm bosses** are scored to their fights' beats since AUD-13 (section 3d). A resync after a pause is a jump
  in the loop, heard as the paused snapshot lets go.
- **The optionals** (the Choir, Hale, the Fallen Star) still fight to their region's motif; the Brood alone has its own.
- **The Bells' theme does not know which bell is cut**: the canon and the great bell come with the phases, not
  with the ropes, and an inscription read as a bell is silenced is not yet a line in the score.
- **Halvard's third fight** stops counting halfway; the theme does not know that yet.
