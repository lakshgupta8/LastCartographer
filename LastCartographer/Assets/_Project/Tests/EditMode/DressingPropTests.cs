#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The dressing is drawn and stood (ENV-06, docs/design/environment-props.md): every piece of the environmental pass
    /// names a drawing in its region's kit, the pieces that change with their place name a second and what changes it
    /// (the same thing their Yarn branches on), every built room stands its pieces, and the pale blocks are gone from
    /// every readable but the birds who ask.
    /// </summary>
    public class DressingPropTests
    {
        const string Scenes = "Assets/_Project/Scenes/Greybox/";
        const string Kits = "Assets/_Project/Art/Environment/";
        static string SceneText(string room) => File.ReadAllText(Path.GetFullPath(Scenes + "Greybox_" + room + ".unity"));
        static bool Built(string room) => File.Exists(Path.GetFullPath(Scenes + "Greybox_" + room + ".unity"));
        /// <summary>How many objects of that exact name the scene has (a YAML "m_Name:" line, whatever its line ending).</summary>
        static int Count(string text, string objectName) => Regex.Matches(text, "^  m_Name: " + Regex.Escape(objectName) + "\r?$", RegexOptions.Multiline).Count;
        static bool Has(string text, string objectName) => Count(text, objectName) > 0;
        static string Kit(Region r) => File.ReadAllText(Path.GetFullPath(Kits + r + "/kit.json"));
        static bool InKit(Region r, string prop) => Kit(r).Contains("\"name\": \"Prop_" + prop + "\"") && File.Exists(Path.GetFullPath(Kits + r + "/Prop_" + prop + ".png"));

        [Test]
        public void EveryPieceHasADrawingInItsKit()
        {
            Assert.AreEqual(42, Dressing.All.Count, "the catalog");
            foreach (var p in Dressing.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(p.Prop), p.Id + " names a drawing");
                Assert.IsTrue(InKit(p.Region, p.Prop), p.Id + ": Prop_" + p.Prop + " is in the " + p.Region + " kit");
                if (p.PropAfter != null)
                {
                    Assert.IsTrue(InKit(p.Region, p.PropAfter), p.Id + ": Prop_" + p.PropAfter + " is in the " + p.Region + " kit");
                    Assert.IsFalse(p.Change.IsEmpty, p.Id + " says what changes it");
                }
                else Assert.IsTrue(p.Change.IsEmpty, p.Id + " never changes, so nothing changes it");
            }
            Assert.AreEqual(6, Dressing.All.Count(p => p.PropAfter != null), "six pieces change with their place");
            var doors = Offerings.All.Where(a => a.Kind == AskerKind.Door).ToList();
            Assert.AreEqual(2, doors.Count, "two doors ask");
            foreach (var d in doors)
            {
                var region = Dressing.RegionOfRoom(d.Room);
                Assert.IsTrue(InKit(region, d.Prop), d.Id + " shut: Prop_" + d.Prop);
                Assert.IsTrue(InKit(region, d.Prop + "_Open"), d.Id + " open: Prop_" + d.Prop + "_Open");
                Assert.IsFalse(string.IsNullOrEmpty(d.Opens), d.Id + " opens on a flag");
            }
            Assert.IsTrue(Offerings.All.Where(a => a.Kind == AskerKind.Bird).All(a => a.Prop == null), "a bird who asks is a character, not a prop");
            Assert.IsTrue(InKit(Region.Verdance, "Inscription"), "the gate's inscription");
        }

        [Test]
        public void TheChangeReadsTheWorldAsTheYarnDoes()
        {
            var w = new WorldState();
            var cups = Dressing.Find("pithead.cups")!;
            Assert.IsFalse(cups.Change.IsMet(w), "turned down in a new game");
            w.Set("emberdown.hollowvein.walked", true);
            Assert.IsTrue(cups.Change.IsMet(w), "right side up after the walk");
            Assert.IsTrue(Dressing.Find("hollow.lamps")!.Change.IsMet(w) && Dressing.Find("hollow.bottom")!.Change.IsMet(w), "the lamps and the bottom with them");

            var lintels = Dressing.Find("merrow.lintels")!;
            Assert.IsFalse(lintels.Change.IsMet(w), "no chalk while unwritten");
            w.Set(Places.Key("Saltmarrow_B"), (int)PlaceFate.Held);
            Assert.IsTrue(lintels.Change.IsMet(w), "chalk when held");
            w.Set(Places.Key("Saltmarrow_B"), (int)PlaceFate.Anchored);
            Assert.IsTrue(lintels.Change.IsMet(w), "chalk when anchored");
            w.Set(Places.Key("Saltmarrow_B"), (int)PlaceFate.Released);
            Assert.IsFalse(lintels.Change.IsMet(w), "no chalk when released");

            var map = Dressing.Find("cloister.tapestry")!;
            w.Set(Places.Key("Verdance_Aldermere_2"), (int)PlaceFate.Released);
            Assert.IsTrue(map.Change.IsMet(w), "Aldermere's knot opens when released");
            var notice = Dressing.Find("lowmarket.notice")!;
            Assert.IsFalse(notice.Change.IsMet(w));
            w.Set(Places.Key("Halden_Lowmarket_2"), (int)PlaceFate.Anchored);
            Assert.IsTrue(notice.Change.IsMet(w), "COMPLETE once anchored");
            Assert.IsFalse(default(DressingChange).IsMet(w), "an empty change never is");
            Assert.IsFalse(cups.Change.IsMet(null), "no world, no change");

            // What the drawing branches on is what the scene branches on.
            foreach (var p in Dressing.All.Where(p => p.PropAfter != null && p.Readable))
            {
                var yarn = File.ReadAllText(Path.GetFullPath("Assets/_Project/Dialogue/" + p.Region + "/" + p.Region + "_Environment.yarn"));
                int at = yarn.IndexOf("title: " + p.Node, System.StringComparison.Ordinal);
                Assert.GreaterOrEqual(at, 0, p.Id + " has its scene");
                int end = yarn.IndexOf("\n===", at, System.StringComparison.Ordinal);
                string scene = yarn.Substring(at, end - at);
                string key = p.Change.Flag ?? p.Change.Place!;
                Assert.IsTrue(scene.Contains(key), p.Id + ": its scene branches on " + key);
            }
        }

        [Test]
        public void EveryPieceStandsInItsBuiltRoom()
        {
            var dressingGuid = AssetDatabase.AssetPathToGUID("Assets/_Project/Code/World/DressingProp.cs");
            Assert.IsFalse(string.IsNullOrEmpty(dressingGuid), "DressingProp is a script");
            int stood = 0, changing = 0;
            foreach (var group in Dressing.All.Where(p => Built(p.Room)).GroupBy(p => p.Room))
            {
                var text = SceneText(group.Key);
                foreach (var p in group)
                {
                    Assert.IsTrue(Has(text, "Prop_" + p.Prop), p.Id + " stands in " + group.Key + " as Prop_" + p.Prop);
                    stood++;
                    if (p.Readable) Assert.IsTrue(Has(text, "Read_" + p.Node), p.Id + " can be read in " + group.Key);
                    if (p.PropAfter == null) continue;
                    Assert.IsTrue(Has(text, "Prop_" + p.PropAfter), p.Id + "'s changed drawing stands in " + group.Key);
                    Assert.IsTrue(Has(text, "Dressing_" + p.Prop), p.Id + " stands under its Dressing_ object");
                    Assert.IsTrue(text.Contains("guid: " + dressingGuid), group.Key + " carries a DressingProp");
                    changing++;
                }
            }
            Assert.AreEqual(42, stood, "every piece's room is built");
            Assert.AreEqual(6, changing);
        }

        [Test]
        public void TheDoorsAreDrawnShutAndOpen()
        {
            var dressingGuid = AssetDatabase.AssetPathToGUID("Assets/_Project/Code/World/DressingProp.cs");
            foreach (var d in Offerings.All.Where(a => a.Kind == AskerKind.Door))
            {
                var text = SceneText(d.Room);
                Assert.IsTrue(Has(text, "Read_" + d.Node), d.Id + " asks in " + d.Room);
                Assert.IsTrue(Has(text, "Prop_" + d.Prop), d.Id + " shut");
                Assert.IsTrue(Has(text, "Prop_" + d.Prop + "_Open"), d.Id + " open");
                Assert.IsTrue(text.Contains("guid: " + dressingGuid), d.Id + " opens on its flag through a DressingProp");
                Assert.IsTrue(text.Contains(d.Opens), d.Id + "'s flag is in the scene");
            }
        }

        [Test]
        public void OnlyTheBirdsWhoAskAreStillBlocks()
        {
            // A Marker is the pale or ochre block under a Read_ object. The catalog's rooms, the askers' and the extras' are
            // all built; every one of them has only as many blocks as birds who ask there.
            var rooms = Dressing.All.Select(p => p.Room).Concat(Offerings.All.Select(a => a.Room))
                .Concat(new[] { "Verdance_Gate_2", "Halden_Orchard_2", "Halden_Observatory_2", "Greyfold_EdgeCamp_2" }).Distinct().Where(Built).ToList();
            Assert.GreaterOrEqual(rooms.Count, 30);
            int birds = 0;
            foreach (var room in rooms)
            {
                int expected = Offerings.All.Count(a => a.Room == room && a.Prop == null);
                int markers = Count(SceneText(room), "Marker");
                Assert.AreEqual(expected, markers, room + ": blocks left standing");
                birds += expected;
            }
            Assert.AreEqual(4, birds, "the gannet, the traveller, Brek and Corvin");
        }

        [Test]
        public void TheRecipesDrawingsAreNotDrawnTwice()
        {
            // Where the recipe stands the piece (a milestone, the standing stones, the exam desks), the trigger takes it
            // and no Dressing_ object is made; where the readable is the only drawing (the toll board), one is.
            Assert.AreEqual(3, Count(SceneText("Verdance_Road_2"), "Prop_Milestone"), "the road's three milestones, and no fourth under the trigger");
            Assert.IsFalse(Has(SceneText("Verdance_Road_2"), "Dressing_Milestone"));
            Assert.AreEqual(1, Count(SceneText("Windreach_Stones_1"), "Prop_Stone"), "the first stone, once");
            Assert.AreEqual(1, Count(SceneText("Halden_Bridges_1"), "Prop_TollBoard"));
            Assert.IsTrue(Has(SceneText("Halden_Bridges_1"), "Dressing_TollBoard"));
            Assert.AreEqual(1, Count(SceneText("Halden_Lowmarket_2"), "Prop_Notice"), "Lowmarket's board once, as the readable's");
            Assert.AreEqual(1, Count(SceneText("Halden_Lowmarket_2"), "Prop_Notice_Complete"));
        }
    }
}
