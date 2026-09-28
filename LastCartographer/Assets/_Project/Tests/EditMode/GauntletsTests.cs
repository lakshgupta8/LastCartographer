using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The gauntlets as data (CMB-18, combat doc §9): one per region but the Blank, each on the map, each around its ability.</summary>
    public class GauntletsTests
    {
        [Test]
        public void OnePerRegionButTheBlank()
        {
            var all = Gauntlets.All;
            Assert.AreEqual(6, all.Count);
            CollectionAssert.AreEquivalent(
                new[] { Region.Saltmarrow, Region.Emberdown, Region.Verdance, Region.Halden, Region.Windreach, Region.Greyfold },
                all.Select(g => g.Region).ToArray(), "the Blank's traversal is the drift");
            Assert.AreEqual(all.Count, all.Select(g => g.Id).Distinct().Count());
        }

        [Test]
        public void EachStandsOnTheMapAroundItsAbility()
        {
            foreach (var g in Gauntlets.All)
            {
                var zone = WorldGraph.Find(g.Zone);
                Assert.IsNotNull(zone, g.Id + "'s zone " + g.Zone + " is on the map");
                StringAssert.StartsWith(g.Region + ".", g.Zone, g.Id + " is in its region");
                Assert.AreNotEqual(Ability.None, g.Needs, g.Id + " is built around an ability");
                if (g.Room != null)
                {
                    var room = RoomPlans.Find(g.Room);
                    Assert.IsNotNull(room, g.Id + "'s planned room " + g.Room);
                    Assert.AreEqual(g.Zone, room.Zone, g.Id + "'s room is in its zone");
                }
                Assert.IsTrue(Gauntlets.IsGauntletScene(Gauntlets.SceneFor(g.Id)));
                Assert.AreEqual("gauntlet." + g.Id + ".done", g.FlagKey);
            }
        }

        [Test]
        public void TheAbilitiesAreTheCombatDocs()
        {
            Assert.AreEqual(Ability.Wingbeat, Gauntlets.Find("lamp_posts").Needs, "the Lantern Chain jumps");
            Assert.AreEqual(Ability.Talonhold, Gauntlets.Find("furnace_shafts").Needs, "the Furnace Stair shafts");
            Assert.AreEqual(Ability.Inkthread, Gauntlets.Find("canopy_threads").Needs, "the canopy threads");
            Assert.AreEqual(Ability.Windmemory, Gauntlets.Find("updrafts").Needs, "the Windreach updrafts");
            Assert.AreEqual(Ability.Clarity, Gauntlets.Find("road_that_stops").Needs, "the Road That Stops");
            Assert.AreEqual(Ability.Talonhold | Ability.Inkthread, Gauntlets.Find("flyer_tower").Needs, "no stairs; a gap");
        }
    }
}
