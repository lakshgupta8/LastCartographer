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
    /// The look's passes, measured on a real render of room A (PRG-04): the foreground reeds' edges are smeared
    /// (foreground blur, measured against a sharp render), and flat paper is not flat (paper grain).
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
            // The foreground reeds (the kit's Paper_Fore_Reeds at z = -4, ENV-01) fill the bottom of the frame. With the
            // blur on their edges spread over several pixels; with its radius at zero they are pen-sharp. Render both
            // and the blurred frame has to carry much less horizontal edge energy across that band.
            var blur = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m => m.name == "M_FS_ForegroundBlur");
            Assert.IsNotNull(blur, "the foreground blur's material is loaded with the renderer");
            float radius = blur.GetFloat("_MaxRadius");
            Assert.Greater(radius, 0f, "the blur has a radius");
            Color[] soft, hard;
            try
            {
                soft = Capture(w, h, "look-test.png");
                blur.SetFloat("_MaxRadius", 0f);
                yield return null;
                hard = Capture(w, h, "look-test-sharp.png");
            }
            finally { blur.SetFloat("_MaxRadius", radius); }

            float Lum(Color[] px, int x, int y) { var c = px[y * w + x]; return 0.299f * c.r + 0.587f * c.g + 0.114f * c.b; }
            // Walk each row of the band: where a reed (dark) gives way to paper (light), count the pixels in between.
            // A pen edge crosses in about one pixel; the blur spreads it over several. Plank lines behind the reeds
            // are the same in both frames, so the mean width moves with the reeds alone.
            float MeanEdgeWidth(Color[] px, int y0, int y1)
            {
                int edges = 0, width = 0;
                for (int y = y0; y < y1; y++)
                {
                    for (int x = 0; x < w - 9; x++)
                    {
                        float lo = Lum(px, x, y), hi = Lum(px, x + 8, y);
                        if (lo >= 0.35f || hi < 0.6f) continue;   // a reed pixel with paper eight pixels on: an edge between
                        int soft = 0;
                        for (int k = 1; k < 8; k++)
                        {
                            float t = (Lum(px, x + k, y) - lo) / (hi - lo);
                            if (t > 0.15f && t < 0.85f) soft++;
                        }
                        edges++; width += soft;
                        x += 8;
                    }
                }
                return edges == 0 ? 0f : (float)width / edges;
            }
            float softWidth = MeanEdgeWidth(soft, 20, h / 5), hardWidth = MeanEdgeWidth(hard, 20, h / 5);
            Debug.Log("[LOOK] reed edge width: blurred " + softWidth.ToString("F2") + " px, sharp " + hardWidth.ToString("F2") + " px");
            Assert.Greater(hardWidth, 0f, "the reeds have edges");
            Assert.Less(hardWidth, 2.0f, "a pen edge is sharp");
            Assert.Greater(softWidth, hardWidth + 1.5f, "the foreground reeds are blurred: their edges are wider than the sharp render's");
            // Grain: the paper above the far dunes is flat colour; without grain its luminance is constant.
            float mean = 0f; int n = 0;
            for (int y = h - 40; y < h - 12; y++) for (int x = 300; x < 900; x++) { mean += Lum(soft, x, y); n++; }
            mean /= n;
            float var = 0f;
            for (int y = h - 40; y < h - 12; y++) for (int x = 300; x < 900; x++) { float d = Lum(soft, x, y) - mean; var += d * d; }
            float std = Mathf.Sqrt(var / n);
            Assert.Greater(std, 0.004f, "paper grain gives flat paper some texture (std " + std.ToString("F4") + ")");
            Assert.Less(std, 0.08f, "but not so much that it reads as noise");
        }

        static Color[] Capture(int w, int h, string file)
        {
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
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, file), tex.EncodeToPNG());
            Object.Destroy(rt); Object.Destroy(tex);
            return px;
        }
    }
}
