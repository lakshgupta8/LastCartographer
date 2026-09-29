using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OWSBG.Core;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OWSBG.Build
{
    /// <summary>
    /// The Windows build (PRG-25). One entry point for the menu, a headless editor and CI (GameCI's unity-builder calls
    /// <see cref="BuildFromCommandLine"/> as its buildMethod):
    /// <list type="number">
    /// <item>resolve the version and commit (arguments, then CI's environment, then git);</item>
    /// <item>stamp them into <c>build_info.json</c> and the player's bundle version;</item>
    /// <item>build the Addressables content (every room is a bundle);</item>
    /// <item>build the 64-bit Windows player from Build Settings' scenes;</item>
    /// <item>write <c>build_info.json</c> beside the exe, and Steam's depot scripts when an app id is given;</item>
    /// <item>put the checked-in "dev" stamp back, whatever happened.</item>
    /// </list>
    /// A failed build exits the editor with 1, so CI and scripts see it.
    /// </summary>
    public static class GameBuild
    {
        public const string ExeName = "LastCartographer.exe";
        public const string BuildInfoAsset = "Assets/_Project/Settings/Build/Resources/build_info.json";
        public const string DefaultOutput = "Builds/Windows";

        public sealed class Options
        {
            public string OutputDir = DefaultOutput;
            public string Version;
            public string Commit;
            public bool Development;
            public bool SkipContent;
            /// <summary>Steam app and depot ids: when both are set, the depot scripts are written beside the build.</summary>
            public string SteamAppId, SteamDepotId, SteamBranch;
        }

        [MenuItem("OWSBG/Build/Windows (release)")]
        static void MenuRelease() => Build(Resolve(new Options()));

        [MenuItem("OWSBG/Build/Windows (development)")]
        static void MenuDevelopment() => Build(Resolve(new Options { Development = true, OutputDir = DefaultOutput + "-dev" }));

        /// <summary>
        /// <c>-executeMethod OWSBG.Build.GameBuild.BuildFromCommandLine</c>, reading <c>-buildOutput dir</c> (or GameCI's
        /// <c>-customBuildPath</c>), <c>-buildVersion v</c>, <c>-development</c>, <c>-skipContent</c>, and
        /// <c>-steamAppId</c> / <c>-steamDepotId</c> / <c>-steamBranch</c>. Exits the editor with the result.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            int code;
            try
            {
                var report = Build(Resolve(FromArgs(Environment.GetCommandLineArgs())));
                code = report.summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[OWSBG] build failed: " + e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        public static Options FromArgs(string[] args)
        {
            var o = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                string next = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i])
                {
                    case "-buildOutput": o.OutputDir = next; i++; break;
                    case "-customBuildPath":
                        // GameCI passes the player's full path; we name the exe ourselves.
                        o.OutputDir = next != null && next.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(next) : next;
                        i++; break;
                    case "-buildVersion": o.Version = next; i++; break;
                    case "-development": o.Development = true; break;
                    case "-skipContent": o.SkipContent = true; break;
                    case "-steamAppId": o.SteamAppId = next; i++; break;
                    case "-steamDepotId": o.SteamDepotId = next; i++; break;
                    case "-steamBranch": o.SteamBranch = next; i++; break;
                }
            }
            return o;
        }

        /// <summary>Fill what the arguments left out: the version and commit from CI's environment, else git.</summary>
        public static Options Resolve(Options o)
        {
            if (string.IsNullOrEmpty(o.OutputDir)) o.OutputDir = DefaultOutput;
            if (string.IsNullOrEmpty(o.Version)) o.Version = Environment.GetEnvironmentVariable("OWSBG_VERSION");
            if (string.IsNullOrEmpty(o.Version)) o.Version = Git("describe --tags --always --dirty");
            o.Version = BuildVersion.Clean(o.Version);
            if (string.IsNullOrEmpty(o.Commit))
            {
                var sha = Environment.GetEnvironmentVariable("GITHUB_SHA");
                o.Commit = !string.IsNullOrEmpty(sha) ? sha.Substring(0, Math.Min(7, sha.Length)) : Git("rev-parse --short HEAD");
            }
            return o;
        }

        public static string[] Scenes() => EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();

        public static BuildReport Build(Options o)
        {
            var scenes = Scenes();
            if (scenes.Length == 0) throw new InvalidOperationException("no scenes in Build Settings");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

            string built = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string json = BuildInfo.ToJson(o.Version, o.Commit, built, o.Development);
            string previousVersion = PlayerSettings.bundleVersion;
            string previousInfo = File.ReadAllText(BuildInfoAsset);
            Debug.Log("[OWSBG] building " + o.Version + " (" + o.Commit + ")" + (o.Development ? ", development" : "") + " to " + o.OutputDir);
            try
            {
                PlayerSettings.bundleVersion = o.Version;
                File.WriteAllText(BuildInfoAsset, json);
                AssetDatabase.ImportAsset(BuildInfoAsset, ImportAssetOptions.ForceSynchronousImport);

                if (!o.SkipContent) BuildContent();

                Directory.CreateDirectory(o.OutputDir);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Path.Combine(o.OutputDir, ExeName),
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = o.Development ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None,
                });
                var s = report.summary;
                Debug.Log("[OWSBG] build " + s.result + " in " + s.totalTime.TotalSeconds.ToString("0") + " s: " +
                          (s.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, " + s.totalErrors + " errors, " + s.totalWarnings + " warnings");
                // GameCI's CLI judges a build by its log: this block, as its own build method prints it (the eighth run).
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", ResultsBlock(s));
                if (s.result != BuildResult.Succeeded) return report;

                File.WriteAllText(Path.Combine(o.OutputDir, "build_info.json"), json);
                if (!string.IsNullOrEmpty(o.SteamAppId) && !string.IsNullOrEmpty(o.SteamDepotId))
                {
                    var steamDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(o.OutputDir)) ?? ".", "steam");
                    SteamDepot.Write(steamDir, o.SteamAppId, o.SteamDepotId, Path.GetFullPath(o.OutputDir), "The Last Cartographer " + o.Label(), o.SteamBranch);
                    Debug.Log("[OWSBG] Steam depot scripts in " + steamDir);
                }
                return report;
            }
            finally
            {
                PlayerSettings.bundleVersion = previousVersion;
                File.WriteAllText(BuildInfoAsset, previousInfo);
                AssetDatabase.ImportAsset(BuildInfoAsset, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        /// <summary>
        /// GameCI's build summary, the one its own build method prints and its CLI reads to decide a build passed
        /// (a "# Build results #" block down to a "Size:" line, with "Errors: 0"). A failed build never reads as
        /// passed: it reports at least one error.
        /// </summary>
        public static string ResultsBlock(BuildSummary s) =>
            ResultsBlock(s.result == BuildResult.Succeeded, s.totalTime, s.totalWarnings, s.totalErrors, s.totalSize);

        public static string ResultsBlock(bool succeeded, TimeSpan time, int warnings, int errorCount, ulong size)
        {
            int errors = succeeded ? errorCount : Math.Max(1, errorCount);
            return string.Join("\n", new[]
            {
                "###########################",
                "#      Build results      #",
                "###########################",
                "",
                "Duration: " + time.ToString(@"hh\:mm\:ss"),
                "Warnings: " + warnings,
                "Errors: " + errors,
                "Size: " + size + " bytes (" + (size / (1024f * 1024f)).ToString("0.0") + " MB)",
            });
        }

        static string Label(this Options o) => string.IsNullOrEmpty(o.Commit) ? o.Version : o.Version + " (" + o.Commit + ")";

        /// <summary>Every room's bundle (PRG-07). Play mode reads the AssetDatabase and doesn't need it; a player does.</summary>
        public static void BuildContent()
        {
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException("Addressables build failed: " + result.Error);
            Debug.Log("[OWSBG] Addressables built in " + result.Duration.ToString("0.0") + " s");
        }

        static string Git(string args)
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo("git", args)
                {
                    RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                });
                if (p == null) return null;
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(10000);
                return p.ExitCode == 0 ? output : null;
            }
            catch { return null; }
        }
    }
}
