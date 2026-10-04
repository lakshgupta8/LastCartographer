using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The atlas's map (docs/design/atlas-map.md): every room is on one page, each in its own cell; the doors go both
    /// ways and mostly join neighbours on the grid in the door's direction (the coast's all of them); the ways off a
    /// page are marked; and the ink follows what she has done: the road in ink, a place with a view in pencil until
    /// surveyed, its neighbours pencilled by the vantage, an erased place rubbed out.
    /// </summary>
    public class AtlasMapTests
    {
        [Test]
        public void EveryRoomIsOnOnePageInACellOfItsOwn()
        {
            var all = AtlasMap.Pages.SelectMany(AtlasMap.Page).ToList();
            CollectionAssert.AllItemsAreUnique(all.Select(r => r.Id), "a room on one page");
            foreach (var plan in RoomPlans.All) Assert.IsNotNull(AtlasMap.Find(plan.Id), plan.Id + " is on its page");
            foreach (var place in Atlas.AllPlaces) Assert.IsNotNull(AtlasMap.Find(place.Id), place.Id + ", a place, is on the map");
            foreach (var page in AtlasMap.Pages)
            {
                var rooms = AtlasMap.Page(page);
                Assert.Greater(rooms.Count, 0, page + " has rooms");
                CollectionAssert.AllItemsAreUnique(rooms.Select(r => (r.X, r.Y)), page + ": no two rooms in one cell");
                foreach (var r in rooms) Assert.AreEqual(page, r.Page);
            }
        }

        static bool InLine(MapRoom a, Side s, MapRoom b)
        {
            int dx = b.X - a.X, dy = b.Y - a.Y;
            return s == Side.West ? dx == -1 && dy == 0 : s == Side.East ? dx == 1 && dy == 0 : s == Side.Up ? dx == 0 && dy == 1 : dx == 0 && dy == -1;
        }

        [Test]
        public void TheDoorsGoBothWaysAndJoinNeighbours()
        {
            foreach (var page in AtlasMap.Pages)
            {
                int doors = 0, inLine = 0;
                foreach (var r in AtlasMap.Page(page))
                    foreach (var (side, to) in r.Doors)
                    {
                        var other = AtlasMap.Find(to);
                        Assert.IsNotNull(other, r.Id + " → " + to);
                        CollectionAssert.Contains(other.Doors, (AtlasMap.Opposite(side), r.Id), to + " has the door back to " + r.Id);
                        doors++;
                        if (InLine(r, side, other)) inLine++;
                    }
                Assert.GreaterOrEqual(inLine, doors * 0.8f, page + ": most doors join neighbours on the grid (" + inLine + " of " + doors + ")");
            }
            // The coast is a line east, the roots climbing above the stilts.
            foreach (var r in AtlasMap.Page("The Saltmarrow"))
                foreach (var (side, to) in r.Doors) Assert.IsTrue(InLine(r, side, AtlasMap.Find(to)), r.Id + " " + side + " " + to);
            Assert.Greater(AtlasMap.Find("Saltmarrow_Roots_1").Y, AtlasMap.Find("Saltmarrow_Stilts").Y, "the roots are up the page");
        }

        [Test]
        public void TheWaysOffAPageAreMarked()
        {
            CollectionAssert.Contains(AtlasMap.Find("Saltmarrow_BoneBridge").Leaves.Select(l => l.side), Side.East, "the Bone Bridge leads on to Emberdown");
            CollectionAssert.Contains(AtlasMap.Find("Saltmarrow_IrisFields").Leaves.Select(l => l.side), Side.East, "the iris fields to the Verdance");
            foreach (var page in AtlasMap.Pages)
                Assert.IsTrue(AtlasMap.Page(page).Any(r => r.Leaves.Count > 0), page + " leads somewhere off its page");
            Assert.AreEqual("Emberdown", AtlasMap.PageOfRoom("Emberdown_Stair_3"));
            Assert.AreEqual("The Verdance", AtlasMap.PageOfRoom("Verdance_Aldermere_2"));
            Assert.AreEqual("The Saltmarrow", AtlasMap.PageOfRoom("Saltmarrow_B"));
            Assert.IsNull(AtlasMap.PageOfRoom("Island_Aldermere"), "an island is no page's");
        }

        [Test]
        public void TheInkFollowsWhatSheHasDone()
        {
            var w = new WorldState();
            foreach (var r in AtlasMap.Page("The Saltmarrow")) Assert.AreEqual(MapInk.Unknown, AtlasMap.InkOf(w, r.Id), r.Id + " is blank before she comes");

            AtlasMap.Walk(w, "Greybox_Saltmarrow_Stilts");
            Assert.IsTrue(AtlasMap.IsWalked(w, "Saltmarrow_Stilts"));
            Assert.AreEqual(MapInk.Drawn, AtlasMap.InkOf(w, "Saltmarrow_Stilts"), "nothing to survey: the road is in ink once walked");

            AtlasMap.Walk(w, "Saltmarrow_BoneBridge");
            Assert.AreEqual(MapInk.Walked, AtlasMap.InkOf(w, "Saltmarrow_BoneBridge"), "a place with a view is in pencil until surveyed");
            Assert.AreEqual(MapInk.Unknown, AtlasMap.InkOf(w, "Saltmarrow_Chapel"), "and its neighbour still blank");

            Assert.IsTrue(Atlas.Survey(w, "Saltmarrow_BoneBridge/Whale"));
            Assert.AreEqual(MapInk.Drawn, AtlasMap.InkOf(w, "Saltmarrow_BoneBridge"), "surveyed: in ink");
            Assert.AreEqual(MapInk.Seen, AtlasMap.InkOf(w, "Saltmarrow_Chapel"), "the vantage saw the chapel: in pencil, unwalked");

            Atlas.Erase(w, "Saltmarrow_BoneBridge");
            Assert.AreEqual(MapInk.Erased, AtlasMap.InkOf(w, "Saltmarrow_BoneBridge"), "erased: rubbed out");

            AtlasMap.Walk(w, "Island_Aldermere");
            Assert.IsFalse(w.Flags.Keys.Any(k => k.Contains("Island_")), "an island is not walked onto any page");
        }
    }
}
