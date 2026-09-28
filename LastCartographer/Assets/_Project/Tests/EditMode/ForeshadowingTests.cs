using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The foreshadowing audit and <c>#still</c> (NAR-16, docs/story/foreshadowing.md), read from the shipped Yarn with
    /// <see cref="YarnAudit"/>. Every secret is revealed on one tagged line in the scene the bible says, planted three
    /// times in three scenes before it whichever climb Act 1 took, at least once on the story's own road, and never
    /// "planted" after it (that is a callback). A line tagged <c>#still</c> can be heard again unchanged, and every line
    /// a revisit repeats is tagged.
    /// </summary>
    public class ForeshadowingTests
    {
        static YarnAudit.Project _project;

        static YarnAudit.Project Project()
        {
            if (_project != null) return _project;
            var p = new YarnAudit.Project();
            string root = Path.Combine(Application.dataPath, "_Project");
            foreach (var f in Directory.GetFiles(Path.Combine(root, "Dialogue"), "*.yarn", SearchOption.AllDirectories))
                p.Add(Path.GetFileName(f), File.ReadAllText(f));
            // Flags the game's code reads route things too: setting one is a change even if no script reads it.
            string code = string.Join("\n", Directory.GetFiles(Path.Combine(root, "Code"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
            p.AddCodeReads(p.WrittenFlags.Where(f => code.Contains("\"" + f + "\"")).ToList());
            return _project = p;
        }

        static IEnumerable<(YarnAudit.YLine line, string secret)> Tagged(string tag)
            => Project().Lines.SelectMany(l => l.Values(tag).Select(v => (l, v)));

        static List<(string node, string secret)> Plants()
            => Tagged("plant").Where(t => t.secret.StartsWith("5.")).Select(t => (t.line.Node, t.secret)).ToList();

        [Test]
        public void EverySecretIsRevealedOnceWhereTheBibleSays()
        {
            var reveals = Tagged("reveal").ToList();
            foreach (var s in Foreshadowing.Secrets)
            {
                var r = reveals.Where(t => t.secret == s).ToList();
                Assert.AreEqual(1, r.Count, s + " (" + Foreshadowing.Title(s) + ") is revealed on one line");
                Assert.AreEqual(Foreshadowing.RevealNode[s], r[0].line.Node, s + " is revealed where the bible says");
            }
            Assert.IsTrue(reveals.All(t => Foreshadowing.Secrets.Contains(t.secret)), "only bible 5's secrets are revealed");
            Assert.AreEqual(Beat.Orchard, Foreshadowing.RevealBeat("5.1"), "bible 7.1.5: the Stillness at Act 1's end");
            foreach (var s in Foreshadowing.Secrets.Skip(1))
                Assert.GreaterOrEqual(Foreshadowing.RevealBeat(s), Beat.LastCamp, s + " is Act 3's");
        }

        [Test]
        public void EveryScenePlantingASecretIsPlaced()
        {
            var nodes = Tagged("plant").Concat(Tagged("reveal")).Concat(Tagged("callback"))
                .Where(t => t.secret.StartsWith("5.")).Select(t => t.line.Node).Distinct();
            foreach (var n in nodes) Assert.IsTrue(Foreshadowing.IsPlaced(n), n + " plants, reveals or calls back a secret: where does it fall?");
            foreach (var n in Foreshadowing.PlacedNodes) Assert.IsTrue(Project().Nodes.ContainsKey(n), n + " is a scene");
        }

        [Test]
        public void EverySecretIsPlantedThreeTimesBeforeItsReveal()
        {
            var plants = Plants();
            foreach (var s in Foreshadowing.Secrets)
                foreach (var climb in new[] { ClimbRoute.Emberdown, ClimbRoute.Verdance })
                {
                    var before = Foreshadowing.PlantsBefore(s, climb, plants);
                    Assert.GreaterOrEqual(before.Count, 3,
                        $"bible 11.1, the rule of three: {s} ({Foreshadowing.Title(s)}) climbing {climb} first has {before.Count}: {string.Join(", ", before)}");
                }
        }

        [Test]
        public void APlantComesBeforeItsRevealAndACallbackAfter()
        {
            foreach (var (line, s) in Tagged("plant").Where(t => t.secret.StartsWith("5.")))
            {
                var reveal = Foreshadowing.RevealBeat(s);
                // A climb's plant comes after 5.1 for a player who climbed the other way first: it is a plant on one road.
                if (Foreshadowing.IsPlaced(line.Node))
                    Assert.IsTrue(new[] { ClimbRoute.Emberdown, ClimbRoute.Verdance }.Any(c => Foreshadowing.BeatOf(line.Node, c) <= reveal),
                        line + " plants " + s + " after it is revealed on every road: a #callback");
                if (line.Node == Foreshadowing.RevealNode[s])
                {
                    var r = Tagged("reveal").First(t => t.secret == s).line;
                    Assert.Less(line.LineNo, r.LineNo, line + " is in the reveal's scene: it has to come before the reveal");
                }
            }
            foreach (var (line, s) in Tagged("callback"))
            {
                var reveal = Foreshadowing.RevealBeat(s);
                var beat = Foreshadowing.BeatOf(line.Node, ClimbRoute.Either);
                if (line.Node == Foreshadowing.RevealNode[s])
                    Assert.Greater(line.LineNo, Tagged("reveal").First(t => t.secret == s).line.LineNo, line + " calls back before the reveal");
                else
                    Assert.Greater(beat, reveal, line + " calls back " + s + " before it is revealed: a #plant");
            }
        }

        [Test]
        public void TheStorysOwnRoadPlantsEverySecret()
        {
            // Every ending's route (EndingRoutes, DES-12) passes at least one plant of each secret before its reveal.
            // Corra is found only if Wren looks (bible 7.3.4), so a route may pass a reveal by; one that reaches it planted it.
            var plantNodes = Plants().GroupBy(p => p.secret).ToDictionary(g => g.Key, g => new HashSet<string>(g.Select(p => p.node)));
            foreach (var s in Foreshadowing.Secrets)
            {
                int reaching = 0;
                foreach (var route in EndingRoutes.All)
                {
                    var seen = new HashSet<string>();
                    bool revealed = false;
                    foreach (var step in route.Steps.Where(st => st.Kind == RouteStepKind.Talk))
                    {
                        var reach = Reach(step.Key);
                        if (reach.Contains(Foreshadowing.RevealNode[s])) { revealed = true; break; }
                        seen.UnionWith(reach);
                    }
                    if (!revealed) continue;
                    reaching++;
                    Assert.IsTrue(seen.Overlaps(plantNodes[s]), route.Ending + "'s road plants " + s + " before it is revealed");
                }
                Assert.Greater(reaching, 0, "some ending's road reaches " + s + "'s reveal");
            }
        }

        /// <summary>A scene and every scene it jumps on to.</summary>
        static HashSet<string> Reach(string node)
        {
            var set = new HashSet<string>();
            var q = new Queue<string>();
            q.Enqueue(node);
            while (q.Count > 0)
            {
                var n = q.Dequeue();
                if (!set.Add(n) || !Project().Nodes.TryGetValue(n, out var yn)) continue;
                foreach (var j in yn.Jumps) q.Enqueue(j);
            }
            return set;
        }

        // ---- #still -----------------------------------------------------------------------------------------------------

        [Test]
        public void AStillLineCanBeHeardAgain()
        {
            var wrong = Project().Lines.Where(l => l.Has("still") && !Project().IsStanding(l)).Select(l => l.ToString()).ToList();
            CollectionAssert.IsEmpty(wrong, "#still on a line no visit repeats: every way to it changes something");
        }

        [Test]
        public void EveryLineARevisitRepeatsIsStill()
        {
            var p = Project();
            var revisited = p.RevisitedNodes();
            var missing = p.Lines.Where(l => !l.IsOption && revisited.Contains(l.Node) && p.IsStanding(l) && !l.Has("still"))
                                 .Select(l => l.ToString()).ToList();
            CollectionAssert.IsEmpty(missing, "heard again, unchanged, on the next visit: tag it #still so QA knows it is meant");
            Assert.Greater(revisited.Count, 50, "most conversations know they will be had again");
        }

        // ---- the reader itself -----------------------------------------------------------------------------------------

        const string Sample = @"title: Sample
---
<<if has_flag(""s.done"")>>
    A: Same as ever. #still #line:1
    <<stop>>
<<endif>>
A: Hello. #line:2
-> Ask. #line:3
    A: An answer. #line:4
-> Decide. #line:5
    A: Decided. #line:6
    <<flag s.done 1>>
<<flag s.seen 1>>
<<jump Sample_After>>
===
title: Sample_After
---
<<voice drift test>>
B: After. #line:7
===
";

        [Test]
        public void TheReaderFollowsBranchesChoicesAndJumps()
        {
            var p = new YarnAudit.Project();
            p.Add("Sample.yarn", Sample);
            var n = p.Nodes["Sample"];
            Assert.AreEqual(new[] { "Same as ever.", "Hello.", "Ask.", "An answer.", "Decide.", "Decided." }, n.Lines.Select(l => l.Text).ToArray());
            Assert.AreEqual("A", n.Lines[0].Speaker);
            Assert.IsTrue(n.Lines[0].Has("still"));
            Assert.AreEqual(new[] { "has_flag(\"s.done\")" }, n.Lines[0].Conditions.ToArray());
            Assert.IsTrue(n.Lines[2].IsOption);
            CollectionAssert.AreEquivalent(new[] { "s.done", "s.seen" }, n.Writes);
            Assert.IsTrue(n.KnowsRevisits, "it reads a flag it writes");
            CollectionAssert.AreEqual(new[] { "Sample_After" }, n.Jumps);

            // s.seen is read by nobody: setting it again changes nothing. s.done routes the next visit.
            Assert.IsTrue(p.IsStanding(n.Lines[0]), "the after-line");
            Assert.IsTrue(p.IsStanding(n.Lines[1]), "the greeting, on the way to asking");
            Assert.IsTrue(p.IsStanding(n.Lines[3]), "asking changes nothing");
            Assert.IsFalse(p.IsStanding(n.Lines[5]), "deciding does");
            Assert.IsTrue(p.IsStanding(p.Nodes["Sample_After"].Lines[0]), "voice is neutral");
            CollectionAssert.AreEquivalent(new[] { "Sample", "Sample_After" }, p.RevisitedNodes(), "and the node it jumps on to");

            p.AddCodeReads(new[] { "s.seen" });
            Assert.IsFalse(p.IsStanding(n.Lines[1]), "once the game reads s.seen, the first visit changes it");
            Assert.IsTrue(p.IsStanding(n.Lines[0]), "the after-line stops before it");
        }
    }
}
