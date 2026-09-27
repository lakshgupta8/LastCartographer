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
    /// The Emberdown arc's writing through the shipped Yarn project (NAR-07): Kettil counts her in, Runa counts her and
    /// teaches the walk and the wall, the debate opens the way to Hollowvein, and the pit-head decides it. The rooms are
    /// only planned, so the scenes run from the persistent scene's dialogue runtime.
    /// </summary>
    public class EmberdownArcTests
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

        ViewDialoguePresenter _presenter = null!;
        DialogueView _view = null!;
        DialogueService _svc = null!;

        IEnumerator Boot()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            _svc = DialogueService.Instance!;
            _presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            _view = UiRoot.Instance.GetComponent<DialogueView>();
        }

        /// <summary>Run a node to its end, taking option <paramref name="choice"/> (clamped) at every choice; returns the first line.</summary>
        IEnumerator Talk(string node, int choice, System.Action<string>? firstLine = null)
        {
            Assert.IsTrue(_svc.StartNode(node), node + " exists and starts");
            yield return Until(() => _view.IsVisible, 5f, node + "'s page");
            firstLine?.Invoke(_view.LineText);
            float t = 0f;
            while (_svc.IsRunning && t < 40f)
            {
                if (_presenter.IsShowingOptions) _presenter.Choose(Mathf.Min(choice, Mathf.Max(0, _view.OptionCount - 1)));
                else if (_presenter.IsShowingLine) _presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(_svc.IsRunning, node + " ends");
        }

        [UnityTest]
        public IEnumerator KettilAndRunaCountHerInAndTeachTheWalkAndTheWall()
        {
            yield return Boot();
            var w = GameState.World;
            string first = "";
            yield return Talk("Rest_Kettil", 2, l => first = l);
            StringAssert.Contains("stranger", first, "Kettil counts her at the gate");
            Assert.IsTrue(w.Is("emberdown.kettil.met"));
            Assert.AreEqual(2, Commissions.PostAvailable(w, "Emberdown"), "the debate and the rescue post once Kettil has met her");
            Assert.AreEqual(CommissionState.Posted, Commissions.StateOf(w, "emberdown.debate"));
            Assert.AreEqual(CommissionState.Unknown, Commissions.StateOf(w, "emberdown.long_roll_call"), "not before Runa has counted her");

            // Runa will not climb with a stranger.
            yield return Talk("Chimneys_Runa_Climb", 0, l => first = l);
            StringAssert.Contains("strangers", first);
            Assert.IsFalse(w.Is("emberdown.runa.climbed"));

            yield return Talk("Bell_Runa_Count", 0, l => first = l);
            Assert.IsTrue(w.Is("emberdown.runa.counted"));
            Assert.IsTrue(BoundsWalks.IsLearned(w), "\"Teach me\" teaches the walk");
            Commissions.PostAvailable(w, "Emberdown");
            Assert.AreEqual(CommissionState.Posted, Commissions.StateOf(w, "emberdown.long_roll_call"));

            var wren = Object.FindFirstObjectByType<WrenController>();
            var abilities = wren.GetComponent<AbilitySet>();
            yield return Talk("Chimneys_Runa_Climb", 1);
            Assert.IsTrue(w.Is("emberdown.runa.climbed"));
            Assert.IsTrue(abilities.Has(Ability.Talonhold), "<<grant Talonhold>> reaches Wren");
            Assert.IsTrue(w.Is(AbilitySet.FlagKey(Ability.Talonhold)), "and the world remembers it");
            Assert.AreEqual(2, Commissions.PostAvailable(w, "Emberdown"), "the ninth chimney and the overlook post once she can climb");

            // A second visit to the bell: counted, not yet named.
            yield return Talk("Bell_Runa_Count", 0, l => first = l);
            StringAssert.Contains("warmer", first);
            Assert.IsFalse(w.Is("runa.named_wren"));
        }

        [UnityTest]
        public IEnumerator TheDebateOpensHollowveinAndThePitHeadDecidesIt()
        {
            yield return Boot();
            var w = GameState.World;
            w.Set("emberdown.kettil.met", true);
            w.Set("emberdown.runa.counted", true);
            w.Set(BoundsWalks.LearnedFlag, true);
            Commissions.Evaluate(w);
            Commissions.Post(w, "emberdown.debate");
            Assert.IsTrue(Commissions.Take(w, "emberdown.debate"));
            Commissions.Post(w, "emberdown.long_roll_call");
            Assert.IsTrue(Commissions.Take(w, "emberdown.long_roll_call"));

            yield return Talk("Baths_Kettil_Debate", 0);
            Assert.IsTrue(w.Is("emberdown.debate.heard"));
            Assert.AreEqual(3, w.Get("emberdown.debate.sided"), "both right");
            Assert.IsTrue(Commissions.Is(w, "emberdown.debate", CommissionState.Fulfilled));

            string first = "";
            yield return Talk("Rest_Kettil", 0, l => first = l);
            StringAssert.Contains("baths", first, "Kettil takes up Hollowvein once the baths have argued");
            Assert.IsTrue(w.Is("emberdown.hollowvein_opened"), "the map's flag: the boards come off");
            Assert.IsTrue(WorldGraph.Reachable(Ability.Wingbeat | Ability.Talonhold, w.Is).Contains("Emberdown.Hollowvein"));

            // Leave them buried: the decision is made and the commission is done.
            yield return Talk("Hollowvein_Runa_Walk", 1);
            Assert.IsTrue(w.Is("emberdown.hollowvein.buried"));
            Assert.AreEqual(2, w.Get("emberdown.hollowvein.decided"));
            Assert.IsTrue(Commissions.Is(w, "emberdown.long_roll_call", CommissionState.Fulfilled));
            yield return Talk("Hollowvein_Runa_Walk", 0, l => first = l);
            StringAssert.Contains("boards stay", first, "she says so every time after");

            // The walked ending, and Runa naming her at the bell.
            GameState.NewGame();
            w = GameState.World;
            yield return Talk("Hollowvein_Runa_After", 0);
            Assert.IsTrue(w.Is("emberdown.hollowvein.walked"));
            Assert.AreEqual(1, w.Get("emberdown.hollowvein.decided"));
            w.Set("emberdown.runa.counted", true);
            yield return Talk("Bell_Runa_Count", 0, l => first = l);
            StringAssert.Contains("Forty-two, Wren", first);
            Assert.IsTrue(w.Is("runa.named_wren"));
        }

        [UnityTest]
        public IEnumerator BrannTheAgentAndTheRescueSpeak()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Stair_Brann", 2);
            Assert.IsTrue(w.Is("emberdown.brann.met"));
            yield return Talk("Stair_Rescue", 0);
            Assert.IsTrue(w.Is("emberdown.rescue.done"));
            string first = "";
            yield return Talk("Chimneys_Ninth_Agent", 0, l => first = l);
            StringAssert.Contains("relief", first);
            Assert.IsTrue(w.Is("emberdown.ninth.agent_met"));
            yield return Talk("Overlook_Runa", 1);
            Assert.IsTrue(w.Is("emberdown.overlook.seen"));

            w.Set("emberdown.kettil.met", true);
            w.Set("boss.brann.defeated", true);
            yield return Talk("Rest_Kettil", 2, l => first = l);
            StringAssert.Contains("One, gone", first, "Kettil counts Brann out");
        }
    }
}
