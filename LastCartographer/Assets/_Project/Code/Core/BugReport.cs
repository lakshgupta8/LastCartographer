using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// One bug report (PRO-07): the build, the machine, where Wren was, the world's flags, the save as it is, and the
    /// last lines of the log, written as a folder the player attaches to the issue. <see cref="Compose"/> is the
    /// report.md the issue template asks for; <see cref="Write"/> puts it beside the save and the screenshot.
    /// </summary>
    public sealed class BugReport
    {
        public const string FolderName = "BugReports";

        public string Version = "", Commit = "", Built = "";
        public bool Development;
        public DateTime When;
        public float Uptime;
        public string Kind = "pressed";                 // "pressed" (a key), "exception" (the game wrote it itself)
        public BugBar.Symptom Symptom = BugBar.Symptom.Unsorted;
        public string Room = "", Scene = "";
        public Vector2? Position;
        public int FlagsSet;
        public string SaveJson = "";
        public string Device = "", Os = "", Graphics = "", Resolution = "";
        public int Errors, Exceptions;
        public string FirstException;
        public string[] Log = Array.Empty<string>();

        /// <summary>The severity the report starts at, from what it says, or none until triage sorts it.</summary>
        public BugBar.Severity? Floor => BugBar.IsSorted(Symptom) ? BugBar.Floor(Symptom) : (BugBar.Severity?)null;

        /// <summary>Everything the game knows, now.</summary>
        public static BugReport Gather(WorldState world, string room, string scene, Vector2? position, BugBar.Symptom symptom, string kind)
        {
            var r = new BugReport
            {
                Version = BuildInfo.Version, Commit = BuildInfo.Commit, Built = BuildInfo.Built, Development = BuildInfo.Development,
                When = DateTime.Now, Uptime = Time.realtimeSinceStartup,
                Kind = kind, Symptom = symptom,
                Room = room ?? "", Scene = scene ?? "", Position = position,
                Device = SystemInfo.deviceModel, Os = SystemInfo.operatingSystem,
                Graphics = SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")",
                Resolution = Screen.width + "x" + Screen.height,
                Errors = LogTail.Errors, Exceptions = LogTail.Exceptions, FirstException = LogTail.FirstException,
                Log = new List<string>(LogTail.Lines).ToArray(),
            };
            if (world != null)
            {
                r.FlagsSet = world.Flags.Count;
                try { r.SaveJson = GameState.ToJson(world); } catch (Exception e) { r.SaveJson = "(no save: " + e.Message + ")"; }
            }
            return r;
        }

        /// <summary>"20260929-143012-pressed": one folder per report, in time order.</summary>
        public static string FolderNameFor(DateTime when, string kind) =>
            when.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + (string.IsNullOrEmpty(kind) ? "report" : kind);

        /// <summary>report.md: what the issue wants, in the order the template asks for it.</summary>
        public string Compose()
        {
            var sb = new StringBuilder();
            sb.Append("# Bug report\n\n");
            sb.Append("- **Build:** ").Append(string.IsNullOrEmpty(Commit) ? Version : Version + " · " + Commit);
            if (Development) sb.Append(" (development)");
            sb.Append('\n');
            if (!string.IsNullOrEmpty(Built)) sb.Append("- **Built:** ").Append(Built).Append('\n');
            sb.Append("- **When:** ").Append(When.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
              .Append(", ").Append(Uptime.ToString("0", CultureInfo.InvariantCulture)).Append(" s after start\n");
            sb.Append("- **How:** ").Append(Kind == "exception" ? "written by the game after an exception" : "F12").Append('\n');
            sb.Append("- **Symptom:** ").Append(Symptom).Append('\n');
            sb.Append("- **Severity:** ").Append(Floor.HasValue ? "at least " + Floor.Value : "to triage").Append('\n');
            sb.Append("- **Room:** ").Append(string.IsNullOrEmpty(Room) ? "(none)" : Room);
            if (!string.IsNullOrEmpty(Scene) && Scene != Room) sb.Append(" (scene ").Append(Scene).Append(')');
            sb.Append('\n');
            if (Position.HasValue)
                sb.Append("- **Wren at:** ").Append(Position.Value.x.ToString("0.00", CultureInfo.InvariantCulture)).Append(", ")
                  .Append(Position.Value.y.ToString("0.00", CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("- **World:** ").Append(FlagsSet).Append(" flags set; the save is beside this file\n");
            sb.Append("- **Machine:** ").Append(Os).Append("; ").Append(Device).Append("; ").Append(Graphics).Append("; ").Append(Resolution).Append('\n');
            sb.Append("- **Log:** ").Append(Errors).Append(" errors, ").Append(Exceptions).Append(" exceptions\n");
            if (!string.IsNullOrEmpty(FirstException))
                sb.Append("\n## First exception\n\n```\n").Append(FirstException.Trim()).Append("\n```\n");
            sb.Append("\n## Steps\n\n1. \n\n## Expected\n\n\n## Actual\n\n\n");
            sb.Append("## Last log lines\n\n```\n");
            int from = Math.Max(0, Log.Length - 40);
            for (int i = from; i < Log.Length; i++) sb.Append(Log[i]).Append('\n');
            sb.Append("```\n");
            return sb.ToString();
        }

        /// <summary>
        /// The report as a folder under <paramref name="root"/>/BugReports: report.md, save.json, log.txt and, when
        /// there is one, screenshot.png. Returns the folder.
        /// </summary>
        public string Write(string root, byte[] screenshotPng = null)
        {
            string dir = Path.Combine(root, FolderName, FolderNameFor(When, Kind));
            int n = 1;
            while (Directory.Exists(dir)) dir = Path.Combine(root, FolderName, FolderNameFor(When, Kind) + "-" + (++n));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "report.md"), Compose());
            if (!string.IsNullOrEmpty(SaveJson)) File.WriteAllText(Path.Combine(dir, "save.json"), SaveJson);
            File.WriteAllText(Path.Combine(dir, "log.txt"), string.Join("\n", Log));
            if (screenshotPng != null && screenshotPng.Length > 0) File.WriteAllBytes(Path.Combine(dir, "screenshot.png"), screenshotPng);
            return dir;
        }
    }
}
