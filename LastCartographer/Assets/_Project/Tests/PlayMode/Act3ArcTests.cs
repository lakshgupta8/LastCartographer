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
    /// Act 3 through the shipped Yarn project (NAR-12): Isolde's atlas starts it; the Hollow, Ilse and Marrow's first word;
    /// Corra carried out or not; Corvin cooperating, persuaded or not; the Return, Voss changed or not; Pell's last list.
    /// </summary>
    public class Act3ArcTests
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
        public IEnumerator IsoldesAtlasStartsActThree()
        {
            yield return Boot();
            var w = GameState.World;
            var kit = Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread | Ability.Windmemory | Ability.Clarity;
            System.Func<string, bool> gates = f => f == "isolde.cache" || f == "act2.threshold" || f == "greyfold.crossed" || w.Is(f);
            Assert.IsFalse(RoomPlans.ReachableRooms(kit, gates).Contains("Blank_Hollow_2"));
            yield return Talk("LastCamp_Isolde", 1);
            Assert.IsTrue(Said("sixth slot") && Said("finding Ilse"), "reveal 5.2");
            Assert.IsTrue(w.Is("act3.started"));
            var act3 = RoomPlans.ReachableRooms(kit, gates);
            Assert.IsTrue(act3.Contains("Blank_Hollow_2") && act3.Contains("Blank_Capital_4") && act3.Contains("Halden_Observatory_2"), "the Blank, and the dome");
            yield return Talk("LastCamp_Isolde");
            StringAssert.Contains("You know why now", _first);
        }

        [UnityTest]
        public IEnumerator TheHollowIlseAndMarrowsFirstWord()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Blank_Marrow_Follow");
            Assert.IsTrue(Said("a grey chick"));
            Assert.IsTrue(w.Is("marrow.following"));
            yield return Talk("Blank_Marrow_Follow");
            StringAssert.Contains("one pace back", _first, "silent until the Hollow");
            Assert.AreEqual(0, w.Get("marrow.words"));

            yield return Talk("Hollow_Ilse", 0);
            StringAssert.Contains("before you saw me", _first);
            Assert.IsTrue(Said("born here") && Said("I let you go"), "reveal 5.3");
            Assert.IsTrue(w.Is("blank.ilse.heard"));
            Assert.IsFalse(_lines.Exists(l => l.Contains("Wren")), "Ilse does not have the name");
            yield return Talk("Blank_Marrow_Follow");
            Assert.IsTrue(Said("Let you go"), "an echo of Ilse");
            Assert.AreEqual(1, w.Get("marrow.words"), "and its first word");

            yield return Talk("Hollow_Isolde", 1);
            StringAssert.Contains("Journeyman", _first, "proud, not scared");
            Assert.IsTrue(w.Is("blank.isolde.found"));
        }

        [UnityTest]
        public IEnumerator CorraIsCarriedOutIfWrenLooks()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Capital_Corra");
            StringAssert.Contains("He's coming", _first, "the drawing first");
            w.Set(Bosses.FlagKey("corras_drawing"), true);
            yield return Talk("Capital_Corra", 1, 0);   // "What's his name?" then "Yes. I'll take it."
            Assert.IsTrue(Said("Aurelian"), "plant 5.5");
            Assert.IsTrue(w.Is("corra.memory_carried") && w.Is("corra.decided"));

            GameState.NewGame(); w = GameState.World;
            w.Set(Bosses.FlagKey("corras_drawing"), true);
            yield return Talk("Capital_Corra", 2);
            Assert.IsTrue(w.Is("corra.decided"));
            Assert.IsFalse(w.Is("corra.memory_carried"));
            yield return Talk("Capital_Corra");
            StringAssert.Contains("Smaller this time", _first);
        }

        [UnityTest]
        public IEnumerator CorvinCooperatesIsPersuadedOrIsNot()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Capital_Corvin", 0);
            StringAssert.Contains("drawn you a chair", _first);
            Assert.IsTrue(Said("my wife and my daughter") && Said("Halloway"), "reveal 5.4");
            Assert.IsTrue(Said("lost the memory"), "reveal 5.6");
            Assert.AreEqual(1, w.Get("corvin.stance"), "the Survey, with him");

            GameState.NewGame(); w = GameState.World;
            yield return Talk("Capital_Corvin", 1, 0, 0, 0);
            Assert.AreEqual(3, w.Get("corvin.stance"), "without Ilse's word or the leap, the right answers are dimmed");
            Assert.IsFalse(w.Is("corvin.argued_ilse") || w.Is("corvin.argued_sky"));

            GameState.NewGame(); w = GameState.World;
            w.Set("blank.ilse.heard", true);
            w.Set("windreach.leap.done", true);
            yield return Talk("Capital_Corvin", 1, 0, 1, 0);
            Assert.AreEqual(3, w.Get("corvin.stance"), "two of three is not enough");
            GameState.NewGame(); w = GameState.World;
            w.Set("blank.ilse.heard", true);
            w.Set("windreach.leap.done", true);
            yield return Talk("Capital_Corvin", 1, 0, 0, 0);
            Assert.AreEqual(2, w.Get("corvin.stance"), "5.3, 5.4 and 5.6: persuaded");
            Assert.IsTrue(Said("Show me"));

            w.Set("marrow.words", 1);
            yield return Talk("Capital_Marrow_Word");
            Assert.IsTrue(Said("Nothing was lost"));
            Assert.AreEqual(2, w.Get("marrow.words"));

            w.Set(Bosses.FlagKey("archivist"), true);
            yield return Talk("Capital_Corvin", 0);
            Assert.IsTrue(w.Is("ending.rest_offered"), "she carries none: he offers the chair");
            Assert.IsFalse(Keystones.Has(w, "archivist"), "and she can leave his stone with him");

            GameState.NewGame(); w = GameState.World;
            w.Set("keystone.windreach", true);
            w.Set(Bosses.FlagKey("archivist"), true);
            yield return Talk("Capital_Corvin");
            Assert.IsFalse(w.Is("ending.rest_offered"));
            Assert.AreEqual(2, Keystones.Count(w), "the seventh, given");
        }

        [UnityTest]
        public IEnumerator TheReturnVossChangedOrNot()
        {
            yield return Boot();
            var w = GameState.World;
            w.Set("act3.started", true);
            w.Set("corra.memory_carried", true);
            w.Set("marrow.words", 2);
            yield return Talk("Return_Voss");
            Assert.IsTrue(Said("the right size") && _lines.Exists(l => l.Contains("Corra.")), "the one word he does not say");
            Assert.IsTrue(w.Is("voss.changed"));
            yield return Talk("Threshold_Marrow");
            Assert.IsTrue(Said("The right size"));
            Assert.AreEqual(3, w.Get("marrow.words"));

            GameState.NewGame(); w = GameState.World;
            w.Set("act3.started", true);
            w.Set("marrow.words", 2);
            yield return Talk("Return_Voss");
            Assert.IsTrue(Said("Somebody must hold the line"));
            Assert.IsFalse(Said("Corra"));
            Assert.IsFalse(w.Is("voss.changed"));
            Assert.IsTrue(w.Is("return.voss.met"));
            yield return Talk("Threshold_Marrow");
            Assert.IsTrue(Said("Hold the line"));
        }

        [UnityTest]
        public IEnumerator PellsLastListIsWrensMostUsedVoice()
        {
            yield return Boot();
            var w = GameState.World;
            w.Set("act3.started", true);
            yield return Talk("Observatory_Pell_Return");
            Assert.AreEqual(1, w.Get("pell.last_list"), "a journeyman starts as a surveyor");
            Assert.IsTrue(Said("The leaves"));

            GameState.NewGame(); w = GameState.World;
            w.Set("act3.started", true);
            Voices.Record(w, Voice.Warden, "halden"); Voices.Record(w, Voice.Warden, "blank");
            yield return Talk("Observatory_Pell_Return");
            Assert.AreEqual(2, w.Get("pell.last_list"));
            Assert.IsTrue(Said("Her."));

            GameState.NewGame(); w = GameState.World;
            w.Set("act3.started", true);
            Voices.Record(w, Voice.Drift, "windreach");
            yield return Talk("Observatory_Pell_Return");
            Assert.AreEqual(3, w.Get("pell.last_list"));
            Assert.IsTrue(Said("The sky"));
        }
    }
}
