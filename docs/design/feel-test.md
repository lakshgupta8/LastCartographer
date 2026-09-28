# Controller feel-test (PRO-03, v1)

This is the M0 gate's protocol: "Wren's controller feels right in a greybox" (`docs/02-production-plan.md` §2),
turned into a course a tester runs, twelve questions they answer after, the numbers the game records while they
run, and the bar both are held to. The course, the questions and the bar are in `OWSBG.Core.FeelTest`; the
course is drawn at runtime (`FeelCourseRooms`); the numbers are kept by `FeelRecorder`. The controller's own
numbers are the combat doc's (`docs/design/combat-and-movement.md` §1) and the tuning pass's (`docs/design/tuning.md`).

## Who, and how many

Five testers at least, none of whom wrote the controller. At least two on a pad and two on a keyboard. Each runs
the course alone, unprompted beyond the station cards, for as long as they like (fifteen minutes is typical),
then answers the questions before anyone talks to them about it. The person running the session watches and
writes down what they see the tester do that the tester doesn't say.

A tester who has Talonhold or Wingbeat runs the shaft and the dash; one who hasn't takes the ladder path round
and skips the dash, and the questions for those are left blank, not scored.

## The course

`OWSBG → Play the Feel Course` in the editor, or `LastCartographer.exe -feel` in a build (with `-tester <name>`
and `-feelOut <path>`), starts a new game and goes straight to the room `Feel_Course`. It is one long room of
seven stations, left to right, each marked by a post. A fall into a pit puts the tester back at the start of the
station and counts.

| Station | The tester is asked to | It tests |
|---|---|---|
| The run (0–30) | Run to the far post and back twice. Turn hard at each end. | acceleration, the four-frame stop, weight |
| The hops (30–70) | Cross the four gaps. Each is wider than the last; the fourth is near her reach. | reach, landing, the late and early jump |
| The steps (70–92) | Up the four steps with the smallest hops you can, then down the far side. | the tap's hop, the buffer on the rise |
| The ceiling (92–104) | Get under the low roof and out the far side without hitting your head. | variable height |
| The shaft (104–118) | Up the shaft by the walls; without Talonhold, the ladder path round. | the cling, the wall-jump, the lock |
| The dash (118–140) | The gap is too wide to jump. Jump, then dash; from standing and from a run. | Wingbeat's five units and its frames |
| The targets (140–160) | Three crabs. Strike the first two; pogo the third as many times as you can. | hitstop, the strike's reach, the pogo |

The gaps are held to the controller's numbers by test: the hops are 3, 4, 5 and 6 units, and a full jump at a
run carries about seven (`FeelTest.JumpReach`: up in 0.38 s, down faster by the 1.3 fall multiplier, three
frames of hang, at 9 units a second); the dash gap is 9, past a jump and within a jump and a dash; the roof is
2.6 up, over a tap's 2.0 hop and under a hold's 4.5; each step rises 1.5.

## The questions

One to five each, in the tester's own words at the ends of the scale.

| Id | Question | 1 | 5 |
|---|---|---|---|
| goes | She goes when I press. | late | at once |
| stops | She stops and turns where I mean. | slides | on the spot |
| weight | She has the right weight. | floaty or leaden | right |
| lands | Jumps land where I mean. | rarely | always |
| tap | A tap makes a short hop; a hold, a tall one. | no difference | clearly |
| late | A jump pressed just after the edge still jumps. | she falls | it jumps |
| early | A jump pressed just before landing still jumps. | it's lost | it jumps |
| wall | The wall holds and the wall-jump goes where I mean. | no | yes |
| dash | The dash goes as far and as fast as I expect. | no | yes |
| hits | A hit feels like it lands. | nothing | it lands |
| pogo | The pogo is readable and I can chain it. | no | yes |
| hour | I would play this controller for an hour. | no | yes |

Then one open line: "What did she do that you didn't mean?"

## What the game records

Every press on the real reader is a named buffer (`ButtonBuffer`, PRG-05): it says when it is pressed, when it
is acted on and how many frames it waited, and when it runs out with nothing done. The recorder keeps, per
session (`FeelTest.Session`, JSON):
- presses, acted on and dropped, per action;
- jumps by the frames they waited in the buffer (0 is the same frame; 1 to 6 is the buffer doing its work);
- coyote jumps (off the ground, no wall, within the five frames), wall jumps, dashes, pogos, landings, hits;
- seconds in each station, and falls in each;
- the build, the device, the tester's name, the session's length.

A **dropped press** is the number to watch. From the player's side it is a press that did nothing: a jump in
the air with no ground coming, a dash with none left. Some are the player's; too many are the buffers'.

## The bar

`FeelTest.Gate` reads the answers and the sessions:
- at least **five** testers;
- every question's median at **4 or more**; a median under **3** on any question fails on its own;
- across every tester's presses, at most **10%** dropped.
A question nobody could answer is left out. The report lists the medians and every reason it failed.

If the gate fails, the numbers go back to the combat doc and the tuning pass, one at a time, and the course is
run again with the same testers where possible. The controller's frame data is held by `WrenControllerTests`,
so a change that breaks the doc's numbers is caught before a tester sees it.

## Running a session

1. Build (`pwsh tools/build.ps1 -Development`) or use the editor menu.
2. `LastCartographer.exe -feel -tester pat -feelOut logs/feel/pat.json`, or the menu; the session is written
   when the game quits.
3. Give the tester the station cards (the table above), a pad or a keyboard, and the room.
4. After, the questions, then the open line.
5. Put the answers beside the session (a row per tester: the ids above, one to five) and run the gate.

The gate runs from files: `pwsh tools/feel-gate.ps1` reads `logs/feel/answers.csv` (a row a tester; the
header is `tester,goes,stops,weight,lands,tap,late,early,wall,dash,hits,pogo,hour`; blank where they couldn't
answer) and every session JSON in `logs/feel`, joins them by the tester's name, and writes `logs/feel/gate.md`:
the verdict, why not, each question's median with its answers, and each tester's numbers. It exits 0 when the
gate is met, 1 when not, 2 when the files can't be read (`FeelGate`, `FeelGateSetup`).

## Tests

`FeelTestTests` (EditMode, 5):
- the course is a line of stations, each asking questions that exist, every question asked somewhere;
- every gap is held to the controller's reach, the ledges never overlap, the ladder path climbs in single jumps;
- the gate wants five testers at four and none under three, leaves an unanswered question out, and counts the
  dropped share over everyone;
- a session round-trips as JSON;
- a named buffer says when it is pressed, acted on or dropped, once each, and a nameless one says nothing.

`FeelGateTests` (EditMode, 3):
- answers are read by their header, in any column order, a blank left out, and bad rows refused with the reason;
- the report says why, shows every question's median and answers, and every tester's numbers;
- the whole run reads a folder, joins sessions to testers by the name inside or the file's, and writes the report.

`FeelRecorderTests` (PlayMode, 3):
- on the real controller, presses are counted as acted on the same frame, buffered, coyote or dropped;
- a fall into a pit is put back at the station and counted against it;
- the course builds from its numbers with its ledges, its spawns and its three crabs.

## Open

- **No session has been run.** The gate is a bar with nothing measured against it yet. M0's exit waits on five
  people and an afternoon.
- **Frames from press to picture.** The recorder measures press to action in fixed frames; the combat doc's
  "input to animation under 3 frames" also counts the render. A camera on the screen, or the built player's
  frame timing (PRG-24's probe), can close that.
- **The pad's dead zone and the keyboard's rollover** are not on the course. If "she goes when I press" scores
  low on one device and not the other, that's where to look first.
- **The targets are crabs.** A block of rubble for the pogo (the Collapse's) would give a fixed, repeatable
  bounce; the crabs move.
