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
    /// Guildmaster Voss at the Threshold (CMB-15, boss sheet 6.11): lance and shield by the book, then the arena
    /// anchored (the grade locks, seals hold her, a strike on a seal's edge breaks it), then the Blank eating the floor
    /// from the west down to his island.
    /// </summary>
    public class VossFightTests
    {
        GameObject _room, _wren;
        WrenController _ctrl;
        WrenVitals _vitals;
        InstrumentBelt _belt;
        BossKit _kit;
        Voss _voss;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        static HitInfo Strike(Vector2 dir) => new HitInfo { Damage = 1, Direction = dir };

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            HeldState.LockGrade(0f);
            _room = new GameObject("Room_Threshold");
            _room.AddComponent<Room>();

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = new ScriptedInput();
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _belt = _wren.AddComponent<InstrumentBelt>();
            _vitals.SetMaxMasks(12);
            _vitals.RestoreAll();
            _ctrl.Teleport(new Vector2(-4f, 0f));

            _kit = BossKits.Build("voss", _room.transform, Vector2.zero);
            _kit.Arena.IntroSeconds = 0.05f;
            _kit.Arena.RetryIntroSeconds = 0.02f;
            _voss = (Voss)_kit.Boss;
            _voss.standSeconds = 999f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_room != null) Object.Destroy(_room);
            if (_wren != null) Object.Destroy(_wren);
            HeldState.LockGrade(0f);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        IEnumerator StartFight(float wrenX)
        {
            _ctrl.Teleport(new Vector2(wrenX, 0f));
            yield return Until(() => _kit.Arena.State == BossArena.ArenaState.Fighting, 5f, "the doors to shut");
            Assert.AreEqual(1, _voss.Phase);
        }

        IEnumerator Recovered()
        {
            yield return Until(() => !_vitals.IsInvulnerable, 3f, "her i-frames to end");
            _vitals.RestoreAll();
        }

        void Hits(int n, Vector2 dir) { for (int i = 0; i < n; i++) _voss.TakeHit(Strike(dir)); }

        [Test]
        public void HisKitIsOnHisSheetAndFourAttacksAPhase()
        {
            var sheet = Bosses.Find("voss");
            Assert.AreEqual(sheet.Name, _voss.BossName);
            Assert.AreEqual("Hold. Everything holds. Do you see? Nothing is lost.", _voss.PhaseLine(2));
            Assert.AreEqual("Greyfold.Threshold", sheet.Zone);
            for (int p = 1; p <= 3; p++) Assert.LessOrEqual(Voss.PatternFor(p).Distinct().Count(), 4);
            Assert.IsFalse(Voss.PatternFor(1).Contains(Voss.Attack.Anchor), "phase 1 is textbook");
            Assert.IsTrue(Voss.PatternFor(2).Contains(Voss.Attack.Anchor), "phase 2 anchors");
        }

        [UnityTest]
        public IEnumerator LanceAndShieldByTheBook()
        {
            float front = _voss.transform.position.x - 1.5f;
            yield return StartFight(front);
            int masks = _vitals.Masks;
            _voss.ForceAttack(Voss.Attack.Thrust);
            yield return Fixed(2);
            Assert.GreaterOrEqual(_voss.TelegraphLeft, _voss.MinTelegraphFrames - 2, "the lance lowers first");
            yield return Until(() => _voss.Thrusts == 1 && _voss.Current == Voss.Move.Recover, 2f, "the thrust");
            Assert.AreEqual(masks - 1, _vitals.Masks, "it lands");

            yield return Recovered();
            _belt.Equip(0, InstrumentKind.SightingLens);
            _voss.ForceAttack(Voss.Attack.Thrust);
            yield return Fixed(2);
            yield return Until(() => _voss.TelegraphLeft <= 2, 2f, "the thrust to come");
            Assert.IsTrue(_belt.TryUse(0));
            yield return Until(() => _voss.Parried == 1, 1f, "the parry");
            Assert.IsTrue(_voss.IsStaggered, "parry the lance");
            Assert.AreEqual(masks, _vitals.Masks);

            yield return Until(() => !_voss.IsStaggered && _voss.Current == Voss.Move.Stand, 3f, "him to stand");
            _voss.ForceAttack(Voss.Attack.Guard);
            yield return Until(() => _voss.IsGuarding, 2f, "the shield");
            Assert.IsFalse(_voss.TakeHit(Strike(Vector2.right)), "the compass rose turns the quill");
            Assert.IsTrue(_voss.TakeHit(Strike(Vector2.down)), "over the shield");
            _ctrl.Teleport(new Vector2(_voss.transform.position.x + 2f, 0f));
            yield return Fixed(2);
            Assert.IsTrue(_voss.TakeHit(Strike(Vector2.left)), "behind him");
        }

        [UnityTest]
        public IEnumerator HeAnchorsTheArenaAndASealHoldsHer()
        {
            float x = _voss.SectionCentre(4) - 0.2f;
            yield return StartFight(x);
            Assert.AreEqual(0f, HeldState.CurrentLevel, 0.001f);
            Hits(14, Vector2.right);
            Assert.AreEqual(2, _voss.Phase);
            Assert.AreEqual(1f, HeldState.CurrentLevel, 0.001f, "he anchors: the grade locks");

            _voss.ForceAttack(Voss.Attack.Anchor);
            Assert.AreEqual(4, _voss.AnchorTarget, "the compass rose is drawn where she stands");
            yield return Until(() => _voss.Anchors == 1, 2f, "the seal");
            Assert.IsTrue(_voss.IsSealed(4));
            Assert.IsTrue(_voss.IsHoldingWren && _ctrl.Frozen, "sealed with her inside: held");
            Assert.AreEqual(1, _voss.Holds);
            yield return Until(() => !_voss.IsHoldingWren, 2f, "the beat to pass");
            Assert.IsFalse(_ctrl.Frozen, "for a beat");
            yield return Fixed(20);
            Assert.AreEqual(1, _voss.Holds, "standing still inside, she is not held again");

            var seal = _voss.Seals[4];
            Assert.IsFalse(seal.TakeHit(Strike(Vector2.left)), "from inside, the quill only rings on it");
            _ctrl.Teleport(new Vector2(_voss.SectionCentre(3) + _voss.SectionWidth * 0.5f - 0.3f, 0f));
            yield return Fixed(2);
            Assert.IsTrue(seal.TakeHit(Strike(Vector2.right)), "its edge, from outside");
            Assert.AreEqual(1, _voss.SealsBroken);
            Assert.IsFalse(_voss.IsSealed(4));

            _voss.ForceAttack(Voss.Attack.Anchor);
            yield return Until(() => _voss.Anchors == 2, 2f, "the next seal");
            Assert.IsTrue(_voss.IsSealed(3));
            Assert.AreEqual(2, _voss.Holds);
            yield return Until(() => !_voss.IsHoldingWren, 2f, "the beat");
            _ctrl.Teleport(new Vector2(_voss.SectionCentre(2), 0f));
            yield return Fixed(40);
            _ctrl.Teleport(new Vector2(_voss.SectionCentre(3), 0f));
            yield return Fixed(3);
            Assert.AreEqual(3, _voss.Holds, "walking back into a held section holds her again");
        }

        [UnityTest]
        public IEnumerator TheBlankEatsTheWestDownToHisIsland()
        {
            yield return StartFight(_voss.SectionCentre(0));
            Hits(14, Vector2.right);
            Assert.AreEqual(2, _voss.Phase);
            _voss.ForceAttack(Voss.Attack.Anchor);
            yield return Until(() => _voss.Anchors == 1, 2f, "a seal in the west");
            Assert.IsTrue(_voss.IsSealed(0));
            yield return Until(() => !_voss.IsHoldingWren, 2f, "the beat");
            _ctrl.Teleport(new Vector2(_voss.SectionCentre(4) - 0.2f, 0f));
            yield return Fixed(3);

            Hits(13, Vector2.right);
            Assert.AreEqual(3, _voss.Phase);
            Assert.AreEqual(_voss.arenaMinX, _voss.EdgeX, 0.001f);
            yield return Until(() => _voss.SectionsEaten == 1, 3f, "the white to take the first section");
            Assert.Greater(_voss.EdgeX, _voss.arenaMinX);
            Assert.IsFalse(_voss.IsSealed(0), "seals and all");
            Assert.AreEqual(1, _voss.SealsEaten);

            yield return Recovered();
            int masks = _vitals.Masks;
            _ctrl.Teleport(new Vector2(_voss.SectionCentre(0), 0f));
            yield return Until(() => _voss.WhiteBurns >= 1, 1f, "the white to hurt");
            Assert.Less(_vitals.Masks, masks, "stay off the west");
            _ctrl.Teleport(new Vector2(_voss.SectionCentre(4) - 0.2f, 0f));

            int island = _voss.sections - _voss.islandSections;
            yield return Until(() => _voss.SectionsEaten == island, 8f, "the white to reach his island");
            yield return new WaitForSeconds(_voss.eatSeconds + 0.3f);
            Assert.AreEqual(island, _voss.SectionsEaten, "and stop there");
            Assert.AreEqual(_voss.arenaMinX + island * _voss.SectionWidth, _voss.EdgeX, 0.01f);
            Assert.GreaterOrEqual(_voss.transform.position.x, _voss.EdgeX, "he holds a shrinking island");

            var w = GameState.World;
            w.Numbers.TryGetValue("$vellum_scraps", out var scraps);
            while (!_voss.IsDead) _voss.TakeHit(Strike(Vector2.right));
            Assert.IsTrue(w.Is(Bosses.FlagKey("voss")), "the flag Threshold_Voss reads");
            Assert.AreEqual(scraps + 3, w.Numbers["$vellum_scraps"]);
            Assert.AreEqual(BossArena.ArenaState.Won, _kit.Arena.State);
            Assert.AreEqual(0f, HeldState.CurrentLevel, 0.001f, "the grade comes free");
            Assert.IsFalse(_ctrl.Frozen);
            Assert.AreEqual(0, _voss.Seals.Count);
        }

        [UnityTest]
        public IEnumerator ARetryUnsealsTheArenaAndFreesHer()
        {
            yield return StartFight(_voss.SectionCentre(4) - 0.2f);
            Hits(14, Vector2.right);
            _voss.ForceAttack(Voss.Attack.Anchor);
            yield return Until(() => _voss.IsHoldingWren, 2f, "the hold");
            _kit.Arena.ResetFight();
            Assert.IsFalse(_ctrl.Frozen, "a retry never leaves her held");
            Assert.AreEqual(0, _voss.Seals.Count);
            Assert.AreEqual(0f, HeldState.CurrentLevel, 0.001f);
            Assert.AreEqual(_voss.MaxHealth, _voss.Health);
        }
    }
}
