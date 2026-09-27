using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;

namespace OWSBG.Tests
{
    /// <summary>Boss staging (PRG-06, PRG-16): first entry plays the intro cutscene, the arena camera holds the fight, both stand down on win or reset.</summary>
    public class BossStagingTests
    {
        GameObject _floor, _wren, _room, _arenaGo, _bossGo, _camGo, _csGo, _doorW, _doorE;
        WrenController _ctrl;
        WrenVitals _vitals;
        BossArena _arena;
        LampKeeper _boss;
        CinemachineCamera _cam;
        Cutscene _intro;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            ScreenFade.Clear();
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
            _boss.SetMaxHealth(6);

            _camGo = new GameObject("CM Arena");
            _camGo.transform.position = new Vector3(3f, 4.2f, -21f);
            _cam = _camGo.AddComponent<CinemachineCamera>();
            _camGo.SetActive(false);

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var hold = timeline.CreateTrack<PlayableTrack>("Hold").CreateClip<PaperFadeClip>();
            hold.start = 0; hold.duration = 0.3;
            ((PaperFadeClip)hold.asset).From = 0f; ((PaperFadeClip)hold.asset).To = 0f;
            _csGo = new GameObject("Cutscene_intro");
            _csGo.SetActive(false);
            var director = _csGo.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.playableAsset = timeline;
            _intro = _csGo.AddComponent<Cutscene>();
            _intro.Id = "test_boss_intro";
            _csGo.SetActive(true);

            _arenaGo = new GameObject("Arena") { layer = Layer("Trigger") };
            _arenaGo.transform.position = new Vector3(3f, 5f, 0f);
            var zone = _arenaGo.AddComponent<BoxCollider2D>();
            zone.isTrigger = true; zone.size = new Vector2(17f, 11f);
            _arena = _arenaGo.AddComponent<BossArena>();
            _arena.Configure(_boss, new[] { _doorW, _doorE }, "staging_test", Ability.None);
            _arena.IntroSeconds = 0f;
            _arena.RetryIntroSeconds = 0f;
            _arena.IntroCutscene = _intro;
            _arena.ArenaCamera = _cam;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _arenaGo, _csGo, _camGo, _bossGo, _doorW, _doorE, _room, _wren, _floor }) if (go != null) Object.Destroy(go);
            Time.timeScale = 1f;
            ScreenFade.Clear();
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

        [UnityTest]
        public IEnumerator FirstEntryPlaysTheIntroAndTheArenaCameraHoldsUntilTheWin()
        {
            yield return Frames(3);
            Assert.IsFalse(_arena.ArenaCameraActive);
            _ctrl.Teleport(new Vector2(0f, 0f));
            for (int i = 0; i < 30 && _arena.State == BossArena.ArenaState.Idle; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(BossArena.ArenaState.Intro, _arena.State, "the intro starts");
            yield return null;
            Assert.IsTrue(_intro.IsPlaying, "the intro cutscene plays on first entry");
            Assert.IsTrue(_arena.ArenaCameraActive, "the arena camera comes up with the doors");
            Assert.AreEqual(40, (int)_cam.Priority);
            Assert.IsTrue(_doorW.activeSelf && _doorE.activeSelf, "doors shut through the intro");
            Assert.IsTrue(_ctrl.Frozen);
            Assert.IsFalse(_boss.IsFightActive, "the boss waits for the intro");

            float t = 0f;
            while (_intro.IsPlaying && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.IsFalse(_intro.IsPlaying);
            for (int i = 0; i < 30 && !_boss.IsFightActive; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(_boss.IsFightActive, "the fight begins when the intro ends");
            Assert.AreEqual(BossArena.ArenaState.Fighting, _arena.State);
            Assert.IsFalse(_ctrl.Frozen, "Wren is free to fight");
            Assert.IsTrue(_arena.ArenaCameraActive, "the arena camera stays for the fight");

            for (int i = 0; i < 6; i++)
            {
                _boss.ForceGrounded(5f);
                Assert.IsTrue(_boss.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }));
                yield return Frames(4);
            }
            for (int i = 0; i < 30 && _arena.State != BossArena.ArenaState.Won; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(BossArena.ArenaState.Won, _arena.State);
            Assert.IsFalse(_arena.ArenaCameraActive, "the follow rig takes back the camera on the win");
            Assert.IsFalse(_doorW.activeSelf);
        }

        [UnityTest]
        public IEnumerator ResetDropsTheCameraAndRetryUsesTheShortIntro()
        {
            yield return Frames(3);
            _ctrl.Teleport(new Vector2(0f, 0f));
            for (int i = 0; i < 30 && _arena.State == BossArena.ArenaState.Idle; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(_intro.IsPlaying);
            _vitals.Damage(99);
            yield return Frames(2);
            Assert.AreEqual(BossArena.ArenaState.Idle, _arena.State, "dying during the intro resets the arena");
            Assert.IsFalse(_intro.IsPlaying, "the intro is cut");
            Assert.IsFalse(_arena.ArenaCameraActive, "the camera goes with it");

            // Back to the desk, in again: the retry skips the cutscene.
            _vitals.RestoreAll();
            _ctrl.Frozen = false;
            _ctrl.Teleport(new Vector2(-12f, 0f));
            yield return Frames(3);
            _ctrl.Teleport(new Vector2(0f, 0f));
            for (int i = 0; i < 30 && !_boss.IsFightActive; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(_boss.IsFightActive, "retry goes straight to the fight");
            Assert.IsFalse(_intro.IsPlaying, "no cutscene on the retry");
            Assert.IsTrue(_arena.ArenaCameraActive, "but the arena camera is back");
        }
    }
}
