using System;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    /// <summary>
    /// PRO-06's gate, headless: <c>Unity.exe -batchmode -executeMethod OWSBG.Setup.PerfGateSetup.Run -perfReports logs/perf
    /// -perfGate logs/perf/gate.md -quit</c>. Every probe report in the folder is judged (<see cref="PerfTarget"/>). Exits 0
    /// when one proves the target, 1 when none does yet, 2 when the folder can't be read. tools/perf-gate.ps1 wraps it.
    /// </summary>
    public static class PerfGateSetup
    {
        static string Arg(string[] args, string name, string fallback)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string dir = Arg(args, "-perfReports", "logs/perf");
            string report = Arg(args, "-perfGate", "logs/perf/gate.md");
            try
            {
                bool met = PerfTarget.Run(dir, report, out var text);
                Debug.Log("[OWSBG] perf gate: " + (met ? "met" : "not yet") + "; " + report + "\n" + text);
                EditorApplication.Exit(met ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogError("[OWSBG] perf gate: " + e.Message);
                EditorApplication.Exit(2);
            }
        }
    }
}
