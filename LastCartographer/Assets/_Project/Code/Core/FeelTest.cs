using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// The controller feel-test (PRO-03, docs/design/feel-test.md), the M0 gate: the course a tester runs, the
    /// questions they answer after, what the game records while they run, and the bar the answers and the numbers
    /// are held to. The course is laid out here as numbers so the test can hold every gap to the controller's reach;
    /// FeelCourseRooms (Narrative) draws it, FeelRecorder (World) keeps the numbers.
    /// </summary>
    public static class FeelTest
    {
        // ---- the course ----

        public enum Station { Run, Hops, Steps, Ceiling, Wall, Dash, Targets }

        public sealed class Ledge { public string Name; public float X0, X1, Y; }
        public sealed class WallBlock { public string Name; public float X, Y0, Y1; }

        public sealed class StationSpec
        {
            public Station Id;
            public string Name;
            /// <summary>The x range the tester is in this station.</summary>
            public float From, To;
            /// <summary>What the tester is asked to do there.</summary>
            public string Asks;
            /// <summary>What it needs; None for the base kit.</summary>
            public Ability Needs;
            public string[] Questions;
        }

        /// <summary>The floor the pits catch on; the recorder puts a fallen tester back at the station.</summary>
        public const float PitY = -6f;
        public const float StartX = -2f;
        public const float EndX = 158f;
        /// <summary>The hops, in order: each gap wider than the last, the last near the reach.</summary>
        public static readonly float[] HopGaps = { 3f, 4f, 5f, 6f };
        public const float DashGap = 9f;
        public const float CeilingHeight = 2.6f;
        public const float StepRise = 1.5f;
        public const float ShaftWidth = 4f, ShaftHeight = 10f;
        public static readonly float[] TargetXs = { 146f, 150f, 154f };

        public static readonly StationSpec[] Stations =
        {
            new StationSpec { Id = Station.Run, Name = "The run", From = 0f, To = 30f, Needs = Ability.None,
                Asks = "Run to the far post and back twice. Turn hard at each end.", Questions = new[] { "goes", "stops", "weight" } },
            new StationSpec { Id = Station.Hops, Name = "The hops", From = 30f, To = 70f, Needs = Ability.None,
                Asks = "Cross the four gaps. Each is wider than the last; the fourth is near her reach.", Questions = new[] { "lands", "late", "early" } },
            new StationSpec { Id = Station.Steps, Name = "The steps", From = 70f, To = 92f, Needs = Ability.None,
                Asks = "Up the four steps with the smallest hops you can, then down the far side.", Questions = new[] { "tap", "lands" } },
            new StationSpec { Id = Station.Ceiling, Name = "The ceiling", From = 92f, To = 104f, Needs = Ability.None,
                Asks = "Get under the low roof and out the far side without hitting your head.", Questions = new[] { "tap" } },
            new StationSpec { Id = Station.Wall, Name = "The shaft", From = 104f, To = 118f, Needs = Ability.Talonhold,
                Asks = "Up the shaft by the walls. If you have no Talonhold, take the ladder path round.", Questions = new[] { "wall" } },
            new StationSpec { Id = Station.Dash, Name = "The dash", From = 118f, To = 140f, Needs = Ability.Wingbeat,
                Asks = "The gap is too wide to jump. Jump, then dash. Try it from a standing start and from a run.", Questions = new[] { "dash" } },
            new StationSpec { Id = Station.Targets, Name = "The targets", From = 140f, To = 160f, Needs = Ability.None,
                Asks = "Three crabs. Strike the first two; pogo the third as many times as you can before it dies.", Questions = new[] { "hits", "pogo" } },
        };

        public static StationSpec Of(Station s) => Stations.First(x => x.Id == s);

        /// <summary>The station an x position is in; the run's before the start, the targets' past the end.</summary>
        public static Station StationAt(float x)
        {
            foreach (var s in Stations) if (x >= s.From && x < s.To) return s.Id;
            return x < 0f ? Station.Run : Station.Targets;
        }

        /// <summary>Where the tester is put back after a pit, by station: its start, on the floor.</summary>
        public static Vector2 RestartOf(Station s)
        {
            var spec = Of(s);
            float y = s == Station.Dash || s == Station.Targets ? ShaftHeight : 0f;
            return new Vector2(spec.From + 1f, y + 0.5f);
        }

        /// <summary>The ledges of the course, left to right.</summary>
        public static List<Ledge> Ledges()
        {
            var l = new List<Ledge>();
            void L(string n, float x0, float x1, float y) => l.Add(new Ledge { Name = n, X0 = x0, X1 = x1, Y = y });
            L("Start", -4f, 0f, 0f);
            L("Run", 0f, 30f, 0f);
            // The hops: a 4-wide ledge, then the gap, from x = 30.
            float x = 30f;
            L("Hop0", x, x + 4f, 0f); x += 4f;
            for (int i = 0; i < HopGaps.Length; i++)
            {
                x += HopGaps[i];
                float w = i == HopGaps.Length - 1 ? 70f - x : 4f;
                L("Hop" + (i + 1), x, x + w, 0f);
                x += w;
            }
            // The steps: four rises of StepRise, four wide each, then the drop.
            for (int i = 0; i < 4; i++) L("Step" + i, 70f + i * 4f, 74f + i * 4f, (i + 1) * StepRise);
            L("StepDown", 86f, 92f, 0f);
            // The ceiling: a floor under the low roof.
            L("CeilingFloor", 92f, 104f, 0f);
            L("CeilingRoof", 95f, 101f, CeilingHeight);   // drawn as a block hanging from this height up
            // The shaft: floor at its foot, the landing at its head, and the ladder path's ledges for those without Talonhold.
            L("ShaftFoot", 104f, 106f, 0f);
            L("ShaftHead", 110f, 118f, ShaftHeight);
            L("Ladder0", 104f, 105.5f, 3.3f); L("Ladder1", 105.5f, 107f, 6.6f); L("Ladder2", 107f, 110f, ShaftHeight);
            // The dash: a gap no jump crosses.
            L("DashFrom", 118f, 122f, ShaftHeight);
            L("DashTo", 122f + DashGap, 140f, ShaftHeight);
            // The targets and the end post.
            L("Targets", 140f, 160f, ShaftHeight);
            return l;
        }

        public static List<WallBlock> Walls() => new List<WallBlock>
        {
            new WallBlock { Name = "ShaftWest", X = 106f, Y0 = 0f, Y1 = ShaftHeight },
            new WallBlock { Name = "ShaftEast", X = 106f + ShaftWidth, Y0 = 0f, Y1 = ShaftHeight + 2f },
        };

        /// <summary>
        /// How far a full jump carries at full run, from the controller's numbers: up in timeToApex, down faster by
        /// the fall multiplier, plus the apex hang, all at run speed. Wingbeat adds its dash on top.
        /// </summary>
        public static float JumpReach(float runSpeed, float timeToApex, float fallGravityMultiplier, int apexHangFrames)
        {
            float down = timeToApex / Mathf.Sqrt(Mathf.Max(0.01f, fallGravityMultiplier));
            return runSpeed * (timeToApex + down + apexHangFrames / 60f);
        }

        // ---- the questions ----

        public sealed class Question { public string Id; public string Text; public string Low, High; }

        /// <summary>One to five each. Low and High are the ends of the scale, in the tester's words.</summary>
        public static readonly Question[] Questions =
        {
            new Question { Id = "goes", Text = "She goes when I press.", Low = "late", High = "at once" },
            new Question { Id = "stops", Text = "She stops and turns where I mean.", Low = "slides", High = "on the spot" },
            new Question { Id = "weight", Text = "She has the right weight.", Low = "floaty or leaden", High = "right" },
            new Question { Id = "lands", Text = "Jumps land where I mean.", Low = "rarely", High = "always" },
            new Question { Id = "tap", Text = "A tap makes a short hop; a hold, a tall one.", Low = "no difference", High = "clearly" },
            new Question { Id = "late", Text = "A jump pressed just after the edge still jumps.", Low = "she falls", High = "it jumps" },
            new Question { Id = "early", Text = "A jump pressed just before landing still jumps.", Low = "it's lost", High = "it jumps" },
            new Question { Id = "wall", Text = "The wall holds and the wall-jump goes where I mean.", Low = "no", High = "yes" },
            new Question { Id = "dash", Text = "The dash goes as far and as fast as I expect.", Low = "no", High = "yes" },
            new Question { Id = "hits", Text = "A hit feels like it lands.", Low = "nothing", High = "it lands" },
            new Question { Id = "pogo", Text = "The pogo is readable and I can chain it.", Low = "no", High = "yes" },
            new Question { Id = "hour", Text = "I would play this controller for an hour.", Low = "no", High = "yes" },
        };

        public static Question QuestionOf(string id) => Questions.First(q => q.Id == id);

        // ---- what the game records ----

        [Serializable]
        public sealed class Session
        {
            public string version, device, tester;
            public float seconds;
            public string[] actions = Array.Empty<string>();
            public int[] presses = Array.Empty<int>(), acted = Array.Empty<int>(), dropped = Array.Empty<int>();
            /// <summary>Jumps by the frames they waited in the buffer: index 0 is the same frame.</summary>
            public int[] jumpWaits = new int[8];
            public int coyoteJumps, wallJumps, dashes, pogos, landings, falls, hits;
            public string[] stations = Array.Empty<string>();
            public float[] stationSeconds = Array.Empty<float>();
            public int[] stationFalls = Array.Empty<int>();

            public int Presses(string action) { int i = Array.IndexOf(actions, action); return i < 0 ? 0 : presses[i]; }
            public int Dropped(string action) { int i = Array.IndexOf(actions, action); return i < 0 ? 0 : dropped[i]; }
            public int BufferedJumps { get { int n = 0; for (int i = 1; i < jumpWaits.Length; i++) n += jumpWaits[i]; return n; } }
            /// <summary>Presses that did nothing, over every press.</summary>
            public float DroppedShare { get { int p = presses.Sum(), d = dropped.Sum(); return p == 0 ? 0f : (float)d / p; } }
            public string ToJson() => JsonUtility.ToJson(this, true);
            public static Session FromJson(string json) => JsonUtility.FromJson<Session>(json);
        }

        // ---- the gate ----

        /// <summary>Answers from one tester: one to five for each question, by id.</summary>
        public sealed class Answers
        {
            public string Tester;
            public Dictionary<string, int> Scores = new Dictionary<string, int>();
            public Session Session;
        }

        public const int MinTesters = 5;
        public const int PassMedian = 4;
        public const int FloorMedian = 3;
        /// <summary>Presses that did nothing, over every press, across the testers: above this the buffers are wrong.</summary>
        public const float DroppedShareMax = 0.10f;

        public sealed class GateReport
        {
            public bool Passed;
            public List<string> Reasons = new List<string>();
            public Dictionary<string, float> Medians = new Dictionary<string, float>();
            public float DroppedShare;
        }

        public static float Median(IEnumerable<int> values)
        {
            var v = values.OrderBy(x => x).ToArray();
            if (v.Length == 0) return 0f;
            return v.Length % 2 == 1 ? v[v.Length / 2] : (v[v.Length / 2 - 1] + v[v.Length / 2]) / 2f;
        }

        /// <summary>
        /// M0's gate: enough testers; every question's median at the pass mark, none below the floor; and the
        /// presses that did nothing under the share. A question nobody could answer (no Talonhold for the wall) is
        /// left out, not failed.
        /// </summary>
        public static GateReport Gate(IReadOnlyList<Answers> answers)
        {
            var r = new GateReport { Passed = true };
            if (answers.Count < MinTesters) { r.Passed = false; r.Reasons.Add(answers.Count + " testers; the gate wants " + MinTesters); }
            foreach (var q in Questions)
            {
                var scores = answers.Where(a => a.Scores.ContainsKey(q.Id)).Select(a => Mathf.Clamp(a.Scores[q.Id], 1, 5)).ToList();
                if (scores.Count == 0) continue;
                float m = Median(scores);
                r.Medians[q.Id] = m;
                if (m < FloorMedian) { r.Passed = false; r.Reasons.Add("\"" + q.Text + "\" is at " + m + ", under the floor of " + FloorMedian); }
                else if (m < PassMedian) { r.Passed = false; r.Reasons.Add("\"" + q.Text + "\" is at " + m + ", under the pass mark of " + PassMedian); }
            }
            int presses = 0, dropped = 0;
            foreach (var a in answers)
            {
                if (a.Session == null) continue;
                presses += a.Session.presses.Sum();
                dropped += a.Session.dropped.Sum();
            }
            r.DroppedShare = presses == 0 ? 0f : (float)dropped / presses;
            if (r.DroppedShare > DroppedShareMax) { r.Passed = false; r.Reasons.Add((r.DroppedShare * 100f).ToString("0") + "% of presses did nothing; at most " + (DroppedShareMax * 100f).ToString("0") + "%"); }
            return r;
        }
    }
}
