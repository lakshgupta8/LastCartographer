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
    /// Halvard's first hunt (CMB-12, boss sheet 6.3): the thrust lands after its telegraph, the survey leaves marks,
    /// the count erupts them, phase 3 marks the floor but one pace, and at zero he withdraws with the flag and a scrap.
    /// Also the tide under the chapel gap: a fall costs a mask and returns her to the bank.
    /// </summary>
    public class HalvardFightTests
    {
        GameObject _floor, _wren, _room, _arenaGo, _bossGo, _doorW, _doorE;
        WrenController _ctrl;
        WrenVitals _vitals;
        BossArena _arena;
        Halvard _boss;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

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
            _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = new ScriptedInput();
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _ctrl.Teleport(new Vector2(-12f, 0f));

            _room = new GameObject("Room_Chapel");
            _room.AddComponent<Room>();
            _doorW = Door("Door_W", new Vector2(-1f, 3f));
            _doorE = Door("Door_E", new Vector2(15f, 3f));

            _bossGo = new GameObject("Halvard") { layer = Layer("Enemy") };
            _bossGo.transform.SetParent(_room.transform, false);
            _bossGo.transform.position = new Vector3(11f, 0.9f, 0f);
            _bossGo.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.8f);
            _bossGo.AddComponent<Rigidbody2D>();
            _boss = _bossGo.AddComponent<Halvard>();
            _boss.SetMaxHealth(30);
            _boss.arenaMinX = 0.5f; _boss.arenaMaxX = 13.5f;

            _arenaGo = new GameObject("Arena") { layer = Layer("Trigger") };
            _arenaGo.transform.position = new Vector3(7f, 5f, 0f);
            var zone = _arenaGo.AddComponent<BoxCollider2D>();
            zone.isTrigger = true; zone.size = new Vector2(16f, 11f);
            _arena = _arenaGo.AddComponent<BossArena>();
            _arena.Configure(_boss, new[] { _doorW, _doorE }, "halvard", Ability.None);
            _arena.IntroSeconds = 0.05f;
            _arena.RetryIntroSeconds = 0.02f;
        }

        GameObject Door(string name, Vector2 pos)
        {
            var d = new GameObject(name) { layer = Layer("Ground") };
            d.transform.position = pos;
            d.AddComponent<BoxCollider2D>().size = new Vector2(1f, 6f);
            d.SetActive(false);
            return d;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var m in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (m.name == "Mark") Object.Destroy(m.gameObject);
            foreach (var go in new[] { _arenaGo, _bossGo, _doorW, _doorE, _room, _wren, _floor }) if (go != null) Object.Destroy(go);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        IEnumerator StartFight(float wrenX = 4f)
        {
            _ctrl.Teleport(new Vector2(wrenX, 0f));
            float t = 0f;
            while (_arena.State != BossArena.ArenaState.Fighting && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(BossArena.ArenaState.Fighting, _arena.State, "the doors shut and the fight begins");
            Assert.IsTrue(_boss.IsFightActive);
            Assert.AreEqual(1, _boss.Phase);
        }

        IEnumerator UntilNotInvulnerable()
        {
            float t = 0f;
            while (_vitals.IsInvulnerable && t < 3f) { t += Time.deltaTime; yield return null; }
        }

        [UnityTest]
        public IEnumerator TheThrustLandsAfterItsTelegraphAndTheLungeCloses()
        {
            yield return StartFight(9.5f);   // a pace and a half in front of him
            int masks = _vitals.Masks;
            _boss.ForceAttack(Halvard.Attack.Thrust);
            yield return Fixed(2);
            Assert.IsTrue(_boss.IsTelegraphing, "the lance lowers first");
            Assert.GreaterOrEqual(_boss.TelegraphLeft, _boss.MinTelegraphFrames - 2);
            for (int i = 0; i < 60 && _boss.Thrusts == 0; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, _boss.Thrusts);
            yield return Fixed(8);
            Assert.AreEqual(masks - 1, _vitals.Masks, "the thrust lands");
            Assert.AreEqual(-1, _boss.Dir, "he faces her");

            // Far away: the lunge covers the ground.
            yield return UntilNotInvulnerable();
            _ctrl.Teleport(new Vector2(3f, 0f));
            float x0 = _boss.transform.position.x;
            _boss.ForceAttack(Halvard.Attack.Lunge);
            for (int i = 0; i < 80 && _boss.Current != Halvard.Move.Recover; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, _boss.Lunges);
            Assert.Less(_boss.transform.position.x, x0 - 3f, "he lunged toward her");
            Assert.GreaterOrEqual(_boss.transform.position.x, _boss.arenaMinX, "and stays in the arena");
        }

        [UnityTest]
        public IEnumerator TheSurveyMarksTheFloorAndTheCountEruptsIt()
        {
            yield return StartFight(4f);
            Assert.IsEmpty(_boss.Marks);
            _boss.ForceAttack(Halvard.Attack.Survey);
            for (int i = 0; i < 80 && _boss.Surveys == 0; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, _boss.Marks.Count, "phase 1: one mark");
            Assert.AreEqual(4f, _boss.Marks[0], 0.05f, "under her feet");
            int masks = _vitals.Masks;
            yield return Fixed(30);
            Assert.AreEqual(masks, _vitals.Masks, "a mark is only a mark");

            // Standing on it when he calls the count.
            yield return UntilNotInvulnerable();
            _boss.ForceAttack(Halvard.Attack.Count);
            for (int i = 0; i < 80 && _boss.Counts == 0; i++) yield return new WaitForFixedUpdate();
            yield return Fixed(3);
            Assert.AreEqual(masks - 1, _vitals.Masks, "the mark erupts under her");
            for (int i = 0; i < 40 && _boss.Marks.Count > 0; i++) yield return new WaitForFixedUpdate();
            Assert.IsEmpty(_boss.Marks, "the count clears the floor");

            // Off the marks, the count is harmless.
            yield return UntilNotInvulnerable();
            _ctrl.Teleport(new Vector2(4f, 0f));
            _boss.ForceAttack(Halvard.Attack.Survey);
            for (int i = 0; i < 80 && _boss.Surveys < 2; i++) yield return new WaitForFixedUpdate();
            _ctrl.Teleport(new Vector2(8f, 0f));
            masks = _vitals.Masks;
            _boss.ForceAttack(Halvard.Attack.Count);
            for (int i = 0; i < 80 && _boss.Counts < 2; i++) yield return new WaitForFixedUpdate();
            yield return Fixed(12);
            Assert.AreEqual(masks, _vitals.Masks, "a pace away is safe");
        }

        [UnityTest]
        public IEnumerator PhaseThreeMarksTheFloorButOnePaceAndZeroIsAWithdrawal()
        {
            yield return StartFight(4f);
            for (int i = 0; i < 10; i++) Assert.IsTrue(_boss.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }), "always hittable");
            Assert.AreEqual(2, _boss.Phase);
            for (int i = 0; i < 10; i++) _boss.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right });
            Assert.AreEqual(3, _boss.Phase, "a third left");
            Assert.GreaterOrEqual(_boss.Marks.Count, 5, "the floor is marked");
            foreach (var m in _boss.Marks) Assert.Greater(Mathf.Abs(m - 4f), _boss.markWidth * 1.5f, "but the pace she stands in");
            Assert.IsTrue(_boss.Marks.All(m => m >= _boss.arenaMinX && m <= _boss.arenaMaxX));

            bool won = false;
            BossArena.FightWon += a => won = true;
            var w = GameState.World;
            w.Numbers.TryGetValue("$vellum_scraps", out var scrapsBefore);
            for (int i = 0; i < 10; i++) _boss.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right });
            Assert.IsTrue(_boss.IsDead);
            Assert.IsTrue(won, "the arena calls it");
            Assert.AreEqual(BossArena.ArenaState.Won, _arena.State);
            Assert.IsTrue(w.Is("boss.halvard.defeated"), "the flag the sheet names");
            Assert.AreEqual(scrapsBefore + 1, w.Numbers["$vellum_scraps"]);
            Assert.IsEmpty(_boss.Marks, "the marks go with him");
            Assert.IsFalse(_doorW.activeSelf, "the doors open");
        }

        [UnityTest]
        public IEnumerator TheTideCostsAMaskAndReturnsHerToTheBank()
        {
            var tideGo = new GameObject("Tide") { layer = Layer("Trigger") };
            tideGo.transform.position = new Vector3(-7.5f, -3f, 0f);
            var col = tideGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true; col.size = new Vector2(9f, 3f);
            var tide = tideGo.AddComponent<Tide>();
            tide.WestBank = new Vector2(-12.6f, 0f); tide.EastBank = new Vector2(-2.4f, 0f);
            _floor.transform.position = new Vector3(0f, -6f, 0f);   // no floor under the gap
            int masks = _vitals.Masks;
            yield return UntilNotInvulnerable();
            _ctrl.Teleport(new Vector2(-9f, 0.5f));
            float t = 0f;
            while (tide.Sweeps == 0 && t < 5f) { t += Time.deltaTime; yield return new WaitForFixedUpdate(); }
            Assert.AreEqual(1, tide.Sweeps, "she fell in");
            Assert.AreEqual(masks - 1, _vitals.Masks, "a mask, not a death");
            Assert.AreEqual(-12.6f, _ctrl.Position.x, 0.05f, "back on the near bank");
            Object.Destroy(tideGo);
        }
    }
}
