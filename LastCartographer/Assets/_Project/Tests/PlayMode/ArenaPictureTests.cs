#nullable enable
using System.Collections;
using System.IO;
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
    /// The two Warden arenas in their built rooms (boss-kits.md 6.5, 6.8): Brann's floor wears the drawn grates and each
    /// section plays its state as the fight runs; Oriel's yard stands its drill. Each fight's picture is written to
    /// logs/arenas/ to be looked at.
    /// </summary>
    public class ArenaPictureTests
    {
        [SetUp] public void SetUp() { Time.timeScale = 1f; GameState.NewGame(); Bootstrap.SkipPrologueOverride = true; }

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
            GameState.NewGame();
        }

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        static IEnumerator Into(string scene, float x)
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            RoomManager.Instance.Transition(scene, "Start");
            yield return Until(() => RoomManager.Instance.CurrentRoom == scene && !RoomManager.Instance.IsTransitioning, 10f, scene);
            Object.FindFirstObjectByType<WrenController>().Teleport(new Vector2(x, 0.6f));
        }

        static IEnumerator Picture(string name)
        {
            for (int i = 0; i < 20; i++) yield return null;
            var cam = Camera.main;
            if (cam == null) yield break;
            var rt = new RenderTexture(1280, 720, 24);
            var before = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = before;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            Directory.CreateDirectory("logs/arenas");
            File.WriteAllBytes("logs/arenas/" + name + ".png", tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        [UnityTest]
        public IEnumerator BrannsFloorIsDrawnAndBurnsAsItIsDrawn()
        {
            yield return Into("Greybox_Emberdown_Stair_3", 2f);
            var brann = Object.FindFirstObjectByType<Brann>();
            Assert.IsNotNull(brann, "Brann waits in the cold furnace");
            var arena = Object.FindFirstObjectByType<BossArena>();
            yield return Until(() => arena.State == BossArena.ArenaState.Fighting, 8f, "the fight");
            Assert.IsTrue(brann.FloorDrawn, "the floor wears the grate's drawing");
            var grates = Enumerable.Range(0, brann.sections).Select(i => brann.transform.parent.Find("Furnace_" + i)).ToList();
            Assert.IsTrue(grates.All(g => g != null && g.GetComponent<InkSheetPlayer>() != null), "every section is drawn");
            yield return Until(() => Enumerable.Range(0, brann.sections).Any(brann.IsHot), 5f, "a hot section");
            for (int i = 0; i < brann.sections; i++)
                Assert.AreEqual(brann.SectionClip(i), grates[i].GetComponent<InkSheetPlayer>().Current, "section " + i + " shows its state");
            yield return Picture("Brann_Furnace");
        }

        [UnityTest]
        public IEnumerator OrielsYardStandsItsDrill()
        {
            yield return Into("Greybox_Halden_Bastion_2", -1f);
            var room = Room.Current;
            foreach (var prop in new[] { "Prop_DrillRack", "Prop_ChalkBoard", "Prop_Paces" })
            {
                var t = room.GetComponentsInChildren<Transform>(true).FirstOrDefault(c => c.name == prop);
                Assert.IsNotNull(t, "the yard stands its " + prop);
                Assert.IsTrue(t!.GetComponent<Renderer>().enabled, prop + " is drawn");
            }
            yield return Picture("Oriel_Yard");
        }
    }
}
