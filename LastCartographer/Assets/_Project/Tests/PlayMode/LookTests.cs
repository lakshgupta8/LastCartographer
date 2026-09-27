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
    /// The look's passes, measured on a real render of room A (PRG-04): the foreground paper layer's edge is
    /// smeared over several pixels (foreground blur), and a flat lit strip is not flat (paper grain).
    /// </summary>
    public class LookTests
    {
        [SetUp]
        public void SetUp() { GameState.NewGame(); Bootstrap.SkipPrologueOverride = true; }

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

        [UnityTest]
        public IEnumerator ForegroundEdgeIsSoftAndPaperHasGrain()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            float t = 0f;
            while ((RoomManager.Instance == null || RoomManager.Instance.CurrentRoom != "Greybox_Saltmarrow_A") && t < 10f) { t += Time.deltaTime; yield return null; }
            var wren = Object.FindFirstObjectByType<WrenController>();
            Assert.IsNotNull(wren);
            wren.Teleport(new Vector2(-2f, 0f));
            for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();   // camera settles
            for (int i = 0; i < 5; i++) yield return null;

            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var cam = Camera.main;
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            var px = tex.GetPixels();
            var outDir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).Parent.FullName, "logs");
            System.IO.Directory.CreateDirectory(outDir);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "look-test.png"), tex.EncodeToPNG());
            Object.Destroy(rt); Object.Destroy(tex);

            float Lum(int x, int y) { var c = px[y * w + x]; return 0.299f * c.r + 0.587f * c.g + 0.114f * c.b; }

            // The foreground reeds (z = -4) end in a horizontal edge against the pale floor strip below the plane.
            // Walk a column from the bottom up: find the darkest run (reeds) meeting the brightest run (strip), then
            // count the pixels in between whose luminance sits well inside the two extremes.
            int col = 200;   // clear of Wren, the desk and the ledger
            // The reeds are the lowest thing on screen: the first strong dark-to-light step scanning upward.
            int best = -1; float bestJump = 0f;
            for (int y = 8; y < h / 3; y++)
            {
                float jump = Lum(col, y + 6) - Lum(col, y - 6);
                if (jump > 0.3f) { bestJump = jump; best = y; break; }
            }
            Assert.Greater(bestJump, 0.15f, "there is a dark-to-light edge in the lower half of the frame");
            var profile = new System.Text.StringBuilder("[LOOK] edge at y=" + best + " jump=" + bestJump.ToString("F3") + " profile:");
            for (int y = best - 10; y <= best + 10; y++) profile.Append(' ').Append(Lum(col, Mathf.Clamp(y, 0, h - 1)).ToString("F3"));
            Debug.Log(profile.ToString());
            float lo = Lum(col, Mathf.Max(0, best - 8)), hi = Lum(col, Mathf.Min(h - 1, best + 8));
            int soft = 0;
            for (int y = best - 8; y <= best + 8; y++)
            {
                float l = Lum(col, y);
                float k = (l - lo) / Mathf.Max(0.001f, hi - lo);
                if (k > 0.2f && k < 0.8f) soft++;
            }
            Assert.GreaterOrEqual(soft, 3, "the foreground edge is blurred over several pixels (found " + soft + " transitional pixels; a hard edge has 1)");

            // Grain: the floor strip is a flat lit quad; without grain its luminance is constant.
            float mean = 0f; int n = 0;
            for (int y = best + 12; y < best + 24; y++) for (int x = 300; x < 900; x++) { mean += Lum(x, y); n++; }
            mean /= n;
            float var = 0f;
            for (int y = best + 12; y < best + 24; y++) for (int x = 300; x < 900; x++) { float d = Lum(x, y) - mean; var += d * d; }
            float std = Mathf.Sqrt(var / n);
            Assert.Greater(std, 0.004f, "paper grain gives a flat strip some texture (std " + std.ToString("F4") + ")");
            Assert.Less(std, 0.08f, "but not so much that it reads as noise");
        }
    }
}
