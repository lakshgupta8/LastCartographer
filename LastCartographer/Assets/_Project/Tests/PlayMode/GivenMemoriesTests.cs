#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The memories people give her (flavour-text §5): Dotha's songs when Wren writes Merrow's End as it was, Sable's
    /// count once the widow is decided, each bound once with <c>&lt;&lt;bind&gt;&gt;</c>. And the noticed breaks
    /// (world-map §6): Kettil and Teodor say so, once, only when the stair or the iris gap was crossed without a wing.
    /// </summary>
    public class GivenMemoriesTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        [UnityTest]
        public IEnumerator DothaAndSableGiveTheirMemories()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            Assert.IsFalse(Memories.Has(w, "dotha.nine_songs"));
            yield return _replay.Talk("Dotha", "Merrow_Dotha_Season", new[] { 0 });   // write it as it was
            Assert.IsTrue(Memories.Has(w, "dotha.nine_songs"), "weather first");
            Assert.AreEqual("nine songs, and which came first", Memories.Name("dotha.nine_songs"));

            yield return _replay.Talk("Sable", "Quay_Sable_Widow", new[] { 0 });
            Assert.IsTrue(Memories.Has(w, "sable.boats_back"), "the number she gives away");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("eleven hundred and six")), "she says the count");
            Assert.IsFalse(_replay.Heard.Any(l => l.Contains("didn't come back") || l.Contains("never came back")), "and never the other number");
            CollectionAssert.AreEquivalent(new[] { "dotha.nine_songs", "sable.boats_back" }, w.BoundMemories);
            foreach (var id in w.BoundMemories) Assert.IsNotEmpty(Flavour.ForMemory(id), id + " has its margin line");
        }

        [UnityTest]
        public IEnumerator KettilAndTeodorNoticeABreak()
        {
            yield return _replay.Boot();
            yield return _replay.Talk("Kettil, walked in", "Rest_Kettil_First", new[] { 0 });
            yield return _replay.Talk("Teodor, walked in", "QuietHouse_Teodor_First", new[] { 0 });
            Assert.IsFalse(_replay.Heard.Any(l => l.Contains("count you twice") || l.Contains("iris gap")), "no break, nothing said");

            var w = GameState.World;
            w.Set(SequenceBreaks.FlagKey("Emberdown.FurnaceStair"), true);
            w.Set(SequenceBreaks.FlagKey("Verdance.OldRoad"), true);
            yield return _replay.Talk("Kettil, broke in", "Rest_Kettil_First", new[] { 0 });
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("count you twice")), "Kettil counts a wingless climber twice");
            yield return _replay.Talk("Teodor, broke in", "QuietHouse_Teodor_First", new[] { 0 });
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("iris gap")), "Teodor heard about the iris gap");
        }
    }
}
