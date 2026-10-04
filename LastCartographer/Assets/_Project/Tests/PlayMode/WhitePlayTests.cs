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
    /// The Greyfold and the Blank in play (ENV-08): where the road stops, the white gives her back without Clarity and
    /// holds her with it while the meter runs and the room is drawn round her lantern; the Road's cobbles are there only
    /// within her radius; and the drift shows as many islands as she left, with a step onto the first of them that leads
    /// back to the built Hollow.
    /// </summary>
    public class WhitePlayTests
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
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_") || s.name.StartsWith(Islands.ScenePrefix)) yield return SceneManager.UnloadSceneAsync(s);
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
        public IEnumerator WhereTheRoadStopsTheWhiteGivesHerBackUntilSheHasClarity()
        {
            yield return Boot("Greybox_Greyfold_Road_3", "West");
            var wren = Object.FindFirstObjectByType<WrenController>();
            var meter = wren.GetComponent<ClarityMeter>();
            Assert.IsNotNull(meter);
            Assert.AreEqual(0, meter.Level, "not learned yet");
            Assert.IsTrue(UntetheredZone.All.Count >= 1, "the white past the road's end");
            Assert.IsTrue(meter.IsLanternLit, "the room is drawn round her lantern");
            meter.SettleLantern();
            Assert.AreEqual(1f, Shader.GetGlobalFloat(Lantern.StrengthGlobal), 0.001f, "and the paper pass knows it");

            // Without Clarity: a step onto the white and it gives her back at once, to the ground she stood on.
            wren.Teleport(new Vector2(-3f, 0f));
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            wren.Teleport(new Vector2(5f, 0.5f));
            yield return Until(() => meter.Empties >= 1, 3f, "the white to give her back");
            Assert.Less(wren.Position.x, 0f, "drawn back to held ground");

            // With it: she stands on the white while the meter runs, and it fills again on the road.
            wren.GetComponent<AbilitySet>().Unlock(Ability.Clarity);
            yield return Until(() => meter.Level == 1 && meter.IsFull, 3f, "the meter to fill");
            int before = meter.Empties;
            wren.Teleport(new Vector2(5f, 0.5f));
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(before, meter.Empties, "the white holds her now");
            Assert.Greater(wren.Position.x, 3f, "still past the road's end");
            Assert.IsTrue(meter.IsUntethered && meter.Seconds < meter.Capacity - 0.3f, "and the meter runs");
            wren.Teleport(new Vector2(-4f, 0f));
            yield return Until(() => !meter.IsUntethered && meter.IsFull, 5f, "held ground to fill it");
        }

        [UnityTest]
        public IEnumerator TheRoadsCobblesAreThereOnlyWithinHerLantern()
        {
            yield return Boot("Greybox_Greyfold_Road_1", "West");
            var wren = Object.FindFirstObjectByType<WrenController>();
            var cobbles = Room.Current!.GetComponentsInChildren<LanternPlatform>().OrderBy(c => c.transform.position.x).ToArray();
            Assert.AreEqual(4, cobbles.Length, "four strides");
            Assert.IsNotNull(Room.Current.GetComponentInChildren<Gauntlet>(), "the Road That Stops is the region's gauntlet, in its room");
            wren.Teleport(new Vector2(-13f, 0.5f));
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(cobbles.Any(c => c.IsDrawn), "without Clarity no cobble is drawn");
            wren.GetComponent<AbilitySet>().Unlock(Ability.Clarity);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(cobbles[0].IsDrawn, "the first stride is within her radius");
            Assert.IsFalse(cobbles[3].IsDrawn, "the last is not");
            Assert.Greater(cobbles[0].Reach, 3.5f, "the reach is her meter's radius, not the greybox's");
        }

        [UnityTest]
        public IEnumerator TheDriftShowsWhatSheLeftAndTheCrossingStepsOntoIt()
        {
            var w = GameState.World;
            w.Set(Clarity.Act3Flag, true);
            w.Set(Clarity.BellsFlag, true);
            yield return Boot("Greybox_Blank_Hollow_1", "East");   // next door first: Clarity before the drift, which is untethered wall to wall
            var wren = Object.FindFirstObjectByType<WrenController>();
            wren.GetComponent<AbilitySet>().Unlock(Ability.Clarity);
            var meter = wren.GetComponent<ClarityMeter>();
            yield return Until(() => meter.Level == Clarity.MaxLevel && meter.IsFull, 3f, "Act 3's Clarity");

            RoomManager.Instance.Transition("Greybox_Blank_Hollow_3", "West");
            yield return Until(() => RoomManager.Instance.CurrentRoom == "Greybox_Blank_Hollow_3" && !RoomManager.Instance.IsTransitioning, 15f, "the drift");
            yield return null;
            var field = Room.Current!.GetComponentInChildren<DriftField>();
            var crossing = Room.Current.GetComponentInChildren<DriftCrossing>();
            Assert.IsNotNull(field); Assert.IsNotNull(crossing);
            Assert.AreEqual(0, field.Showing, "she has left nothing to the white: nothing drifts");
            Assert.IsTrue(field.Slabs.All(s => !s.gameObject.activeSelf));
            Assert.IsTrue(meter.IsUntethered, "the drift is untethered wall to wall");

            Places.Release(w, "Verdance_Aldermere_2");
            field.Refresh();
            Assert.AreEqual(1, field.Showing, "Aldermere drifts past");
            Assert.IsTrue(field.Slabs[0].gameObject.activeSelf && !field.Slabs[1].gameObject.activeSelf);
            float x0 = field.Slabs[0].localPosition.x;
            for (int i = 0; i < 20; i++) yield return null;
            Assert.Less(field.Slabs[0].localPosition.x, x0, "going west");

            // The step off the far edge: onto Aldermere, whose west exit leads back here.
            wren.Teleport(new Vector2(crossing.transform.position.x, crossing.transform.position.y - 0.4f));
            yield return Until(() => RoomManager.Instance.CurrentRoom == "Island_Aldermere" && !RoomManager.Instance.IsTransitioning, 15f, "Aldermere");
            Assert.AreEqual("Island_Aldermere", crossing != null ? crossing.Last : Room.Current!.RoomId);
            var back = Room.Current!.GetComponentsInChildren<RoomTransition>(true).First(t => t.name == "Transition_To_West");
            Assert.AreEqual("Greybox_Blank_Hollow_3", back.TargetScene, "and the island leads back to the built drift");
        }
    }
}
