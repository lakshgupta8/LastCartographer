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
        public IEnumerator TheDoorTheTravellerAndBrekTakeTheirs()
        {
            yield return _replay.Boot();
            var w = GameState.World;

            // The ninth chimney's door: a count with her in it. Kettil's Rest is held, so nothing loosens.
            yield return _replay.Talk("the ninth door, uncounted", "Ninth_Door", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("waits for a count")));
            Memories.Bind(w, "kettil.count");
            Places.Hold(w, "Emberdown_Rest_2");
            int scraps = Commissions.Scraps(w);
            yield return _replay.Talk("the ninth door, counted", "Ninth_Door", new[] { 0 });
            Assert.IsTrue(w.Is("emberdown.ninth.door_open"), "open");
            Assert.AreEqual(scraps + 2, Commissions.Scraps(w), "the builder's satchel");
            Assert.IsFalse(Memories.Has(w, "kettil.count"));
            Assert.AreEqual(0, FadeStages.Get(w, "Emberdown_Rest_2"), "a held place is held by its people");
            Assert.AreEqual(0, Offerings.Weakened(w, "Emberdown_Rest_2"));

            // The traveller at the one-night inn: nobody there until the road is, then the eleven names for seed.
            yield return _replay.Talk("the inn, no road", "Inn_Traveller", new int[0]);
            Assert.IsTrue(_replay.Heard.Last().Contains("nobody on it tonight"));
            w.Set("verdance.gate.inn_visited", true);
            Memories.Bind(w, "teodor.eleven_names");
            int seeds = Economy.Seeds(w);
            yield return _replay.Talk("the inn, the names", "Inn_Traveller", new[] { 0 });
            Assert.AreEqual(seeds + 8, Economy.Seeds(w), "seed, for the names");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Mine was let go")));
            yield return _replay.Talk("the inn, after", "Inn_Traveller", new int[0]);
            Assert.IsTrue(_replay.Heard.Last().Contains("in order"));

            // Brek: near the stones until she has jumped; his answer dimmed until she carries Idrenne's place.
            yield return _replay.Talk("Brek, before the leap", "Gate_Brek", new int[0]);
            Assert.IsTrue(_replay.Heard.Last().Contains("near them"));
            w.Set("windreach.leap.done", true);
            yield return _replay.Talk("Brek, no place to give", "Gate_Brek", new[] { 1 });
            Assert.IsFalse(Offerings.IsDone(w, "gate_brek"));
            Memories.Bind(w, "idrenne.standing_place");
            yield return _replay.Talk("Brek, her fire", "Gate_Brek", new[] { 0 });
            Assert.IsTrue(w.Is("windreach.brek.on_the_stone"), "he stands on it");
            Assert.IsTrue(Offerings.IsGiven(w, "idrenne.standing_place"));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("I'm on the stone")));
        }

        [UnityTest]
        public IEnumerator CorvinAsksForIsoldesMemoryWhenTheWordsFallShort()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            // Ilse unheard, the sky unflown: the argument falls short on all three. She carries Isolde's memory.
            Memories.Bind(w, "isolde.first_sight");
            yield return _replay.Talk("Corvin, given what Isolde saw", "Capital_Corvin", new[] { 1, 1, 1, 1, 0 });
            Assert.AreEqual(Endings.CorvinPersuaded, w.Get(Endings.CorvinStanceFlag), "persuaded by what he saw");
            Assert.IsTrue(w.Is("corvin.saw_her"));
            Assert.IsTrue(Offerings.IsGiven(w, "isolde.first_sight"));
            Assert.IsFalse(Memories.Has(w, "isolde.first_sight"), "the first thing ever bound, gone");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Looking straight at her")));

            // Without it, the same argument ends where it did.
            GameState.NewGame(); w = GameState.World;
            yield return _replay.Talk("Corvin, nothing to give", "Capital_Corvin", new[] { 1, 1, 1, 1 });
            Assert.AreEqual(Endings.CorvinUnpersuaded, w.Get(Endings.CorvinStanceFlag));
            Assert.IsTrue(_replay.Heard.Last().Contains("and I love you"));
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
