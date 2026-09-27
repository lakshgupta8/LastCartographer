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
    /// <summary>Rooms stream through Addressables (PRG-07): loads by address, neighbour bundles resident, old room gone.</summary>
    public class RoomStreamingTests
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

        [UnityTest]
        public IEnumerator RoomsLoadByAddressAndNeighboursStayResident()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && RoomManager.Instance.CurrentRoom == "Greybox_Saltmarrow_A", 10f, "room A");
            var rm = RoomManager.Instance;
            Assert.IsTrue(rm.UsesAddressables);
            Assert.IsTrue(rm.IsAddressable("Greybox_Saltmarrow_A"), "room A came in through Addressables");
            yield return Until(() => !rm.IsPreloading, 10f, "neighbour preload");
            CollectionAssert.AreEquivalent(new[] { "Greybox_Saltmarrow_Shore", "Greybox_Saltmarrow_Stilts" }, rm.PreloadedNeighbours.ToArray(), "room A's neighbours are the shore and the stilts");

            rm.Transition("Greybox_Saltmarrow_B", "West");
            yield return Until(() => rm.CurrentRoom == "Greybox_Saltmarrow_B" && !rm.IsTransitioning, 10f, "room B");
            Assert.IsTrue(rm.IsAddressable("Greybox_Saltmarrow_B"));
            Assert.IsFalse(rm.IsAddressable("Greybox_Saltmarrow_A"), "the old room's handle is gone");
            Assert.IsFalse(SceneManager.GetSceneByName("Greybox_Saltmarrow_A").isLoaded, "the old room is unloaded");
            Assert.IsTrue(SceneManager.GetSceneByName("Greybox_Saltmarrow_B").isLoaded);
            var wren = Object.FindFirstObjectByType<WrenController>();
            Assert.Less(wren.Position.x, -10f, "Wren stands at B's west spawn");
            yield return Until(() => !rm.IsPreloading, 10f, "neighbour preload from B");
            CollectionAssert.AreEquivalent(new[] { "Greybox_Saltmarrow_Boardwalk", "Greybox_Saltmarrow_Tetherline" }, rm.PreloadedNeighbours.ToArray(), "B's neighbours are resident, B itself is not");
        }
    }
}
