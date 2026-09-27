using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>The returning cast as data (NAR-05): the five recur, every appearance stands on the map, staged scenes are in built rooms.</summary>
    public class CastTests
    {
        [SetUp]
        public void SetUp() { Cast.Reset(); Cast.EnsureDefaults(); Atlas.Reset(); Atlas.EnsureDefaults(); }

        [Test]
        public void TheFiveRecurAndOwnAnImage()
        {
            foreach (var id in Cast.Recurring)
            {
                var m = Cast.Find(id);
                Assert.IsNotNull(m, id + " is in the cast");
                Assert.IsTrue(Cast.IsRecurring(id), id + " is met in at least two acts: " + string.Join(",", Cast.ActsOf(id)));
                Assert.IsNotEmpty(m.Image, id + " owns an image");
                Assert.IsNotEmpty(m.Tell, id + " has a tell");
                Assert.IsNotEmpty(m.NeverSays, id + " has a line they never say");
                Assert.IsTrue(Cast.AppearancesOf(id).Any(a => a.Act == Cast.Epilogue), id + " is in the epilogue");
            }
            Assert.IsTrue(Cast.Find("pell").Plain && Cast.Find("isolde").Plain, "Isolde and Pell speak plainly (style guide §6)");
            Assert.IsFalse(Cast.Find("sable").Plain);
        }

        [Test]
        public void EveryAppearanceStandsOnTheMap()
        {
            var ids = new HashSet<string>();
            foreach (var m in Cast.Members) Assert.IsTrue(ids.Add(m.Id), "duplicate member " + m.Id);
            var nodes = new HashSet<string>();
            foreach (var a in Cast.Appearances)
            {
                Assert.IsNotNull(Cast.Find(a.Character), "appearance of a member: " + a.Character);
                Assert.That(a.Act, Is.InRange(Cast.Prologue, Cast.Epilogue), a.Scene);
                Assert.IsNotNull(WorldGraph.Find(a.Zone), a.Character + " at " + a.Zone + " (" + a.Scene + ") is on the map");
                if (a.Staged) Assert.IsNotNull(a.Node, "a staged scene has a node: " + a.Scene);
                if (a.Node != null) Assert.IsTrue(nodes.Add(a.Node), "duplicate node " + a.Node);
            }
        }

        [Test]
        public void TheOrderOfTheMapIsKept()
        {
            // Sable belongs to the coast until the Blank.
            foreach (var a in Cast.AppearancesOf("sable").Where(a => a.Act <= Cast.Act2))
                Assert.IsTrue(a.Zone.StartsWith("Saltmarrow."), "Sable stays on the coast in the acts: " + a.Zone);
            // Pell is not met before the act break, and is Halden's from then on.
            var pell = Cast.AppearancesOf("pell");
            Assert.AreEqual("Greyfold.RoadThatStops", pell.First().Zone, "Pell first, at the Edge");
            Assert.IsFalse(pell.Any(a => a.Act == Cast.Prologue));
            // Runa and Teodor are the climbs: their first meetings are in their regions, and in Act 1 (the earliest climb).
            Assert.IsTrue(Cast.AppearancesOf("runa").Where(a => a.Act == Cast.Act1).All(a => a.Zone.StartsWith("Emberdown.")));
            Assert.IsTrue(Cast.AppearancesOf("teodor").Where(a => a.Act == Cast.Act1).All(a => a.Zone.StartsWith("Verdance.")));
            // Marrow is silent before Act 2 and speaks only in the Greyfold and the Blank.
            foreach (var a in Cast.AppearancesOf("marrow"))
            {
                if (a.Act < Cast.Act2) Assert.IsNull(a.Node, "Marrow has no lines before the Mirror Pool");
                Assert.IsTrue(a.Zone.StartsWith("Greyfold.") || a.Zone.StartsWith("Blank."), "Marrow is of the white: " + a.Zone);
            }
            CollectionAssert.AreEqual(new[] { 0, 2, 3, 4 }, Cast.ActsOf("marrow"));
            // The five ability-teachers stand where the abilities live.
            Assert.AreEqual(Ability.Talonhold, WorldGraph.Find(Cast.AppearancesOf("runa").First(a => a.Scene.StartsWith("Talonhold")).Zone).Grants);
            Assert.AreEqual(Ability.Inkthread, WorldGraph.Find(Cast.AppearancesOf("teodor").First(a => a.Scene.StartsWith("Inkthread")).Zone).Grants);
        }

        [Test]
        public void StagedScenesHaveAStage()
        {
            // A written scene stands in a built greybox room, or in a planned room (DES-09) where its speaker stands.
            var builtZones = new HashSet<string>(Atlas.AllPlaces.Select(p => WorldGraph.ZoneOfPlace(p.Id)).Where(z => z != null));
            var staged = Cast.Appearances.Where(a => a.Staged).ToList();
            foreach (var a in staged)
            {
                bool built = builtZones.Contains(a.Zone);
                bool planned = RoomPlans.InZone(a.Zone).Any(r => r.Npcs.Contains(a.Character));
                Assert.IsTrue(built || planned, a.Node + " has a room in " + a.Zone + ", built or planned, with " + a.Character + " in it");
            }
            CollectionAssert.AreEquivalent(new[] { "isolde", "sable", "dotha", "halvard", "runa", "kettil", "teodor", "pell", "maren" }, staged.Select(a => a.Character).Distinct());
            Assert.AreEqual(3, Cast.AppearancesOf("pell").Count(a => a.Staged), "Pell's Halden scenes are written (NAR-09)");
            Assert.AreEqual(6, Cast.AppearancesOf("runa").Count(a => a.Staged), "Runa's Emberdown scenes are written (NAR-07)");
            Assert.AreEqual(6, Cast.AppearancesOf("teodor").Count(a => a.Staged), "Teodor's Verdance scenes are written (NAR-08)");
        }
    }
}
