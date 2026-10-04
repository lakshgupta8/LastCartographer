# Wren's Sounds and the Tells (AUD-03, v1)

Her quill is ink and paper, never metal (audio-direction 4): a strike is a pen stroke, a pogo a nib's tap, a Bind
a word being written, a dash a page turned fast; hurt is a smudge, not a cry; her voice is never heard in combat.
Version one makes every one of them from a few lines of noise, filters and envelopes, rendered on demand, and
gives every telegraph in the game its tell.

## 1. The cues

`InkSounds` (Core) is the table and the generators; `InkSoundBank` (World) wakes with the game, renders a cue into a
clip the first time it is played, and plays it at the **Sfx bus's gain** (no duck touches that bus, so the tells
are never lowered; paused, it is silent). `WrenSounds` sits on Wren and plays hers from the events she already
raises; the bank itself answers the world's.

| Cue | On | What it is | ms |
|---|---|---|---|
| `stroke` | `QuillStrike.Swung` | a pen stroke: a short scratch sweeping down as the nib runs | 100 |
| `hit` | `QuillStrike.Landed` (the hit layer, over the stroke) | a denser scratch with the paper's thump under it | 90 |
| `kill` | `Enemy.AnyDied` (the kill layer) | a long scrape, and an ink drop falling and soaking | 420 |
| `pogo` | `WrenController.Pogoed` | the nib's tap | 50 |
| `dash` | `Dashed` | a page turned fast: two humps of air | 150 |
| `thread` | `Threaded` | a thin line drawn out fast, rising, and taking at its end | 180 |
| `refused` | `ThreadRefused`, `Flourishes.Refused` | a dry dot: no ink for it | 35 |
| `bind` | `WrenVitals.Bound` | a word being written: six scratches and a full stop | 600 |
| `survey` | while `VantagePoint.Surveying` (looped) | steady hatching, eight ticks a half-second | 500 |
| `drawn` | `VantagePoint.AnySurveyed` | one long stroke and a tap | 270 |
| `hurt` | `WrenVitals.Hurt` | a smudge: low, dull | 240 |
| `died` | `WrenVitals.Died` | the smudge drawn out until there is only paper, and the pen set down | 700 |
| `jump` / `land` | `Jumped` / `Landed` | the pen lifting; the pen set down (quiet: 0.35 and 0.45) | 45 / 60 |
| `crosshatch` / `longstroke` / `blot` | `Flourishes.Performed` | six quick ticks; one long stroke run dry; a wet drop spreading | 200 / 320 / 320 |

**The tells** (audio-direction 4). `Enemy.Telegraphed` (new) fires on a telegraph's first frame with the attack's
kind: `Boss.Telegraph` raises it from the boss's `TelegraphKind` (each boss with slams, windows or shapes in its kit
says which attack is which; the rest are strikes), and the Warden raises it as it draws the lance back. The bank
plays the kind's tell, each rendered to end within its frames, all before the fastest read (tier IV's 8):

| Tell | Frames | What |
|---|---|---|
| `tell_strike` | 6 | a short, high scratch of the pen (everything above 2.5 kHz) |
| `tell_slam` | 8 | a low drawn breath, swelling and stopped: the only low tell |
| `tell_window` | 4 | a chime, C7 with a fifth above it, as the opening starts |
| `tell_shape` | 8 | a swell rising to its end and cut |

## 2. How they are made

Four tools: a **scratch** (white noise through a resonant band-pass whose centre sweeps, under an attack and a
decay), a **drop** (a sine sliding down, or up for the thread), a **smear** (noise through a one-pole low-pass, for
smudges and breaths) and an envelope that always reaches nothing at its end (no clicks). Every cue is seeded, so it
renders the same every time, and every render is brought to −1 dBTP; a cue's `Gain` sets how loud it plays against
that (a jump is a third of a strike).

## 3. The pipeline

```
Unity.exe -batchmode -nographics -projectPath LastCartographer -executeMethod OWSBG.Setup.InkSoundsExport.Render -quit
```

(or **OWSBG → Render Wren's Sounds**) writes `docs/audio/sfx/wren_sfx_<cue>.wav` and `tell_sfx_<kind>.wav`, 48 kHz
24-bit mono, for the sound designer to hear and replace. A replacement is a clip by cue id in the bank's cache;
v1 has the hook (`InkSoundBank.Clip`) and no loader yet: the first recorded set decides whether they live in
`Audio/SFX/Resources/` or on the bank.

## 4. Verification

- `InkSoundsTests` (EditMode, 5): every cue renders under the peak, the same every time, ending at nothing; a tell
  for every attack kind by the direction's name, within its frames and before the fastest read, the slam the only
  low one, the strike's high, the window's chime a C; her quill is paper (a tap shorter than a stroke, nothing low
  in a stroke, hurt low and dull, the hit layer's thump, six scratches in a Bind, steps quieter than strikes); the
  survey loops without a click; the deliverables are rendered.
- `WrenSoundsTests` (PlayMode, 5): a swing is one stroke, a landed strike adds the hit layer, a death the kill
  layer and nothing else; jumps, landings and dashes; the survey hatches while held, ends drawn, stops when the pen
  lifts; hurt, then a Bind, then the last mask; Halvard's survey telegraph is a shape's swell and his thrust a
  strike's scratch, the volume is the Sfx bus's and never under the tell floor in a fight, and paused it is nothing.

## 5. Open

- **They are sketches.** A band-passed noise is a pen only by suggestion; the sound designer's recordings of a real
  nib on real paper are the point of the delivery files.
- **Plain enemies' tells** are in since AUD-10 (`docs/design/enemy-sounds.md`): the crab's hop, the skimmer's rise and a
  smudge's lunge tell as strikes, and the Cantors', the Choir's, the Bells' rings and the Survey's named ground as windows.
- **Captions for tells** ("[a low breath]") are the direction's ask for deaf players; the captions system can take
  them, but a caption per attack in a fight wants a setting first (DES-14).
- **Voice limiting** is enforced since AUD-10: 24 sources, each cue ranked in the direction's order
  (`AudioDirection.Voice`), the newcomer taking the least voice's place or being dropped (`InkSoundBank.Dropped`).
- **Hit-stop and shake** do not yet read the cue, and the hit layer does not scale with the strike's strength.
