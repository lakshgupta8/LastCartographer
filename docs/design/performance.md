# Performance (PRG-24, v1)

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

## Tests

`PerfTests` (EditMode, 4):
- physics at 60 Hz;
- the lock for every kind of display;
- percentiles, and one hitch in twenty;
- world keys made once, with the strings saves hold.

`PerfProbeTests` (PlayMode, 2):
- the probe, run short in the editor: it walks two rooms, times the way through the room manager, lifts the cap and
  puts it back, and writes its report;
- Wren is found without searching.

## Open

- **Target hardware.** The GTX 1060 run, and 1080p on it, is PRO-06's.
- **The painted rooms.** Real art brings sprite atlases, overdraw from the paper layers, and texture memory per room
  bundle. The budgets for those (batches, texture MB per room, resident bundles) get set when a painted room exists
  to measure.
- **Fights.** The probe only walks. A boss fight, a full camp at night and the Blank's islands need their own
  samples; the probe's route can take a scripted list of rooms when those rooms are built.
- **Not in CI.** GitHub's Windows runners have no GPU. A CPU-only `-Batch` run could still watch transition times
  and allocations there.
