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
    /// <summary>The atlas UI layer: HUD binding, the dialogue page through the presenter, the desk page, the boss bar.</summary>
    public class UiTests
    {
        const string Script = @"
title: Start
---
Sable: Wings high, stranger.
-> Where is this?
    Sable: The Saltmarrow.
-> ...
    Sable: Quiet one.
Sable: Everything here has a price.
===
";

        GameObject? _floor, _wren, _ui, _dialogue, _desk, _arenaGo, _bossGo;
        WrenController? _ctrl;
        WrenVitals? _vitals;
        Inkwell? _ink;
        InstrumentBelt? _belt;
        ScriptedInput? _input;
        HudView? _hud;
        DialogueView? _view;
        DeskMenu? _menu;
        BossView? _bossView;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

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
            _ink = _wren.AddComponent<Inkwell>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _wren.AddComponent<QuillStrike>();
            _wren.AddComponent<Flourishes>();
            _belt = _wren.AddComponent<InstrumentBelt>();
            _wren.AddComponent<CharterSet>();
            _ctrl.Teleport(Vector2.zero);

            _ui = new GameObject("UI");
            _ui.AddComponent<UIDocument>();
            _ui.AddComponent<UiRoot>();
            _hud = _ui.AddComponent<HudView>();
            _view = _ui.AddComponent<DialogueView>();
            _menu = _ui.AddComponent<DeskMenu>();
            _bossView = _ui.AddComponent<BossView>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _dialogue, _desk, _arenaGo, _bossGo, _ui, _wren, _floor }) if (go != null) Object.Destroy(go);
            Time.timeScale = 1f;
            GameState.NewGame();
            Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey);
        }

        [UnityTest]
        public IEnumerator TheHudFollowsThePlayersLanguage()
        {
            yield return Frames(3);
            var death = _hud!.Root.parent.Q<Label>("hud-death");
            Assert.IsNotNull(death);
            Assert.AreEqual("the ink runs out", death.text);
            Loc.SetLocale(Loc.Pseudo);
            yield return Frames(1);
            Assert.IsTrue(Loc.LooksPseudo(death.text), "re-texted the moment the locale changes: " + death.text);
            Loc.Register("fr", new System.Collections.Generic.Dictionary<string, string> { { "hud.death", "l'encre s'épuise" } });
            Loc.SetLocale("fr");
            yield return Frames(1);
            Assert.AreEqual("l'encre s'épuise", death.text);
            Loc.SetLocale(Loc.Base);
            yield return Frames(1);
            Assert.AreEqual("the ink runs out", death.text);
        }

        [UnityTest]
        public IEnumerator HudShowsClarityOnlyWhileItRuns()
        {
            yield return Frames(3);
            var bar = _hud!.Root.Q("hud-clarity");
            Assert.IsNotNull(bar, "the Clarity meter has a place on the HUD");
            Assert.AreEqual(DisplayStyle.None, bar.style.display.value, "not before she has Clarity");
            _wren!.GetComponent<AbilitySet>().Unlock(Ability.Clarity);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(DisplayStyle.None, bar.style.display.value, "hidden while it is full and she is held");
            UntetheredZone.Make("White", _floor!.transform, new Vector2(0f, 4f), new Vector2(10f, 10f));
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(DisplayStyle.Flex, bar.style.display.value, "shown in the white");
            var fill = bar.Q("hud-clarity-fill");
            Assert.Less(fill.style.width.value.value, 100f, "and running down");
            Assert.Greater(fill.style.width.value.value, 0f);
        }

        [UnityTest]
        public IEnumerator HudShowsMasksInkCharterAndSlots()
        {
            yield return Frames(3);
            Assert.IsTrue(_hud!.IsBound, "HUD finds Wren");
            var root = _hud.Root;
            Assert.IsNotNull(root);
            var masks = root.Q("hud-masks");
            Assert.AreEqual(5, masks.childCount);
            Assert.AreEqual(5, Filled(masks));
            _vitals!.Damage(1);
            yield return Frames(2);
            Assert.AreEqual(4, Filled(masks), "a lost mask empties a diamond");

            var pips = root.Q("hud-ink");
            Assert.AreEqual(9, pips.childCount);
            _ink!.Add(3);
            yield return Frames(2);
            Assert.AreEqual(3, Filled(pips));

            Assert.AreEqual("Surveyor's Charter", ((Label)root.Q("hud-charter")).text);

            var slots = root.Q("hud-slots");
            Assert.AreEqual(3, slots.childCount);
            Assert.IsTrue(slots[0].ClassListContains("selected"));
            Assert.AreEqual("Compass-dart", ((Label)slots[0].Q("name")).text);
            Assert.AreEqual("12", ((Label)slots[0].Q("uses")).text);
            _belt!.Select(1);
            yield return Frames(2);
            Assert.IsFalse(slots[0].ClassListContains("selected"));
            Assert.IsTrue(slots[1].ClassListContains("selected"));
        }

        static int Filled(VisualElement parent)
        {
            int n = 0;
            for (int i = 0; i < parent.childCount; i++) if (parent[i].ClassListContains("filled")) n++;
            return n;
        }

        [UnityTest]
        public IEnumerator DialoguePageShowsLinesAndOptionsThroughThePresenter()
        {
            yield return Frames(2);
            var project = RuntimeYarnBuilder.Build(Script);
            _dialogue = new GameObject("Dialogue");
            var presenter = _dialogue.AddComponent<ViewDialoguePresenter>();
            var service = _dialogue.AddComponent<DialogueService>();
            service.Initialize(project, presenter);
            Assert.IsTrue(service.StartNode("Start"));
            for (int i = 0; i < 60 && !_view!.IsVisible; i++) yield return null;
            Assert.IsTrue(_view!.IsVisible, "the page opens");
            Assert.AreEqual("Sable", _view.SpeakerText);
            Assert.AreEqual("Wings high, stranger.", _view.LineText);
            Assert.IsTrue(_ctrl!.Frozen, "Wren waits while the page is open");

            presenter.Advance();
            for (int i = 0; i < 60 && !_view.IsShowingOptions; i++) yield return null;
            Assert.IsTrue(_view.IsShowingOptions);
            Assert.AreEqual(2, _view.OptionCount);
            Assert.IsTrue(_view.OptionCount > 0 && _view.OptionCount == 2);

            presenter.Choose(1);
            for (int i = 0; i < 60 && (_view.IsShowingOptions || !_view.IsVisible); i++) yield return null;
            Assert.AreEqual("Quiet one.", _view.LineText);
            presenter.Advance();
            for (int i = 0; i < 60 && _view.LineText != "Everything here has a price."; i++) yield return null;
            Assert.AreEqual("Everything here has a price.", _view.LineText);
            presenter.Advance();
            for (int i = 0; i < 60 && service.IsRunning; i++) yield return null;
            Assert.IsFalse(service.IsRunning);
            Assert.IsFalse(_view.IsVisible, "the page closes");
            Assert.IsFalse(_ctrl.Frozen);
        }

        [UnityTest]
        public IEnumerator DeskPageOpensOnRestSwapsTheCharterAndCloses()
        {
            yield return Frames(3);
            var interactor = _wren!.AddComponent<Interactor>();
            _desk = new GameObject("Desk") { layer = Layer("Trigger") };
            var col = _desk.AddComponent<BoxCollider2D>(); col.isTrigger = true;
            var desk = _desk.AddComponent<DraftingDesk>();
            desk.Interact(interactor);
            yield return Frames(2);
            Assert.IsTrue(_menu!.IsOpen);
            Assert.IsTrue(_ctrl!.Frozen);
            Assert.IsNotNull(_menu.Panel);
            Assert.AreEqual(DisplayStyle.Flex, _menu.Panel.style.display.value);
            Assert.AreEqual(_menu.RowCount, _menu.Panel.Q("rows").childCount, "Charter row, three slots, the place, masks, belt");
            Assert.AreEqual(7, _menu.RowCount);
            Assert.IsTrue(_menu.Panel.Q("rows")[0].ClassListContains("selected"));

            _menu.Step(1);
            Assert.AreEqual(CharterKind.Warden, GameState.World.Equipment.Charter);
            Assert.AreEqual("Warden's Charter", _wren.GetComponent<CharterSet>().Current.DisplayName);
            StringAssert.Contains("Warden", ((Label)_menu.Panel.Q("rows")[0].Q("value")).text);

            _menu.SetRow(1);
            _menu.Step(1);
            Assert.AreEqual(InstrumentKind.PlumbWeight, GameState.World.Equipment.Slots[0].Kind, "the next tool moves into slot 1");

            _menu.Close();
            yield return Frames(2);
            Assert.IsFalse(_menu.IsOpen);
            Assert.IsFalse(_ctrl.Frozen);
            Assert.AreEqual(DisplayStyle.None, _menu.Panel.style.display.value);
        }

        [UnityTest]
        public IEnumerator BossBarAppearsForTheFightAndCarriesThePhaseLine()
        {
            _ctrl!.Teleport(new Vector2(-12f, 0f));   // outside the zone until the test walks in
            _bossGo = new GameObject("LampKeeper") { layer = Layer("Enemy") };
            _bossGo.transform.position = new Vector3(3f, 8.6f, 0f);
            _bossGo.AddComponent<BoxCollider2D>().size = new Vector2(1.6f, 1.2f);
            _bossGo.AddComponent<Rigidbody2D>();
            var boss = _bossGo.AddComponent<LampKeeper>();
            boss.SetMaxHealth(24);
            _arenaGo = new GameObject("Arena") { layer = Layer("Trigger") };
            _arenaGo.transform.position = new Vector3(3f, 5f, 0f);
            var zone = _arenaGo.AddComponent<BoxCollider2D>();
            zone.isTrigger = true; zone.size = new Vector2(17f, 11f);
            var arena = _arenaGo.AddComponent<BossArena>();
            arena.Configure(boss, new GameObject[0], "ui_test", Ability.None);
            arena.IntroSeconds = 0f;
            yield return Frames(3);
            Assert.IsFalse(_bossView!.IsShowing);

            _ctrl.Teleport(new Vector2(0f, 0f));
            for (int i = 0; i < 30 && !boss.IsFightActive; i++) yield return new WaitForFixedUpdate();
            yield return Frames(2);
            Assert.IsTrue(_bossView.IsShowing);
            Assert.AreEqual(boss.BossName, _bossView.NameText);
            Assert.AreEqual(1f, _bossView.FillFraction, 0.001f);

            for (int i = 0; i < 9; i++)
            {
                boss.ForceGrounded(5f);
                boss.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right });
                yield return new WaitForFixedUpdate();
            }
            yield return Frames(2);
            Assert.AreEqual(2, boss.Phase);
            Assert.AreEqual(15f / 24f, _bossView.FillFraction, 0.001f);
            Assert.AreEqual(boss.PhaseLine(2), _bossView.CaptionText, "the phase line shows as a caption");

            _vitals!.Damage(99);
            yield return Frames(2);
            Assert.IsFalse(_bossView.IsShowing, "bar goes when the fight resets");
        }
    }
}
