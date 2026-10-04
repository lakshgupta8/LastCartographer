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
    /// The Tether-Widows answered in person (NAR-04, saltmarrow-arc.md): the widow at the post speaks for herself once
    /// the fourth lamp is lit; whichever way she is answered, the commission's flag is the same and Sable gives her
    /// count after, bound as her memory; and she is gone from the post once she went in (FlagPresence), there while
    /// she stays. The Ferrymen's two other voices on the quay read the same state.
    /// </summary>
    public class WidowTests
    {
        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        static FlagPresence AtThePost()
        {
            var go = new GameObject("Knot_Test");
            new GameObject("Drawing").transform.SetParent(go.transform, false);
            var p = go.AddComponent<FlagPresence>();
            p.Configure("boss.lamp_keeper.defeated", "saltmarrow.widow.decided", 2);
            return p;
        }

        [UnityTest]
        public IEnumerator TalkedOutAtThePostSheStaysAndSableGivesTheCount()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            var post = AtThePost();
            yield return null;
            Assert.IsFalse(post.Standing, "before the fourth lamp she is not at the post");
            w.Set("boss.lamp_keeper.defeated", 1);
            Assert.IsTrue(post.Standing, "the lamp lit, she waits there");
            Assert.IsTrue(post.transform.GetChild(0).gameObject.activeSelf);

            yield return _replay.Talk("Knot", "Tether_Knot", new[] { 0 });   // he went forty paces; the rope came back
            Assert.AreEqual(1, w.Get("saltmarrow.widow.decided"), "talked out");
            Assert.IsTrue(w.Is("saltmarrow.widow.met"), "and answered in person");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Take the rope off me")));
            Assert.IsTrue(post.Standing, "she stays at the post");
            Assert.AreEqual(1, Voices.Count(w, Voice.Surveyor, "saltmarrow"), "in the surveyor's voice");

            yield return _replay.Talk("Knot", "Tether_Knot", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Still here")), "and says so after");

            Assert.IsFalse(Memories.Has(w, "sable.boats_back"));
            yield return _replay.Talk("Sable", "Quay_Sable_Widow_Met", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("post yourself")), "Sable heard");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("rope back")), "and how it went");
            Assert.IsTrue(Memories.Has(w, "sable.boats_back"), "and gives the count all the same");
            Assert.IsTrue(w.Is("saltmarrow.sable.widow_heard"));
            Assert.IsFalse(_replay.Heard.Any(l => l.Contains("didn't come back") || l.Contains("never came back")), "never the other number");
            Object.Destroy(post.gameObject);
        }

        [UnityTest]
        public IEnumerator GoneWithHerSheIsGoneFromThePost()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            w.Set("boss.lamp_keeper.defeated", 1);
            var post = AtThePost();
            yield return null;
            Assert.IsTrue(post.Standing);
            yield return _replay.Talk("Knot", "Tether_Knot", new[] { 1 });   // then I hold the other end
            Assert.AreEqual(2, w.Get("saltmarrow.widow.decided"));
            Assert.IsFalse(post.Standing, "one fewer rope on the post");
            Assert.IsFalse(post.transform.GetChild(0).gameObject.activeSelf);
            Assert.AreEqual(1, Voices.Count(w, Voice.Warden, "saltmarrow"));

            yield return _replay.Talk("Sable", "Quay_Sable_Widow_Met", new int[0]);
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("held the other end")));
            Assert.IsTrue(Memories.Has(w, "sable.boats_back"));
            Object.Destroy(post.gameObject);
        }

        [UnityTest]
        public IEnumerator TheFerrymenReadTheWidowsAnswer()
        {
            yield return _replay.Boot();
            var w = GameState.World;
            yield return _replay.Talk("Skua", "Quay_Skua", new[] { 0 });
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("my rope")), "Skua's first word");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Depends who holds the other end")));
            yield return _replay.Talk("Dunlin", "Quay_Dunlin", new[] { 1 });
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Hers has mine in it")), "Dunlin counts inside Sable's count");
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("Her brother")), "and knows who taught her");
            Assert.IsFalse(_replay.Heard.Any(l => l.Contains("went out") && l.StartsWith("Dunlin:") && !l.Contains("count them out")), "he never counts the boats out");

            w.Set("boss.lamp_keeper.defeated", 1);
            w.Set("saltmarrow.widow.decided", 1);
            _replay.Heard.Clear();
            yield return _replay.Talk("Skua", "Quay_Skua", new[] { 2 });
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("handed the rope back")), "Skua knows she stayed");
            yield return _replay.Talk("Dunlin", "Quay_Dunlin", new[] { 2 });
            Assert.IsTrue(_replay.Heard.Any(l => l.Contains("new light")), "Dunlin counted by the new lamp");
        }
    }
}
