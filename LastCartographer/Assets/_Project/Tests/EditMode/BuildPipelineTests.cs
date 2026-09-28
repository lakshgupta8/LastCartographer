using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Build;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The build pipeline's parts (PRG-25): versions from tags and <c>git describe</c>; the arguments a person, a script
    /// and GameCI pass; Steam's depot scripts; the checked-in "dev" stamp; Build Settings' scenes; and the CI workflow
    /// naming things that exist (the build method, the test assembly, the smoke script).
    /// </summary>
    public class BuildPipelineTests
    {
        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [Test]
        public void VersionsComeFromTags()
        {
            Assert.AreEqual("0.5.0", BuildVersion.Clean("v0.5.0"));
            Assert.AreEqual("0.5.0", BuildVersion.Clean("0.5.0"));
            Assert.AreEqual("0.5.0+3", BuildVersion.Clean("v0.5.0-3-g1a2b3c4"));
            Assert.AreEqual("0.5.0+3.dirty", BuildVersion.Clean("v0.5.0-3-g1a2b3c4-dirty"));
            Assert.AreEqual("0.5.0+dirty", BuildVersion.Clean("v0.5.0-dirty"));
            Assert.AreEqual("1.0.0-beta.2", BuildVersion.Clean("v1.0.0-beta.2"));
            Assert.AreEqual("1.0.0-beta.2+4", BuildVersion.Clean("v1.0.0-beta.2-4-gabcdef0"));
            Assert.AreEqual(BuildVersion.Untagged, BuildVersion.Clean("1a2b3c4"), "no tag yet");
            Assert.AreEqual(BuildVersion.Untagged, BuildVersion.Clean("1a2b3c4-dirty"));
            Assert.AreEqual(BuildVersion.Untagged, BuildVersion.Clean(null));
            Assert.AreEqual("0.1.23", BuildVersion.Clean("0.1.23"), "GameCI's semantic version");
            Assert.AreEqual("nightly-2026-09-28", BuildVersion.Clean("nightly 2026/09/28"));
        }

        [Test]
        public void TheArgumentsAPersonAScriptAndCiPass()
        {
            var o = GameBuild.FromArgs(new[] { "Unity.exe", "-batchmode", "-buildOutput", "Builds/Win", "-buildVersion", "v0.2.0", "-development", "-skipContent",
                                               "-steamAppId", "480", "-steamDepotId", "481", "-steamBranch", "beta" });
            Assert.AreEqual("Builds/Win", o.OutputDir);
            Assert.AreEqual("v0.2.0", o.Version);
            Assert.IsTrue(o.Development);
            Assert.IsTrue(o.SkipContent);
            Assert.AreEqual(("480", "481", "beta"), (o.SteamAppId, o.SteamDepotId, o.SteamBranch));

            // GameCI's unity-builder: the player's full path, and its own version.
            var ci = GameBuild.FromArgs(new[] { "-customBuildPath", "/github/workspace/build/StandaloneWindows64/LastCartographer.exe", "-buildVersion", "0.1.23" });
            Assert.AreEqual("/github/workspace/build/StandaloneWindows64", ci.OutputDir.Replace('\\', '/'));
            Assert.IsFalse(ci.Development);
            var resolved = GameBuild.Resolve(ci);
            Assert.AreEqual("0.1.23", resolved.Version);
            Assert.IsFalse(string.IsNullOrEmpty(GameBuild.Resolve(new GameBuild.Options()).Version), "git or the fallback names it");
        }

        [Test]
        public void SteamsDepotScriptsShipTheBuildFolder()
        {
            var app = SteamDepot.AppBuild("480", "481", "D:/Builds/Windows", "The Last Cartographer 0.5.0 (1a2b3c4)", "beta");
            StringAssert.StartsWith("\"AppBuild\"", app);
            StringAssert.Contains("\t\"AppID\" \"480\"", app);
            StringAssert.Contains("\t\"ContentRoot\" \"D:/Builds/Windows\"", app);
            StringAssert.Contains("\t\"SetLive\" \"beta\"", app);
            StringAssert.Contains("\t\t\"481\" \"depot_build_481.vdf\"", app);
            Assert.AreEqual(app.Count(c => c == '{'), app.Count(c => c == '}'), "balanced");
            StringAssert.DoesNotContain("SetLive", SteamDepot.AppBuild("480", "481", "x", "d", "default"), "the default branch is set live by hand");

            var depot = SteamDepot.DepotBuild("481", "D:/Builds/Windows");
            StringAssert.Contains("\t\"DepotID\" \"481\"", depot);
            StringAssert.Contains("\t\t\"LocalPath\" \"*\"", depot);
            StringAssert.Contains("\t\t\"Recursive\" \"1\"", depot);
            StringAssert.Contains("\t\"FileExclusion\" \"*.pdb\"", depot);
            StringAssert.Contains("DoNotShip", depot);
            Assert.AreEqual("\"say \\\"hi\\\" C:\\\\x\"", SteamDepot.Quote("say \"hi\" C:\\x"), "quotes and backslashes escaped");

            var dir = Path.Combine(Path.GetTempPath(), "owsbg-steam-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var path = SteamDepot.Write(dir, "480", "481", "D:\\Builds\\Windows", "desc", "");
                Assert.IsTrue(File.Exists(path));
                Assert.IsTrue(File.Exists(Path.Combine(dir, "depot_build_481.vdf")));
                StringAssert.Contains("\"D:/Builds/Windows\"", File.ReadAllText(path), "forward slashes");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        [Test]
        public void TheCheckedInStampIsDev()
        {
            BuildInfo.Reset();
            Assert.AreEqual("dev", BuildInfo.Version, "the build script puts it back after every build");
            Assert.IsTrue(BuildInfo.Development);
            Assert.AreEqual("dev", BuildInfo.Label);
            Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "..", GameBuild.BuildInfoAsset)));
            var json = BuildInfo.ToJson("0.5.0", "1a2b3c4", "2026-09-28T12:00:00Z", false);
            StringAssert.Contains("\"version\": \"0.5.0\"", json);
            StringAssert.Contains("\"development\": false", json);
        }

        [Test]
        public void TheBuildHasItsScenesAndItsRooms()
        {
            var scenes = GameBuild.Scenes();
            CollectionAssert.Contains(scenes, "Assets/_Project/Scenes/Persistent/Persistent.unity", "the persistent scene boots the build");
            var addressables = File.ReadAllText(Path.Combine(Application.dataPath, "AddressableAssetsData/AssetGroups/Rooms.asset"));
            StringAssert.Contains("Greybox_Saltmarrow_A", addressables, "the smoke test's second room is a bundle");
        }

        [Test]
        public void TheWorkflowNamesThingsThatExist()
        {
            var ci = File.ReadAllText(Path.Combine(RepoRoot, ".github/workflows/ci.yml"));
            var method = Regex.Match(ci, @"buildMethod: (\S+)").Groups[1].Value;
            Assert.AreEqual("OWSBG.Build.GameBuild.BuildFromCommandLine", method);
            Assert.IsNotNull(typeof(GameBuild).GetMethod("BuildFromCommandLine"), method);
            StringAssert.Contains("-assemblyNames OWSBG.Tests.PlayMode", ci);
            Assert.IsTrue(System.AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "OWSBG.Tests.PlayMode"), "the play-mode test assembly");
            StringAssert.Contains("unityVersion: ${{ env.UNITY_VERSION }}", ci);
            var version = File.ReadAllLines(Path.Combine(Application.dataPath, "../ProjectSettings/ProjectVersion.txt"))[0].Split(':')[1].Trim();
            StringAssert.Contains("UNITY_VERSION: " + version, ci, "CI builds with this project's editor");
            foreach (var script in new[] { "tools/smoke.ps1", "tools/build.ps1", "tools/steam-upload.ps1" })
                Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, script)), script);
            StringAssert.Contains("./tools/smoke.ps1", ci);
            StringAssert.Contains("-smoke", File.ReadAllText(Path.Combine(RepoRoot, "tools/smoke.ps1")));
            Assert.AreEqual("-smoke", OWSBG.Narrative.SmokeTest.Arg);
        }
    }
}
