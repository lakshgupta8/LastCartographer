using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The first test round (PRO-04, docs/design/playtest-round1.md): the questions and the papers agree, the gate passes
    /// a good round and names every way to fail one, sessions round-trip and two sittings merge, the answers sheet is
    /// read by its header, and the report says what a team needs.
    /// </summary>
    public class PlaytestTests
    {
        static string Doc(string relative) => Path.GetFullPath(Path.Combine("..", relative));

        static Playtest.Session Played(string tester, bool reached = true, bool beat = true, float lost = 120f, bool closed = true)
        {
            var s = new Playtest.Session
            {
                tester = tester, seconds = 2400f, closed = closed, finished = beat, finishedAt = beat ? 2000f : -1f,
                rooms = new[] { "Greybox_Greyfold_Edge", "Greybox_Saltmarrow_A" }, roomSeconds = new[] { 300f, 2100f }, roomDeaths = new[] { 0, 3 },
                deaths = 3, longestLost = lost, longestLostIn = "Greybox_Saltmarrow_A",
            };
            if (reached)
            {
                s.bosses = new[] { Playtest.FinishBoss };
                s.bossAttempts = new[] { 2 };
                s.bossWon = new[] { beat };
            }
            return s;
        }

        static Playtest.Answers Answered(string tester, int score = 4, Playtest.Session session = null)
        {
            var a = new Playtest.Answers { Tester = tester, Session = session ?? Played(tester) };
            foreach (var q in Playtest.Questions) a.Scores[q.Id] = score;
            return a;
        }

        static List<Playtest.Answers> GoodRound() => Enumerable.Range(0, Playtest.MinTesters).Select(i => Answered("t" + i)).ToList();

        [Test]
        public void TheQuestionsAndThePapersAgree()
        {
            var ids = Playtest.Questions.Select(q => q.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
            Assert.AreEqual(12, ids.Count);
            Assert.Contains(Playtest.KeyQuestion, ids);
            foreach (var q in Playtest.Questions) Assert.IsFalse(string.IsNullOrEmpty(q.Text) || string.IsNullOrEmpty(q.Low) || string.IsNullOrEmpty(q.High), q.Id);

            var template = File.ReadAllLines(Doc("docs/playtest/round1/answers-template.csv")).Where(l => !l.StartsWith("#")).First();
            Assert.AreEqual(Playtest.Header, template, "the template's header is the gate's");
            var comments = string.Join("\n", File.ReadAllLines(Doc("docs/playtest/round1/answers-template.csv")).Where(l => l.StartsWith("#")));
            foreach (var q in Playtest.Questions) StringAssert.Contains(q.Text, comments, q.Id + " is spelled out for the facilitator");

            var brief = File.ReadAllText(Doc("docs/playtest/round1/tester-brief.md"));
            StringAssert.Contains("F12", brief, "the brief says how to report a bug");
            StringAssert.Contains("Lamp-Keeper", brief, "and where the test ends");
            StringAssert.Contains("What we record", brief, "and what is recorded");
            var sheet = File.ReadAllText(Doc("docs/playtest/round1/facilitator-sheet.md"));
            StringAssert.Contains("-playtest -tester", sheet, "the launch line");
            StringAssert.Contains("-continue", sheet, "and the restart after a crash");
            Assert.IsTrue(File.Exists(Doc("docs/playtest/round1/notes-template.md")));
            var protocol = File.ReadAllText(Doc("docs/design/playtest-round1.md"));
            foreach (var q in Playtest.Questions) StringAssert.Contains(q.Text, protocol, q.Id + " in the protocol");
        }

        [Test]
        public void TheGatePassesAGoodRoundAndNamesEveryWayToFailOne()
        {
            var good = Playtest.Gate(GoodRound());
            Assert.IsTrue(good.Passed, string.Join("; ", good.Reasons));
            Assert.AreEqual(1f, good.Reached); Assert.AreEqual(1f, good.Finished);

            void Fails(List<Playtest.Answers> round, string why)
            {
                var g = Playtest.Gate(round);
                Assert.IsFalse(g.Passed, why);
                Assert.IsTrue(g.Reasons.Any(r => r.Contains(why)), why + " not in: " + string.Join("; ", g.Reasons));
            }

            Fails(GoodRound().Take(Playtest.MinTesters - 1).ToList(), "testers");
            var low = GoodRound(); foreach (var a in low) a.Scores["atlas"] = 2;
            Fails(low, "under the floor");
            var soso = GoodRound(); foreach (var a in soso) a.Scores["look"] = 3;
            Fails(soso, "the round wants 3.5");
            var bored = GoodRound(); foreach (var a in bored) a.Scores[Playtest.KeyQuestion] = 3;
            Fails(bored, "the round wants 4");
            var far = GoodRound(); for (int i = 0; i < far.Count; i++) far[i].Session = Played(far[i].Tester, reached: i < 4, beat: i < 4);
            Fails(far, "reached the Lamp-Keeper");
            var lost = GoodRound(); foreach (var a in lost) a.Session = Played(a.Tester, lost: 600f);
            Fails(lost, "without finding a new room");
            var crashed = GoodRound(); crashed[0].Session.closed = false; crashed[1].Session.closed = false;
            Fails(crashed, "never closed");
            var unrecorded = GoodRound(); unrecorded[0].Session = null;
            Fails(unrecorded, "no session");

            var blank = GoodRound(); foreach (var a in blank) a.Scores.Remove("sound");
            Assert.IsTrue(Playtest.Gate(blank).Passed, "a question nobody could answer is left out");
            Assert.IsFalse(Playtest.Gate(blank).Medians.ContainsKey("sound"));
        }

        [Test]
        public void ASessionRoundTripsAndTwoSittingsMerge()
        {
            var s = Played("ana");
            var back = Playtest.Session.FromJson(s.ToJson());
            Assert.AreEqual("ana", back.tester);
            Assert.AreEqual(Playtest.Round, back.round);
            CollectionAssert.AreEqual(s.rooms, back.rooms);
            Assert.AreEqual(2, back.AttemptsAt(Playtest.FinishBoss));
            Assert.IsTrue(back.Beat(Playtest.FinishBoss));

            var first = Played("ana", reached: false, beat: false, lost: 200f, closed: false);   // the game closed under her
            var second = Played("ana", reached: true, beat: true, lost: 90f);
            second.rooms = new[] { "Greybox_Saltmarrow_A", "Greybox_Saltmarrow_Lighthouse" };
            second.roomSeconds = new[] { 100f, 400f }; second.roomDeaths = new[] { 1, 2 };
            var m = Playtest.Merge(first, second);
            Assert.AreEqual(4800f, m.seconds);
            Assert.IsFalse(m.closed, "one sitting never closed: it was a crash");
            CollectionAssert.AreEqual(new[] { "Greybox_Greyfold_Edge", "Greybox_Saltmarrow_A", "Greybox_Saltmarrow_Lighthouse" }, m.rooms);
            Assert.AreEqual(2200f, m.roomSeconds[1]); Assert.AreEqual(4, m.roomDeaths[1]);
            Assert.IsTrue(m.Beat(Playtest.FinishBoss)); Assert.AreEqual(2, m.AttemptsAt(Playtest.FinishBoss));
            Assert.AreEqual(2400f + 2000f, m.finishedAt, "her finish counted from the start of the first sitting");
            Assert.AreEqual(200f, m.longestLost);

            var dir = Path.Combine(Path.GetTempPath(), "owsbg-playtest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, "ana-1.json"), first.ToJson());
                File.WriteAllText(Path.Combine(dir, "ana-2.json"), second.ToJson());
                File.WriteAllText(Path.Combine(dir, "feel.json"), "{\"round\":\"feel\",\"tester\":\"ana\"}");
                var loaded = Playtest.LoadSessions(dir);
                Assert.AreEqual(1, loaded.Count, "one tester, two sittings; another test's file left alone");
                Assert.AreEqual(4800f, loaded["ana"].seconds);
            }
            finally { Directory.Delete(dir, true); }
        }

        [Test]
        public void TheAnswersSheetIsReadByItsHeader()
        {
            var csv = "# a comment\n" + "tester,again,start,read\n" + "ana,5,4,\n" + "ben,3,2,5\n";
            var a = Playtest.ParseAnswers(csv);
            Assert.AreEqual(2, a.Count);
            Assert.AreEqual(5, a[0].Scores["again"]); Assert.AreEqual(4, a[0].Scores["start"]);
            Assert.IsFalse(a[0].Scores.ContainsKey("read"), "a blank is a question not answered");
            Assert.AreEqual(5, a[1].Scores["read"]);
            Assert.Throws<FormatException>(() => Playtest.ParseAnswers("who,again\nana,5\n"));
            Assert.Throws<FormatException>(() => Playtest.ParseAnswers("tester,nope\nana,5\n"));
            Assert.Throws<FormatException>(() => Playtest.ParseAnswers("tester,again\nana,6\n"));
            Assert.Throws<FormatException>(() => Playtest.ParseAnswers("tester,again\n,4\n"));
        }

        [Test]
        public void TheReportSaysWhatTheTeamNeeds()
        {
            var round = GoodRound();
            round[0].Session = Played("t0", reached: true, beat: false, closed: false);
            round[1].Session = null;
            var gate = Playtest.Gate(round);
            var text = Playtest.Report(gate, round);
            StringAssert.Contains("Not yet", text);
            StringAssert.Contains("## Why not", text);
            foreach (var q in Playtest.Questions) StringAssert.Contains(q.Text, text);
            StringAssert.Contains("| t0 |", text);
            StringAssert.Contains("2 tries", text, "her attempts at the boss");
            StringAssert.Contains("**no**", text, "the session that never closed stands out");
            StringAssert.Contains("| t1 | no session", text);
            StringAssert.Contains("## Where they died", text);
            StringAssert.Contains("Greybox_Saltmarrow_A", text);

            var dir = Path.Combine(Path.GetTempPath(), "owsbg-gate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var csv = Playtest.Header + "\n" + string.Join("\n", Enumerable.Range(0, Playtest.MinTesters).Select(i => "t" + i + string.Concat(Enumerable.Repeat(",5", Playtest.Questions.Count))));
                File.WriteAllText(Path.Combine(dir, "answers.csv"), csv);
                for (int i = 0; i < Playtest.MinTesters; i++) File.WriteAllText(Path.Combine(dir, "t" + i + ".json"), Played("t" + i).ToJson());
                var run = Playtest.Run(Path.Combine(dir, "answers.csv"), dir, Path.Combine(dir, "gate.md"));
                Assert.IsTrue(run.Passed, string.Join("; ", run.Reasons));
                StringAssert.StartsWith("# Test round 1", File.ReadAllText(Path.Combine(dir, "gate.md")));
            }
            finally { Directory.Delete(dir, true); }
        }
    }
}
