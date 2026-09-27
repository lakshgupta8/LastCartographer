using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>Combat doc 8 and CMB-10/11: arena lock, phases and lines, rewards, retry loop, the Lamp-Keeper's answer.</summary>
    public class BossTests
    {
        GameObject _floor, _wren, _room, _arenaGo, _bossGo, _systems;
        readonly List<GameObject> _spawned = new List<GameObject>();
        WrenController _ctrl;
        WrenVitals _vitals;
        AbilitySet _abilities;
        ScriptedInput _input;
        BossArena _arena;
        LampKeeper _boss;
        GameObject _doorW, _doorE;

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
            _wren.AddComponent<Inkwell>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _ctrl.Teleport(new Vector2(-12f, 0f));   // outside the arena

            // Room with a Start spawn by the desk, for respawns.
            _room = new GameObject("Room_Test");
            _room.AddComponent<Room>();
            var start = new GameObject("Spawn_Start");
            start.transform.SetParent(_room.transform, false);
            start.transform.position = new Vector3(-12f, 0f, 0f);

            _doorW = Door("Door_W", new Vector2(-6.5f, 3f));
            _doorE = Door("Door_E", new Vector2(12.5f, 3f));

            _bossGo = new GameObject("LampKeeper") { layer = Layer("Enemy") };
            _bossGo.transform.position = new Vector3(3f, 8.6f, 0f);
            _bossGo.AddComponent<BoxCollider2D>().size = new Vector2(1.6f, 1.2f);
            _bossGo.AddComponent<Rigidbody2D>();
            _boss = _bossGo.AddComponent<LampKeeper>();
            _boss.SetMaxHealth(24);

            _arenaGo = new GameObject("Arena") { layer = Layer("Trigger") };
            _arenaGo.transform.position = new Vector3(3f, 5f, 0f);
            var zone = _arenaGo.AddComponent<BoxCollider2D>();
            zone.isTrigger = true; zone.size = new Vector2(17f, 11f);
            _arena = _arenaGo.AddComponent<BossArena>();
            _arena.Configure(_boss, new[] { _doorW, _doorE }, "lamp_keeper", Ability.Wingbeat);
            _arena.IntroSeconds = 0f;
            _arena.RetryIntroSeconds = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var g in _spawned) if (g != null) Object.Destroy(g);
            _spawned.Clear();
            foreach (var go in new[] { _arenaGo, _bossGo, _doorW, _doorE, _room, _systems, _wren, _floor }) if (go != null) Object.Destroy(go);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        GameObject Door(string name, Vector2 pos)
        {
            var d = new GameObject(name) { layer = Layer("Ground") };
            d.transform.position = pos;
            d.AddComponent<BoxCollider2D>().size = new Vector2(1f, 6f);
            d.SetActive(false);
            return d;
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        IEnumerator EnterArena()
        {
            _ctrl.Teleport(new Vector2(0f, 0f));
            for (int i = 0; i < 30 && !_boss.IsFightActive; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(_boss.IsFightActive, "the arena starts the fight when Wren steps in");
        }

        IEnumerator HitBoss(int times)
        {
            for (int i = 0; i < times; i++)
            {
                _boss.ForceGrounded(5f);
                Assert.IsTrue(_boss.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }), "grounded: hit " + i + " lands");
                yield return Frames(4);   // hurtstun
            }
        }

        [UnityTest]
        public IEnumerator EnteringLocksTheDoorsAndOpensPhaseOneWithALine()
        {
            yield return Frames(3);
            Assert.IsFalse(_doorW.activeSelf, "doors open before the fight");
            Assert.IsFalse(_boss.IsFightActive, "dormant on the perch");
            int phase = 0; string line = null;
            _boss.PhaseStarted += (b, p, l) => { phase = p; line = l; };
            yield return EnterArena();
            Assert.IsTrue(_boss.IsFightActive);
            Assert.AreEqual(BossArena.ArenaState.Fighting, _arena.State);
            Assert.IsTrue(_doorW.activeSelf && _doorE.activeSelf, "doors close");
            Assert.AreEqual(1, phase);
            Assert.AreEqual(_boss.PhaseLine(1), line);
            Assert.AreEqual(3, _boss.PhaseCount);
        }

        [UnityTest]
        public IEnumerator PhasesAdvanceOnHealthThirds()
        {
            yield return Frames(3);
            yield return EnterArena();
            var phases = new List<int>();
            _boss.PhaseStarted += (b, p, l) => phases.Add(p);
            int third = _boss.MaxHealth / 3;
            yield return HitBoss(third - 1);
            Assert.AreEqual(1, _boss.Phase, "still phase 1 just above two thirds");
            yield return HitBoss(1);
            Assert.AreEqual(2, _boss.Phase, "two thirds: phase 2");
            yield return HitBoss(third);
            Assert.AreEqual(3, _boss.Phase, "one third: phase 3");
            CollectionAssert.AreEqual(new[] { 2, 3 }, phases);
        }

        [UnityTest]
        public IEnumerator DefeatOpensDoorsFlagsAndGrantsWingbeat()
        {
            yield return Frames(3);
            Assert.IsFalse(_abilities.Has(Ability.Wingbeat));
            yield return EnterArena();
            bool won = false;
            BossArena.FightWon += a => won = true;
            yield return HitBoss(_boss.MaxHealth);
            Assert.IsTrue(_boss.IsDead);
            Assert.IsTrue(won);
            Assert.AreEqual(BossArena.ArenaState.Won, _arena.State);
            Assert.IsFalse(_doorW.activeSelf, "doors open again");
            Assert.IsTrue(_abilities.Has(Ability.Wingbeat), "reward");
            Assert.IsTrue(GameState.World.Is("boss.lamp_keeper.defeated"));
            Assert.AreEqual(1f, GameState.World.Numbers["$vellum_scraps"], 0.001f);
            yield return new WaitForSeconds(0.4f);   // death shrink
            Assert.IsFalse(_bossGo.activeSelf, "a defeated boss is put away, not destroyed");

            // Walking back in does nothing now.
            _ctrl.Teleport(new Vector2(-12f, 0f));
            yield return Frames(2);
            _ctrl.Teleport(new Vector2(0f, 0f));
            yield return Frames(5);
            Assert.AreEqual(BossArena.ArenaState.Won, _arena.State);
            Assert.IsFalse(_doorW.activeSelf);
        }

        [UnityTest]
        public IEnumerator WrenDeathResetsTheFightAndTheRetryIsUnderEightSeconds()
        {
            _systems = new GameObject("Systems");
            _systems.AddComponent<PlayerRespawn>();
            yield return Frames(3);
            yield return EnterArena();
            yield return HitBoss(_boss.MaxHealth / 3 + 1);
            Assert.AreEqual(2, _boss.Phase);
            bool reset = false;
            BossArena.FightReset += a => reset = true;

            float t0 = Time.time;
            _vitals.Damage(99);
            yield return null;
            Assert.IsTrue(reset, "the fight resets on Wren's death");
            Assert.AreEqual(BossArena.ArenaState.Idle, _arena.State);
            Assert.IsFalse(_doorW.activeSelf, "doors open for the walk back");
            Assert.AreEqual(_boss.MaxHealth, _boss.Health, "boss back to full");
            Assert.AreEqual(0, _boss.Phase);
            Assert.IsTrue(_bossGo.activeSelf);
            Assert.AreEqual(8.6f, _boss.transform.position.y, 0.05f, "back on the perch");
            yield return Frames(6);
            Assert.AreEqual(BossArena.ArenaState.Idle, _arena.State, "no restart over Wren's body");

            // Respawn at the desk, walk back in (teleport stands in for the ~2 s walk).
            for (float t = 0f; _ctrl.Frozen && t < 3f; t += Time.deltaTime) yield return null;
            Assert.AreEqual(-12f, _ctrl.Position.x, 0.1f, "respawned by the desk");
            yield return new WaitForSeconds(2f);
            yield return EnterArena();
            Assert.IsTrue(_boss.IsFightActive, "second fight starts");
            Assert.AreEqual(1, _boss.Phase);
            Assert.Less(Time.time - t0, 8f, "retry loop under 8 s");
        }

        [UnityTest]
        public IEnumerator DiveIsTelegraphedAndSheIsOnlyOpenWhileGrounded()
        {
            yield return Frames(3);
            yield return EnterArena();
            Assert.IsFalse(_boss.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.up }), "on the perch: out of reach");
            _boss.ForceAttack(LampKeeper.Attack.Dive);
            int telegraphFrames = 0;
            for (int i = 0; i < 120 && _boss.Current != LampKeeper.Move.Dive; i++)
            {
                yield return new WaitForFixedUpdate();
                if (_boss.IsTelegraphing) telegraphFrames++;
                Assert.IsFalse(_boss.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.up }), "telegraph: still closed");
            }
            Assert.AreEqual(LampKeeper.Move.Dive, _boss.Current, "the dive follows the telegraph");
            Assert.GreaterOrEqual(telegraphFrames, _boss.MinTelegraphFrames, "Tier I telegraph is 12+ frames");
            for (int i = 0; i < 120 && _boss.Current == LampKeeper.Move.Dive; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(LampKeeper.Move.Grounded, _boss.Current, "she lands");
            Assert.Less(_boss.transform.position.y, 1.5f, "on the floor");
            int hp = _boss.Health;
            Assert.IsTrue(_boss.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }), "grounded: open");
            Assert.AreEqual(hp - 1, _boss.Health);
            for (int i = 0; i < 200 && _boss.Current == LampKeeper.Move.Grounded; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(LampKeeper.Move.Return, _boss.Current, "then she returns to the perch");
        }

        [UnityTest]
        public IEnumerator BeamSweepsTheFloorAndAJumpClearsIt()
        {
            yield return Frames(3);
            yield return EnterArena();
            int masks = _vitals.Masks;
            _boss.ForceAttack(LampKeeper.Attack.Beam);
            for (int i = 0; i < 300 && _boss.Current != LampKeeper.Move.Perch; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(masks - 1, _vitals.Masks, "standing on the floor: the beam lands once");

            // A ledge above the beam's reach: safe.
            var ledge = new GameObject("Ledge") { layer = Layer("Ground") };
            ledge.AddComponent<BoxCollider2D>().size = new Vector2(3f, 0.5f);
            ledge.transform.position = new Vector3(0f, 2.75f, 0f);
            _spawned.Add(ledge);
            _ctrl.Teleport(new Vector2(0f, 3f));
            yield return Frames(70);   // outlast invulnerability
            masks = _vitals.Masks;
            _boss.ForceAttack(LampKeeper.Attack.Beam);
            for (int i = 0; i < 300 && _boss.Current != LampKeeper.Move.Perch; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(masks, _vitals.Masks, "above the beam: untouched");
        }
    }
}
