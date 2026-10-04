using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Sable's boat north (character-bibles.md §2, Act 2; <see cref="Boat"/>): in Act 2 she says there is a short way to the
    /// steppe, rows Wren from the quay to the Dry River, stands by the boat among the Ferrymen's hulls, counts the ones
    /// that never came back, and rows her home when asked; the boat stays where Wren left it.
    /// </summary>
    public class SableRowsNorthTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static FlagPresence SableByTheBoat()
        {
            var talker = Object.FindObjectsByType<NpcTalker>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.StartNode == "River_Sable");
            Assert.IsNotNull(talker, "Sable stands in the riverbed");
            var presence = talker!.GetComponent<FlagPresence>();
            Assert.IsNotNull(presence, "while the boat is there");
            return presence;
        }

        [UnityTest]
        public IEnumerator SheRowsNorthCountsTheHullsAndRowsHome()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            w.Set("saltmarrow.sable.talked", 1); w.Set("saltmarrow.sable.ledger", 1);

            yield return _replay.Talk("Sable", "Quay_Sable", new[] { 2 });   // Act 1: nothing today
            Assert.IsFalse(w.Is(Boat.OfferedFlag), "not before Act 2");

            w.Set("act2.started", 1);
            float before = (DayClock.Day(w) + DayClock.Time(w)) * 24f;
            yield return _replay.Talk("Sable", "Quay_Sable", new[] { 0, 1 });   // what are you selling? the boat north
            Assert.IsTrue(w.Is(Boat.OfferedFlag) && w.Is(Boat.RowedFlag));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Up the Dry River")), "she says there is a short way");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("No price")), "and names what has none");
            Assert.AreEqual(Boat.AtRiver, Boat.At(w));
            Assert.AreEqual(WorldGraph.GreyboxPrefix + Boat.RiverRoom, RoomManager.Instance.CurrentRoom, "the boat lands in the riverbed");
            float after = (DayClock.Day(w) + DayClock.Time(w)) * 24f;
            Assert.AreEqual(Boat.Hours, after - before, 0.1f, "most of a day on the water");

            var sable = SableByTheBoat();
            Assert.IsTrue(sable.Standing, "she waits by the boat");
            yield return _replay.Talk("Sable", "River_Sable", new[] { 1 });   // whose was the blue one?
            Assert.IsTrue(w.Is(Boat.HullsFlag));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Eleven out, none back")), "she counts who did not come back, aloud");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("My father's")));

            yield return _replay.Talk("Sable", "River_Sable", new[] { 0 });   // row me back to the quay
            Assert.AreEqual(Boat.AtQuay, Boat.At(w));
            Assert.AreEqual(WorldGraph.GreyboxPrefix + Boat.QuayRoom, RoomManager.Instance.CurrentRoom, "home");
        }

        [UnityTest]
        public IEnumerator TheBoatStaysWhereSheLeftIt()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            var go = new GameObject("Sable_River_Test");
            new GameObject("Drawing").transform.SetParent(go.transform, false);
            var sable = go.AddComponent<FlagPresence>();
            sable.Configure(Boat.AtKey, null, 0, Boat.AtRiver);   // as the riverbed's recipe stands her
            yield return null;
            Assert.IsFalse(sable.Standing, "never rowed: not on the steppe");
            Boat.Row(w, Boat.AtRiver);
            Assert.IsTrue(sable.Standing, "rowed north: she waits by the boat");
            Assert.IsTrue(w.Is(Boat.RowedFlag));
            w.Set("saltmarrow.sable.talked", 1); w.Set("saltmarrow.sable.ledger", 1); w.Set("act2.started", 1); w.Set(Boat.OfferedFlag, 1);
            yield return _replay.Talk("Sable", "Quay_Sable", new[] { 2 });   // Wren walked home; the quay's Sable does not row
            Assert.IsTrue(sable.Standing, "the boat is up the river, where she left it");
            Boat.Row(w, Boat.AtQuay);
            Assert.IsFalse(sable.Standing, "rowed home: gone from the riverbed");
            Object.Destroy(go);
        }
    }
}
