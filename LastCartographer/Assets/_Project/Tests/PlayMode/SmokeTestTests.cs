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
    /// The smoke test a built player runs with <c>-smoke</c> (PRG-25), run here in the editor: the game boots to its
    /// first room, walks into the Drowned Quay, and starts and stops a conversation, with no error logged.
    /// </summary>
    public class SmokeTestTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            DialogueService.Instance?.Stop();
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator ABootedGameWalksTwoRoomsAndTalks()
        {
            var go = new GameObject("~SmokeTest");
            var smoke = go.AddComponent<SmokeTest>();
            smoke.RoomTimeout = 20f;
            try
            {
                SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
                Object.DontDestroyOnLoad(go);
                float t = 0f;
                while (smoke.Outcome == SmokeTest.Result.Running && t < 60f) { t += Time.unscaledDeltaTime; yield return null; }
                Assert.AreEqual(SmokeTest.Result.Passed, smoke.Outcome, smoke.Report);
                StringAssert.Contains(SmokeTest.SecondRoom, smoke.Report);
                StringAssert.Contains(SmokeTest.Conversation, smoke.Report);
                Assert.AreEqual(SmokeTest.SecondRoom, RoomManager.Instance.CurrentRoom);
                Assert.IsFalse(DialogueService.Instance!.IsRunning, "the conversation was stopped");
                Assert.AreEqual(0, smoke.Errors);
            }
            finally { Object.Destroy(go); }
        }
    }
}
