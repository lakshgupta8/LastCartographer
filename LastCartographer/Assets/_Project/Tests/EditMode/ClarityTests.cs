using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// Clarity as rules (PRG-18): its level grows with the story, the level sets how long she lasts untethered and how
    /// wide her lantern-radius is, the radius narrows as the meter runs down, and the Greyfold and the Blank are drawn
    /// round her lantern but for the last places colour reaches by itself.
    /// </summary>
    public class ClarityTests
    {
        [SetUp] public void SetUp() { GameState.NewGame(); }
        [TearDown] public void TearDown() { GameState.NewGame(); }

        [Test]
        public void ItGrowsWithTheStory()
        {
            var w = GameState.World;
            Assert.AreEqual(0, Clarity.Level(false, w), "not learned yet");
            w.Set(Clarity.BellsFlag, true);
            Assert.AreEqual(0, Clarity.Level(false, w), "the bells grow only what she has");
            w.Set(Clarity.BellsFlag, false);
            Assert.AreEqual(1, Clarity.Level(true, w), "learned where the road stops");
            w.Set(Clarity.BellsFlag, true);
            Assert.AreEqual(2, Clarity.Level(true, w), "the bells: Clarity grows (6.12)");
            w.Set(Clarity.Act3Flag, true);
            Assert.AreEqual(3, Clarity.Level(true, w), "Isolde's atlas: grows in Act 3");
            Assert.AreEqual("boss.bells.defeated", Clarity.BellsFlag);
            Assert.AreEqual(Bosses.FlagKey("bells"), Clarity.BellsFlag);
        }

        [Test]
        public void EachLevelLastsLongerAndSeesFurther()
        {
            Assert.AreEqual(0f, Clarity.Capacity(0), "without Clarity the white gives her back at once: the gate");
            for (int l = 1; l <= Clarity.MaxLevel; l++)
            {
                Assert.Greater(Clarity.Capacity(l), Clarity.Capacity(l - 1), "level " + l + " lasts longer");
                Assert.Greater(Clarity.FullRadius(l), Clarity.FullRadius(l - 1), "level " + l + " sees further");
            }
            Assert.AreEqual(6f, Clarity.FullRadius(2), 0.01f, "about six units in the Greyfold once the bells are silent (DES-11)");
            Assert.AreEqual(7f, Clarity.FullRadius(Clarity.MaxLevel), 0.01f, "as wide as the bells' nave at its fullest");
            Assert.Greater(Clarity.FullRadius(0), 3f, "without Clarity she still sees the next cobble");
        }

        [Test]
        public void TheRadiusIsTheMeter()
        {
            for (int l = 0; l <= Clarity.MaxLevel; l++)
            {
                Assert.AreEqual(Clarity.FullRadius(l), Clarity.Radius(l, 1f), 1e-4f, "full");
                Assert.AreEqual(Clarity.FullRadius(l), Clarity.Radius(l, Clarity.NarrowsBelow), 1e-4f, "full down to half");
                Assert.AreEqual(Clarity.MinRadius, Clarity.Radius(l, 0f), 1e-4f, "as small as the bells make it, at empty");
                float prev = Clarity.MinRadius;
                for (float f = 0.05f; f <= Clarity.NarrowsBelow; f += 0.05f)
                {
                    float r = Clarity.Radius(l, f);
                    Assert.GreaterOrEqual(r, prev - 1e-4f, "it narrows steadily as the meter runs down");
                    prev = r;
                }
            }
        }

        [Test]
        public void TheGreyfoldAndTheBlankAreDrawnRoundHerLantern()
        {
            foreach (var id in new[] { "Greyfold_Cathedral_2", "Greyfold_Road_1", "Greyfold_Road_3", "Greyfold_Pool_1", "Greyfold_Threshold_1", "Blank_Hollow_1", "Blank_Capital_2", "Blank_Aury_2" })
                Assert.IsTrue(Clarity.IsLanternLit(id), id);
            foreach (var id in new[] { "Greyfold_EdgeCamp_1", "Greyfold_EdgeCamp_2", "Greyfold_Edge", "Windreach_Gate_2", "Halden_Bastion_1", "Greybox_Saltmarrow_A", null, "" })
                Assert.IsFalse(Clarity.IsLanternLit(id), id ?? "null");

            // Runtime rooms answer for the rooms they stand in for.
            Assert.IsTrue(Clarity.IsLanternLit(Islands.SceneOfPlace("aldermere")), "an island");
            Assert.IsTrue(Clarity.IsLanternLit(Gauntlets.SceneFor("road_that_stops")), "the Road That Stops");
            Assert.IsFalse(Clarity.IsLanternLit(Gauntlets.SceneFor("updrafts")), "the Wind Gate");
            Assert.IsFalse(Clarity.IsLanternLit(Gauntlets.SceneFor("lamp_posts")), "a gauntlet with no room");
            Assert.IsTrue(Clarity.IsLanternLit("Arena_Greyfold_Cathedral_2"), "the bells' nave");
            Assert.IsFalse(Clarity.IsLanternLit("Arena_Saltmarrow_Nope"), "no such room");

            // Every planned room of the two regions, by the rule.
            foreach (var r in RoomPlans.InRegion(Region.Blank)) Assert.IsTrue(Clarity.IsLanternLit(r.Id), r.Id);
            foreach (var r in RoomPlans.InRegion(Region.Greyfold))
                Assert.AreEqual(r.Zone != "Greyfold.EdgeCamp" && r.Id != "Greyfold_Edge", Clarity.IsLanternLit(r.Id), r.Id);
        }

        [Test]
        public void OnlyTheDriftIsUntetheredWallToWall()
        {
            Assert.IsNotNull(RoomPlans.Find(Clarity.DriftRoom), "the drift is a planned room");
            Assert.AreEqual(Islands.DriftEntryScene, WorldGraph.GreyboxPrefix + Clarity.DriftRoom, "the islands' drift begins there, in the built room (ENV-08)");
            foreach (var r in RoomPlans.All)
                Assert.AreEqual(r.Id == Clarity.DriftRoom, Clarity.IsUntetheredRoom(r.Id), r.Id);
        }
    }
}
