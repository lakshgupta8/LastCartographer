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
        public IEnumerator TheClimbsAndTheSteppeGiveTheirs()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            // Named at the bell, once she has held a place by walking it: Runa gives the count with her in it.
            w.Set("emberdown.runa.counted", true);
            w.Set("emberdown.hollowvein.walked", true);
            yield return _replay.Talk("Runa, naming her", "Bell_Runa_Count", new int[0]);
            Assert.IsTrue(Memories.Has(w, "kettil.count"), "the count at the bell");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Take the count with you")));
            yield return _replay.Talk("Runa, again", "Bell_Runa_Count", new int[0]);
            Assert.AreEqual(1, w.BoundMemories.Count(m => m == "kettil.count"), "once");

            // The vigil: eleven villages, in order.
            yield return _replay.Talk("Teodor, the vigil", "Grove_Teodor_Vigil", new int[0]);
            Assert.IsTrue(Memories.Has(w, "teodor.eleven_names"));
            Assert.IsTrue(_replay.Heard.Any(l => l.StartsWith("Teodor") && l.Contains("Carry the eleven")));

            // The fire: where she was standing when she learned it, given after the stone.
            w.Set("windreach.camp.walked", true);
            yield return _replay.Talk("Idrenne's Fire", "Fire_Idrenne", new[] { 0, 1 });
            Assert.IsTrue(w.Is(Steppe.KeystoneFlag), "the stone first");
            Assert.IsTrue(Memories.Has(w, "idrenne.standing_place"), "then where she stood");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("lighter than the stone")));

            foreach (var id in new[] { "kettil.count", "teodor.eleven_names", "idrenne.standing_place" })
            {
                Assert.IsNotEmpty(Flavour.ForMemory(id), id + " has its margin line");
                Assert.IsNotNull(Memories.HomeOf(id), id + " belongs to where its giver lives");
                Assert.AreNotEqual(id, Memories.Name(id), id + " has words");
            }
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
