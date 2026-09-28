using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// Flavour text (NAR-17, docs/story/flavour-text.md): every region and zone on the map, every Charter and Instrument,
    /// the keystones, abilities, memories and purses has its line, and every line keeps the style guide: twenty words
    /// or fewer, no numerals (the blurbs carry the numbers), no modern idiom, a full stop at the end. No line is kept
    /// for a thing that isn't there, and the translators get them all.
    /// </summary>
    public class FlavourTests
    {
        [Test]
        public void EveryThingCarriedOrDrawnHasALine()
        {
            foreach (Region r in Enum.GetValues(typeof(Region))) Assert.IsNotEmpty(Flavour.ForRegion(r), "region " + r);
            foreach (var z in WorldGraph.Zones) Assert.IsNotEmpty(Flavour.ForZone(z.Id), "zone " + z.Id);
            foreach (CharterKind k in Enum.GetValues(typeof(CharterKind))) Assert.IsNotEmpty(Flavour.ForCharter(k), "Charter " + k);
            foreach (InstrumentKind k in Enum.GetValues(typeof(InstrumentKind)))
                if (k != InstrumentKind.None) Assert.IsNotEmpty(Flavour.ForInstrument(k), "Instrument " + k);
            Assert.IsNotEmpty(Flavour.HalesLens);
            foreach (var h in Keystones.Homes)
            {
                Assert.IsNotEmpty(Flavour.ForKeystone(h), "keystone " + h);
                Assert.AreNotEqual(h, Keystones.NameOf(h), "keystone " + h + " has a name");
            }
            foreach (Ability a in Enum.GetValues(typeof(Ability)))
                if (a != Ability.None) Assert.IsNotEmpty(Flavour.ForAbility(a), "ability " + a);
            foreach (var m in Memories.English.Keys) Assert.IsNotEmpty(Flavour.ForMemory(m), "memory " + m);
            Assert.IsNotEmpty(Flavour.ForCurrency(Flavour.IrisSeed));
            Assert.IsNotEmpty(Flavour.ForCurrency(Flavour.VellumScrap));
            Assert.AreEqual("", Flavour.ForInstrument(InstrumentKind.None));
            Assert.AreEqual("", Flavour.ForZone("Nowhere.AtAll"));
        }

        [Test]
        public void NoLineForAThingThatIsntThere()
        {
            var zones = WorldGraph.Zones.Select(z => z.Id).ToHashSet();
            foreach (var key in Flavour.English.Keys)
            {
                var parts = key.Split(new[] { '.' }, 3);
                Assert.AreEqual("flavour", parts[0], key);
                string kind = parts[1], id = parts[2];
                bool known = kind switch
                {
                    "region" => Enum.TryParse<Region>(id, out _),
                    "zone" => zones.Contains(id),
                    "charter" => Enum.TryParse<CharterKind>(id, out _),
                    "instrument" => id == "HalesLens" || (Enum.TryParse<InstrumentKind>(id, out var ik) && ik != InstrumentKind.None),
                    "keystone" => Keystones.Homes.Contains(id),
                    "ability" => Enum.TryParse<Ability>(id, out var a) && a != Ability.None,
                    "memory" => Memories.English.ContainsKey(id),
                    "currency" => id == Flavour.IrisSeed || id == Flavour.VellumScrap,
                    _ => false,
                };
                Assert.IsTrue(known, key + " names nothing in the game");
            }
        }

        static readonly string[] Idiom = { "okay", "ok", "yeah", "guys", "kind of", "sort of", "gonna", "stuff" };

        [Test]
        public void EveryLineKeepsTheStyleGuide()
        {
            foreach (var kv in Flavour.English)
            {
                string line = kv.Value;
                int words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                Assert.LessOrEqual(words, 20, kv.Key + ": style guide 2.1, twenty words or fewer (" + words + ")");
                Assert.IsFalse(Regex.IsMatch(line, @"\d"), kv.Key + ": numbers belong to the blurb");
                Assert.IsTrue(Regex.IsMatch(line, @"[.!?']$"), kv.Key + ": a whole sentence");
                Assert.IsFalse(line.Contains("  "), kv.Key);
                foreach (var i in Idiom) Assert.IsFalse(Regex.IsMatch(line, @"\b" + i + @"\b", RegexOptions.IgnoreCase), kv.Key + ": no modern idiom (" + i + ")");
            }
            Assert.AreEqual(Flavour.English.Count, Flavour.English.Values.Distinct().Count(), "no line said twice");
        }

        [Test]
        public void HalesWayWithTheLensShowsOnceHeIsBeaten()
        {
            var w = new WorldState();
            Assert.AreEqual(Flavour.ForInstrument(InstrumentKind.SightingLens), Flavour.ForInstrument(InstrumentKind.SightingLens, w));
            w.Set(Bosses.FlagKey("hale"), true);
            Assert.AreEqual(Flavour.HalesLens, Flavour.ForInstrument(InstrumentKind.SightingLens, w));
            Assert.AreEqual(Flavour.ForInstrument(InstrumentKind.WaxSeal), Flavour.ForInstrument(InstrumentKind.WaxSeal, w), "only the lens");
        }

        [Test]
        public void TheTranslatorsGetEveryLine()
        {
            var harvested = DataText.All().Select(e => e.key).ToHashSet();
            foreach (var key in Flavour.English.Keys) Assert.IsTrue(harvested.Contains(key), key + " is harvested");
            foreach (var h in Keystones.Homes) Assert.IsTrue(harvested.Contains(Keystones.NameKey(h)), h + "'s name is harvested");
            Assert.AreEqual("flavour.zone.Saltmarrow.Quay", Flavour.Key("zone", "Saltmarrow.Quay"));
        }
    }
}
