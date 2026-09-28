using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;

namespace OWSBG.Tests
{
    /// <summary>
    /// The full-playthrough matrix as data (PRO-05, docs/design/playthrough-matrix.md): each route cut into legs in
    /// the story's order, every order the map allows made from them, each a permutation of its route, the sequence
    /// breaks entering by a soft gap and nothing else, and the matrix named once each.
    /// </summary>
    public class PlaythroughsTests
    {
        static bool NoFlags(string _) => false;

        [Test]
        public void EveryRouteCutsIntoLegsInTheStorysOrder()
        {
            foreach (var route in EndingRoutes.All)
            {
                var legs = Playthroughs.LegsOf(route);
                Assert.AreEqual(route.Steps.Length, legs.Length);
                // Each leg is one run: once it ends, it never comes back.
                var seen = new HashSet<Playthroughs.Leg>();
                Playthroughs.Leg? last = null;
                foreach (var l in legs)
                {
                    if (l != last) { Assert.IsTrue(seen.Add(l), route.Ending + ": " + l + " is one run"); last = l; }
                }
                var order = legs.Distinct().ToList();
                Assert.AreEqual(Playthroughs.Leg.Shore, order[0], route.Ending + " starts on the shore");
                Assert.Less(order.IndexOf(Playthroughs.Leg.Emberdown), order.IndexOf(Playthroughs.Leg.ActBreak), route.Ending + ": Emberdown before the act break");
                Assert.Less(order.IndexOf(Playthroughs.Leg.Verdance), order.IndexOf(Playthroughs.Leg.ActBreak), route.Ending + ": Verdance before the act break");
                Assert.Less(order.IndexOf(Playthroughs.Leg.ActBreak), order.IndexOf(Playthroughs.Leg.Halden), route.Ending + ": Halden after it");
                if (order.Contains(Playthroughs.Leg.Windreach))
                    Assert.Less(order.IndexOf(Playthroughs.Leg.ActBreak), order.IndexOf(Playthroughs.Leg.Windreach), route.Ending + ": Windreach after it");
                Assert.Less(order.IndexOf(Playthroughs.Leg.Halden), order.IndexOf(Playthroughs.Leg.Threshold), route.Ending + ": the Threshold after Act 2");
                Assert.AreEqual(Playthroughs.Leg.End, order[order.Count - 1], route.Ending + " ends past the last camp");
                // The legs are where they say: a climb's steps are in its region, Windreach's in Windreach.
                for (int i = 0; i < legs.Length; i++)
                {
                    var z = route.Steps[i].Zone;
                    switch (legs[i])
                    {
                        case Playthroughs.Leg.Emberdown: StringAssert.StartsWith("Emberdown.", z); break;
                        case Playthroughs.Leg.Verdance: StringAssert.StartsWith("Verdance.", z); break;
                        case Playthroughs.Leg.Windreach: StringAssert.StartsWith("Windreach.", z); break;
                        case Playthroughs.Leg.Halden: StringAssert.StartsWith("Halden.", z); break;
                        case Playthroughs.Leg.Shore: StringAssert.StartsWith("Saltmarrow.", z); break;
                    }
                }
            }
        }

        [Test]
        public void EveryOrderIsAPermutationOfItsRoute()
        {
            foreach (Ending e in Enum.GetValues(typeof(Ending)))
            {
                var route = EndingRoutes.For(e);
                if (route == null) continue;
                var expected = route.Steps.OrderBy(s => s.Key).ThenBy(s => string.Join(",", s.Choices)).ToList();
                foreach (Playthroughs.ClimbOrder c in Enum.GetValues(typeof(Playthroughs.ClimbOrder)))
                    foreach (Playthroughs.Act2Order a in Enum.GetValues(typeof(Playthroughs.Act2Order)))
                    {
                        var p = Playthroughs.Build(e, c, a);
                        CollectionAssert.AreEqual(expected, p.Steps.OrderBy(s => s.Key).ThenBy(s => string.Join(",", s.Choices)).ToList(), p.Name + " is the route, reordered");
                        var legs = Playthroughs.LegsOf(new EndingRoute { Ending = e, Steps = p.Steps });
                        var order = legs.Distinct().ToList();
                        int em = order.IndexOf(Playthroughs.Leg.Emberdown), ve = order.IndexOf(Playthroughs.Leg.Verdance);
                        Assert.AreEqual(c == Playthroughs.ClimbOrder.EmberdownFirst, em < ve, p.Name + ": the climbs in the asked order");
                        int ha = order.IndexOf(Playthroughs.Leg.Halden), wi = order.IndexOf(Playthroughs.Leg.Windreach);
                        if (wi >= 0) Assert.AreEqual(a == Playthroughs.Act2Order.HaldenFirst, ha < wi, p.Name + ": Act 2 in the asked order");
                        Assert.IsFalse(p.AllowSoft, p.Name + " crosses no soft gap");
                        Assert.AreEqual(-1, p.BreakStep);
                    }
            }
        }

        [Test]
        public void ASequenceBreakEntersByASoftGapAndNothingElse()
        {
            var pogo = WorldGraph.Reachable(Ability.None, NoFlags, allowSoft: true);
            var walk = WorldGraph.Reachable(Ability.None, NoFlags, allowSoft: false);
            foreach (Ending e in Enum.GetValues(typeof(Ending)))
            {
                if (EndingRoutes.For(e) == null) continue;
                foreach (var b in new[] { Playthroughs.SequenceBreak.EmberdownBeforeTheLamp, Playthroughs.SequenceBreak.VerdanceBeforeTheLamp })
                {
                    var p = Playthroughs.Build(e, Playthroughs.ClimbOrder.EmberdownFirst, Playthroughs.Act2Order.HaldenFirst, b);
                    Assert.IsTrue(p.AllowSoft);
                    Assert.AreEqual(0, p.BreakStep, p.Name + " breaks on its first step");
                    string region = b == Playthroughs.SequenceBreak.EmberdownBeforeTheLamp ? "Emberdown." : "Verdance.";
                    StringAssert.StartsWith(region, p.Steps[0].Zone, p.Name + " starts up the climb");
                    Assert.IsTrue(pogo.Contains(p.Steps[0].Zone), p.Name + ": a skilled pogo reaches " + p.Steps[0].Zone);
                    Assert.IsFalse(walk.Contains(p.Steps[0].Zone), p.Name + ": nobody walks there without Wingbeat");
                    // Up Emberdown before the lamp, the Collapse is met first, without Wingbeat: a finding the doc records.
                    string firstFight = p.Steps.First(s => s.Kind == RouteStepKind.Boss).Key;
                    Assert.IsTrue(firstFight == "lamp_keeper" || (firstFight == "collapse" && region == "Emberdown."), p.Name + ": the first fight is " + firstFight);
                    Assert.Greater(Array.FindIndex(p.Steps, s => s.Key == "lamp_keeper"), p.Steps.Count(s => s.Zone.StartsWith(region)) - 1, p.Name + ": the whole climb is before the lamp");
                    var route = EndingRoutes.For(e);
                    CollectionAssert.AreEquivalent(route.Steps.Select(s => s.Key + ":" + string.Join(",", s.Choices)), p.Steps.Select(s => s.Key + ":" + string.Join(",", s.Choices)), p.Name + " is the route, reordered");
                }
            }
        }

        [Test]
        public void TheMatrixNamesEachPlaythroughOnce()
        {
            var all = Playthroughs.All().ToList();
            Assert.AreEqual(all.Count, all.Select(p => p.Name).Distinct().Count(), "no name twice");
            foreach (var p in all) Assert.AreEqual(p.Steps.Length, Playthroughs.Named(p.Name).Steps.Length, p.Name + " is found by its name");
            // Four orders where both Act 2 legs exist, two where Windreach is not on the route, and two breaks each.
            foreach (Ending e in Enum.GetValues(typeof(Ending)))
            {
                var route = EndingRoutes.For(e);
                if (route == null) continue;
                bool windreach = Playthroughs.LegsOf(route).Contains(Playthroughs.Leg.Windreach);
                int orders = all.Count(p => p.Ending == e && p.Break == Playthroughs.SequenceBreak.None);
                Assert.AreEqual(windreach ? 4 : 2, orders, e + "'s orders");
                Assert.AreEqual(2, all.Count(p => p.Ending == e && p.Break != Playthroughs.SequenceBreak.None), e + "'s breaks");
            }
            Assert.AreEqual(20, all.Count);
            Assert.IsNull(Playthroughs.Named("nothing"));
        }
    }
}
