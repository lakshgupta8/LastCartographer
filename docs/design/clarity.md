# Clarity and the Lantern-Radius (PRG-18, v1)

Bible 4.6, 4.7 and 10; combat doc §3 and §5; DES-11 §3. Clarity is Wren's capacity to be in the Blank untethered:
a meter and a gate. The meter is also her lantern-radius. In the Greyfold and the Blank, colour exists only round
her, and everything beyond is white paper with outlines at the edge of the eye.

The rules are `Clarity` (Core). The meter is `ClarityMeter` (World), which every Wren carries (the controller adds
it). The white is `UntetheredZone`. The published radius is `Lantern`. The picture is a stage at the top of the
paper-grain pass (`PaperGrain.shader`), so the grain lies on the white.

## The level

| Level | When | Untethered for | Full radius |
|---|---|---|---|
| 0 | Before the Road That Stops | 0 s: the white gives her back at once | 3.5 units |
| 1 | `<<grant Clarity>>` at Road_3, the act break | 6 s | 5 |
| 2 | The Half-Cathedral Bells silenced (`boss.bells.defeated`): "Clarity grows" | 10 s | 6, the Greyfold's "about six" |
| 3 | Isolde's atlas (`act3.started`): "grows in Act 3" | 16 s | 7 |

When the level rises, the meter fills and the caption says **Clarity grows.** (not for the first grant, which has
its own scene).

## The meter

- **Untethered** means inside an `UntetheredZone`, or anywhere in the drift (`Blank_Hollow_3`, the only room
  untethered from wall to wall). There the meter runs down one second per second.
- **Everywhere else is held ground.** The meter fills at 4 s per second. The last ground she stood on there is where
  the white gives her back, and so is the spot where she entered a room.
- **Empty:** she is drawn back to held ground for a mask, never her last, and the meter is full again. The caption
  is "Drawn back to held ground."
- **Without Clarity** the meter is empty, so any white patch gives her back at once ("The white will not hold her.
  Not yet."). That is the gate: "White patches hidden across every region" (bible 4.0) need no other lock. A longer
  patch needs a higher level.
- **A lost Remnant's touch** takes a mask and 3 more seconds (combat doc §3, "hits from Remnant drain more"). Any
  enemy can do this through `Enemy.ClarityDrain`. The first to use it is `LostRemnant`, a greybox pale figure that
  drifts to her through the white.
- **HUD:** a thin ochre bar under the Inkwell, shown only while the meter runs or refills.

## The radius

- **Full above half the meter.** Below half, the radius narrows steadily to 1.2 units at empty (the bells' floor).
  The radius is how the player reads the meter without looking at the HUD.
- **The Field lantern** widens it by 3 units for its 10 seconds (combat doc §5, "in the Blank, extends
  lantern-radius"). The meter is `IRevealable`, so the lantern reaches it like everything else it shows.
- **A fight can hold it.** While the Half-Cathedral Bells ring, they hold the radius (`Lantern.Hold`), shrinking it
  ring by ring. They let go when they're silenced, on a retry, or when they're gone.
- **The Road That Stops' cobbles** (`LanternPlatform`) are drawn within her radius, whatever it is now. At level 1
  that's 5 units; the gauntlet was built around 3.5, and each next cobble is still in reach and the one after it is
  not.

## The picture

Three globals, published every frame by the meter:

| Global | Meaning |
|---|---|
| `_OWSBG_Lantern` | How much of the room is drawn round her lantern, 0..1. It eases in and out over half a second, and is 0 once she's gone |
| `_OWSBG_LanternRadius` | The radius in world units (the Bells' global from CMB-16, now the meter's) |
| `_OWSBG_LanternCentre` | Her centre on screen (viewport x, y), the radius in viewport heights, the aspect |

In the paper pass:
- **Inside the radius** the image is untouched.
- **Across the edge**, over 0.85 to 1.15 of the radius, it turns to paper white.
- **Beyond, the image's own edges survive** as grey outlines. They are faint near the middle of the frame and
  stronger toward its edges, so buildings "appear only in peripheral vision" (bible 4.6).

**Where it's lit:** the whole Blank (islands included) and the Greyfold, but not the Edge Camp or the Edge itself.
Those are the last places colour reaches by itself; the Edge's white is the prologue's own fade (`BlankEdge`). A
white patch anywhere is lit while she stands in it. Runtime rooms count as the rooms they stand in for: arenas by
their planned room, gauntlets by theirs.

**In the Blank's islands** (`IslandBuilder`), the white between the drift's edge and each island is untethered: a
short run of meter between one held place and the next.

## Tests

`ClarityTests` (EditMode, 5):
- The level grows with the story.
- Each level lasts longer and sees further.
- The radius is full down to half the meter and narrows to the floor.
- The rooms lit are exactly the Blank's and the Greyfold's, less the Edge Camp and the Edge, with runtime rooms
  resolved.
- Only the drift is untethered wall to wall.

`ClarityMeterTests` (PlayMode, 10):
- **The meter itself:**
  - Every Wren carries one.
  - It runs down in the white and fills again on held ground.
  - Empty, it draws her back for a mask, never her last.
  - Without Clarity it draws her back at once.
  - At level 1 she can't stay 8 seconds; after the bells she can. The caption shows, and Act 3 grows it again.
  - A lost Remnant's touch takes a mask and more than the white would have.
- **The radius:**
  - It narrows below half the meter, the lantern widens it, and a hold overrides it and lets go.
  - The bells hold it only while they ring.
  - The Road's cobbles follow it.
- **The lighting:** only the Greyfold, the Blank and a white patch are lit, and nothing stays lit after she's gone.

`LanternRenderTests` (PlayMode, 3) render a red wall through the project's renderer and read the pixels:
- **In the Greyfold:** red round her and within her radius. Paper beyond it in every direction. A dark stripe far
  out goes to paper with its outline kept.
- **A narrower radius** whitens what was in it.
- **Saltmarrow** keeps its colour.

The renders are saved as `logs/lantern-*.png`.

`UiTests.HudShowsClarityOnlyWhileItRuns`: the bar is hidden before Clarity and while full on held ground, and shown
and running down in the white.

## Open

- **The drift room and the white patches are not built.** The drift is planned (PRG-20). No region yet has a white
  patch; each patch is a zone plus a secret behind it (DES, ENV-08). The islands' white strips are the meter's only
  in-game use so far.
- **The lost Remnant is greybox.** It drifts and clings. Its family's kit (a telegraph, a grey that the Remnant
  Charter reads differently) is CMB's, and the rooms that list "lost Remnant" place it when they're built.
- **Screen-space radius.** The pass measures the radius on screen at Wren's depth. Parallax layers far behind her go
  white at the same screen distance, which reads as "parallax layers dissolve" (bible 4.6), but a pass that reads
  depth could keep near props by their true distance. That can wait for the art.
- **The Bells' own full radius is 7**, whatever her level. Their fight is the only place her radius is not her own.
