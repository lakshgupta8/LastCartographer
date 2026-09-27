using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The regions on paper (DES-09 to DES-11): every planned room agrees with the macro map, the boss sheets, the cast,
    /// and itself. Saltmarrow is built and planned in its own spec; everything else is here.
    /// </summary>
    public class RoomPlanTests
    {
        static readonly Region[] Planned = { Region.Emberdown, Region.Verdance, Region.Halden, Region.Windreach, Region.Greyfold, Region.Blank };
        const Ability Climbs = Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread;

        [SetUp]
        public void SetUp() { RoomPlans.Reset(); RoomPlans.EnsureDefaults(); Bosses.Reset(); Cast.Reset(); }

        static IEnumerable<Zone> PlannedZones => Planned.SelectMany(WorldGraph.ZonesOf);

        [Test]
        public void EveryZoneHasTheMapsRoomsAndVantages()
        {
            Assert.AreEqual(96, RoomPlans.All.Count, "twenty-one, nineteen, twenty-one; Windreach fourteen, the Greyfold twelve, the Blank's nine fixed");
            Assert.AreEqual(123, RoomPlans.All.Count + WorldGraph.ZonesOf(Region.Saltmarrow).Sum(z => z.Rooms), "with Saltmarrow's, the whole map");
            foreach (var z in PlannedZones)
            {
                var rooms = RoomPlans.InZone(z.Id);
                Assert.AreEqual(z.Rooms, rooms.Count, z.Id + " rooms");
                Assert.AreEqual(z.Vantages, rooms.Count(r => r.Vantage != null), z.Id + " vantages");
            }
            var ids = new HashSet<string>();
            foreach (var r in RoomPlans.All)
            {
                Assert.IsTrue(ids.Add(r.Id), "duplicate " + r.Id);
                Assert.IsNotNull(WorldGraph.Find(r.Zone), r.Id + "'s zone " + r.Zone);
                Assert.IsTrue(r.Id.StartsWith(r.Zone.Split('.')[0] + "_"), r.Id + " is named for its region");
                Assert.IsNotEmpty(r.Name); Assert.IsNotEmpty(r.Purpose, r.Id + " says what it is for");
                Assert.IsNotEmpty(r.Exits, r.Id + " has a way out");
            }
            foreach (var z in PlannedZones.Where(z => z.IsHub))
                Assert.IsTrue(RoomPlans.InZone(z.Id).Any(r => r.Desk), z.Id + ", a hub, has a desk");
        }

        [Test]
        public void ExitsPairAndGatesBetweenZonesAreTheMaps()
        {
            foreach (var r in RoomPlans.All)
            {
                var zone = WorldGraph.Find(r.Zone);
                foreach (var e in r.Exits)
                {
                    if (!e.IsExternal)
                    {
                        var to = RoomPlans.Find(e.To);
                        Assert.IsNotNull(to, r.Id + " → " + e.To + " exists");
                        var back = to.Exits.Where(b => b.To == r.Id).ToList();
                        Assert.AreEqual(1, back.Count, e.To + " leads back to " + r.Id);
                        Assert.AreEqual(RoomPlans.Opposite(e.Side), back[0].Side, r.Id + " " + e.Side + " pairs with " + e.To + " " + back[0].Side);
                        Assert.AreEqual(e.Needs, back[0].Needs, "a gate applies both ways: " + r.Id + " / " + e.To);
                        Assert.AreEqual(e.Flag, back[0].Flag);
                    }
                    else Assert.IsNotNull(WorldGraph.Find(e.To), r.Id + " leads out to a zone on the map: " + e.To);

                    var otherZone = RoomPlans.ZoneOf(e.To);
                    if (otherZone == r.Zone)
                    {
                        Assert.IsNull(e.Flag, "no story flag inside a zone: " + r.Id);
                        Assert.IsFalse(e.Soft);
                        Assert.IsTrue(e.Needs == Ability.None || e.Needs == zone.Grants,
                            "inside a zone only its own ability gates a room: " + r.Id + " → " + e.To + " needs " + e.Needs);
                        continue;
                    }
                    var link = WorldGraph.Links.FirstOrDefault(l => l.Joins(r.Zone) && l.Other(r.Zone) == otherZone);
                    Assert.IsNotNull(link, r.Id + " → " + e.To + ": the map joins " + r.Zone + " and " + otherZone);
                    Assert.AreEqual(link.Needs, e.Needs, "the map's gate: " + r.Id + " → " + e.To);
                    Assert.AreEqual(link.Flag, e.Flag, "the map's flag: " + r.Id + " → " + e.To);
                    Assert.AreEqual(link.Soft, e.Soft, "the map's softness: " + r.Id + " → " + e.To);
                }
            }
            // Every way the map draws into, out of, or within these regions has a door.
            var planned = new HashSet<string>(PlannedZones.Select(z => z.Id));
            foreach (var l in WorldGraph.Links.Where(l => planned.Contains(l.From) || planned.Contains(l.To)))
            {
                bool realised = RoomPlans.All.Any(r => r.Exits.Any(e =>
                    (r.Zone == l.From && RoomPlans.ZoneOf(e.To) == l.To) || (r.Zone == l.To && RoomPlans.ZoneOf(e.To) == l.From)));
                Assert.IsTrue(realised, "a door for the map's way " + l.From + " – " + l.To);
            }
        }

        [Test]
        public void TheRoomGraphReachesWhatTheZoneGraphDoes()
        {
            var planned = new HashSet<string>(PlannedZones.Select(z => z.Id));
            void Check(Ability have, System.Func<string, bool> flags, bool soft, string what)
            {
                var zones = WorldGraph.Reachable(have, flags, soft).Where(planned.Contains).OrderBy(z => z).ToList();
                var rooms = RoomPlans.ReachableRooms(have, flags, soft);
                var viaRooms = rooms.Select(id => RoomPlans.Find(id).Zone).Distinct().OrderBy(z => z).ToList();
                CollectionAssert.AreEqual(zones, viaRooms, what);
            }
            Check(Ability.None, null, false, "nothing: the coast only");
            Check(Ability.None, null, true, "a pogo over the soft gaps");
            Check(Ability.Wingbeat, null, false, "Wingbeat: both hubs");
            Check(Ability.Wingbeat | Ability.Talonhold, null, false, "the Emberdown climb");
            Check(Ability.Wingbeat | Ability.Talonhold, f => f == "emberdown.hollowvein_opened", false, "and the mine opened");
            Check(Ability.Wingbeat | Ability.Inkthread, null, false, "the Verdance climb");
            Check(Ability.Wingbeat | Ability.Talonhold, f => f == "isolde.cache", false, "Act 1's end");
            Check(Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread, f => f == "isolde.cache" || f == "act2.started", false, "Act 2");
            Check(Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread, f => f != "act3.started", false, "Act 2, everything but the dome");
            Check(Climbs | Ability.Clarity, f => f == "isolde.cache" || f == "act2.started", false, "Act 2 with Clarity");
            Check(Climbs | Ability.Windmemory, f => f == "act2.started", false, "the glide, without Clarity");
            Check(Climbs | Ability.Windmemory | Ability.Clarity, f => f != "act3.started", false, "the climax");
            Check(Ability.None, f => f == "saltmarrow.tether", false, "Aury's island by tether");
            Check(Climbs | Ability.Windmemory | Ability.Clarity, f => true, false, "Act 3");

            // Halden: either climb reaches the Hall; the towers want both; the Vault is Act 2; the dome is Act 3.
            var ember = RoomPlans.ReachableRooms(Ability.Wingbeat | Ability.Talonhold);
            var verd = RoomPlans.ReachableRooms(Ability.Wingbeat | Ability.Inkthread);
            Assert.IsTrue(ember.Contains("Halden_Hall_2") && verd.Contains("Halden_Hall_2"), "either climb reaches the Hall");
            Assert.IsTrue(ember.Contains("Halden_Orchard_2") && verd.Contains("Halden_Orchard_2"), "and the orchard's cache");
            Assert.IsFalse(ember.Contains("Halden_Bastion_1") || verd.Contains("Halden_Bastion_1"), "the flyer-tower wants both");
            var both = RoomPlans.ReachableRooms(Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread, f => f == "halden.vault_opened");
            Assert.IsTrue(both.Contains("Halden_Bastion_3") && both.Contains("Halden_Vault_1"), "the Guildmaster's window and the Vault, in Act 2");
            Assert.IsFalse(both.Contains("Halden_Observatory_1"), "the dome waits for Act 3");

            var none = RoomPlans.ReachableRooms(Ability.None);
            Assert.IsEmpty(none, "without Wingbeat or a pogo the climbs are closed");
            var wing = RoomPlans.ReachableRooms(Ability.Wingbeat);
            Assert.IsTrue(wing.Contains("Emberdown_Chimneys_1"), "Runa's chimney foot, where Talonhold is learned");
            Assert.IsFalse(wing.Contains("Emberdown_Chimneys_2"), "and the next room needs it");
            Assert.IsTrue(wing.Contains("Verdance_Chapel_2"), "Teodor's chapel, where Inkthread is learned");
            Assert.IsFalse(wing.Contains("Verdance_Grove_1"));
            Assert.IsTrue(wing.Contains("Verdance_Aldermere_2"), "the last day happens whether she has the thread or not");
            Assert.IsFalse(wing.Contains("Emberdown_Hollow_1"));

            var all = RoomPlans.ReachableRooms(Climbs | Ability.Windmemory | Ability.Clarity, f => true);
            Assert.IsTrue(all.Contains("Halden_Observatory_2"));
            CollectionAssert.AreEquivalent(RoomPlans.All.Select(r => r.Id), all, "every room, in the end");
        }

        [Test]
        public void TheLateRegionsOpenInTheStorysOrder()
        {
            // Act 1's end: the orchard road to the Edge, and the road in as far as it stops.
            var act1 = RoomPlans.ReachableRooms(Ability.Wingbeat | Ability.Talonhold, f => f == "isolde.cache");
            Assert.IsTrue(act1.Contains("Greyfold_Edge") && act1.Contains("Greyfold_Road_3"), "back to the Edge, and in, where Clarity is learned");
            Assert.IsFalse(act1.Contains("Greyfold_Pool_1"), "no further without it");
            Assert.IsFalse(act1.Contains("Windreach_Stones_1"), "Windreach is Act 2");

            // Act 2: the Steppe to the Wind Gate's leap; beyond it, the glide.
            var act2 = RoomPlans.ReachableRooms(Climbs | Ability.Clarity, f => f == "isolde.cache" || f == "act2.started");
            Assert.IsTrue(act2.Contains("Windreach_Stones_3") && act2.Contains("Windreach_Camp_2") && act2.Contains("Windreach_Gate_1"), "the stones, the camp, the leap");
            Assert.IsFalse(act2.Contains("Windreach_Gate_2") || act2.Contains("Windreach_Fire_2"), "the updrafts and the keystone want Windmemory");
            Assert.IsTrue(act2.Contains("Greyfold_Pool_2"), "Clarity reaches the Mirror Pool");
            Assert.IsFalse(act2.Contains("Greyfold_Threshold_1"), "the Threshold waits for the climax");
            var glide = RoomPlans.ReachableRooms(Climbs | Ability.Windmemory, f => f == "act2.started");
            Assert.IsTrue(glide.Contains("Windreach_Star_2") && glide.Contains("Greyfold_Pool_2"), "the Fallen Star, and the glide into the white");
            Assert.IsFalse(glide.Contains("Greyfold_Road_3"), "but without Clarity the white road back is shut");

            // The climax, the crossing, Act 3.
            bool Upto(string f, string last) { var order = new[] { "act2.threshold", "greyfold.crossed", "act3.started" }; return System.Array.IndexOf(order, f) >= 0 ? System.Array.IndexOf(order, f) <= System.Array.IndexOf(order, last) : true; }
            var kit = Climbs | Ability.Windmemory | Ability.Clarity;
            var climax = RoomPlans.ReachableRooms(kit, f => Upto(f, "act2.threshold"));
            Assert.IsTrue(climax.Contains("Greyfold_Threshold_2"));
            Assert.IsFalse(climax.Contains("Greyfold_LastCamp_1"), "Isolde's camp is across the line, not beside the Edge");
            var crossed = RoomPlans.ReachableRooms(kit, f => Upto(f, "greyfold.crossed"));
            Assert.IsTrue(crossed.Contains("Greyfold_LastCamp_1"));
            Assert.IsFalse(crossed.Contains("Blank_Hollow_1") || crossed.Contains("Halden_Observatory_1"), "her atlas starts Act 3");
            var act3 = RoomPlans.ReachableRooms(kit, f => Upto(f, "act3.started"));
            Assert.IsTrue(act3.Contains("Blank_Hollow_2") && act3.Contains("Blank_Capital_4") && act3.Contains("Blank_Aury_2"));

            // Aury's lighthouse, by tether in Act 2: his island, and no further until she is inside.
            var tether = RoomPlans.ReachableRooms(kit, f => f == "saltmarrow.tether" || f == "isolde.cache" || f == "act2.started");
            Assert.IsTrue(tether.Contains("Blank_Aury_2"));
            Assert.IsFalse(tether.Contains("Blank_Hollow_3"), "Clarity alone does not drift his island to the Hollow in Act 2");

            // The Greyfold is drawn round her lantern; no room in the Blank is surveyed.
            Assert.IsTrue(RoomPlans.InRegion(Region.Blank).All(r => r.Vantage == null), "the Blank has no vantages");
            Assert.AreEqual("Greyfold_Edge/HalfCathedral", RoomPlans.Find("Greyfold_Edge").VantageId, "the built prologue room and its plan are the same room");
            Assert.IsNotNull(Atlas.FindVantage("Greyfold_Edge/HalfCathedral"));
        }

        [Test]
        public void EveryBossHasAnArenaWithADeskAtTheDoor()
        {
            foreach (var sheet in Bosses.All.Where(b => Planned.Any(p => b.Zone.StartsWith(p + "."))))
            {
                var arenas = RoomPlans.All.Where(r => r.Arena == sheet.Id).ToList();
                Assert.AreEqual(1, arenas.Count, sheet.Id + " has one arena");
                var arena = arenas[0];
                Assert.AreEqual(sheet.Zone, arena.Zone, sheet.Id + " is fought where its sheet says");
                bool desk = arena.Desk || arena.Exits.Any(e => !e.IsExternal && RoomPlans.Find(e.To).Desk);
                Assert.IsTrue(desk, sheet.Id + ": a desk within fifteen seconds of the door");
            }
            foreach (var r in RoomPlans.All.Where(r => r.Arena != null))
                Assert.IsNotNull(Bosses.Find(r.Arena), r.Id + "'s arena is a boss with a sheet");
        }

        [Test]
        public void TheCastStandsInTheirZones()
        {
            var planned = new HashSet<string>(PlannedZones.Select(z => z.Id));
            foreach (var a in Cast.Appearances.Where(a => planned.Contains(a.Zone)))
                Assert.IsTrue(RoomPlans.InZone(a.Zone).Any(r => r.Npcs.Contains(a.Character)),
                    a.Character + " (" + a.Scene + ") stands somewhere in " + a.Zone);
            foreach (var r in RoomPlans.All)
                foreach (var n in r.Npcs) Assert.IsNotNull(Cast.Find(n), r.Id + ": " + n + " is in the cast");
            // The walks the bounds-walk spec designs have rooms.
            Assert.AreEqual(1, RoomPlans.All.Count(r => r.Walk == "kettils_rest"));
            Assert.AreEqual(4, RoomPlans.All.Count(r => r.Walk == "hollowvein"), "the long roll-call is walked four rooms down");
            Assert.IsTrue(RoomPlans.All.Where(r => r.Walk == "hollowvein").All(r => r.Zone == "Emberdown.Hollowvein"));
        }
    }
}
