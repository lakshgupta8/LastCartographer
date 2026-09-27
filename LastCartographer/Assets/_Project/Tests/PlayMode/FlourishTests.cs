using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>Combat doc 4: costs, gating on ink, multi-hit, piercing reach, and Blot's slow.</summary>
    public class FlourishTests
    {
        GameObject _floor, _wren;
        readonly System.Collections.Generic.List<GameObject> _spawned = new System.Collections.Generic.List<GameObject>();
        WrenController _ctrl;
        Inkwell _ink;
        Flourishes _fl;
        ScriptedInput _input;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _ink = _wren.AddComponent<Inkwell>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _fl = _wren.AddComponent<Flourishes>();
            _fl.hitMask = LayerMask.GetMask("Hittable", "Enemy");
            _ctrl.Teleport(Vector2.zero);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var g in _spawned) if (g != null) Object.Destroy(g);
            _spawned.Clear();
            Object.Destroy(_wren); Object.Destroy(_floor);
            Time.timeScale = 1f;
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        TrainingDummy Dummy(Vector2 pos)
        {
            var d = new GameObject("Dummy") { layer = Layer("Hittable") };
            d.AddComponent<BoxCollider2D>().size = Vector2.one;
            d.transform.position = pos;
            _spawned.Add(d);
            return d.AddComponent<TrainingDummy>();
        }

        T EnemyAt<T>(Vector2 pos) where T : Enemy
        {
            var e = new GameObject(typeof(T).Name) { layer = Layer("Enemy") };
            e.transform.position = pos;
            e.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
            e.AddComponent<Rigidbody2D>();
            _spawned.Add(e);
            return e.AddComponent<T>();
        }

        [UnityTest]
        public IEnumerator FlourishIsRefusedWithoutInkAndCostsPipsWhenUsed()
        {
            yield return Frames(3);
            bool refused = false;
            _fl.Refused += _ => refused = true;
            Assert.IsFalse(_fl.TryPerform(FlourishKind.Crosshatch), "no ink: refused");
            Assert.IsTrue(refused);

            _ink.Add(9);
            Assert.IsTrue(_fl.TryPerform(FlourishKind.Crosshatch));
            Assert.AreEqual(9 - _fl.crosshatchCost, _ink.Pips, "Crosshatch spends its cost up front");
        }

        [UnityTest]
        public IEnumerator CrosshatchLandsSixHitsWithoutRefillingInk()
        {
            _ink.Add(9);
            var d = Dummy(new Vector2(1.4f, 0.6f));
            yield return Frames(3);
            Assert.IsTrue(_fl.TryPerform(FlourishKind.Crosshatch));
            yield return Frames(_fl.crosshatchHits * _fl.crosshatchInterval + _fl.recoveryFrames + 4);
            Assert.AreEqual(_fl.crosshatchHits, d.Hits, "six ticks of the flurry");
            Assert.AreEqual(9 - _fl.crosshatchCost, _ink.Pips, "flourish hits do not refill ink");
            Assert.IsFalse(_fl.IsBusy, "flourish ends and unlocks");
        }

        [UnityTest]
        public IEnumerator LongstrokePiercesThroughTwoTargetsInLine()
        {
            _ink.Add(9);
            var near = Dummy(new Vector2(2f, 0.6f));
            var far = Dummy(new Vector2(5.2f, 0.6f));
            var beyond = Dummy(new Vector2(8f, 0.6f));
            yield return Frames(3);
            Assert.IsTrue(_fl.TryPerform(FlourishKind.Longstroke));
            yield return Frames(_fl.longstrokeStartup + _fl.recoveryFrames + 2);
            Assert.AreEqual(1, near.Hits);
            Assert.AreEqual(1, far.Hits, "reach covers 6 units and passes through the first target");
            Assert.AreEqual(0, beyond.Hits, "out of reach");
        }

        [UnityTest]
        public IEnumerator BlotHitsAllAroundAndSlowsASmudge()
        {
            _ink.Add(9);
            var left = Dummy(new Vector2(-1.6f, 0.6f));
            var right = Dummy(new Vector2(1.6f, 0.6f));
            var smudge = EnemyAt<Smudge>(new Vector2(0f, 2.2f));
            smudge.ForceDrawn(true);
            yield return Frames(3);
            int hp = smudge.Health;
            Assert.IsTrue(_fl.TryPerform(FlourishKind.Blot));
            yield return Frames(_fl.blotStartup + 2);
            Assert.AreEqual(1, left.Hits);
            Assert.AreEqual(1, right.Hits);
            Assert.AreEqual(hp - 1, smudge.Health, "Blot damages the drawn smudge");
            Assert.IsTrue(smudge.IsSlowed, "Blot slows smudges");
        }

        [UnityTest]
        public IEnumerator DirectionHeldPicksTheFlourishFromTheButton()
        {
            _ink.Add(9);
            yield return Frames(3);
            FlourishKind performed = FlourishKind.None;
            _fl.Performed += k => performed = k;
            _input.Move = Vector2.up;
            _input.PressFlourish();
            yield return Frames(2);
            Assert.AreEqual(FlourishKind.Blot, performed, "up + Flourish is Blot");
        }
    }
}
