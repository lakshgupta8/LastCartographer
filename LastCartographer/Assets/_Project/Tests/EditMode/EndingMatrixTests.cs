using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The matrix's second table (ending-matrix.md §2): from the origin where everything is met, what each decision closes.</summary>
    public class EndingMatrixTests
    {
        /// <summary>Every requirement at once: six stones carried, Corvin persuaded, five allies, both witnesses, the chair offered, nothing anchored.</summary>
        static WorldState Origin()
        {
            var w = new WorldState();
            foreach (var h in Keystones.Homes.Where(h => h != Keystones.InTheFrame)) w.Set(Keystones.FlagKey(h), true);
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinPersuaded);
            w.Set("sable.tether_sold", true); w.Set("runa.named_wren", true); w.Set("teodor.keystone_given", true);
            w.Set("windreach.camp.walked", true); w.Set("pell.report_kept", true);
            w.Set("windreach.fire.witnessed", true); w.Set("verdance.aldermere.attended", true); w.Set("emberdown.hollowvein.walked", true);
            w.Set(Endings.RestOfferedFlag, true);
            return w;
        }

        static string Open(WorldState w) => string.Join(",", new[] { Ending.Fixed, Ending.Open, Ending.Unwritten, Ending.Rest }.Where(e => Endings.IsOpen(w, e)));

        [Test]
        public void TheOriginOpensAllButTheOnesStancesAndStonesExclude()
        {
            var w = Origin();
            Assert.AreEqual("Open,Unwritten", Open(w), "persuaded is not cooperating; six stones are not zero");
            Assert.AreEqual(5, Endings.AlliedCount(w));
        }

        [Test]
        public void CorvinsStanceDecidesBetweenTheFixedAndTheOpen()
        {
            var w = Origin();
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinCooperates);
            Assert.AreEqual("Fixed,Unwritten", Open(w));
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinUnpersuaded);
            Assert.AreEqual("Unwritten", Open(w));
        }

        [Test]
        public void StoppingAldermereClosesTheUnwrittenAndTheOpenUnlessHollowveinWasWalked()
        {
            var w = Origin();
            w.Set("verdance.aldermere.attended", false); w.Set("verdance.aldermere.stopped", true);
            Assert.AreEqual("Open", Open(w), "Hollowvein was walked: the Open World stands");
            w.Set("emberdown.hollowvein.walked", false);
            Assert.AreEqual("", Open(w));
        }

        [Test]
        public void TheFireTeodorAndPellEachCloseWhatTheBibleSays()
        {
            var w = Origin();
            w.Set("windreach.fire.witnessed", false);
            Assert.AreEqual("Unwritten", Open(w), "skip the Fire: no Open World");

            w = Origin();
            w.Set("teodor.keystone_given", false); w.Set("keystone.quiet_house", false);
            Assert.AreEqual("Open", Open(w), "Teodor refuses: no Unwritten; five stones and four allies still open the world");
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinCooperates);
            Assert.AreEqual("", Open(w), "and six is not seven");

            w = Origin();
            w.Set("pell.report_kept", false); w.Set("pell.report_sent", true);
            Assert.AreEqual("Open,Unwritten", Open(w), "sending the report costs one ally of five");
            w.Set("windreach.hale.finished", true);
            Assert.AreEqual("Open,Unwritten", Open(w), "Hale's survey closes nothing at the frame");
        }

        [Test]
        public void TheRestIsClosedByAnyStoneOrAnchor()
        {
            var w = new WorldState();
            w.Set(Endings.RestOfferedFlag, true);
            Assert.AreEqual("Rest", Open(w));
            Places.Anchor(w, "Saltmarrow_A");
            Assert.AreEqual("", Open(w), "the quay's desk, anchored in Act 1, closes the Rest before she knows it exists");
            var v = new WorldState();
            v.Set(Endings.RestOfferedFlag, true);
            v.Set("keystone.aury", true);
            Assert.AreEqual("", Open(v));
        }

        [Test]
        public void EveryRouteIsDataTheMapAndTheSheetsAgreeWith()
        {
            foreach (Ending e in System.Enum.GetValues(typeof(Ending)))
            {
                if (e == Ending.None) { Assert.IsNull(EndingRoutes.For(e)); continue; }
                var route = EndingRoutes.For(e);
                Assert.IsNotNull(route, e + " has a route");
                Assert.Greater(route.Steps.Length, 10);
                foreach (var s in route.Steps)
                {
                    Assert.IsNotNull(WorldGraph.Find(s.Zone), e + ": " + s.Key + " at " + s.Zone + " is on the map");
                    Assert.IsNotEmpty(s.Note);
                    if (s.Kind == RouteStepKind.Boss)
                    {
                        var sheet = Bosses.Find(s.Key);
                        Assert.IsNotNull(sheet, e + ": " + s.Key + " has a sheet");
                        Assert.AreEqual(sheet.Zone, s.Zone); Assert.AreEqual(sheet.Grants, s.Grants);
                    }
                }
                var walk = Endings.EpilogueWalk(e);
                CollectionAssert.AreEqual(walk, route.Steps.Skip(route.Steps.Length - walk.Length).Select(s => s.Key).ToList(), e + " ends with its epilogue walk");
            }
        }
    }
}
