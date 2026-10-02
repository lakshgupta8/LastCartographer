using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The roster's later families (CMB-09, combat doc 7, enemy-animation.md 2d): the lantern-moth cloud flares and
    /// darts, and no strike lands on it until a Blot's slow has gathered it; the pulp-wasp holds its stand-off, backing
    /// from the quill, and spits a pellet after its tell; the Sketch is an outline past her lantern-radius, unhittable
    /// and harmless, and inside it fills, tells and lunges. Each takes its numbers from the tuning table.
    /// </summary>
    public class RosterEnemyTests
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
            foreach (var p in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)) Object.Destroy(p.gameObject);
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

        HitInfo Strike(Vector2 dir) => new HitInfo { Damage = 1, Direction = dir, Source = _wren };

        [UnityTest]
        public IEnumerator TheMothCloudFlaresDartsAndOnlyAGatheredCloudCanBeStruck()
        {
            var cloud = Make<Mothcloud>(new Vector2(2f, 2f), new Vector2(1.4f, 1.2f));
            AttackKind? told = null;
            void OnTell(Enemy e, AttackKind k) { if (e == cloud) told = k; }
            Enemy.Telegraphed += OnTell;
            try
            {
                yield return Frames(3);
                Assert.AreEqual(3, cloud.MaxHealth, "the table's cloud");
                Assert.AreEqual(EnemyAnswer.Blot, cloud.Answer);
                Assert.AreEqual(Mothcloud.Move.Flare, cloud.State, "Wren is within its dart: it flares at once");
                Assert.AreEqual(AttackKind.Strike, told, "the tell on the flare's first frame");
                Assert.AreEqual("flare", cloud.Clip);
                Assert.IsFalse(cloud.TakeHit(Strike(Vector2.right)), "the quill passes through a cloud");
                Assert.AreEqual(3, cloud.Health);
                yield return Frames(18);
                Assert.AreEqual(Mothcloud.Move.Dart, cloud.State, "after the flare, the dart");
                Assert.AreEqual("dart", cloud.Clip);
                Assert.Less(cloud.GetComponent<Rigidbody2D>().linearVelocity.x, 0f, "through where she stood");
                yield return Frames(25);
                Assert.AreEqual(Mothcloud.Move.Drift, cloud.State, "and it drifts again, with a cooldown before the next");

                cloud.ApplySlow(2f);                                   // the Blot's slow: the cloud gathered
                Assert.IsTrue(cloud.IsGathered);
                yield return null;
                Assert.AreEqual("gather", cloud.Clip, "pulled tight");
                Assert.IsTrue(cloud.TakeHit(Strike(Vector2.right)), "a gathered cloud takes the strike");
                Assert.AreEqual(2, cloud.Health);
            }
            finally { Enemy.Telegraphed -= OnTell; }
        }

        [UnityTest]
        public IEnumerator TheWaspHoldsItsStandOffTellsAndSpitsAPelletAtHer()
        {
            var wasp = Make<Pulpwasp>(new Vector2(6f, 2f), new Vector2(0.9f, 0.7f));
            AttackKind? told = null;
            void OnTell(Enemy e, AttackKind k) { if (e == wasp) told = k; }
            Enemy.Telegraphed += OnTell;
            try
            {
                yield return Frames(3);
                Assert.AreEqual(3, wasp.MaxHealth, "the table's wasp");
                Assert.AreEqual(EnemyAnswer.Longstroke, wasp.Answer);
                Assert.AreEqual(Pulpwasp.Move.Spit, wasp.State, "she is within its throw: the sac swells");
                Assert.AreEqual(AttackKind.Strike, told, "the tell on the spit's first frame");
                Assert.AreEqual("spit", wasp.Clip);
                Assert.IsNull(wasp.LastPellet, "nothing thrown during the telegraph");
                yield return Frames(24);
                Assert.AreEqual(1, wasp.Spat, "then the pellet");
                Assert.IsNotNull(wasp.LastPellet);
                Assert.Less(wasp.LastPellet.Velocity.x, 0f, "thrown toward her");
                Assert.AreEqual(1, wasp.LastPellet.Damage, "a mask");
                Assert.AreEqual(Pulpwasp.Move.Hover, wasp.State);

                // Close with her and it backs away; the quill's reach is short of it.
                _ctrl.Teleport(new Vector2(4.5f, 0f));
                yield return Frames(6);
                Assert.Greater(wasp.GetComponent<Rigidbody2D>().linearVelocity.x, 0f, "it backs off to its stand-off");
                Assert.Greater(wasp.StandOff, 2f, "past the quill");
                Assert.Less(wasp.StandOff, 6f, "within the Longstroke's reach");
                Assert.IsTrue(wasp.TakeHit(Strike(Vector2.right)), "anything that reaches it lands");
            }
            finally { Enemy.Telegraphed -= OnTell; }
        }

        [UnityTest]
        public IEnumerator TheSketchIsAnOutlinePastHerLanternAndADrawingInsideIt()
        {
            var sketch = Make<Sketch>(new Vector2(3f, 0.6f), new Vector2(1.0f, 1.2f));
            sketch.ForceRadius(1.5f);
            AttackKind? told = null;
            void OnTell(Enemy e, AttackKind k) { if (e == sketch) told = k; }
            Enemy.Telegraphed += OnTell;
            try
            {
                yield return Frames(3);
                Assert.AreEqual(4, sketch.MaxHealth, "the table's Sketch");
                Assert.IsFalse(sketch.IsDrawn, "three units off, past a radius of one and a half: an outline");
                Assert.IsFalse(sketch.TakeHit(Strike(Vector2.right)), "an outline cannot be struck");
                Assert.AreEqual(Sketch.Move.Pace, sketch.State, "it paces toward her, and does not lunge");
                Assert.IsNull(told);
                Assert.Less(sketch.GetComponent<Rigidbody2D>().linearVelocity.x, 0f, "toward her");
                for (int i = 0; i < 30 && sketch.InkLevel > 0.5f; i++) yield return null;
                Assert.Less(sketch.InkLevel, 0.5f, "its ink thins to the outline");

                sketch.ForceRadius(5f);                                 // her lantern grows (Clarity)
                yield return Frames(2);
                Assert.IsTrue(sketch.IsDrawn, "inside the radius it is drawn");
                Assert.AreEqual(Sketch.Move.Fill, sketch.State, "and within its lunge it fills");
                Assert.AreEqual(AttackKind.Strike, told, "the tell on the fill's first frame");
                Assert.AreEqual("fill", sketch.Clip);
                Assert.IsTrue(sketch.TakeHit(Strike(Vector2.right)), "a drawing takes the strike");
                Assert.AreEqual(3, sketch.Health);
                yield return Frames(30);                                // the hurtstun, then the rest of the fill
                Assert.AreEqual(Sketch.Move.Lunge, sketch.State, "then the lunge");
                Assert.Less(sketch.GetComponent<Rigidbody2D>().linearVelocity.x, -5f, "low and fast, toward her");
                for (int i = 0; i < 30 && sketch.InkLevel < 0.9f; i++) yield return null;
                Assert.Greater(sketch.InkLevel, 0.9f, "drawn in full");
            }
            finally { Enemy.Telegraphed -= OnTell; }
        }

        [UnityTest]
        public IEnumerator TheTussockTravelsUnderTheGrassHeavesBreachesAndOnlyAPogoOnTheSurfacedShellLands()
        {
            var tussock = Make<Tussock>(new Vector2(3f, 0.45f), new Vector2(1.0f, 0.9f));
            AttackKind? told = null;
            void OnTell(Enemy e, AttackKind k) { if (e == tussock) told = k; }
            Enemy.Telegraphed += OnTell;
            try
            {
                yield return Frames(3);
                Assert.AreEqual(3, tussock.MaxHealth, "the table's tussock");
                Assert.AreEqual(EnemyAnswer.Pogo, tussock.Answer);
                Assert.AreEqual(Tussock.Move.Ridge, tussock.State, "under the turf, travelling");
                Assert.Less(tussock.GetComponent<Rigidbody2D>().linearVelocity.x, 0f, "toward her");
                Assert.AreEqual("ridge", tussock.Clip);
                Assert.IsFalse(tussock.IsSurfaced);
                Assert.IsFalse(tussock.TakeHit(Strike(Vector2.down)), "a ridge in the grass cannot be struck, even from above");
                Assert.IsNull(told);

                _ctrl.Teleport(new Vector2(3.4f, 0f));                   // she stands over it
                yield return Frames(2);
                Assert.AreEqual(Tussock.Move.Heave, tussock.State, "under her: the turf lifts");
                Assert.AreEqual(AttackKind.Strike, told, "the tell on the heave's first frame");
                Assert.AreEqual("heave", tussock.Clip);
                yield return Frames(30);
                Assert.AreEqual(Tussock.Move.Breach, tussock.State, "then it breaches");
                Assert.IsTrue(tussock.IsSurfaced);
                Assert.IsFalse(tussock.TakeHit(Strike(Vector2.right)), "shelled: a side strike glances off");
                Assert.IsTrue(tussock.TakeHit(Strike(Vector2.down)), "the pogo lands on the shell");
                Assert.AreEqual(2, tussock.Health);
                yield return Frames(30);
                Assert.AreEqual(Tussock.Move.Sit, tussock.State, "it sits a moment on the surface");
                Assert.AreEqual("idle", tussock.Clip);
                yield return Frames(100);
                Assert.AreEqual(Tussock.Move.Ridge, tussock.State, "then burrows and travels again");
                Assert.IsFalse(tussock.IsSurfaced);
            }
            finally { Enemy.Telegraphed -= OnTell; }
        }

        [UnityTest]
        public IEnumerator TheReedlingsScurryInAClutchPeckAndScatterTogetherWhenOneIsStruck()
        {
            var clutch = new Reedling[3];
            var extra = new System.Collections.Generic.List<GameObject>();
            for (int k = 0; k < 3; k++)
            {
                var go = new GameObject("Reedling_" + k) { layer = Layer("Enemy") };
                go.transform.position = new Vector2(2f + 0.9f * k, 0.35f);
                go.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.7f);
                go.AddComponent<Rigidbody2D>();
                clutch[k] = go.AddComponent<Reedling>();
                if (k == 0) _enemy = go; else extra.Add(go);
            }
            AttackKind? told = null; Enemy who = null;
            void OnTell(Enemy e, AttackKind k) { who = e; told = k; }
            Enemy.Telegraphed += OnTell;
            try
            {
                yield return Frames(3);
                Assert.AreEqual(2, clutch[0].MaxHealth, "the table's chick: fodder alone");
                Assert.AreEqual(EnemyAnswer.Blot, clutch[0].Answer, "the clutch is a swarm");
                Assert.AreEqual(2, System.Linq.Enumerable.Count(clutch[0].Clutch), "its two siblings are its clutch");
                foreach (var r in clutch) Assert.AreEqual(Reedling.Move.Scurry, r.State);
                Assert.Less(clutch[0].GetComponent<Rigidbody2D>().linearVelocity.x, 0f, "the nearest scurries toward her");

                _ctrl.Teleport(new Vector2(1.2f, 0f));
                for (int i = 0; i < 40 && clutch[0].State != Reedling.Move.Peck; i++) yield return new WaitForFixedUpdate();
                Assert.AreEqual(Reedling.Move.Peck, clutch[0].State, "within its reach it rears");
                Assert.AreSame(clutch[0], who); Assert.AreEqual(AttackKind.Strike, told, "the tell on the peck's first frame");
                Assert.AreEqual("peck", clutch[0].Clip);

                Assert.IsTrue(clutch[2].TakeHit(Strike(Vector2.right)), "any strike lands on a chick");
                Assert.AreEqual(1, clutch[2].Health);
                foreach (var r in clutch) Assert.AreEqual(Reedling.Move.Scatter, r.State, r.name + " scatters with the clutch");
                yield return Frames(8);
                Assert.Greater(clutch[1].GetComponent<Rigidbody2D>().linearVelocity.x, 0f, "away from her");
                yield return Frames(45);
                foreach (var r in clutch) Assert.AreEqual(Reedling.Move.Scurry, r.State, r.name + " regroups");
                foreach (var r in clutch) r.ApplySlow(2f);                 // the Blot's slow holds all three
                foreach (var r in clutch) Assert.IsTrue(r.IsSlowed);
            }
            finally
            {
                Enemy.Telegraphed -= OnTell;
                foreach (var go in extra) Object.Destroy(go);
            }
        }
    }
}
