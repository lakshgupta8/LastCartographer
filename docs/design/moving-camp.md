# The Moving Camp and Day-Advance Travel (PRG-21, v1)

Bible 4.5 ("the Long Grass Camp moves between three sites across the game") and 8.5 ("walk with the clan for three
days; camp scenes each night"); DES-11 §3; hub-life.md. Windreach isn't anchored: its hour moves and its hub moves
with it. Travel anywhere now takes time too.

The rules are `Camp` and `Travel` (Core). The walk is `CampWalk` (World). The sites are `CampSite` and the stand-in
rooms are `CampRooms` (Narrative). The Yarn is `Windreach_Camp_Road.yarn`: the bedroll and the ashes.

## The camp

| Site | Room | Its fire (scene) | Night written |
|---|---|---|---|
| 0 | `Windreach_Camp_2`, the fire ring | `Camp_Idrenne` | 1 |
| 1 | `Windreach_River_2`, the riverbed | `River_Idrenne_Night` | 2 |
| 2 | `Windreach_Fire_1`, the high grass past the Gate | `Grass_Idrenne_Night` | 3, and the walk is walked |

- **The scenes count the nights** (`windreach.camp.night`, NAR-10). The camp keeps its own site
  (`windreach.camp.site`).
- **At first light it walks on.** Once the fire has been had where the camp stands, the next new day moves it one
  site. That day can come from a sleep at any desk, a journey past midnight, or play past midnight; the camp goes
  whether Wren walks with it or not. It never walks past the high grass.
- **Walking with it costs her the day.** The bedroll by the wagons offers "Sleep, and walk with them."
  (`<<camp walk>>`):
  - The paper closes and she sleeps.
  - The day count turns and the camp moves.
  - She walks the day with them, and the paper lifts at dusk at the next site, by the bedroll.

  The caption reads "A day's walk with the clan. The riverbed, at dusk." Before the fire, the bedroll says nobody's
  walking tomorrow.
- **Where the camp stands:** the wagons in a ring, the fire, Idrenne with that site's fire scene, and the bedroll.
- **Where it doesn't:** a ring of stones round old ashes.
  - If the camp has gone on, the ashes say which way it went: "Still warm. The grass is flattened east, toward the
    riverbed."
  - If it hasn't come yet: "Somebody camps here, some nights."
- **The walkers' post** (`Windreach_Camp_1`): one wagon stays at the Long Grass with the desk and the ledger, so the
  hub's desk is always where the map says. Fast travel never has to chase the camp. The macro map's links don't
  change either (world-map.md's open question): the sites are rooms in three zones, and the camp is what stands in
  them.
- **Idrenne's changed line:** at the fire ring after the first fire, she now says "We walk at first light. There's a
  bedroll by the wagons, if you're coming." Before, she said the camp had already gone on while standing at its
  fire.

**Yarn:** `<<camp walk>>` (waits until she's there); `camp_ready()`, `camp_site()` (0 the fire ring, 1 the
riverbed, 2 the high grass).

## Stand-in rooms

Windreach isn't built (ENV-07). Until it is, `CampRooms` builds the camp's four rooms at runtime when RoomManager asks
for `Camp_<planned room>`: `Camp_Windreach_Camp_1`, `_Camp_2`, `_River_2`, `_Fire_1`.
- **Each room:** grass, straw and sky paper; spawns Start, West, East and Camp.
- **Exits:** they're joined in walking order along the flattened grass (post → fire ring → riverbed → high grass),
  so the camp's road can be walked as well as travelled.
- **The post has the desk.** The three sites each carry a `CampSite`.
- **Once a room is built,** `CampWalk` uses it instead: `SceneFor` prefers an addressable scene of the planned
  room's name.

## Day-advance travel

Fast travel was free (survey.md: "the story may want a day to pass"). The road now takes time:
- **An hour for each way between zones** on the macro map (`WorldGraph`, fewest ways, gates ignored: the page only
  offers places she has been). **Half an hour within a zone.**
- The day moves on by the journey while the paper is closed. A late journey arrives tomorrow and turns the day
  count, and the camp may walk on.
- **Anchored places** still show their locked hour on arrival.
- **The caption** on arrival: "3 hours on the road. Dusk, day 4." `FastTravel.LastHours` keeps the number.
- **The quay to the fourth lighthouse** along the coast is the test's journey.

Hub-life's rule changes from "only resting at a desk counts a day" to: a day counts when the clock passes midnight,
by sleep, play or the road.

## Tests

`CampTests` (EditMode, 5):
- The camp's road runs through the planned rooms, and the post keeps the desk.
- It walks on at first light only after its fire, one site a day, never past the high grass.
- Play past midnight moves it too.
- Walking with it costs a day and makes camp at dusk (not before the fire).
- Stand-ins name the rooms they stand for.

`TravelTests` (EditMode, 4):
- The road is the macro map: every zone is on it; next door is one way; as far there as back; the capital is a long
  way from the shore.
- An hour a way, half an hour within a zone, and the words for it.
- Every waypoint stands in a zone.
- A journey moves the day on by its hours, and a late one arrives tomorrow.

`MovingCampTests` (PlayMode, 2):
- **Where the camp stands:**
  - The post keeps the desk, at the head of the road.
  - The fire ring has the camp, Idrenne and the bedroll.
  - The riverbed is ashes that say "some nights".
  - After the first fire and a sleep, the camp arrives at the riverbed while she stands there, with the second
    fire's scene.
  - The fire ring's ashes then point toward the riverbed.
- **Walking with the clan** (through the shipped Yarn):
  - The bedroll refuses before the fire. After it, Idrenne says they go at first light, and "Not yet" keeps her
    there.
  - "Sleep, and walk with them" takes her to the riverbed: a day later, at dusk, with the camp, beside the bedroll,
    unfrozen, the paper lifted, with the caption.
  - The second fire can be had there.

`SurveyPlayTests.FastTravelFromTheDeskToTheLitLamp`: the journey from the quay to the lamp takes its hours, and the
day moves on by them.

## Open

- **The real rooms:** Windreach's built rooms (ENV-07) replace the stand-ins. The camp's three sites then need the
  wagons and the fire dressed, and `CampSite` placed.
- **The walk as a set piece.** v1's day passes behind closed paper. A walked version (the clan on the road in the
  background, a fledgling leaping) is CHR-14's and AUD's.
- **The ledger travels?** Commissions.md's "Windreach's moving camp carries its own" is answered as no for v1: the
  ledger stays at the post with the desk.
- **Travel and commissions:** a journey's hours don't expire anything yet (DES-06's open expiry question).
- **Walking time isn't counted.** Rooms walked between still cost only the play time they take, not map hours.
