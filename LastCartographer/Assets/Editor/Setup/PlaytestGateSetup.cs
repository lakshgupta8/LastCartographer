using System;
using System.IO;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    /// <summary>
    /// The first test round's gate, headless (PRO-04): <c>Unity.exe -batchmode -executeMethod OWSBG.Setup.PlaytestGateSetup.Run
    /// -playtestAnswers answers.csv -playtestSessions logs/playtest -playtestReport logs/playtest/gate.md -quit</c>. Exits 0
    /// when the round's bar is met, 1 when not, 2 when the files can't be read. tools/playtest-gate.ps1 wraps it.
    /// </summary>
    public static class PlaytestGateSetup
    {
        static string Arg(string[] args, string name, string fallback)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string answers = Arg(args, "-playtestAnswers", "logs/playtest/answers.csv");
            string sessions = Arg(args, "-playtestSessions", "logs/playtest");
            string report = Arg(args, "-playtestReport", "logs/playtest/gate.md");
            try
            {
                if (!File.Exists(answers)) { Debug.LogError("[OWSBG] playtest gate: no answers at " + answers); EditorApplication.Exit(2); return; }
                var gate = Playtest.Run(answers, sessions, report);
                Debug.Log("[OWSBG] playtest gate: " + (gate.Passed ? "passed" : "not yet") + "; " + report);
                EditorApplication.Exit(gate.Passed ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogError("[OWSBG] playtest gate: " + e.Message);
                EditorApplication.Exit(2);
            }
        }
    }
}
