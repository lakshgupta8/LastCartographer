# The Score: Saltmarrow and the Lamp-Keeper (AUD-04, v1)

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
  in combat; a boss theme's stems enter by phase, a phase change waiting for the next bar line.
- **Defeat** stops the stems and plays the resolution: the roll-call's answer on the fiddle over the drone, in the
  boss's region's key; nothing else plays until it has rung. A reset returns the room's theme.

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

## 4. The pipeline

```
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.ScoreExport.Render -quit
```

(or **OWSBG → Render the Score**) writes `docs/audio/music/`: every stem as `saltmarrow_music_<stem>_67.wav` and
`saltmarrow_music_lampkeeper-<stem>_67.wav` (48 kHz 24-bit mono, the stems together peaking at −1 dBTP), an
`all-stems` mix of each to listen to, the resolution, and each theme as MIDI (the tempo, a track a stem with its
instrument's program, the notes at their times) for a real arrangement.

## 5. Reworking by hand

- **Recorded stems** replace the render per theme: the driver caches clips by theme and stem id (`_clips`), so a
  loader that fills that cache from files is the whole change. Stems must be exactly the theme's loop long.
- **A theme** is a `Theme` in `Score.Compose`: bars, rest bars, stems with notes as degrees. A new region's theme is
  one more, and `ThemeOf(region)` finds it; a boss's names its family.
- **An instrument** is a row: harmonics, envelope, vibrato, noise, detune, inharmonicity, a low-pass, a transpose.

## 6. Verification

- `ScoreTests` (EditMode, 5): both themes have the stems the direction asks for, at bars of four at the coast's
  beat, every note in D Dorian and before the rest, the instruments the coast's band, the silence shares 20% and 0;
  the lead waits two bars, quotes the answer in the mode and comes home; the stems are exactly the loop, the mix at
  the ceiling, the measured silence the designed one, the join quiet, a render the same every time, the fiddle's
  first note the fifth; the boss theme's three phases and its resolution starting on the fifth and ending on the
  tonic; the file names and MIDI header, and the deliverables rendered.
- `MusicDriverTests` (PlayMode, 4): the coast's stems play in step within 50 ms at their levels under the bus, the
  drive waiting; a blow brings the drive in over its fade and the fight's end takes it away; a boss fight's start is
  not the theme's start, its first telegraph is, phase 2 waits for the bar line and enters, phase 3 does not, and
  its death stops the stems and resolves; paused, the theme plays on at the snapshot's gain and cutoff.

## 7. Open

- **It is a synth.** The fiddle is a sawtooth with vibrato and bow noise; the composer's recordings are the point of
  the delivery files, and the MIDI is the hand-off.
- **Loudness** is by peak (−1 dBTP across the stems), not the spec's −18 LUFS.
- **Crossfades.** A theme change stops one set of stems and starts the next; a bar-synced handover between two
  themes (region to region) is for AUD-06, when there are two regions.
- **Phase changes** add a layer; the direction also allows changing one. Nothing changes a layer yet.
- **The remaining themes**: Emberdown, the Verdance, Halden, Windreach (AUD-06), the bosses (AUD-07), the Blank and
  the endings (AUD-08) are themes and instruments in the same table.
