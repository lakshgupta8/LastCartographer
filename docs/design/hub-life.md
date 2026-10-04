# Hub Life (PRG-15, v1)

The day, where people stand in it, and what an anchored town does with both. Runtime: `DayClock` (Core),
`DayCycle` (World), `NpcSchedule` (Narrative), `<<clock>>`, `<<sleep>>`, `phase()`, `day()`, `phase_in()` (Yarn).

## 1. Rules
- The world has a **day**: a fraction 0–1 that advances with play time, never with the story. Fade stages still
  move only on beats (DES-04); the day is texture, not pressure.
- Four **phases**: dawn, day, dusk, night. A day is 15 minutes of play by default (`DayCycle.DaySeconds`).
  The clock pauses while Wren is frozen: talk, menus, cutscenes.
- **Resting at a desk sleeps to the next dawn** and counts a day. The day count also turns when the clock passes
  midnight, by play or on the road: fast travel takes an hour a way on the macro map (PRG-21, `moving-camp.md`).
- Story beats may set the hour (`<<clock dusk>>`): the prologue's Edge is at dusk; Wren wakes on the shore at dawn.
- Every NPC with a **schedule** has a post per phase: a spot in its room, a facing, an activity, and optionally
  the Yarn node it starts there. On a phase change it walks along the floor to the new post; a room that loads
  mid-day snaps. During a conversation it stays put.
- A phase without a post falls back to the latest earlier post of the day, then to the last post (before a dawn
  post exists, the night's holds).
- **Anchored places keep the hour they were sealed at.** Inside one, `DayClock.PhaseIn` is that hour forever, the
  paper is tinted for it, and schedules stop following the clock: they **loop** their posts in order on a fixed
  period (`NpcSchedule.LoopSeconds`, 24 s). Lines said at looped posts carry `#still` (style guide 6).
- **Held** places (the Holdfast way) keep their days: life goes on.

## 2. The day

| Phase | Fraction | At 15 min/day | Paper |
|---|---|---|---|
| dawn | 0.00–0.10 | 1.5 min | a little warm, a little cool, both fading |
| day | 0.10–0.50 | 6 min | as drawn |
| dusk | 0.50–0.62 | 1.8 min | warm cast (`_OWSBG_Dusk`) |
| night | 0.62–1.00 | 5.7 min | cool, darker (`_OWSBG_Night`), easing before dawn |

The tints are two globals read by the paper-grain pass after the held grade; `DayCycle` eases them over 1.5 s
so `<<clock>>` and room changes never pop. The region's sun follows them too (ENV-10, lighting.md §2): night
dims it to a third and cools it, dusk warms it; the white and anchored Halden have no hour and keep their light.

## 3. Story hooks
```yarn
<<clock dawn>>                       // dawn | day | dusk | night (also morning, noon, evening)
<<sleep>>                            // next dawn, day + 1
<<if phase() == "night">>            // the world's phase
<<if phase_in("Saltmarrow_A") == "dusk">>   // the place's (locked when anchored)
<<if day() >= 3>>
```
Code: `DayClock.Advance / SetPhase / Sleep / Lock / TimeIn / PhaseIn`, `NpcSchedule.AddPost / PostFor / Current`.

## 4. WorldState
`Numbers["$day_time"]` (fraction), `Numbers["$day"]` (count from 1), `Numbers["place.<id>.locked_time"]` (set by
`Places.Decide` on Anchored). Saves need nothing extra.

## 5. Greybox bindings
- **Sable** (room A): dawn and day mending nets at the quay; dusk reading the ledger; night asleep under the
  stilts ("Quay's shut. So am I." `#still`). Anchor the quay and she loops the four posts every 24 s.
- **Dotha** (room B): dawn and day at her stoop; dusk singing to the water by the tether-post ("Not now. The
  water's listening." `#still`); night asleep ("Ask me in the light." `#still`).
- The atlas page prints `Day 3 · dusk`, and `(held at this hour)` inside an anchored place.
- A post's activity string names the clip a drawn NPC shows there by its first word (`npc-animation.md` §3, CHR-11):
  Sable's "mending nets", "reading the ledger" and "asleep under the stilts" have clips; Dotha's "singing to the
  water" and "asleep" do; "on her stoop" idles. Rename an activity and its clip is lost silently.

## 6. Open
- Routes across rooms (an NPC whose dusk post is next door) and platforms: NavMesh or authored paths (the
  plan's NavMesh line; v1 is one floor per room).
- Shops and **stock that never varies** in anchored towns (DES-05).
- What sleeping does to commissions (expiry, DES-06 §open). What it does to the Windreach camp is settled: a new
  day walks it on once its fire is had (PRG-21, `moving-camp.md`).
- The locked sky in real art. The hubs' furniture is drawn (ENV-09, `paper-kit.md` §2a); looping props (a net that
  sways, a lamp that gutters) would be sheets like the cast's.
