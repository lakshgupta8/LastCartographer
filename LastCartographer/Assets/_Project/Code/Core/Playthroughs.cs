using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>
    /// The full-playthrough matrix (PRO-05, docs/design/playthrough-matrix.md): every ending's route cut into legs,
    /// the legs put in every order the map allows (either climb first in Act 1, Halden or Windreach first in Act 2),
    /// and the sequence breaks the map's soft gaps permit (a climb before the Lamp-Keeper). Each playthrough is a
    /// step list like an <see cref="EndingRoute"/>'s, replayed through the shipped scripts by PlaythroughMatrixTests.
    /// </summary>
    public static class Playthroughs
    {
        /// <summary>A run of steps on a route: where the story is, so the order can be changed where the map allows.</summary>
        public enum Leg { Shore, Emberdown, Verdance, ActBreak, Halden, Windreach, Threshold, End }

        public enum ClimbOrder { EmberdownFirst, VerdanceFirst }
        public enum Act2Order { HaldenFirst, WindreachFirst }
        /// <summary>A soft Wingbeat gap crossed by skill: a climb entered before the Lamp-Keeper gave Wingbeat.</summary>
        public enum SequenceBreak { None, EmberdownBeforeTheLamp, VerdanceBeforeTheLamp }

        public sealed class Playthrough
        {
            public Ending Ending;
            public ClimbOrder Climb;
            public Act2Order Act2;
            public SequenceBreak Break;
            public RouteStep[] Steps;
            /// <summary>Soft gaps may be crossed: only on a sequence break.</summary>
            public bool AllowSoft => Break != SequenceBreak.None;
            /// <summary>On a break, the first step that is only reachable by the soft gap; else -1.</summary>
            public int BreakStep = -1;
            public string Name =>
                Ending + (Break != SequenceBreak.None ? "/" + Break : "/" + Climb + "/" + Act2);
            public override string ToString() => Name;
        }

        /// <summary>The keys that mark the act break and the Threshold, so the legs are cut by the story, not by geography.</summary>
        public const string OrchardKey = "Orchard_Isolde_Cache", EdgeKey = "Edge_Pell_Watch", NoticeKey = "EdgeCamp_Notice", LastCampKey = "LastCamp_Isolde";

        /// <summary>Each step's leg, in the route's order.</summary>
        public static Leg[] LegsOf(EndingRoute route)
        {
            var legs = new Leg[route.Steps.Length];
            var leg = Leg.Shore;
            bool broke = false, threshold = false, ended = false;
            for (int i = 0; i < route.Steps.Length; i++)
            {
                var s = route.Steps[i];
                if (ended) leg = Leg.End;
                else if (threshold) { leg = Leg.Threshold; if (s.Key == LastCampKey) ended = true; }
                else if (s.Key == NoticeKey) { leg = Leg.Threshold; threshold = true; }
                else if (s.Key == OrchardKey || s.Key == EdgeKey) { leg = Leg.ActBreak; broke = true; }
                else if (!broke && s.Zone.StartsWith("Emberdown.", StringComparison.Ordinal)) leg = Leg.Emberdown;
                else if (!broke && s.Zone.StartsWith("Verdance.", StringComparison.Ordinal)) leg = Leg.Verdance;
                else if (!broke) leg = Leg.Shore;
                else if (s.Zone.StartsWith("Windreach.", StringComparison.Ordinal)) leg = Leg.Windreach;
                else leg = Leg.Halden;
                legs[i] = leg;
            }
            return legs;
        }

        static List<RouteStep> Run(EndingRoute route, Leg[] legs, Leg leg)
        {
            var list = new List<RouteStep>();
            for (int i = 0; i < legs.Length; i++) if (legs[i] == leg) list.Add(route.Steps[i]);
            return list;
        }

        /// <summary>One playthrough: the route's legs in this order, with this break.</summary>
        public static Playthrough Build(Ending ending, ClimbOrder climb, Act2Order act2, SequenceBreak brk = SequenceBreak.None)
        {
            var route = EndingRoutes.For(ending);
            if (route == null) return null;
            var legs = LegsOf(route);
            var shore = Run(route, legs, Leg.Shore);
            var emberdown = Run(route, legs, Leg.Emberdown);
            var verdance = Run(route, legs, Leg.Verdance);
            var actBreak = Run(route, legs, Leg.ActBreak);
            var halden = Run(route, legs, Leg.Halden);
            var windreach = Run(route, legs, Leg.Windreach);
            var threshold = Run(route, legs, Leg.Threshold);
            var end = Run(route, legs, Leg.End);

            var steps = new List<RouteStep>();
            int breakStep = -1;
            if (brk == SequenceBreak.EmberdownBeforeTheLamp) { breakStep = 0; steps.AddRange(emberdown); emberdown.Clear(); }
            if (brk == SequenceBreak.VerdanceBeforeTheLamp) { breakStep = 0; steps.AddRange(verdance); verdance.Clear(); }
            steps.AddRange(shore);
            if (climb == ClimbOrder.EmberdownFirst) { steps.AddRange(emberdown); steps.AddRange(verdance); }
            else { steps.AddRange(verdance); steps.AddRange(emberdown); }
            steps.AddRange(actBreak);
            if (act2 == Act2Order.HaldenFirst) { steps.AddRange(halden); steps.AddRange(windreach); }
            else { steps.AddRange(windreach); steps.AddRange(halden); }
            steps.AddRange(threshold);
            steps.AddRange(end);
            return new Playthrough { Ending = ending, Climb = climb, Act2 = act2, Break = brk, Steps = steps.ToArray(), BreakStep = breakStep };
        }

        static string Signature(Playthrough p) => string.Join("|", p.Steps.Select(s => s.Key + ":" + string.Join(",", s.Choices)));

        /// <summary>
        /// The matrix: every ending in every order the map allows, and every sequence break, each once (an ending
        /// with no Windreach leg has one Act 2 order, not two).
        /// </summary>
        public static IEnumerable<Playthrough> All()
        {
            var seen = new HashSet<string>();
            foreach (Ending e in Enum.GetValues(typeof(Ending)))
            {
                if (EndingRoutes.For(e) == null) continue;
                foreach (ClimbOrder c in Enum.GetValues(typeof(ClimbOrder)))
                    foreach (Act2Order a in Enum.GetValues(typeof(Act2Order)))
                    {
                        var p = Build(e, c, a);
                        if (seen.Add(Signature(p))) yield return p;
                    }
                foreach (SequenceBreak b in Enum.GetValues(typeof(SequenceBreak)))
                {
                    if (b == SequenceBreak.None) continue;
                    var p = Build(e, ClimbOrder.EmberdownFirst, Act2Order.HaldenFirst, b);
                    if (seen.Add(Signature(p))) yield return p;
                }
            }
        }

        public static Playthrough Named(string name) => All().FirstOrDefault(p => p.Name == name);
        public static IEnumerable<string> Names() => All().Select(p => p.Name);
    }
}
