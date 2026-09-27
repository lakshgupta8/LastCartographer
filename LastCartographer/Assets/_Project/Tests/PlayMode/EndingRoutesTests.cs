#nullable enable
using System.Collections;
using System.Linq;
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
    /// Every ending is reachable from a new game (DES-12): each route in EndingRoutes replayed through the shipped Yarn
    /// project, with the step's zone proven reachable on the macro map first, using only what the route has earned.
    /// </summary>
    public class EndingRoutesTests
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

        /// <summary>Run a node to its end taking exactly these options; a dimmed option fails the route.</summary>
        IEnumerator Talk(string where, string node, int[] choices)
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
                else if (_presenter.IsShowingLine) _presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(_svc.IsRunning, node + " ends");
            Assert.AreEqual(choices.Length, k, where + ": " + node + " made every choice the route lists");
        }

        IEnumerator Replay(Ending ending)
        {
            yield return Boot();
            var w = GameState.World;
            var route = EndingRoutes.For(ending);
            Assert.IsNotNull(route, ending + " has a route");
            var kit = Ability.None;
            for (int i = 0; i < route.Steps.Length; i++)
            {
                var step = route.Steps[i];
                string where = ending + " step " + (i + 1) + " (" + step.Key + ")";
                Assert.IsNotNull(WorldGraph.Find(step.Zone), where + ": " + step.Zone + " is on the map");
                Assert.IsTrue(WorldGraph.Reachable(kit, w.Is).Contains(step.Zone),
                    where + ": " + step.Zone + " is reachable with " + kit + " and what has been written so far");
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
            Assert.AreEqual(ending, Endings.Chosen(w), "the route ends in its ending");
            Assert.IsTrue(w.Is("epilogue.done"), "and walks its epilogue");
        }

        [UnityTest] public IEnumerator TheFixedWorldIsReachable() { yield return Replay(Ending.Fixed); }
        [UnityTest] public IEnumerator TheOpenWorldIsReachable() { yield return Replay(Ending.Open); }
        [UnityTest] public IEnumerator TheUnwrittenIsReachable() { yield return Replay(Ending.Unwritten); }
        [UnityTest] public IEnumerator TheRestIsReachable() { yield return Replay(Ending.Rest); }

        [UnityTest]
        public IEnumerator TheRoutesEarnWhatTheyClaim()
        {
            yield return Boot();
            // The Fixed World's route carries every stone but the frame's; the Rest's carries none and anchors nothing.
            var fixedNotes = string.Join(" ", EndingRoutes.For(Ending.Fixed).Steps.Select(s => s.Note));
            foreach (var h in Keystones.Homes.Where(h => h != Keystones.InTheFrame))
                StringAssert.Contains("keystone." + h, fixedNotes, "the Fixed World's route picks up the " + h + " stone");
            var rest = EndingRoutes.For(Ending.Rest).Steps;
            Assert.IsFalse(rest.Any(s => s.Note.Contains("keystone.")), "the Rest carries nothing");
            Assert.IsFalse(rest.Any(s => s.Key == "Lowmarket_Strike"), "and anchors nothing");
            // Every ability but the Sky is earned on the long routes, in the map's order.
            foreach (var e in new[] { Ending.Fixed, Ending.Open })
            {
                var grants = EndingRoutes.For(e).Steps.Where(s => s.Grants != Ability.None).Select(s => s.Grants).ToList();
                CollectionAssert.AreEqual(new[] { Ability.Wingbeat, Ability.Talonhold, Ability.Inkthread, Ability.Clarity, Ability.Windmemory }, grants.Take(5).ToList(), e + ": the spine's order");
            }
        }
    }
}
