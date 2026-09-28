# Accessibility and comfort (DES-14, v1)

The combat doc promises "no difficulty modes at launch; accessibility options instead". This is the first set:
remapping, holds that can be toggles, hitstop and shake at the player's strength, high-contrast ink, and no text
that leaves before it's read. It also gives the game an options page, where the language picker from PRG-19 now
lives.

## The options page

Esc on a keyboard, or Start on a pad, opens it (`OptionsView`, UI), unless another page, a conversation or a
cutscene has the screen. The Esc that closes a page never opens the options in the same breath.
- **It stops the world.** `Pause` (Core) holds time at zero until the page closes; a hitstop that ends meanwhile
  leaves time stopped. Wren is frozen as she is on the desk page.
- **Keys:** up/down picks a row; left/right changes it; J / Space / Enter / South switches or opens it; Esc / East
  goes back a page, then away.

| Row | Choices | Default |
|---|---|---|
| Language | English, and every locale with a table (the pseudo-locale in development builds only) | the system's, else English |
| Hitstop | off, 25%, 50%, 75%, 100% | 100% |
| Screen shake | off, 25%, 50%, 75%, 100% | 100% |
| Captions stay | as written, twice as long, three times as long, until dismissed | as written |
| High-contrast ink | off, on | off |
| Bind, Survey, Glide | hold, or press to start and press to stop | hold |
| Volume, Music, Sounds, Voices | off to 100% in tenths (`docs/design/audio-mix.md`) | 100% |
| Controls | a page: every action's key and pad button | the asset's |
| Resume | | |

Everything is saved the moment it changes, in `PlayerPrefs` (`owsbg.opt.*`, `owsbg.bindings`). Like the language,
these belong to the player rather than to a save slot. `Options` (Core) holds them and raises `Options.Changed`.

## Remapping

`Controls` (Core) lays the player's remaps over the WrenInput asset whenever an `InputReader` binds it. The remaps
are stored as the Input System's own override JSON.
- **Which actions.** Every press Wren makes, on each device: Jump, Strike, Dash, Bind, Survey, Flourish, Instrument,
  Next Instrument, Inkthread. Move stays on WASD, the arrows and the sticks.
- **A key in use swaps.** Put Jump on J and Strike takes Space. No action is ever left without a key, and no two
  share one.
- **Keys no action can take:** the menu keys (Esc, M, Start, Select) and every movement key, a stick's directions
  included.
- **A pad is a pad.** A button pressed on an Xbox or DualShock pad is stored as `<Gamepad>/…`, so it works on any.
- **The controls page.** Left/right picks the keyboard or pad column. Confirm listens for the next press, with the
  action map off so the press isn't also a jump. Esc keeps the old key. The last row puts every key back.
- **Prompts name the key the player has.** The tutorials say "Hold E to bind", or "Press R" once Bind is on R and
  toggled. They name the device the player last pressed something on.

## Holds as toggles

Bind, Survey and Glide are the three presses held to act. Each can instead be pressed to start and pressed to stop.
- **Bind and Survey** latch in the `InputReader`. The latch lets go on the next press, or when what it was doing
  ends or can't go on: masks full, ink too low, a survey drawn, Wren in the air. That's `Controls.Release`, called
  by `WrenVitals` and `VantagePoint`. So a latch never fires later by surprise.
- **Glide** lives in the controller, which knows where Wren is. A jump press in the air while falling, where it
  can't jump, switches the glide. Landing lets go.
  - A toggle pressed less than six frames before landing is treated as the jump it was meant to be: she jumps as
    she lands, as the jump buffer would have made her.

## Hitstop and shake

- **Hitstop.** `Hitstop.Request(frames)` keeps the player's share of the combat doc's frames, rounded, and at least
  one while any is kept. At 0 there is none, and time never stops.
- **Shake.** New: `Shake.Request(amplitude, seconds)` (Core), the combat doc's "hard shake only on boss slams".
  - **Where it's used:** only the Fallen Star's slam, at 0.35 units for 0.35 s.
  - **How it moves the camera:** it nudges the main camera after Cinemachine has placed it. The nudge is
    Perlin-noise movement that dies away, on unscaled time, and the stronger of two shakes wins.
  - **Its guarantees:** at 0 the camera never moves, and when a shake ends the camera is exactly where Cinemachine
    put it.

## High-contrast ink

- **UI.** `InkTheme` has two palettes: the warm one and high contrast.
  - **High contrast:** opaque white paper, black ink, firm rules, a darker wash, dim text and ochre.
  - **Legibility, tested:** every text colour reads at 7:1 or better on the paper and on a selected row (WCAG
    AAA). Ochre reads at 4.5:1 or better.
  - **Switching live:** `UiRoot` redraws everything already on screen in the other palette. Each swatch maps to
    its partner; a colour that isn't a swatch, such as a boss's bar, is left alone. Anything drawn later uses the
    chosen palette.
- **World.** The paper pass (`PaperGrain.shader`) reads `_OWSBG_Contrast`:
  - lights and darks pulled apart;
  - every edge drawn in near-black;
  - the lantern-radius outline drawn whole instead of fading toward the centre;
  - grain cut to a sixth, and no fibre;
  - night darkening only half as much.

## No timed text

- **Dialogue never moves on by itself.** A line waits for the advance button however long the player takes. There
  are no timed options.
- **Captions** are the only timed text: world captions, tutorial prompts, the journal's toasts, a boss's phase line,
  the walk's "Held."
  - They stay as long as written, twice, three times, or until dismissed with the advance button.
  - A boss's phase line is cleared if Wren falls, so a caption waiting to be dismissed doesn't outlast the fight.

## Tests

`OptionsTests` (EditMode, 5):
- the defaults, and choices kept across a reload;
- a change announced once;
- hitstop frames and caption times for every choice;
- the 7:1 palette;
- a redraw that moves only swatches.

`ControlsTests` (EditMode, 6), on a copy of the shipped asset:
- every action has a key and a button;
- a key in use swaps places, on the keyboard and on the pad separately, and no key is shared afterwards;
- menu and movement keys refused;
- pad layouts made generic;
- remaps kept, and a reset;
- prompts name the player's key, held or pressed.

`AccessibilityPlayTests` (PlayMode, 6):
- hitstop at 100%, 50% and 0 (none);
- a pause outlasts a hitstop;
- shake at 100%, 25% and 0 (still), leaving the camera exactly where it was;
- a toggled glide floats with nothing held, switches off, lets go on landing, and turns a late toggle into a jump;
- Bind lets go when it can't go on;
- high-contrast ink on a real render: a near-black edge, lights and darks further apart, less grain.

`InputAccessibilityTests` (PlayMode, 5), with keyboard events from the Input System's test fixture:
- toggled holds latch until pressed again or let go;
- a remapped key works, the swapped key moved, and the remap is kept;
- listening takes the next key, passes over a menu key, and Esc keeps the old one;
- in the game, Esc opens the page, which:
  - stops the world;
  - sets hitstop to 50%;
  - turns on high contrast, redrawing the page and telling the world;
  - speaks the pseudo-locale at once;
  - opens the controls page and backs out;
  - gives the world back without reopening, and doesn't open over a conversation;
- a caption waits to be dismissed, and a line is unchanged 300 frames on until a press moves it.

`LateBossKitFightTests`: the Fallen Star's slam is the shake.

`com.unity.inputsystem` is listed under `testables` in the manifest so the play tests can use its fixture. Its own
four integration tests now run with the play suite, and Unity skips two of them as unstable. To run only ours, add
`-assemblyNames OWSBG.Tests.PlayMode`.

## Open

- **Menu keys aren't remappable.** Pages read Esc, M, arrows, WASD, J, Space and Enter, and the pad's d-pad, South,
  East, Start and Select, directly. A player who moves Strike off J still confirms with Space or Enter.
- **No mouse on the options page.** Keyboard and pad only.
- **Still to do:** thread aim and a reticle, which come with the controls art (gauntlets doc). Text size and a
  dyslexia-friendly face wait for the ink font (UI art). Subtitles for barks and sound captions wait for audio.
  The bounds-walk is timed by design; a wider beat window is a candidate for CMB-19.
- **Not yet tested:** the options page on a pad, and remapping with a real device, both beyond the test
  fixture's keyboard.
