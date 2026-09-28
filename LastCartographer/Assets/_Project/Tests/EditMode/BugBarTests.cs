using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The bug bar (PRO-07, docs/design/bug-bar.md): a symptom's least severity, priority from severity and reach,
    /// each milestone's bar stricter than the last, the tracker's words the same in the code, the issue form and the
    /// triage script, the log tail's ring, and a report written whole.
    /// </summary>
    public class BugBarTests
    {
        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [Test]
        public void TheWorstSymptomsSetTheFloor()
        {
            Assert.AreEqual(BugBar.Severity.Blocker, BugBar.Floor(BugBar.Symptom.Crash));
            Assert.AreEqual(BugBar.Severity.Blocker, BugBar.Floor(BugBar.Symptom.SaveLost), "a lost save is the worst thing a game built on saves can do");
            Assert.AreEqual(BugBar.Severity.Blocker, BugBar.Floor(BugBar.Symptom.CantFinish));
            Assert.AreEqual(BugBar.Severity.Critical, BugBar.Floor(BugBar.Symptom.SoftLock));
            Assert.AreEqual(BugBar.Severity.Critical, BugBar.Floor(BugBar.Symptom.WrongEnding));
            Assert.AreEqual(BugBar.Severity.Major, BugBar.Floor(BugBar.Symptom.WrongState));
            Assert.AreEqual(BugBar.Severity.Major, BugBar.Floor(BugBar.Symptom.OverBudget));
            Assert.AreEqual(BugBar.Severity.Major, BugBar.Floor(BugBar.Symptom.NoRead));
            foreach (var s in new[] { BugBar.Symptom.Visual, BugBar.Symptom.Text, BugBar.Symptom.Audio, BugBar.Symptom.Feel })
                Assert.AreEqual(BugBar.Severity.Minor, BugBar.Floor(s), s.ToString());
            Assert.AreEqual(BugBar.Severity.Trivial, BugBar.Floor(BugBar.Symptom.Unsorted), "nothing said, nothing assumed");
            Assert.IsFalse(BugBar.IsSorted(BugBar.Symptom.Unsorted));
            foreach (BugBar.Severity s in Enum.GetValues(typeof(BugBar.Severity)))
            {
                Assert.IsNotEmpty(BugBar.Definition(s), s + " is defined");
                Assert.IsNotEmpty(BugBar.FixBy(s), s + " has a turnaround");
            }
        }

        [Test]
        public void ReachOrdersTheWork()
        {
            Assert.AreEqual(BugBar.Priority.P0, BugBar.PriorityOf(BugBar.Severity.Blocker, BugBar.Reach.Few), "a Blocker is first whoever meets it");
            Assert.AreEqual(BugBar.Priority.P0, BugBar.PriorityOf(BugBar.Severity.Critical, BugBar.Reach.Everyone));
            Assert.AreEqual(BugBar.Priority.P1, BugBar.PriorityOf(BugBar.Severity.Critical, BugBar.Reach.Some));
            Assert.AreEqual(BugBar.Priority.P1, BugBar.PriorityOf(BugBar.Severity.Major, BugBar.Reach.Most));
            Assert.AreEqual(BugBar.Priority.P2, BugBar.PriorityOf(BugBar.Severity.Major, BugBar.Reach.Few));
            Assert.AreEqual(BugBar.Priority.P2, BugBar.PriorityOf(BugBar.Severity.Minor, BugBar.Reach.Everyone), "a typo everyone reads");
            Assert.AreEqual(BugBar.Priority.P3, BugBar.PriorityOf(BugBar.Severity.Minor, BugBar.Reach.Some));
            Assert.AreEqual(BugBar.Priority.P3, BugBar.PriorityOf(BugBar.Severity.Trivial, BugBar.Reach.Everyone));
        }

        [Test]
        public void EachMilestonesBarIsStricterThanTheLast()
        {
            var beta = BugBar.Bar(BugBar.Milestone.Beta);
            var rc = BugBar.Bar(BugBar.Milestone.ReleaseCandidate);
            var release = BugBar.Bar(BugBar.Milestone.Release);
            for (int i = 0; i < beta.Length; i++)
            {
                Assert.LessOrEqual(rc[i], beta[i], (BugBar.Severity)i + ": release candidate no looser than beta");
                Assert.LessOrEqual(release[i], rc[i], (BugBar.Severity)i + ": release no looser than release candidate");
            }
            Assert.AreEqual(0, BugBar.MaxOpen(BugBar.Milestone.Beta, BugBar.Severity.Blocker), "no milestone carries a Blocker");
            Assert.AreEqual(0, BugBar.MaxOpen(BugBar.Milestone.ReleaseCandidate, BugBar.Severity.Critical));
            Assert.AreEqual(0, BugBar.MaxOpen(BugBar.Milestone.Release, BugBar.Severity.Major));
            Assert.AreEqual(int.MaxValue, BugBar.MaxOpen(BugBar.Milestone.Release, BugBar.Severity.Trivial), "trivial never holds a release");

            Assert.IsTrue(BugBar.Meets(new[] { 0, 3, 40, 100, 5 }, BugBar.Milestone.Beta));
            Assert.IsFalse(BugBar.Meets(new[] { 0, 4, 0, 0, 0 }, BugBar.Milestone.Beta));
            Assert.IsFalse(BugBar.Meets(new[] { 0, 1, 0, 0, 0 }, BugBar.Milestone.ReleaseCandidate));
            Assert.IsTrue(BugBar.Meets(new[] { 0, 0, 10, 0, 0 }, BugBar.Milestone.ReleaseCandidate));
            Assert.IsFalse(BugBar.Meets(new[] { 0, 0, 1, 0, 0 }, BugBar.Milestone.Release));
            Assert.IsTrue(BugBar.Meets(new[] { 0, 0, 0, 25, 999 }, BugBar.Milestone.Release));
            var why = BugBar.Check(new[] { 1, 0, 11, 26, 0 }, BugBar.Milestone.Release).ToList();
            Assert.AreEqual(3, why.Count, string.Join("; ", why));
            StringAssert.Contains("Blocker: 1 open, at most 0", why[0]);
            Assert.IsTrue(BugBar.Meets(new[] { 0 }, BugBar.Milestone.Release), "counts not given are none");
        }

        [Test]
        public void TheTrackersWordsAreTheSameEverywhere()
        {
            var labels = BugBar.Labels().ToList();
            Assert.AreEqual(labels.Count, labels.Select(l => l.Name).Distinct().Count(), "no label twice");
            foreach (var l in labels) { Assert.IsNotEmpty(l.Colour, l.Name); Assert.IsNotEmpty(l.Description, l.Name); }
            Assert.AreEqual("severity:blocker", BugBar.Label(BugBar.Severity.Blocker));
            Assert.AreEqual("area:cmb", BugBar.Label(BugBar.Area.CMB));
            Assert.IsTrue(BugBar.TryParseSeverity("Severity:Major", out var s) && s == BugBar.Severity.Major);
            Assert.IsFalse(BugBar.TryParseSeverity("area:cmb", out _));

            // The plan's sections are the areas.
            var plan = File.ReadAllText(Path.Combine(RepoRoot, "docs/02-production-plan.md"));
            foreach (var a in BugBar.Areas) StringAssert.Contains("(" + a + ")", plan, a + " is a section of the plan");

            // The issue form names every symptom, severity and area as the code does.
            var form = File.ReadAllText(Path.Combine(RepoRoot, ".github/ISSUE_TEMPLATE/bug_report.yml"));
            foreach (BugBar.Symptom sym in Enum.GetValues(typeof(BugBar.Symptom))) StringAssert.Contains("\"" + sym + ":", form, sym + " on the form");
            foreach (var sev in BugBar.Severities) StringAssert.Contains("- \"" + sev + "\"", form, sev + " on the form");
            foreach (var a in BugBar.Areas) StringAssert.Contains("\"" + a + ":", form, a + " on the form");
            StringAssert.Contains("labels: [\"triage\"]", form, "a new report starts in triage");
            StringAssert.Contains("F12", form, "the form says how to make a report");

            // The triage script carries the same labels, descriptions and bar.
            var script = File.ReadAllText(Path.Combine(RepoRoot, "tools/triage.ps1"));
            foreach (var l in labels)
            {
                StringAssert.Contains("\"" + l.Name + "\"", script, l.Name + " in the script");
                StringAssert.Contains("\"" + l.Colour + "\"", script, l.Name + "'s colour in the script");
                StringAssert.Contains(l.Description, script, l.Name + "'s line in the script");
            }
            var bars = new[] { ("beta", BugBar.Milestone.Beta), ("rc", BugBar.Milestone.ReleaseCandidate), ("release", BugBar.Milestone.Release) };
            foreach (var (name, m) in bars)
            {
                var match = Regex.Match(script, name + @"\s*=\s*@\(([^)]*)\)");
                Assert.IsTrue(match.Success, name + "'s bar in the script");
                var nums = match.Groups[1].Value.Split(',').Select(x => int.Parse(x.Trim())).ToArray();
                var bar = BugBar.Bar(m).Select(x => x == int.MaxValue ? -1 : x).ToArray();
                CollectionAssert.AreEqual(bar, nums, name + "'s bar is the code's");
            }
        }

        [Test]
        public void TheLogTailKeepsTheLastLinesAndCountsTheWorst()
        {
            LogTail.Clear();
            for (int i = 0; i < LogTail.Capacity + 10; i++) LogTail.Record(LogType.Log, "line " + i, "", i * 0.1f);
            Assert.AreEqual(LogTail.Capacity, LogTail.Lines.Count);
            StringAssert.StartsWith("1.000s   line 10", LogTail.Lines.First(), "the oldest ten have gone");
            Assert.AreEqual(0, LogTail.Errors);
            LogTail.Record(LogType.Error, "bad", "", 30f);
            LogTail.Record(LogType.Warning, "meh\nsecond line", "", 31f);
            LogTail.Record(LogType.Exception, "NullReferenceException: x", "Boss.Update () (at Boss.cs:10)\nnext", 32f);
            LogTail.Record(LogType.Exception, "again", "", 33f);
            Assert.AreEqual(1, LogTail.Errors);
            Assert.AreEqual(2, LogTail.Exceptions);
            StringAssert.StartsWith("NullReferenceException: x", LogTail.FirstException, "the first is the one kept");
            var text = LogTail.Text();
            StringAssert.Contains("31.000s W meh | second line", text, "one line each");
            StringAssert.Contains("32.000s X NullReferenceException: x @ Boss.Update () (at Boss.cs:10)", text);
            LogTail.Clear();
            Assert.AreEqual(0, LogTail.Lines.Count);
            Assert.IsNull(LogTail.FirstException);
        }

        [Test]
        public void AReportIsWrittenWhole()
        {
            LogTail.Clear();
            LogTail.Record(LogType.Log, "walked in", "", 1f);
            LogTail.Record(LogType.Exception, "boom", "At () (at X.cs:1)", 2f);
            var w = new WorldState();
            w.Set("test.flag", true);
            var r = BugReport.Gather(w, "Greybox_Saltmarrow_A", "Persistent", new Vector2(3.5f, -1.25f), BugBar.Symptom.SoftLock, "pressed");
            Assert.AreEqual(BugBar.Severity.Critical, r.Floor);
            var md = r.Compose();
            StringAssert.Contains("**Build:** " + BuildInfo.Label, md);
            StringAssert.Contains("**Symptom:** SoftLock", md);
            StringAssert.Contains("**Severity:** at least Critical", md);
            StringAssert.Contains("**Room:** Greybox_Saltmarrow_A (scene Persistent)", md);
            StringAssert.Contains("**Wren at:** 3.50, -1.25", md);
            StringAssert.Contains("1 flags set", md);
            StringAssert.Contains("0 errors, 1 exceptions", md);
            StringAssert.Contains("## First exception\n\n```\nboom\nAt () (at X.cs:1)\n```", md);
            StringAssert.Contains("2.000s X boom @ At () (at X.cs:1)", md, "the tail is in the report");
            StringAssert.Contains("## Steps", md, "room left for the player's words");

            var unsorted = BugReport.Gather(null, null, "Persistent", null, BugBar.Symptom.Unsorted, "pressed");
            Assert.IsNull(unsorted.Floor);
            StringAssert.Contains("**Severity:** to triage", unsorted.Compose());
            StringAssert.Contains("**Room:** (none)", unsorted.Compose());
            Assert.AreEqual("", unsorted.SaveJson, "no world, no save");

            string root = Path.Combine(Path.GetTempPath(), "owsbg-bugbar-" + Guid.NewGuid().ToString("N"));
            try
            {
                r.When = new DateTime(2026, 9, 29, 14, 30, 12);
                string dir = r.Write(root, new byte[] { 1, 2, 3 });
                Assert.AreEqual(Path.Combine(root, BugReport.FolderName, "20260929-143012-pressed"), dir);
                Assert.IsTrue(File.Exists(Path.Combine(dir, "report.md")));
                Assert.IsTrue(File.Exists(Path.Combine(dir, "save.json")), "the save goes with it");
                StringAssert.Contains("test.flag", File.ReadAllText(Path.Combine(dir, "save.json")));
                Assert.IsTrue(File.Exists(Path.Combine(dir, "log.txt")));
                Assert.AreEqual(3, File.ReadAllBytes(Path.Combine(dir, "screenshot.png")).Length);
                string again = r.Write(root);
                Assert.AreEqual(dir + "-2", again, "a second report in the same second sits beside the first");
                Assert.IsFalse(File.Exists(Path.Combine(again, "screenshot.png")), "none given, none written");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); LogTail.Clear(); }
        }
    }
}
