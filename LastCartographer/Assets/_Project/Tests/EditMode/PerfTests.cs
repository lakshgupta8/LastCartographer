using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The 60 fps lock and the budgets (PRG-24): physics at 60 Hz as the combat doc has it; vsync on a display whose
    /// refresh is a multiple of 60 and a 60 cap on any other; and a sample of frames summarised by percentile.
    /// </summary>
    public class PerfTests
    {
        [Test]
        public void PhysicsRunsAtSixtyHertz()
        {
            Assert.AreEqual(1f / 60f, Time.fixedDeltaTime, 1e-6f, "combat doc 1: physics at 60 Hz fixed step");
            Assert.AreEqual(1f / 60f, PerfBudget.FrameMs / 1000f, 1e-6f);
        }

        [Test]
        public void SixtyIsLockedOnEveryDisplay()
        {
            Assert.AreEqual((1, 60), FrameRate.For(60));
            Assert.AreEqual((1, 60), FrameRate.For(59.94), "NTSC's 59.94 is 60");
            Assert.AreEqual((2, 60), FrameRate.For(120));
            Assert.AreEqual((2, 60), FrameRate.For(119.88));
            Assert.AreEqual((3, 60), FrameRate.For(180));
            Assert.AreEqual((4, 60), FrameRate.For(240));
            Assert.AreEqual((0, 60), FrameRate.For(144), "vsync alone would run at 144: capped instead");
            Assert.AreEqual((0, 60), FrameRate.For(75));
            Assert.AreEqual((0, 60), FrameRate.For(165));
            Assert.AreEqual((0, 60), FrameRate.For(360), "past four refreshes a frame, capped");
            Assert.AreEqual((0, 60), FrameRate.For(0), "unknown refresh: capped");
        }

        [Test]
        public void WorldKeysAreMadeOnce()
        {
            // The hot paths ask for these every frame; the same key must be the same string, not a new one.
            Assert.AreSame(Places.Key("Saltmarrow_A"), Places.Key("Saltmarrow_A"));
            Assert.AreSame(FadeStages.Key("Saltmarrow_A"), FadeStages.Key("Saltmarrow_A"));
            Assert.AreSame(FadeStages.ErasedKey("Saltmarrow_A"), FadeStages.ErasedKey("Saltmarrow_A"));
            Assert.AreSame(DayClock.LockKey("Saltmarrow_A"), DayClock.LockKey("Saltmarrow_A"));
            Assert.AreSame(BoundsWalks.DoneKey("Saltmarrow_B"), BoundsWalks.DoneKey("Saltmarrow_B"));
            Assert.AreEqual("place.Saltmarrow_A.fate", Places.Key("Saltmarrow_A"), "the keys saves already hold");
            Assert.AreEqual("fade.Saltmarrow_A.erased", FadeStages.ErasedKey("Saltmarrow_A"));
            Assert.AreEqual("walk.Saltmarrow_B.done", BoundsWalks.DoneKey("Saltmarrow_B"));
            Assert.AreEqual("place.x.locked_time", DayClock.LockKey("x"));
            int made = Keys.Count;
            for (int i = 0; i < 100; i++) Places.Key("Saltmarrow_A");
            Assert.AreEqual(made, Keys.Count, "asking again makes nothing");
            Assert.AreNotSame(Places.Key("Saltmarrow_A"), Places.Key("Saltmarrow_B"));
        }

        [Test]
        public void FramesAreSummarisedByPercentile()
        {
            var ms = new float[100];
            for (int i = 0; i < 100; i++) ms[i] = i + 1;   // 1..100 ms
            var f = PerfBudget.Summarise(ms);
            Assert.AreEqual(100, f.Count);
            Assert.AreEqual(50.5f, f.AvgMs, 1e-4f);
            Assert.AreEqual(50f, f.P50Ms);
            Assert.AreEqual(95f, f.P95Ms);
            Assert.AreEqual(99f, f.P99Ms);
            Assert.AreEqual(100f, f.MaxMs);
            Assert.AreEqual(1000f / 50.5f, f.Fps, 1e-3f);

            var steady = PerfBudget.Summarise(new[] { 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 5f, 30f });
            Assert.AreEqual(5f, steady.P95Ms, "one hitch in twenty is past the 95th percentile");
            Assert.AreEqual(30f, steady.MaxMs, "and the worst frame still shows it");
            Assert.IsTrue(PerfBudget.FrameWithin(steady));
            Assert.IsFalse(PerfBudget.FrameWithin(f));
            Assert.IsFalse(PerfBudget.FrameWithin(PerfBudget.Summarise(new float[0])), "no sample is no pass");
            Assert.IsTrue(PerfBudget.TransitionWithin(99f));
            Assert.IsFalse(PerfBudget.TransitionWithin(101f));
        }
    }
}
