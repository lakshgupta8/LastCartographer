# External Test Round 1 (PRO-04, v1)

This is the M1 milestone's "External testers": the vertical slice, played cold by people who didn't make it.

The round has five parts:
- a tester brief;
- a session the game records;
- twelve questions after;
- a facilitator's sheet;
- a bar the round is held to.

The questions, the session and the bar are in `OWSBG.Core.Playtest`. The recorder is `PlaytestRun` with
`PlaytestRecorder` (Narrative). The gate runs from files (`tools/playtest-gate.ps1`, `PlaytestGateSetup`). The
feel-test (PRO-03, `docs/design/feel-test.md`) is the controller's gate; this round is the slice's.

The papers are in `docs/playtest/round1/`:

| File | For | What |
|---|---|---|
| `tester-brief.md` | the tester | what to do, the controls, F12 for a bug, what is recorded |
| `facilitator-sheet.md` | whoever runs the session | before, during (what to watch for, what to say), after |
| `answers-template.csv` | the facilitator | the answers sheet's header, the questions in its comments |
| `notes-template.md` | the facilitator | the open answers and the observations, one per tester |

## 1. Who, and how many

- At least **eight** testers, none of whom worked on the game, and not all of whom play this kind of game.
- Each plays alone, from the start, for as long as they like. About an hour is the most the round asks.
- A facilitator watches and writes. They do not help: "what would you try?" is the only answer to "how do I…".

## 2. The slice

The game as it is, from the start: the prologue at the Edge, then Saltmarrow. The round's finish line is the
Lamp-Keeper (`Playtest.FinishBoss`). When she falls, the game says the test is over, and the tester may stop or
keep playing. The recorder keeps going either way.

Two things change only with the round's switches:
- **`-playtest`** turns the recorder on. The game itself plays as it always does.
- **`-continue`** (`Bootstrap.ContinueArg`) starts from the last desk's save in the room she rested in, instead of
  a new game. It is for a tester whose game froze or closed. Nothing else loaded a save on launch before this; the
  desks only wrote one.

## 3. What the game records

`-playtest -tester <name> -playtestOut <path>` writes the session as JSON (`Playtest.Session`):
- every room entered, in the order first entered, with the seconds and the deaths in each;
- the boss fights started and won, by boss;
- the dialogue nodes read and the vantages surveyed;
- the F12 bug reports filed;
- the exceptions logged;
- how many times the options were changed;
- the **longest stretch with no new room**, and the room it was spent in. This is the measure of being lost;
- the build, the device and the session's length;
- whether the session was **closed**: the game wrote it on quitting. One never closed was a crash or a killed process.

The recorder writes the file every 30 seconds while it plays, so a crash leaves a session marked open. A second
launch for the same tester (after `-continue`) writes a new file, and the gate adds the two sittings together
(`Playtest.Merge`).

Nothing else is recorded: no keystrokes, no video, no audio. The tester brief says so, in those words.

## 4. The questions

One to five each, asked after the session, before anyone talks to the tester about it.

| Id | Question | 1 | 5 |
|---|---|---|---|
| start | The opening told me enough to begin. | I was lost | I knew what to do |
| next | I usually knew where I could go next. | rarely | always |
| atlas | The atlas helped me find my way. | never used it / no help | a great help |
| control | Wren went where I meant her to. | rarely | always |
| fair | When I was hit, I could see why. | never | always |
| boss | The Lamp-Keeper was hard in a good way. | unfair or dull | hard and fair |
| people | The people I met were worth talking to. | skipped them | wanted more |
| story | I wanted to know what happens next. | not at all | very much |
| read | I could read everything on screen comfortably. | struggled | easily |
| look | The world looks like a drawing come to life. | no | yes |
| sound | The sound and music suited it. | no | yes |
| again | I would play the next hour. | no | yes |

Then the open lines (`Playtest.OpenLines`), written in the tester's words:
- Where did you get stuck?
- What did you want to do that the game would not let you?
- What would you tell a friend this game is?
- Did anything look broken?

## 5. The bar

`Playtest.Gate` reads the answers and the sessions. The round passes when all of these hold:

| Measure | Bar |
|---|---|
| Testers | at least 8 |
| Every question's median | 3.5 or more; any median under 3 fails on its own |
| "I would play the next hour" | median 4 or more |
| Reached the Lamp-Keeper (a fight started) | 70% of testers |
| Beat her | 50% |
| Median longest time lost | under 5 minutes |
| Sessions never closed (crashes) | at most 1 |

A tester with answers and no session counts against the round: the recorder wasn't on, or the file was lost. A
question nobody could answer is left out.

The report (`logs/playtest/gate.md`) has five parts:
- the verdict, and why not;
- each question's median with every answer;
- each tester's row: time played, rooms, deaths, the Lamp-Keeper, the longest time lost and where, bug reports,
  exceptions, and whether the session closed;
- the ten rooms that took the most lives across the round.

The script's exit code is 0 when the bar is met, 1 when it isn't, and 2 when the files can't be read.

## 6. Running it

1. Build: `pwsh tools/build.ps1 -Development`.
2. Follow `facilitator-sheet.md`: the brief, the launch line, what to watch, the questions after.
3. Put the answers in `logs/playtest/answers.csv` and the sessions in `logs/playtest/`.
4. Run `pwsh tools/playtest-gate.ps1`.

If the round fails:
- the report's reasons point at the part of the game to look at first;
- the open lines and the facilitator's notes say why;
- fixes go to the rows they belong to, and round 2 is run with new testers.

## 7. Tests

`PlaytestTests` (edit mode):
- the questions have unique ids;
- the template's header is the gate's;
- the papers exist and the brief names every switch it relies on;
- the gate passes a good round and names each way to fail one;
- a session round-trips as JSON, and two sittings merge into one;
- the answers are read by header, and bad rows are refused;
- the report carries the verdict, the medians and each tester.

`PlaytestRecorderTests` (play mode):
- on the real game, rooms are recorded in order with their seconds, deaths are counted in the room, nodes read
  are counted, the longest stretch with no new room grows and resets, and the Lamp-Keeper's fall finishes the
  round once;
- the session is written while open and closed on request;
- `-continue` starts in the room of the last desk's save, with its flags.

## 8. Open

- **No round has been run.** The bar stands with nothing measured against it. It wants eight people and a week
  of afternoons.
- **The slice is the whole greybox.** A tester can wander past Saltmarrow before the Lamp-Keeper; the recorder
  notes it, and the report shows it in their room count. A build that walls the slice off is a choice for the
  team.
- **Consent.** The brief says what is recorded and that nicknames are fine. The round keeps no other personal
  data. If testers are recruited outside the team, a signed consent form is the team's call.
