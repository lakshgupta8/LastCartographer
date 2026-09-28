using System;
using System.Collections;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The in-game bug reporter (PRO-07). F12 writes a <see cref="BugReport"/> folder under the game's data folder:
    /// report.md, the save as it is, the last log lines and a screenshot, and says where on the HUD. In a built
    /// player the first exception writes one by itself ("exception"), once a session, so a crash comes with what
    /// the tracker needs. It wakes with the game and lives across rooms.
    /// </summary>
    public sealed class BugReporter : MonoBehaviour
    {
        public static BugReporter Instance { get; private set; }
        /// <summary>A report was written to this folder; the HUD says so.</summary>
        public static event Action<string> Saved;

        /// <summary>Where the BugReports folder goes. Tests point it somewhere temporary.</summary>
        public string Root { get; set; }
        /// <summary>Write a report on the first exception. On in a built player, off in the editor unless a test says so.</summary>
        public bool AutoCapture { get; set; }
        /// <summary>The last folder written, or null.</summary>
        public string LastFolder { get; private set; }
        public int Written { get; private set; }

        bool _autoDone, _busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            LogTail.Install();
            if (Instance != null) return;
            var go = new GameObject("~BugReporter");
            DontDestroyOnLoad(go);
            go.AddComponent<BugReporter>();
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            Root ??= Application.persistentDataPath;
            AutoCapture = !Application.isEditor;
            LogTail.Install();
            LogTail.ExceptionLogged += OnException;
        }

        void OnDestroy()
        {
            LogTail.ExceptionLogged -= OnException;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.f12Key.wasPressedThisFrame && !_busy) StartCoroutine(Capture(BugBar.Symptom.Unsorted, "pressed"));
        }

        void OnException(string condition, string stack)
        {
            if (!AutoCapture || _autoDone || _busy) return;
            _autoDone = true;
            // Not a coroutine: the exception may have come from anywhere, and a frame later the player may be gone.
            Write(BugBar.Symptom.Crash, "exception", null);
        }

        /// <summary>A report with a screenshot, taken at the end of this frame.</summary>
        public IEnumerator Capture(BugBar.Symptom symptom, string kind = "pressed")
        {
            _busy = true;
            byte[] png = null;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                // The screen, UI and all, once the frame is drawn. WaitForEndOfFrame never fires in batch mode, so
                // there the camera is rendered by hand instead: the room without the pages.
                if (Application.isBatchMode) { yield return null; png = RenderCamera(); }
                else
                {
                    yield return new WaitForEndOfFrame();
                    Texture2D tex = null;
                    try
                    {
                        tex = ScreenCapture.CaptureScreenshotAsTexture();
                        png = tex != null ? tex.EncodeToPNG() : null;
                    }
                    catch (Exception e) { Debug.LogWarning("[OWSBG] bug report: no screenshot (" + e.Message + ")"); }
                    finally { if (tex != null) Destroy(tex); }
                }
            }
            Write(symptom, kind, png);
            _busy = false;
        }

        static byte[] RenderCamera()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            RenderTexture rt = null; Texture2D tex = null;
            var prevTarget = cam.targetTexture; var prevActive = RenderTexture.active;
            try
            {
                int w = Math.Max(16, Screen.width), h = Math.Max(16, Screen.height);
                rt = new RenderTexture(w, h, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                return tex.EncodeToPNG();
            }
            catch (Exception e) { Debug.LogWarning("[OWSBG] bug report: no screenshot (" + e.Message + ")"); return null; }
            finally
            {
                cam.targetTexture = prevTarget; RenderTexture.active = prevActive;
                if (tex != null) Destroy(tex);
                if (rt != null) { rt.Release(); Destroy(rt); }
            }
        }

        /// <summary>The report, now, to <see cref="Root"/>. Returns the folder.</summary>
        public string Write(BugBar.Symptom symptom, string kind, byte[] screenshotPng)
        {
            string room = RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : null;
            string scene = SceneManager.GetActiveScene().name;
            Vector2? at = WrenController.Current != null ? (Vector2?)WrenController.Current.transform.position : null;
            var report = BugReport.Gather(GameState.World, room, scene, at, symptom, kind);
            string dir;
            try { dir = report.Write(Root ?? Application.persistentDataPath, screenshotPng); }
            catch (Exception e)
            {
                Debug.LogWarning("[OWSBG] bug report: could not write (" + e.Message + ")");
                return null;
            }
            LastFolder = dir;
            Written++;
            Debug.Log("[OWSBG] bug report: " + dir);
            Saved?.Invoke(dir);
            return dir;
        }
    }
}
