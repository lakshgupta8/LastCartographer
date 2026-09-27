#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The Edge end to end in the shipped scenes (NAR-03): arrive, survey, bind and seal, the smudges,
    /// Isolde walks in, Wren follows, the shore. Conversations are driven through the real presenter.
    /// </summary>
    public class PrologueTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            ScreenFade.Clear();
            Bootstrap.SkipPrologueOverride = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            Time.timeScale = 1f;
            ScreenFade.Clear();
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_"))
                    yield return SceneManager.UnloadSceneAsync(s);
            }
            GameState.NewGame();
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        /// <summary>Advance every line and pick the given option until the conversation ends.</summary>
        static IEnumerator Converse(ViewDialoguePresenter presenter, DialogueService svc, int choice, float seconds = 40f)
        {
            float t = 0f;
            while (svc.IsRunning && t < seconds)
            {
                if (presenter.IsShowingOptions) presenter.Choose(choice);
                else if (presenter.IsShowingLine) presenter.Advance();
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsFalse(svc.IsRunning, "the conversation ends");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator TheEdgePlaysThroughToTheShore()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == "Greybox_Greyfold_Edge", 10f, "the Edge to load as the first room");
            var w = GameState.World;
            var wren = Object.FindFirstObjectByType<WrenController>();
            var svc = DialogueService.Instance;
            var presenter = Object.FindFirstObjectByType<ViewDialoguePresenter>();
            Assert.IsNotNull(wren); Assert.IsNotNull(svc); Assert.IsNotNull(presenter);
            var arrive = Cutscene.Find("edge_arrive");
            var departure = Cutscene.Find("edge_departure");
            var director = Object.FindFirstObjectByType<PrologueDirector>();
            Assert.IsNotNull(arrive, "the arrival cutscene"); Assert.IsNotNull(departure, "the departure cutscene"); Assert.IsNotNull(director);

            // Arrive: the paper thins and Isolde speaks; the timeline holds for her.
            yield return Until(() => svc!.IsRunning, 8f, "the arrival conversation");
            Assert.IsTrue(arrive!.IsPlaying && arrive.IsPaused, "the timeline waits for the talk");
            yield return Converse(presenter!, svc!, 0);
            yield return Until(() => !arrive.IsPlaying, 6f, "the arrival cutscene to finish");
            Assert.IsTrue(w.Is(PrologueDirector.Started));
            Assert.AreEqual(0f, ScreenFade.Level, 0.02f, "the paper is clear");
            Assert.IsFalse(wren!.Frozen);
            Assert.IsFalse(director!.SmudgesReleased);

            // Survey the Half-Cathedral: bind Isolde's memory, seal the page.
            Assert.IsTrue(w.MarkSurveyed("Greyfold_Edge/HalfCathedral"));
            yield return Until(() => svc!.IsRunning, 5f, "the survey conversation");
            yield return Converse(presenter!, svc!, 1);
            CollectionAssert.Contains(w.BoundMemories, "isolde.first_sight");
            Assert.IsTrue(w.HasWaxSeal, "the seal is set where Wren stands");
            Assert.AreEqual("Greybox_Greyfold_Edge", w.WaxSealRoom);
            Assert.IsTrue(w.Is(PrologueDirector.Sealed));

            // Dusk: three smudges come out; killing them brings Isolde's last lesson and her leaving.
            yield return Until(() => director.SmudgesReleased, 3f, "the smudges");
            var smudges = Object.FindObjectsByType<Smudge>(FindObjectsSortMode.None);
            Assert.AreEqual(3, smudges.Length, "three smudges at dusk");
            foreach (var s in smudges)
            {
                s.ForceDrawn(true);
                yield return new WaitForFixedUpdate();
                Assert.IsTrue(s.TakeHit(new HitInfo { Damage = 99, Direction = Vector2.right }));
            }
            yield return Until(() => svc!.IsRunning, 6f, "the conversation after the smudges");
            Assert.IsTrue(w.Is(PrologueDirector.FirstSmudges));
            var isolde = GameObject.Find("Isolde");
            Assert.IsNotNull(isolde);
            float isoldeX0 = isolde.transform.position.x;
            yield return Converse(presenter!, svc!, 1, 60f);   // includes <<cutscene edge_departure>>
            Assert.IsTrue(w.Is(PrologueDirector.IsoldeEntered));
            Assert.IsFalse(departure!.IsPlaying);
            Assert.Greater(isolde.transform.position.x, isoldeX0 + 10f, "Isolde walked into the white");
            Assert.IsTrue(director.BlankOpen);
            var wall = GameObject.Find("BlankWall");
            Assert.IsTrue(wall == null || !wall.activeInHierarchy, "the wall at the edge is gone");
            Assert.IsFalse(wren.Frozen, "Wren may follow");

            // Follow: the white takes her, and the shore takes her in.
            wren.Teleport(new Vector2(23f, 0.5f));
            yield return Until(() => RoomManager.Instance!.CurrentRoom == "Greybox_Saltmarrow_A", 10f, "the shore");
            Assert.IsTrue(w.Is(PrologueDirector.Crossed));
            yield return Until(() => svc!.IsRunning, 8f, "Sable on the shore");
            Assert.Greater(ScreenFade.Level, 0.15f, "the white is still thinning when Sable speaks");
            yield return Converse(presenter!, svc!, 2);
            yield return Until(() => Cutscene.Current == null, 6f, "the shore cutscene to finish");
            Assert.IsTrue(w.Is("prologue.woke_on_shore"));
            Assert.IsTrue(w.Is("act1.started"));
            Assert.IsTrue(w.Is("saltmarrow.met_sable"));
            Assert.AreEqual(3, w.Get("prologue.torn_pages"));
            Assert.AreEqual(0f, ScreenFade.Level, 0.02f);
            Assert.IsFalse(wren.Frozen, "Act 1 begins with Wren free");
            Assert.AreEqual(-11.5f, wren.Position.x, 1.5f, "she wakes on the shore spawn");
        }
    }
}
