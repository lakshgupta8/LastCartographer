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
    /// Replays a list of route steps through the shipped Yarn project (DES-12, PRO-05): the persistent scene booted
    /// past the prologue, each scene run to its end taking exactly the route's choices (a dimmed option fails it),
    /// each fight recorded as its sheet says, and before every step the step's zone proven reachable on the macro
    /// map with only what the route has earned. EndingRoutesTests and PlaythroughMatrixTests share it.
    /// </summary>
    public sealed class RouteReplay
    {
        ViewDialoguePresenter _presenter = null!;
        DialogueView _view = null!;
        DialogueService _svc = null!;

        /// <summary>Every line shown since the replay booted, as "Speaker: text".</summary>
        public readonly System.Collections.Generic.List<string> Heard = new System.Collections.Generic.List<string>();

        public static void Prepare()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;
            EndingsRunner.AutoWalk = false;
            CommissionCatalog.Reset(); CommissionCatalog.EnsureDefaults();
        }

        public static IEnumerator Unload()
        {
            Bootstrap.SkipPrologueOverride = null;
            EndingsRunner.AutoWalk = true;
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

        public static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        public IEnumerator Boot()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            _svc = DialogueService.Instance!;
            _presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            _view = UiRoot.Instance.GetComponent<DialogueView>();
        }

        /// <summary>Run a node to its end taking exactly these options; a dimmed option fails the route.</summary>
        public IEnumerator Talk(string where, string node, int[] choices)
        {
            Assert.IsTrue(_svc.StartNode(node), where + ": " + node + " exists and starts");
            yield return Until(() => _view.IsVisible, 5f, node + "'s page");
            int k = 0;
            float t = 0f;
            while (_svc.IsRunning && t < 60f)
            {
                if (_presenter.IsShowingOptions)
                {
                    Assert.Less(k, choices.Length, where + ": " + node + " offers a choice the route does not make");
                    int c = choices[k++];
                    Assert.Less(c, _view.OptionCount, where + ": " + node + " has no option " + c);
                    Assert.IsTrue(_view.IsOptionAvailable(c), where + ": " + node + "'s option " + c + " is dimmed here");
                    _presenter.Choose(c);
                    yield return null;
                }
                else if (_presenter.IsShowingLine)
                {
                    string heard = _view.SpeakerText + ": " + _view.LineText;
                    if (Heard.Count == 0 || Heard[Heard.Count - 1] != heard) Heard.Add(heard);
                    _presenter.Advance();
                }
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(_svc.IsRunning, node + " ends");
            Assert.AreEqual(choices.Length, k, where + ": " + node + " made every choice the route lists");
        }

        /// <summary>
        /// The steps in order, from a booted game. With <paramref name="allowSoft"/> the map's soft gaps count as
        /// crossed (a sequence break); <paramref name="breakStep"/> names a step that must not be reachable without
        /// them, so a break is a break. Ends in <paramref name="ending"/> with its epilogue walked.
        /// </summary>
        public IEnumerator Steps(string name, RouteStep[] steps, Ending ending, bool allowSoft = false, int breakStep = -1)
        {
            var w = GameState.World;
            var kit = Ability.None;
            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                string where = name + " step " + (i + 1) + " (" + step.Key + ")";
                Assert.IsNotNull(WorldGraph.Find(step.Zone), where + ": " + step.Zone + " is on the map");
                Assert.IsTrue(WorldGraph.Reachable(kit, w.Is, allowSoft).Contains(step.Zone),
                    where + ": " + step.Zone + " is reachable with " + kit + " and what has been written so far" + (allowSoft ? ", soft gaps allowed" : ""));
                if (i == breakStep)
                    Assert.IsFalse(WorldGraph.Reachable(kit, w.Is, false).Contains(step.Zone), where + ": " + step.Zone + " is only reached by the soft gap, so this is a break");
                if (step.Kind == RouteStepKind.Boss)
                {
                    var sheet = Bosses.Find(step.Key);
                    Assert.IsNotNull(sheet, where + " is a boss with a sheet");
                    Assert.AreEqual(sheet.Zone, step.Zone, where + " is fought where its sheet says");
                    Assert.AreEqual(sheet.Grants, step.Grants, where + " grants what its sheet says");
                    w.Set(Bosses.FlagKey(step.Key), true);
                    if (sheet.Grants != Ability.None) w.Set(AbilitySet.FlagKey(sheet.Grants), true);
                }
                else
                {
                    yield return Talk(where, step.Key, step.Choices);
                    if (step.Grants != Ability.None)
                        Assert.IsTrue(w.Is(AbilitySet.FlagKey(step.Grants)), where + " grants " + step.Grants);
                }
                kit |= step.Grants;
            }
            Assert.AreEqual(ending, Endings.Chosen(w), name + " ends in its ending");
            Assert.IsTrue(w.Is("epilogue.done"), name + " walks its epilogue");
        }
    }
}
