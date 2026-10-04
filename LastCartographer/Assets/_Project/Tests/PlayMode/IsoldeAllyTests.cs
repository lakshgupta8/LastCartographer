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
    /// Isolde fighting beside Wren at the Edge (NAR-03, <see cref="IsoldeAlly"/>): she strikes the near smudges only while
    /// they are drawn, leaves the last one to Wren and says so, and goes back to where she stood.
    /// </summary>
    public class IsoldeAllyTests
    {
        GameObject _room;
        readonly System.Collections.Generic.List<string> _captions = new System.Collections.Generic.List<string>();
        void OnCaption(string text, float seconds) => _captions.Add(text);

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            GameState.NewGame();
            _room = new GameObject("IsoldeAllyRoom");
            Captions.Shown += OnCaption;
        }

        [TearDown]
        public void TearDown()
        {
            Captions.Shown -= OnCaption;
            Object.Destroy(_room);
            GameState.NewGame();
        }

        Smudge Target(float x)
        {
            var go = new GameObject("Smudge") { layer = Layer("Enemy") };
            go.transform.SetParent(_room.transform, false);
            go.transform.position = new Vector3(x, 0.6f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
            // No Wren here to drift toward, so nothing takes back a knockback (in the room, the chase toward her does): pin it.
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            var s = go.AddComponent<Smudge>();
            s.SetMaxHealth(2);
            return s;
        }

        IsoldeAlly Isolde(float x, params Smudge[] targets)
        {
            var go = new GameObject("Isolde");
            go.transform.SetParent(_room.transform, false);
            go.transform.position = new Vector3(x, 0f, 0f);
            var a = go.AddComponent<IsoldeAlly>();
            a.Configure(targets.Cast<Enemy>().ToArray());
            return a;
        }

        [UnityTest]
        public IEnumerator SheTakesTheNearOnesAndLeavesTheLastToWren()
        {
            var smudges = new[] { Target(2f), Target(4f), Target(9f) };
            foreach (var s in smudges) s.ForceDrawn(true);
            var isolde = Isolde(0f, smudges);

            float t = 0f;
            while (!isolde.Yielded && t < 15f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(isolde.Yielded, "she steps back for the last one");
            Assert.AreEqual(2, smudges.Count(s => s == null || s.IsDead || s.IsDying), "the two near ones are hers");
            var last = smudges.Single(s => s != null && !s.IsDead && !s.IsDying);
            Assert.AreEqual(last.MaxHealth, last.Health, "the last one is untouched: Wren's");
            Assert.IsTrue(_captions.Any(c => c.Contains("That one's yours")), "and she says so");
            Assert.Greater(isolde.Strokes, 0);

            for (int i = 0; i < 90; i++) yield return null;
            Assert.AreEqual(last.MaxHealth, last.Health, "she keeps her word");
            Assert.Less(isolde.transform.position.x, 1f, "back where she stood");
        }

        [UnityTest]
        public IEnumerator SheNeverStrikesWhatIsNotThere()
        {
            var smudges = new[] { Target(1f), Target(1.5f), Target(8f) };
            foreach (var s in smudges) s.ForceDrawn(false);
            var isolde = Isolde(0f, smudges);
            for (float t = 0f; t < 3f; t += Time.deltaTime) yield return null;
            Assert.AreEqual(0, isolde.Strokes, "undrawn, there is nothing to hit: she waits for the ink");
            Assert.IsTrue(smudges.All(s => s.Health == s.MaxHealth));

            smudges[0].ForceDrawn(true);
            for (float t = 0f; t < 2f; t += Time.deltaTime) yield return null;
            Assert.Greater(isolde.Strokes, 0, "drawn, she strikes");
        }
    }
}
