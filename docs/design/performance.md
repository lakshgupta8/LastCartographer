# Performance (PRG-24, PRO-06, v1)

This covers the 60 fps lock, the budgets a build is measured against, the probe that measures them, and what the
first measurements found and fixed. The targets are PRO-06's: 60 fps at 1080p on a GTX 1060-class card, and a room
transition under 100 ms.

## 60 fps, and 60 Hz physics

The combat doc says "60 fps locked, physics at 60 Hz fixed step". The project had physics at **50 Hz** (a 0.02 s
fixed step), so every frame-counted window ran a fifth longer than written:
- the 6-frame jump buffer was 120 ms, not 100;
- the 5-frame coyote time was 100 ms, not 83.

Hitstop was already counted in 60ths. The fixed step is now 1/60 s. The play suite passed at 60 Hz, because the
controller works out its speeds from heights and times rather than per frame. One Collapse test later failed once the
frame timing moved: it waited for a lit beat, then struck a frame late, after the window had closed. It now strikes
whenever the boss is drawn and waits a frame when it isn't.

`FrameRate` (Core) locks rendering at 60 when the game starts:
- **Refresh a multiple of 60** (60, 120, 180, 240 Hz, and 59.94 or 119.88): vsync on every first, second, third or
  fourth refresh, so nothing tears.
- **Anything else** (75, 144, 165 Hz): vsync off and a 60 cap, because vsync alone would run at 144.

## The budgets (`PerfBudget`)

| What | Budget | Why |
|---|---|---|
| Frame time | 16.7 ms at the 95th percentile, uncapped | 60 fps with room to spare |
| Room transition | 100 ms from the request to Wren standing in the room | PRO-06 |
| A quiet frame (standing in a room) | 64 bytes of managed allocation on average, and no collection | a garbage collection is a hitch |
| Draw batches | 400 in a greybox room | a ceiling until the painted rooms set their own |

`RoomManager.LastTransitionMs` times every transition, and `RoomManager.Transitioned` announces it.

## The probe (`PerfProbe`, `-perf`)

```
pwsh tools/perf.ps1                         # the release build in Builds/Windows; report in logs/perf.json
pwsh tools/perf.ps1 -Exe <dev build> -Attribute
```
- **How it runs:** it starts the build with `-perf -skipPrologue` at 1920×1080 in a window. A `-batchmode` player
  doesn't render, so `-Batch` measures the CPU side only.
- **What it does:**
  - lifts the 60 fps cap, so a frame costs what it costs;
  - walks a route from the start room, always taking the first way on to a room it hasn't seen, and walking back
    from dead ends;
  - samples 300 frames in each room once it has settled;
  - times every transition.
- **What each room gets:**
  - frame times (average, median, 95th and 99th percentiles, worst);
  - draw batches, SetPass calls and triangles;
  - managed bytes per frame and garbage collections.
- **How it reports:** as JSON against the budgets, plus one `[OWSBG] perf:` line per room and per transition. The
  exit code is 0 when everything is within budget, 1 when something is over, and 2 when a room never came in.
- **Bytes per frame need a development build.** Unity's per-frame allocation counter exists only there, so a release
  build reports "n/a" and counts collections instead.
- **`-perfAttribute`** (development builds) finds who allocates. In the first room it switches off each of our script
  types in turn and puts the frame's allocation down to the scripts whose absence lowers it.
- **The editor won't do for this.** Its own loop allocates over a megabyte a frame.
- **`-skipPrologue`** is new for built players: they start at Saltmarrow, as the editor's Play From Saltmarrow does.
  Testers can use it too.

## First measurements (2026-09-28)

Measured on this machine, not the target: an RTX 3050 Laptop GPU on Direct3D 12, 1920×1080, the greybox rooms of
`v0.1.0`. The GTX 1060 figure (PRO-06) still needs that hardware.

| Room | Release fps (avg ms) | p95 ms | Batches | SetPass | Triangles |
|---|---|---|---|---|---|
| Saltmarrow A (the Drowned Quay) | 813 (1.23) | 1.62 | 51 | 40 | 1,726 |
| Saltmarrow Shore | 842 (1.19) | 1.61 | 21 | 21 | 639 |
| Saltmarrow Stilts | 863 (1.16) | 1.47 | 29 | 22 | 745 |
| Saltmarrow Boardwalk | 867 (1.15) | 1.49 | 28 | 27 | 2,225 |
| Saltmarrow B (Merrow's End) | 860 (1.16) | 1.54 | 40 | 28 | 867 |
| Saltmarrow Tetherline | 881 (1.14) | 1.46 | 28 | 26 | 703 |

- **Transitions: 9–16 ms**, against a budget of 100.
- **Neighbour loading:** it asks Addressables for a neighbour's dependencies, which for local bundles only makes
  sure they're cached. The room itself still loads on the way in. At these sizes that doesn't matter; with painted
  rooms it's the first thing to revisit.
- **Batching:**
  - the SRP Batcher is on, and runtime materials are shared, one per colour (`InkMaterials`), never one per object;
  - static batching is on for the Windows player;
  - there are no sprites yet, so sprite atlases come with the art (ENV rows);
  - a few props tint through `MaterialPropertyBlock`, which takes them out of the SRP Batcher; that's worth
    changing when the art replaces them.

**Garbage, found and fixed.** The development build allocated **333–538 bytes on every quiet frame**, and a
collection landed inside a half-second sample (the Drowned Quay in one run, Merrow's End in another). The attribution pointed at the fade, the held grade, the hour, the
schedules and the Clarity meter. All of them were building world-state keys (`"place." + id + ".fate"`) as new
strings every frame.
- **Keys:** `Keys.Of` (Core) now makes each key once, and the key builders in `Places`, `FadeStages`, `DayClock`,
  `BoundsWalks`, `Commissions`, `Bosses`, `Gauntlets` and `Keystones` go through it. Saves are unchanged: the
  strings are the same.
- **The lantern check:** `Clarity.IsLanternLit`, which depends only on the room, is worked out once per room.
- **Layer masks:** `LayerMask.GetMask` in per-frame hit checks is looked up once (`Layers`).
- **Finding Wren:** the options page no longer searches the scene for Wren every frame
  (`WrenController.Current`).

After these: **25–29 bytes a frame, and no collections** on the whole route. What's left is the HUD's text
refresh, a few hundred bytes every tenth of a second.

## PRO-06: the target, and the gate

PRO-06 is 60 fps at 1920×1080 on a GTX 1060-class card, with every room transition under 100 ms. This machine can't
prove it, because its card is faster. So the target is written down as something a tester's run can meet:
`PerfTarget` (Core) judges runs, and `tools/perf-gate.ps1` gives the verdict.

**Classing a card.** `PerfTarget.Cards` scores cards against a GTX 1060 6GB (1.0) (56 rows), matching the name Unity
reports. The scores are rough rasterisation figures from public benchmark aggregates, rounded, and are for classing,
not benchmarking.
- **Target:** 0.85–1.10× a GTX 1060. That covers the GTX 1060 3GB and 6GB, GTX 970, RX 470/480/570/580 and GTX 1650
  SUPER.
- **Below:** a GTX 1050 Ti, GTX 1650, an Iris Xe.
- **Above:** everything faster, including this project's own RTX 3050 6GB Laptop GPU (1.2×).
- **Unknown:** a card not in the table. Add it before its run can count.

The probe writes the card's class, score and memory into its report (`gpuClass`, `gpuScore`, `vramMb`).

**What proves PRO-06.** A run proves it when all of these hold:
- the card is Target or Below;
- it was rendered (not `-batchmode`) at 1920×1080;
- it covered the whole six-room route;
- every room's p95 is within 16.7 ms and every transition within 100 ms;
- the probe found no garbage over budget.

A faster card's run never proves it. Its worst p95 is **projected** onto the target instead: scaled by the card's
score, as if the frame were all GPU work. A CPU-bound frame doesn't scale, so this errs high. The gate warns when the
projection would be over, so a fast card can still raise a warning sign.

**The gate.**
```
pwsh tools/perf.ps1 -Out logs/perf/<machine>.json     # on each tester's machine
pwsh tools/perf-gate.ps1                              # judges logs/perf/*.json into logs/perf/gate.md
```
It exits 0 when some run proves the target, 1 when none does yet, and 2 when the folder can't be read. The report
lists every run with its card, class, worst p95 (and projection), worst transition, and every reason.

**CI.** GitHub's hosted Windows runners have no GPU. The smoke job now also runs the probe headless on the build
(`tools/perf.ps1 -Batch`) and uploads `perf-ci.json` beside the smoke log. It fails the run on a transition over
100 ms, or on garbage or a collection in a quiet frame. Frame rate is left to the gate.

**A probe fix the headless run found.** Uncapped and unrendered, the player runs at around 10,000 fps, so the probe's
60-frame settle was over in 6 ms. The garbage from loading the room then set off a collection inside the Shore's
"quiet" sample, and the run failed.
- A room now settles for at least half a second as well as 60 frames.
- The probe then counts the collections since arriving (`loadCollections`, the load's cost, not budgeted) and
  collects once before sampling, so the sample measures quiet frames only.
- Headless runs show 2–4 load collections per room. At these sizes they fall inside the 5–15 ms transitions.
  Painted rooms will load more, and a collection in the frames after arrival is a hitch worth watching.

**The wrapper fix.** `perf-gate.ps1` and `feel-gate.ps1` started Unity with `&`. Unity is a windowed program, so
PowerShell didn't wait for it, the exit code was lost and the report wasn't there to print. Both now start it with
`Start-Process` and wait.

### Measured, 2026-09-29 (`4a64602`, release, this machine)

| Run | Rooms p95 | Transitions | Garbage | Verdict |
|---|---|---|---|---|
| Rendered, 1920×1080, RTX 3050 6GB Laptop (Above, 1.2×) | 1.90–2.38 ms (projected 2.86) | 12–27 ms | 0 collections | within budget; can't prove the target |
| Headless (`-Batch`), as CI runs it | 0.11–0.17 ms (CPU only) | 5–13 ms | 0 collections, 2–4 per load | within budget |

The rendered frames are a little slower than the first measurement's (1.4–1.6 ms average against 1.2). This is the
same greybox on a laptop, most likely on a different power plan; it is far inside the budget either way.

## Tests

`PerfTests` (EditMode, 4):
- physics at 60 Hz;
- the lock for every kind of display;
- percentiles, and one hitch in twenty;
- world keys made once, with the strings saves hold.

`PerfTargetTests` (EditMode, 3):
- **Classing cards:** a GTX 1060, a 3 GB one, an RX 580 and a GTX 970 are Target; a GTX 1050 Ti and an Iris Xe are
  Below; a GTX 1660 SUPER and this laptop are Above; a batch run's null device is Unknown; an RX 5600 isn't read as a
  560.
- **What proves it:**
  - a target or slower card within budget proves it;
  - a faster card doesn't, and is projected;
  - a run over 16.7 ms, a slow transition, a short route, a lower resolution, an unrendered run or an unknown card
    fails;
  - the probe's garbage findings count once.
- **The gate over a folder:** a laptop run alone is "Not yet"; adding a GTX 1060 run makes it "Met"; unreadable files
  are skipped; an empty folder says how to start.

`PerfProbeTests` (PlayMode, 2):
- the probe, run short in the editor: it walks two rooms, times the way through the room manager, lifts the cap and
  puts it back, and writes its report. The report reads back as `PerfReport` with the card classed, and a two-room
  walk proves nothing;
- Wren is found without searching.

## Open

- **The target is decided** (2026-09-29): a GTX 1060-class card, for the widest reach. The development machine's card
  says nothing about the game's specs, so its runs stay "Above".
- **Target hardware.** PRO-06 is met the day a tester's GTX 1060-class run passes the gate. None has been run yet.
- **The painted rooms.** Real art brings sprite atlases, overdraw from the paper layers, and texture memory per room
  bundle. The budgets for those (batches, texture MB per room, resident bundles) get set when a painted room exists
  to measure.
- **Fights.** The probe only walks. A boss fight, a full camp at night and the Blank's islands need their own
  samples; the probe's route can take a scripted list of rooms when those rooms are built.
- **CI measures the CPU side only.** A self-hosted runner with a target card would let CI run the gate itself.
- **The card table** is rough, and needs a row for any card a tester brings that it doesn't know.
