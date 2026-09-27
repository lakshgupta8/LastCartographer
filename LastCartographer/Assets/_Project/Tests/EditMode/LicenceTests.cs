using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>Wren's standing with the Guild (DES-03 §2): a journeyman until the count, hunted after, stood down by Oriel, hunted again by the report.</summary>
    public class LicenceTests
    {
        [Test]
        public void TheCountRevokesHerOnce()
        {
            var w = new WorldState();
            Assert.IsFalse(Licence.IsUnlicensed(w));
            Assert.AreEqual("journeyman", Licence.Describe(w));
            Assert.IsFalse(Licence.WardensHostile(w), "Wardens measure a journeyman");
            Assert.IsTrue(Licence.Revoke(w));
            Assert.IsFalse(Licence.Revoke(w), "once");
            Assert.IsTrue(Licence.IsUnlicensed(w));
            Assert.AreEqual("unlicensed", Licence.Describe(w));
            Assert.IsTrue(Licence.WardensHostile(w), "and hunt an unlicensed cartographer");
            Assert.IsTrue(w.Is("act1.unlicensed"), "the flag Halvard's scene writes");
        }

        [Test]
        public void OrielStandsThemDownAndTheReportSetsThemOnAgain()
        {
            var w = new WorldState();
            Licence.Revoke(w);
            w.Set(Licence.StoodDownFlag, true);
            Assert.IsFalse(Licence.WardensHostile(w), "Oriel's stand-down");
            w.Set(Licence.ReportSentFlag, true);
            Assert.IsTrue(Licence.WardensHostile(w), "Pell's report, sent, is the Guild's stance whatever Oriel said");

            var fresh = new WorldState();
            fresh.Set(Licence.ReportSentFlag, true);
            Assert.IsTrue(Licence.WardensHostile(fresh), "the report alone is enough");
            Assert.IsFalse(Licence.IsUnlicensed(fresh));
        }
    }
}
