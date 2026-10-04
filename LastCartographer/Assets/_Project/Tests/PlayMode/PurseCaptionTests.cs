using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>The first iris seed says what the coast's money is to her, once (flavour-text.md, the purses); the rest say nothing.</summary>
    public class PurseCaptionTests
    {
        GameObject _wren;
        readonly List<string> _captions = new List<string>();

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        void OnCaption(string text, float seconds) => _captions.Add(text);

        [SetUp]
        public void SetUp()
        {
            GameState.NewGame();
            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>().gravityScale = 0f;
            _wren.AddComponent<WrenController>();
            Captions.Shown += OnCaption;
        }

        [TearDown]
        public void TearDown()
        {
            Captions.Shown -= OnCaption;
            if (_wren != null) Object.Destroy(_wren);
            foreach (var s in Object.FindObjectsByType<IrisSeed>(FindObjectsSortMode.None)) Object.Destroy(s.gameObject);
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator TheFirstSeedSaysWhatItIsAndTheRestSayNothing()
        {
            var w = GameState.World;
            int before = Economy.Seeds(w);
            IrisSeed.Spawn(new Vector2(0f, 0.5f), 2);
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(before + 2, Economy.Seeds(w), "taken");
            Assert.AreEqual(1, _captions.Count, "one caption");
            Assert.AreEqual(Flavour.ForCurrency(Flavour.IrisSeed), _captions[0]);
            StringAssert.Contains("small change", _captions[0]);
            Assert.IsTrue(w.Is(Flavour.SeenKey(Flavour.IrisSeed)));

            IrisSeed.Spawn(new Vector2(0f, 0.5f), 1);
            for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(before + 3, Economy.Seeds(w));
            Assert.AreEqual(1, _captions.Count, "the second says nothing");
        }
    }
}
