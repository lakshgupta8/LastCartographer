using System;
using System.IO;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    /// <summary>
    /// The feel-test's gate, headless (PRO-03): <c>Unity.exe -batchmode -executeMethod OWSBG.Setup.FeelGateSetup.Run
    /// -feelAnswers answers.csv -feelSessions logs/feel -feelReport logs/feel/gate.md -quit</c>. Exits 0 when the gate
    /// is met, 1 when not, 2 when the files can't be read. tools/feel-gate.ps1 wraps it.
    /// </summary>
    public static class FeelGateSetup
    {
        static string Arg(string[] args, string name, string fallback)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string answers = Arg(args, "-feelAnswers", "logs/feel/answers.csv");
            string sessions = Arg(args, "-feelSessions", "logs/feel");
            string report = Arg(args, "-feelReport", "logs/feel/gate.md");
            try
            {
                if (!File.Exists(answers)) { Debug.LogError("[OWSBG] feel gate: no answers at " + answers); EditorApplication.Exit(2); return; }
                var gate = FeelGate.Run(answers, sessions, report, out var text);
                Debug.Log("[OWSBG] feel gate: " + (gate.Passed ? "passed" : "not yet") + "; " + report + "\n" + text);
                EditorApplication.Exit(gate.Passed ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogError("[OWSBG] feel gate: " + e.Message);
                EditorApplication.Exit(2);
            }
        }
    }
}
