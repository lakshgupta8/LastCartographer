#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The coast wears its paper kit in play (ENV-01, ENV-02): every backdrop layer is an inked cut-out, the ground is on
    /// its tile, and the place's fade still thins the ink on all of it, foreground first, ground never.
    /// </summary>
    public class PaperKitPlayTests
    {
        static readonly int InkId = Shader.PropertyToID("_Ink");

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameState.NewGame();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
            GameState.NewGame();
        }

        static float InkOf(Renderer r)
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            return mpb.GetFloat(InkId);
        }

        [UnityTest]
        public IEnumerator TheQuayIsDrawnFromTheKitAndStillFades()
        {
            var handle = Addressables.LoadSceneAsync("Greybox_Saltmarrow_A", LoadSceneMode.Additive);
            yield return handle;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, "the quay loads");
            var group = Object.FindObjectsByType<FadeGroup>(FindObjectsSortMode.None).First(g => g.PlaceId == "Saltmarrow_A");
            var ink = Shader.Find("OWSBG/InkSprite");
            var paper = group.Layers.Where(l => l.Renderer.name.StartsWith("Paper_")).ToList();
            var skins = group.Layers.Where(l => !l.Renderer.name.StartsWith("Paper_") && l.Renderer.sharedMaterial.name == "M_Ground_Boardwalk").ToList();
            Assert.AreEqual(4, paper.Count, "four backdrop layers");
            Assert.GreaterOrEqual(skins.Count, 5, "the walkway, three platforms and the stilt are on the planks");
            foreach (var l in paper.Concat(skins))
            {
                var m = l.Renderer.sharedMaterial;
                Assert.AreEqual(ink, m.shader, l.Renderer.name + " on the ink shader");
                Assert.IsNotNull(m.GetTexture("_BaseMap"), l.Renderer.name + " has its drawing");
            }
            Assert.AreEqual(3, paper.First(l => l.Renderer.name == "Paper_Fore_Reeds").DropoutStage, "the foreground drops first");
            Assert.IsTrue(skins.All(l => l.DropoutStage > FadeStages.Max), "the ground never drops");
            Assert.AreEqual(1f, InkOf(paper[0].Renderer), 0.001f, "drawn at stage 0");

            FadeStages.Advance(GameState.World, "Saltmarrow_A", 2);
            float t = 0f;
            while (group.IsAnimating && t < 10f) { t += Time.deltaTime; yield return null; }
            yield return null;
            Assert.IsFalse(group.IsAnimating, "the fade lands");
            var reeds = paper.First(l => l.Renderer.name == "Paper_Mid_Reeds").Renderer;
            Assert.AreEqual(FadeStages.InkFor(2), InkOf(reeds), 0.01f, "the reeds thin with the place");
            Assert.AreEqual(FadeStages.InkFor(2), InkOf(skins[0].Renderer), 0.01f, "the planks wash with the place");
            Assert.IsTrue(paper.All(l => l.Renderer.enabled) && skins.All(l => l.Renderer.enabled), "nothing drops at stage 2");
        }

        [UnityTest]
        public IEnumerator TheFadedThirdIsPalerFromTheStart() => RoomWearsTheKit("Greybox_Saltmarrow_Chain_3", "Paper_Mid_Reeds_Faded", "M_Ground_Boardwalk_Faded");

        [UnityTest]
        public IEnumerator TheChapelStandsOnSaltStone() => RoomWearsTheKit("Greybox_Saltmarrow_Chapel", "Paper_Far_Chapel", "M_Ground_Stone");

        [UnityTest]
        public IEnumerator TheBoardwalksGapsHoldTheTide() => RoomWearsTheKit("Greybox_Saltmarrow_Boardwalk", "Paper_Mid_Reeds", "M_Ground_Shallows");

        // ENV-02: every layer of the room's fade group is a kit drawing on the ink shader; one named backdrop and
        // one named ground material are among them.
        IEnumerator RoomWearsTheKit(string scene, string backdrop, string ground)
        {
            var handle = Addressables.LoadSceneAsync(scene, LoadSceneMode.Additive);
            yield return handle;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, scene + " loads");
            var group = Object.FindObjectsByType<FadeGroup>(FindObjectsSortMode.None).First(g => scene.EndsWith(g.PlaceId));
            var ink = Shader.Find("OWSBG/InkSprite");
            Assert.GreaterOrEqual(group.Layers.Count, 4, scene + " has its layers");
            foreach (var l in group.Layers)
            {
                var m = l.Renderer.sharedMaterial;
                Assert.AreEqual(ink, m.shader, l.Renderer.name + " on the ink shader");
                Assert.IsNotNull(m.GetTexture("_BaseMap"), l.Renderer.name + " has its drawing");
            }
            Assert.IsTrue(group.Layers.Any(l => l.Renderer.name == backdrop), scene + " shows " + backdrop);
            Assert.IsTrue(group.Layers.Any(l => l.Renderer.sharedMaterial.name == ground), scene + " stands on " + ground);
        }
    }
}
