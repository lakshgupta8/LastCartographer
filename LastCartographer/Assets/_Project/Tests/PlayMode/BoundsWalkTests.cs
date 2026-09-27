#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace OWSBG.Tests
{
    /// <summary>The bounds-walk (DES-13, PRG-22): beats land, misses restart the verse, walking every verse holds the place, Yarn starts it.</summary>
    public class BoundsWalkTests
    {
        GameObject? _floor, _wren, _room, _ui, _dialogue;
        WrenController? _ctrl;
        BoundsWalk? _walk;

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
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = new ScriptedInput();
            _ctrl.Recompute();
            _ctrl.Teleport(new Vector2(-15f, 0f));

            _room = new GameObject("Room_Walk");
            _room.AddComponent<Room>().RoomId = "Walk_Test";
            var walkGo = new GameObject("Walk");
            walkGo.transform.SetParent(_room.transform, false);
            _walk = walkGo.AddComponent<BoundsWalk>();
            _walk.Id = "walk_test";
            _walk.SecondsPerBeat = 0.4f;
            _walk.AddVerse("the stoop and the post", ("the stoop", new Vector2(-8f, 0f)), ("the post", new Vector2(0f, 0f)), ("the shaft", new Vector2(8f, 0f)));
            _walk.AddVerse("back again", ("the post", new Vector2(0f, 0f)), ("the stoop", new Vector2(-8f, 0f)));
            walkGo.SetActive(false); walkGo.SetActive(true);   // OnEnable registers the id
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _dialogue, _ui, _room, _wren, _floor }) if (go != null) Object.Destroy(go);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.deltaTime; yield return null; }
        }

        [UnityTest]
        public IEnumerator StandingOnEveryBeatHoldsThePlace()
        {
            Assert.IsNotNull(BoundsWalk.Find("walk_test"));
            BoundsWalk.NameCalled += Follow;
            int landed = 0, hits = 0;
            BoundsWalk.BeatLanded += (w, b, hit) => { landed++; if (hit) hits++; };
            Assert.IsTrue(_walk!.Begin());
            Assert.IsFalse(_walk.Begin(), "once at a time");
            yield return null; yield return null;
            Assert.AreEqual(BoundsWalk.Phase.Walking, _walk.State, "Wren is free, so it counts in");
            Assert.AreSame(_walk, BoundsWalk.Current);
            float t = 0f;
            while (_walk.State == BoundsWalk.Phase.Walking && t < 6f) { t += Time.deltaTime; yield return null; }
            BoundsWalk.NameCalled -= Follow;
            Assert.AreEqual(BoundsWalk.Phase.Done, _walk.State);
            Assert.AreEqual(5, landed);
            Assert.AreEqual(5, hits, "on the spot every time");
            Assert.AreEqual(0, _walk.Restarts);
            Assert.IsTrue(BoundsWalks.IsWalked(GameState.World, "Walk_Test"));
            Assert.AreEqual(PlaceFate.Held, Places.FateOf(GameState.World, "Walk_Test"), "walked is held");
            Assert.IsFalse(_walk.Begin(), "a held place is not walked again");
        }

        void Follow(BoundsWalk w, BoundsWalk.Bound b) => _ctrl!.Teleport(b.Position);

        [UnityTest]
        public IEnumerator ThreeMissesRestartTheVerse()
        {
            int restarts = 0;
            BoundsWalk.VerseRestarted += _ => restarts++;
            Assert.IsTrue(_walk!.Begin());
            yield return null; yield return null;
            // Stand nowhere: three beats miss, the verse restarts at beat one with the misses forgiven.
            float t = 0f;
            while (restarts == 0 && t < 4f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(1, restarts, "the third miss sends the verse back to the top");
            Assert.AreEqual(0, _walk.BeatIndex);
            Assert.AreEqual(0, _walk.Misses);
            Assert.AreEqual(0, _walk.VerseIndex);
            Assert.AreEqual(BoundsWalk.Phase.Walking, _walk.State);
            // Now follow the calls: it still completes.
            BoundsWalk.NameCalled += Follow;
            t = 0f;
            while (_walk.State == BoundsWalk.Phase.Walking && t < 8f) { t += Time.deltaTime; yield return null; }
            BoundsWalk.NameCalled -= Follow;
            Assert.AreEqual(BoundsWalk.Phase.Done, _walk.State);
            Assert.AreEqual(1, _walk.Restarts);
            Assert.IsTrue(BoundsWalks.IsWalked(GameState.World, "Walk_Test"));
        }

        const string Script = @"
title: Start
---
Dotha: Walk it with me.
<<walk walk_test>>
===
";

        [UnityTest]
        public IEnumerator YarnStartsTheWalkAndTheStripFollowsIt()
        {
            _ui = new GameObject("UI");
            _ui.AddComponent<UIDocument>();
            _ui.AddComponent<UiRoot>();
            var strip = _ui.AddComponent<WalkView>();
            var project = RuntimeYarnBuilder.Build(Script);
            _dialogue = new GameObject("Dialogue");
            var presenter = _dialogue.AddComponent<RecordingPresenter>();
            var service = _dialogue.AddComponent<DialogueService>();
            service.Initialize(project, presenter);
            yield return null; yield return null;
            Assert.IsFalse(DialogueService.IsWalked("Walk_Test"));
            Assert.IsTrue(service.StartNode("Start"));
            for (int i = 0; i < 120 && service.IsRunning; i++) yield return null;
            Assert.IsFalse(service.IsRunning);
            for (int i = 0; i < 10 && _walk!.State != BoundsWalk.Phase.Walking; i++) yield return null;
            Assert.AreEqual(BoundsWalk.Phase.Walking, _walk!.State, "the walk begins once the talk is over");
            BoundsWalk.NameCalled += Follow;
            float t = 0f;
            while (string.IsNullOrEmpty(strip.CalledText) || strip.CalledText == "…") { t += Time.deltaTime; yield return null; Assert.Less(t, 3f, "a name is called"); }
            Assert.IsTrue(strip.IsVisible);
            StringAssert.Contains("the stoop", strip.CalledText);
            t = 0f;
            while (_walk.State == BoundsWalk.Phase.Walking && t < 8f) { t += Time.deltaTime; yield return null; }
            BoundsWalk.NameCalled -= Follow;
            Assert.AreEqual(BoundsWalk.Phase.Done, _walk.State);
            Assert.IsTrue(DialogueService.IsWalked("Walk_Test"));
            Assert.AreEqual("Held.", strip.CalledText);
        }
    }
}
