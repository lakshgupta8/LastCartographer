using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OWSBG.Core
{
    /// <summary>
    /// The feel-test's verdict from files (PRO-03): an answers CSV (one row a tester: <c>tester,goes,stops,…</c>, one
    /// to five, blank for a question they couldn't answer) and a folder of session JSON files named by tester,
    /// through <see cref="FeelTest.Gate"/>, to a report in Markdown. <c>tools/feel-gate.ps1</c> runs it headless.
    /// </summary>
    public static class FeelGate
    {
        /// <summary>The CSV's header: tester, then every question id in order.</summary>
        public static string Header => "tester," + string.Join(",", FeelTest.Questions.Select(q => q.Id));

        /// <summary>
        /// Rows into answers. The header names the columns, so a sheet with the questions in another order still
        /// reads; an unknown column is an error, a missing one is left blank for everyone.
        /// </summary>
        public static List<FeelTest.Answers> ParseAnswers(string csv)
        {
            var answers = new List<FeelTest.Answers>();
            if (string.IsNullOrWhiteSpace(csv)) return answers;
            var lines = csv.Replace("\r", "").Split('\n').Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#")).ToList();
            var header = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
            if (header.Length == 0 || header[0] != "tester") throw new FormatException("the first column is 'tester'");
            var ids = FeelTest.Questions.Select(q => q.Id).ToHashSet();
            for (int c = 1; c < header.Length; c++)
                if (!ids.Contains(header[c])) throw new FormatException("no question called '" + header[c] + "'");
            for (int r = 1; r < lines.Count; r++)
            {
                var cells = lines[r].Split(',');
                var a = new FeelTest.Answers { Tester = cells[0].Trim() };
                if (a.Tester.Length == 0) throw new FormatException("row " + (r + 1) + " has no tester");
                for (int c = 1; c < header.Length && c < cells.Length; c++)
                {
                    var cell = cells[c].Trim();
                    if (cell.Length == 0) continue;
                    if (!int.TryParse(cell, NumberStyles.Integer, CultureInfo.InvariantCulture, out int score) || score < 1 || score > 5)
                        throw new FormatException(a.Tester + "'s " + header[c] + " is '" + cell + "', not one to five");
                    a.Scores[header[c]] = score;
                }
                answers.Add(a);
            }
            return answers;
        }

        /// <summary>Every *.json in the folder as a session, by the tester named inside it (else by file name).</summary>
        public static Dictionary<string, FeelTest.Session> LoadSessions(string dir)
        {
            var sessions = new Dictionary<string, FeelTest.Session>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return sessions;
            foreach (var path in Directory.GetFiles(dir, "*.json").OrderBy(p => p))
            {
                FeelTest.Session s;
                try { s = FeelTest.Session.FromJson(File.ReadAllText(path)); } catch { continue; }
                if (s == null || s.actions == null || s.actions.Length == 0) continue;
                string key = string.IsNullOrEmpty(s.tester) ? Path.GetFileNameWithoutExtension(path) : s.tester;
                sessions[key] = s;
            }
            return sessions;
        }

        /// <summary>The report as Markdown: the verdict, the reasons, each question's median, and each tester's numbers.</summary>
        public static string Report(FeelTest.GateReport gate, IReadOnlyList<FeelTest.Answers> answers)
        {
            var sb = new StringBuilder();
            sb.Append("# Feel-test gate\n\n");
            sb.Append("**").Append(gate.Passed ? "Passed" : "Not yet").Append("** with ").Append(answers.Count).Append(" testers");
            int withSessions = answers.Count(a => a.Session != null);
            sb.Append(", ").Append(withSessions).Append(" with a session");
            sb.Append(", ").Append((gate.DroppedShare * 100f).ToString("0.0", CultureInfo.InvariantCulture)).Append("% of presses dropped.\n\n");
            if (gate.Reasons.Count > 0)
            {
                sb.Append("## Why not\n\n");
                foreach (var r in gate.Reasons) sb.Append("- ").Append(r).Append('\n');
                sb.Append('\n');
            }
            sb.Append("## Questions\n\n| Id | Question | Median | Answers |\n|---|---|---|---|\n");
            foreach (var q in FeelTest.Questions)
            {
                var scores = answers.Where(a => a.Scores.ContainsKey(q.Id)).Select(a => a.Scores[q.Id]).ToList();
                string median = gate.Medians.TryGetValue(q.Id, out var m) ? m.ToString("0.#", CultureInfo.InvariantCulture) : "-";
                sb.Append("| ").Append(q.Id).Append(" | ").Append(q.Text).Append(" | ").Append(median).Append(" | ").Append(string.Join(" ", scores)).Append(" |\n");
            }
            sb.Append("\n## Testers\n\n| Tester | Device | Minutes | Presses | Dropped | Buffered jumps | Coyote | Falls | Hits |\n|---|---|---|---|---|---|---|---|---|\n");
            foreach (var a in answers)
            {
                var s = a.Session;
                if (s == null) { sb.Append("| ").Append(a.Tester).Append(" | (no session) | | | | | | | |\n"); continue; }
                sb.Append("| ").Append(a.Tester).Append(" | ").Append(s.device).Append(" | ").Append((s.seconds / 60f).ToString("0.0", CultureInfo.InvariantCulture))
                  .Append(" | ").Append(s.presses.Sum()).Append(" | ").Append(s.dropped.Sum()).Append(" (").Append((s.DroppedShare * 100f).ToString("0", CultureInfo.InvariantCulture)).Append("%)")
                  .Append(" | ").Append(s.BufferedJumps).Append(" | ").Append(s.coyoteJumps).Append(" | ").Append(s.falls).Append(" | ").Append(s.hits).Append(" |\n");
            }
            return sb.ToString();
        }

        /// <summary>The whole thing: read, join sessions to testers by name, gate, write the report. Returns the gate.</summary>
        public static FeelTest.GateReport Run(string answersPath, string sessionsDir, string reportPath, out string report)
        {
            var answers = ParseAnswers(File.ReadAllText(answersPath));
            var sessions = LoadSessions(sessionsDir);
            foreach (var a in answers) if (sessions.TryGetValue(a.Tester, out var s)) a.Session = s;
            var gate = FeelTest.Gate(answers);
            report = Report(gate, answers);
            if (!string.IsNullOrEmpty(reportPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)) ?? ".");
                File.WriteAllText(reportPath, report);
            }
            return gate;
        }
    }
}
