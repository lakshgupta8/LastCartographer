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
    /// The Halden arc's writing through the shipped Yarn project (NAR-09): Isolde's cache, the strike three ways, Pell's
    /// report sent or kept by the voices Wren used in Halden, the audience, the office and the Vault, Oriel only if sent.
    /// </summary>
    public class HaldenArcTests
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
        public IEnumerator TheCacheNamesFiveAndOpensTheRoadToTheEdge()
        {
            yield return Boot();
            var w = GameState.World;
            Assert.IsFalse(WorldGraph.Reachable(Ability.Wingbeat | Ability.Talonhold, w.Is).Contains("Greyfold.RoadThatStops"));
            yield return Talk("Orchard_Isolde_Cache", 1);
            StringAssert.Contains("long way", _first);
            Assert.IsTrue(w.Is("isolde.cache"));
            Assert.AreEqual(1, Voices.Count(w, Voice.Warden, "halden"), "\"Then I'll find them\" is the Warden's voice");
            Assert.IsTrue(WorldGraph.Reachable(Ability.Wingbeat | Ability.Talonhold, w.Is).Contains("Greyfold.RoadThatStops"), "Act 1's end: the road to the Edge");
            yield return Talk("Orchard_Isolde_Cache");
            StringAssert.Contains("Five who could hold", _first, "after, the pages fall open at the names");
            yield return Talk("Orchard_Keeper", 0);
            Assert.IsTrue(w.Is("halden.orchard.keeper_met"));
            yield return Talk("Orchard_Gravestone");
            StringAssert.Contains("Halloway", _first);
        }

        [UnityTest]
        public IEnumerator TheStrikeGoesThreeWays()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Lowmarket_Strike", 0);
            Assert.AreEqual(1, w.Get("halden.strike.decided"));
            Assert.AreEqual(PlaceFate.Anchored, Places.FateOf(w, "Halden_Lowmarket_2"), "the owners pay: anchored");
            yield return Talk("Lowmarket_Strike");
            StringAssert.Contains("Same bread", _first);

            GameState.NewGame(); w = GameState.World;
            yield return Talk("Lowmarket_Strike", 2);
            Assert.AreEqual(3, w.Get("halden.strike.decided"));
            Assert.AreEqual(PlaceFate.Released, Places.FateOf(w, "Halden_Lowmarket_2"), "the strike breaks: Lowmarket goes");

            GameState.NewGame(); w = GameState.World;
            yield return Talk("Lowmarket_Strike", 1);
            Assert.AreNotEqual(2, w.Get("halden.strike.decided"), "without the walk, the second way is offered dimmed");
            GameState.NewGame(); w = GameState.World;
            w.Set(BoundsWalks.LearnedFlag, true);
            yield return Talk("Lowmarket_Strike", 1);
            Assert.AreEqual(2, w.Get("halden.strike.decided"));
            Assert.IsTrue(w.Is("halden.strike.walk_taught"));
            Assert.AreEqual(PlaceFate.Unwritten, Places.FateOf(w, "Halden_Lowmarket_2"), "held by walking, later, not by a verb");
        }

        [UnityTest]
        public IEnumerator PellSendsTheReportUnlessSheReadItAsAWarden()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Hall_Pell_Minder", 0);
            Assert.IsTrue(w.Is("halden.hall.pell_minder"));
            Assert.AreEqual(5, Commissions.PostAvailable(w, "Halden"), "the bridge and the keeper are open from arrival; the strike, the exam and the office post with the minder");

            // Reads it, but has not spoken as a Warden in Halden: Pell sends it.
            yield return Talk("Hall_Pell_Minder", 0);
            Assert.IsTrue(w.Is("pell.report_read"));
            Assert.IsTrue(w.Is("pell.report_sent"));
            Assert.IsTrue(Licence.WardensHostile(w), "the report is the Guild's stance now");
            yield return Talk("Bastion_Oriel");
            StringAssert.Contains("Pell writes well", _first, "Oriel has read it");
            Assert.IsTrue(w.Is("halden.oriel.met"));
            yield return Talk("Hall_Pell_Minder");
            StringAssert.Contains("Three things", _first);

            // Two Warden answers in Halden, then reads it: Pell keeps it.
            GameState.NewGame(); w = GameState.World;
            yield return Talk("Hall_Pell_Minder", 1);         // "Then don't send it." (warden)
            yield return Talk("Bridges_Halvard_Hunt", 1);     // "Stand aside. I'm going to the Hall." (warden)
            Assert.AreEqual(2, Voices.Count(w, Voice.Warden, "halden"));
            yield return Talk("Hall_Pell_Minder", 0);         // "Let me read it."
            Assert.IsTrue(w.Is("pell.report_kept"));
            Assert.IsFalse(w.Is("pell.report_sent"));
            Assert.IsFalse(Licence.WardensHostile(w));
            yield return Talk("Bastion_Oriel");
            StringAssert.Contains("not receiving", _first, "no report, no Oriel");

            // Not read at all: sent, whatever the voices.
            GameState.NewGame(); w = GameState.World;
            Voices.Record(w, Voice.Warden, "halden"); Voices.Record(w, Voice.Warden, "halden");
            w.Set("halden.hall.pell_minder", true);
            yield return Talk("Hall_Pell_Minder", 1);         // "Then send it."
            Assert.IsTrue(w.Is("pell.report_sent"));
        }

        [UnityTest]
        public IEnumerator TheAudienceTheOfficeAndTheVault()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Bastion_Maren_Audience", 2);
            Assert.AreNotEqual(3, w.Get("halden.maren.decided"), "she cannot be told about the cygnet before the cache");
            GameState.NewGame(); w = GameState.World;
            w.Set("halden.orchard.cache_read", true);
            yield return Talk("Bastion_Maren_Audience", 2);
            Assert.AreEqual(3, w.Get("halden.maren.decided"));

            Assert.IsFalse(RoomPlans.ReachableRooms(Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread, w.Is).Contains("Halden_Vault_1"));
            yield return Talk("Office_Pell_Drawing", 0);
            StringAssert.Contains("office", _first.ToLowerInvariant());
            Assert.IsTrue(w.Is("halden.vault_opened"), "Pell takes the key");
            Assert.IsTrue(RoomPlans.ReachableRooms(Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread, w.Is).Contains("Halden_Vault_1"), "and the Vault is on the way now");
            yield return Talk("Vault_Pell_Slot", 0);
            Assert.IsTrue(w.Is("halden.vault.pell_counted"));
            yield return Talk("Hall_Tam", 0);
            Assert.IsTrue(w.Is("halden.exam.notes_read"));
            yield return Talk("Bridges_Family", 0);
            Assert.IsTrue(w.Is("halden.bridge.family_met"));
        }
    }
}
