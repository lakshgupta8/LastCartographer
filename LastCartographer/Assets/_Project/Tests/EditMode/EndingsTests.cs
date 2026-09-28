using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>Bible 9 as flag logic: each ending needs what the bible says, no less; the frame offers only what is earned.</summary>
    public class EndingsTests
    {
        static WorldState Stones(params string[] homes)
        {
            var w = new WorldState();
            foreach (var h in homes) w.Set(Keystones.FlagKey(h), true);
            return w;
        }

        [Test]
        public void TheFixedWorldNeedsSevenAndCorvin()
        {
            var w = Stones("aury", "hollowvein", "quiet_house", "windreach", "isolde", "archivist");
            Assert.AreEqual(7, Endings.StonesAtTheFrame(w), "six carried and the frame's own");
            Assert.IsFalse(Endings.IsOpen(w, Ending.Fixed), "not without Corvin");
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinCooperates);
            Assert.IsTrue(Endings.IsOpen(w, Ending.Fixed));
            w.Set("keystone.aury", false);
            Assert.IsFalse(Endings.IsOpen(w, Ending.Fixed), "six is not seven");
        }

        [Test]
        public void TheOpenWorldNeedsEverythingNinePointTwoSays()
        {
            var w = Stones("hollowvein", "quiet_house", "windreach", "isolde");
            w.Set("runa.named_wren", true); w.Set("teodor.keystone_given", true); w.Set("windreach.camp.walked", true);
            w.Set("windreach.fire.witnessed", true); w.Set("verdance.aldermere.attended", true);
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinPersuaded);
            Assert.AreEqual(3, Endings.AlliedCount(w));
            Assert.IsTrue(Endings.IsOpen(w, Ending.Open));

            void Without(string flag, string why) { w.Set(flag, false); Assert.IsFalse(Endings.IsOpen(w, Ending.Open), why); w.Set(flag, true); }
            Without("keystone.isolde", "four stones at least");
            Without("windreach.camp.walked", "three of the five names");
            Without("windreach.fire.witnessed", "Idrenne's Fire");
            Without("verdance.aldermere.attended", "and Aldermere or Hollowvein");
            w.Set("verdance.aldermere.attended", false); w.Set("emberdown.hollowvein.walked", true);
            Assert.IsTrue(Endings.IsOpen(w, Ending.Open), "Hollowvein does instead of Aldermere");
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinUnpersuaded);
            Assert.IsFalse(Endings.IsOpen(w, Ending.Open), "and Corvin persuaded");
        }

        [Test]
        public void TheUnwrittenNeedsTeodorAndAldermereAttended()
        {
            var w = Stones("quiet_house");
            w.Set("teodor.keystone_given", true);
            Assert.IsFalse(Endings.IsOpen(w, Ending.Unwritten));
            w.Set("verdance.aldermere.attended", true);
            Assert.IsTrue(Endings.IsOpen(w, Ending.Unwritten));
            w.Set("verdance.aldermere.stopped", true);
            Assert.IsFalse(Endings.IsOpen(w, Ending.Unwritten), "attended without stopping it");
        }

        [Test]
        public void TheRestNeedsNothingAtAllAndTheChair()
        {
            var w = new WorldState();
            Assert.IsFalse(Endings.IsOpen(w, Ending.Rest), "Corvin has to offer it");
            w.Set(Endings.RestOfferedFlag, true);
            Assert.IsTrue(Endings.IsOpen(w, Ending.Rest));
            Places.Anchor(w, "Halden_Lowmarket_2");
            Assert.IsFalse(Endings.IsOpen(w, Ending.Rest), "zero anchors");
            var v = Stones("windreach");
            v.Set(Endings.RestOfferedFlag, true);
            Assert.IsFalse(Endings.IsOpen(v, Ending.Rest), "zero stones");
            Assert.IsFalse(Endings.AnyAtTheFrame(w), "the Rest is not the frame's");
        }

        [Test]
        public void ChoosingIsOnceAndOnlyWhatIsOpen()
        {
            var w = new WorldState();
            Assert.IsFalse(Endings.Choose(w, Ending.Unwritten));
            w.Set("teodor.keystone_given", true); w.Set("verdance.aldermere.attended", true);
            Assert.IsTrue(Endings.Choose(w, Ending.Unwritten));
            Assert.AreEqual(Ending.Unwritten, Endings.Chosen(w));
            w.Set(Endings.RestOfferedFlag, true);
            Assert.IsFalse(Endings.Choose(w, Ending.Rest), "once");
            Assert.IsTrue(Endings.TryParse("OPEN", out var e) && e == Ending.Open);
            Assert.IsFalse(Endings.TryParse("none", out _));
            foreach (Ending x in System.Enum.GetValues(typeof(Ending)))
            {
                if (x == Ending.None) continue;
                var walk = Endings.EpilogueWalk(x);
                Assert.AreEqual("Epilogue_Pell", walk.First(), x + ": Halden first");
                Assert.AreEqual("Epilogue_Marrow", walk.Last(), x + ": Marrow last");
                foreach (var stop in Endings.EpilogueStops(x))
                    Assert.IsNotNull(WorldGraph.Find(stop.Zone), x + "'s stop " + stop.Node + " has a zone on the map");
                Assert.AreEqual("Halden.JourneymansHall", Endings.EpilogueStops(x)[0].Zone, "Halden first");
                Assert.AreEqual("Blank.ThessalyHollow", Endings.EpilogueStops(x).Last().Zone, "the Hollow last");
            }
        }

        [Test]
        public void MarrowsOpenWorldWordIsANewWord()
        {
            var dir = Path.Combine(Application.dataPath, "_Project/Dialogue");
            int n = 0;
            foreach (var f in Directory.GetFiles(dir, "*.yarn", SearchOption.AllDirectories))
                n += Regex.Matches(File.ReadAllText(f), @"\bSkywalk\b", RegexOptions.IgnoreCase).Count;
            Assert.AreEqual(1, n, "a word nobody has said before (9.2): once, in the whole script");
        }
    }
}
