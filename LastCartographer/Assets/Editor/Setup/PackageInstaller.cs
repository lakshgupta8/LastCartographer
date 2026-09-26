// Batch-mode package installer. Run with:
//   Unity.exe -batchmode -nographics -projectPath <proj> -executeMethod OWSBG.Setup.PackageInstaller.Install -logFile <log>
// Deliberately NOT using -quit: the Package Manager request is asynchronous, so we poll and
// exit ourselves when it finishes.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace OWSBG.Setup
{
    public static class PackageInstaller
    {
        static readonly string[] Packages =
        {
            "com.unity.render-pipelines.universal",
            "com.unity.cinemachine",
            "com.unity.inputsystem",
            "com.unity.timeline",
            "com.unity.addressables",
            "com.unity.localization",
            "com.unity.2d.sprite",
            "com.unity.2d.animation",
            "com.unity.2d.psdimporter",
            "com.unity.ai.navigation",
            "com.unity.ugui",
            "com.unity.test-framework",
            "com.unity.shadergraph",
            "com.unity.splines",
            "com.unity.probuilder",
            "dev.yarnspinner.unity",
        };

        static AddAndRemoveRequest _request;
        static double _start;

        public static void Install()
        {
            EnsureOpenUpmRegistry();
            Debug.Log("[OWSBG] Requesting packages: " + string.Join(", ", Packages));
            _request = Client.AddAndRemove(Packages, Array.Empty<string>());
            _start = EditorApplication.timeSinceStartup;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (_request == null) return;
            if (!_request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup - _start > 900)
                {
                    Debug.LogError("[OWSBG] Package install timed out after 15 minutes.");
                    EditorApplication.Exit(2);
                }
                return;
            }
            EditorApplication.update -= Poll;
            if (_request.Status == StatusCode.Success)
            {
                foreach (var p in _request.Result)
                    Debug.Log("[OWSBG] Resolved " + p.name + "@" + p.version);
                AssetDatabase.SaveAssets();
                Debug.Log("[OWSBG] Package install complete.");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError("[OWSBG] Package install failed: " + (_request.Error != null ? _request.Error.message : "unknown"));
                EditorApplication.Exit(1);
            }
        }

        // Scoped registries have no Client API; the manifest is the only place they live.
        static void EnsureOpenUpmRegistry()
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Packages", "manifest.json");
            var json = File.ReadAllText(path);
            if (json.Contains("package.openupm.com")) return;
            const string registry =
                "  \"scopedRegistries\": [\n" +
                "    {\n" +
                "      \"name\": \"OpenUPM\",\n" +
                "      \"url\": \"https://package.openupm.com\",\n" +
                "      \"scopes\": [ \"dev.yarnspinner\" ]\n" +
                "    }\n" +
                "  ],\n";
            var idx = json.IndexOf("\"dependencies\"", StringComparison.Ordinal);
            json = json.Insert(idx, registry);
            File.WriteAllText(path, json);
            Debug.Log("[OWSBG] Added OpenUPM scoped registry for Yarn Spinner.");
        }
    }
}
