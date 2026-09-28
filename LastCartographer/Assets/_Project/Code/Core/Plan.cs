using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OWSBG.Core
{
    /// <summary>
    /// The production plan as data (PRO-02): its rows read from <c>docs/02-production-plan.md</c>, one per task, with
    /// the id, the section it sits in, its status mark, its milestone, its references and what it comes after; and
    /// the milestones from §2. The tracker is made from these (<c>tools/tracker.ps1</c>): one issue per row, titled
    /// by its id, in its milestone, labelled by its area. The plan stays the source; the tracker follows it.
    /// </summary>
    public static class Plan
    {
        public const string RelativePath = "docs/02-production-plan.md";
        public enum Status { Todo, InProgress, Done }

        public sealed class Row
        {
            public string Id;
            public BugBar.Area Area;
            public Status Status;
            /// <summary>The whole task cell, as written.</summary>
            public string Task;
            /// <summary>"M2", or the first of a range "M2–M3".</summary>
            public string Milestone;
            /// <summary>The range's end when the row spans milestones, else the same as Milestone.</summary>
            public string MilestoneEnd;
            public string Ref;
            public string[] After = Array.Empty<string>();
            public int Line;

            /// <summary>The task's first clause, for an issue's title: up to the first "; v1 in" note or colon, at most eighty characters.</summary>
            public string Title
            {
                get
                {
                    string t = Task;
                    int cut = t.IndexOf("; v1", StringComparison.Ordinal);
                    if (cut > 0) t = t.Substring(0, cut);
                    int paren = t.IndexOf(" (", StringComparison.Ordinal);
                    if (paren > 20) t = t.Substring(0, paren);
                    t = t.Trim().TrimEnd(';', ':', ',');
                    if (t.Length > 80)
                    {
                        int sp = t.LastIndexOf(' ', 77);
                        t = (sp > 40 ? t.Substring(0, sp) : t.Substring(0, 77)).TrimEnd(',', ';', ':') + "…";
                    }
                    return t;
                }
            }

            public string IssueTitle => "[" + Id + "] " + Title;
            public override string ToString() => Id + " " + Title;
        }

        public sealed class Milestone
        {
            public string Id, Name, Exit, Length;
        }

        public sealed class Document
        {
            public List<Row> Rows = new List<Row>();
            public List<Milestone> Milestones = new List<Milestone>();
            public Row Find(string id) => Rows.Find(r => r.Id == id);
        }

        static readonly Regex Section = new Regex(@"^###\s+3\.\d+\s+.*\(([A-Z]{3})\)\s*$");
        static readonly Regex RowLine = new Regex(@"^\|\s*([A-Z]{3}-\d{2})\s+`\[(.)\]`\s*\|(.*)\|\s*$");
        static readonly Regex MilestoneLine = new Regex(@"^\|\s*\*\*(M\d)\*\*\s*\|\s*\*\*(.+?)\*\*\s*\|(.*?)\|(.*?)\|\s*$");

        public static Document Parse(string markdown)
        {
            var doc = new Document();
            BugBar.Area? area = null;
            var lines = markdown.Replace("\r", "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                var sec = Section.Match(line);
                if (sec.Success) { area = Enum.TryParse<BugBar.Area>(sec.Groups[1].Value, out var a) ? a : (BugBar.Area?)null; continue; }
                var ms = MilestoneLine.Match(line);
                if (ms.Success)
                {
                    doc.Milestones.Add(new Milestone { Id = ms.Groups[1].Value, Name = ms.Groups[2].Value.Trim(), Exit = ms.Groups[3].Value.Trim(), Length = ms.Groups[4].Value.Trim() });
                    continue;
                }
                var m = RowLine.Match(line);
                if (!m.Success) continue;
                if (area == null) throw new FormatException("line " + (i + 1) + ": a task row outside any section");
                var cells = m.Groups[3].Value.Split('|').Select(c => c.Trim()).ToList();
                // NAR..AUD: Task | M | Ref | after.  PRO: Task | M | after.
                if (cells.Count < 3) throw new FormatException("line " + (i + 1) + ": too few cells");
                var row = new Row
                {
                    Id = m.Groups[1].Value,
                    Area = area.Value,
                    Status = m.Groups[2].Value switch { "x" => Status.Done, "~" => Status.InProgress, _ => Status.Todo },
                    Task = cells[0],
                    Line = i + 1,
                };
                string mcell = cells[1];
                var range = Regex.Match(mcell, @"^(M\d)\s*[–\-]\s*(M\d)$");
                if (range.Success) { row.Milestone = range.Groups[1].Value; row.MilestoneEnd = range.Groups[2].Value; }
                else { row.Milestone = mcell; row.MilestoneEnd = mcell; }
                string afterCell;
                if (cells.Count >= 4) { row.Ref = cells[2]; afterCell = cells[3]; }
                else { row.Ref = ""; afterCell = cells[2]; }
                row.After = afterCell == "—" || afterCell.Length == 0 ? Array.Empty<string>() : Regex.Split(afterCell, @",\s*").Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                if (!string.Equals(row.Id.Substring(0, 3), area.Value.ToString(), StringComparison.Ordinal))
                    throw new FormatException("line " + (i + 1) + ": " + row.Id + " sits in the " + area + " section");
                doc.Rows.Add(row);
            }
            return doc;
        }

        public static Document Load(string path) => Parse(File.ReadAllText(path));

        /// <summary>The ids an "after" cell may name besides a row: none, everything, a milestone's exit, a range.</summary>
        public static bool IsKnownDependency(Document doc, string after)
        {
            if (after == "all" || after == "Engine" || after.EndsWith(" exit", StringComparison.Ordinal)) return true;
            if (doc.Find(after) != null) return true;
            var all = Regex.Match(after, @"^([A-Z]{3})-all$");
            if (all.Success) return doc.Rows.Any(r => r.Id.StartsWith(all.Groups[1].Value + "-", StringComparison.Ordinal));
            var range = Regex.Match(after, @"^([A-Z]{3})-(\d{2})\.\.(\d{2})$");
            if (range.Success)
            {
                int a = int.Parse(range.Groups[2].Value), b = int.Parse(range.Groups[3].Value);
                for (int n = a; n <= b; n++) if (doc.Find(range.Groups[1].Value + "-" + n.ToString("00")) == null) return false;
                return a <= b;
            }
            return false;
        }

        /// <summary>The tracker's JSON: rows and milestones, for tools/tracker.ps1.</summary>
        [Serializable] public sealed class Export
        {
            [Serializable] public sealed class RowOut { public string id, area, status, title, task, milestone, milestoneEnd, @ref, issueTitle; public string[] after; public int line; }
            [Serializable] public sealed class MilestoneOut { public string id, name, exit, length; }
            public List<RowOut> rows = new List<RowOut>();
            public List<MilestoneOut> milestones = new List<MilestoneOut>();
        }

        public static Export ToExport(Document doc)
        {
            var e = new Export();
            foreach (var m in doc.Milestones) e.milestones.Add(new Export.MilestoneOut { id = m.Id, name = m.Name, exit = m.Exit, length = m.Length });
            foreach (var r in doc.Rows) e.rows.Add(new Export.RowOut
            {
                id = r.Id, area = BugBar.Label(r.Area), status = r.Status.ToString(), title = r.Title, task = r.Task,
                milestone = r.Milestone, milestoneEnd = r.MilestoneEnd, @ref = r.Ref, issueTitle = r.IssueTitle, after = r.After, line = r.Line,
            });
            return e;
        }

        public static string ToJson(Document doc) => UnityEngine.JsonUtility.ToJson(ToExport(doc), true);
    }
}
