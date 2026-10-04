#nullable enable
using System.Collections;
using System.Linq;
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
    /// The cast data against the shipped Yarn project (NAR-05): every scene marked staged has its node, and no scene
    /// marked unstaged does, so a script written without flipping the flag (or a node renamed) is caught.
    /// </summary>
    public class CastPlayTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;
            Cast.Reset(); Cast.EnsureDefaults();
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

        [UnityTest]
        public IEnumerator StagedScenesHaveTheirNodesAndUnstagedOnesDoNot()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            float t = 0f;
            while (!(RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == "Greybox_Saltmarrow_A" && !RoomManager.Instance.IsTransitioning
                     && DialogueService.Instance != null && DialogueService.Instance.Runner != null) && t < 10f) { t += Time.deltaTime; yield return null; }
            var svc = DialogueService.Instance;
            Assert.IsNotNull(svc?.Runner, "the dialogue runtime is up");
            var dialogue = svc!.Runner!.Dialogue;

            int staged = 0, planned = 0;
            foreach (var a in Cast.Appearances)
            {
                if (a.Node == null) continue;
                if (a.Staged) { staged++; Assert.IsTrue(dialogue.NodeExists(a.Node), a.Character + "'s " + a.Scene + " is staged: node " + a.Node); }
                else { planned++; Assert.IsFalse(dialogue.NodeExists(a.Node), a.Node + " exists in the project but the cast data says it is not staged yet"); }
            }
            Assert.That(staged, Is.GreaterThanOrEqualTo(4));
            // Every scene the cast data names is written and staged now (the Bone Bridge crossing was the last, 2026-10-04).
            CollectionAssert.IsEmpty(Cast.Appearances.Where(a => a.Node != null && !a.Staged).Select(a => a.Node),
                "every scene with a stage is written");

            // The staged talkers in the loaded hub start on the cast's nodes.
            var talkers = Object.FindObjectsByType<NpcTalker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var castNodes = Cast.Appearances.Where(a => a.Staged).Select(a => a.Node).ToList();
            Assert.IsTrue(talkers.Any(n => castNodes.Contains(n.StartNode)), "a hub talker starts on a cast node");
        }
    }
}
