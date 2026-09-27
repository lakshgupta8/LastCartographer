#nullable enable
using System.Collections;
using System.Collections.Generic;
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
    /// The Blank's islands through the shipped Yarn project (NAR-14): every island speaks, reads how she left it, and asks
    /// whether she has eaten; the tether, Aury and his stone, and Sable beside him in Act 3.
    /// </summary>
    public class IslandsArcTests
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
        readonly List<string> _lines = new List<string>();

        IEnumerator Boot()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            _svc = DialogueService.Instance!;
            _presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>()!;
            _view = UiRoot.Instance.GetComponent<DialogueView>();
        }

        /// <summary>Run a node to its end, taking the given options in order (the last repeats); keeps every line shown.</summary>
        IEnumerator Talk(string node, params int[] choices)
        {
            Assert.IsTrue(_svc.StartNode(node), node + " exists and starts");
            yield return Until(() => _view.IsVisible, 5f, node + "'s page");
            _first = _view.LineText;
            _lines.Clear();
            string? last = null;
            int k = 0;
            float t = 0f;
            while (_svc.IsRunning && t < 60f)
            {
                if (_presenter.IsShowingOptions)
                {
                    int c = choices.Length == 0 ? 0 : choices[Mathf.Min(k++, choices.Length - 1)];
                    c = Mathf.Min(c, Mathf.Max(0, _view.OptionCount - 1));
                    if (!_view.IsOptionAvailable(c))
                        for (int i = 0; i < _view.OptionCount; i++) if (_view.IsOptionAvailable(i)) { c = i; break; }
                    _presenter.Choose(c);
                    yield return null;
                }
                else if (_presenter.IsShowingLine)
                {
                    if (_view.LineText != last) { last = _view.LineText; _lines.Add(last); }
                    _presenter.Advance();
                }
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(_svc.IsRunning, node + " ends");
        }

        bool Said(string s) => _lines.Exists(l => l.Contains(s));

        [UnityTest]
        public IEnumerator EveryIslandSpeaksAndAsksIfSheHasEaten()
        {
            yield return Boot();
            var w = GameState.World;
            foreach (var node in Islands.Authored.Select(i => i.Node).Concat(new[] { Islands.GenericNode }))
            {
                yield return Talk(node, 0);
                Assert.IsFalse(_lines.Exists(l => l.Contains("I'm dead") || l.Contains("I am dead")), node + ": the Remnant never say it");
                if (node != "Island_Merrow_Dotha") Assert.IsTrue(Said("eaten"), node + " asks whether she has eaten");
            }
            Assert.IsTrue(w.Is("blank.merrow.visited") && w.Is("blank.aldermere.visited") && w.Is("blank.inn.visited")
                          && w.Is("blank.lowmarket.visited") && w.Is("blank.remnant.visited"));
            yield return Talk("Island_Aldermere");
            StringAssert.Contains("Still tonight", _first);
        }

        [UnityTest]
        public IEnumerator TheBuriedAreCalledOnlyByWrenWhoKnowsTheWalk()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Island_Hollowvein", 0);
            Assert.IsFalse(w.Is("blank.hollowvein.called"), "without the walk, the roll-call is offered dimmed");
            Assert.IsTrue(Said("Runa? Kettil's girl?"));

            GameState.NewGame(); w = GameState.World;
            w.Set(BoundsWalks.LearnedFlag, true);
            yield return Talk("Island_Hollowvein", 0);
            Assert.IsTrue(Said("Thirty-one answers"));
            Assert.IsTrue(w.Is("blank.hollowvein.called"));
            yield return Talk("Island_Hollowvein");
            StringAssert.Contains("Tell Runa we heard", _first);

            GameState.NewGame(); w = GameState.World;
            w.Set("saltmarrow.dotha.decided", 2);
            yield return Talk("Island_Merrow_Dotha", 1);
            Assert.IsTrue(Said("You let it go"), "Dotha reads how Wren left Merrow's End");
        }

        [UnityTest]
        public IEnumerator TheTetherAuryAndHisStone()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Chain_Sable_Tether");
            StringAssert.Contains("Not for sale", _first, "Act 2, and only once she has asked about the third lighthouse");

            w.Set("act2.started", true);
            w.Set("saltmarrow.sable.aury", true);
            Assert.IsFalse(RoomPlans.ReachableRooms(Ability.None, w.Is).Contains("Blank_Aury_2"));
            yield return Talk("Chain_Sable_Tether", 1);
            Assert.IsTrue(Said("Fifteen") || Said("fifteen"));
            Assert.IsTrue(w.Is("sable.tether_sold") && w.Is("saltmarrow.tether"));
            Assert.IsTrue(RoomPlans.ReachableRooms(Ability.None, w.Is).Contains("Blank_Aury_2"), "the tether opens Aury's causeway");
            Assert.IsTrue(Endings.Allied(w, "sable"), "she went with it");

            yield return Talk("Aury_Lighthouse", 1, 1);   // "Who carried the chick?", then "Keep it. It's the lamp's."
            Assert.IsTrue(Said("carried past, out of the white"), "plant 5.3: he recognises her");
            Assert.IsFalse(Keystones.Has(w, "aury"), "she can leave it with him");
            yield return Talk("Aury_Lighthouse", 0);
            Assert.IsTrue(Keystones.Has(w, "aury"), "and take it later");

            yield return Talk("Aury_Sable");
            StringAssert.Contains("Not lately", _first, "Sable sits with him in Act 3");
            w.Set("act3.started", true);
            yield return Talk("Aury_Sable", 0);
            Assert.IsTrue(Said("That's why the oil's late") && Said("Boat's leaving"));
            Assert.IsTrue(w.Is("blank.aury.knows") && w.Is("sable.aury_told"));
            yield return Talk("Aury_Island");
            Assert.IsTrue(w.Is("blank.aury.island"));
        }
    }
}
