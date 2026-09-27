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
    /// The endings through the shipped Yarn project (NAR-13): the frame offers only what is earned; each of the four plays
    /// and walks its epilogue; Marrow's verdict; Wren's last line by voice; Voss a note or a statue.
    /// </summary>
    public class EndingsArcTests
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

        IEnumerator Walk(Ending e)
        {
            var all = new List<string>();
            foreach (var node in Endings.EpilogueWalk(e))
            {
                yield return Talk(node);
                all.AddRange(_lines);
            }
            _lines.Clear(); _lines.AddRange(all);
        }

        static void Stones(WorldState w, params string[] homes) { foreach (var h in homes) w.Set(Keystones.FlagKey(h), true); }

        [UnityTest]
        public IEnumerator TheFrameOffersOnlyWhatIsEarned()
        {
            yield return Boot();
            var w = GameState.World;
            w.Set("act3.started", true);
            yield return Talk("Observatory_Frame");
            Assert.IsTrue(Said("Not yet"), "nothing earned, nothing offered");
            Assert.AreEqual(Ending.None, Endings.Chosen(w));

            Stones(w, "hollowvein", "quiet_house");
            w.Set("teodor.keystone_given", true); w.Set("verdance.aldermere.attended", true);
            yield return Talk("Observatory_Frame", 2);
            Assert.AreEqual(Ending.Unwritten, Endings.Chosen(w));
            Assert.IsTrue(Said("Hollowvein is.") && Said("The Quiet House is.") && Said("Now nothing is held"), "Teodor names what she carried");
            Assert.IsFalse(Said("The Steppe is."), "and only that");
            Assert.IsTrue(w.Is("ending.unwritten"));
            yield return Talk("Observatory_Frame");
            StringAssert.Contains("as she left it", _first);

            Voices.Record(w, Voice.Drift, "blank");
            yield return Walk(Ending.Unwritten);
            Assert.IsTrue(Said("It holds") && Said("Look."), "Teodor walks Aldermere; Marrow's third word, repeated");
            Assert.AreEqual(4, w.Get("marrow.words"));
            Assert.IsTrue(Said("left blank"), "the last line, in the Drift's voice");
            Assert.IsTrue(w.Is("epilogue.done"));
        }

        [UnityTest]
        public IEnumerator TheFixedWorld()
        {
            yield return Boot();
            var w = GameState.World;
            w.Set("act3.started", true);
            Stones(w, "aury", "hollowvein", "quiet_house", "windreach", "isolde", "archivist");
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinCooperates);
            w.Set("marrow.words", 3);
            yield return Talk("Observatory_Frame", 0);
            Assert.AreEqual(Ending.Fixed, Endings.Chosen(w));
            Assert.IsTrue(Said("The Atlas is whole") && Said("Nobody flies"), "bible 9.1");

            yield return Walk(Ending.Fixed);
            Assert.IsTrue(Said("Prices are the same") && Said("next spring"));
            Assert.IsTrue(Said("Same."), "Marrow echoes, bright and beautiful");
            Assert.AreEqual(3, w.Get("marrow.words"), "and has not said a new word");
            Assert.IsTrue(Said("drawn true"), "a journeyman starts as a surveyor");
        }

        [UnityTest]
        public IEnumerator TheOpenWorld()
        {
            yield return Boot();
            var w = GameState.World;
            w.Set("act3.started", true);
            Stones(w, "hollowvein", "quiet_house", "windreach", "isolde");
            w.Set("runa.named_wren", true); w.Set("teodor.keystone_given", true); w.Set("windreach.camp.walked", true);
            w.Set("windreach.fire.witnessed", true); w.Set("emberdown.hollowvein.walked", true);
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinPersuaded);
            w.Set("emberdown.kettil.met", true); w.Set("blank.ilse.heard", true);
            yield return Talk("Observatory_Frame", 1);
            Assert.AreEqual(Ending.Open, Endings.Chosen(w));
            Assert.IsTrue(Said("Kettil, who counts") && Said("Ilse, of the Hollow") && Said("Forty-two, Wren"), "Runa sings who she has met");
            Assert.IsFalse(Said("Corra, who draws"), "and only them");
            Assert.IsTrue(w.Is("ending.chorus_led"));

            yield return Talk("Ending_Open_After");
            StringAssert.Contains("pulls at her", _first, "the Complete Survey first (6.15)");
            w.Set(Bosses.FlagKey("complete_survey"), true);
            yield return Talk("Ending_Open_After");
            Assert.IsTrue(Said("Wren flies. Once. Briefly."));
            Assert.IsTrue(w.Is(AbilitySet.FlagKey(Ability.Sky)) && w.Is("ending.open"));

            Voices.Record(w, Voice.Warden, "blank");
            yield return Walk(Ending.Open);
            Assert.IsTrue(Said("Forty-three") && Said("Skywalk"));
            Assert.AreEqual(4, w.Get("marrow.words"), "a word nobody has said before");
            Assert.IsTrue(Said("carried out"));
        }

        [UnityTest]
        public IEnumerator TheRestAtCorvinsChair()
        {
            yield return Boot();
            var w = GameState.World;
            w.Set("act3.started", true);
            w.Set(Bosses.FlagKey("archivist"), true);
            w.Set("capital.corvin.after", true);
            w.Set(Endings.RestOfferedFlag, true);
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinUnpersuaded);
            yield return Talk("Capital_Corvin", 2);
            Assert.AreEqual(Ending.Rest, Endings.Chosen(w));
            Assert.IsTrue(Said("Isolde can go") && Said("draws beside her"));

            yield return Walk(Ending.Rest);
            Assert.IsTrue(Said("Let the pen find the line") && Said("keeps looking at the door"), "Isolde teaching, Wren's atlas on the desk");
            Assert.IsTrue(Said("No chick"), "Marrow is not there");

            GameState.NewGame(); w = GameState.World;
            w.Set(Bosses.FlagKey("archivist"), true);
            w.Set("capital.corvin.after", true);
            w.Set(Endings.RestOfferedFlag, true);
            w.Set(Endings.CorvinStanceFlag, Endings.CorvinUnpersuaded);
            w.Set("keystone.windreach", true);
            yield return Talk("Capital_Corvin", 2);
            Assert.AreEqual(Ending.None, Endings.Chosen(w), "one stone and the chair is dimmed");
        }

        [UnityTest]
        public IEnumerator VossIsANoteOrAStatue()
        {
            yield return Boot();
            var w = GameState.World;
            yield return Talk("Observatory_Voss");
            StringAssert.Contains("empty", _first, "before an ending, only his chair");
            w.Set(Endings.ChosenFlag, (int)Ending.Unwritten);
            w.Set(Endings.VossChangedFlag, true);
            yield return Talk("Observatory_Voss");
            Assert.IsTrue(Said("Gone in to find her"));
            GameState.NewGame(); w = GameState.World;
            w.Set(Endings.ChosenFlag, (int)Ending.Fixed);
            yield return Talk("Observatory_Voss");
            Assert.IsTrue(Said("A statue"));
            Assert.IsTrue(w.Is("ending.voss_coda"));
        }
    }
}
