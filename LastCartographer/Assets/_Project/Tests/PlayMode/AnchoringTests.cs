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
    /// <summary>
    /// Anchoring in the running game (PRG-13): the Warden patrols and lances, an anchored place switches its
    /// Wardens on and locks the grade, the desk seals a fate only once the place is surveyed, Yarn decides too.
    /// </summary>
    public class AnchoringTests
    {
        GameObject? _floor, _wallL, _wallR, _wren, _room, _ui, _desk, _dialogue, _vantageGo;
        WrenController? _ctrl;
        WrenVitals? _vitals;
        ScriptedInput? _input;
        Room? _roomC;
        HeldState? _held;
        readonly System.Collections.Generic.List<GameObject> _spawned = new System.Collections.Generic.List<GameObject>();

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Shader.SetGlobalFloat(HeldState.HeldGlobal, 0f);
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _wallL = Wall(-12f); _wallR = Wall(12f);

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
            _wren.AddComponent<QuillStrike>();
            _wren.AddComponent<Flourishes>();
            _wren.AddComponent<InstrumentBelt>();
            _wren.AddComponent<CharterSet>();
            _wren.AddComponent<Interactor>();
            _ctrl.Teleport(new Vector2(-8f, 0f));

            _room = new GameObject("Room_Anchor");
            _roomC = _room.AddComponent<Room>();
            _roomC.RoomId = "Anchor_Test";
            _held = _room.AddComponent<HeldState>();
        }

        GameObject Wall(float x)
        {
            var w = new GameObject("Wall") { layer = Layer("Ground") };
            w.transform.position = new Vector3(x, 3f, 0f);
            w.AddComponent<BoxCollider2D>().size = new Vector2(1f, 8f);
            return w;
        }

        Warden SpawnWarden(Vector2 pos, bool active = true)
        {
            var go = new GameObject("Warden") { layer = Layer("Enemy") };
            go.transform.position = pos;
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 1.6f);
            go.AddComponent<Rigidbody2D>();
            var w = go.AddComponent<Warden>();
            go.SetActive(active);
            _spawned.Add(go);
            return w;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var g in _spawned) if (g != null) Object.Destroy(g);
            _spawned.Clear();
            foreach (var go in new[] { _dialogue, _desk, _vantageGo, _ui, _room, _wren, _wallL, _wallR, _floor }) if (go != null) Object.Destroy(go);
            Shader.SetGlobalFloat(HeldState.HeldGlobal, 0f);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator WardenPatrolsTurnsAtWallsAndLancesWrenInFront()
        {
            Licence.Revoke(GameState.World);                        // Halvard's count has come in: she is hunted
            _ctrl!.Teleport(new Vector2(10f, 0f));                 // out of the way
            var warden = SpawnWarden(new Vector2(-6f, 0.9f));       // faces left by default: six units to the wall
            yield return Fixed(3);
            float x0 = warden.transform.position.x;
            yield return Fixed(40);
            Assert.AreNotEqual(x0, warden.transform.position.x, "it walks");
            int facing0 = warden.Dir;
            for (int i = 0; i < 600 && warden.Dir == facing0; i++) yield return new WaitForFixedUpdate();
            Assert.AreNotEqual(facing0, warden.Dir, "it turns at the wall");

            // Step in front of it: lance down, then the thrust lands.
            int masks = _vitals!.Masks;
            _ctrl.Teleport(new Vector2(warden.transform.position.x + warden.Dir * 2f, 0f));
            for (int i = 0; i < 60 && !warden.IsTelegraphing; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(warden.IsTelegraphing, "the lance comes down first");
            Assert.AreEqual(masks, _vitals.Masks, "no damage during the telegraph");
            for (int i = 0; i < 60 && warden.Thrusts == 0; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, warden.Thrusts);
            yield return Fixed(8);
            Assert.Less(_vitals.Masks, masks, "the thrust hurts");
            Assert.AreEqual(EnemyAnswer.Parry, warden.Answer);
        }

        [UnityTest]
        public IEnumerator WardensMeasureAJourneymanAndHuntHerOnceUnlicensed()
        {
            var w = GameState.World;
            _ctrl!.Teleport(new Vector2(10f, 0f));
            var warden = SpawnWarden(new Vector2(-6f, 0.9f));
            yield return Fixed(3);
            Assert.IsFalse(warden.Hostile, "a journeyman's cowl still counts");

            // In front of him with her papers in order: he stops, measures, and walks on. No lance, no hurt.
            int masks = _vitals!.Masks;
            _ctrl.Teleport(new Vector2(warden.transform.position.x + warden.Dir * 1.2f, 0f));
            for (int i = 0; i < 60 && !warden.IsMeasuring; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(warden.IsMeasuring, "he measures");
            Assert.AreEqual(1, warden.Measures);
            for (int i = 0; i < 80 && warden.IsMeasuring; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(Warden.Move.Patrol, warden.State, "and looks away");
            yield return Fixed(20);
            Assert.AreEqual(0, warden.Thrusts, "no lance for a journeyman");
            Assert.AreEqual(masks, _vitals.Masks, "and touching him does not hurt");

            // Halvard's count comes in: the same Warden lowers the lance.
            Assert.IsTrue(Licence.Revoke(w));
            Assert.IsTrue(warden.Hostile);
            _ctrl.Teleport(new Vector2(10f, 0f));
            yield return Fixed(5);
            for (int i = 0; i < 400 && warden.State != Warden.Move.Patrol; i++) yield return new WaitForFixedUpdate();
            _ctrl.Teleport(new Vector2(warden.transform.position.x + warden.Dir * 2f, 0f));
            for (int i = 0; i < 300 && !warden.IsTelegraphing; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(warden.IsTelegraphing, "the lance comes down for an unlicensed cartographer");
            for (int i = 0; i < 60 && warden.Thrusts == 0; i++) yield return new WaitForFixedUpdate();
            yield return Fixed(8);
            Assert.Less(_vitals.Masks, masks, "and it lands");

            // Oriel stands them down; a struck Warden hunts her anyway.
            w.Set(Licence.StoodDownFlag, true);
            Assert.IsFalse(warden.Hostile, "stood down");
            Assert.IsTrue(warden.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.left }));
            Assert.IsTrue(warden.Provoked);
            Assert.IsTrue(warden.Hostile, "provoked");
        }

        [UnityTest]
        public IEnumerator AnchoringSwitchesWardensOnAndLocksTheGrade()
        {
            var w1 = SpawnWarden(new Vector2(4f, 0.9f), false);
            var w2 = SpawnWarden(new Vector2(-4f, 0.9f), false);
            _held!.AddWarden(w1.gameObject); _held.AddWarden(w2.gameObject);
            yield return null;
            Assert.AreEqual("Anchor_Test", _held.PlaceId);
            Assert.IsFalse(w1.gameObject.activeSelf);
            Assert.AreEqual(0f, HeldState.CurrentLevel);

            Assert.IsTrue(Places.Anchor(GameState.World, "Anchor_Test"));
            yield return null;
            Assert.IsTrue(w1.gameObject.activeSelf && w2.gameObject.activeSelf, "the Wardens arrive with the anchoring");
            float t = 0f;
            while (HeldState.CurrentLevel < 0.999f && t < 8f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(1f, HeldState.CurrentLevel, 0.01f, "the grade locks");
            Assert.AreEqual(1f, Shader.GetGlobalFloat(HeldState.HeldGlobal), 0.01f);
            Assert.IsFalse(FadeStages.Advance(GameState.World, "Anchor_Test", 2), "an anchored place no longer fades");

            _room!.SetActive(false);
            yield return null;
            Assert.AreEqual(0f, HeldState.CurrentLevel, 0.01f, "leaving the room takes the grade with it");
        }

        [UnityTest]
        public IEnumerator DeskSealsAFateOnlyOnceThePlaceIsSurveyed()
        {
            _vantageGo = new GameObject("Vantage_Test") { layer = Layer("Trigger") };
            _vantageGo.transform.SetParent(_room!.transform, false);
            _vantageGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var vantage = _vantageGo.AddComponent<VantagePoint>();
            vantage.VantageId = "Anchor_Test/Post";

            _ui = new GameObject("UI");
            _ui.AddComponent<UIDocument>();
            _ui.AddComponent<UiRoot>();
            var menu = _ui.AddComponent<DeskMenu>();
            yield return null; yield return null;
            menu.Open(_ctrl!);
            yield return null;
            int fateRow = menu.FateRow;
            Assert.AreEqual(1 + GameState.World.Equipment.SlotCount, fateRow, "the place row sits under the slots");
            Assert.AreEqual(menu.RowCount, menu.Panel.Q("rows").childCount);
            menu.SetRow(fateRow);
            Assert.IsFalse(menu.CanSeal, "unsurveyed: nothing to seal");
            menu.Step(1);
            Assert.IsFalse(menu.Confirm(), "the desk refuses before the survey");
            Assert.AreEqual(PlaceFate.Unwritten, Places.FateOf(GameState.World, "Anchor_Test"));

            GameState.World.MarkSurveyed("Anchor_Test/Post");
            menu.Refresh();
            Assert.IsTrue(menu.CanSeal);
            menu.Step(1);   // unwritten -> anchor
            Assert.AreEqual(PlaceFate.Anchored, menu.Proposed);
            menu.Step(1);   // -> hold
            Assert.AreEqual(PlaceFate.Held, menu.Proposed);
            Assert.IsFalse(menu.Confirm(), "hold is the walk's to give, not the seal's (DES-13)");
            GameState.World.Set(BoundsWalks.DoneKey("Anchor_Test"), true);
            Assert.IsTrue(menu.Confirm(), "J on the place row seals it");
            Assert.AreEqual(PlaceFate.Held, Places.FateOf(GameState.World, "Anchor_Test"));
            Assert.IsTrue(menu.IsOpen, "sealing does not close the desk");
            menu.Step(1);
            Assert.IsFalse(menu.Confirm(), "a decided place cannot be re-decided");
            StringAssert.Contains("held", ((Label)menu.Panel.Q("rows")[fateRow].Q("value")).text.ToLowerInvariant());
            menu.Close();
        }

        const string Script = @"
title: Start
---
Sable: Anchored, then.
<<anchor Anchor_Test>>
<<if place_fate(""Anchor_Test"") == ""anchored"">>
    Sable: The same tide every day now.
<<endif>>
<<release Anchor_Test>>
===
";

        [UnityTest]
        public IEnumerator YarnDecidesAFate()
        {
            var project = RuntimeYarnBuilder.Build(Script);
            _dialogue = new GameObject("Dialogue");
            var presenter = _dialogue.AddComponent<RecordingPresenter>();
            var service = _dialogue.AddComponent<DialogueService>();
            service.Initialize(project, presenter);
            Assert.IsTrue(service.StartNode("Start"));
            for (int i = 0; i < 120 && service.IsRunning; i++) yield return null;
            Assert.IsFalse(service.IsRunning);
            CollectionAssert.Contains(presenter.Lines, "Sable|The same tide every day now.");
            Assert.AreEqual(PlaceFate.Anchored, Places.FateOf(GameState.World, "Anchor_Test"), "the later <<release>> is refused");
        }
    }
}
