#nullable enable
using System.Collections;
using System.IO;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace OWSBG.Tests
{
    /// <summary>
    /// The atlas's map in the game (docs/design/atlas-map.md): a room she comes into is walked; the book opens at the
    /// page she stands on with the nib in her room; what she has surveyed is in ink and what the vantage saw in pencil;
    /// ← → turn the pages round the book. The open page's picture is written to logs/atlas/.
    /// </summary>
    public class AtlasMapPlayTests
    {
        [SetUp] public void SetUp() { Time.timeScale = 1f; GameState.NewGame(); Bootstrap.SkipPrologueOverride = true; }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AtlasView.Instance?.Close();
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
            while (!cond() && t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        /// <summary>The UI as it draws, written to logs/atlas/ on paper.</summary>
        static IEnumerator Picture(string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;   // no GPU here: no picture
            var doc = UiRoot.Instance.GetComponent<UIDocument>();
            var ps = doc.panelSettings;
            var rt = new RenderTexture(1920, 1080, 24);
            ps.targetTexture = rt;
            for (int i = 0; i < 4; i++) yield return null;   // (WaitForEndOfFrame never fires in batch mode; plain frames do)
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            ps.targetTexture = null;
            var px = tex.GetPixels32();
            var paper = new Color32(110, 104, 96, 255);   // the desk under the page
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                float a = c.a / 255f;
                px[i] = new Color32((byte)(c.r * a + paper.r * (1 - a)), (byte)(c.g * a + paper.g * (1 - a)), (byte)(c.b * a + paper.b * (1 - a)), 255);
            }
            tex.SetPixels32(px);
            Directory.CreateDirectory("logs/atlas");
            File.WriteAllBytes("logs/atlas/" + name + ".png", tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheBookOpensAtHerPageWithWhatSheHasDrawn()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            var w = GameState.World;

            // She walks the coast to the stilts; surveys the quay; the shore she started on is walked as she opens the book.
            RoomManager.Instance.Transition("Greybox_Saltmarrow_Stilts", "West");
            yield return Until(() => RoomManager.Instance.CurrentRoom == "Greybox_Saltmarrow_Stilts" && !RoomManager.Instance.IsTransitioning, 10f, "the stilts");
            Assert.IsTrue(AtlasMap.IsWalked(w, "Saltmarrow_Stilts"), "a room she comes into is walked");
            foreach (var v in Atlas.VantagesOf("Saltmarrow_A")) Atlas.Survey(w, v.Id);

            var atlas = AtlasView.Instance;
            Assert.IsNotNull(atlas);
            yield return Until(() => atlas.Map != null, 5f, "the atlas built");
            atlas.Open(Object.FindFirstObjectByType<WrenController>());
            yield return null; yield return null;
            Assert.AreEqual("The Saltmarrow", AtlasMap.Pages[atlas.Page], "the book opens at the page she stands on");
            var map = atlas.Map;
            Assert.AreEqual("The Saltmarrow", map.PageName);
            Assert.AreEqual("Saltmarrow_Stilts", map.Here, "the nib in her room");
            Assert.AreEqual(MapInk.Drawn, AtlasMap.InkOf(w, "Saltmarrow_A"), "the quay surveyed, in ink");
            Assert.AreEqual(MapInk.Drawn, AtlasMap.InkOf(w, "Saltmarrow_Stilts"), "the stilts walked: nothing to survey, in ink");
            Assert.GreaterOrEqual(map.Count(MapInk.Drawn), 2);
            if (!AtlasMap.IsWalked(w, "Saltmarrow_Shore") && Atlas.VantagesOf("Saltmarrow_Shore").Count > 0)
                Assert.AreEqual(MapInk.Seen, AtlasMap.InkOf(w, "Saltmarrow_Shore"), "the shore, beside the quay: the vantage saw it");
            Assert.Greater(map.contentRect.height, 100f, "the page has room to draw on");
            yield return Picture("Saltmarrow");

            atlas.Turn(1);
            yield return null;
            Assert.AreEqual("Emberdown", map.PageName, "→ turns the page");
            Assert.AreEqual(0, map.Count(MapInk.Drawn) + map.Count(MapInk.Walked) + map.Count(MapInk.Seen), "a page she has not been to is blank");
            atlas.Turn(-2);
            yield return null;
            Assert.AreEqual(AtlasMap.Pages[AtlasMap.Pages.Length - 1], map.PageName, "and ← round the back of the book");
            atlas.Close();
        }
    }
}
