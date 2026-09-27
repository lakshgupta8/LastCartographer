using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The stones as flags: one per home the map names, counted the way Voss and the endings count them.</summary>
    public class KeystonesTests
    {
        [Test]
        public void EveryHomeIsAKeystoneOnTheMapOrABossWhoHoldsOne()
        {
            Assert.AreEqual(7, Keystones.Homes.Length, "seven homes, Isolde's the seventh");
            Assert.AreEqual(Keystones.Homes.Length, Keystones.Homes.Distinct().Count());
            Assert.AreEqual(WorldGraph.Zones.Count(z => z.Keystone), Keystones.Homes.Length, "one home per keystone zone");
            var w = new WorldState();
            Assert.AreEqual(0, Keystones.Count(w));
            w.Set("keystone.quiet_house", true);
            w.Set("keystone.windreach", true);
            w.Set("keystone.somewhere_else", true);
            Assert.AreEqual(2, Keystones.Count(w), "only the named homes count");
            Assert.IsTrue(Keystones.Has(w, "windreach"));
            Assert.Less(Keystones.Count(w), Keystones.OpenWorldNeeds);
            w.Set(Keystones.FlagKey(Keystones.InTheFrame), true);
            Assert.AreEqual(2, Keystones.Count(w), "the frame's own stone is not carried");
        }
    }
}
