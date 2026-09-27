using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>Combat doc 5: each Charter rewrites the combo, the neutral Flourish and one passive.</summary>
    public class CharterTests
    {
        GameObject _floor, _wren;
        readonly System.Collections.Generic.List<GameObject> _spawned = new System.Collections.Generic.List<GameObject>();
        WrenController _ctrl;
        Inkwell _ink;
        WrenVitals _vitals;
        QuillStrike _strike;
        Flourishes _fl;
        CharterSet _charters;
        AbilitySet _abilities;
        ScriptedInput _input;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _abilities = _wren.AddComponent<AbilitySet>();
            _abilities.Unlock(Ability.Wingbeat);
            _ink = _wren.AddComponent<Inkwell>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _strike = _wren.AddComponent<QuillStrike>();
            _strike.hitMask = LayerMask.GetMask("Hittable", "Enemy");
            _fl = _wren.AddComponent<Flourishes>();
            _fl.hitMask = LayerMask.GetMask("Hittable", "Enemy");
            _charters = _wren.AddComponent<CharterSet>();   // last: it drives the others
            _ctrl.Teleport(Vector2.zero);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var g in _spawned) if (g != null) Object.Destroy(g);
            _spawned.Clear();
            Object.Destroy(_wren); Object.Destroy(_floor);
            Time.timeScale = 1f;
            GameState.NewGame();
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

        IEnumerator Swing()
        {
            _input.PressAttack();
            yield return Frames(1);
            while (_strike.IsBusy) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator SurveyorIsTheDefaultAndItsThirdHitIsALongThrustWithKnockback()
        {
            yield return Frames(3);
            Assert.AreEqual(CharterKind.Surveyor, _charters.Current.Kind);
            var far = Dummy(new Vector2(2.9f, 0.6f));   // beyond the 2.2 slash, inside the 3.0 thrust
            float knock = 0f;
            far.WasHit += h => knock = h.Knockback;

            yield return Swing();
            Assert.AreEqual(0, _strike.ComboIndex);
            Assert.AreEqual(0, far.Hits, "first slash cannot reach");
            yield return Swing();
            Assert.AreEqual(1, _strike.ComboIndex);
            yield return Swing();
            Assert.AreEqual(2, _strike.ComboIndex, "third press within the window is the thrust");
            Assert.AreEqual(1, far.Hits, "the thrust reaches 3.0");
            Assert.AreEqual(2.5f, knock, 0.01f, "the thrust knocks back");
        }

        [UnityTest]
        public IEnumerator ComboDropsBackToTheFirstHitAfterTheWindow()
        {
            yield return Frames(3);
            yield return Swing();
            yield return Swing();
            Assert.AreEqual(1, _strike.ComboIndex);
            yield return Frames(_strike.comboWindowFrames + 2);
            yield return Swing();
            Assert.AreEqual(0, _strike.ComboIndex, "too late: the combo restarts");
        }

        [UnityTest]
        public IEnumerator SurveyorInkFillsAQuarterFaster()
        {
            yield return Frames(2);
            _charters.Apply(CharterKind.Surveyor);
            for (int i = 0; i < 4; i++) _ink.AddFromHit(1);
            Assert.AreEqual(5, _ink.Pips, "four hits at 1.25 are five pips");
            _charters.Apply(CharterKind.Warden);
            _ink.Empty();
            for (int i = 0; i < 4; i++) _ink.AddFromHit(1);
            Assert.AreEqual(4, _ink.Pips, "other Charters fill at 1.0");
        }

        [UnityTest]
        public IEnumerator WardenAddsAMaskAndShortensTheWingbeat()
        {
            yield return Frames(3);
            _charters.Apply(CharterKind.Warden);
            Assert.AreEqual(6, _vitals.MaxMasks);
            Assert.AreEqual(FlourishKind.Blot, _fl.PickKind(Vector2.zero), "neutral Flourish is Blot");
            Assert.AreEqual(FlourishKind.Crosshatch, _fl.PickKind(Vector2.up), "up falls back to Crosshatch");

            _ctrl.Teleport(new Vector2(0f, 6f));
            yield return Frames(1);
            float x0 = _ctrl.Position.x;
            _input.PressDash();
            yield return Frames(_ctrl.dashFrames);
            Assert.AreEqual(_ctrl.dashDistance * 0.7f, _ctrl.Position.x - x0, 0.3f, "Warden dash is 70%");
        }

        [UnityTest]
        public IEnumerator DrifterGetsASecondWingbeatAndOnlyFourMasks()
        {
            yield return Frames(3);
            _charters.Apply(CharterKind.Drifter);
            Assert.AreEqual(4, _vitals.MaxMasks);
            Assert.AreEqual(4, _vitals.Masks, "current masks clamp to the cap");
            Assert.AreEqual(FlourishKind.Longstroke, _fl.PickKind(Vector2.zero));
            Assert.AreEqual(FlourishKind.Crosshatch, _fl.PickKind(Vector2.right));
            yield return Frames(2);   // charges refresh on the ground

            _ctrl.Teleport(new Vector2(0f, 12f));
            yield return Frames(1);
            _input.PressDash();
            yield return Frames(_ctrl.dashFrames + 2);
            _input.PressDash();
            yield return Frames(2);
            Assert.IsTrue(_ctrl.IsDashing, "Drifter: a second Wingbeat in the same airtime");

            _charters.Apply(CharterKind.Surveyor);
            yield return Frames(_ctrl.dashFrames + 2);
            _input.PressDash();
            yield return Frames(2);
            Assert.IsFalse(_ctrl.IsDashing, "Surveyor: no third dash without landing");
        }

        [UnityTest]
        public IEnumerator SwappingThroughEquipmentAppliesTheCharter()
        {
            yield return Frames(2);
            var e = GameState.World.Equipment;
            Assert.IsTrue(e.OwnsCharter(CharterKind.Warden), "greybox owns the base Charters");
            Assert.IsTrue(e.SetCharter(CharterKind.Warden));
            Assert.AreEqual(CharterKind.Warden, _charters.Current.Kind);
            Assert.AreEqual(3, _strike.Combo.Length);
            Assert.AreEqual("Sweep", _strike.Combo[0].Name);
            Assert.IsFalse(e.SetCharter(CharterKind.Ferryman), "not owned");
            Assert.AreEqual(CharterKind.Warden, e.Charter);
        }
    }
}
