using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The Long Grass Camp moves (PRG-21): three sites in the planned rooms; it walks on at first light once its fire has
    /// been had, one site a day, never past the high grass; walking with it costs her the day and makes camp at dusk.
    /// </summary>
    public class CampTests
    {
        [SetUp] public void SetUp() { GameState.NewGame(); }
        [TearDown] public void TearDown() { GameState.NewGame(); }

        [Test]
        public void ItsRoadRunsThroughThePlannedRooms()
        {
            Assert.AreEqual(3, Camp.SiteCount);
            Assert.AreEqual(Camp.PostRoom, Camp.Road[0], "the walkers' post first");
            foreach (var room in Camp.Road)
            {
                var plan = RoomPlans.Find(room);
                Assert.IsNotNull(plan, room + " is a planned room");
                StringAssert.StartsWith("Windreach.", plan.Zone, room);
            }
            Assert.IsTrue(RoomPlans.Find(Camp.PostRoom).Desk, "the post keeps the desk, so the hub's desk is where the map says");
            for (int i = 0; i < Camp.SiteCount; i++)
            {
                Assert.AreEqual(Camp.Road[i + 1], Camp.RoomOfSite(i));
                Assert.AreEqual(i, Camp.SiteOf(Camp.RoomOfSite(i)));
            }
            CollectionAssert.AreEqual(new[] { "Camp_Idrenne", "River_Idrenne_Night", "Grass_Idrenne_Night" },
                new[] { Camp.FireNodeOf(0), Camp.FireNodeOf(1), Camp.FireNodeOf(2) }, "the three fires' scenes");
            Assert.AreEqual(-1, Camp.SiteOf(Camp.PostRoom), "the post is not a site");
        }

        [Test]
        public void ItWalksOnAtFirstLightOnceItsFireIsHad()
        {
            var w = GameState.World;
            var moves = new List<int>();
            System.Action<int> seen = moves.Add;
            Camp.Moved += seen;
            try
            {
                Assert.AreEqual(0, Camp.Site(w), "the fire ring first");
                Assert.IsFalse(Camp.IsReadyToWalk(w));
                DayClock.Sleep(w);
                Assert.AreEqual(0, Camp.Site(w), "it waits for its fire");

                w.Set(Camp.NightKey, 1);   // the first fire
                Assert.IsTrue(Camp.IsReadyToWalk(w));
                DayClock.Sleep(w);
                Assert.AreEqual(1, Camp.Site(w), "the riverbed, whether she walks with it or not");
                DayClock.Sleep(w);
                Assert.AreEqual(1, Camp.Site(w), "one site a day, and only after its fire");

                w.Set(Camp.NightKey, 2);
                DayClock.Sleep(w);
                Assert.AreEqual(2, Camp.Site(w), "the high grass past the Gate");
                w.Set(Camp.NightKey, 3);
                Assert.IsFalse(Camp.IsReadyToWalk(w), "the last fire; nowhere left to walk");
                DayClock.Sleep(w);
                Assert.AreEqual(2, Camp.Site(w), "never past the high grass");
                CollectionAssert.AreEqual(new[] { 1, 2 }, moves);
                Assert.AreEqual("Windreach_Fire_1", Camp.SiteRoom(w));
            }
            finally { Camp.Moved -= seen; }
        }

        [Test]
        public void PlayPastMidnightMovesItToo()
        {
            var w = GameState.World;
            w.Set(Camp.NightKey, 1);
            DayClock.SetPhase(w, DayPhase.Night);
            DayClock.Advance(w, 0.1f);
            Assert.AreEqual(0, Camp.Site(w), "still night");
            DayClock.Advance(w, 0.4f);
            Assert.AreEqual(2, DayClock.Day(w));
            Assert.AreEqual(1, Camp.Site(w), "midnight turned the day: it walked");
        }

        [Test]
        public void WalkingWithItCostsTheDayAndMakesCampAtDusk()
        {
            var w = GameState.World;
            Assert.IsFalse(Camp.WalkWith(w), "not before its fire");
            Assert.AreEqual(1, DayClock.Day(w), "and nothing passes");

            w.Set(Camp.NightKey, 1);
            DayClock.SetPhase(w, DayPhase.Night);
            Assert.IsTrue(Camp.WalkWith(w));
            Assert.AreEqual(2, DayClock.Day(w), "a day on the walk");
            Assert.AreEqual(DayPhase.Dusk, DayClock.Phase(w), "camp is made at dusk");
            Assert.AreEqual(1, Camp.Site(w));
            Assert.IsFalse(Camp.WalkWith(w), "and then it waits for the second fire");
        }

        [Test]
        public void StandInsNameTheRoomsTheyStandFor()
        {
            foreach (var room in Camp.Road)
            {
                var scene = Camp.StandInScene(room);
                Assert.IsTrue(Camp.IsStandIn(scene), scene);
                Assert.AreEqual(room, Camp.RoomOfScene(scene));
                Assert.IsFalse(Camp.IsStandIn(room), room + " is the built room's name");
                Assert.AreEqual(room, Camp.RoomOfScene(room));
            }
            Assert.IsFalse(Camp.IsStandIn("Camp_Idrenne"));
            Assert.IsFalse(Camp.IsStandIn(null));
        }
    }

    /// <summary>
    /// Travel takes time (PRG-21): an hour a way on the macro map, half an hour within a zone; the day moves on by it and
    /// turns past midnight.
    /// </summary>
    public class TravelTests
    {
        [SetUp] public void SetUp() { GameState.NewGame(); }
        [TearDown] public void TearDown() { GameState.NewGame(); }

        [Test]
        public void TheRoadIsTheMacroMap()
        {
            foreach (var z in WorldGraph.Zones)
            {
                Assert.AreEqual(0, Travel.Ways(z.Id, z.Id));
                Assert.GreaterOrEqual(Travel.Ways(WorldGraph.Start, z.Id), 0, z.Id + " is on the road from the shore");
                foreach (var l in WorldGraph.LinksOf(z.Id))
                    Assert.AreEqual(1, Travel.Ways(z.Id, l.Other(z.Id)), "next door is one way: " + z.Id + " to " + l.Other(z.Id));
            }
            var zones = WorldGraph.Zones;
            for (int i = 0; i < zones.Count; i += 5)
                for (int j = 0; j < zones.Count; j += 7)
                    Assert.AreEqual(Travel.Ways(zones[i].Id, zones[j].Id), Travel.Ways(zones[j].Id, zones[i].Id), "as far there as back");
            Assert.AreEqual(-1, Travel.Ways("Nowhere.Much", WorldGraph.Start));
            Assert.Greater(Travel.Ways(WorldGraph.Start, "Blank.OldCapital"), 5, "the capital is a long way from the shore");
        }

        [Test]
        public void AnHourAWayAndHalfAnHourWithinAZone()
        {
            Assert.AreEqual(Travel.HoursWithinZone, Travel.Hours(WorldGraph.Start, WorldGraph.Start));
            var next = WorldGraph.LinksOf(WorldGraph.Start)[0].Other(WorldGraph.Start);
            Assert.AreEqual(Travel.HoursPerWay, Travel.Hours(WorldGraph.Start, next));
            Assert.AreEqual("half an hour", Travel.Describe(0.5f));
            Assert.AreEqual("an hour", Travel.Describe(1f));
            Assert.AreEqual("3 hours", Travel.Describe(3f));
        }

        [Test]
        public void EveryWaypointStandsInAZone()
        {
            foreach (var wp in Atlas.AllWaypoints)
            {
                var zone = Travel.ZoneOf(wp);
                Assert.IsNotNull(zone, wp.Id);
                Assert.IsNotNull(WorldGraph.Find(zone), wp.Id + "'s zone " + zone);
            }
        }

        [Test]
        public void TheJourneyMovesTheDayOnAndTurnsItPastMidnight()
        {
            var w = GameState.World;
            var quay = Atlas.FindWaypoint("desk.Saltmarrow_A");
            var lamp = Atlas.FindWaypoint("lamp.Saltmarrow_Lighthouse");
            Assert.IsNotNull(quay); Assert.IsNotNull(lamp);
            float h = Travel.Hours(quay, lamp);
            Assert.GreaterOrEqual(h, Travel.HoursWithinZone);

            float before = DayClock.Time(w);
            Assert.AreEqual(h, Travel.Journey(w, quay, lamp), 1e-5f);
            Assert.AreEqual(before + h / 24f, DayClock.Time(w), 1e-4f, "the day moves on by the journey");
            Assert.AreEqual(1, DayClock.Day(w));

            w.Numbers[DayClock.TimeKey] = 0.99f;
            Travel.Journey(w, lamp, quay);
            Assert.AreEqual(2, DayClock.Day(w), "a late journey arrives tomorrow");
            Assert.AreEqual(Travel.Hours(lamp, quay), Travel.Hours(quay, lamp), 1e-5f);
        }
    }
}
