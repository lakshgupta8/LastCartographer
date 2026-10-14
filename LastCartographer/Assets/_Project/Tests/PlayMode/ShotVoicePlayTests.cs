#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The things thrown at Wren, heard in play (AUD-19, docs/design/enemy-sounds.md §2c): a pellet whizzes as it falls
    /// and pats where it lands, or crumbles where its time runs out; Hale's ink leaves the nib, hisses from where it is
    /// and splashes on her; the Unwriter's strike rubs out what it meets.
    /// </summary>
    public class ShotVoicePlayTests
    {
        GameObject? _floor, _wren;
        float _lx;
        readonly List<ShotEnd> _ends = new List<ShotEnd>();
        InkSoundBank Bank => InkSoundBank.Instance!;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        IEnumerator Until(System.Func<bool> done, float seconds, string what)
        {
            float t = 0f;
            while (!done() && t < seconds) { t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(done(), "timed out waiting for " + what);
        }

        int _mark;
        void Mark() => _mark = Bank.Recent.Count;
        List<string> Since => Bank.Recent.Skip(_mark).ToList();
        void OnSpent(EnemyProjectile p) => _ends.Add(p.Ending);

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            Pause.End();
            GameState.NewGame();
            Assert.IsNotNull(InkSoundBank.Instance, "the bank wakes with the game");
            Assert.IsNotNull(WorldSoundHooks.Instance, "and the world's hooks beside it");
            Bank.Hush();
            _lx = Bank.ListenerX;
            _ends.Clear();
            EnemyProjectile.Spent += OnSpent;
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(_lx, -0.5f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            EnemyProjectile.Spent -= OnSpent;
            foreach (var p in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)) Object.Destroy(p.gameObject);
            foreach (var go in new[] { _wren, _floor }) if (go != null) Object.Destroy(go);
            if (InkSoundBank.Instance != null) InkSoundBank.Instance.Hush();
            Pause.End();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        void MakeWren()
        {
            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            var ctrl = _wren.AddComponent<WrenController>();
            ctrl.groundMask = LayerMask.GetMask("Ground");
            ctrl.Input = new ScriptedInput();
            ctrl.Recompute();
            _wren.AddComponent<WrenVitals>();
            _wren.AddComponent<WrenSounds>();
            _wren.AddComponent<CharterSet>();
            ctrl.Teleport(new Vector2(_lx, 0f));
        }

        [UnityTest]
        public IEnumerator APelletWhizzesAsItFallsAndPatsWhereItLands()
        {
            Mark();
            var p = EnemyProjectile.Spawn("Pulp", null, new Vector2(_lx + 3f, 2.5f), new Vector2(0f, -5f), new Vector2(0.35f, 0.35f), 1, null);
            yield return Frames(2);
            var voice = p.GetComponent<ProjectileVoice>();
            Assert.AreEqual("Pulp", voice.Shot.Name);
            Assert.AreEqual("pellet_fly", voice.Looping, "a small whizz while it flies");
            Assert.Greater(p.GetComponent<AudioSource>().panStereo, 0f, "from the right, where it is");
            Assert.IsEmpty(Since, "its leaving is the wasp's spit, not its own");
            yield return Until(() => p == null, 2f, "the pellet to land");
            CollectionAssert.AreEqual(new[] { ShotEnd.Ground }, _ends);
            CollectionAssert.AreEqual(new[] { "pellet_land" }, Since, "a wet pat, and only that");
        }

        [UnityTest]
        public IEnumerator APelletSpentInTheAirCrumbles()
        {
            Mark();
            var p = EnemyProjectile.Spawn("Pulp", null, new Vector2(_lx + 2f, 3f), Vector2.zero, new Vector2(0.35f, 0.35f), 1, null);
            p.LifeLeft = 0.05f;
            yield return Until(() => p == null, 1f, "its time to run out");
            CollectionAssert.AreEqual(new[] { ShotEnd.Time }, _ends);
            CollectionAssert.AreEqual(new[] { "pellet_crumble" }, Since, "dried in the air, crumbling; no pat with nothing under it");
        }

        [UnityTest]
        public IEnumerator HalesInkLeavesTheNibHissesAndSplashesOnHer()
        {
            MakeWren();
            yield return Frames(3);
            Mark();
            var p = EnemyProjectile.Spawn("Flick", null, new Vector2(_lx + 5f, 0.6f), new Vector2(-8f, 0f), new Vector2(0.35f, 0.35f), 1, null);
            yield return Frames(1);
            Assert.AreEqual("flick_throw", Since.FirstOrDefault(), "thrown off the nib");
            Assert.AreEqual("flick_fly", p.GetComponent<ProjectileVoice>().Looping, "a thin hiss in flight");
            Assert.Greater(Bank.LastPan, 0f, "from the right, where he threw it");
            int masks = _wren!.GetComponent<WrenVitals>().Masks;
            yield return Until(() => p == null, 2f, "the ink to reach her");
            CollectionAssert.AreEqual(new[] { ShotEnd.Her }, _ends);
            Assert.AreEqual(masks - 1, _wren.GetComponent<WrenVitals>().Masks, "it cost her a mask");
            Assert.That(Since, Does.Contain("flick_splash"), "a splash on her");
            Assert.That(Since, Does.Not.Contain("flick_spatter"), "not the ground's spatter");
        }

        [UnityTest]
        public IEnumerator TheUnwritersStrikeRubsItOut()
        {
            MakeWren();
            var e = GameState.World.Equipment;
            e.OwnedCharters.Add(CharterKind.Unwriter);
            Assert.IsTrue(e.SetCharter(CharterKind.Unwriter), "wear the Unwriter");
            yield return Frames(2);
            var p = EnemyProjectile.Spawn("Pulp", null, new Vector2(_lx + 6f, 0.6f), new Vector2(-1f, 0f), new Vector2(0.35f, 0.35f), 1, null);
            yield return Frames(2);
            Mark();
            Assert.IsTrue(p.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right, Source = _wren }), "unwritten");
            yield return null;
            CollectionAssert.AreEqual(new[] { ShotEnd.Unwritten }, _ends);
            CollectionAssert.AreEqual(new[] { "shot_unwritten" }, Since, "rubbed out, not landed");
        }
    }
}
