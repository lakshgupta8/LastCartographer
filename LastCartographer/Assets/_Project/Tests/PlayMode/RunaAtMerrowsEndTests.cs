using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Runa at Merrow's End (bible 4.1's hold option, character-bibles.md §3): asked at the bell in Act 2 she comes to
    /// the coast, stands by Dotha's stoop while asked for and not once the village was let go, teaches the walk to a
    /// Wren who never learned it, and once the village is held puts it in the roll-call.
    /// </summary>
    public class RunaAtMerrowsEndTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static FlagPresence AtTheStoop()
        {
            var go = new GameObject("Runa_Merrow_Test");
            new GameObject("Drawing").transform.SetParent(go.transform, false);
            var p = go.AddComponent<FlagPresence>();
            p.Configure("emberdown.runa.asked_for_merrow", "saltmarrow.dotha.decided", 2);
            return p;
        }

        [UnityTest]
        public IEnumerator AskedAtTheBellSheComesAndTeachesTheWalk()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            w.Set("emberdown.runa.counted", 1); w.Set("saltmarrow.dotha.songs", 1); w.Set("act2.started", 1);
            var runa = AtTheStoop();
            yield return null;
            Assert.IsFalse(runa.Standing, "not asked: she is at her bell");

            yield return _replay.Talk("Runa", "Bell_Runa_Named", new[] { 0 });   // Merrow's End: nine songs, nobody counting
            Assert.IsTrue(w.Is("emberdown.runa.asked_for_merrow"), "asked");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("fish village")));
            Assert.IsTrue(w.Is(BoundsWalks.LearnedFlag), "the bell teaches the walk as before");
            Assert.IsTrue(runa.Standing, "asked: she stands by the stoop");

            w.Set(BoundsWalks.LearnedFlag, 0);   // a Wren who never learned it
            yield return _replay.Talk("Runa", "Merrow_Runa", new[] { 1 });   // where does it start?
            Assert.IsTrue(w.Is("saltmarrow.runa.came"));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("I brought the tune")));
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Her stoop, the post")), "she names the bounds");
            Assert.IsFalse(BoundsWalks.IsLearned(w), "asking is not learning");

            yield return _replay.Talk("Runa", "Merrow_Runa", new[] { 2 });   // ...
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Still nine")), "and waits");
            Object.Destroy(runa.gameObject);
        }

        [UnityTest]
        public IEnumerator HeldSheCountsTheVillageInAndGoneSheIsGone()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            w.Set("emberdown.runa.asked_for_merrow", 1);
            w.Set(BoundsWalks.DoneKey("Saltmarrow_B"), 1);
            var runa = AtTheStoop();
            yield return null;
            Assert.IsTrue(runa.Standing);
            yield return _replay.Talk("Runa", "Merrow_Runa", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Forty-three, Dotha")), "Merrow's End in the roll-call");
            Assert.IsTrue(w.Is("saltmarrow.runa.counted_merrow"));
            _replay.Heard.Clear();
            yield return _replay.Talk("Runa", "Merrow_Runa", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("She hums along now")), "and keeps counting");

            w.Set("saltmarrow.dotha.decided", 2);
            Assert.IsFalse(runa.Standing, "the village let go: nobody to count, and she is not here");
            Object.Destroy(runa.gameObject);
        }
    }
}
