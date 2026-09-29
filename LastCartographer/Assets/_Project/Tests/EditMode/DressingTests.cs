using System;
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
    /// The environmental storytelling pass (NAR-15, docs/story/environment.md): every piece stands in a room that
    /// exists, every readable one is its own Yarn scene where the thing speaks and nothing changes, the plants the
    /// foreshadowing audit was waiting on are dressed, and the fledglings glide a little further for every piece of
    /// the sky.
    /// </summary>
    public class DressingTests
    {
        static string ProjectDir => Path.Combine(Application.dataPath, "_Project");
        static YarnAudit.Project _project;
        static Dictionary<string, string> _raw;

        static YarnAudit.Project Yarn()
        {
            if (_project != null) return _project;
            _project = new YarnAudit.Project();
            _raw = new Dictionary<string, string>();
            foreach (var f in Directory.GetFiles(Path.Combine(ProjectDir, "Dialogue"), "*.yarn", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(f).Replace("\r\n", "\n");
                _project.Add(Path.GetFileName(f), text);
                foreach (Match m in Regex.Matches(text, @"^title:\s*(\S+)\s*\n---\n(.*?)^===", RegexOptions.Multiline | RegexOptions.Singleline))
                    _raw[m.Groups[1].Value] = m.Groups[2].Value;
            }
            return _project;
        }

        static bool RoomExists(string room)
            => RoomPlans.Find(room) != null || File.Exists(Path.Combine(ProjectDir, "Scenes/Greybox/Greybox_" + room + ".unity"));

        [Test]
        public void EveryPieceStandsInARoomThatExists()
        {
            Assert.AreEqual(Dressing.All.Count, Dressing.All.Select(p => p.Id).Distinct().Count(), "one id, one piece");
            foreach (var p in Dressing.All)
            {
                Assert.IsTrue(RoomExists(p.Room), p.Id + ": " + p.Room + " is a planned or built room");
                Assert.That(p.Brief, Is.Not.Empty, p.Id + " has a brief for the art pass");
                if (p.Plant != null) Assert.IsTrue(Foreshadowing.Secrets.Contains(p.Plant) || p.Plant == "9.2", p.Id + " plants a bible section: " + p.Plant);
            }
            foreach (Region r in Enum.GetValues(typeof(Region)))
                Assert.GreaterOrEqual(Dressing.In(r).Count(), 2, r + " is dressed");
            foreach (DressingKind k in Enum.GetValues(typeof(DressingKind)))
                Assert.GreaterOrEqual(Dressing.All.Count(p => p.Kind == k), 3, "the pass has " + k);
            Assert.GreaterOrEqual(Dressing.All.Count(p => p.Readable), Dressing.All.Count / 2, "most of it can be read");
        }

        [Test]
        public void EveryReadablePieceIsAYarnSceneWhereTheThingSpeaks()
        {
            var project = Yarn();
            var people = new HashSet<string>(Cast.Members.Select(m => m.Name.Split(' ').Last()), StringComparer.OrdinalIgnoreCase);
            foreach (var m in Cast.Members) people.Add(m.Id);
            foreach (var p in Dressing.All.Where(p => p.Readable))
            {
                Assert.IsTrue(project.Nodes.TryGetValue(p.Node, out var node), p.Id + ": " + p.Node + " is a scene");
                Assert.AreEqual(p.Region + "_Environment.yarn", node.File, p.Id + " is read in its region's file");
                Assert.IsNotEmpty(node.Lines, p.Node);
                foreach (var line in node.Lines)
                {
                    Assert.IsFalse(line.IsOption, line + ": there is nothing to choose, only to read");
                    Assert.That(line.Speaker, Is.Not.Empty, line + " names the thing that speaks");
                    Assert.IsFalse(people.Contains(line.Speaker), line + ": the thing speaks, never a person (style guide 6)");
                    int words = line.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    Assert.LessOrEqual(words, 20, line + ": twenty words or fewer (style guide 2.1)");
                    Assert.IsTrue(line.Has("still"), line + ": it reads the same every time, so it's #still");
                }
                Assert.IsEmpty(node.Writes, p.Node + ": reading changes nothing");
                var commands = Regex.Matches(_raw[p.Node], @"<<\s*(\w+)").Cast<Match>().Select(m => m.Groups[1].Value)
                    .Where(c => c != "if" && c != "elseif" && c != "else" && c != "endif").ToList();
                CollectionAssert.IsEmpty(commands, p.Node + ": no commands, only lines");

                var plants = node.Lines.SelectMany(l => l.Values("plant")).Distinct().ToList();
                if (p.Plant == null) CollectionAssert.IsEmpty(plants, p.Id + " plants nothing the catalog doesn't say");
                else CollectionAssert.AreEqual(new[] { p.Plant }, plants, p.Id + " plants " + p.Plant + " in its Yarn");
                if (p.Changes != null)
                    Assert.IsTrue(node.Lines.Any(l => l.Conditions.Count > 0), p.Id + " changes with its place, so some line is conditional");
            }
            // Every scene in an environment file is in the catalog.
            var catalogued = new HashSet<string>(Dressing.All.Where(p => p.Readable).Select(p => p.Node));
            foreach (var n in project.Nodes.Values.Where(n => n.File.EndsWith("_Environment.yarn")))
                Assert.IsTrue(catalogued.Contains(n.Title), n.Title + " (" + n.File + ") is catalogued in Dressing");
        }

        [Test]
        public void ThePlantsTheAuditWaitedOnAreDressed()
        {
            // foreshadowing.md §4: the exam papers, the framed drawing, the orchard's leaves, the towers without stairs.
            Assert.IsTrue(Dressing.InRoom("Halden_Hall_3").Any(p => p.Plant == "5.1" && p.Readable), "the Hall_3 exam papers");
            Assert.IsTrue(Dressing.InRoom("Halden_Bastion_3").Any(p => p.Plant == "5.5" && p.Readable), "the Bastion_3 drawing");
            Assert.IsTrue(Dressing.InRoom("Halden_Orchard_1").Any(p => p.Plant == "5.1"), "the orchard's fallen leaves");
            Assert.GreaterOrEqual(Dressing.All.Count(p => p.Plant == "5.6" && (p.Brief.Contains("no stairs") || p.Brief.Contains("no steps") || p.Brief.Contains("ladders"))), 3,
                "the towers without stairs (bible 5.6), on the coast, in Emberdown and in Halden");

            // The open item: 5.5 had exactly three plants. The Hall's standing order is a fourth, on both roads.
            var plants = Yarn().Lines.SelectMany(l => l.Values("plant").Where(v => v.StartsWith("5.")).Select(v => (l.Node, v))).ToList();
            foreach (var climb in new[] { ClimbRoute.Emberdown, ClimbRoute.Verdance })
            {
                var before = Foreshadowing.PlantsBefore("5.5", climb, plants);
                Assert.GreaterOrEqual(before.Count, 4, "5.5 climbing " + climb + " first: " + string.Join(", ", before));
                CollectionAssert.Contains(before, "Hall_Order");
                foreach (var s in Foreshadowing.Secrets)
                    Assert.GreaterOrEqual(Foreshadowing.PlantsBefore(s, climb, plants).Count(n => Dressing.All.Any(p => p.Node == n)), s == "5.2" || s == "5.3" ? 0 : 1,
                        s + " is planted by a room as well as a person, climbing " + climb + " first");
            }
        }

        [Test]
        public void TheFledglingsGlideFurtherForEveryPieceOfTheSky()
        {
            foreach (Region r in Enum.GetValues(typeof(Region)))
            {
                var loop = Dressing.LoopOf(r);
                Assert.IsNotNull(loop, r + " has fledglings (bible 10: every region)");
                Assert.IsTrue(RoomExists(loop.Room), r + "'s loop is in " + loop.Room);
                Assert.AreEqual(r, Dressing.RegionOfRoom(loop.Room));
            }
            var shore = Dressing.LoopOf(Region.Saltmarrow);
            var none = Dressing.At(shore, Ability.None);
            Assert.AreEqual(Dressing.Leapers, none.Leaping, "they leap from the start (bible 1.2)");
            Assert.AreEqual(0, none.Gliding, "and nobody glides");

            // In the spine's order (EndingRoutes): one more glider each time, and the newest goes further.
            var order = new[] { Ability.Wingbeat, Ability.Talonhold, Ability.Inkthread, Ability.Clarity, Ability.Windmemory, Ability.Sky };
            var have = Ability.None;
            float last = 0f;
            for (int i = 0; i < order.Length; i++)
            {
                have |= order[i];
                var s = Dressing.At(shore, have);
                Assert.AreEqual(i + 1, s.Gliding, "after " + order[i]);
                Assert.Greater(s.Farthest, last, "a little further after " + order[i]);
                last = s.Farthest;
                Assert.IsFalse(s.OneFlies, "nobody flies before the end");
            }
            Assert.AreEqual(Dressing.Leapers, order.Length, "one leaper for each piece of the sky");

            // Held places live on; anchored ones leap the same leap forever, Halden since before the story.
            Assert.AreEqual(3, Dressing.At(shore, Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread, PlaceFate.Held).Gliding);
            Assert.AreEqual(0, Dressing.At(shore, have, PlaceFate.Anchored).Gliding, "anchoring stops everything else too");
            Assert.AreEqual(0, Dressing.At(Dressing.LoopOf(Region.Halden), have).Gliding, "Halden's fledglings never get any further");
            Assert.AreEqual(Dressing.Leapers, Dressing.At(Dressing.LoopOf(Region.Halden), have).Leaping, "though they still leap");

            // Forgetting is subtraction; the faded don't fade further.
            Assert.AreEqual(Dressing.Leapers / 2, Dressing.At(shore, have, fadeStage: 2).Leaping);
            Assert.AreEqual(Dressing.Leapers / 2, Dressing.At(shore, have, fadeStage: 2).Gliding);
            Assert.AreEqual(0, Dressing.At(shore, have, fadeStage: 3).Leaping, "stage 3: nobody left to leap");
            Assert.AreEqual(Dressing.Leapers, Dressing.At(Dressing.LoopOf(Region.Blank), have, fadeStage: 4).Leaping, "the Remnant's chicks leap in the white");

            // The endings (bible 9.1, 9.2).
            Assert.AreEqual(0, Dressing.At(shore, have, ending: Ending.Fixed).Gliding, "the Atlas holds the sky for them");
            var open = Dressing.At(Dressing.LoopOf(Region.Halden), Ability.None, ending: Ending.Open);
            Assert.AreEqual(Dressing.Leapers, open.Gliding, "the Open World: they all begin to remember, even in Halden");
            Assert.IsTrue(open.OneFlies, "and one of them doesn't come down");
            Assert.AreEqual(4, Dressing.At(shore, Ability.Wingbeat | Ability.Talonhold | Ability.Inkthread | Ability.Windmemory, ending: Ending.Unwritten).Gliding,
                "the Unwritten keeps what she remembered");
        }
    }
}
