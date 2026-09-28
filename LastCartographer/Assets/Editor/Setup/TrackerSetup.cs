using System;
using System.IO;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    /// <summary>
    /// The plan as JSON for the tracker (PRO-02): <c>Unity.exe -batchmode -executeMethod OWSBG.Setup.TrackerSetup.Export
    /// [-trackerOut logs/tracker.json] -quit</c>. tools/tracker.ps1 runs it, then makes the issues with gh. Exits 0
    /// when the plan parses, 2 when it doesn't.
    /// </summary>
    public static class TrackerSetup
    {
        public static void Export()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-trackerOut");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string outPath = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(root, "logs", "tracker.json");
            try
            {
                var doc = Plan.Load(Path.Combine(root, Plan.RelativePath));
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)) ?? ".");
                File.WriteAllText(outPath, Plan.ToJson(doc));
                Debug.Log("[OWSBG] tracker: " + doc.Rows.Count + " rows, " + doc.Milestones.Count + " milestones to " + outPath);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[OWSBG] tracker: " + e.Message);
                EditorApplication.Exit(2);
            }
        }
    }
}
