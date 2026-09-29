using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>What the performance probe writes (PRG-24, <c>PerfProbe</c>): the machine, each room's sample, each transition.</summary>
    [Serializable]
    public sealed class PerfReport
    {
        [Serializable]
        public sealed class Room
        {
            public string room;
            public int frames;
            public float avgMs, p50Ms, p95Ms, p99Ms, maxMs, fps;
            public float batches, setPass, triangles;
            public float gcBytesPerFrame = -1f;
            public int gcCollections;
            /// <summary>Collections between arriving and sampling: the load's garbage, not the quiet frame's. Not budgeted.</summary>
            public int loadCollections;
        }

        [Serializable]
        public sealed class Transition
        {
            public string from, to;
            public float ms;
        }

        public string version, device, cpu, graphics, resolution;
        public int refreshHz;
        public bool rendering;
        /// <summary>The card's memory in MB, and how it classes against the target (<see cref="PerfTarget"/>).</summary>
        public int vramMb;
        public float gpuScore;
        public string gpuClass;
        public List<Room> rooms = new List<Room>();
        public List<Transition> transitions = new List<Transition>();
        public List<string> overBudget = new List<string>();
        public bool passed;

        public static PerfReport FromJson(string json) => JsonUtility.FromJson<PerfReport>(json);
        public string ToJson() => JsonUtility.ToJson(this, true);
    }

    /// <summary>
    /// PRO-06's target, and whether a report meets it: 60 fps at 1920×1080 on a GTX 1060-class card, and every room
    /// transition under 100 ms (<see cref="PerfBudget"/>). A report proves it only from a card at or below the target:
    /// a faster card passing says little about a slower one, so its report is <b>projected</b> instead (its frame times
    /// scaled by how much slower the target is) and counts as a warning sign, never a pass.
    /// </summary>
    public static class PerfTarget
    {
        public enum GpuClass { Unknown, Below, Target, Above }

        public const int Width = 1920, Height = 1080;
        /// <summary>The rooms a report must have sampled: the probe's full route.</summary>
        public const int Rooms = 6;
        /// <summary>A card within this band of the GTX 1060 6GB's score is the target class.</summary>
        public const float TargetLow = 0.85f, TargetHigh = 1.10f;

        /// <summary>
        /// Rough rasterisation performance relative to a GTX 1060 6GB (1.0), from public benchmark aggregates, rounded.
        /// For classing a report, not for benchmarking: a card's laptop and desktop parts, clocks and drivers vary more
        /// than these steps. Most specific pattern first.
        /// </summary>
        public static readonly (string pattern, float score)[] Cards =
        {
            // NVIDIA, desktop and laptop
            (@"GTX 1050 Ti", 0.62f), (@"GTX 1050", 0.48f),
            (@"GTX 1060.*3\s?GB", 0.93f), (@"GTX 1060", 1.00f),
            (@"GTX 1070 Ti", 1.55f), (@"GTX 1070", 1.40f), (@"GTX 1080 Ti", 2.10f), (@"GTX 1080", 1.70f),
            (@"GTX 960", 0.55f), (@"GTX 970", 0.95f), (@"GTX 980 Ti", 1.25f), (@"GTX 980", 1.10f),
            (@"GTX 1650 SUPER", 1.02f), (@"GTX 1650", 0.78f),
            (@"GTX 1660 (Ti|SUPER)", 1.30f), (@"GTX 1660", 1.15f),
            (@"RTX 2050", 0.80f), (@"RTX 2060", 1.45f), (@"RTX 2070", 1.80f), (@"RTX 2080", 2.10f),
            (@"RTX 3050.*Laptop", 1.20f), (@"RTX 3050", 1.35f), (@"RTX 3060.*Laptop", 1.75f), (@"RTX 3060", 1.85f),
            (@"RTX 3070", 2.40f), (@"RTX 3080", 2.90f), (@"RTX 3090", 3.20f),
            (@"RTX 4050", 1.80f), (@"RTX 4060", 2.10f), (@"RTX 4070", 2.90f), (@"RTX 4080", 3.70f), (@"RTX 4090", 4.50f),
            // AMD
            (@"RX 460\b", 0.45f), (@"RX 470\b", 0.85f), (@"RX 480\b", 0.95f), (@"RX 560\b", 0.50f), (@"RX 570\b", 0.90f), (@"RX 580\b", 1.00f), (@"RX 590\b", 1.10f),
            (@"RX 5500", 1.05f), (@"RX 5600", 1.45f), (@"RX 5700", 1.80f),
            (@"RX 6500", 0.85f), (@"RX 6600", 1.65f), (@"RX 6700", 2.20f), (@"RX 7600", 2.10f),
            (@"Radeon 780M", 0.70f), (@"Radeon 680M", 0.60f), (@"Vega 8\b", 0.25f), (@"Vega 11", 0.30f),
            (@"Van Gogh|Steam Deck", 0.45f),
            // Intel
            (@"Arc A380", 0.85f), (@"Arc A750", 2.00f), (@"Arc A770", 2.20f),
            (@"Iris(\(R\))? Xe", 0.30f), (@"UHD Graphics", 0.12f),
        };

        /// <summary>A card's score and class by its name as Unity reports it ("NVIDIA GeForce GTX 1060 6GB"), or Unknown.</summary>
        public static (float score, GpuClass cls) Classify(string graphicsName)
        {
            if (string.IsNullOrEmpty(graphicsName)) return (0f, GpuClass.Unknown);
            foreach (var (pattern, score) in Cards)
                if (Regex.IsMatch(graphicsName, pattern, RegexOptions.IgnoreCase))
                    return (score, score < TargetLow ? GpuClass.Below : score > TargetHigh ? GpuClass.Above : GpuClass.Target);
            return (0f, GpuClass.Unknown);
        }

        /// <summary>
        /// A frame time on this card as it would be on the target, if the frame is the GPU's (scaled by the scores).
        /// An estimate for faster cards: a CPU-bound frame doesn't scale, so this errs high, which is the safe way.
        /// </summary>
        public static float Projected(float ms, float score) => score > 0f ? ms * score : ms;

        public sealed class Verdict
        {
            public string Source = "";
            public GpuClass Class;
            public float Score;
            /// <summary>This report proves PRO-06: the target class or below, rendered at 1080p, the whole route, all within budget.</summary>
            public bool Proves;
            /// <summary>Within budget as measured (on whatever card).</summary>
            public bool WithinBudget;
            /// <summary>The worst room's p95 as measured, and as projected onto the target.</summary>
            public float WorstP95, ProjectedP95;
            public float WorstTransition;
            public readonly List<string> Reasons = new List<string>();
        }

        /// <summary>One report against PRO-06.</summary>
        public static Verdict Judge(PerfReport r, string source = "")
        {
            var v = new Verdict { Source = source };
            if (r == null) { v.Reasons.Add("unreadable"); return v; }
            string gpuName = r.graphics ?? "";
            int paren = gpuName.LastIndexOf(" (", StringComparison.Ordinal);
            if (paren > 0) gpuName = gpuName.Substring(0, paren);
            (v.Score, v.Class) = Classify(gpuName);
            v.WorstP95 = r.rooms.Count == 0 ? 0f : r.rooms.Max(x => x.p95Ms);
            v.ProjectedP95 = v.Class == GpuClass.Above ? Projected(v.WorstP95, v.Score) : v.WorstP95;
            v.WorstTransition = r.transitions.Count == 0 ? 0f : r.transitions.Max(t => t.ms);

            if (!r.rendering) v.Reasons.Add("not rendered (a -batchmode run measures the CPU side only)");
            if (r.resolution != Width + "x" + Height) v.Reasons.Add("at " + r.resolution + ", not " + Width + "x" + Height);
            if (r.rooms.Count < Rooms) v.Reasons.Add(r.rooms.Count + " rooms sampled of " + Rooms);
            foreach (var room in r.rooms)
                if (room.p95Ms > PerfBudget.FrameMs) v.Reasons.Add(room.room + ": p95 " + F(room.p95Ms) + " ms");
            foreach (var t in r.transitions)
                if (!PerfBudget.TransitionWithin(t.ms)) v.Reasons.Add(t.from + " → " + t.to + ": " + F(t.ms) + " ms");
            // The probe's own findings the frame and transition checks above don't cover: garbage, batches, a room that never came.
            foreach (var o in r.overBudget)
                if (o.Contains("GC") || o.Contains("collections") || o.Contains("batches") || o.Contains("never came in")) v.Reasons.Add(o);
            v.WithinBudget = v.Reasons.Count == 0;

            switch (v.Class)
            {
                case GpuClass.Unknown: v.Reasons.Add("the card (" + gpuName + ") isn't in the table: add it to PerfTarget.Cards"); break;
                case GpuClass.Above:
                    v.Reasons.Add(gpuName + " is " + F(v.Score) + "× a GTX 1060: faster than the target, so it can't prove it" +
                                  (v.ProjectedP95 > PerfBudget.FrameMs ? "; projected p95 " + F(v.ProjectedP95) + " ms would be over" : ""));
                    break;
            }
            v.Proves = v.WithinBudget && (v.Class == GpuClass.Target || v.Class == GpuClass.Below);
            return v;
        }

        // ---- the gate ------------------------------------------------------------------------------------------------

        /// <summary>Every *.json in the folder that reads as a report, by file name.</summary>
        public static List<(string file, PerfReport report)> Load(string dir)
        {
            var list = new List<(string, PerfReport)>();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return list;
            foreach (var path in Directory.GetFiles(dir, "*.json").OrderBy(p => p))
            {
                PerfReport r;
                try { r = PerfReport.FromJson(File.ReadAllText(path)); } catch { continue; }
                if (r == null || r.rooms == null || string.IsNullOrEmpty(r.graphics)) continue;
                list.Add((Path.GetFileName(path), r));
            }
            return list;
        }

        /// <summary>PRO-06 is met when some report proves it.</summary>
        public static bool Met(IEnumerable<Verdict> verdicts) => verdicts.Any(v => v.Proves);

        /// <summary>The gate's report in Markdown: the verdict, then each run with its card, class and numbers.</summary>
        public static string Report(IReadOnlyList<Verdict> verdicts)
        {
            var sb = new StringBuilder("# Performance gate (PRO-06)\n\n");
            sb.Append("**").Append(Met(verdicts) ? "Met" : "Not yet").Append("**: 60 fps at ").Append(Width).Append('×').Append(Height)
              .Append(" on a GTX 1060-class card (").Append(F(TargetLow)).Append("–").Append(F(TargetHigh)).Append("× its score), every transition under ")
              .Append(F(PerfBudget.TransitionMs)).Append(" ms. ").Append(verdicts.Count).Append(verdicts.Count == 1 ? " run.\n\n" : " runs.\n\n");
            if (verdicts.Count == 0) sb.Append("No reports yet. Run `pwsh tools/perf.ps1 -Out logs/perf/<machine>.json` on the target card.\n");
            foreach (var v in verdicts)
            {
                sb.Append("## ").Append(v.Source).Append("\n\n");
                sb.Append("- ").Append(v.Class).Append(v.Score > 0 ? " (" + F(v.Score) + "× a GTX 1060)" : "").Append(v.Proves ? ": **proves the target**" : "").Append('\n');
                sb.Append("- worst p95 ").Append(F(v.WorstP95)).Append(" ms");
                if (v.Class == GpuClass.Above) sb.Append(", projected onto the target ").Append(F(v.ProjectedP95)).Append(" ms");
                sb.Append("; worst transition ").Append(F(v.WorstTransition)).Append(" ms\n");
                foreach (var reason in v.Reasons) sb.Append("- ").Append(reason).Append('\n');
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Read the folder, judge every report, write the Markdown. True when PRO-06 is met.</summary>
        public static bool Run(string dir, string reportPath, out string text)
        {
            var verdicts = Load(dir).Select(x => Judge(x.report, x.file)).ToList();
            text = Report(verdicts);
            if (!string.IsNullOrEmpty(reportPath))
            {
                var d = Path.GetDirectoryName(Path.GetFullPath(reportPath));
                if (!string.IsNullOrEmpty(d)) Directory.CreateDirectory(d);
                File.WriteAllText(reportPath, text);
            }
            return Met(verdicts);
        }

        static string F(float x) => x.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
