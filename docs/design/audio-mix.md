# The mix (AUD-09, v1)

This covers the buses the game's sound runs through, the snapshot for each state the game can be in, the ducks for
the moments inside a state, the low-pass a place's fade stage puts on its ambience, and the player's volumes. It is
built before the music, ambience and effects rows land (AUD-03 to AUD-08), so those rows hand the mix their clips and
find the room already shaped. The numbers are in `OWSBG.Core.Mix`; `Mixer` runs them; `MixDriver` (Narrative) puts
them on real sources in the game.

There is no `AudioMixer` asset. The mixer is code: a gain and a cutoff per bus, computed each frame from the state,
so the tests can run it in edit mode and a build can't drift from the doc. `Mix.ToDb` is there for the day a mixer
asset takes over the DSP.

## Buses

| Bus | Carries | Player's volume | Filtered |
|---|---|---|---|
| Master | everything | Volume | |
| Music | the region themes, the boss themes, the roll-call | Music | yes |
| Ambience | the room's bed: wind, water, the camp at night | Sounds | yes, by the snapshot and the fade stage |
| Sfx | Wren's strikes and steps, the enemies, the world | Sounds | |
| Dialogue | the voices, and the roll-call's names | Voices | |
| Ui | the pages: a turn, a row, a toast | Sounds | |

The options page has four volumes in tenths: Volume, Music, Sounds, Voices (`Options.Volume`). Ambience and the
UI follow Sounds with the effects; nobody wants to set six sliders.

## Snapshots

The driver picks the first that holds, most pressing first, and the mixer moves there over the snapshot's In
seconds (or the old one's Out, on the way back to Explore), eased.

| Snapshot | When | Music | Ambience | Sfx | Dialogue | Ui | Low-pass (music / ambience) | In / Out s |
|---|---|---|---|---|---|---|---|---|
| Paused | the options page is open | 0.4 | 0.3 | 0 | 0 | 1 | 1200 / 1200 Hz | 0.15 / 0.25 |
| Boss | any boss's fight is on (`Boss.FightsActive`) | 1 | 0.3 | 1 | 1 | 1 | open | 1.0 / 2.5 |
| Dialogue | a conversation is running | 0.55 | 0.65 | 0.8 | 1 | 1 | open | 0.3 / 0.8 |
| Blank | the room is in the Blank | 0.7 | 0.4 | 0.9 | 1 | 1 | 2500 / 1800 Hz | 2.0 / 2.0 |
| Combat | a blow landed or was taken in the last six seconds | 1 | 0.7 | 1 | 1 | 1 | open | 0.4 / 2.0 |
| Explore | otherwise: the room as designed | 1 | 1 | 1 | 1 | 1 | open | 0.6 / 0.6 |

Why these:
- **Paused** is the world through a wall: the theme keeps going, dull and low, so the page never feels like a
  crash, and nothing in the room plays. The page's own sounds stay.
- **Boss** gives the theme the room. The way out is slow, because the aftermath (a boss's last lines, the answers)
  wants the theme to fall away rather than snap.
- **Dialogue** keeps the room present under the voice. The duck below does the rest per line.
- **Blank** is the bible's reversed motifs under a low-pass, with the room mostly gone. The Greyfold's end, where
  the road stops, is not the Blank yet: the clarity meter (DES-13) can ask for this snapshot when that room exists.
- **Combat** steps the ambience back so the reads are heard; the theme's combat layer is the music row's business.

## Ducks

A duck is a moment: it presses over its attack, holds, and lets go over its release, and while it presses the
other buses drop toward its floor. Two ducks at once: the deeper wins on each bus. A duck never touches its own
bus.

| Duck | On | Music floor | Ambience floor | Attack / Hold / Release s | Why |
|---|---|---|---|---|---|
| Line | a line is shown (`ViewDialoguePresenter`) | 0.6 | 0.7 | 0.08 / 0 / 0.6 | the room makes room for the voice |
| Impact | a hit lands on an enemy (`Enemy.TakeHit`) | 0.85 | 0.5 | 0.01 / 0.12 / 0.25 | the impact is clean (the combat doc's clarity rule) |
| Hurt | Wren takes a mask (`WrenVitals.Damage`) | 0.5 | 0.6 | 0.02 / 0.3 / 0.8 | the music dips for a breath |
| Bind | a Bind takes (`WrenVitals.Bound`) | 0.6 | 0.3 | 0.05 / 0.5 / 1.2 | the room holds its breath while the ink takes |

Impact and Hurt also count as combat for the snapshot. The hooks are one line each, `Mix.Note(duck)`, and do
nothing when no mix is running (a bare test scene).

## The fade stage

AUD-05 asks for ambience filtered by the place's fade stage. The mixer does it: the ambience sits under the duller
of the snapshot's cutoff and the stage's.

| Stage | Cutoff |
|---|---|
| 0 | open |
| 1 | 9000 Hz |
| 2 | 3500 Hz |
| 3 | 1200 Hz |

The driver reads the stage of the place the current room draws (`FadeStages.Get`, the room's name without its
greybox prefix). The music is never the place's: a region theme plays the same in a fading room, which is the point.

## The gain a source plays at

For every bus but the master: **the player's master × the bus's volume × the snapshot's gain (mid-move included)
× the ducks' gain.** It is never above what was designed. The master alone is a number the driver reports; there
is no master source.

## In the game

`MixDriver` wakes with the first scene, like the smoke test and the bug reporter, and lives across rooms. It makes
one 2D `AudioSource` per bus (none for the master), with an `AudioLowPassFilter` on Music and Ambience, and each
frame it decides the snapshot, reads the stage, ticks the mixer with unscaled time (the paused snapshot has to move
while time stands still) and writes each source's volume and cutoff. The sources ignore the listener's pause for
the same reason.

The music and ambience rows use it: `Loop(bus, clip)` for a theme or a bed, `Play(bus, clip)` for a one-shot.
Nothing plays yet; there are no clips.

## Tests

`MixTests` (EditMode, 8):
- every snapshot's shape, and the relations above (combat below explore, a boss below combat, the Blank filtered,
  paused silent in the room);
- a move takes its time, is monotone, settles, starts from where it is when the mind changes;
- a duck's envelope, its own bus untouched, the deeper of two winning;
- combat outlasts the last blow and restarts with each;
- the stage's cutoffs fall, and the ambience takes the duller of stage and snapshot;
- the player's volumes multiply in, never above design;
- decibels both ways, and a room's region from its plan or its name;
- the volumes as options in tenths, kept.

`MixDriverTests` (PlayMode, 4):
- it wakes with the game with a source per bus, 2D, filtered where it should be, safe with no clip;
- the snapshot follows the state: a blow is combat, the Blank over combat, paused over everything, and back;
- the fade stage dulls the ambience's filter and not the music's;
- the player's volumes reach the sources.

## Open

- **Clips.** Everything above shapes silence until AUD-03 to AUD-08 land. The first real test of the numbers is
  the Saltmarrow theme under the Lamp-Keeper's fight (AUD-04).
- **A mixer asset.** If the DSP wants more than a low-pass (reverb in the Half-Cathedral, the Blank's reversal),
  an `AudioMixer` asset with these buses can take the gains as decibels from `Mix.ToDb`; the snapshots and ducks
  stay in code.
- **The Greyfold's end and the clarity meter.** The Blank snapshot is by region. The road that stops (DES-13's
  white paper) can ask for it by clarity when that room is built.
- **The desk and the atlas.** Neither page stops the world, so they take no snapshot. Whether the desk wants the
  room dulled the way pausing does is a feel question for the first external round.
- **Per-room ambience layers** (the camp at night, the Choir's room) are AUD-05's, on the Ambience bus.
