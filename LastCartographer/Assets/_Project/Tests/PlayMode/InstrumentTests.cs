using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>Combat doc 6: uses, slots, desk restore, and one real effect per Instrument.</summary>
    public class InstrumentTests
    {
        GameObject _floor, _wren, _systems;
        readonly System.Collections.Generic.List<GameObject> _spawned = new System.Collections.Generic.List<GameObject>();
        WrenController _ctrl;
        Inkwell _ink;
        WrenVitals _vitals;
        QuillStrike _strike;
        InstrumentBelt _belt;
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
            _wren.AddComponent<AbilitySet>();
            _ink = _wren.AddComponent<Inkwell>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _strike = _wren.AddComponent<QuillStrike>();
            _strike.hitMask = LayerMask.GetMask("Hittable", "Enemy");
            _belt = _wren.AddComponent<InstrumentBelt>();
            _belt.hitMask = LayerMask.GetMask("Hittable", "Enemy");
            _belt.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Teleport(Vector2.zero);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var g in _spawned) if (g != null) Object.Destroy(g);
            _spawned.Clear();
            foreach (var d in Object.FindObjectsByType<CompassDart>(FindObjectsSortMode.None)) Object.Destroy(d.gameObject);
            foreach (var p in Object.FindObjectsByType<PlumbWeight>(FindObjectsSortMode.None)) Object.Destroy(p.gameObject);
            foreach (var t in Object.FindObjectsByType<TetherAnchor>(FindObjectsSortMode.None)) Object.Destroy(t.gameObject);
            if (_systems != null) Object.Destroy(_systems);
            Object.Destroy(_wren); Object.Destroy(_floor);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        T Spawn<T>(string name, string layer, Vector2 pos, Vector2 size, bool trigger = false, bool body = false) where T : Component
        {
            var go = new GameObject(name) { layer = Layer(layer) };
            go.transform.position = pos;
            var c = go.AddComponent<BoxCollider2D>();
            c.size = size; c.isTrigger = trigger;
            if (body) go.AddComponent<Rigidbody2D>();
            _spawned.Add(go);
            return go.AddComponent<T>();
        }

        [UnityTest]
        public IEnumerator UsesSpendRefuseWhenEmptyAndComeBackAtTheDesk()
        {
            yield return Frames(2);
            var e = GameState.World.Equipment;
            Assert.IsTrue(e.OwnsInstrument(InstrumentKind.WaxSeal), "greybox owns every Instrument");
            Assert.IsTrue(_belt.Equip(0, InstrumentKind.WaxSeal));
            Assert.AreEqual(1, e.Slots[0].UsesLeft);
            Assert.IsTrue(_belt.Equip(1, InstrumentKind.WaxSeal), "moves to slot 1");
            Assert.AreEqual(InstrumentKind.PlumbWeight, e.Slots[0].Kind, "and the plumb weight takes slot 0");
            Assert.IsTrue(_belt.Equip(0, InstrumentKind.WaxSeal));

            InstrumentKind refused = InstrumentKind.None;
            _belt.Refused += (k, s) => refused = k;
            Assert.IsTrue(_belt.TryUse(0));
            Assert.AreEqual(0, e.Slots[0].UsesLeft);
            Assert.IsFalse(_belt.TryUse(0), "empty");
            Assert.AreEqual(InstrumentKind.WaxSeal, refused);

            // Resting at a desk restores every slot.
            var interactor = _wren.AddComponent<Interactor>();
            var desk = Spawn<DraftingDesk>("Desk", "Trigger", new Vector2(0f, 0f), new Vector2(1.8f, 1.6f), trigger: true);
            desk.Interact(interactor);
            Assert.AreEqual(1, e.Slots[0].UsesLeft, "desk restores uses");
        }

        [UnityTest]
        public IEnumerator SelectionCyclesOverThreeSlotsOrFourWithTheUpgrade()
        {
            yield return Frames(2);
            var e = GameState.World.Equipment;
            Assert.AreEqual(3, e.SlotCount);
            Assert.IsFalse(_belt.Equip(3, InstrumentKind.WaxSeal), "fourth slot locked");
            _input.PressCycleInstrument(); yield return Frames(2);
            _input.PressCycleInstrument(); yield return Frames(2);
            _input.PressCycleInstrument(); yield return Frames(2);
            Assert.AreEqual(0, e.SelectedSlot, "wraps after three");

            e.FourthSlotUnlocked = true;
            Assert.IsTrue(_belt.Equip(3, InstrumentKind.WaxSeal));
            _belt.Select(3);
            Assert.AreEqual(3, e.SelectedSlot);
            _belt.CycleSelection();
            Assert.AreEqual(0, e.SelectedSlot);
        }

        [UnityTest]
        public IEnumerator IrisTinctureFillsFivePipsAndIsRefusedWhenFull()
        {
            yield return Frames(2);
            _belt.Equip(0, InstrumentKind.IrisTincture);
            _input.PressInstrument();
            yield return Frames(2);
            Assert.AreEqual(5, _ink.Pips);
            Assert.AreEqual(1, GameState.World.Equipment.Slots[0].UsesLeft);
            _ink.Add(9);
            yield return new WaitForSeconds(0.3f);   // past the cooldown
            Assert.IsFalse(_belt.TryUse(0), "full Inkwell: keep the tincture");
            Assert.AreEqual(1, GameState.World.Equipment.Slots[0].UsesLeft);
        }

        [UnityTest]
        public IEnumerator PlumbWeightBreaksAWeakFloorThatTheQuillOnlyScratches()
        {
            var floor = Spawn<WeakFloor>("WeakFloor", "Ground", new Vector2(3.6f, 0.6f), new Vector2(3f, 0.4f));
            yield return Frames(2);
            Assert.IsFalse(floor.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }), "a quill scratch does not land");
            Assert.IsFalse(floor.IsBroken);

            _belt.Equip(0, InstrumentKind.PlumbWeight);
            Assert.IsTrue(_belt.TryUse(0));
            for (int i = 0; i < 90 && !floor.IsBroken; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(floor.IsBroken, "the weight lands on the slab and breaks it");
        }

        [UnityTest]
        public IEnumerator CompassDartHomesHitsAndMarksAnEnemy()
        {
            var smudge = Spawn<Smudge>("Smudge", "Enemy", new Vector2(5f, 1.4f), new Vector2(0.9f, 0.9f), body: true);
            smudge.ForceDrawn(true);
            yield return Frames(2);
            int hp = smudge.Health;
            _belt.Equip(0, InstrumentKind.CompassDart);
            Assert.IsTrue(_belt.TryUse(0));
            for (int i = 0; i < 60 && !smudge.IsMarked; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(smudge.IsMarked, "marked for the Inkthread");
            Assert.AreEqual(hp - 1, smudge.Health, "one damage");
            Assert.AreEqual(11, GameState.World.Equipment.Slots[0].UsesLeft);
        }

        [UnityTest]
        public IEnumerator SightingLensParryStaggersAnEnemyInsteadOfTakingDamage()
        {
            var smudge = Spawn<Smudge>("Smudge", "Enemy", new Vector2(0.3f, 0.6f), new Vector2(0.9f, 0.9f), body: true);
            smudge.ForceDrawn(true);
            _belt.Equip(0, InstrumentKind.SightingLens);
            int masks = _vitals.Masks;
            Enemy parried = null;
            _belt.Parried += en => parried = en;
            Assert.IsTrue(_belt.TryUse(0), "lens is unlimited");
            yield return Frames(3);
            Assert.AreSame(smudge, parried, "the contact was parried");
            Assert.IsTrue(smudge.IsStaggered);
            Assert.AreEqual(masks, _vitals.Masks, "no damage taken");
            Assert.IsFalse(_belt.TryUse(0), "lens is on cooldown");
        }

        [UnityTest]
        public IEnumerator FieldLanternDrawsHiddenPlatformsAndSmudges()
        {
            var platform = Spawn<HiddenPlatform>("Hidden", "Ground", new Vector2(6f, 3f), new Vector2(3f, 0.5f));
            var smudge = Spawn<Smudge>("Smudge", "Enemy", new Vector2(9f, 1.4f), new Vector2(0.9f, 0.9f), body: true);
            yield return Frames(2);
            Assert.IsFalse(platform.GetComponent<Collider2D>().enabled, "hidden: no collider");
            _belt.Equip(0, InstrumentKind.FieldLantern);
            Assert.IsTrue(_belt.TryUse(0));
            yield return Frames(2);
            Assert.IsTrue(platform.IsRevealed);
            Assert.IsTrue(platform.GetComponent<Collider2D>().enabled, "revealed: solid");
            Assert.IsTrue(smudge.IsRevealed);
            Assert.IsTrue(smudge.IsDrawn, "a revealed smudge stays drawn");
        }

        [UnityTest]
        public IEnumerator WaxSealIsAOneShotRespawnPoint()
        {
            _systems = new GameObject("Systems");
            var respawn = _systems.AddComponent<PlayerRespawn>();
            yield return Frames(2);
            _ctrl.Teleport(new Vector2(4f, 0f));
            yield return Frames(2);
            _belt.Equip(0, InstrumentKind.WaxSeal);
            Assert.IsTrue(_belt.TryUse(0));
            Assert.IsTrue(GameState.World.HasWaxSeal);
            _ctrl.Teleport(new Vector2(-6f, 0f));
            yield return Frames(2);
            bool used = false;
            respawn.WaxSealUsed += () => used = true;
            _vitals.Damage(99);
            Assert.IsTrue(_vitals.IsDead);
            yield return new WaitForSeconds(1.3f);
            Assert.IsTrue(used, "the seal was consumed");
            Assert.AreEqual(4f, _ctrl.Position.x, 0.1f, "back at the seal");
            Assert.IsFalse(GameState.World.HasWaxSeal, "one shot");
            Assert.AreEqual(_vitals.MaxMasks, _vitals.Masks);
        }

        [UnityTest]
        public IEnumerator TetherHookLeavesATemporaryAnchor()
        {
            yield return Frames(2);
            _belt.Equip(0, InstrumentKind.TetherHook);
            Assert.IsTrue(_belt.TryUse(0));
            yield return null;
            var anchors = Object.FindObjectsByType<TetherAnchor>(FindObjectsSortMode.None);
            Assert.AreEqual(1, anchors.Length);
            Assert.Greater(anchors[0].transform.position.y, _ctrl.Position.y + 2f, "anchor sits above Wren");
        }
    }
}
