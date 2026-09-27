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
    /// <summary>The economy in play (DES-05): seeds drop and are picked up, a talk opens the shop, the desk sells masks.</summary>
    public class EconomyPlayTests
    {
        sealed class Prey : Enemy { protected override void Tick(float dt) { } }

        GameObject? _floor, _wren, _drops, _prey, _ui, _dialogue;
        WrenController? _ctrl;
        WrenVitals? _vitals;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

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
            _vitals = _wren.AddComponent<WrenVitals>();
            _ctrl.Teleport(new Vector2(-6f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var s in Object.FindObjectsByType<IrisSeed>(FindObjectsSortMode.None)) Object.Destroy(s.gameObject);
            foreach (var go in new[] { _dialogue, _ui, _prey, _drops, _wren, _floor }) if (go != null) Object.Destroy(go);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator EnemiesDropSeedsAndWrenPicksThemUp()
        {
            _drops = new GameObject("Drops");
            var drops = _drops.AddComponent<IrisSeedDrops>();
            _prey = new GameObject("Prey") { layer = Layer("Enemy") };
            _prey.transform.position = new Vector3(4f, 0.9f, 0f);
            _prey.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.8f);
            _prey.AddComponent<Rigidbody2D>();
            var prey = _prey.AddComponent<Prey>();
            yield return Fixed(2);
            prey.SetMaxHealth(1);
            Assert.IsTrue(prey.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }));
            Assert.IsTrue(prey.IsDead);
            yield return null;
            Assert.AreEqual(1, drops.Spawned, "one seed for an unlisted family");
            var seeds = Object.FindObjectsByType<IrisSeed>(FindObjectsSortMode.None);
            Assert.AreEqual(1, seeds.Length);
            Assert.AreEqual(4f, seeds[0].transform.position.x, 0.1f, "where it fell");
            Assert.AreEqual(0, Economy.Seeds(GameState.World));

            int collected = 0;
            IrisSeed.Collected += (_, n) => collected += n;
            _ctrl!.Teleport(new Vector2(4f, 0f));
            for (int i = 0; i < 30 && Economy.Seeds(GameState.World) == 0; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, Economy.Seeds(GameState.World), "walking through takes it");
            Assert.AreEqual(1, collected);
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<IrisSeed>(FindObjectsSortMode.None).Length, "and it is gone");
            Assert.AreEqual(10, IrisSeedDrops.DropsFor("LampKeeper"));
        }

        const string Script = @"
title: Start
---
Sable: Tools, mostly.
<<shop Saltmarrow>>
===
";

        [UnityTest]
        public IEnumerator ATalkOpensTheShopAndJBuys()
        {
            _ui = new GameObject("UI");
            _ui.AddComponent<UIDocument>();
            _ui.AddComponent<UiRoot>();
            var shop = _ui.AddComponent<ShopView>();
            var project = RuntimeYarnBuilder.Build(Script);
            _dialogue = new GameObject("Dialogue");
            var presenter = _dialogue.AddComponent<RecordingPresenter>();
            var service = _dialogue.AddComponent<DialogueService>();
            service.Initialize(project, presenter);
            yield return null; yield return null;
            Assert.IsTrue(service.StartNode("Start"));
            for (int i = 0; i < 120 && service.IsRunning; i++) yield return null;
            Assert.IsFalse(service.IsRunning);
            for (int i = 0; i < 10 && !shop.IsOpen; i++) yield return null;
            Assert.IsTrue(shop.IsOpen, "the shop opens once the talk is over");
            Assert.AreEqual("Saltmarrow", shop.Hub);
            Assert.IsTrue(_ctrl!.Frozen);
            Assert.AreEqual(4, shop.Items.Count);
            Assert.AreEqual(InstrumentKind.FieldLantern, shop.Items[0].Kind);
            Assert.IsFalse(shop.Confirm(), "no seeds");

            Economy.AddSeeds(GameState.World, 20);
            shop.SetRow(0);
            StringAssert.Contains("12 ✿", ((Label)shop.Panel.Q("rows")[0].Q("price")).text);
            Assert.IsTrue(shop.Confirm(), "J buys the lantern");
            Assert.IsTrue(GameState.World.Equipment.OwnsInstrument(InstrumentKind.FieldLantern));
            Assert.AreEqual(8, Economy.Seeds(GameState.World));
            StringAssert.Contains("owned", ((Label)shop.Panel.Q("rows")[0].Q("price")).text);
            Assert.IsFalse(shop.Confirm(), "once");
            shop.Close();
            Assert.IsFalse(shop.IsOpen);
            Assert.IsFalse(_ctrl.Frozen);
        }

        [UnityTest]
        public IEnumerator TheDeskSellsMasksForScraps()
        {
            _ui = new GameObject("UI");
            _ui.AddComponent<UIDocument>();
            _ui.AddComponent<UiRoot>();
            var menu = _ui.AddComponent<DeskMenu>();
            yield return null; yield return null;
            Assert.AreEqual(5, _vitals!.MaxMasks);
            menu.Open(_ctrl!);
            yield return null;
            Assert.AreEqual(menu.BeltRow + 1, menu.Panel.Q("rows").childCount, "Charter, slots, place, masks, belt");
            menu.SetRow(menu.MaskRow);
            Assert.IsFalse(menu.Confirm(), "no scraps");
            Assert.AreEqual(5, _vitals.MaxMasks);

            Commissions.AddScraps(GameState.World, 3);
            menu.Refresh();
            StringAssert.Contains("3 scraps", ((Label)menu.Panel.Q("rows")[menu.MaskRow].Q("value")).text);
            Assert.IsTrue(menu.Confirm(), "J stitches a mask in");
            Assert.AreEqual(6, _vitals.MaxMasks, "one more mask");
            Assert.AreEqual(6, _vitals.Masks, "and it is full");
            Assert.AreEqual(0, Economy.Scraps(GameState.World));
            Assert.IsTrue(menu.IsOpen, "buying keeps the desk open");
            Assert.IsFalse(menu.Confirm(), "no more scraps");
            menu.SetRow(menu.BeltRow);
            Assert.IsFalse(menu.Confirm());
            Commissions.AddScraps(GameState.World, 4);
            Assert.IsTrue(menu.Confirm(), "the fourth loop on the belt");
            Assert.AreEqual(4, GameState.World.Equipment.SlotCount);
            Assert.AreEqual(1 + 4, menu.FateRow, "the place row moves down a slot");
            menu.Close();
        }
    }
}
