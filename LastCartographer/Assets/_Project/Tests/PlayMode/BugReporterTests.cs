#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The in-game reporter (PRO-07): it wakes with the game, F12's report carries the save and a screenshot, and in
    /// a built player the first exception writes one by itself, once.
    /// </summary>
    public class BugReporterTests
    {
        string? _root;
        GameObject? _go;

        [SetUp]
        public void SetUp()
        {
            GameState.NewGame();
            _root = Path.Combine(Path.GetTempPath(), "owsbg-reporter-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.Destroy(_go);
            if (_root != null && Directory.Exists(_root)) Directory.Delete(_root, true);
            LogTail.Clear();
            GameState.NewGame();
        }

        BugReporter Own()
        {
            _go = new GameObject("Reporter");
            var r = _go.AddComponent<BugReporter>();
            r.Root = _root;
            return r;
        }

        [UnityTest]
        public IEnumerator ItWakesWithTheGameAndStaysQuietInTheEditor()
        {
            yield return null;
            Assert.IsNotNull(BugReporter.Instance, "booted with the first scene");
            Assert.IsFalse(BugReporter.Instance!.AutoCapture, "the editor's exceptions are the tests' business");
            Assert.AreEqual(Application.persistentDataPath, BugReporter.Instance.Root);
        }

        [UnityTest]
        public IEnumerator TheFirstExceptionWritesAReportByItselfOnce()
        {
            var r = Own();
            r.AutoCapture = true;
            GameState.World.Set("reporter.test", true);
            LogAssert.Expect(LogType.Exception, new Regex("first boom"));
            Debug.LogException(new InvalidOperationException("first boom"));
            yield return null;
            Assert.AreEqual(1, r.Written);
            StringAssert.EndsWith("-exception", r.LastFolder);
            var md = File.ReadAllText(Path.Combine(r.LastFolder!, "report.md"));
            StringAssert.Contains("**Symptom:** Crash", md);
            StringAssert.Contains("**Severity:** at least Blocker", md);
            StringAssert.Contains("written by the game after an exception", md);
            StringAssert.Contains("first boom", md, "the exception is in the report");
            StringAssert.Contains("reporter.test", File.ReadAllText(Path.Combine(r.LastFolder!, "save.json")), "with the world as it was");

            LogAssert.Expect(LogType.Exception, new Regex("second boom"));
            Debug.LogException(new InvalidOperationException("second boom"));
            yield return null;
            Assert.AreEqual(1, r.Written, "one a session; a repeating exception would flood the folder");
        }

        [UnityTest]
        public IEnumerator APressedReportCarriesTheSaveAndAScreenshot()
        {
            var r = Own();
            Assert.IsFalse(r.AutoCapture);
            var cam = new GameObject("Cam") { tag = "MainCamera" };
            cam.transform.SetParent(_go!.transform);
            cam.AddComponent<Camera>();
            string? told = null;
            Action<string> onSaved = d => told = d;
            BugReporter.Saved += onSaved;
            try { yield return r.Capture(BugBar.Symptom.Unsorted); }
            finally { BugReporter.Saved -= onSaved; }
            Assert.AreEqual(1, r.Written);
            Assert.AreEqual(r.LastFolder, told, "the HUD is told where");
            StringAssert.EndsWith("-pressed", r.LastFolder);
            Assert.IsTrue(File.Exists(Path.Combine(r.LastFolder!, "report.md")));
            Assert.IsTrue(File.Exists(Path.Combine(r.LastFolder!, "save.json")));
            Assert.IsTrue(File.Exists(Path.Combine(r.LastFolder!, "log.txt")));
            var md = File.ReadAllText(Path.Combine(r.LastFolder!, "report.md"));
            StringAssert.Contains("**Severity:** to triage", md);
            StringAssert.Contains("**How:** F12", md);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.IsTrue(File.Exists(Path.Combine(r.LastFolder!, "screenshot.png")), "a screenshot when there is a screen");
            LogAssert.Expect(LogType.Exception, new Regex("quiet"));
            Debug.LogException(new InvalidOperationException("quiet"));
            yield return null;
            Assert.AreEqual(1, r.Written, "nothing written by itself unless asked");
        }
    }
}
