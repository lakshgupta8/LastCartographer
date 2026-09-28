using System.IO;
using System.Text;

namespace OWSBG.Build
{
    /// <summary>
    /// Steam's build scripts for SteamPipe (PRG-25): an app build that names one depot, and that depot's content, the
    /// whole Windows build folder except what must not ship (debug symbols, Burst's and IL2CPP's leftovers).
    /// <c>steamcmd +login &lt;user&gt; +run_app_build &lt;dir&gt;/app_build_&lt;app&gt;.vdf +quit</c> uploads it
    /// (<c>tools/steam-upload.ps1</c>). Nothing here holds credentials.
    /// </summary>
    public static class SteamDepot
    {
        public static readonly string[] Excluded =
        {
            "*.pdb",
            "*_BurstDebugInformation_DoNotShip*",
            "*_BackUpThisFolder_ButDontShipItWithYourGame*",
        };

        public static string AppBuildFile(string appId) => "app_build_" + appId + ".vdf";
        public static string DepotBuildFile(string depotId) => "depot_build_" + depotId + ".vdf";

        /// <summary>
        /// The app build. <paramref name="branch"/> is the beta branch it goes live on ("" leaves it unset: set it live
        /// by hand in Steamworks, as the default branch needs).
        /// </summary>
        public static string AppBuild(string appId, string depotId, string contentRoot, string description, string branch)
        {
            var sb = new StringBuilder();
            sb.Append("\"AppBuild\"\n{\n");
            Pair(sb, 1, "AppID", appId);
            Pair(sb, 1, "Desc", description);
            Pair(sb, 1, "ContentRoot", contentRoot);
            Pair(sb, 1, "BuildOutput", "output");
            if (!string.IsNullOrEmpty(branch) && branch != "default") Pair(sb, 1, "SetLive", branch);
            Pair(sb, 1, "Preview", "0");
            sb.Append("\t\"Depots\"\n\t{\n");
            Pair(sb, 2, depotId, DepotBuildFile(depotId));
            sb.Append("\t}\n}\n");
            return sb.ToString();
        }

        public static string DepotBuild(string depotId, string contentRoot)
        {
            var sb = new StringBuilder();
            sb.Append("\"DepotBuild\"\n{\n");
            Pair(sb, 1, "DepotID", depotId);
            Pair(sb, 1, "ContentRoot", contentRoot);
            sb.Append("\t\"FileMapping\"\n\t{\n");
            Pair(sb, 2, "LocalPath", "*");
            Pair(sb, 2, "DepotPath", ".");
            Pair(sb, 2, "Recursive", "1");
            sb.Append("\t}\n");
            foreach (var x in Excluded) Pair(sb, 1, "FileExclusion", x);
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>Write both scripts to <paramref name="dir"/>. Returns the app build's path.</summary>
        public static string Write(string dir, string appId, string depotId, string contentRoot, string description, string branch)
        {
            Directory.CreateDirectory(dir);
            contentRoot = contentRoot.Replace('\\', '/');
            var app = Path.Combine(dir, AppBuildFile(appId));
            File.WriteAllText(app, AppBuild(appId, depotId, contentRoot, description, branch));
            File.WriteAllText(Path.Combine(dir, DepotBuildFile(depotId)), DepotBuild(depotId, contentRoot));
            return app;
        }

        static void Pair(StringBuilder sb, int indent, string key, string value)
        {
            sb.Append('\t', indent).Append(Quote(key)).Append(' ').Append(Quote(value)).Append('\n');
        }

        /// <summary>A VDF string: quotes and backslashes escaped.</summary>
        public static string Quote(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
