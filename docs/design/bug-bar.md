# Bug bar and triage (PRO-07, v1)

This covers what a bug's severity means in this game, how a report gets one, how soon each is fixed, how many of
each a milestone may carry, and what the game itself puts in a report. The words are in `OWSBG.Core.BugBar`, the
form is `.github/ISSUE_TEMPLATE/bug_report.yml`, the count is `tools/triage.ps1`, and `BugBarTests` holds the three
to each other.

A bug is sized by what it does to a player, never by how hard it is to fix.

## Severity

| Severity | Means | Fixed by |
|---|---|---|
| **Blocker** | The game crashes, a save is lost or wrong, or no ending can be reached. Nobody can play past it. | Today, before any other work. The build is pulled. |
| **Critical** | Progress stops for some players, or an ending goes wrong: a soft-lock, a gate that stays shut, a boss that can't be hurt. | This week. |
| **Major** | Wrong but passable: a flag, a fate, a commission or a purse wrong; a room over budget; an attack with no read. | Before the milestone ends. |
| **Minor** | Cosmetic or feel: a clip, a typo, a missing sound, a hit that lands soft. | Before release, or on the known-issues list. |
| **Trivial** | Nobody would notice without being told. | Backlog. Closed at release candidate if nobody has claimed it. |

### The floor

A report says what the player saw (its **symptom**), and the severity starts from that. Triage may raise it, never
lower it.

| Symptom | At least |
|---|---|
| Crash, SaveLost, CantFinish | Blocker |
| SoftLock, WrongEnding | Critical |
| WrongState, OverBudget, NoRead | Major |
| Visual, Text, Audio, Feel | Minor |
| Unsorted (an F12 with no word yet) | nothing assumed; triage sorts it |

Two of these deserve a word. A **lost save** is Blocker, not Critical, because the game is built on the desk and
the return to it: a player who loses twelve hours does not come back. **NoRead** is Major on its own, since
CMB-19's floors are what make a hit fair; an attack under its tier's floor is a broken promise, not a feel note.

### Reach, and the order of work

Severity says how bad; reach says how many. **Everyone** means every player meets it, and the first hour (the
Edge, the Quay) is always Everyone. **Most** is the critical path after that; **Some** is one road, one commission,
one Charter; **Few** is a sequence break or a corner.

| | Everyone | Most | Some | Few |
|---|---|---|---|---|
| Blocker | P0 | P0 | P0 | P0 |
| Critical | P0 | P1 | P1 | P1 |
| Major | P1 | P1 | P2 | P2 |
| Minor | P2 | P3 | P3 | P3 |
| Trivial | P3 | P3 | P3 | P3 |

Priority orders the board; severity holds the milestone. A P2 Major still has to be fixed before the milestone ends.

## The bar

How many bugs a milestone may still have open, by severity. "Open" excludes `known-issue` and `wontfix`.

| Milestone | Blocker | Critical | Major | Minor | Trivial |
|---|---|---|---|---|---|
| Beta (M4 entry) | 0 | 3 | any | any | any |
| Release candidate (M5 entry) | 0 | 0 | 10 | any | any |
| Release | 0 | 0 | 0 | 25, each on the known-issues list | any |

`tools/triage.ps1 -Milestone beta|rc|release` counts the open issues and exits 1 when the bar isn't met. It reads
the tracker through `gh`, which isn't installed on this machine, so it has not been run yet.

## Triage

**Cadence.** Weekly until beta, at the same sitting as the weekly full playthrough (PRO-05); daily from release
candidate. A Blocker doesn't wait for the sitting.

**A report is sorted when it has a severity and an area.** The area is the plan's section that owns it (NAR, DES,
CMB, PRG, CHR, ENV, AUD, PRO), so every bug has a table it belongs to and a person who reads that table.

**The states**, as labels:

| Label | Means |
|---|---|
| `triage` | New: no severity or area yet. The form puts it here. |
| `needs-repro` | Nobody has made it happen again. Two weeks without a repro and it closes, with a note. |
| `fixed-needs-verify` | Fixed on main, with its test; not yet seen fixed in a build. |
| `verified` | Seen fixed in a build. Closed. |
| `known-issue` | Ships as it is; on the known-issues list. Only Minor and Trivial may. |
| `wontfix` | Won't be fixed, and why is in the thread. |

**Every fix has a test**, in the suite that would have caught it: EditMode for data and rules, PlayMode for anything
that moves. A fix without a test isn't fixed, it's hidden. Where a bug was found by a test the tests didn't have,
the test is the fix's first commit.

**A Blocker pulls the build.** The Steam branch it went to is rolled back to the last good build before the fix
starts (`tools/steam-upload.ps1` takes the branch).

`tools/triage.ps1 -Labels` makes or refreshes the labels: five severities coloured red through blue, eight areas,
six states.

## What the game puts in a report

**F12** writes a folder under the game's data folder (`%USERPROFILE%\AppData\LocalLow\<company>\LastCartographer\BugReports\`),
named by the time (`20260929-143012-pressed`), and the HUD says where. The options page's footer says F12 beside the
version. In it:

| File | What |
|---|---|
| `report.md` | The build and commit, when, how long since start, the symptom and its floor, the room and Wren's position, how many flags are set, the machine, the error counts, the first exception with its stack, empty Steps / Expected / Actual for the player to fill, and the last forty log lines. It's the issue's body, ready to paste. |
| `save.json` | The world as it is, in the save's own format. Load it and you are where the player was. |
| `log.txt` | The last two hundred log lines (`LogTail`), one a line, with the seconds since start and E/W/X for errors, warnings and exceptions. |
| `screenshot.png` | The frame, pages and all, when there is a screen. A batch-mode run has no end of frame to wait for, so there the camera is rendered by hand: the room without the pages. |

**After an exception**, a built player writes one by itself, named `…-exception`, with the symptom Crash and the
floor Blocker. Once a session: a repeating exception would fill the folder. The editor never does; the tests own
its exceptions. The reporter (`BugReporter`, Narrative) wakes with the first scene and lives across rooms, like the
smoke test and the probe.

The report writes nothing the player didn't already have on disk, and nothing leaves the machine: the player
attaches the folder to the issue themselves. There is no upload, no account, no telemetry.

## Tests

`BugBarTests` (EditMode, 6):
- the floors, and every severity defined with a turnaround;
- priority from severity and reach;
- each milestone's bar no looser than the last, no Blocker anywhere, and the counts checked with their reasons;
- the tracker's words the same in the code, the plan's sections, the issue form and the triage script (labels,
  colours, lines and the bar's numbers);
- the log tail's ring, its counts and its first exception;
- a report gathered, composed and written whole, with the save and the screenshot, twice in one second.

`BugReporterTests` (PlayMode, 3):
- it wakes with the game and stays quiet in the editor;
- the first exception writes a report by itself, once, with the exception and the world in it;
- a pressed report carries the save, the tail and a screenshot, and asks for nothing more.

## Open

- **The tracker.** Nothing has been pushed, so the labels aren't made and the script hasn't run against real issues.
  `pwsh tools/triage.ps1 -Labels` is the first thing to do after the repo is on GitHub and `gh auth login` is done.
- **Known-issues list.** The release bar allows Minor bugs that are listed. Where the list lives (the store page, a
  `KNOWN_ISSUES.md`) is PRO-08's call.
- **The symptom from the player.** F12 takes no word from the player in-game; the form asks. A small in-game
  picker (a row of symptoms on the pause page) would put the floor on the report before it leaves the machine.
- **A crash the engine doesn't survive.** An exception the game can log gets a report. A hard crash (a driver, a
  native fault) leaves only `Player.log` and Unity's crash folder; the form tells the player where those are only
  by implication. A line for it belongs on the form once there's a build on Steam to crash.
- **CI.** The bar could be a CI check on a release tag (`triage.ps1 -Milestone release` with a token). It waits for
  the tracker to exist.
