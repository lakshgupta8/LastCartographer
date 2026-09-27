using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The day (PRG-15): phases, midnight, sleeping at a desk, and the hour an anchored place keeps.</summary>
    public class DayClockTests
    {
        [Test]
        public void PhasesFollowTheClockAndMidnightRollsTheDay()
        {
            var w = new WorldState();
            Assert.AreEqual(DayPhase.Day, DayClock.Phase(w), "a new game starts in the morning");
            Assert.AreEqual(1, DayClock.Day(w));
            int changes = 0;
            DayClock.PhaseChanged += _ => changes++;

            DayClock.SetPhase(w, DayPhase.Dusk);
            Assert.AreEqual(DayPhase.Dusk, DayClock.Phase(w));
            Assert.AreEqual(1, changes);
            DayClock.Advance(w, 0.4f);
            Assert.AreEqual(DayPhase.Night, DayClock.Phase(w));
            Assert.AreEqual(1, DayClock.Day(w), "still today");
            DayClock.Advance(w, 0.2f);
            Assert.AreEqual(DayPhase.Day, DayClock.Phase(w), "past midnight and past dawn");
            Assert.AreEqual(2, DayClock.Day(w));
            Assert.AreEqual(3, changes);
            DayClock.Advance(w, 0.01f);
            Assert.AreEqual(3, changes, "no change inside a phase");

            Assert.AreEqual(DayPhase.Dawn, DayClock.PhaseAt(0.05f));
            Assert.AreEqual(DayPhase.Night, DayClock.PhaseAt(0.99f));
            Assert.AreEqual(DayPhase.Dawn, DayClock.PhaseAt(1.02f), "wraps");
            Assert.IsTrue(DayClock.TryParse("evening", out var p) && p == DayPhase.Dusk);
            Assert.IsFalse(DayClock.TryParse("teatime", out _));
        }

        [Test]
        public void SleepingAtADeskGoesToTheNextDawn()
        {
            var w = new WorldState();
            DayClock.SetPhase(w, DayPhase.Night);
            DayClock.Sleep(w);
            Assert.AreEqual(DayPhase.Dawn, DayClock.Phase(w));
            Assert.AreEqual(2, DayClock.Day(w));
            DayClock.Sleep(w);
            Assert.AreEqual(3, DayClock.Day(w), "a nap at dawn is still a night");
            Assert.AreEqual(DayPhase.Dawn, DayClock.Phase(w));
        }

        [Test]
        public void AnchoredPlacesKeepTheHourTheyWereSealedAt()
        {
            var w = new WorldState();
            DayClock.SetPhase(w, DayPhase.Dusk);
            Assert.IsTrue(Places.Anchor(w, "Quay"));
            Assert.IsTrue(DayClock.IsLocked(w, "Quay"));
            Assert.IsTrue(Places.Hold(w, "Village"));
            Assert.IsFalse(DayClock.IsLocked(w, "Village"), "holding lets the days go on");

            DayClock.Advance(w, 0.3f);
            Assert.AreEqual(DayPhase.Night, DayClock.Phase(w));
            Assert.AreEqual(DayPhase.Night, DayClock.PhaseIn(w, "Village"));
            Assert.AreEqual(DayPhase.Dusk, DayClock.PhaseIn(w, "Quay"), "the same tide every day");
            DayClock.Sleep(w);
            Assert.AreEqual(DayPhase.Dusk, DayClock.PhaseIn(w, "Quay"));

            var back = GameState.FromJson(GameState.ToJson(w));
            Assert.IsTrue(DayClock.IsLocked(back, "Quay"));
            Assert.AreEqual(DayPhase.Dusk, DayClock.PhaseIn(back, "Quay"));
            Assert.AreEqual(2, DayClock.Day(back));
            Assert.AreEqual(DayPhase.Dawn, DayClock.Phase(back));
        }
    }
}
