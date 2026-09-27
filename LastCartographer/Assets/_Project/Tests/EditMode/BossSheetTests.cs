using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The boss sheets as data (NAR-06): fifteen bosses, the twelve-word rule, the map names each fight, tiers and options as the bible has them.</summary>
    public class BossSheetTests
    {
        [SetUp]
        public void SetUp() { Bosses.Reset(); Bosses.EnsureDefaults(); }

        [Test]
        public void FifteenBossesSeventeenFights()
        {
            Assert.AreEqual(15, Bosses.Numbers.Count(), "bible §6 has fifteen");
            Assert.AreEqual(17, Bosses.All.Count, "Halvard is fought three times");
            Assert.AreEqual(3, Bosses.ByNumber("6.3").Count);
            var ids = new HashSet<string>();
            foreach (var b in Bosses.All) Assert.IsTrue(ids.Add(b.Id), "duplicate id " + b.Id);
            for (int i = 1; i <= 15; i++) Assert.IsNotEmpty(Bosses.ByNumber("6." + i), "6." + i + " has a sheet");
            CollectionAssert.AreEquivalent(new[] { "6.2", "6.6", "6.9", "6.10" }, Bosses.All.Where(b => b.Optional).Select(b => b.Number), "the optional four");
            CollectionAssert.AreEquivalent(new[] { "6.4", "6.10", "6.14" }, Bosses.All.Where(b => b.Keystone).Select(b => b.Number), "keystones taken from bosses: Hollowvein's, the Star's, the seventh");
        }

        [Test]
        public void EveryFightSpeaksThreeTimesUnderTwelveWords()
        {
            foreach (var b in Bosses.All)
            {
                Assert.IsNotEmpty(b.Reason, b.Id + " has a reason");
                Assert.IsNotEmpty(b.Arena, b.Id + " has an arena");
                Assert.IsNotEmpty(b.Aftermath, b.Id + " has an aftermath");
                var lines = b.Lines;
                Assert.AreEqual(3, lines.Length);
                foreach (var line in lines)
                {
                    Assert.IsNotEmpty(line, b.Id + " speaks three times");
                    Assert.That(Bosses.WordCount(line), Is.LessThanOrEqualTo(Bosses.MaxLineWords), b.Id + ": \"" + line + "\"");
                }
                Assert.AreEqual(3, lines.Distinct().Count(), b.Id + "'s lines differ");
            }
            Assert.AreEqual(3, Bosses.WordCount("The light stays."));
            Assert.AreEqual(0, Bosses.WordCount(""));
        }

        [Test]
        public void TheMapNamesEveryFight()
        {
            foreach (var b in Bosses.All)
            {
                var zone = WorldGraph.Find(b.Zone);
                Assert.IsNotNull(zone, b.Id + " at " + b.Zone + " is on the map");
                Assert.IsNotNull(zone.Boss, zone.Id + " notes a boss");
                StringAssert.Contains(b.Number + " ", zone.Boss + " ", zone.Id + " names " + b.Number);
                if (b.Grants != Ability.None && b.Grants != Ability.Sky)
                    Assert.AreEqual(b.Grants, zone.Grants, b.Id + " grants what its zone grants");
            }
            // And every boss note on the map has a sheet.
            foreach (var z in WorldGraph.Zones.Where(z => z.Boss != null))
                Assert.IsTrue(Bosses.All.Any(b => b.Zone == z.Id), z.Id + "'s boss has a sheet");
        }

        [Test]
        public void TiersRiseWithTheMap()
        {
            int Tier(string id) => Bosses.Find(id).Tier;
            Assert.AreEqual(1, Tier("lamp_keeper")); Assert.AreEqual(1, Tier("reedmother_brood")); Assert.AreEqual(1, Tier("halvard"));
            Assert.AreEqual(2, Tier("halvard_2")); Assert.AreEqual(3, Tier("halvard_3"));
            foreach (var id in new[] { "collapse", "brann", "choir", "gatekeeper" }) Assert.AreEqual(2, Tier(id), id);
            foreach (var id in new[] { "oriel", "hale", "fallen_star", "voss", "bells" }) Assert.AreEqual(3, Tier(id), id);
            foreach (var id in new[] { "corras_drawing", "archivist", "complete_survey" }) Assert.AreEqual(4, Tier(id), id);
            foreach (var b in Bosses.All) Assert.That(b.Tier, Is.InRange(1, 4));
            Assert.AreEqual("boss.lamp_keeper.defeated", Bosses.Find("lamp_keeper").FlagKey, "the flag the lighthouse and Sable already read");
            var w = new WorldState();
            Assert.IsFalse(Bosses.IsDefeated(w, "voss"));
            w.Set(Bosses.FlagKey("voss"), true);
            Assert.IsTrue(Bosses.IsDefeated(w, "voss"));
        }
    }
}
