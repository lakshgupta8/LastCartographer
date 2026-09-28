using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The feel-test's gate from files (PRO-03): an answers CSV read by its header, sessions joined to testers by
    /// name, the report's verdict and tables, and the whole run over a folder.
    /// </summary>
    public class FeelGateTests
    {
        static string Row(string tester, int score, string blank = null)
        {
            var cells = FeelTest.Questions.Select(q => q.Id == blank ? "" : score.ToString()).ToList();
            return tester + "," + string.Join(",", cells);
        }

        [Test]
        public void AnswersAreReadByTheirHeader()
        {
            string csv = FeelGate.Header + "\n" + Row("pat", 4) + "\n# a note\n" + Row("sam", 5, "wall") + "\n\n";
            var answers = FeelGate.ParseAnswers(csv);
            Assert.AreEqual(2, answers.Count);
            Assert.AreEqual("pat", answers[0].Tester);
            Assert.AreEqual(FeelTest.Questions.Length, answers[0].Scores.Count);
            Assert.AreEqual(FeelTest.Questions.Length - 1, answers[1].Scores.Count, "a blank is a question not answered");
            Assert.IsFalse(answers[1].Scores.ContainsKey("wall"));
            Assert.AreEqual(5, answers[1].Scores["dash"]);

            // The columns in another order, and fewer of them.
            var few = FeelGate.ParseAnswers("Tester,hour,goes\r\nkim,3,5\r\n");
            Assert.AreEqual(2, few[0].Scores.Count); Assert.AreEqual(3, few[0].Scores["hour"]); Assert.AreEqual(5, few[0].Scores["goes"]);
            Assert.IsEmpty(FeelGate.ParseAnswers(""));

            Assert.Throws<FormatException>(() => FeelGate.ParseAnswers("name,goes\npat,4"), "the first column is tester");
            Assert.Throws<FormatException>(() => FeelGate.ParseAnswers("tester,goes,vibes\npat,4,4"), "no such question");
            Assert.Throws<FormatException>(() => FeelGate.ParseAnswers("tester,goes\npat,7"), "one to five");
            Assert.Throws<FormatException>(() => FeelGate.ParseAnswers("tester,goes\n,4"), "a row with no tester");
        }

        [Test]
        public void TheReportSaysWhyAndShowsEveryone()
        {
            var answers = FeelGate.ParseAnswers(FeelGate.Header + "\n" + string.Join("\n", Enumerable.Range(0, 5).Select(i => Row("t" + i, i < 3 ? 3 : 5, "wall"))));
            answers[0].Session = new FeelTest.Session { device = "Gamepad", seconds = 600f, actions = new[] { "Jump" }, presses = new[] { 100 }, acted = new[] { 96 }, dropped = new[] { 4 }, coyoteJumps = 2, falls = 1, hits = 8, jumpWaits = new[] { 90, 3, 3, 0, 0, 0, 0, 0 } };
            var gate = FeelTest.Gate(answers);
            var text = FeelGate.Report(gate, answers);
            StringAssert.Contains("**Not yet** with 5 testers, 1 with a session, 4.0% of presses dropped.", text);
            StringAssert.Contains("## Why not", text);
            StringAssert.Contains("\"She goes when I press.\" is at 3, under the pass mark of 4", text);
            StringAssert.Contains("| goes | She goes when I press. | 3 | 3 3 3 5 5 |", text);
            StringAssert.Contains("| wall | The wall holds and the wall-jump goes where I mean. | - |  |", text, "nobody answered the wall");
            StringAssert.Contains("| t0 | Gamepad | 10.0 | 100 | 4 (4%) | 6 | 2 | 1 | 8 |", text);
            StringAssert.Contains("| t1 | (no session) |", text);

            var good = FeelGate.ParseAnswers(FeelGate.Header + "\n" + string.Join("\n", Enumerable.Range(0, 5).Select(i => Row("t" + i, 4))));
            var passed = FeelGate.Report(FeelTest.Gate(good), good);
            StringAssert.Contains("**Passed** with 5 testers", passed);
            Assert.IsFalse(passed.Contains("## Why not"));
        }

        [Test]
        public void TheWholeRunReadsAFolderAndWritesTheReport()
        {
            string root = Path.Combine(Path.GetTempPath(), "owsbg-feelgate-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                string answers = Path.Combine(root, "answers.csv");
                File.WriteAllText(answers, FeelGate.Header + "\n" + string.Join("\n", Enumerable.Range(0, 5).Select(i => Row("t" + i, 4))));
                // Sessions: one named inside, one named only by its file, one that isn't a session at all.
                File.WriteAllText(Path.Combine(root, "a.json"), new FeelTest.Session { tester = "t0", actions = new[] { "Jump" }, presses = new[] { 50 }, acted = new[] { 50 }, dropped = new[] { 0 } }.ToJson());
                File.WriteAllText(Path.Combine(root, "t1.json"), new FeelTest.Session { actions = new[] { "Jump" }, presses = new[] { 50 }, acted = new[] { 30 }, dropped = new[] { 20 } }.ToJson());
                File.WriteAllText(Path.Combine(root, "notes.json"), "{ \"hello\": 1 }");
                var sessions = FeelGate.LoadSessions(root);
                Assert.AreEqual(2, sessions.Count);
                Assert.IsTrue(sessions.ContainsKey("t0") && sessions.ContainsKey("t1"));
                Assert.IsEmpty(FeelGate.LoadSessions(Path.Combine(root, "nowhere")));

                string report = Path.Combine(root, "out", "gate.md");
                var gate = FeelGate.Run(answers, root, report, out var text);
                Assert.IsFalse(gate.Passed, "twenty of a hundred presses dropped");
                StringAssert.Contains("20% of presses did nothing", gate.Reasons[0]);
                Assert.IsTrue(File.Exists(report));
                Assert.AreEqual(text, File.ReadAllText(report));
                StringAssert.Contains("| t1 |", text);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
