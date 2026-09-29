using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// PRO-06's target and gate (docs/design/performance.md): a card is classed against a GTX 1060, only a run on the
    /// target class or below at 1080p, over the whole route and within every budget, proves it; a faster card's run is
    /// projected, never a pass; the gate reads a folder of probe reports and writes its verdict.
    /// </summary>
    public class PerfTargetTests
    {
        static PerfReport Run(string gpu, float p95 = 2f, float transition = 12f, int rooms = PerfTarget.Rooms, string res = "1920x1080", bool rendering = true)
        {
            var r = new PerfReport { graphics = gpu + " (Direct3D12)", resolution = res, rendering = rendering };
            for (int i = 0; i < rooms; i++)
                r.rooms.Add(new PerfReport.Room { room = "Greybox_R" + i, frames = 300, avgMs = p95 * 0.8f, p50Ms = p95 * 0.7f, p95Ms = p95, p99Ms = p95 * 1.2f, maxMs = p95 * 2f });
            for (int i = 1; i < rooms; i++)
                r.transitions.Add(new PerfReport.Transition { from = "Greybox_R" + (i - 1), to = "Greybox_R" + i, ms = transition });
            return r;
        }

        [Test]
        public void CardsAreClassedAgainstAGtx1060()
        {
            Assert.AreEqual((1.00f, PerfTarget.GpuClass.Target), PerfTarget.Classify("NVIDIA GeForce GTX 1060 6GB"));
            Assert.AreEqual(PerfTarget.GpuClass.Target, PerfTarget.Classify("NVIDIA GeForce GTX 1060 3GB").cls, "the 3 GB card too");
            Assert.AreEqual(PerfTarget.GpuClass.Target, PerfTarget.Classify("Radeon RX 580 Series").cls, "AMD's equal");
            Assert.AreEqual(PerfTarget.GpuClass.Target, PerfTarget.Classify("NVIDIA GeForce GTX 970").cls);
            Assert.AreEqual(PerfTarget.GpuClass.Below, PerfTarget.Classify("NVIDIA GeForce GTX 1050 Ti").cls, "Ti before the plain 1050");
            Assert.AreEqual(0.62f, PerfTarget.Classify("NVIDIA GeForce GTX 1050 Ti").score);
            Assert.AreEqual(PerfTarget.GpuClass.Below, PerfTarget.Classify("Intel(R) Iris(R) Xe Graphics").cls);
            Assert.AreEqual(PerfTarget.GpuClass.Above, PerfTarget.Classify("NVIDIA GeForce GTX 1660 SUPER").cls);
            Assert.AreEqual(1.30f, PerfTarget.Classify("NVIDIA GeForce GTX 1660 SUPER").score, "the SUPER before the plain 1660");
            Assert.AreEqual(PerfTarget.GpuClass.Above, PerfTarget.Classify("NVIDIA GeForce RTX 3050 6GB Laptop GPU").cls, "this project's own machine");
            Assert.AreEqual(PerfTarget.GpuClass.Unknown, PerfTarget.Classify("Null Device").cls, "a batch run has no card");
            Assert.AreEqual(PerfTarget.GpuClass.Unknown, PerfTarget.Classify("").cls);
            foreach (var (pattern, score) in PerfTarget.Cards) Assert.That(score, Is.InRange(0.05f, 5f), pattern);
            Assert.AreEqual(PerfTarget.Cards.Length, PerfTarget.Cards.Select(c => c.pattern).Distinct().Count(), "one row a card");
            Assert.AreEqual(1.45f, PerfTarget.Classify("AMD Radeon RX 5600 XT").score, "the 5600 isn't read as a 560");
        }

        [Test]
        public void OnlyATargetClassRunProvesIt()
        {
            var v = PerfTarget.Judge(Run("NVIDIA GeForce GTX 1060 6GB"));
            Assert.IsTrue(v.Proves, string.Join("; ", v.Reasons));
            Assert.IsTrue(PerfTarget.Judge(Run("NVIDIA GeForce GTX 1050 Ti")).Proves, "a slower card within budget proves it too");

            var fast = PerfTarget.Judge(Run("NVIDIA GeForce RTX 3050 6GB Laptop GPU", p95: 1.5f));
            Assert.IsTrue(fast.WithinBudget, "within budget as measured");
            Assert.IsFalse(fast.Proves, "but a faster card can't prove the target");
            Assert.AreEqual(1.8f, fast.ProjectedP95, 1e-4f, "projected onto the target: 1.5 ms × 1.2");
            var hot = PerfTarget.Judge(Run("NVIDIA GeForce RTX 4090", p95: 5f));
            Assert.IsTrue(hot.Reasons.Any(x => x.Contains("projected p95")), "a fast card whose projection is over says so: " + string.Join("; ", hot.Reasons));

            Assert.IsFalse(PerfTarget.Judge(Run("NVIDIA GeForce GTX 1060 6GB", p95: 17.5f)).Proves, "over 16.7 ms");
            Assert.IsFalse(PerfTarget.Judge(Run("NVIDIA GeForce GTX 1060 6GB", transition: 140f)).Proves, "a transition over 100 ms");
            Assert.IsFalse(PerfTarget.Judge(Run("NVIDIA GeForce GTX 1060 6GB", rooms: 3)).Proves, "the whole route");
            Assert.IsFalse(PerfTarget.Judge(Run("NVIDIA GeForce GTX 1060 6GB", res: "1280x720")).Proves, "at 1080p");
            Assert.IsFalse(PerfTarget.Judge(Run("NVIDIA GeForce GTX 1060 6GB", rendering: false)).Proves, "rendered");
            Assert.IsFalse(PerfTarget.Judge(Run("Some Future Card 9000")).Proves, "an unknown card is added to the table first");

            var gc = Run("NVIDIA GeForce GTX 1060 6GB");
            gc.overBudget.Add("Greybox_R2: 1 collections while standing still");
            gc.overBudget.Add("Greybox_R1: p95 20.0 ms");   // the frame check is the judge's own
            var g = PerfTarget.Judge(gc);
            Assert.IsFalse(g.Proves, "the probe's garbage findings count");
            Assert.AreEqual(1, g.Reasons.Count, "and aren't counted twice: " + string.Join("; ", g.Reasons));
        }

        [Test]
        public void TheGateReadsAFolderOfRuns()
        {
            var dir = Path.Combine(Path.GetTempPath(), "owsbg-perf-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, "laptop.json"), Run("NVIDIA GeForce RTX 3050 6GB Laptop GPU").ToJson());
                File.WriteAllText(Path.Combine(dir, "notes.json"), "{ not a report");
                var gate = Path.Combine(dir, "gate.md");
                Assert.IsFalse(PerfTarget.Run(dir, gate, out var text), "a faster card alone: not yet");
                StringAssert.Contains("**Not yet**", text);
                StringAssert.Contains("laptop.json", text);
                StringAssert.Contains("projected onto the target", text);
                Assert.AreEqual(1, PerfTarget.Load(dir).Count, "the unreadable file is skipped");

                File.WriteAllText(Path.Combine(dir, "tester-1060.json"), Run("NVIDIA GeForce GTX 1060 6GB", p95: 9f).ToJson());
                Assert.IsTrue(PerfTarget.Run(dir, gate, out text), "a GTX 1060 within budget: met");
                StringAssert.Contains("**Met**", File.ReadAllText(gate));
                StringAssert.Contains("proves the target", text);

                Assert.IsFalse(PerfTarget.Run(Path.Combine(dir, "nowhere"), null, out text));
                StringAssert.Contains("No reports yet", text);
            }
            finally { Directory.Delete(dir, true); }
        }
    }
}
