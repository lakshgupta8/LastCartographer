using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>Sable's boat north (<see cref="Boat"/>): two berths, a day's hours on the water, a ride and not a way on the map.</summary>
    public class BoatTests
    {
        [Test]
        public void TwoBerthsAndNothingElse()
        {
            Assert.IsTrue(Boat.TryDestination("windreach", out var room, out var spawn, out int berth));
            Assert.AreEqual(("Windreach_River_2", "Camp", Boat.AtRiver), (room, spawn, berth), "the riverbed, among the hulls");
            Assert.AreEqual("Windreach.DryRiver", WorldGraph.ZoneOfPlace(room) ?? RoomPlans.Find(room)?.Zone);
            Assert.IsTrue(Boat.TryDestination("quay", out room, out spawn, out berth));
            Assert.AreEqual(("Saltmarrow_A", "Desk", Boat.AtQuay), (room, spawn, berth));
            Assert.AreEqual("Saltmarrow.Quay", WorldGraph.ZoneOfPlace(room));
            Assert.IsFalse(Boat.TryDestination("emberdown", out _, out _, out _), "she rows two places");
        }

        [Test]
        public void RowingMovesTheBoatAndTheDay()
        {
            var w = new WorldState();
            Assert.AreEqual(0, Boat.At(w), "never rowed");
            float before = (DayClock.Day(w) + DayClock.Time(w)) * 24f;
            Assert.AreEqual(Boat.Hours, Boat.Row(w, Boat.AtRiver));
            Assert.AreEqual(Boat.AtRiver, Boat.At(w));
            Assert.IsTrue(w.Is(Boat.RowedFlag));
            Assert.AreEqual(Boat.Hours, (DayClock.Day(w) + DayClock.Time(w)) * 24f - before, 0.01f);
            Boat.Row(w, Boat.AtQuay);
            Assert.AreEqual(Boat.AtQuay, Boat.At(w), "the boat stays where she left it");
        }

        [Test]
        public void ARideNotAWay()
        {
            foreach (var l in WorldGraph.LinksOf("Windreach.DryRiver"))
                Assert.AreNotEqual("Saltmarrow.Quay", l.Other("Windreach.DryRiver"), "the map's ways are walked; the boat is Sable's to give");
        }
    }
}
