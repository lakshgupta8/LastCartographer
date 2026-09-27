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
    /// The Windreach arc's writing through the shipped Yarn project (NAR-10): three fires and the leap in order, Idrenne's
    /// Fire and the survey three ways, the keystone and its one cost, Hale stopped, helped, or let finish.
    /// </summary>
    public class WindreachArcTests
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
        string _first = "";

        IEnumerator Boot()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            _svc = DialogueService.Instance!;
            _presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            _view = UiRoot.Instance.GetComponent<DialogueView>();
        }

        /// <summary>Run a node to its end, taking the given options in order (the last repeats); keeps the first line.</summary>
        IEnumerator Talk(string node, params int[] choices)
        {
            Assert.IsTrue(_svc.StartNode(node), node + " exists and starts");
            yield return Until(() => _view.IsVisible, 5f, node + "'s page");
            _first = _view.LineText;
            int k = 0;
            float t = 0f;
            while (_svc.IsRunning && t < 60f)
            {
                if (_presenter.IsShowingOptions)
                {
                    int c = choices.Length == 0 ? 0 : choices[Mathf.Min(k++, choices.Length - 1)];
                    c = Mathf.Min(c, Mathf.Max(0, _view.OptionCount - 1));
                    if (!_view.IsOptionAvailable(c))   // a dimmed option cannot be taken: take the first open one, as a player would
                        for (int i = 0; i < _view.OptionCount; i++) if (_view.IsOptionAvailable(i)) { c = i; break; }
                    _presenter.Choose(c);
                    yield return null;
                }
                else if (_presenter.IsShowingLine) _presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(_svc.IsRunning, node + " ends");
        }

        [UnityTest]
        public IEnumerator ThreeFiresAndTheLeapInOrder()
        {
            yield return Boot();
            var w = GameState.World;
            Commissions.PostAvailable(w, "Windreach");
            Assert.IsTrue(Commissions.Take(w, "windreach.moving_camp"));

            yield return Talk("Gate_Idrenne_Leap");
            StringAssert.Contains("third day", _first, "not before the walk");
            yield return Talk("River_Idrenne_Night");
            StringAssert.Contains("Cold ashes", _first, "the riverbed is empty before the first night");

            yield return Talk("Camp_Idrenne", 1);
            StringAssert.Contains("Plateau", _first);
            Assert.AreEqual(1, w.Get("windreach.camp.night"));
            Assert.AreEqual(1, Voices.Count(w, Voice.Warden, "windreach"));
            Assert.AreEqual(1, Commissions.PostAvailable(w, "Windreach"), "the stones post once she has met Idrenne");

            yield return Talk("River_Idrenne_Night", 2);
            StringAssert.Contains("Second fire", _first);
            Assert.IsTrue(w.Is("windreach.stones.named"));
            Assert.AreEqual(1, Commissions.PostAvailable(w, "Windreach"), "and the surveyor once she knows what he is sighting");

            yield return Talk("Grass_Idrenne_Night");
            StringAssert.Contains("trampled", _first, "the third camp is past the Gate");
            yield return Talk("Gate_Idrenne_Leap", 0);
            Assert.IsTrue(w.Is("windreach.leap.done"));
            Assert.IsTrue(w.Is(AbilitySet.FlagKey(Ability.Windmemory)), "the air remembers her");

            yield return Talk("Grass_Idrenne_Night", 1);
            StringAssert.Contains("Third fire", _first);
            Assert.IsTrue(w.Is("windreach.camp.walked"));
            Commissions.Evaluate(w);
            Assert.IsTrue(Commissions.Is(w, "windreach.moving_camp", CommissionState.Fulfilled), "three nights walked");
            yield return Talk("Camp_Idrenne");
            StringAssert.Contains("known here", _first);
        }

        [UnityTest]
        public IEnumerator TheFireSaysItPlainlyAndTheSurveyGoesThreeWays()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Fire_Idrenne");
            StringAssert.Contains("three fires", _first, "only to someone who has walked them");
            Assert.IsFalse(Steppe.FireWitnessed(w));

            w.Set("windreach.camp.walked", true);
            yield return Talk("Fire_Idrenne", 0, 0);
            Assert.IsTrue(Steppe.FireWitnessed(w));
            Assert.AreEqual(Steppe.Drawn, w.Get(Steppe.SurveyFlag));
            Assert.IsTrue(Steppe.OnTheGuildsMap(w), "drawn, it is paper the Guild can use");
            Assert.IsTrue(w.Is(Steppe.KeystoneFlag), "the cooking-stone, given laughing");
            Assert.IsTrue(w.Is("windreach.star.woken"));
            yield return Talk("Fire_Idrenne");
            StringAssert.Contains("only a fire", _first);

            yield return Talk("Star_Idrenne", 0);
            StringAssert.Contains("The cost", _first);
            w.Set(Bosses.FlagKey("fallen_star"), true);
            yield return Talk("Star_Idrenne");
            StringAssert.Contains("No cost. Well. One.", _first);
            Assert.IsTrue(w.Is("windreach.star.cold"));

            GameState.NewGame(); w = GameState.World;
            w.Set("windreach.camp.walked", true);
            yield return Talk("Fire_Idrenne", 0, 2);
            Assert.AreNotEqual(Steppe.Walked, w.Get(Steppe.SurveyFlag), "walking it is offered dimmed until she knows a walk");

            GameState.NewGame(); w = GameState.World;
            w.Set("windreach.camp.walked", true);
            w.Set(BoundsWalks.LearnedFlag, true);
            yield return Talk("Fire_Idrenne", 2, 2);
            Assert.IsTrue(Steppe.HeldByWalking(w));
            Assert.IsFalse(Steppe.OnTheGuildsMap(w));
            Assert.AreEqual(2, Voices.Count(w, Voice.Drift, "windreach"));

            GameState.NewGame(); w = GameState.World;
            w.Set("windreach.camp.walked", true);
            yield return Talk("Fire_Idrenne", 1, 1);
            Assert.AreEqual(Steppe.Undrawn, w.Get(Steppe.SurveyFlag));
            Assert.IsFalse(Steppe.OnTheGuildsMap(w) || Steppe.HeldByWalking(w));
        }

        [UnityTest]
        public IEnumerator HaleIsStoppedHelpedOrLetFinish()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Stones_Hale", 0);
            StringAssert.Contains("Professional courtesy", _first);
            Assert.IsTrue(w.Is("windreach.hale.challenged"));
            Assert.IsFalse(Steppe.OnTheGuildsMap(w));
            w.Set(Bosses.FlagKey("hale"), true);
            yield return Talk("Stones_Hale", 1);
            StringAssert.Contains("Eleven months", _first, "beaten, he cannot tear them");
            Assert.AreEqual(2, w.Get("windreach.hale.pages"), "kept");
            Assert.IsTrue(w.Is("windreach.hale.lens"));
            Assert.IsFalse(Steppe.OnTheGuildsMap(w), "kept pages are hers, not the Guild's");

            GameState.NewGame(); w = GameState.World;
            yield return Talk("Stones_Hale", 1, 1);   // "Why won't the ninth sit?" then "Stand there. I'll hold the staff."
            Assert.IsTrue(w.Is(Steppe.HaleFinishedFlag), "somebody standing is all the ninth wanted");
            Assert.IsTrue(Steppe.OnTheGuildsMap(w));
            w.Set("windreach.camp.walked", true);
            yield return Talk("Fire_Idrenne", 0);
            Assert.IsTrue(Steppe.FireWitnessed(w));
            Assert.AreEqual(0, w.Get(Steppe.SurveyFlag), "there is nothing left to decide: it is on Guild paper");
            Assert.IsTrue(w.Is(Steppe.KeystoneFlag), "and she gives the stone anyway");

            GameState.NewGame(); w = GameState.World;
            yield return Talk("Stones_Hale", 2);
            Assert.IsTrue(Steppe.OnTheGuildsMap(w));
            yield return Talk("Stones_Hale");
            StringAssert.Contains("Nine of nine", _first);
        }
    }
}
