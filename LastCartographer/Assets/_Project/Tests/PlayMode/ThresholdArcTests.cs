#nullable enable
using System.Collections;
using System.Collections.Generic;
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
    /// The Greyfold through Act 2 in the shipped Yarn project (NAR-11): the act break, the notice that calls the climax,
    /// Marrow's echo, Halvard's third, Voss's one speech in its variants, and Pell at the line only if the report was kept.
    /// </summary>
    public class ThresholdArcTests
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
        public IEnumerator TheActBreakAtTheRoadsEnd()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Edge_Pell_Watch");
            StringAssert.Contains("a step from the white", _first, "not before the cache");
            Assert.IsFalse(w.Is("act2.started"));

            w.Set("isolde.cache", true);
            yield return Talk("Edge_Pell_Watch", 1);
            Assert.IsTrue(Said("Five things"), "five at the Edge");
            Assert.IsTrue(Said("Wren. How?"));
            Assert.IsTrue(w.Is(AbilitySet.FlagKey(Ability.Clarity)), "she stays herself");
            Assert.IsTrue(w.Is("pell.saw_her_cross") && w.Is("act2.started"), "the act break");
            Assert.IsTrue(WorldGraph.Reachable(Ability.Wingbeat | Ability.Talonhold, w.Is).Contains("Windreach.NineStones"), "the south road opens");
            yield return Talk("Edge_Pell_Watch");
            StringAssert.Contains("haven't sent it", _first);
        }

        [UnityTest]
        public IEnumerator TheNoticeCallsTheClimaxAndMarrowEchoesHer()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("EdgeCamp_Notice");
            StringAssert.Contains("unposted", _first, "Act 2's climax waits for Interlude A");
            Assert.IsFalse(w.Is("act2.threshold"));
            w.Set("pell.report_decided", true);
            w.Set("pell.report_kept", true);
            yield return Talk("EdgeCamp_Notice", 0);
            Assert.IsTrue(Said("at dawn") && Said("Don't wait. P."), "Voss's notice, and Pell's hand under it");
            Assert.IsTrue(w.Is("act2.threshold"));
            var kit = Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread | Ability.Clarity;
            Assert.IsTrue(RoomPlans.ReachableRooms(kit, f => f == "isolde.cache" || w.Is(f)).Contains("Greyfold_Threshold_2"), "the way to the line is open");

            yield return Talk("MirrorPool_Marrow", 1);
            Assert.IsTrue(Said("Not on the bank"), "Marrow says back her own words");
            Assert.IsTrue(w.Is("marrow.seen_in_pool"));
            yield return Talk("MirrorPool_Marrow");
            StringAssert.Contains("Only Wren", _first);
        }

        [UnityTest]
        public IEnumerator HalvardStopsCounting()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Threshold_Halvard", 0);
            Assert.IsTrue(Said("The bridges missed you"), "he did not catch her on the bridges");
            w.Set("act2.halvard_second", true);
            yield return Talk("Threshold_Halvard", 2);
            Assert.IsTrue(Said("I counted every one"));
            w.Set(Bosses.FlagKey("halvard_3"), true);
            yield return Talk("Threshold_Halvard");
            Assert.IsTrue(Said("as a survey") && Said("Lances down"));
            Assert.IsTrue(w.Is("act2.halvard_third"));
        }

        [UnityTest]
        public IEnumerator VossReadsWhatSheHasDone()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Threshold_Voss", 2);
            StringAssert.Contains("Journeyman Halloway", _first);
            Assert.IsTrue(Said("no stones"), "nothing carried");
            Assert.IsFalse(Said("minder") || Said("Queen-Regent") || Said("Hale"), "and nothing done to read");
            Assert.IsTrue(Said("I cannot say why"), "the speech's core is the same every time");
            Assert.IsTrue(w.Is("threshold.voss.spoken"));
            yield return Talk("Threshold_Voss");
            StringAssert.Contains("past speeches", _first);

            GameState.NewGame(); w = GameState.World;
            w.Set("pell.report_sent", true);
            w.Set("halden.maren.decided", 3);
            w.Set("keystone.hollowvein", true); w.Set("keystone.quiet_house", true); w.Set("keystone.windreach", true);
            w.Set("windreach.hale.finished", true);
            yield return Talk("Threshold_Voss", 1);
            Assert.IsTrue(Said("writes well"), "the report");
            Assert.IsTrue(Said("about the boy"), "the audience");
            Assert.IsTrue(Said("half of Aurenne"), "three stones");
            Assert.IsTrue(Said("Hale's survey"), "the Steppe on his desk");

            GameState.NewGame(); w = GameState.World;
            w.Set("pell.report_kept", true);
            w.Set("halden.maren.decided", 1);
            w.Set("keystone.windreach", true);
            w.Set(Bosses.FlagKey("hale"), true);
            yield return Talk("Threshold_Voss", 0);
            Assert.IsTrue(Said("filed nothing") && Said("make Halden permanent") && Said("in the Vault") && Said("without his lens"));
            Assert.IsFalse(Said("Corra"), "he does not say her name");

            w.Set(Bosses.FlagKey("voss"), true);
            yield return Talk("Threshold_Voss");
            StringAssert.Contains("Tell her", _first);
            Assert.IsTrue(w.Is("greyfold.crossed"));
            var kit = Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread | Ability.Windmemory | Ability.Clarity;
            Assert.IsTrue(RoomPlans.ReachableRooms(kit, f => f == "isolde.cache" || f == "act2.threshold" || w.Is(f)).Contains("Greyfold_LastCamp_1"), "across the line: Isolde's camp");
        }

        [UnityTest]
        public IEnumerator PellIsAtTheLineOnlyIfTheReportWasKept()
        {
            yield return Boot();
            var w = GameState.World;
            w.Set("pell.report_sent", true);
            w.Set(Bosses.FlagKey("voss"), true);
            yield return Talk("Threshold_Pell_Cross");
            StringAssert.Contains("Nobody from the Hall", _first);
            Assert.IsFalse(w.Is("pell.at_threshold"));

            GameState.NewGame(); w = GameState.World;
            w.Set("pell.report_kept", true);
            yield return Talk("Threshold_Pell_Cross");
            StringAssert.Contains("not across yet", _first, "after Voss, not before");
            w.Set(Bosses.FlagKey("voss"), true);
            yield return Talk("Threshold_Pell_Cross", 1);
            StringAssert.Contains("Three things", _first, "the list is three long now");
            Assert.IsTrue(w.Is("pell.at_threshold"));
            Assert.AreEqual(1, Voices.Count(w, Voice.Warden, "greyfold"));
        }
    }
}
