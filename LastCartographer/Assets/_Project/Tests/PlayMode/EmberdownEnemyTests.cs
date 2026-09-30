using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The highland's creatures (CMB-09, rooms doc §3 "pogo the salamanders, strike the bats as they dive"): the cave-bat
    /// hangs until Wren is under it, tells, swoops one arc through her and up the far side, and returns to its roost,
    /// dying to any hit; the salamander patrols its ledge, tells, rushes low and stops at the edge, cools, and turns
    /// away everything but a pogo. Both take their numbers from the tuning table.
    /// </summary>
    public class EmberdownEnemyTests
    {
        GameObject _floor, _wren, _enemy;
        WrenController _ctrl;
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
            _wren.AddComponent<WrenVitals>();
            _ctrl.Teleport(new Vector2(0f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _enemy, _wren, _floor }) if (go != null) Object.Destroy(go);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        T Make<T>(Vector2 pos, Vector2 size) where T : Enemy
        {
            _enemy = new GameObject(typeof(T).Name) { layer = Layer("Enemy") };
            _enemy.transform.position = pos;
            _enemy.AddComponent<BoxCollider2D>().size = size;
            _enemy.AddComponent<Rigidbody2D>();
            return _enemy.AddComponent<T>();
        }

        [UnityTest]
        public IEnumerator TheBatHangsTellsSwoopsThroughWrenAndReturnsToItsRoost()
        {
            var bat = Make<CaveBat>(new Vector2(3f, 4f), new Vector2(0.8f, 0.6f));
            AttackKind? told = null; Enemy who = null;
            void OnTell(Enemy e, AttackKind k) { who = e; told = k; }
            Enemy.Telegraphed += OnTell;
            try
            {
                yield return Frames(3);
                Assert.AreEqual(2, bat.MaxHealth, "the table's fodder on the wing");
                Assert.AreEqual(CaveBat.Move.Unfurl, bat.State, "Wren is within its reach: it unfurls at once");
                Assert.AreEqual(AttackKind.Strike, told, "the tell on the unfurl's first frame");
                Assert.AreSame(bat, who);
                Assert.AreEqual("unfurl", bat.Clip);
                Assert.That(bat.ClipProgress, Is.InRange(0f, 1f), "the unfurl is sought by its progress");
                Assert.That(Vector2.Distance(bat.transform.position, bat.Roost), Is.LessThan(0.2f), "it hangs still while it unfurls");

                // The swoop: down through where she stands and up the far side.
                while (bat.State == CaveBat.Move.Unfurl) yield return new WaitForFixedUpdate();
                Assert.AreEqual(CaveBat.Move.Swoop, bat.State);
                Assert.AreEqual("swoop", bat.Clip);
                float lowest = 99f, farthest = 99f;
                int guard = 0;
                while (bat.State == CaveBat.Move.Swoop && guard++ < 120)
                {
                    lowest = Mathf.Min(lowest, bat.transform.position.y);
                    farthest = Mathf.Min(farthest, bat.transform.position.x);
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(lowest, Is.LessThan(1.2f), "the arc's bottom is where Wren stood (it came down to her)");
                Assert.That(farthest, Is.LessThan(-1.5f), "and it went on up the far side of her");
                Assert.AreEqual(CaveBat.Move.Return, bat.State);
                Assert.AreEqual("move", bat.Clip, "it flaps back");

                guard = 0;
                while (bat.State == CaveBat.Move.Return && guard++ < 400) yield return new WaitForFixedUpdate();
                Assert.AreEqual(CaveBat.Move.Roost, bat.State, "back at the roost");
                Assert.That(Vector2.Distance(bat.transform.position, bat.Roost), Is.LessThan(0.5f));
                Assert.AreEqual("idle", bat.Clip, "and hangs");
            }
            finally { Enemy.Telegraphed -= OnTell; }
        }

        [UnityTest]
        public IEnumerator TheBatDiesToAnyHit()
        {
            var bat = Make<CaveBat>(new Vector2(9f, 4f), new Vector2(0.8f, 0.6f));   // out of reach: it stays at the roost
            yield return Frames(3);
            Assert.AreEqual(CaveBat.Move.Roost, bat.State);
            bool died = false;
            bat.Died += _ => died = true;
            Assert.IsTrue(bat.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }), "a side strike lands");
            yield return Frames(8);
            Assert.IsTrue(bat.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.up }), "an upward one too");
            Assert.IsTrue(died, "two hits: fodder");
        }

        [UnityTest]
        public IEnumerator TheSalamanderTellsRushesLowAtWrenThenCools()
        {
            var sal = Make<Salamander>(new Vector2(4f, 0.35f), new Vector2(1.2f, 0.5f));
            AttackKind? told = null;
            void OnTell(Enemy e, AttackKind k) { if (e == sal) told = k; }
            Enemy.Telegraphed += OnTell;
            try
            {
                yield return Frames(3);
                Assert.AreEqual(3, sal.MaxHealth, "the table's");
                Assert.AreEqual(EnemyAnswer.Pogo, sal.Answer);
                Assert.AreEqual(Salamander.Move.Flare, sal.State, "Wren is within its five units: it flares");
                Assert.AreEqual(AttackKind.Strike, told, "the tell on the flare's first frame");
                Assert.AreEqual(-1, sal.FacingDir, "turned to face her");
                Assert.IsTrue(sal.IsHot);
                Assert.AreEqual("flare", sal.Clip);
                Assert.That(sal.ClipProgress, Is.InRange(0f, 1f));
                float xBefore = sal.transform.position.x;
                while (sal.State == Salamander.Move.Flare) yield return new WaitForFixedUpdate();
                Assert.That(Mathf.Abs(sal.transform.position.x - xBefore), Is.LessThan(0.1f), "it holds still through the flare");

                Assert.AreEqual(Salamander.Move.Rush, sal.State);
                Assert.AreEqual("rush", sal.Clip);
                yield return Frames(4);
                Assert.That(sal.GetComponent<Rigidbody2D>().linearVelocity.x, Is.LessThan(-6f), "the rush: low and fast toward her");
                int guard = 0;
                while (sal.State == Salamander.Move.Rush && guard++ < 60) yield return new WaitForFixedUpdate();
                Assert.AreEqual(Salamander.Move.Cool, sal.State);
                Assert.AreEqual("cool", sal.Clip);
                Assert.IsFalse(sal.IsHot);
                Assert.That(sal.transform.position.x, Is.LessThan(xBefore - 2f), "it covered ground");
                guard = 0;
                while (sal.State == Salamander.Move.Cool && guard++ < 80) yield return new WaitForFixedUpdate();
                Assert.AreEqual(Salamander.Move.Crawl, sal.State, "then crawls again");
            }
            finally { Enemy.Telegraphed -= OnTell; }
        }

        [UnityTest]
        public IEnumerator TheSalamanderStopsItsRushAtTheLedgesEdgeAndTurnsAwayAllButAPogo()
        {
            // A short ledge: Wren at its west end, the salamander rushing at her would run off it.
            _floor.GetComponent<BoxCollider2D>().size = new Vector2(6f, 1f);
            _ctrl.Teleport(new Vector2(-1.5f, 0f));
            var sal = Make<Salamander>(new Vector2(1.0f, 0.35f), new Vector2(1.2f, 0.5f));
            yield return Frames(3);
            int guard = 0;
            while (sal.State != Salamander.Move.Cool && guard++ < 120) yield return new WaitForFixedUpdate();
            Assert.AreEqual(Salamander.Move.Cool, sal.State, "flared, rushed, stopped");
            Assert.That(sal.transform.position.x, Is.GreaterThan(-3f), "it stopped at the edge, not past it");
            yield return Frames(60);
            Assert.That(sal.transform.position.y, Is.GreaterThan(0f), "and did not fall off");

            int hp = sal.Health;
            Assert.IsFalse(sal.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.left }), "a side strike is turned by the burning back");
            Assert.IsFalse(sal.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.up }), "an upward one too");
            Assert.AreEqual(hp, sal.Health);
            Assert.IsTrue(sal.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.down }), "the pogo lands");
            Assert.AreEqual(hp - 1, sal.Health);
        }
    }
}
