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
    /// Flavour text on the pages (NAR-17): the desk shows each Charter's and Instrument's line under its blurb (Hale's way
    /// with the lens once he is beaten), the atlas's margin fills in as a place is drawn, and the journal lists what
    /// Wren carries, each with its line.
    /// </summary>
    public class FlavourPlayTests
    {
        GameObject? _floor, _wren, _ui;
        WrenController? _ctrl;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
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
            _wren.AddComponent<WrenVitals>();
            _wren.AddComponent<CharterSet>();
            _ctrl.Teleport(new Vector2(-6f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _ui, _wren, _floor }) if (go != null) Object.Destroy(go);
            Atlas.Reset();
            GameState.NewGame();
        }

        IEnumerator Ui()
        {
            _ui = new GameObject("UI");
            _ui.AddComponent<UIDocument>();
            _ui.AddComponent<UiRoot>();
            _ui.AddComponent<DeskMenu>();
            _ui.AddComponent<AtlasView>();
            _ui.AddComponent<JournalView>();
            yield return null; yield return null;
        }

        static string Text(VisualElement root, string name) => root.Q<Label>(name)?.text ?? "";

        [UnityTest]
        public IEnumerator TheDeskReadsWhatEachThingIs()
        {
            yield return Ui();
            var menu = _ui!.GetComponent<DeskMenu>();
            menu.Open(_ctrl!);
            yield return null;
            menu.SetRow(0);
            Assert.AreEqual(Flavour.ForCharter(CharterKind.Surveyor), Text(menu.Panel, "flavour"), "the Surveyor's charter, under its blurb");
            StringAssert.Contains("Slash, slash, thrust", Text(menu.Panel, "blurb"), "the blurb still says what it does");

            var e = GameState.World.Equipment;
            int lens = 2;
            e.OwnedInstruments.Add(InstrumentKind.SightingLens);
            Assert.IsTrue(e.Equip(lens, InstrumentKind.SightingLens, -1), "on the belt");
            menu.Refresh();
            menu.SetRow(lens + 1);
            Assert.AreEqual(Flavour.ForInstrument(InstrumentKind.SightingLens), Text(menu.Panel, "flavour"));
            GameState.World.Set(Bosses.FlagKey("hale"), true);
            menu.Refresh();
            Assert.AreEqual(Flavour.HalesLens, Text(menu.Panel, "flavour"), "held the way Hale held it");
            menu.SetRow(menu.MaskRow);
            Assert.AreEqual("", Text(menu.Panel, "flavour"), "the masks row has no line of its own");
            menu.Close();
        }

        [UnityTest]
        public IEnumerator TheAtlasMarginFillsInAsThePageIsDrawn()
        {
            yield return Ui();
            var atlas = _ui!.GetComponent<AtlasView>();
            var w = GameState.World;
            atlas.Open(_ctrl);
            yield return null;
            Assert.IsNull(atlas.Panel.Q("place-Saltmarrow_A").Q("zone-note"), "nothing drawn, nothing in the margin");
            Assert.IsNull(atlas.Panel.Q("region-note"));
            atlas.Close();

            Atlas.Survey(w, "Saltmarrow_A/Reedmother");
            atlas.Open(_ctrl);
            yield return null;
            Assert.AreEqual(Flavour.ForZone("Saltmarrow.Quay"), Text(atlas.Panel.Q("place-Saltmarrow_A"), "zone-note"), "the quay, drawn");
            Assert.AreEqual(Flavour.ForRegion(Region.Saltmarrow), Text(atlas.Panel, "region-note"), "and the page's heading");
            Assert.IsNull(atlas.Panel.Q("place-Saltmarrow_B").Q("zone-note"), "Merrow's End is not drawn yet");
            atlas.Close();
        }

        [UnityTest]
        public IEnumerator TheJournalListsWhatSheCarries()
        {
            yield return Ui();
            var w = GameState.World;
            w.Set(AbilitySet.FlagKey(Ability.Wingbeat), true);
            w.Set(Keystones.FlagKey("windreach"), true);
            Memories.Bind(w, "isolde.first_sight");
            Economy.AddSeeds(w, 3);
            var atlas = _ui!.GetComponent<AtlasView>();
            atlas.Open(_ctrl);
            yield return null;
            var carried = atlas.Panel.Q("carried");
            Assert.IsNotNull(carried, "the journal's Carried section");
            Assert.AreEqual(Flavour.ForAbility(Ability.Wingbeat), Text(carried.Q("ability-Wingbeat"), "flavour"));
            Assert.AreEqual(Keystones.NameOf("windreach"), Text(carried.Q("keystone-windreach"), "what"));
            Assert.AreEqual(Flavour.ForKeystone("windreach"), Text(carried.Q("keystone-windreach"), "flavour"));
            Assert.IsNull(carried.Q("keystone-aury"), "only the stones she has");
            Assert.AreEqual(Flavour.ForMemory("isolde.first_sight"), Text(carried.Q("memory-isolde.first_sight"), "flavour"));
            StringAssert.Contains("3 iris seeds", Text(carried.Q("purse-seeds"), "what"));
            Assert.AreEqual(Flavour.ForCurrency(Flavour.VellumScrap), Text(carried.Q("purse-scraps"), "flavour"));
            atlas.Close();
        }
    }
}
