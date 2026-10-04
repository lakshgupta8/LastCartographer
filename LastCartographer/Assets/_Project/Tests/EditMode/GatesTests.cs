using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using UnityEditor;

namespace OWSBG.Tests
{
    /// <summary>
    /// The gates pass (docs/design/gates.md): every built way carries the gate the plans and the macro map put on it,
    /// a shut way has a bar in it and a soft gap never does, every story flag on the map stands at a door, the Edge's
    /// doors wait for the prologue, the Threshold's Wardens read Halvard's word, and every room has a catch under it.
    /// </summary>
    public class GatesTests
    {
        const string Scenes = "Assets/_Project/Scenes/Greybox/";

        static readonly Dictionary<string, string> _texts = new Dictionary<string, string>();
        static string SceneText(string id)
        {
            if (!_texts.TryGetValue(id, out var t)) _texts[id] = t = File.ReadAllText(Path.GetFullPath(Scenes + "Greybox_" + id + ".unity"));
            return t;
        }
        static bool SceneExists(string id) => File.Exists(Path.GetFullPath(Scenes + "Greybox_" + id + ".unity"));
        static IEnumerable<string> AllScenes() => Directory.GetFiles(Path.GetFullPath(Scenes), "Greybox_*.unity").Select(p => Path.GetFileNameWithoutExtension(p).Substring("Greybox_".Length));
        static string Guid(string codePath) => AssetDatabase.AssetPathToGUID("Assets/_Project/Code/" + codePath);
        static int Count(string text, string needle) => text.Split(new[] { needle }, StringSplitOptions.None).Length - 1;
        static Gate Of(RoomExit e) => new Gate { Needs = e.Needs, Flag = e.Flag, Soft = e.Soft };

        [Test]
        public void TheRuleReadsThePlanAndTheMap()
        {
            // A planned room's exit, both ways.
            var south = Gates.Between("Halden_Lowmarket_3", "Windreach_Stones_1");
            Assert.AreEqual("act2.started", south.Flag); Assert.AreEqual(Ability.None, south.Needs); Assert.IsTrue(south.Bars);
            Assert.AreEqual("act2.started", Gates.Between("Windreach_Stones_1", "Halden_Lowmarket_3").Flag, "a gate applies in both directions");
            var grove = Gates.Between("Verdance_Chapel_2", "Verdance_Grove_1");
            Assert.AreEqual(Ability.Inkthread, grove.Needs); Assert.IsFalse(grove.Soft); Assert.IsTrue(grove.Bars, "a hard ability gate bars");
            var mine = Gates.Between("Emberdown_Rest_3", "Emberdown_Hollow_1");
            Assert.AreEqual(Ability.Talonhold, mine.Needs); Assert.AreEqual("emberdown.hollowvein_opened", mine.Flag);
            // A planned room's external exit matches the coast's room by its zone.
            var road = Gates.Between("Verdance_Road_1", "Saltmarrow_IrisFields");
            Assert.AreEqual(Ability.Wingbeat, road.Needs); Assert.IsTrue(road.Soft); Assert.IsFalse(road.Bars, "a soft gap never bars");
            Assert.AreEqual("saltmarrow.tether", Gates.Between("Blank_Aury_1", "Saltmarrow_Chain_3").Flag, "the tether from the faded third");
            // The coast's rooms take the map's link between their zones; inside one zone there is none.
            var bones = Gates.Between("Saltmarrow_BoneBridge", "Emberdown_Stair_1");
            Assert.AreEqual(Ability.Wingbeat, bones.Needs); Assert.IsTrue(bones.Soft);
            Assert.AreEqual(Ability.Wingbeat, Gates.Between("Saltmarrow_Lighthouse", "Saltmarrow_Chapel").Needs, "the gap between the fifth and sixth lighthouses");
            Assert.IsTrue(Gates.Between("Saltmarrow_Chapel", "Saltmarrow_BoneBridge").Soft);
            Assert.IsTrue(Gates.Between("Saltmarrow_A", "Saltmarrow_Stilts").IsNone);
            Assert.IsTrue(Gates.Between("Saltmarrow_B", "Saltmarrow_Tetherline").IsNone);
            // The Edge's doors wait for the prologue, one way only.
            Assert.AreEqual(PrologueDirector.Crossed, Gates.PrologueFlag);
            Assert.AreEqual(Gates.PrologueFlag, Gates.Between("Greyfold_Edge", "Greyfold_EdgeCamp_2").Flag);
            Assert.AreEqual(Gates.PrologueFlag, Gates.Between("Greyfold_Edge", "Greyfold_Cathedral_2").Flag);
            Assert.IsTrue(Gates.Between("Greyfold_EdgeCamp_2", "Greyfold_Edge").IsNone, "into the Edge is open: she comes back to it at Act 1's end");
            Assert.IsTrue(Gates.Between(null, "x").IsNone);
            Assert.AreEqual("Halden_Lowmarket_3", Gates.PlaceOfScene("Greybox_Halden_Lowmarket_3"));

            // Open or shut.
            bool None(string f) => false;
            Assert.IsTrue(road.IsOpen(Ability.None, None), "soft: skill crosses it");
            Assert.IsFalse(grove.IsOpen(Ability.Wingbeat, None)); Assert.IsTrue(grove.IsOpen(Ability.Inkthread | Ability.Wingbeat, None));
            Assert.IsFalse(south.IsOpen(Ability.Sky, None), "a story flag is hard, never soft");
            Assert.IsTrue(south.IsOpen(Ability.None, f => f == "act2.started"));
            Assert.IsFalse(mine.IsOpen(Ability.Talonhold, None)); Assert.IsFalse(mine.IsOpen(Ability.None, f => true)); Assert.IsTrue(mine.IsOpen(Ability.Talonhold, f => true));
            Assert.AreEqual("Talonhold, emberdown.hollowvein_opened", mine.ToString());
            Assert.AreEqual("Wingbeat (soft)", road.ToString());
        }

        [Test]
        public void EveryPlannedWayCarriesItsGateAndOnlyAShutWayHasABar()
        {
            int gated = 0, barred = 0;
            foreach (var p in RoomPlans.All)
            {
                if (!SceneExists(p.Id)) continue;
                var text = SceneText(p.Id);
                foreach (var e in p.Exits)
                {
                    var g = Of(e);
                    if (g.IsNone) continue;
                    gated++;
                    if (e.Flag != null) Assert.GreaterOrEqual(Count(text, "\n  Flag: " + e.Flag), 1, p.Id + "'s way to " + e.To + " waits for " + e.Flag);
                    if (e.Needs != Ability.None) Assert.GreaterOrEqual(Count(text, "\n  Needs: " + (int)e.Needs), 1, p.Id + "'s way to " + e.To + " needs " + e.Needs);
                }
                int bars = p.Exits.Count(e => Of(e).Bars);
                if (p.Id == Gates.EdgeRoom) bars = 2;   // the prologue's doors
                Assert.AreEqual(bars, Count(text, "m_Name: Bar"), p.Id + "'s bars: one per shut way, none for a soft gap");
                Assert.AreEqual(p.Exits.Count(e => e.Soft), Count(text, "\n  Soft: 1"), p.Id + "'s soft gaps");
                barred += bars;
            }
            Assert.GreaterOrEqual(gated, 55, "the plans' gated ways stand in their scenes (fifty-five at v1)");
            Assert.GreaterOrEqual(barred, 40);
            foreach (var id in AllScenes())
                Assert.AreEqual(Count(SceneText(id), "m_Name: Bar"), Count(SceneText(id), Guid("World/GateBar.cs")), id + ": every bar knows its gate");
        }

        [Test]
        public void TheCoastsWaysTakeTheMapsLinks()
        {
            var bones = SceneText("Saltmarrow_BoneBridge");
            Assert.AreEqual(2, Count(bones, "\n  Soft: 1"), "the whale's gap and the climb to Emberdown are soft Wingbeat gaps");
            Assert.AreEqual(2, Count(bones, "\n  Needs: " + (int)Ability.Wingbeat));
            Assert.AreEqual(0, Count(bones, "m_Name: Bar"), "and nothing bars them: the game notices instead");
            var chapel = SceneText("Saltmarrow_Chapel");
            Assert.AreEqual(2, Count(chapel, "\n  Soft: 1"), "both of the chapel's ways are soft gaps: the lighthouses' gap west, the whale's east");
            Assert.AreEqual(0, Count(chapel, "m_Name: Bar"));
            Assert.AreEqual(1, Count(SceneText("Saltmarrow_Lighthouse"), "\n  Soft: 1"), "the gap between the fifth and sixth lighthouses");
            Assert.AreEqual(0, Count(SceneText("Saltmarrow_A"), "\n  Soft: 1") + Count(SceneText("Saltmarrow_A"), "m_Name: Bar"), "the Quay's ways are open");
            Assert.AreEqual(2, Count(SceneText("Greyfold_Edge"), "\n  Flag: " + Gates.PrologueFlag), "the Edge's two doors wait for the prologue");
        }

        [Test]
        public void EveryStoryFlagOnTheMapStandsAtADoor()
        {
            var all = AllScenes().Select(SceneText).ToList();
            foreach (var link in WorldGraph.Links)
            {
                if (string.IsNullOrEmpty(link.Flag)) continue;
                Assert.IsTrue(all.Any(t => Count(t, "\n  Flag: " + link.Flag) > 0), link.From + " to " + link.To + " waits for " + link.Flag + " at a built door");
            }
            foreach (var flag in new[] { "act2.started", "isolde.cache", "act2.threshold", "greyfold.crossed", "act3.started", "halden.vault_opened", "emberdown.hollowvein_opened", "saltmarrow.tether" })
                Assert.GreaterOrEqual(all.Sum(t => Count(t, "\n  Flag: " + flag)), flag == "saltmarrow.tether" ? 1 : 2, flag + " stands at both ends of its way");
        }

        [Test]
        public void TheThresholdsWardensReadHalvardsWord()
        {
            Assert.AreEqual(3, Count(SceneText("Greyfold_Threshold_1"), "_standDownFlag: act2.halvard_third"), "the three on the line stand down on Halvard's word");
            Assert.AreEqual(3, AllScenes().Sum(id => Count(SceneText(id), "_standDownFlag: act2")), "and nobody else does");
            Assert.IsTrue(File.ReadAllText(Path.GetFullPath("Assets/_Project/Dialogue/Greyfold/Greyfold_Threshold.yarn")).Contains("<<flag act2.halvard_third 1>>"), "the scene sets it");
        }

        [Test]
        public void EveryRoomHasACatchUnderIt()
        {
            string catchGuid = Guid("World/FallCatch.cs");
            int n = 0;
            foreach (var id in AllScenes())
            {
                var text = SceneText(id);
                Assert.AreEqual(1, Count(text, "m_Name: FallCatch"), id + " has one catch under it");
                Assert.AreEqual(1, Count(text, catchGuid), id);
                n++;
            }
            Assert.AreEqual(114, n, "every built room");
        }
    }
}
