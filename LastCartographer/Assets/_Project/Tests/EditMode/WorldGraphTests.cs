using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The macro map (DES-07): the spine holds, sequence breaks are only the soft ones, the scope targets add up.</summary>
    public class WorldGraphTests
    {
        static bool NoFlags(string f) => false;
        static bool AllFlags(string f) => true;

        [Test]
        public void TheSpineHoldsFromTheShore()
        {
            var none = WorldGraph.Reachable(Ability.None, NoFlags);
            CollectionAssert.AreEquivalent(new[]
            {
                "Saltmarrow.Shore", "Saltmarrow.Quay", "Saltmarrow.Reedmother", "Saltmarrow.IrisFields", "Saltmarrow.MerrowsEnd", "Saltmarrow.LanternChain",
            }, none, "without Wingbeat the coast ends at the Lamp-Keeper");

            var wing = WorldGraph.Reachable(Ability.Wingbeat, NoFlags);
            CollectionAssert.IsSubsetOf(new[] { "Saltmarrow.SaltChapel", "Saltmarrow.BoneBridge", "Emberdown.KettilsRest", "Verdance.QuietHouse", "Verdance.Aldermere" }, wing, "Wingbeat opens both climbs to their hubs");
            CollectionAssert.IsNotSubsetOf(new[] { "Halden.JourneymansHall" }, wing, "the Plateau needs a climb finished");
            CollectionAssert.IsNotSubsetOf(new[] { "Emberdown.CinderBaths" }, wing, "the chimneys need Talonhold");
            CollectionAssert.IsNotSubsetOf(new[] { "Verdance.LanternGrove" }, wing, "the grove needs Inkthread");

            var viaEmber = WorldGraph.Reachable(Ability.Wingbeat | Ability.Talonhold, NoFlags);
            var viaVerd = WorldGraph.Reachable(Ability.Wingbeat | Ability.Inkthread, NoFlags);
            Assert.IsTrue(viaEmber.Contains("Halden.JourneymansHall"), "Emberdown reaches the Plateau");
            Assert.IsTrue(viaVerd.Contains("Halden.JourneymansHall"), "the Verdance reaches the Plateau");
            Assert.IsFalse(viaEmber.Contains("Halden.Bastion") || viaVerd.Contains("Halden.Bastion"), "the flyer-towers want both climbs' abilities");
            Assert.IsFalse(viaEmber.Contains("Greyfold.EdgeCamp"), "the Edge waits for Isolde's cache");

            var act1End = WorldGraph.Reachable(Ability.Wingbeat | Ability.Talonhold, f => f == "isolde.cache");
            Assert.IsTrue(act1End.Contains("Greyfold.RoadThatStops"), "Act 1 ends stepping in");
            Assert.IsFalse(act1End.Contains("Greyfold.MirrorPool"), "no further without Clarity");
            Assert.IsFalse(act1End.Contains("Windreach.NineStones"), "Windreach is Act 2");

            var act2 = WorldGraph.Reachable(Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread | Ability.Clarity, f => f == "isolde.cache" || f == "act2.started");
            Assert.IsTrue(act2.Contains("Windreach.WindGate"));
            Assert.IsFalse(act2.Contains("Windreach.IdrennesFire"), "the keystone is past the Wind Gate's ceremony");
            Assert.IsFalse(act2.Contains("Greyfold.Threshold"), "the Threshold is the climax");
            Assert.IsFalse(act2.Contains("Greyfold.IsoldesLastCamp"), "her camp is across the line (DES-11)");
            var tether = WorldGraph.Reachable(Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread | Ability.Clarity, f => f == "saltmarrow.tether" || f == "isolde.cache");
            Assert.IsTrue(tether.Contains("Blank.AurysLighthouse"));
            Assert.IsFalse(tether.Contains("Blank.ThessalyHollow"), "Aury's island is no back door into Act 3");
        }

        [Test]
        public void EverythingIsReachableInTheEnd()
        {
            var all = WorldGraph.Reachable(Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread | Ability.Windmemory | Ability.Clarity | Ability.Sky, AllFlags);
            var missing = WorldGraph.Zones.Select(z => z.Id).Where(id => !all.Contains(id)).ToList();
            CollectionAssert.IsEmpty(missing, "unreachable zones");
            foreach (var l in WorldGraph.Links)
            {
                Assert.IsNotNull(WorldGraph.Find(l.From), "link from a zone that exists: " + l.From);
                Assert.IsNotNull(WorldGraph.Find(l.To), "link to a zone that exists: " + l.To);
            }
        }

        [Test]
        public void SequenceBreaksAreOnlyTheSoftGates()
        {
            var pogo = WorldGraph.Reachable(Ability.None, NoFlags, allowSoft: true);
            Assert.IsTrue(pogo.Contains("Saltmarrow.SaltChapel"), "a skilled pogo crosses the lighthouse gap");
            Assert.IsTrue(pogo.Contains("Emberdown.KettilsRest"), "and climbs to Kettil's without Wingbeat");
            Assert.IsTrue(pogo.Contains("Verdance.QuietHouse"));
            Assert.IsFalse(pogo.Contains("Emberdown.CinderBaths"), "Talonhold shafts are hard");
            Assert.IsFalse(pogo.Contains("Verdance.LanternGrove"), "Inkthread anchors are hard");
            Assert.IsFalse(pogo.Contains("Halden.SevenBridges"), "the Plateau is hard-gated either way");
            foreach (var l in WorldGraph.Links)
            {
                if (!l.Soft) continue;
                Assert.AreEqual(Ability.Wingbeat, l.Needs, "only Wingbeat gaps are soft: " + l.From + " to " + l.To);
                Assert.IsNull(l.Flag, "a story gate is never soft: " + l.From + " to " + l.To);
            }
        }

        [Test]
        public void TheScopeTargetsAddUp()
        {
            Assert.AreEqual(7, System.Enum.GetValues(typeof(Region)).Length, "six regions and the Blank");
            foreach (Region r in System.Enum.GetValues(typeof(Region)))
                Assert.AreEqual(1, WorldGraph.ZonesOf(r).Count(z => z.IsHub), "one hub in " + r);
            Assert.That(WorldGraph.SubZoneCount(), Is.InRange(32, 38), "about 34 sub-zones");
            Assert.That(WorldGraph.VantageCount(), Is.InRange(44, 52), "about 48 vantage points");
            Assert.That(WorldGraph.RoomCount(), Is.InRange(100, 140), "rooms");
            var grants = WorldGraph.Zones.Where(z => z.Grants != Ability.None).Select(z => z.Grants).ToList();
            CollectionAssert.AreEquivalent(new[] { Ability.Wingbeat, Ability.Talonhold, Ability.Inkthread, Ability.Windmemory, Ability.Clarity }, grants, "each ability lives in exactly one zone; the Sky is the ending's");
            Assert.AreEqual(7, WorldGraph.Zones.Count(z => z.Keystone), "seven keystones, seven homes: the seventh is the one Isolde carries (NAR-13)");
            var ids = new HashSet<string>();
            foreach (var z in WorldGraph.Zones) Assert.IsTrue(ids.Add(z.Id), "duplicate zone " + z.Id);
        }

        [Test]
        public void TheGreyboxRoomsSitOnTheMap()
        {
            foreach (var place in Atlas.AllPlaces)
            {
                var zone = WorldGraph.ZoneOfPlace(place.Id);
                Assert.IsNotNull(zone, place.Id + " is on the map");
                Assert.IsNotNull(WorldGraph.Find(zone), zone + " exists");
            }
            Assert.AreEqual("Saltmarrow.Quay", WorldGraph.ZoneOfPlace("Saltmarrow_A"));
            Assert.IsTrue(WorldGraph.Find("Saltmarrow.Quay").IsHub);
            Assert.IsNull(WorldGraph.ZoneOfPlace("Nowhere"));
            Assert.AreEqual("Greybox_Saltmarrow_A", WorldGraph.BuiltRoomScene("Saltmarrow.Quay"), "the quay's first built room");
            Assert.AreEqual("Greybox_Halden_Hall_2", WorldGraph.BuiltRoomScene("Halden.JourneymansHall"), "the Hall is built (ENV-05): the room Pell stands in");
            Assert.AreEqual("Greybox_Windreach_Camp_2", WorldGraph.BuiltRoomScene("Windreach.LongGrassCamp"), "the camp is built (ENV-07): the fire ring Idrenne stands at");
            Assert.IsNull(WorldGraph.BuiltRoomScene("Greyfold.EdgeCamp"), "the Edge Camp is not built");
            CollectionAssert.AreEqual(new[] { "Saltmarrow_A", "Saltmarrow_Boardwalk", "Saltmarrow_Stilts" }, WorldGraph.PlacesOf("Saltmarrow.Quay"));
        }
    }
}
