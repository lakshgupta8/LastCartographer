#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace OWSBG.Tests
{
    /// <summary>
    /// The drawn UI in play (ENV-11): the HUD's masks are the feathers and empty one by one, the Inkwell is the bottle
    /// and its fill follows the pips, the pages are the drawn paper with the quill as their marker and the serif on
    /// their titles, and the paper stands down under high-contrast ink and comes back.
    /// </summary>
    public class UiArtPlayTests
    {
        GameObject? _floor, _wren, _ui;
        WrenController? _ctrl;
        WrenVitals? _vitals;
        Inkwell? _ink;
        HudView? _hud;
        DeskMenu? _menu;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [SetUp]
        public void SetUp()
        {
            GameState.NewGame();
            Options.HighContrast = false;
            InkArt.Forget();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _ink = _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = new ScriptedInput();
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            _wren.AddComponent<QuillStrike>();
            _wren.AddComponent<Flourishes>();
            _wren.AddComponent<InstrumentBelt>();
            _wren.AddComponent<CharterSet>();
            _ctrl.Teleport(Vector2.zero);

            _ui = new GameObject("UI");
            _ui.AddComponent<UIDocument>();
            _ui.AddComponent<UiRoot>();
            _hud = _ui.AddComponent<HudView>();
            _menu = _ui.AddComponent<DeskMenu>();
        }

        [TearDown]
        public void TearDown()
        {
            Options.HighContrast = false;
            foreach (var go in new[] { _ui, _wren, _floor }) if (go != null) Object.Destroy(go);
            GameState.NewGame();
        }

        static Texture2D? Shown(VisualElement e) => e.style.backgroundImage.value.texture;

        [UnityTest]
        public IEnumerator TheHudDrawsFeathersAndTheBottle()
        {
            if (!InkArt.Available) Assert.Ignore("the UI's drawings are not packed here");
            yield return Frames(3);
            Assert.IsTrue(_hud!.IsBound);
            var masks = _hud.Root.Q("hud-masks");
            Assert.AreEqual(_vitals!.MaxMasks, masks.childCount);
            for (int i = 0; i < masks.childCount; i++)
                Assert.AreSame(InkArt.Tex("UI_MaskFull"), Shown(masks[i]), "mask " + i + " is a full feather");
            _vitals.Damage(1);
            yield return Frames(2);
            Assert.AreSame(InkArt.Tex("UI_MaskEmpty"), Shown(masks[masks.childCount - 1]), "the last feather empties");
            Assert.AreSame(InkArt.Tex("UI_MaskFull"), Shown(masks[0]), "the first stays");

            Assert.IsNotNull(_hud.Well, "the Inkwell is the bottle");
            Assert.AreSame(InkArt.Tex("UI_Inkwell"), Shown(_hud.Well));
            var pips = _hud.Root.Q("hud-ink");
            Assert.AreEqual(_ink!.MaxPips, pips.childCount, "the pips are the marks up its glass");
            Assert.AreEqual(_hud.Well, pips.parent);
            float expected = _ink.Pips / (float)_ink.MaxPips;
            Assert.AreEqual(expected, _hud.WellFraction, 0.001f, "the ink stands as high as the pips say");
            _ink.Add(_ink.MaxPips);
            yield return Frames(2);
            Assert.AreEqual(1f, _hud.WellFraction, 0.001f, "full to the brim");
            Assert.IsTrue(_ink.TrySpend(_ink.Pips));
            yield return Frames(2);
            Assert.AreEqual(0f, _hud.WellFraction, 0.001f, "and dry");

            var death = _hud.Root.parent.Q<Label>("hud-death");
            Assert.AreSame(InkArt.TitleFont, death.style.unityFontDefinition.value.font, "the death line is in the serif");
        }

        [UnityTest]
        public IEnumerator ThePagesAreDrawnPaperAndStandDownUnderHighContrast()
        {
            if (!InkArt.Available) Assert.Ignore("the UI's drawings are not packed here");
            yield return Frames(2);
            _menu!.Open(_ctrl!);
            yield return Frames(2);
            var panel = _menu.Panel;
            Assert.IsTrue(panel.ClassListContains(InkArt.PaperClass));
            Assert.AreSame(InkArt.Tex("UI_Page"), Shown(panel), "the desk is a drawn page");
            Assert.Greater(panel.style.unitySliceLeft.value, 0, "sliced at its edges");
            var title = panel.Q<Label>("title");
            Assert.AreSame(InkArt.TitleFont, title.style.unityFontDefinition.value.font, "its title is in the serif");
            var marker = panel.Q("rows")[0].Q("marker");
            Assert.AreSame(InkArt.Tex("UI_Marker"), Shown(marker), "the quill marks the chosen row");
            Assert.AreSame(InkArt.CharterIcon(CharterKind.Surveyor), Shown(panel.Q("rows")[0].Q("icon")), "the Charter row shows her cowl");

            Options.HighContrast = true;
            yield return Frames(2);
            Assert.IsNull(Shown(panel), "under high contrast the paper is flat");
            Assert.AreEqual(InkTheme.Paper, panel.style.backgroundColor.value, "opaque white");
            Assert.AreEqual(0, panel.style.unitySliceLeft.value);
            Assert.AreSame(InkArt.Tex("UI_Marker"), Shown(panel.Q("rows")[0].Q("marker")), "the glyphs stay");

            Options.HighContrast = false;
            yield return Frames(2);
            Assert.AreSame(InkArt.Tex("UI_Page"), Shown(panel), "and the page comes back");
        }
    }
}
