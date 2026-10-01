#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The Steppe in play (ENV-07): the grass leans with the wind and parts round Wren as she walks through it, and the
    /// Wind Gate's ink-swirls rise on the screen and lift her only once she has Windmemory.
    /// </summary>
    public class SteppePlayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            Pause.End();
            GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            Time.timeScale = 1f;
            GameState.NewGame();
            yield return null;
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        IEnumerator Boot(string room, string spawn)
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            RoomManager.Instance.Transition(room, spawn);
            yield return Until(() => RoomManager.Instance.CurrentRoom == room && !RoomManager.Instance.IsTransitioning, 15f, room);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheGrassLeansWithTheWindAndPartsRoundHer()
        {
            // The high grass at the third fire: nothing lives here to hit her (a hit's stop would freeze the wind with everything else).
            yield return Boot("Greybox_Windreach_Fire_1", "West");
            var wren = Object.FindFirstObjectByType<WrenController>();
            Assert.IsNotNull(wren);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            var fields = Room.Current!.GetComponentsInChildren<GrassField>();
            Assert.GreaterOrEqual(fields.Length, 3, "grass before and behind her");
            var field = fields.OrderByDescending(f => f.Count).First();
            Assert.GreaterOrEqual(field.Count, 30, "rows of tufts");

            // Away from her, every tuft leans east (a negative turn) and keeps moving: the wind.
            wren.Teleport(new Vector2(-16f, 0f));   // clear of the west door's trigger
            for (int i = 0; i < 40; i++) yield return null;
            int far = field.Nearest(10f);
            float lean0 = field.LeanOf(far);
            Assert.Less(lean0, 0f, "the tips go east (timeScale " + Time.timeScale + ", dt " + Time.deltaTime + ", enabled " + field.enabled + ")");
            for (int i = 0; i < 30; i++) yield return null;
            Assert.AreNotEqual(lean0, field.LeanOf(far), "and the gust moves on");
            Assert.Less(Mathf.Abs(field.LeanOf(far)), 20f, "a lean, not a fall");

            // She walks into a tuft from the west: it bends away from her, east and hard; past her, it springs back.
            int tuft = field.Nearest(0f);
            float tx = field.Tufts[tuft].position.x;
            wren.Teleport(new Vector2(tx - 0.4f, 0f));
            for (int i = 0; i < 40; i++) yield return null;
            Assert.Less(field.LeanOf(tuft), -18f, "bent away from her, east");
            wren.Teleport(new Vector2(tx + 0.4f, 0f));
            for (int i = 0; i < 40; i++) yield return null;
            Assert.Greater(field.LeanOf(tuft), 8f, "she is east of it now: it bends west, against the wind");
            wren.Teleport(new Vector2(tx + 6f, 0f));
            for (int i = 0; i < 60; i++) yield return null;
            Assert.Less(field.LeanOf(tuft), 0f, "and it leans with the wind again once she has passed");
        }

        [UnityTest]
        public IEnumerator TheInkSwirlsRiseAndLiftHerOnlyWithWindmemory()
        {
            yield return Boot("Greybox_Windreach_Gate_2", "West");
            var wren = Object.FindFirstObjectByType<WrenController>();
            var updrafts = Room.Current!.GetComponentsInChildren<Updraft>();
            Assert.AreEqual(3, updrafts.Length, "three ink-swirls");
            var swirl = updrafts[1].GetComponent<InkSwirl>();
            Assert.IsNotNull(swirl, "drawn");
            Assert.GreaterOrEqual(swirl.Ribbons.Count, 5, "stacked up the column");
            var ys = swirl.Ribbons.Select(r => r.localPosition.y).ToArray();
            for (int i = 0; i < 20; i++) yield return null;
            Assert.IsTrue(swirl.Ribbons.Select(r => r.localPosition.y).Zip(ys, (a, b) => a - b).Any(d => Mathf.Abs(d) > 0.05f), "the ink rises");
            foreach (var r in swirl.Ribbons) Assert.Less(Mathf.Abs(r.localPosition.y), swirl.height * 0.5f + swirl.segment * 0.5f + 0.01f, "and stays in its column");

            // Without Windmemory the column does nothing for her; with it, she rides it.
            var column = updrafts[1].transform.position;
            var abilities = wren.GetComponent<AbilitySet>();
            wren.Teleport(new Vector2(column.x, 0.5f));
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, updrafts[1].Lifts, "no wings yet: she falls through the grass");
            abilities.Unlock(Ability.Windmemory);
            var gauntlet = Room.Current.GetComponentInChildren<Gauntlet>();
            Assert.IsNotNull(gauntlet, "the Gate's course is the region's gauntlet");
            Assert.AreEqual("updrafts", gauntlet.Id);
            wren.Teleport(new Vector2(column.x, 0.5f));
            float top = 0.5f;
            for (int i = 0; i < 90; i++) { yield return new WaitForFixedUpdate(); top = Mathf.Max(top, wren.Position.y); }
            Assert.Greater(updrafts[1].Lifts, 0, "the swirl takes her");
            Assert.Greater(top, 3f, "up past the far ledge's height");
        }
    }
}
