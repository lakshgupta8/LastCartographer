using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Every side commission played through (the commissions pass): what the story has done before its ledger posts it,
    /// then posted at its hub, taken, its steps done the way the world does them (a scene played with the choices that
    /// lead to the step, a vantage surveyed, crabs counted, a fight won, the camp walked with), fulfilled, and closed at the
    /// ledger for its reward. Before each scene someone who starts it is standing, and before each fight its arena is
    /// awake (<see cref="StagePresence"/>), so a commission whose step the world never lets her reach fails here.
    /// </summary>
    public class CommissionRunsTests
    {
        enum Act { Talk, Survey, Count, Win, CampWalk, Set }

        sealed class Do
        {
            public Act Act; public string Key; public int[] Choices = new int[0]; public int N = 1;
            public override string ToString() => Act + " " + Key;
        }

        static Do Talk(string node, params int[] choices) => new Do { Act = Act.Talk, Key = node, Choices = choices };
        static Do Survey(string vantage) => new Do { Act = Act.Survey, Key = vantage };
        static Do Count(string counter, int n) => new Do { Act = Act.Count, Key = counter, N = n };
        static Do Win(string boss) => new Do { Act = Act.Win, Key = boss };
        static readonly Do CampWalk = new Do { Act = Act.CampWalk, Key = "the camp" };
        /// <summary>A beat the story has already written by then, elsewhere (its writer is audited by StoryAuditTests).</summary>
        static Do Set(string flag, int value = 1) => new Do { Act = Act.Set, Key = flag, N = value };

        sealed class Run
        {
            public string Id; public Do[] Before = new Do[0]; public Do[] Steps = new Do[0];
            public override string ToString() => Id;
        }

        static Run R(string id, Do[] before, params Do[] steps) => new Run { Id = id, Before = before, Steps = steps };
        static Do[] B(params Do[] d) => d;
        static readonly Do[] Nothing = new Do[0];

        static IEnumerable<Run> Runs() => new[]
        {
            // ---- Saltmarrow: Sable's ledger on the quay
            R("saltmarrow.lantern_chain", Nothing, Survey("Saltmarrow_Lighthouse/Lamp")),   // the lamp is drawn when the Lamp-Keeper lets it go
            R("saltmarrow.bone_bridge", B(Set("saltmarrow.sable.talked")), Survey("Saltmarrow_A/Reedmother"), Talk("Quay_Sable", 0)),
            R("saltmarrow.iris_harvest", Nothing, Count("kill.MarshCrab", 3)),
            R("saltmarrow.dotha", B(Set("saltmarrow.met_sable")), Talk("Merrow_Dotha", 0)),
            R("saltmarrow.tether_widows", B(Win("lamp_keeper")), Talk("Tether_Knot", 0)),
            // ---- Emberdown: Kettil's ledger
            R("emberdown.long_roll_call", B(Set("emberdown.runa.counted"), Set(BoundsWalks.LearnedFlag)), Talk("Hollowvein_Runa_Walk", 1)),   // leave them buried
            R("emberdown.ninth_chimney", B(Set("emberdown.runa.climbed")), Survey("Emberdown_Chimneys_3/Ninth"), Talk("Chimneys_Ninth_Agent", 0)),
            R("emberdown.debate", B(Set("emberdown.kettil.met")), Talk("Baths_Kettil_Debate", 0)),
            R("emberdown.furnace_rescue", B(Set("emberdown.kettil.met")), Talk("Stair_Rescue", 0)),
            R("emberdown.overlook", B(Set("emberdown.runa.climbed")), Survey("Emberdown_Overlook_2/Overlook")),
            // ---- The Verdance: the Quiet House's ledger
            R("verdance.ash_remembers", B(Set("verdance.teodor.met")), Talk("Aldermere_Teodor", 0)),   // stay to the end
            R("verdance.sunken_library", B(Set("verdance.teodor.thread")), Survey("Verdance_Library_2/Page"), Talk("Library_Ansel", 1, 1)),   // offer to turn it, then leave it
            R("verdance.solvent", B(Set("verdance.teodor.met")), Talk("Road_Solvent", 0)),
            R("verdance.overgrown_gate", B(Set("verdance.teodor.thread")), Survey("Verdance_Gate_2/Gate"), Talk("Gate_Inn", 0)),
            R("verdance.vigil", B(Set("verdance.teodor.thread")), Talk("Grove_Teodor_Vigil")),
            // ---- Halden: the Hall's ledger
            R("halden.strike", B(Set("halden.hall.pell_minder")), Talk("Lowmarket_Strike", 0)),   // the owners pay
            R("halden.masters_exam", B(Set("halden.hall.pell_minder")), Talk("Hall_Tam", 0)),
            R("halden.seventh_bridge", Nothing, Survey("Halden_Bridges_3/Seventh"), Talk("Bridges_Family", 0)),
            R("halden.orchard_keeper", Nothing, Talk("Orchard_Keeper", 0)),
            R("halden.voss_office", B(Set("halden.hall.pell_minder")), Talk("Office_Pell_Drawing", 0)),
            // ---- Windreach: the camp's ledger, walked with
            R("windreach.moving_camp", Nothing,
                Talk("Camp_Idrenne", 0), CampWalk, Talk("River_Idrenne_Night", 0), CampWalk, Set("windreach.leap.done"), Talk("Grass_Idrenne_Night")),
            R("windreach.nine_stones", B(Talk("Camp_Idrenne", 0), CampWalk), Talk("River_Idrenne_Night", 2)),   // then the ninth stone is here
            R("windreach.guild_surveyor", B(Talk("Camp_Idrenne", 0), CampWalk, Talk("River_Idrenne_Night", 2)), Talk("Stones_Hale", 2)),   // finish it, then
            R("windreach.fallen_star", B(Set("windreach.star.woken")), Win("fallen_star")),
            R("windreach.idrennes_fire",
                B(Talk("Camp_Idrenne", 0), CampWalk, Talk("River_Idrenne_Night", 0), CampWalk, Set("windreach.leap.done"), Talk("Grass_Idrenne_Night")),
                Talk("Fire_Idrenne", 0, 1)),   // ask, then leave it undrawn
        };

        readonly RouteReplay _replay = new RouteReplay();

        [SetUp] public void SetUp() => RouteReplay.Prepare();
        [UnityTearDown] public IEnumerator TearDown() { yield return RouteReplay.Unload(); }

        [Test]
        public void EveryCommissionHasARun()
        {
            CommissionCatalog.Reset();
            CollectionAssert.AreEquivalent(CommissionCatalog.All.Select(d => d.Id), Runs().Select(r => r.Id), "one run for each commission on every ledger");
        }

        IEnumerator Perform(Do d, string where)
        {
            var w = GameState.World;
            switch (d.Act)
            {
                case Act.Set: w.Set(d.Key, d.N); break;
                case Act.Survey: Atlas.Survey(w, d.Key); break;
                case Act.Count: for (int i = 0; i < d.N; i++) Commissions.Bump(w, d.Key); break;
                case Act.CampWalk: Assert.IsTrue(Camp.WalkWith(w), where + ": the camp walks on"); break;
                case Act.Win:
                    var arenas = StagePresence.ArenasFor(d.Key);
                    Assert.IsNotEmpty(arenas, where + ": " + d.Key + " has an arena");
                    Assert.IsTrue(arenas.Any(a => string.IsNullOrEmpty(a.flag) || w.Is(a.flag)),
                        where + ": the arena is awake by now (" + string.Join(", ", arenas.Select(a => a.scene + " waits on " + a.flag)) + ")");
                    w.Set(Bosses.FlagKey(d.Key), true);
                    var sheet = Bosses.Find(d.Key);
                    if (sheet != null && sheet.Grants != Ability.None) w.Set(AbilitySet.FlagKey(sheet.Grants), true);
                    break;
                case Act.Talk:
                    var talkers = StagePresence.TalkersFor(d.Key);
                    Assert.IsNotEmpty(talkers, where + ": someone in a room starts " + d.Key);
                    Assert.IsTrue(talkers.Any(t => t.Stands(w)), where + ": nobody who starts " + d.Key + " is standing by now: " + string.Join(", ", talkers.Select(t => t.ToString())));
                    yield return _replay.Converse(where, d.Key, d.Choices);
                    break;
            }
            Commissions.Evaluate(w);
        }

        [UnityTest]
        public IEnumerator TheCommissionIsPostedTakenDoneAndClosed([ValueSource(nameof(Ids))] string id)
        {
            yield return _replay.Boot();
            var w = GameState.World;
            var run = Runs().Single(r => r.Id == id);
            var def = CommissionCatalog.Find(id);
            Assert.IsNotNull(def);

            for (int i = 0; i < run.Before.Length; i++) yield return Perform(run.Before[i], id + " before, " + run.Before[i]);
            Commissions.PostAvailable(w, def!.Hub);
            Assert.AreEqual(CommissionState.Posted, Commissions.StateOf(w, id), id + " is on " + def.Hub + "'s ledger by now");
            Assert.IsTrue(Commissions.Take(w, id), id + " is taken");
            Assert.AreEqual(CommissionState.Taken, Commissions.StateOf(w, id), id + ": nothing done yet, so not fulfilled on taking");

            for (int i = 0; i < run.Steps.Length; i++) yield return Perform(run.Steps[i], id + " step " + (i + 1) + ", " + run.Steps[i]);
            Assert.AreEqual(CommissionState.Fulfilled, Commissions.StateOf(w, id), id + ": its steps are done");

            int scraps = Commissions.Scraps(w);
            Assert.IsTrue(Commissions.Close(w, id), id + " is turned in at the ledger");
            Assert.AreEqual(CommissionState.Closed, Commissions.StateOf(w, id));
            Assert.AreEqual(scraps + def.RewardScraps, Commissions.Scraps(w), id + " pays what it says");
            if (def.SeedsIsland) Assert.IsTrue(Commissions.IsIslandSeeded(w, def.BlankIsland), id + " seeds its island in the Blank");
        }

        public static IEnumerable<string> Ids => Runs().Select(r => r.Id).ToList();
    }
}
