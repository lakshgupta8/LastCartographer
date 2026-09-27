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
    /// Commissions in the running game: the ledger posts, the tracker fulfils from surveys and kills, Yarn drives
    /// the state machine, and the ledger page and journal show it.
    /// </summary>
    public class CommissionPlayTests
    {
        GameObject? _floor, _wren, _systems, _ledgerGo, _ui, _dialogue;
        readonly System.Collections.Generic.List<GameObject> _enemies = new System.Collections.Generic.List<GameObject>();
        WrenController? _ctrl;
        Interactor? _interactor;
        CommissionLedger? _ledger;
        CommissionTracker? _tracker;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            CommissionCatalog.Reset();
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
            _wren.AddComponent<WrenVitals>();
            _interactor = _wren.AddComponent<Interactor>();
            _ctrl.Teleport(Vector2.zero);

            _systems = new GameObject("Systems");
            _tracker = _systems.AddComponent<CommissionTracker>();

            _ledgerGo = new GameObject("Ledger") { layer = Layer("Trigger") };
            _ledgerGo.AddComponent<BoxCollider2D>().isTrigger = true;
            _ledger = _ledgerGo.AddComponent<CommissionLedger>();
            _ledger.HubId = "Saltmarrow";
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var e in _enemies) if (e != null) Object.Destroy(e);
            _enemies.Clear();
            foreach (var go in new[] { _dialogue, _ui, _ledgerGo, _systems, _wren, _floor }) if (go != null) Object.Destroy(go);
            Time.timeScale = 1f;
            CommissionCatalog.Reset();
            GameState.NewGame();
        }

        MarshCrab SpawnCrab(Vector2 pos)
        {
            var go = new GameObject("MarshCrab") { layer = Layer("Enemy") };
            go.transform.position = pos;
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
            go.AddComponent<Rigidbody2D>();
            _enemies.Add(go);
            return go.AddComponent<MarshCrab>();
        }

        [UnityTest]
        public IEnumerator LedgerPostsWhatIsReadyAndTheTrackerFulfilsOnSurvey()
        {
            yield return Frames(2);
            var w = GameState.World;
            _ledger!.Interact(_interactor!);
            Assert.AreEqual(CommissionState.Posted, Commissions.StateOf(w, "saltmarrow.lantern_chain"));
            Assert.AreEqual(CommissionState.Posted, Commissions.StateOf(w, "saltmarrow.bone_bridge"));
            Assert.AreEqual(CommissionState.Posted, Commissions.StateOf(w, "saltmarrow.iris_harvest"));
            Assert.AreEqual(CommissionState.Unknown, Commissions.StateOf(w, "saltmarrow.dotha"), "waits for Sable");
            Assert.AreEqual(CommissionState.Unknown, Commissions.StateOf(w, "saltmarrow.tether_widows"), "waits for the Lamp-Keeper");
            Assert.AreEqual(3, _ledger.Entries().Count);

            Assert.IsTrue(Commissions.Take(w, "saltmarrow.lantern_chain"));
            w.MarkSurveyed("Saltmarrow_Lighthouse/Lamp");    // what the arena does on a win
            Assert.AreEqual(CommissionState.Fulfilled, Commissions.StateOf(w, "saltmarrow.lantern_chain"), "the tracker fulfils on the survey event");
            Assert.IsTrue(Commissions.Close(w, "saltmarrow.lantern_chain"));
            Assert.AreEqual(2, Commissions.Scraps(w));

            w.Set("greybox.met_sable", 1);
            Assert.AreEqual(1, _ledger.PostAvailable(), "Dotha's commission posts once Sable has been met");
            Assert.AreEqual(CommissionState.Posted, Commissions.StateOf(w, "saltmarrow.dotha"));

            // A flag from a conversation finishes it through the tracker, and closing seeds the island.
            Commissions.Take(w, "saltmarrow.dotha");
            w.Set("saltmarrow.dotha.decided", 1);
            Assert.AreEqual(CommissionState.Fulfilled, Commissions.StateOf(w, "saltmarrow.dotha"));
            Commissions.Close(w, "saltmarrow.dotha");
            Assert.IsTrue(Commissions.IsIslandSeeded(w, "Merrows_End"));
        }

        [UnityTest]
        public IEnumerator KillingCrabsFulfilsTheIrisHarvest()
        {
            yield return Frames(2);
            var w = GameState.World;
            _ledger!.Interact(_interactor!);
            var stray = SpawnCrab(new Vector2(8f, 0.5f));
            yield return Fixed(2);
            stray.TakeHit(new HitInfo { Damage = 99, Direction = Vector2.down });
            yield return Fixed(2);
            Assert.IsTrue(Commissions.Take(w, "saltmarrow.iris_harvest"));
            Assert.AreEqual(0, Commissions.Progress(w, CommissionCatalog.Find("saltmarrow.iris_harvest"), 0), "kills before taking do not count");

            for (int i = 0; i < 3; i++)
            {
                var crab = SpawnCrab(new Vector2(4f + i * 2f, 0.5f));
                yield return Fixed(2);
                Assert.IsTrue(crab.TakeHit(new HitInfo { Damage = 99, Direction = Vector2.down }));
                yield return Fixed(2);
                Assert.AreEqual(i + 1, Commissions.Progress(w, CommissionCatalog.Find("saltmarrow.iris_harvest"), 0));
            }
            Assert.AreEqual(CommissionState.Fulfilled, Commissions.StateOf(w, "saltmarrow.iris_harvest"));
            w.Equipment.OwnedInstruments.Remove(InstrumentKind.IrisTincture);
            Assert.IsTrue(Commissions.Close(w, "saltmarrow.iris_harvest"));
            Assert.IsTrue(w.Equipment.OwnsInstrument(InstrumentKind.IrisTincture), "the grower pays in tincture");
            Assert.AreEqual(1, Commissions.Scraps(w));
        }

        const string Script = @"
title: Start
---
<<commission test.quest post>>
<<commission test.quest take>>
<<if commission_is(""test.quest"", ""taken"")>>
    Sable: Taken.
<<endif>>
<<commission test.quest fulfil>>
Sable: State is {commission_state(""test.quest"")}.
<<commission test.quest close>>
===
";

        [UnityTest]
        public IEnumerator YarnCommandAndFunctionsDriveTheLedger()
        {
            CommissionCatalog.Register(new CommissionDef { Id = "test.quest", Title = "Test", Hub = "Test", Steps = new[] { CommissionStep.Flag("test.never", "x") }, RewardScraps = 3 });
            var project = RuntimeYarnBuilder.Build(Script);
            _dialogue = new GameObject("Dialogue");
            var presenter = _dialogue.AddComponent<RecordingPresenter>();
            var service = _dialogue.AddComponent<DialogueService>();
            service.Initialize(project, presenter);
            Assert.IsTrue(service.StartNode("Start"));
            for (int i = 0; i < 120 && service.IsRunning; i++) yield return null;
            Assert.IsFalse(service.IsRunning);
            CollectionAssert.Contains(presenter.Lines, "Sable|Taken.");
            CollectionAssert.Contains(presenter.Lines, "Sable|State is fulfilled.");
            Assert.AreEqual(CommissionState.Closed, Commissions.StateOf(GameState.World, "test.quest"));
            Assert.AreEqual(3, Commissions.Scraps(GameState.World));
        }

        [UnityTest]
        public IEnumerator LedgerPageTakesAndTurnsInAndTheJournalListsIt()
        {
            _ui = new GameObject("UI");
            _ui.AddComponent<UIDocument>();
            _ui.AddComponent<UiRoot>();
            var page = _ui.AddComponent<LedgerView>();
            var journal = _ui.AddComponent<JournalView>();
            yield return Frames(3);
            var w = GameState.World;

            _ledger!.Interact(_interactor!);
            yield return Frames(2);
            Assert.IsTrue(page.IsOpen, "the page opens on the board");
            Assert.IsTrue(_ctrl!.Frozen);
            Assert.AreEqual(3, page.Entries.Count);
            Assert.AreEqual(3, page.Panel.Q("rows").childCount);
            Assert.IsTrue(page.Panel.Q("rows")[0].ClassListContains("posted"));
            Assert.AreEqual("Lantern Chain", ((Label)page.Panel.Q("rows")[0].Q("title")).text);

            Assert.IsTrue(page.Confirm(), "J takes a posted commission");
            Assert.AreEqual(CommissionState.Taken, Commissions.StateOf(w, "saltmarrow.lantern_chain"));
            Assert.IsTrue(page.Panel.Q("rows")[0].ClassListContains("taken"));
            StringAssert.Contains("taken", journal.ToastText);
            Assert.IsTrue(journal.IsToastShowing);
            Assert.IsFalse(page.Confirm(), "nothing to do with a taken one at the board");

            w.MarkSurveyed("Saltmarrow_Lighthouse/Lamp");
            yield return Frames(1);
            Assert.IsTrue(page.Panel.Q("rows")[0].ClassListContains("fulfilled"), "the page follows the change");
            Assert.IsTrue(page.Confirm(), "J turns in a fulfilled commission");
            Assert.AreEqual(CommissionState.Closed, Commissions.StateOf(w, "saltmarrow.lantern_chain"));
            Assert.AreEqual(2, Commissions.Scraps(w));
            StringAssert.Contains("closed", journal.ToastText);
            page.Close();
            yield return Frames(1);
            Assert.IsFalse(_ctrl.Frozen);
            Assert.AreEqual(DisplayStyle.None, page.Panel.style.display.value);

            page.SetRow(0);
            journal.Toggle();
            yield return Frames(1);
            Assert.IsTrue(journal.IsOpen);
            Assert.IsTrue(_ctrl.Frozen);
            Assert.IsNotNull(journal.Panel.Q("entry-saltmarrow.lantern_chain"), "closed ones are listed");
            Assert.IsTrue(journal.Panel.Q("entry-saltmarrow.lantern_chain").ClassListContains("closed"));
            StringAssert.Contains("2", ((Label)journal.Panel.Q("scraps")).text);
            journal.Toggle();
            yield return Frames(1);
            Assert.IsFalse(journal.IsOpen);
            Assert.IsFalse(_ctrl.Frozen);
        }
    }
}
