using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>Enemy framework rules from the combat doc: answers, hurt, death, contact damage, respawn.</summary>
    public class EnemyTests
    {
        GameObject _floor, _wren, _enemy, _room;
        WrenController _ctrl;
        WrenVitals _vitals;
        ScriptedInput _input;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _ctrl.Teleport(new Vector2(0f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _enemy, _wren, _floor, _room }) if (go != null) Object.Destroy(go);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        T MakeEnemy<T>(Vector2 pos) where T : Enemy
        {
            _enemy = new GameObject(typeof(T).Name) { layer = Layer("Enemy") };
            _enemy.transform.position = pos;
            _enemy.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
            _enemy.AddComponent<Rigidbody2D>();
            return _enemy.AddComponent<T>();
        }

        [UnityTest]
        public IEnumerator CrabIgnoresSideHitsAndDiesToPogo()
        {
            var crab = MakeEnemy<MarshCrab>(new Vector2(6f, 0.5f));
            yield return Frames(2);
            int hp = crab.Health;
            Assert.IsFalse(crab.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }), "side hit bounces off the shell");
            Assert.AreEqual(hp, crab.Health);
            bool died = false;
            crab.Died += _ => died = true;
            for (int i = 0; i < hp; i++)
            {
                Assert.IsTrue(crab.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.down }), "down-strike lands");
                yield return Frames(8);   // hurtstun; hits are per-swing anyway
            }
            Assert.IsTrue(died, "crab dies after " + hp + " pogo hits");
            Assert.IsTrue(crab.IsDead);
        }

        [UnityTest]
        public IEnumerator SmudgeIsOnlyHittableWhenDrawn()
        {
            var smudge = MakeEnemy<Smudge>(new Vector2(6f, 2f));
            yield return Frames(2);
            smudge.ForceDrawn(false);
            Assert.IsFalse(smudge.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }), "undrawn: quill passes through");
            smudge.ForceDrawn(true);
            int hp = smudge.Health;
            Assert.IsTrue(smudge.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }), "drawn: hit lands");
            Assert.AreEqual(hp - 1, smudge.Health);
        }

        [UnityTest]
        public IEnumerator ContactDamagesWrenOncePerInvulnerabilityWindow()
        {
            // Skimmer with no gravity parked on top of Wren: constant contact.
            int masks = _vitals.Masks;
            MakeEnemy<ReedSkimmer>(new Vector2(0f, 0.6f));
            yield return Frames(8);
            Assert.AreEqual(masks - 1, _vitals.Masks, "first contact costs one mask");
            Assert.IsTrue(_vitals.IsInvulnerable);
            yield return Frames(20);
            Assert.AreEqual(masks - 1, _vitals.Masks, "invulnerability frames block repeat damage");
        }

        [UnityTest]
        public IEnumerator DeathRespawnsAtDeskSpawnWithFullMasks()
        {
            _room = new GameObject("Room_Test");
            var room = _room.AddComponent<Room>();
            var desk = new GameObject("Spawn_Desk");
            desk.transform.SetParent(_room.transform, false);
            desk.transform.position = new Vector3(-8f, 0f, 0f);
            GameState.World.RespawnSpawn = "Desk";

            var respawnGo = new GameObject("Respawn");
            var respawn = respawnGo.AddComponent<PlayerRespawn>();
            yield return null;   // let Start bind to vitals

            bool respawned = false;
            respawn.Respawned += () => respawned = true;
            for (int i = 0; i < _vitals.MaxMasks - 1; i++)
            {
                Assert.IsTrue(_vitals.Damage(1), "hit " + i + " should land");
                yield return Frames(62);           // outlast the 60 invulnerability frames
            }
            Assert.IsTrue(_vitals.Damage(1), "final hit should land");
            Assert.IsTrue(_vitals.IsDead, "five hits should kill");
            yield return null;
            Assert.IsTrue(_ctrl.Frozen, "frozen while dead");

            float t = 0f;
            while (!respawned && t < 4f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(respawned, "respawn should fire");
            Assert.AreEqual(_vitals.MaxMasks, _vitals.Masks);
            Assert.AreEqual(-8f, _ctrl.Position.x, 0.05f, "back at the desk spawn");
            Assert.IsFalse(_ctrl.Frozen);
            Object.Destroy(respawnGo);
        }
    }
}
