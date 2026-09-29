#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// "Their rhythm is the region's music tempo" (combat doc 5, AUD-01): a smudge in a region's room flickers a beat
    /// drawn and a beat undrawn at that region's beat; one on no region keeps its own numbers.
    /// </summary>
    public class SmudgeTempoTests
    {
        Scene _scene;
        Scene _was;

        static Smudge Spawn()
        {
            var go = new GameObject("Smudge") { layer = LayerMask.NameToLayer("Enemy") };
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
            go.AddComponent<Rigidbody2D>();
            return go.AddComponent<Smudge>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_was.IsValid()) SceneManager.SetActiveScene(_was);
            if (_scene.IsValid() && _scene.isLoaded) yield return SceneManager.UnloadSceneAsync(_scene);
        }

        [UnityTest]
        public IEnumerator ASmudgeKeepsItsRegionsBeat()
        {
            _was = SceneManager.GetActiveScene();
            var loose = Spawn();
            float own = loose.CycleSeconds;
            Assert.AreEqual(1.6f, own, 1e-4f, "on no region, its own flicker");
            Object.Destroy(loose.gameObject);

            _scene = SceneManager.CreateScene("Greybox_Emberdown_TempoTest");
            SceneManager.SetActiveScene(_scene);
            var smudge = Spawn();
            yield return null;
            float beat = AudioDirection.BeatOf(Region.Emberdown);
            Assert.AreEqual(beat, smudge.DrawnSeconds, 1e-4f, "drawn on Emberdown's beat");
            Assert.AreEqual(2f * beat, smudge.CycleSeconds, 1e-4f, "and undrawn on its off-beat");

            // It flickers on that cycle: drawn now, undrawn a beat later, drawn again a beat after that.
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(smudge.IsDrawn, "drawn on the beat");
            for (float t = 0f; t < beat + 0.05f; t += Time.fixedDeltaTime) yield return new WaitForFixedUpdate();
            Assert.IsFalse(smudge.IsDrawn, "undrawn on the off-beat");
            for (float t = 0f; t < beat; t += Time.fixedDeltaTime) yield return new WaitForFixedUpdate();
            Assert.IsTrue(smudge.IsDrawn, "and back on the next beat");
        }
    }
}
