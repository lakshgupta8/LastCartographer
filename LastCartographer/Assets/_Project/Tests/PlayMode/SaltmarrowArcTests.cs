#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The Saltmarrow arc's writing in the shipped scenes (NAR-04): Sable's first quay talk and her state-aware
    /// return, Dotha's season and its three ways, and Halvard's first hunt when the lamp is lit.
    /// </summary>
    public class SaltmarrowArcTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;
            CommissionCatalog.Reset(); CommissionCatalog.EnsureDefaults();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
            ScreenFade.Clear();
            GameState.NewGame();
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        /// <summary>Click through a conversation, taking the given option whenever one is offered (the last one if fewer).</summary>
        static IEnumerator Converse(ViewDialoguePresenter presenter, DialogueView view, DialogueService svc, int choice, float seconds = 40f)
        {
            float t = 0f;
            while (svc.IsRunning && t < seconds)
            {
                if (presenter.IsShowingOptions) presenter.Choose(Mathf.Min(choice, Mathf.Max(0, view.OptionCount - 1)));
                else if (presenter.IsShowingLine) presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(svc.IsRunning, "the conversation ends");
        }

        /// <summary>Click through lines until a choice is offered (or the talk ends).</summary>
        static IEnumerator UntilOptions(ViewDialoguePresenter presenter, DialogueService svc, float seconds = 10f)
        {
            float t = 0f;
            while (svc.IsRunning && !presenter.IsShowingOptions && t < seconds)
            {
                if (presenter.IsShowingLine) presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(presenter.IsShowingOptions, "a choice is offered");
        }

        IEnumerator LoadQuay()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == "Greybox_Saltmarrow_A" && !RoomManager.Instance.IsTransitioning, 10f, "the quay");
        }

        [UnityTest]
        public IEnumerator SableSpeaksToTheStateOfTheCoast()
        {
            yield return LoadQuay();
            var w = GameState.World;
            var svc = DialogueService.Instance!;
            var presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            var view = UiRoot.Instance.GetComponent<DialogueView>();
            var sable = GameObject.Find("Sable_Greybox").GetComponent<NpcTalker>();
            Assert.AreEqual("Quay_Sable", sable.StartNode, "the real script is wired");

            Assert.IsTrue(svc.StartNode("Quay_Sable"));
            yield return Until(() => view.IsVisible, 5f, "the page");
            StringAssert.Contains("boardwalk", view.LineText, "her first quay line");
            yield return Converse(presenter, view, svc, 1);
            Assert.IsTrue(w.Is("saltmarrow.sable.talked"));

            // Again: nothing drawn yet, so she points at the reed; the board once; "Nothing today" ends it.
            Assert.IsTrue(svc.StartNode("Quay_Sable"));
            yield return Until(() => view.IsVisible, 5f, "the page");
            StringAssert.Contains("Still here", view.LineText);
            yield return Converse(presenter, view, svc, 1);
            Assert.IsTrue(w.Is("saltmarrow.sable.ledger"));
            Assert.IsFalse(w.Is("saltmarrow.sable.reed"));

            // The Reedmother drawn and the Bone Bridge taken: she asks about the whale; hearing it teaches the walk.
            Atlas.Survey(w, "Saltmarrow_A/Reedmother");
            Commissions.Post(w, "saltmarrow.bone_bridge");
            Assert.IsTrue(Commissions.Take(w, "saltmarrow.bone_bridge"));
            Assert.IsTrue(svc.StartNode("Quay_Sable"));
            yield return Converse(presenter, view, svc, 0);
            Assert.IsTrue(w.Is("saltmarrow.sable.reed"));
            Assert.IsTrue(w.Is("saltmarrow.bone_bridge.heard"), "\"I heard it\" writes the whale's step");
            Assert.IsTrue(BoundsWalks.IsLearned(w), "the whale's song is the roll-call");
            Assert.IsTrue(Commissions.Is(w, "saltmarrow.bone_bridge", CommissionState.Fulfilled));

            // The lamp lit and Halvard met: she notices both, and the third lighthouse can be asked about.
            w.Set("boss.lamp_keeper.defeated", true);
            w.Set("act1.unlicensed", true);
            Assert.IsTrue(svc.StartNode("Quay_Sable"));
            yield return Until(() => view.IsVisible, 5f, "the page");
            StringAssert.Contains("Unlicensed", view.LineText);
            yield return Converse(presenter, view, svc, 1);   // "The third lighthouse. Who keeps it?"
            Assert.IsTrue(w.Is("saltmarrow.sable.aury"), "Aury's line was said");
        }

        [UnityTest]
        public IEnumerator DothaOffersThreeWaysOnceTheWhaleIsHeard()
        {
            yield return LoadQuay();
            var w = GameState.World;
            var svc = DialogueService.Instance!;
            var presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            var view = UiRoot.Instance.GetComponent<DialogueView>();

            Assert.IsTrue(svc.StartNode("Merrow_Dotha"));
            yield return Converse(presenter, view, svc, 2);   // "..."
            Assert.IsTrue(w.Is("saltmarrow.dotha.met"));
            Assert.IsFalse(w.Is("saltmarrow.dotha.decided"));

            Commissions.Post(w, "saltmarrow.dotha");
            Assert.IsTrue(Commissions.Take(w, "saltmarrow.dotha"));
            Assert.IsTrue(svc.StartNode("Merrow_Dotha"));
            yield return UntilOptions(presenter, svc);
            Assert.AreEqual(3, view.OptionCount);
            Assert.IsTrue(view.IsOptionAvailable(1));
            Assert.IsFalse(view.IsOptionAvailable(2), "without the whale, the walk is offered dimmed");
            presenter.Choose(1);   // let it go
            yield return Converse(presenter, view, svc, 1);
            Assert.AreEqual(2, w.Get("saltmarrow.dotha.decided"));
            Assert.AreEqual(2, FadeStages.Get(w, "Saltmarrow_B"), "Merrow's End begins to go");
            Assert.IsTrue(Commissions.Is(w, "saltmarrow.dotha", CommissionState.Fulfilled));

            // A fresh coast where the whale has been heard: three ways, and the walk is the third.
            GameState.NewGame();
            w = GameState.World;
            w.Set("saltmarrow.bone_bridge.heard", true);
            Commissions.Post(w, "saltmarrow.dotha");
            Commissions.Take(w, "saltmarrow.dotha");
            Assert.IsTrue(svc.StartNode("Merrow_Dotha"));
            yield return UntilOptions(presenter, svc);
            Assert.AreEqual(3, view.OptionCount);
            Assert.IsTrue(view.IsOptionAvailable(2), "the whale heard: the walk can be taken");
            presenter.Choose(0);   // write it as it was
            yield return Converse(presenter, view, svc, 0);
            Assert.AreEqual(1, w.Get("saltmarrow.dotha.decided"));
            Assert.IsTrue(w.Is("saltmarrow.dotha.remembered"));
            Assert.IsTrue(svc.StartNode("Merrow_Dotha"));
            yield return Until(() => view.IsVisible, 5f, "the page");
            StringAssert.Contains("seal it", view.LineText, "written but not sealed");
            yield return Converse(presenter, view, svc, 0);
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator HalvardFindsHerAtTheLitLamp()
        {
            var w = GameState.World;
            w.Set("boss.lamp_keeper.defeated", true);
            Atlas.Survey(w, "Saltmarrow_Lighthouse/Lamp");
            yield return LoadQuay();
            var rm = RoomManager.Instance;
            var svc = DialogueService.Instance!;
            var presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            var view = UiRoot.Instance.GetComponent<DialogueView>();
            var wren = Object.FindFirstObjectByType<WrenController>();

            rm.Transition("Greybox_Saltmarrow_Lighthouse", "West");
            yield return Until(() => rm.CurrentRoom == "Greybox_Saltmarrow_Lighthouse" && !rm.IsTransitioning, 15f, "the lighthouse");
            var hunt = Object.FindFirstObjectByType<HalvardHunt>();
            Assert.IsNotNull(hunt, "the lighthouse has the hunt");
            yield return Until(() => hunt!.Played, 6f, "Halvard to arrive: the lamp is lit and he has not been met");
            Assert.IsTrue(hunt!.Cutscene.IsPlaying);
            Assert.IsTrue(wren.Frozen, "she stands where he tells her");
            var him = hunt.Halvard;
            Assert.IsNotNull(him);
            Assert.IsTrue(him!.activeSelf, "he is in the room");
            float x0 = him.transform.position.x;
            yield return Until(() => svc.IsRunning, 8f, "his lines");
            Assert.Greater(him.transform.position.x, x0 + 3f, "he walked in first");
            yield return Until(() => view.IsVisible, 5f, "the page");
            StringAssert.Contains("Three paces", view.LineText);
            yield return Converse(presenter, view, svc, 0, 60f);
            Assert.IsTrue(w.Is("act1.halvard_met"));
            Assert.IsTrue(w.Is("act1.unlicensed"), "she is unlicensed now");
            yield return Until(() => !hunt.Cutscene.IsPlaying, 10f, "him to leave");
            Assert.IsFalse(him.activeSelf, "and he is gone");
            Assert.IsFalse(wren.Frozen);

            // It does not happen twice.
            rm.Transition("Greybox_Saltmarrow_Chain_3", "East");
            yield return Until(() => rm.CurrentRoom == "Greybox_Saltmarrow_Chain_3" && !rm.IsTransitioning, 15f, "the faded lighthouse");
            rm.Transition("Greybox_Saltmarrow_Lighthouse", "West");
            yield return Until(() => rm.CurrentRoom == "Greybox_Saltmarrow_Lighthouse" && !rm.IsTransitioning, 15f, "back");
            hunt = Object.FindFirstObjectByType<HalvardHunt>();
            float t = 0f;
            while (t < 2f) { t += Time.deltaTime; yield return null; }
            Assert.IsFalse(hunt!.Played, "met once");
            Assert.IsFalse(svc.IsRunning);
        }
    }
}
