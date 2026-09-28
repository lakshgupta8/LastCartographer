using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The feel-test (PRO-03, docs/design/feel-test.md): the course's stations in a line with every gap held to
    /// the controller's reach (the hops within it, the dash gap beyond it and within a dash), each station asking
    /// questions that exist, the gate's arithmetic, the session's JSON, and the button buffer's press, wait and drop.
    /// </summary>
    public class FeelTestTests
    {
        // The combat doc's controller numbers (WrenController's defaults).
        const float RunSpeed = 9f, TimeToApex = 0.38f, FallMultiplier = 1.3f, DashDistance = 5f;
        const int ApexHang = 3;

        [Test]
        public void TheCourseIsALineOfStationsEachAskingQuestionsThatExist()
        {
            var st = FeelTest.Stations;
            Assert.AreEqual(System.Enum.GetValues(typeof(FeelTest.Station)).Length, st.Length, "every station laid out once");
            for (int i = 0; i < st.Length; i++)
            {
                Assert.AreEqual((FeelTest.Station)i, st[i].Id, "in the enum's order");
                Assert.Less(st[i].From, st[i].To, st[i].Name);
                if (i > 0) Assert.AreEqual(st[i - 1].To, st[i].From, st[i].Name + " starts where the last ends");
                Assert.IsNotEmpty(st[i].Asks, st[i].Name + " asks something");
                Assert.IsNotEmpty(st[i].Questions, st[i].Name + " has questions");
                foreach (var q in st[i].Questions) Assert.IsNotNull(FeelTest.Questions.FirstOrDefault(x => x.Id == q), st[i].Name + " asks " + q + ", which exists");
                Assert.AreEqual(st[i].Id, FeelTest.StationAt(st[i].From + 0.5f));
            }
            Assert.AreEqual(FeelTest.Station.Run, FeelTest.StationAt(-3f), "before the start is the run's");
            Assert.AreEqual(FeelTest.Station.Targets, FeelTest.StationAt(999f));
            Assert.AreEqual(FeelTest.Questions.Length, FeelTest.Questions.Select(q => q.Id).Distinct().Count(), "no question twice");
            foreach (var q in FeelTest.Questions) { Assert.IsNotEmpty(q.Text, q.Id); Assert.IsNotEmpty(q.Low, q.Id); Assert.IsNotEmpty(q.High, q.Id); }
            var asked = st.SelectMany(s => s.Questions).ToHashSet();
            foreach (var q in FeelTest.Questions) if (q.Id != "hour") Assert.IsTrue(asked.Contains(q.Id), q.Id + " is asked at some station");
            Assert.AreEqual(Ability.Talonhold, FeelTest.Of(FeelTest.Station.Wall).Needs);
            Assert.AreEqual(Ability.Wingbeat, FeelTest.Of(FeelTest.Station.Dash).Needs);
        }

        [Test]
        public void EveryGapIsHeldToTheControllersReach()
        {
            float reach = FeelTest.JumpReach(RunSpeed, TimeToApex, FallMultiplier, ApexHang);
            Assert.That(reach, Is.InRange(6f, 8f), "a full jump at a run carries about seven units: " + reach);
            float last = 0f;
            foreach (var g in FeelTest.HopGaps) { Assert.Greater(g, last, "each hop wider than the last"); last = g; }
            Assert.Less(FeelTest.HopGaps.Last() + 0.3f, reach, "the widest hop is within reach, with a little to spare");
            Assert.Greater(FeelTest.HopGaps.Last(), reach - 1.5f, "and near it: the hard hop");
            Assert.Greater(FeelTest.DashGap, reach, "no jump crosses the dash gap");
            Assert.Less(FeelTest.DashGap, reach + DashDistance, "a jump and a dash do");
            Assert.Greater(FeelTest.CeilingHeight, 2.0f, "a tap's hop (2.0) fits under the roof");
            Assert.Less(FeelTest.CeilingHeight, 4.5f, "a full jump (4.5) does not");
            Assert.Less(FeelTest.StepRise, 2.0f, "each step is under the smallest hop");

            // The ledges: in a line, no two overlapping on the same height, the pits between the hops as wide as the gaps.
            var ledges = FeelTest.Ledges();
            for (int i = 0; i < ledges.Count; i++)
                for (int j = i + 1; j < ledges.Count; j++)
                {
                    var a = ledges[i]; var b = ledges[j];
                    if (!Mathf.Approximately(a.Y, b.Y)) continue;
                    Assert.IsTrue(a.X1 <= b.X0 + 1e-3f || b.X1 <= a.X0 + 1e-3f, a.Name + " and " + b.Name + " overlap");
                }
            for (int i = 0; i < FeelTest.HopGaps.Length; i++)
            {
                var from = ledges.First(l => l.Name == "Hop" + i);
                var to = ledges.First(l => l.Name == "Hop" + (i + 1));
                Assert.AreEqual(FeelTest.HopGaps[i], to.X0 - from.X1, 1e-3f, "gap " + i);
            }
            var dashFrom = ledges.First(l => l.Name == "DashFrom"); var dashTo = ledges.First(l => l.Name == "DashTo");
            Assert.AreEqual(FeelTest.DashGap, dashTo.X0 - dashFrom.X1, 1e-3f);
            Assert.AreEqual(FeelTest.ShaftHeight, ledges.First(l => l.Name == "ShaftHead").Y);
            var ladder = ledges.Where(l => l.Name.StartsWith("Ladder")).OrderBy(l => l.Y).ToList();
            Assert.AreEqual(3, ladder.Count, "a ladder path round the shaft for those without Talonhold");
            float y = 0f;
            foreach (var l in ladder) { Assert.Less(l.Y - y, 4.5f, l.Name + " is one jump up"); y = l.Y; }
            Assert.AreEqual(FeelTest.ShaftHeight, y, "and it reaches the head");
            foreach (var t in FeelTest.TargetXs) Assert.AreEqual(FeelTest.Station.Targets, FeelTest.StationAt(t));
            Assert.AreEqual(FeelTest.ShaftHeight + 0.5f, FeelTest.RestartOf(FeelTest.Station.Dash).y, "put back on the high floor");
            Assert.AreEqual(0.5f, FeelTest.RestartOf(FeelTest.Station.Hops).y);
        }

        static FeelTest.Answers Tester(string name, int score, params (string, int)[] except)
        {
            var a = new FeelTest.Answers { Tester = name };
            foreach (var q in FeelTest.Questions) a.Scores[q.Id] = score;
            foreach (var (id, s) in except) a.Scores[id] = s;
            return a;
        }

        [Test]
        public void TheGateWantsFiveTestersAtFourAndNoneUnderThree()
        {
            Assert.AreEqual(3f, FeelTest.Median(new[] { 5, 1, 3 }));
            Assert.AreEqual(3.5f, FeelTest.Median(new[] { 4, 3 }));
            Assert.AreEqual(0f, FeelTest.Median(new int[0]));

            var five = Enumerable.Range(0, 5).Select(i => Tester("t" + i, 4)).ToList();
            var r = FeelTest.Gate(five);
            Assert.IsTrue(r.Passed, string.Join("; ", r.Reasons));
            Assert.AreEqual(4f, r.Medians["goes"]);

            r = FeelTest.Gate(five.Take(4).ToList());
            Assert.IsFalse(r.Passed); StringAssert.Contains("4 testers", r.Reasons[0]);

            // One low score among five doesn't move a median of four; three do.
            var mixed = new List<FeelTest.Answers> { Tester("a", 5, ("dash", 2)), Tester("b", 4), Tester("c", 4), Tester("d", 4), Tester("e", 5) };
            Assert.IsTrue(FeelTest.Gate(mixed).Passed, "one tester's dash");
            mixed = new List<FeelTest.Answers> { Tester("a", 5, ("dash", 3)), Tester("b", 4, ("dash", 3)), Tester("c", 4, ("dash", 3)), Tester("d", 4), Tester("e", 5) };
            r = FeelTest.Gate(mixed);
            Assert.IsFalse(r.Passed); StringAssert.Contains("under the pass mark", r.Reasons[0]);
            mixed = new List<FeelTest.Answers> { Tester("a", 5, ("weight", 2)), Tester("b", 4, ("weight", 2)), Tester("c", 4, ("weight", 1)), Tester("d", 4), Tester("e", 5) };
            r = FeelTest.Gate(mixed);
            Assert.IsFalse(r.Passed); StringAssert.Contains("under the floor", r.Reasons[0]);

            // A question nobody answered (no Talonhold for the shaft) is left out, not failed.
            var noWall = five.Select(t => { var c = Tester(t.Tester, 4); c.Scores.Remove("wall"); return c; }).ToList();
            r = FeelTest.Gate(noWall);
            Assert.IsTrue(r.Passed); Assert.IsFalse(r.Medians.ContainsKey("wall"));

            // The presses that did nothing.
            var s = new FeelTest.Session { actions = new[] { "Jump", "Dash" }, presses = new[] { 100, 20 }, acted = new[] { 85, 20 }, dropped = new[] { 15, 0 } };
            Assert.AreEqual(0.125f, s.DroppedShare, 1e-4f);
            five[0].Session = s;
            r = FeelTest.Gate(five);
            Assert.IsFalse(r.Passed); StringAssert.Contains("13% of presses did nothing", r.Reasons[0]);
            five[1].Session = new FeelTest.Session { actions = new[] { "Jump" }, presses = new[] { 200 }, acted = new[] { 200 }, dropped = new[] { 0 } };
            r = FeelTest.Gate(five);
            Assert.IsTrue(r.Passed, "over every press of every tester: " + r.DroppedShare);
        }

        [Test]
        public void ASessionRoundTripsAsJson()
        {
            var s = new FeelTest.Session
            {
                version = "dev", device = "Gamepad", tester = "pat", seconds = 312.5f,
                actions = new[] { "Jump", "Attack" }, presses = new[] { 40, 12 }, acted = new[] { 37, 12 }, dropped = new[] { 3, 0 },
                jumpWaits = new[] { 30, 4, 2, 1, 0, 0, 0, 0 }, coyoteJumps = 3, dashes = 5, pogos = 7, landings = 36, falls = 2, hits = 9,
                stations = new[] { "Run", "Hops" }, stationSeconds = new[] { 40f, 90f }, stationFalls = new[] { 0, 2 },
            };
            var back = FeelTest.Session.FromJson(s.ToJson());
            Assert.AreEqual(40, back.Presses("Jump")); Assert.AreEqual(3, back.Dropped("Jump")); Assert.AreEqual(0, back.Presses("Dash"));
            Assert.AreEqual(7, back.BufferedJumps, "the jumps that waited");
            Assert.AreEqual(3, back.coyoteJumps); Assert.AreEqual(2, back.falls); Assert.AreEqual("pat", back.tester);
            Assert.AreEqual(3f / 52f, back.DroppedShare, 1e-4f);
        }

        [Test]
        public void ANamedBufferSaysWhenItIsPressedActedOnOrDropped()
        {
            var log = new List<string>();
            System.Action<string> pressed = a => log.Add("press " + a);
            System.Action<string, int> consumed = (a, w) => log.Add("act " + a + " after " + w);
            System.Action<string> dropped = a => log.Add("drop " + a);
            ButtonBuffer.Pressed += pressed; ButtonBuffer.Consumed += consumed; ButtonBuffer.Dropped += dropped;
            try
            {
                var jump = new ButtonBuffer(6, "Jump");
                var quiet = new ButtonBuffer(6);
                quiet.Press(); quiet.Tick(); quiet.Consume();
                Assert.IsEmpty(log, "a nameless buffer says nothing");
                jump.Press();
                Assert.IsTrue(jump.Consume());
                jump.Press(); jump.Tick(); jump.Tick(); jump.Tick();
                Assert.IsTrue(jump.Consume());
                Assert.IsFalse(jump.Consume(), "nothing left");
                jump.Press();
                for (int i = 0; i < 6; i++) jump.Tick();
                Assert.IsFalse(jump.Pending);
                jump.Tick();
                CollectionAssert.AreEqual(new[] { "press Jump", "act Jump after 0", "press Jump", "act Jump after 3", "press Jump", "drop Jump" }, log, "once each, the drop on the frame the window closes");
                Assert.AreEqual(6, jump.Window);
            }
            finally { ButtonBuffer.Pressed -= pressed; ButtonBuffer.Consumed -= consumed; ButtonBuffer.Dropped -= dropped; }
        }
    }
}
