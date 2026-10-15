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
            yield return Follow(where, node, choices);
        }

        /// <summary>
        /// A scene played for one thing (a commission's step): the choices that lead to it, and at any other question the
        /// last option open, the one that walks away. It need not ask every question the choices name.
        /// </summary>
        public IEnumerator Converse(string where, string node, int[] choices)
        {
            Assert.IsTrue(_svc.StartNode(node), where + ": " + node + " exists and starts");
            yield return Follow(where, node, choices, lenient: true);
        }

        /// <summary>A scene something else started (an arena whose boss speaks first): read it and make its choices.</summary>
        public IEnumerator Follow(string where, string node, int[] choices, bool lenient = false)
        {
            yield return Until(() => _view.IsVisible, 5f, node + "'s page");
            int k = 0;
            float t = 0f;
            while (_svc.IsRunning && t < 60f)
            {
                if (_presenter.IsShowingOptions && lenient && k >= choices.Length)
                {
                    int last = _view.OptionCount - 1;
                    while (last > 0 && !_view.IsOptionAvailable(last)) last--;
                    _presenter.Choose(last);
                    yield return null;
                }
                else if (_presenter.IsShowingOptions)
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
            if (!lenient) Assert.AreEqual(choices.Length, k, where + ": " + node + " made every choice the route lists");
            else Assert.AreEqual(choices.Length, System.Math.Min(k, choices.Length), where + ": " + node + " made every choice listed");
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
                if (step.Kind == RouteStepKind.Walk)
                {
                    // Her feet: each room of the relay walked in turn, as the rooms take it up when she comes in.
                    var relay = BoundsWalks.FindRelay(step.Key);
                    Assert.IsNotNull(relay, where + " is a walk through rooms");
                    while (BoundsWalks.LegsWalked(w, relay!) < step.Legs)
                    {
                        int leg = BoundsWalks.LegsWalked(w, relay!);
                        Assert.IsTrue(BoundsWalks.IsLegDue(w, relay!, leg), where + ": leg " + (leg + 1) + " (" + relay!.Legs[leg] + ") is due");
                        BoundsWalks.WalkLeg(w, relay!, leg);
                    }
                }
                else if (step.Kind == RouteStepKind.Boss)
                {
                    var sheet = Bosses.Find(step.Key);
                    Assert.IsNotNull(sheet, where + " is a boss with a sheet");
                    Assert.AreEqual(sheet.Zone, step.Zone, where + " is fought where its sheet says");
                    Assert.AreEqual(sheet.Grants, step.Grants, where + " grants what its sheet says");
                    // The story-timing pass: the arena is awake by now, not still waiting on a flag the route has not written.
                    var arenas = StagePresence.ArenasFor(step.Key);
                    if (arenas.Count > 0)
                        Assert.IsTrue(arenas.Any(a => string.IsNullOrEmpty(a.flag) || w.Is(a.flag)),
                            where + ": the arena is awake by now (" + string.Join(", ", arenas.Select(a => a.scene + " waits on " + a.flag)) + ")");
                    w.Set(Bosses.FlagKey(step.Key), true);
                    if (sheet.Grants != Ability.None) w.Set(AbilitySet.FlagKey(sheet.Grants), true);
                }
                else
                {
                    // The story-timing pass: someone who starts this scene is standing by now (a scene only code starts has no talker).
                    var talkers = StagePresence.TalkersFor(step.Key);
                    if (talkers.Count > 0)
                        Assert.IsTrue(talkers.Any(t => t.Stands(w)),
                            where + ": nobody who starts it is standing by now: " + string.Join(", ", talkers.Select(t => t.ToString())));
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
