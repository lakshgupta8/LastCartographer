using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace OWSBG.Editor
{
    /// <summary>
    /// Lets tooling drive the open editor without batch mode (Unity allows one editor per project).
    /// Drop a file at <c>&lt;repo&gt;/logs/requests/&lt;name&gt;.req</c> whose first line is one of:
    ///   configure | build-scenes | screenshot | tests-playmode | tests-editmode | refresh
    /// The runner executes it when the editor is idle and writes <c>&lt;name&gt;.done</c> with the result.
    /// </summary>
    [InitializeOnLoad]
    public static class SetupRequests
    {
        static readonly string RequestDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "logs", "requests"));
        const string PendingTestKey = "owsbg.pendingTestRequest";
        static double _nextPoll;
        static readonly TestCallbacks Callbacks = new TestCallbacks();
        static TestRunnerApi _api;

        static SetupRequests()
        {
            if (Application.isBatchMode) return;
            EditorApplication.update += Poll;
            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _api.RegisterCallbacks(Callbacks);   // re-registered after every domain reload
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 2.0;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!string.IsNullOrEmpty(SessionState.GetString(PendingTestKey, ""))) return;   // a test run is in flight
            if (!Directory.Exists(RequestDir)) return;

            foreach (var req in Directory.GetFiles(RequestDir, "*.req"))
            {
                var name = Path.GetFileNameWithoutExtension(req);
                string command;
                try { command = File.ReadAllLines(req)[0].Trim(); File.Delete(req); }
                catch (Exception e) { Done(name, "ERROR reading request: " + e.Message); continue; }
                Debug.Log("[OWSBG] request '" + name + "': " + command);
                try { Run(name, command); }
                catch (Exception e) { Done(name, "ERROR: " + e); }
                break;   // one request per poll; some commands reload the domain
            }
        }

        static void Run(string name, string command)
        {
            switch (command)
            {
                case "configure":   InvokeSetup("Configure");           Done(name, "OK configure"); break;
                case "build-scenes": InvokeSetup("BuildBootstrapScene"); Done(name, "OK build-scenes"); break;
                case "screenshot":  InvokeSetup("CaptureScreenshot");   Done(name, "OK screenshot"); break;
                case "refresh":     AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); Done(name, "OK refresh"); break;
                case "tests-playmode": StartTests(name, TestMode.PlayMode); break;
                case "tests-editmode": StartTests(name, TestMode.EditMode); break;
                default: Done(name, "ERROR unknown command: " + command); break;
            }
        }

        static void InvokeSetup(string method)
        {
            var type = Type.GetType("OWSBG.Setup.ProjectSetup, Assembly-CSharp-Editor");
            if (type == null) throw new InvalidOperationException("ProjectSetup not found (Assets/Editor/Setup)");
            var m = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            if (m == null) throw new InvalidOperationException("ProjectSetup." + method + " not found");
            m.Invoke(null, null);
        }

        static void StartTests(string name, TestMode mode)
        {
            SessionState.SetString(PendingTestKey, name);
            File.WriteAllText(Path.Combine(RequestDir, name + ".running"), mode.ToString());
            _api.Execute(new ExecutionSettings(new Filter { testMode = mode }));
        }

        static void Done(string name, string text)
        {
            Directory.CreateDirectory(RequestDir);
            var running = Path.Combine(RequestDir, name + ".running");
            if (File.Exists(running)) File.Delete(running);
            File.WriteAllText(Path.Combine(RequestDir, name + ".done"), text + "\n");
        }

        sealed class TestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var name = SessionState.GetString(PendingTestKey, "");
                SessionState.EraseString(PendingTestKey);
                if (string.IsNullOrEmpty(name)) return;
                var sb = new StringBuilder();
                sb.Append("TOTAL ").Append(result.PassCount + result.FailCount + result.SkipCount)
                  .Append(" PASSED ").Append(result.PassCount)
                  .Append(" FAILED ").Append(result.FailCount)
                  .Append(" SKIPPED ").Append(result.SkipCount).Append('\n');
                Collect(result, sb);
                Done(name, sb.ToString().TrimEnd());
            }

            static void Collect(ITestResultAdaptor r, StringBuilder sb)
            {
                if (r.HasChildren)
                {
                    foreach (var c in r.Children) Collect(c, sb);
                    return;
                }
                sb.Append(r.TestStatus == TestStatus.Passed ? "  PASS " : r.TestStatus == TestStatus.Failed ? "  FAIL " : "  SKIP ")
                  .Append(r.Test.Name);
                if (r.TestStatus == TestStatus.Failed && !string.IsNullOrEmpty(r.Message))
                    sb.Append("\n       ").Append(r.Message.Replace("\n", " | "));
                sb.Append('\n');
            }
        }
    }
}
