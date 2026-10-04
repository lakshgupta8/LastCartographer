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
    /// Reedmother's Brood (boss sheet 6.2), built from its kit and fought to its answers: the nest is closed to the quill
    /// and opens to call a clutch of reedlings, and open it can be struck; its reeds thresh the floor either side; in
    /// the last phase the Guild's fire creeps toward the nest, and what she strikes decides the field: stamp the fire out
    /// and the Brood calms and the beds stand; kill the nest, or let the fire reach it, and the field burns and the
    /// Ferrymen's prices rise. Either way the Tether-hook is hers.
    /// </summary>
    public class ReedmotherBroodTests
    {
        GameObject _room, _wren;
        WrenController _ctrl;
        WrenVitals _vitals;
        BossKit _kit;
        ReedmotherBrood _nest;

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
            _room = new GameObject("Room_Arena");
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
            _wren.AddComponent<InstrumentBelt>();
            _vitals.SetMaxMasks(12);
            _vitals.RestoreAll();
            _ctrl.Teleport(new Vector2(-4f, 0f));

            _kit = BossKits.Build("reedmother_brood", _room.transform, Vector2.zero);
            Assert.IsNotNull(_kit, "the Brood has a kit");
            _kit.Arena.IntroSeconds = 0.05f;
            _kit.Arena.RetryIntroSeconds = 0.02f;
            _nest = (ReedmotherBrood)_kit.Boss;
        }

        [TearDown]
        public void TearDown()
        {
            if (_room != null) Object.Destroy(_room);
            if (_wren != null) Object.Destroy(_wren);
            foreach (var r in Object.FindObjectsByType<Reedling>(FindObjectsSortMode.None)) Object.Destroy(r.gameObject);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        IEnumerator StartFight(float wrenX)
        {
            _ctrl.Teleport(new Vector2(wrenX, 0f));
            yield return Until(() => _kit.Arena.State == BossArena.ArenaState.Fighting, 5f, "the doors to shut");
            Assert.AreEqual(1, _nest.Phase);
        }

        [UnityTest]
        public IEnumerator TheNestIsClosedToTheQuillAndOpensToCallAClutch()
        {
            yield return StartFight(3f);
            Assert.AreEqual("Reedmother's Brood", _nest.EnglishName);
            Assert.AreEqual(1, _nest.Tier);
            Assert.AreEqual(Tuning.BossHealth("reedmother_brood"), _nest.MaxHealth, "tuned as a windowed tier I");
            Assert.IsFalse(_nest.TakeHit(Strike(Vector2.right)), "closed, the nest turns the quill");
            Assert.AreEqual(_nest.MaxHealth, _nest.Health);

            _nest.ForceAttack(ReedmotherBrood.Attack.Brood);
            yield return Fixed(2);
            Assert.AreEqual(ReedmotherBrood.Move.Telegraph, _nest.Current);
            Assert.AreEqual("call", _nest.Clip, "the call is read before the nest opens");
            Assert.IsTrue(_nest.IsTelegraphing);
            yield return Until(() => _nest.Current == ReedmotherBrood.Move.Open, 2f, "the nest to open");
            Assert.AreEqual(1, _nest.Broods);
            Assert.AreEqual(3, _nest.LiveChicks, "a clutch of three hops out");
            Assert.IsTrue(_nest.Chicks.All(c => c.gameObject.activeSelf && c.MaxHealth == 2), "live reedlings, tuned as the table says");
            Assert.AreEqual("open", _nest.Clip);
            Assert.IsTrue(_nest.TakeHit(Strike(Vector2.right)), "open, it can be struck: the answer");
            Assert.AreEqual(_nest.MaxHealth - 1, _nest.Health);
            yield return Until(() => _nest.Current == ReedmotherBrood.Move.Closed, 3f, "the nest to close again");
            Assert.IsFalse(_nest.TakeHit(Strike(Vector2.right)), "and closed it turns the quill again");

            // A second call while the first clutch lives stops at the brood's limit.
            _nest.ForceAttack(ReedmotherBrood.Attack.Brood);
            yield return Until(() => _nest.Broods == 2, 2f, "the second call");
            Assert.LessOrEqual(_nest.LiveChicks, _nest.maxChicks, "never more than the limit at once");
        }

        [UnityTest]
        public IEnumerator TheReedsThreshTheFloorEitherSide()
        {
            yield return StartFight(5.5f);                     // beside the nest, on the floor, within the reeds' reach
            int masks = _vitals.Masks;
            _nest.ForceAttack(ReedmotherBrood.Attack.Thresh);
            yield return Fixed(2);
            Assert.AreEqual("telegraph", _nest.Clip, "the reeds draw in first");
            Assert.AreEqual(AttackKind.Strike, _nest.Kit().First(a => a.Name == "Thresh").Kind);
            Assert.GreaterOrEqual(_nest.Kit().First(a => a.Name == "Thresh").Telegraph, _nest.MinTelegraphFrames, "read within the tier's floor");
            yield return Until(() => _nest.Threshes == 1 && _nest.Current == ReedmotherBrood.Move.Thresh, 2f, "the thresh");
            Assert.AreEqual("thresh", _nest.Clip);
            yield return Until(() => _vitals.Masks < masks, 1f, "the reeds to reach her on the floor");
            Assert.AreEqual(masks - 1, _vitals.Masks, "one mask: a strike, jumped or taken");
            yield return Until(() => _nest.Current == ReedmotherBrood.Move.Closed, 2f, "the reeds to settle");
        }

        [UnityTest]
        public IEnumerator StampingTheFireOutCalmsTheBroodAndTheBedsStand()
        {
            yield return StartFight(3f);
            Assert.IsFalse(_nest.IsBurning, "no fire before the last phase");
            _nest.LightTheFire();
            yield return Fixed(2);
            Assert.IsTrue(_nest.IsBurning);
            Assert.IsNotNull(_nest.Fire);
            Assert.Greater(_nest.Fire.Position.x, _nest.transform.position.x, "the Guild's fire comes from the east edge");
            Assert.AreEqual("burn", _nest.Clip, "the nest burns in its idle");
            Assert.IsFalse(_nest.Fire.TakeHit(Strike(Vector2.right)), "a swing does nothing to a fire");
            for (int i = 0; i < _nest.stampsToOut; i++) Assert.IsTrue(_nest.Fire.TakeHit(Strike(Vector2.down)), "a stamp from above");
            Assert.IsTrue(_nest.IsCalmed, "the fire out: the Brood calms");
            Assert.IsTrue(_nest.IsDead, "and the fight is over");
            Assert.IsFalse(_nest.IsBurning);
            Assert.IsFalse(_nest.FieldBurned, "the beds stand");
            Assert.IsFalse(Economy.IrisBurned(GameState.World), "and the Ferrymen's prices hold");
            Assert.IsTrue(GameState.World.Equipment.OwnsInstrument(InstrumentKind.TetherHook), "the Tether-hook is hers");
            Assert.AreEqual("calm", _nest.Clip, "the nest settles rather than burns");
            Assert.AreEqual(0, _nest.LiveChicks, "the brood is gone");
            yield return Until(() => _kit.Arena.State == BossArena.ArenaState.Won, 3f, "the arena to open");
            Assert.IsTrue(GameState.World.Is(Bosses.FlagKey("reedmother_brood")), "the Brood counts as met either way");
        }

        [UnityTest]
        public IEnumerator TheFireReachingTheNestBurnsTheFieldAndTheFerrymensPricesRise()
        {
            yield return StartFight(3f);
            _nest.fireStepSeconds = 0.1f;
            _nest.LightTheFire();
            float start = _nest.Fire.Position.x;
            yield return Until(() => _nest.FireStep >= 1, 1f, "the fire's first step");
            Assert.Less(_nest.Fire.Position.x, start, "it creeps toward the nest");
            yield return Until(() => _nest.IsDead, 3f, "the fire to reach the nest");
            Assert.IsTrue(_nest.FieldBurned, "the field burns");
            Assert.IsFalse(_nest.IsCalmed);
            Assert.IsTrue(Economy.IrisBurned(GameState.World), "the Ferrymen's prices rise by half");
            var hook = Economy.Find("Saltmarrow", InstrumentKind.TetherHook);
            Assert.IsNotNull(hook);
            Assert.AreEqual(Mathf.CeilToInt(hook.Price * Economy.BurnedMarkup), Economy.PriceOf(GameState.World, hook), "the hook on the board is dearer now");
            Assert.AreEqual("death", _nest.Clip);
            Assert.IsTrue(GameState.World.Equipment.OwnsInstrument(InstrumentKind.TetherHook), "but she has one anyway");
        }

        [UnityTest]
        public IEnumerator KillingTheOpenNestBurnsTheFieldToo()
        {
            yield return StartFight(3f);
            while (!_nest.IsDead)
            {
                _nest.ForceAttack(ReedmotherBrood.Attack.Brood);
                yield return Until(() => _nest.Current == ReedmotherBrood.Move.Open || _nest.IsDead, 2f, "the nest to open");
                if (_nest.IsDead) break;
                Assert.IsTrue(_nest.TakeHit(Strike(Vector2.right)));
                if (_nest.Phase >= 3 && !_nest.IsDead) Assert.IsTrue(_nest.IsBurning, "the fire arrives with the last phase (and goes with the nest)");
            }
            Assert.IsTrue(_nest.FieldBurned, "killed, the nest and the field burn");
            Assert.IsTrue(Economy.IrisBurned(GameState.World));
            Assert.AreEqual(3, _nest.Phase, "all three lines were heard");
        }
    }
}
