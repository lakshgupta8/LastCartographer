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
    /// The moving camp at runtime (PRG-21): the camp's rooms stand in until Windreach is built, the camp stands at its
    /// site and ashes stand where it is not, the camp walks on at first light, and walking with it through the bedroll
    /// costs a day and makes camp at dusk at the next fire.
    /// </summary>
    public class MovingCampTests
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
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_") || s.name.StartsWith(Camp.StandInPrefix))
                    yield return SceneManager.UnloadSceneAsync(s);
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

        IEnumerator GoTo(string room, string spawn)
        {
            var scene = Camp.StandInScene(room);
            RoomManager.Instance.Transition(scene, spawn);
            yield return Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 10f, scene);
        }

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
                    yield return null;
                }
                else if (_presenter.IsShowingLine) _presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(_svc.IsRunning, node + " ends");
        }

        static CampSite Site() => Room.Current!.GetComponent<CampSite>();
        static NpcTalker? Talker(string name) => Room.Current!.GetComponentsInChildren<NpcTalker>(false).FirstOrDefault(t => t.gameObject.name == name);
        static string? Exit(string side) => Room.Current!.GetComponentsInChildren<RoomTransition>(true).FirstOrDefault(t => t.gameObject.name == "Transition_" + side)?.TargetScene;

        [UnityTest]
        public IEnumerator TheCampStandsAtItsSiteAndAshesWhereItIsNot()
        {
            yield return Boot();
            var w = GameState.World;
            Assert.IsTrue(RoomManager.Generators.Contains(CampRooms.Build), "the stand-ins registered themselves");

            // The post: the desk that stays, at the head of the camp's road.
            yield return GoTo(Camp.PostRoom, "Start");
            Assert.IsNotNull(Room.Current!.GetComponentInChildren<DraftingDesk>(), "the walkers' post keeps the desk");
            Assert.IsNull(Site(), "the post is not a site");
            Assert.IsNull(Exit("To_West"));
            Assert.AreEqual(Camp.StandInScene("Windreach_Camp_2"), Exit("To_East"));

            yield return GoTo("Windreach_Camp_2", "West");
            Assert.IsTrue(Site().IsCampHere, "the fire ring first");
            Assert.AreEqual("Camp_Idrenne", Talker("Npc_Idrenne")!.StartNode);
            Assert.IsNotNull(Talker("Bedroll"));
            Assert.IsNull(Talker("Ashes"), "no ashes where the fire is lit");
            Assert.AreEqual(Camp.StandInScene("Windreach_River_2"), Exit("To_East"));

            yield return GoTo("Windreach_River_2", "West");
            Assert.IsFalse(Site().IsCampHere);
            Assert.IsNull(Talker("Npc_Idrenne"), "nobody at the riverbed yet");
            Assert.AreEqual(CampSite.AshesAheadNode, Talker("Ashes")!.StartNode);
            yield return Talk(CampSite.AshesAheadNode);
            StringAssert.Contains("some nights", _first);

            // The first fire had, the night slept: at first light it has walked here, while she watched.
            w.Set(Camp.NightKey, 1);
            DayClock.Sleep(w);
            yield return Until(() => Site().IsCampHere, 1f, "the camp to arrive");
            Assert.AreEqual("River_Idrenne_Night", Talker("Npc_Idrenne")!.StartNode, "the second fire's scene");

            yield return GoTo("Windreach_Camp_2", "East");
            Assert.IsFalse(Site().IsCampHere, "the fire ring is ashes now");
            Assert.AreEqual(CampSite.AshesBehindNode, Talker("Ashes")!.StartNode);
            yield return Talk(CampSite.AshesBehindNode);
            StringAssert.Contains("riverbed", _first, "the ashes say which way it went");
        }

        [UnityTest]
        public IEnumerator WalkingWithTheClanCostsADayAndMakesCampAtDusk()
        {
            yield return Boot();
            var w = GameState.World;
            yield return GoTo("Windreach_Camp_2", "Start");

            yield return Talk(CampSite.BedrollNode);
            StringAssert.Contains("Nobody's walking", _first, "not before the first fire");
            Assert.AreEqual(0, Camp.Site(w));

            yield return Talk("Camp_Idrenne", 0);
            Assert.AreEqual(1, Camp.Nights(w), "the first fire");
            yield return Talk("Camp_Idrenne");
            StringAssert.Contains("first light", _first, "she is still here tonight, and says when they go");

            yield return Talk(CampSite.BedrollNode, 1);
            Assert.AreEqual(Camp.StandInScene("Windreach_Camp_2"), RoomManager.Instance.CurrentRoom, "not yet: she stays");
            Assert.AreEqual(0, Camp.Site(w));

            DayClock.SetPhase(w, DayPhase.Night);
            int day = DayClock.Day(w);
            string? caption = null;
            System.Action<string, float> seen = (text, _) => caption = text;
            Captions.Shown += seen;
            try
            {
                yield return Talk(CampSite.BedrollNode, 0);
            }
            finally { Captions.Shown -= seen; }
            var rm = RoomManager.Instance;
            Assert.AreEqual(Camp.StandInScene("Windreach_River_2"), rm.CurrentRoom, "she walked with them to the riverbed");
            Assert.AreEqual(1, Camp.Site(w));
            Assert.AreEqual(day + 1, DayClock.Day(w), "a day on the walk");
            Assert.AreEqual(DayPhase.Dusk, DayClock.Phase(w), "camp made at dusk");
            Assert.IsTrue(Site().IsCampHere, "with the camp");
            var wren = Object.FindFirstObjectByType<WrenController>();
            Assert.Less(Vector2.Distance(wren.Position, Room.Current!.FindSpawn(CampWalk.SpawnName).position), 1.5f, "beside the bedroll");
            Assert.IsFalse(wren.Frozen, "hers again once the page closes");
            Assert.AreEqual(0f, ScreenFade.Level, 0.01f, "the paper lifts");
            StringAssert.Contains("riverbed, at dusk", caption ?? "");

            yield return Talk("River_Idrenne_Night", 0);
            Assert.AreEqual(2, Camp.Nights(w), "the second fire, where the camp is");
            Assert.IsTrue(Camp.IsReadyToWalk(w), "and tomorrow the high grass");
        }
    }
}
