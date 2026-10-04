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
    /// The quay's furniture in play (ENV-09): the desk, the ledger and the dummy are drawn from the kit under the
    /// behaviours they had; the props thin with the place and never drop; the dummy rests in its own colours and
    /// still flashes and recoils when struck; the seeds are still taken.
    /// </summary>
    public class HubPropsPlayTests
    {
        static readonly int InkId = Shader.PropertyToID("_Ink");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

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
            return mpb.HasFloat(InkId) ? mpb.GetFloat(InkId) : 1f;
        }

        [UnityTest]
        public IEnumerator TheQuaysFurnitureIsDrawnAndFadesWithThePlace()
        {
            var handle = Addressables.LoadSceneAsync("Greybox_Saltmarrow_A", LoadSceneMode.Additive);
            yield return handle;
            Assert.AreEqual(AsyncOperationStatus.Succeeded, handle.Status, "the quay loads");
            var ink = Shader.Find("OWSBG/InkSprite");
            var desk = Object.FindFirstObjectByType<DraftingDesk>();
            var ledger = Object.FindFirstObjectByType<CommissionLedger>();
            var dummy = Object.FindFirstObjectByType<TrainingDummy>();
            Assert.IsNotNull(desk); Assert.IsNotNull(ledger); Assert.IsNotNull(dummy);
            foreach (var (who, go) in new[] { ("desk", desk!.gameObject), ("ledger", ledger!.gameObject), ("dummy", dummy!.gameObject) })
            {
                var r = go.GetComponentInChildren<MeshRenderer>();
                Assert.IsNotNull(r, who + " has a drawing");
                Assert.IsTrue(r.name.StartsWith("Prop_"), who + " is a prop, not a block: " + r.name);
                Assert.AreEqual(ink, r.sharedMaterial.shader, who + " on the ink shader");
                Assert.Less(r.transform.position.z, 0.95f, who + " stands just behind the play plane");
                Assert.Greater(r.transform.position.z, 0f, who);
            }
            Assert.IsTrue(dummy.IsDrawn, "the dummy knows it is drawn");

            var group = Object.FindObjectsByType<FadeGroup>(FindObjectsSortMode.None).First(g => g.PlaceId == "Saltmarrow_A");
            var props = group.Layers.Where(l => l.Renderer.name.StartsWith("Prop_")).ToList();
            Assert.GreaterOrEqual(props.Count, 8, "desk, ledger, dummy, stake, seeds, stall, nets, boat are in the fade group");
            Assert.IsTrue(props.All(l => l.DropoutStage > FadeStages.Max), "furniture never drops");

            FadeStages.Advance(GameState.World, "Saltmarrow_A", 2);
            float t = 0f;
            while (group.IsAnimating && t < 10f) { t += Time.deltaTime; yield return null; }
            yield return null;
            var deskR = desk.GetComponentInChildren<MeshRenderer>();
            Assert.AreEqual(FadeStages.InkFor(2), InkOf(deskR), 0.01f, "the desk thins with the place");
            Assert.IsTrue(deskR.enabled, "and is still there");
        }

        [UnityTest]
        public IEnumerator TheDrawnDummyRestsInItsColoursAndFlashesWhenStruck()
        {
            var handle = Addressables.LoadSceneAsync("Greybox_Saltmarrow_A", LoadSceneMode.Additive);
            yield return handle;
            var dummy = Object.FindFirstObjectByType<TrainingDummy>();
            Assert.IsNotNull(dummy);
            yield return null; yield return null;
            var r = dummy!.GetComponentInChildren<MeshRenderer>();
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            Assert.AreEqual(Color.white, mpb.GetColor(BaseColorId), "no red tint over the drawing");

            var rest = dummy.transform.position;
            Assert.IsTrue(dummy.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }));
            yield return null;
            r.GetPropertyBlock(mpb);
            Assert.Greater(mpb.GetColor(BaseColorId).r, 1f, "the flash goes bright, not white-over-white");
            Assert.Greater(dummy.transform.position.x, rest.x, "shoved by the hit");
            Assert.AreEqual(1, dummy.Hits);
        }
    }
}
