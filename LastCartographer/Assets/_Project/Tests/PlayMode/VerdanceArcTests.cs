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
    /// The Verdance arc's writing through the shipped Yarn project (NAR-08): Teodor meets her and teaches the thread,
    /// the vigil, Aldermere's last day attended or stopped, the keystone check, Ansel's page, the mill, and the inn at
    /// the gate. The rooms are planned only; the scenes run from the persistent scene's dialogue runtime.
    /// </summary>
    public class VerdanceArcTests
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

        /// <summary>Run a node to its end, taking the given options in order (the last one repeats); keeps the first line.</summary>
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
                    _presenter.Choose(Mathf.Min(c, Mathf.Max(0, _view.OptionCount - 1)));
                    yield return null;   // let the choice land before looking again
                }
                else if (_presenter.IsShowingLine) _presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(_svc.IsRunning, node + " ends");
        }

        [UnityTest]
        public IEnumerator TeodorTeachesTheThreadAndSitsTheVigil()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("RootChapel_Teodor_Thread", 0);
            StringAssert.Contains("cloister", _first, "not before she has met him");
            Assert.IsFalse(w.Is("verdance.teodor.thread"));

            yield return Talk("QuietHouse_Teodor", 0);
            StringAssert.Contains("Isolde", _first);
            Assert.IsTrue(w.Is("verdance.teodor.met"));
            Assert.AreEqual(2, Commissions.PostAvailable(w, "Verdance"), "Aldermere and the mill post once she has met him");

            yield return Talk("RootChapel_Teodor_Thread", 0);
            var abilities = Object.FindFirstObjectByType<WrenController>().GetComponent<AbilitySet>();
            Assert.IsTrue(abilities.Has(Ability.Inkthread), "<<grant Inkthread>>");
            Assert.IsTrue(w.Is(AbilitySet.FlagKey(Ability.Inkthread)));
            Assert.AreEqual(3, Commissions.PostAvailable(w, "Verdance"), "the library, the gate and the vigil post once she has the thread");

            yield return Talk("Grove_Teodor_Vigil");
            Assert.IsTrue(w.Is("verdance.grove.vigil"), "a vigil with no choices still ends");
            yield return Talk("QuietHouse_Teodor", 2);
            StringAssert.Contains("Wenlow", _first, "he speaks of the vigil after");
        }

        [UnityTest]
        public IEnumerator AldermereAttendedEarnsTheKeystoneOnlyWithTheHonestAnswer()
        {
            yield return Boot();
            var w = GameState.World;
            w.Set("verdance.teodor.met", true);
            Commissions.PostAvailable(w, "Verdance");
            Assert.IsTrue(Commissions.Take(w, "verdance.ash_remembers"));

            yield return Talk("Aldermere_Teodor", 0);
            StringAssert.Contains("bread", _first);
            Assert.IsTrue(w.Is("verdance.aldermere.attended"));
            Assert.AreEqual(1, w.Get("verdance.aldermere.decided"));
            Assert.AreEqual(PlaceFate.Released, Places.FateOf(w, "Verdance_Aldermere_2"), "the square is let go");
            Assert.IsTrue(Commissions.Is(w, "verdance.ash_remembers", CommissionState.Fulfilled));

            yield return Talk("QuietHouse_Teodor", 1);   // "Because they asked. And you made sure."
            StringAssert.Contains("square", _first, "he raises the keystone once Aldermere is decided");
            Assert.IsTrue(w.Is("teodor.keystone_given"));
            Assert.IsTrue(w.Is("keystone.quiet_house"));

            // A wrong answer, on a fresh road: kindly refused.
            GameState.NewGame();
            w = GameState.World;
            w.Set("verdance.teodor.met", true);
            yield return Talk("Aldermere_Teodor", 2);   // "Who will remember you?" then attends
            Assert.IsTrue(w.Is("verdance.aldermere.attended"));
            yield return Talk("QuietHouse_Teodor", 0);  // "Because you were brave enough to."
            Assert.IsTrue(w.Is("teodor.refused"));
            Assert.IsFalse(w.Is("keystone.quiet_house"));
        }

        [UnityTest]
        public IEnumerator StoppingAldermereCostsTheKeystone()
        {
            yield return Boot();
            var w = GameState.World;
            w.Set("verdance.teodor.met", true);
            yield return Talk("Aldermere_Teodor", 1, 0);
            Assert.IsTrue(w.Is("verdance.aldermere.stopped"));
            Assert.AreEqual(2, w.Get("verdance.aldermere.decided"));
            Assert.AreEqual(PlaceFate.Unwritten, Places.FateOf(w, "Verdance_Aldermere_2"), "stopped, the square is not let go");
            yield return Talk("QuietHouse_Teodor", 1);
            StringAssert.Contains("stopped", _first);
            Assert.IsTrue(w.Is("teodor.refused"), "the Choir's defeat is not an argument");
            Assert.IsFalse(w.Is("keystone.quiet_house"));
            yield return Talk("Aldermere_Teodor");
            StringAssert.Contains("still here", _first, "Aldermere, held against its wish, says so");
        }

        [UnityTest]
        public IEnumerator AnselsPageTheMillAndTheInn()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Library_Teodor_Ansel");
            Assert.IsTrue(w.Is("verdance.library.teodor_asked"));
            yield return Talk("Library_Ansel", 1, 0);   // "Can I turn it for you?" then "Turn the page."
            StringAssert.Contains("two hundred and fourteen", _first.ToLowerInvariant());
            Assert.IsTrue(w.Is("verdance.library.page_turned"));
            Assert.AreEqual(1, w.Get("verdance.library.decided"));
            yield return Talk("Library_Ansel");
            StringAssert.Contains("daughter", _first, "page two hundred and fifteen");

            yield return Talk("Road_Solvent", 2);
            Assert.AreEqual(3, w.Get("verdance.solvent.decided"), "asked his mother, with him there");
            yield return Talk("Road_Solvent");
            StringAssert.Contains("talking", _first);

            yield return Talk("Gate_Inn");
            StringAssert.Contains("Nothing is drawn", _first, "no road until the gate is surveyed");
            Assert.IsFalse(w.Is("verdance.gate.inn_visited"));
            w.MarkSurveyed("Verdance_Gate_2/Gate");
            yield return Talk("Gate_Inn", 1);
            Assert.IsTrue(w.Is("verdance.gate.inn_visited"));
            yield return Talk("Gate_Inscription");
            StringAssert.Contains("winged", _first);
        }
    }
}
