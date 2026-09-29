#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The coast's two askers played through the shipped Yarn (docs/design/offerings.md): the Salt Chapel's door takes
    /// Dotha's songs and loosens an anchored Merrow's End; the gannet at the faded third light takes Sable's count.
    /// Without the memory the door only waits, and the gannet's answer is dimmed.
    /// </summary>
    public class OfferingsPlayTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        [UnityTest]
        public IEnumerator TheDoorTakesASongAndTheSealLoosens()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            yield return _replay.Talk("the door, nothing to give", "Chapel_Door", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Sing me in")));
            Assert.IsFalse(Offerings.IsDone(w, "chapel_door"));

            yield return _replay.Talk("Dotha", "Merrow_Dotha_Season", new[] { 0 });
            Places.Anchor(w, "Saltmarrow_B");
            int scraps = Commissions.Scraps(w);
            yield return _replay.Talk("the door, singing", "Chapel_Door", new[] { 0 });
            Assert.IsTrue(w.Is("saltmarrow.chapel.door_open"), "open");
            Assert.IsFalse(Memories.Has(w, "dotha.nine_songs"), "the songs are the door's now");
            Assert.AreEqual(scraps + 2, Commissions.Scraps(w));
            Assert.AreEqual(1, FadeStages.Get(w, "Saltmarrow_B"), "Merrow's End's seal loosened a stage");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("sealed into Merrow's End")), "the door said what it would cost");

            yield return _replay.Talk("the door, after", "Chapel_Door", new int[0]);
            Assert.IsTrue(_replay.Heard.Last().Contains("reliquary behind it is empty"));
        }

        [UnityTest]
        public IEnumerator TheGannetTakesSablesCount()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            yield return _replay.Talk("the gannet, nothing to tell", "Chain_Gannet", new[] { 1 });
            Assert.IsFalse(Offerings.IsDone(w, "chain_gannet"), "\"I don't know\" gives nothing");

            yield return _replay.Talk("Sable", "Quay_Sable_Widow", new[] { 0 });
            int seeds = Economy.Seeds(w);
            yield return _replay.Talk("the gannet, told", "Chain_Gannet", new[] { 0 });
            Assert.IsTrue(Offerings.IsDone(w, "chain_gannet"));
            Assert.IsTrue(Offerings.IsGiven(w, "sable.boats_back"));
            Assert.AreEqual(seeds + 6, Economy.Seeds(w), "seed, for the number");
            Assert.AreEqual(0, FadeStages.Get(w, "Saltmarrow_A"), "the quay wasn't anchored: nothing to loosen");
        }
    }
}
