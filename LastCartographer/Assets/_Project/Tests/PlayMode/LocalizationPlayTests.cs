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
    /// Dialogue in the player's language (PRG-19), through the shipped Yarn project: the pseudo-locale's table is on the
    /// project, the runner speaks it when the player picks it, the speaker's name still comes through, options are
    /// localised too, a locale with no dialogue table falls back to English lines, and the captions follow.
    /// </summary>
    public class LocalizationPlayTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;
            Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            DialogueService.Instance?.Stop();
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
            Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey);
            GameState.NewGame();
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        DialogueService _svc = null!;
        DialogueView _view = null!;
        ViewDialoguePresenter _presenter = null!;

        IEnumerator Boot()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            _svc = DialogueService.Instance!;
            _presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            _view = UiRoot.Instance.GetComponent<DialogueView>();
        }

        IEnumerator FirstLine(string node)
        {
            _svc.Stop();
            yield return null;
            Assert.IsTrue(_svc.StartNode(node), node);
            yield return Until(() => _view.IsVisible && !string.IsNullOrEmpty(_view.LineText), 5f, node + "'s first line");
        }

        [UnityTest]
        public IEnumerator DialogueSpeaksThePlayersLanguage()
        {
            yield return Boot();
            Assert.AreEqual(Loc.Base, _svc.DialogueLocale);

            yield return FirstLine("Camp_Ashes_Ahead");
            Assert.AreEqual("Ashes", _view.SpeakerText);
            StringAssert.StartsWith("Old ashes", _view.LineText, "English to start");

            Loc.SetLocale(Loc.Pseudo);
            Assert.AreEqual(Loc.Pseudo, _svc.DialogueLocale, "the project carries the pseudo-locale's table");
            yield return FirstLine("Camp_Ashes_Ahead");
            Assert.AreEqual("Ashes", _view.SpeakerText, "the speaker still comes through");
            Assert.IsTrue(Loc.LooksPseudo(_view.LineText), "the line is the pseudo-locale's: " + _view.LineText);
            StringAssert.DoesNotContain("Old ashes", _view.LineText);

            // Options are lines too.
            GameState.World.Set(Camp.NightKey, 1);
            yield return FirstLine(CampSite.BedrollNode);
            _presenter.Advance();
            yield return Until(() => _presenter.IsShowingOptions, 5f, "the bedroll's options");
            yield return null;
            Assert.IsTrue(Loc.LooksPseudo(_view.OptionText(0)), "an option in the pseudo-locale: " + _view.OptionText(0));
            _presenter.Choose(1);   // "Not yet": let the node end rather than stop it mid-choice
            yield return Until(() => { if (_presenter.IsShowingLine) _presenter.Advance(); return !_svc.IsRunning; }, 5f, "the bedroll to end");

            // A locale with a UI table but no dialogue yet: its lines fall back to English.
            Loc.Register("fr", new System.Collections.Generic.Dictionary<string, string> { { "hud.death", "l'encre s'épuise" } });
            Loc.SetLocale("fr");
            Assert.AreEqual(Loc.Base, _svc.DialogueLocale);
            yield return FirstLine("Camp_Ashes_Ahead");
            StringAssert.StartsWith("Old ashes", _view.LineText, "English lines, rather than none");
            _svc.Stop();
        }

        [UnityTest]
        public IEnumerator CaptionsFollowTheLocale()
        {
            yield return Boot();
            string? caption = null;
            System.Action<string, float> seen = (text, _) => caption = text;
            Captions.Shown += seen;
            try
            {
                Loc.SetLocale(Loc.Pseudo);
                var w = GameState.World;
                w.Set(Camp.NightKey, 1);
                yield return null;
                var wren = Object.FindFirstObjectByType<WrenController>();
                var clarity = wren.GetComponent<ClarityMeter>();
                wren.GetComponent<AbilitySet>().Unlock(Ability.Clarity);
                yield return new WaitForFixedUpdate();
                w.Set(Clarity.BellsFlag, true);
                yield return Until(() => caption != null, 2f, "the Clarity caption");
                Assert.IsTrue(Loc.LooksPseudo(caption), caption);
                Assert.AreEqual(Loc.T("caption.clarity_grows", "Clarity grows."), caption);
                Assert.AreEqual(2, clarity.Level);
            }
            finally { Captions.Shown -= seen; }
        }
    }
}
