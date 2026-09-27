using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The ledger state machine, step evaluation, rewards and save round-trip, all on plain WorldState.</summary>
    public class CommissionTests
    {
        WorldState _w;

        [SetUp]
        public void SetUp()
        {
            CommissionCatalog.Reset();
            _w = new WorldState();
        }

        [TearDown]
        public void TearDown() { CommissionCatalog.Reset(); }

        static CommissionDef Def(string id, params CommissionStep[] steps) => new CommissionDef
        {
            Id = id, Title = id, Hub = "Test", Brief = "b", Journal = "j", Aftermath = "a", Steps = steps,
        };

        [Test]
        public void StateMachineOnlyMovesForward()
        {
            CommissionCatalog.Register(Def("t.a", CommissionStep.Flag("t.flag", "flag")));
            var changes = new List<CommissionState>();
            void OnChanged(string id, CommissionState s) { if (id == "t.a") changes.Add(s); }
            Commissions.Changed += OnChanged;
            try
            {
                Assert.AreEqual(CommissionState.Unknown, Commissions.StateOf(_w, "t.a"));
                Assert.IsFalse(Commissions.Take(_w, "t.a"), "cannot take what is not posted");
                Assert.IsTrue(Commissions.Post(_w, "t.a"));
                Assert.IsFalse(Commissions.Post(_w, "t.a"), "posting twice is a no-op");
                Assert.IsFalse(Commissions.Close(_w, "t.a"), "cannot close before fulfilment");
                Assert.IsTrue(Commissions.Take(_w, "t.a"));
                Assert.AreEqual(CommissionState.Taken, Commissions.StateOf(_w, "t.a"), "steps not met, so still taken");
                Assert.IsTrue(Commissions.Fulfil(_w, "t.a"));
                Assert.IsFalse(Commissions.Fail(_w, "t.a"), "fulfilled commissions cannot fail");
                Assert.IsTrue(Commissions.Close(_w, "t.a"));
                Assert.IsFalse(Commissions.Take(_w, "t.a"));
                CollectionAssert.AreEqual(new[] { CommissionState.Posted, CommissionState.Taken, CommissionState.Fulfilled, CommissionState.Closed }, changes);
            }
            finally { Commissions.Changed -= OnChanged; }
        }

        [Test]
        public void EvaluateFulfilsWhenEveryStepIsMet()
        {
            CommissionCatalog.Register(Def("t.b", CommissionStep.Vantage("Room/V", "draw"), CommissionStep.Flag("t.b.talked", "talk")));
            Commissions.Post(_w, "t.b");
            Commissions.Take(_w, "t.b");
            Assert.IsEmpty(Commissions.Evaluate(_w));
            _w.MarkSurveyed("Room/V");
            Assert.IsEmpty(Commissions.Evaluate(_w), "one of two steps");
            Assert.IsTrue(Commissions.StepDone(_w, CommissionCatalog.Find("t.b"), 0));
            Assert.IsFalse(Commissions.StepDone(_w, CommissionCatalog.Find("t.b"), 1));
            _w.Set("t.b.talked", 1);
            CollectionAssert.AreEqual(new[] { "t.b" }, Commissions.Evaluate(_w));
            Assert.AreEqual(CommissionState.Fulfilled, Commissions.StateOf(_w, "t.b"));
        }

        [Test]
        public void TakingAnAlreadyMetCommissionFulfilsItAtOnce()
        {
            CommissionCatalog.Register(Def("t.c", CommissionStep.Vantage("Room/V", "draw")));
            _w.MarkSurveyed("Room/V");
            Commissions.Post(_w, "t.c");
            Commissions.Take(_w, "t.c");
            Assert.AreEqual(CommissionState.Fulfilled, Commissions.StateOf(_w, "t.c"));
        }

        [Test]
        public void CountersOnlyMoveForTakenCommissions()
        {
            CommissionCatalog.Register(Def("t.d", CommissionStep.Count("kill.MarshCrab", 3, "crabs")));
            var def = CommissionCatalog.Find("t.d");
            Assert.AreEqual(0, Commissions.Bump(_w, "kill.MarshCrab"), "unknown: nothing counts");
            Commissions.Post(_w, "t.d");
            Assert.AreEqual(0, Commissions.Bump(_w, "kill.MarshCrab"), "posted but not taken: nothing counts");
            Commissions.Take(_w, "t.d");
            Assert.AreEqual(1, Commissions.Bump(_w, "kill.MarshCrab"));
            Assert.AreEqual(0, Commissions.Bump(_w, "kill.ReedSkimmer"), "other counters do not match");
            Assert.AreEqual(1, Commissions.Progress(_w, def, 0));
            Commissions.Bump(_w, "kill.MarshCrab");
            Assert.AreEqual(CommissionState.Taken, Commissions.StateOf(_w, "t.d"));
            Commissions.Bump(_w, "kill.MarshCrab");
            Assert.AreEqual(CommissionState.Fulfilled, Commissions.StateOf(_w, "t.d"));
            Assert.AreEqual(3, Commissions.Progress(_w, def, 0));
        }

        [Test]
        public void CloseGrantsRewardsAndSeedsTheBlankIsland()
        {
            CommissionCatalog.Register(new CommissionDef
            {
                Id = "t.e", Title = "e", Hub = "Test", Steps = new[] { CommissionStep.Flag("t.e.done", "x") },
                RewardScraps = 2, RewardInstrument = InstrumentKind.WaxSeal, RewardFlag = "t.e.rewarded", BlankIsland = "Test_Isle",
            });
            Assert.IsFalse(_w.Equipment.OwnsInstrument(InstrumentKind.WaxSeal));
            Commissions.Post(_w, "t.e");
            Commissions.Take(_w, "t.e");
            _w.Set("t.e.done", 1);
            Commissions.Evaluate(_w);
            Assert.AreEqual(0, Commissions.Scraps(_w), "nothing until it is turned in");
            Assert.IsTrue(Commissions.Close(_w, "t.e"));
            Assert.AreEqual(2, Commissions.Scraps(_w));
            Assert.IsTrue(_w.Equipment.OwnsInstrument(InstrumentKind.WaxSeal));
            Assert.IsTrue(_w.Is("t.e.rewarded"));
            Assert.IsTrue(Commissions.IsIslandSeeded(_w, "Test_Isle"));
        }

        [Test]
        public void ApplyUnderstandsTheYarnVerbs()
        {
            CommissionCatalog.Register(Def("t.f", CommissionStep.Flag("t.f.x", "x")));
            Assert.IsFalse(Commissions.Apply(_w, "t.f", "dance"));
            Assert.IsTrue(Commissions.Apply(_w, "t.f", "post"));
            Assert.IsTrue(Commissions.Apply(_w, "t.f", "take"));
            Assert.IsTrue(Commissions.Apply(_w, "t.f", "Fulfill"));
            Assert.IsTrue(Commissions.Apply(_w, "t.f", "close"));
            Assert.AreEqual("closed", Commissions.Describe(Commissions.StateOf(_w, "t.f")));
            Assert.IsTrue(Commissions.Apply(_w, "t.g", "post"));
            Assert.IsTrue(Commissions.Apply(_w, "t.g", "fail"));
            Assert.AreEqual(CommissionState.Failed, Commissions.StateOf(_w, "t.g"));
        }

        [Test]
        public void StateAndCountersSurviveTheSaveRoundTrip()
        {
            CommissionCatalog.Register(Def("t.h", CommissionStep.Count("kill.any", 5, "five")));
            Commissions.Post(_w, "t.h");
            Commissions.Take(_w, "t.h");
            Commissions.Bump(_w, "kill.any", 2);
            var back = GameState.FromJson(GameState.ToJson(_w));
            Assert.AreEqual(CommissionState.Taken, Commissions.StateOf(back, "t.h"));
            Assert.AreEqual(2, Commissions.Counter(back, "t.h", 0));
            Commissions.Bump(back, "kill.any", 3);
            Assert.AreEqual(CommissionState.Fulfilled, Commissions.StateOf(back, "t.h"));
        }

        [Test]
        public void SaltmarrowSetIsFiveUniqueFinishableEntries()
        {
            var all = CommissionCatalog.All;
            Assert.AreEqual(5, all.Count);
            var ids = new HashSet<string>();
            foreach (var d in all)
            {
                Assert.IsTrue(ids.Add(d.Id), "duplicate id " + d.Id);
                Assert.AreEqual("Saltmarrow", d.Hub);
                Assert.IsNotEmpty(d.Title); Assert.IsNotEmpty(d.Brief); Assert.IsNotEmpty(d.Journal); Assert.IsNotEmpty(d.Aftermath);
                Assert.Greater(d.Steps.Length, 0, d.Id + " has steps");
                Assert.Greater(d.RewardScraps, 0, d.Id + " pays");
            }
            Assert.AreEqual(1, CommissionCatalog.AtHub("Saltmarrow").FindAll(d => d.SeedsIsland).Count, "exactly one [B] commission");
        }
    }
}
