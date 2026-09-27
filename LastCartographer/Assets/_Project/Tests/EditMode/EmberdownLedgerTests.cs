using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// Kettil's Rest's ledger (NAR-07, bible 8.2), and every ledger's flags against the scripts: a commission that waits
    /// on a flag nobody writes can never be finished.
    /// </summary>
    public class EmberdownLedgerTests
    {
        [SetUp]
        public void SetUp() { CommissionCatalog.Reset(); RoomPlans.Reset(); }

        static HashSet<string> FlagsWrittenByScripts()
        {
            var dir = Path.Combine(Application.dataPath, "_Project/Dialogue");
            var written = new HashSet<string>();
            var rx = new Regex(@"<<flag\s+([a-z0-9_.]+)\s");
            foreach (var f in Directory.GetFiles(dir, "*.yarn", SearchOption.AllDirectories))
                foreach (Match m in rx.Matches(File.ReadAllText(f))) written.Add(m.Groups[1].Value);
            return written;
        }

        [Test]
        public void KettilsLedgerIsFiveEntriesOnThePlannedRooms()
        {
            var ember = CommissionCatalog.AtHub("Emberdown");
            Assert.AreEqual(5, ember.Count);
            Assert.AreEqual(1, ember.Count(d => d.SeedsIsland), "the Long Roll-Call seeds an island");
            Assert.AreEqual("Hollowvein", CommissionCatalog.Find("emberdown.long_roll_call").BlankIsland);
            var vantages = new HashSet<string>(RoomPlans.All.Where(r => r.Vantage != null).Select(r => r.VantageId));
            foreach (var d in ember)
            {
                Assert.IsTrue(d.Id.StartsWith("emberdown."), d.Id);
                Assert.IsNotEmpty(d.Brief); Assert.IsNotEmpty(d.Journal); Assert.IsNotEmpty(d.Aftermath);
                Assert.Greater(d.RewardScraps, 0);
                foreach (var s in d.Steps.Where(s => s.Kind == StepKind.Vantage))
                    Assert.IsTrue(vantages.Contains(s.Key), d.Id + " asks for a vantage a planned room has: " + s.Key);
            }
            Assert.AreEqual(25, CommissionCatalog.All.Count, "five at each of the five hubs with a ledger");
            Assert.AreEqual(5, CommissionCatalog.AtHub("Saltmarrow").Count, "the coast's ledger is unchanged");
        }

        [Test]
        public void EveryFlagALedgerWaitsOnIsWrittenSomewhere()
        {
            var written = FlagsWrittenByScripts();
            // Written by code, not script: boss wins, bounds-walk completion, the survey.
            bool ByCode(string f) => f.StartsWith("boss.") || f.StartsWith("walk.") || f.StartsWith("ability.");
            foreach (var d in CommissionCatalog.All)
            {
                if (!string.IsNullOrEmpty(d.PostAfterFlag))
                    Assert.IsTrue(written.Contains(d.PostAfterFlag) || ByCode(d.PostAfterFlag), d.Id + " posts after " + d.PostAfterFlag + ", which nothing writes");
                foreach (var s in d.Steps.Where(s => s.Kind == StepKind.Flag))
                    Assert.IsTrue(written.Contains(s.Key) || ByCode(s.Key), d.Id + " waits on " + s.Key + ", which nothing writes");
            }
            // Verdance's ledger: five, two islands, vantage steps on the planned rooms.
            var verd = CommissionCatalog.AtHub("Verdance");
            Assert.AreEqual(5, verd.Count);
            CollectionAssert.AreEquivalent(new[] { "Aldermere", "Overgrown_Inn" }, verd.Where(d => d.SeedsIsland).Select(d => d.BlankIsland));
            var vantages = new HashSet<string>(RoomPlans.All.Where(r => r.Vantage != null).Select(r => r.VantageId));
            foreach (var d in verd)
                foreach (var s in d.Steps.Where(s => s.Kind == StepKind.Vantage))
                    Assert.IsTrue(vantages.Contains(s.Key), d.Id + " asks for a planned vantage: " + s.Key);
            // Halden's ledger: five, one island (Lowmarket), vantage steps on the planned rooms.
            var hal = CommissionCatalog.AtHub("Halden");
            Assert.AreEqual(5, hal.Count);
            CollectionAssert.AreEquivalent(new[] { "Lowmarket" }, hal.Where(d => d.SeedsIsland).Select(d => d.BlankIsland));
            foreach (var d in hal)
                foreach (var s in d.Steps.Where(s => s.Kind == StepKind.Vantage))
                    Assert.IsTrue(vantages.Contains(s.Key), d.Id + " asks for a planned vantage: " + s.Key);
            foreach (var f in new[] { "isolde.cache", "halden.orchard.cache_read", "pell.report_sent", "pell.report_kept", "pell.report_read",
                                      "halden.vault_opened", "halden.vault.pell_counted", "halden.maren.decided", "halden.strike.decided",
                                      "act2.halvard_second", "halden.oriel.met" })
                Assert.IsTrue(written.Contains(f), f + " is written by a script");
            // Windreach's ledger: five, no island (the Steppe is walked, not left), two past the Wind Gate.
            var wind = CommissionCatalog.AtHub("Windreach");
            Assert.AreEqual(5, wind.Count);
            Assert.IsFalse(wind.Any(d => d.SeedsIsland));
            Assert.AreEqual(2, wind.Count(d => d.RequiresAbility == Ability.Windmemory));
            foreach (var f in new[] { "windreach.idrenne.met", "windreach.stones.named", "windreach.leap.done", "windreach.camp.walked",
                                      "windreach.fire.witnessed", "windreach.survey.decided", "keystone.windreach", "windreach.star.woken",
                                      "windreach.hale.finished", "windreach.hale.pages", "windreach.hale.decided" })
                Assert.IsTrue(written.Contains(f), f + " is written by a script");
            // The arcs' own beats are all written.
            foreach (var f in new[] { "verdance.teodor.met", "verdance.teodor.thread", "verdance.grove.vigil", "verdance.aldermere.attended",
                                      "verdance.aldermere.stopped", "teodor.keystone_given", "teodor.refused", "keystone.quiet_house",
                                      "verdance.library.page_turned", "verdance.solvent.decided", "verdance.gate.inn_visited" })
                Assert.IsTrue(written.Contains(f), f + " is written by a script");
            foreach (var f in new[] { "emberdown.kettil.met", "emberdown.runa.counted", "emberdown.runa.climbed", "emberdown.debate.heard",
                                      "emberdown.hollowvein_opened", "emberdown.hollowvein.walked", "emberdown.hollowvein.buried",
                                      "emberdown.overlook.seen", "runa.named_wren", "holdfast.walk_learned" })
                Assert.IsTrue(written.Contains(f), f + " is written by a script");
        }

        [Test]
        public void AbilitiesPersistAsFlags()
        {
            var w = new WorldState();
            Assert.AreEqual(Ability.None, AbilitySet.FromWorld(w));
            w.Set(AbilitySet.FlagKey(Ability.Talonhold), true);
            w.Set(AbilitySet.FlagKey(Ability.Wingbeat), true);
            Assert.AreEqual(Ability.Talonhold | Ability.Wingbeat, AbilitySet.FromWorld(w));
            Assert.AreEqual("ability.talonhold", AbilitySet.FlagKey(Ability.Talonhold));
            var back = GameState.FromJson(GameState.ToJson(w));
            Assert.AreEqual(Ability.Talonhold | Ability.Wingbeat, AbilitySet.FromWorld(back), "and the save keeps them");
        }
    }
}
