# Tracker and cadence (PRO-02, v1)

This covers how the plan becomes a tracker without becoming two plans, and the weekly build. The plan
(`docs/02-production-plan.md`) stays the source: a row is edited there first, and the tracker follows it.

## The plan as data

`OWSBG.Core.Plan` reads the plan's markdown: every task row (`| NAR-01 \`[x]\` | … |`) with its id, the section it
sits in (the same eight areas the bug bar uses), its status mark, its milestone (or range, `M2–M3`), its
references and what it comes after; and the six milestones from §2 with their exit criteria. It refuses a row
outside a section or in the wrong one, and the tests hold that every "after" names rows the plan has (a range
like `NAR-07..11`, `ENV-all`, `all`, `M1 exit` included).

A row's **title** for an issue is its first clause: the text before the "; v1 in …" note that an in-progress
row carries, without a long parenthesis, at most eighty characters. The whole cell goes in the issue's body.

`Unity.exe -batchmode -executeMethod OWSBG.Setup.TrackerSetup.Export -quit` writes it all to `logs/tracker.json`.

## The tracker

GitHub issues, made by `pwsh tools/tracker.ps1` (the GitHub CLI, signed in):

| Thing | Made as |
|---|---|
| Milestone | one per plan milestone, "M0 Pre-production" … "M5 Release candidate", with the exit criteria as its description |
| Labels | `area:nar` … `area:pro` (the bug bar's), `plan`, `in-progress` |
| Issue | one per row not yet done, titled `[NAR-01] Story bible v2`, in its milestone, labelled `plan` and its area; `in-progress` when the plan marks it `[~]` |

The script finds an issue by the `[ID]` at the front of its title and never makes one twice. A row marked done
closes its open issue with a note; a row that turns in progress gets the label. Bugs are separate: they come
through the bug-report form with their own labels (`docs/design/bug-bar.md`), and a bug that belongs to a row
links it in the thread.

`-DryRun` says what it would make. `-Json logs/tracker.json` skips the Unity export when one exists. Exit 0
done, 1 something could not be made, 2 gh or the export missing.

## The cadence

- **A build a week.** CI runs on a schedule, Mondays at 06:00 UTC, as well as on every push: both test suites, the
  Windows build and its smoke run. The week's build is that run's artifact (`LastCartographer-Windows`, kept
  fourteen days). From M4, the weekly full playthrough (`docs/design/playthrough-matrix.md` for the orders) is
  played on it.
- **Triage** at the same sitting: `tools/triage.ps1` against the milestone's bar (`docs/design/bug-bar.md`).
- **The plan is touched in the same commit as the work.** A row goes `[~]` with its v1 note when the first
  version lands, and `[x]` when nothing in it is open. The tracker script is run after, not before.

## Tests

`PlanTests` (EditMode, 3):
- every row of the real plan parses, once, in its own section, in a real milestone, after rows that exist, with
  a title that fits; the six milestones read; PRO-02 itself is in progress;
- the parser reads the shapes the sections use (a Ref column or not, a range, a v1 note, a long parenthesis, the
  after vocabulary) and refuses a row out of place; the export round-trips;
- the tracker script names the bar's areas and the plan's labels, and CI has its schedule.

## Open

- **The script hasn't run.** `gh` isn't installed on this machine. The first run after `gh auth login` makes about
  a hundred and ten issues; do it with `-DryRun` first.
- **Dependencies as links.** An issue's "after" is text. GitHub's task lists or the Projects board could make it
  a relation; not yet worth it.
- **Weekly build on a tag.** The scheduled run isn't tagged, so it's not on Steam. A `v0.x.y-weekly` tag from
  the schedule would put it on the beta branch, once the Steam secrets exist.
