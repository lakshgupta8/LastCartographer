using System.Collections;
using System.IO;
using NUnit.Framework;
using OWSBG.Narrative;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace OWSBG.Tests
{
    /// <summary>
    /// Renders the shipped scene with the atlas UI composited over the camera and writes PNGs to
    /// logs/ (ui-hud.png, ui-dialogue.png, ui-desk.png, ui-ledger.png, ui-atlas.png). A visual check for headless runs; the only
    /// assertion is that the files were written.
    /// </summary>
    public class UiScreenshotTests
    {
        [SetUp]
        public void SetUp() { OWSBG.Core.GameState.NewGame(); Bootstrap.SkipPrologueOverride = true; }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Bootstrap.SkipPrologueOverride = null;
            Time.timeScale = 1f;
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_"))
                    yield return SceneManager.UnloadSceneAsync(s);
            }
        }

        /// <summary>A capture slower than this is a software rasteriser: here one takes well under a second.</summary>
        const float SlowCaptureSeconds = 10f;

        /// <summary>
        /// The whole test's budget: here it takes about two seconds. Past this after any step the test steps aside and
        /// says which step took what, so the runner's log tells where a machine without a GPU spends its minutes
        /// (the default three-minute timeout of a test is checked between frames, and one capture in software can
        /// hold a frame for over a minute).
        /// </summary>
        const float BudgetSeconds = 60f;

        static string OutDir => Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "logs");

        [UnityTest]
        public IEnumerator CaptureHudDialogueAndDeskPages()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("no graphics device here (" + SystemInfo.graphicsDeviceName + "); the screenshots want a GPU");

            // The clock: each step's seconds are kept for the message, and past the budget the test steps aside.
            var times = new System.Text.StringBuilder();
            float started = Time.realtimeSinceStartup, last = started;
            float Step(string name)
            {
                float now = Time.realtimeSinceStartup, seconds = now - last;
                times.Append(name).Append(' ').Append(seconds.ToString("0.0")).Append("s, ");
                last = now;
                if (now - started > BudgetSeconds)
                    Assert.Ignore("over " + BudgetSeconds.ToString("0") + " s here (" + times + "on " + SystemInfo.graphicsDeviceName + "); the screenshots want a GPU");
                return seconds;
            }

            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            float t = 0f;
            while ((RoomManager.Instance == null || string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom)) && t < 8f) { t += Time.deltaTime; yield return null; }
            var wren = Object.FindFirstObjectByType<WrenController>();
            Assert.IsNotNull(wren);
            var ui = UiRoot.Instance;
            Assert.IsNotNull(ui, "UI root in the persistent scene");
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Step("scene");

            const int w = 1280, h = 720;
            var uiRt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            var camRt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var doc = ui.GetComponent<UIDocument>();
            var settings = Object.Instantiate(doc.panelSettings);   // do not touch the asset
            settings.targetTexture = uiRt;
            settings.clearColor = true;                        // otherwise old pages linger under new ones
            settings.colorClearValue = new Color(0f, 0f, 0f, 0f);
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            doc.panelSettings = settings;
            var cam = Camera.main;
            Assert.IsNotNull(cam);

            // Some ink and a lost mask so the HUD shows mixed states.
            wren.GetComponent<Inkwell>().Add(4);
            wren.GetComponent<WrenVitals>().Damage(1);
            // The pictures are for a person to look at, drawn through the real pipeline and read back. On a machine
            // with no GPU (CI's runners draw in software) a capture can take over a minute and five never finish in the
            // test's three minutes. The first capture is the measure: well under a second here; past
            // SlowCaptureSeconds, step aside and say so. The budget above covers whatever else is slow there.
            yield return Capture(cam, camRt, uiRt, "ui-hud.png");
            float captureSeconds = Step("hud");
            if (captureSeconds > SlowCaptureSeconds)
                Assert.Ignore("one capture took " + captureSeconds.ToString("0.0") + " s here (" + SystemInfo.graphicsDeviceName + "); the screenshots want a GPU");

            var svc = DialogueService.Instance;
            Assert.IsNotNull(svc);
            svc.StartNode("Quay_Sable");
            var view = ui.GetComponent<DialogueView>();
            for (int i = 0; i < 90 && !view.IsVisible; i++) yield return null;
            Step("dialogue shown");
            yield return Capture(cam, camRt, uiRt, "ui-dialogue.png");
            svc.Stop();
            for (int i = 0; i < 30 && svc.IsRunning; i++) yield return null;
            Assert.IsFalse(view.IsVisible, "a stopped conversation leaves no page");
            Step("dialogue");

            var menu = ui.GetComponent<DeskMenu>();
            menu.Open(wren);
            yield return null;
            menu.SetRow(2);
            yield return Capture(cam, camRt, uiRt, "ui-desk.png");
            menu.Close();
            Step("desk");

            var ledger = Object.FindFirstObjectByType<CommissionLedger>();
            Assert.IsNotNull(ledger, "room A has the Saltmarrow ledger");
            var page = ui.GetComponent<LedgerView>();
            ledger.Interact(wren.GetComponent<Interactor>());
            yield return null;
            Assert.IsTrue(page.IsOpen);
            page.Confirm();                                     // take the first one so the journal has an entry
            page.SetRow(1);
            yield return Capture(cam, camRt, uiRt, "ui-ledger.png");
            page.Close();
            Step("ledger");
            // The atlas spread: stand at the desk so the travel list shows, with the lamp lit and the quay drawn.
            var world = OWSBG.Core.GameState.World;
            OWSBG.Core.Atlas.Survey(world, "Saltmarrow_A/Reedmother");
            OWSBG.Core.Atlas.Survey(world, "Saltmarrow_Lighthouse/Lamp");
            OWSBG.Core.Atlas.Discover(world, "lamp.Saltmarrow_Lighthouse");
            OWSBG.Core.FadeStages.Advance(world, "Saltmarrow_B", 2);
            wren.Teleport(new Vector2(-4.5f, 0f));
            for (int i = 0; i < 30 && TravelPoint.Nearby == null; i++) yield return new WaitForFixedUpdate();
            var atlas = ui.GetComponent<AtlasView>();
            var journal = ui.GetComponent<JournalView>();
            atlas.Toggle();
            yield return null;
            Assert.IsTrue(atlas.IsOpen && journal.IsOpen, "the journal opens on the atlas's right page");
            yield return Capture(cam, camRt, uiRt, "ui-atlas.png");
            atlas.Toggle();
            Step("atlas");
            Debug.Log("[OWSBG] ui screenshots: " + times + "on " + SystemInfo.graphicsDeviceName);

            Object.Destroy(uiRt); Object.Destroy(camRt);
            Assert.IsTrue(File.Exists(Path.Combine(OutDir, "ui-desk.png")));
        }

        static IEnumerator Capture(Camera cam, RenderTexture camRt, RenderTexture uiRt, string file)
        {
            // Let the panel draw into its target for a few frames, then read both and composite.
            // (WaitForEndOfFrame never fires in batch mode; plain frame yields do.)
            for (int i = 0; i < 4; i++) yield return null;
            var prev = cam.targetTexture;
            cam.targetTexture = camRt;
            cam.Render();
            cam.targetTexture = prev;

            int w = camRt.width, h = camRt.height;
            var scene = ReadBack(camRt);
            var overlay = ReadBack(uiRt);
            var pixels = scene.GetPixels();
            var over = overlay.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                var o = over[i];
                pixels[i] = Color.Lerp(pixels[i], new Color(o.r, o.g, o.b, 1f), o.a);
            }
            scene.SetPixels(pixels);
            scene.Apply();
            Directory.CreateDirectory(OutDir);
            File.WriteAllBytes(Path.Combine(OutDir, file), scene.EncodeToPNG());
            Object.Destroy(scene); Object.Destroy(overlay);
            Debug.Log("[OWSBG] wrote " + file);
        }

        static Texture2D ReadBack(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }
    }
}
