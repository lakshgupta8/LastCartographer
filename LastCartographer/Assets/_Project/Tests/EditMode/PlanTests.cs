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
    /// The plan as data (PRO-02): every row of docs/02-production-plan.md parses, once, in its own section, in a real
    /// milestone, after rows that exist; the milestones read; titles fit an issue; the export carries it all; and
    /// the tracker script names the same labels as the bug bar.
    /// </summary>
    public class PlanTests
    {
        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [Test]
        public void EveryRowOfThePlanParsesInItsPlace()
        {
            var doc = Plan.Load(Path.Combine(RepoRoot, Plan.RelativePath));
            Assert.GreaterOrEqual(doc.Rows.Count, 120, "the plan's rows");
            Assert.AreEqual(doc.Rows.Count, doc.Rows.Select(r => r.Id).Distinct().Count(), "no id twice");
            foreach (var r in doc.Rows)
            {
                Assert.IsTrue(Regex.IsMatch(r.Id, @"^[A-Z]{3}-\d{2}$"), r.Id);
                Assert.AreEqual(r.Area.ToString(), r.Id.Substring(0, 3), r.Id + " is in its section");
                Assert.IsTrue(Regex.IsMatch(r.Milestone, @"^M[0-5]$"), r.Id + "'s milestone " + r.Milestone);
                Assert.IsTrue(Regex.IsMatch(r.MilestoneEnd, @"^M[0-5]$"), r.Id + "'s milestone end " + r.MilestoneEnd);
                Assert.LessOrEqual(string.CompareOrdinal(r.Milestone, r.MilestoneEnd), 0, r.Id + "'s range runs forward");
                Assert.IsNotEmpty(r.Task, r.Id);
                foreach (var a in r.After) Assert.IsTrue(Plan.IsKnownDependency(doc, a), r.Id + " comes after " + a + ", which the plan doesn't have");
                Assert.IsNotEmpty(r.Title, r.Id + " has a title");
                Assert.LessOrEqual(r.Title.Length, 81, r.Id + "'s title fits: " + r.Title);
                StringAssert.StartsWith("[" + r.Id + "] ", r.IssueTitle);
                Assert.Greater(r.Line, 0);
            }
            Assert.AreEqual(Enum.GetValues(typeof(BugBar.Area)).Length, doc.Rows.Select(r => r.Area).Distinct().Count(), "every area has rows");
            Assert.AreEqual(6, doc.Milestones.Count, "M0 to M5");
            for (int i = 0; i < 6; i++) { Assert.AreEqual("M" + i, doc.Milestones[i].Id); Assert.IsNotEmpty(doc.Milestones[i].Name); Assert.IsNotEmpty(doc.Milestones[i].Exit); StringAssert.Contains("weeks", doc.Milestones[i].Length); }
            var pro02 = doc.Find("PRO-02");
            Assert.IsNotNull(pro02);
            Assert.AreEqual(BugBar.Area.PRO, pro02.Area);
            Assert.AreEqual("M0", pro02.Milestone);
            CollectionAssert.AreEqual(new[] { "PRO-01" }, pro02.After);
            Assert.AreEqual(Plan.Status.Done, doc.Find("PRO-01").Status);
            Assert.AreNotEqual(Plan.Status.Todo, pro02.Status, "this row is in progress once the tracker exists");
            var spanning = doc.Rows.FirstOrDefault(r => r.Milestone != r.MilestoneEnd);
            Assert.IsNotNull(spanning, "some rows span milestones");
            Assert.IsTrue(doc.Rows.Any(r => r.Status == Plan.Status.InProgress) && doc.Rows.Any(r => r.Status == Plan.Status.Todo));
        }

        [Test]
        public void TheParserReadsTheShapesTheSectionsUse()
        {
            string md = string.Join("\n", new[]
            {
                "| **M0** | **Pre-production** | Bible signed off. | 8 weeks |",
                "### 3.1 Narrative (NAR)",
                "| ID | Task | M | Ref | after |",
                "| NAR-01 `[x]` | Story bible, second draft (framing, bosses) | M0 | SB-all | — |",
                "| NAR-02 `[~]` | Style guide; v1 in `docs/x.md`: the rules and the tests | M1–M2 | SB-11 | NAR-01, all |",
                "### 3.8 Production and QA (PRO)",
                "| ID | Task | M | after |",
                "| PRO-05 `[ ]` | Full-playthrough matrix: each ending, each region order, sequence breaks | M3 | NAR-01..02, NAR-all, M1 exit |",
            });
            var doc = Plan.Parse(md);
            Assert.AreEqual(3, doc.Rows.Count);
            Assert.AreEqual(1, doc.Milestones.Count);
            Assert.AreEqual("Pre-production", doc.Milestones[0].Name);
            var n1 = doc.Find("NAR-01");
            Assert.AreEqual(Plan.Status.Done, n1.Status); Assert.AreEqual("SB-all", n1.Ref); Assert.IsEmpty(n1.After);
            Assert.AreEqual("Story bible, second draft", n1.Title, "a parenthesis after twenty characters is dropped from the title");
            var n2 = doc.Find("NAR-02");
            Assert.AreEqual(Plan.Status.InProgress, n2.Status);
            Assert.AreEqual("M1", n2.Milestone); Assert.AreEqual("M2", n2.MilestoneEnd);
            Assert.AreEqual("Style guide", n2.Title, "the v1 note is not the title");
            CollectionAssert.AreEqual(new[] { "NAR-01", "all" }, n2.After);
            var p5 = doc.Find("PRO-05");
            Assert.AreEqual(Plan.Status.Todo, p5.Status); Assert.AreEqual("", p5.Ref); Assert.AreEqual(BugBar.Area.PRO, p5.Area);
            Assert.AreEqual("Full-playthrough matrix: each ending, each region order, sequence breaks", p5.Title);
            foreach (var a in p5.After) Assert.IsTrue(Plan.IsKnownDependency(doc, a), a);
            Assert.IsFalse(Plan.IsKnownDependency(doc, "NAR-09"));
            Assert.IsFalse(Plan.IsKnownDependency(doc, "NAR-01..09"), "a range with a hole");
            Assert.IsFalse(Plan.IsKnownDependency(doc, "XYZ-all"));

            var ex = Assert.Throws<FormatException>(() => Plan.Parse("| NAR-01 `[x]` | lost | M0 | — |"));
            StringAssert.Contains("outside any section", ex.Message);
            ex = Assert.Throws<FormatException>(() => Plan.Parse("### 3.1 Narrative (NAR)\n| PRO-01 `[x]` | wrong room | M0 | — |"));
            StringAssert.Contains("sits in the NAR section", ex.Message);

            var json = Plan.ToJson(doc);
            var back = JsonUtility.FromJson<Plan.Export>(json);
            Assert.AreEqual(3, back.rows.Count);
            Assert.AreEqual("[NAR-02] Style guide", back.rows[1].issueTitle);
            Assert.AreEqual("area:nar", back.rows[1].area);
            Assert.AreEqual("InProgress", back.rows[1].status);
            Assert.AreEqual("M0", back.milestones[0].id);
        }

        [Test]
        public void TheTrackerScriptUsesTheBarsLabelsAndTheWeeklyBuildIsScheduled()
        {
            var script = File.ReadAllText(Path.Combine(RepoRoot, "tools/tracker.ps1"));
            foreach (var a in BugBar.Areas) StringAssert.Contains("\"" + a.ToString().ToLowerInvariant() + "\"", script, a + " in the script's areas");
            StringAssert.Contains("\"plan\"", script);
            StringAssert.Contains("\"in-progress\"", script);
            StringAssert.Contains("OWSBG.Setup.TrackerSetup.Export", script);
            var ci = File.ReadAllText(Path.Combine(RepoRoot, ".github/workflows/ci.yml"));
            StringAssert.Contains("schedule:", ci, "the weekly build");
            StringAssert.Contains("cron:", ci);
        }
    }
}
