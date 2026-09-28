using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>
    /// The performance budgets (PRG-24, PRO-06), and how a sample of frames is summarised against them. The frame budget
    /// is 60 fps's 16.7 ms at the 95th percentile, measured uncapped; a room transition is 100 ms from the request to
    /// Wren standing in the room; and a quiet frame (standing in a room, nothing happening) allocates no managed
    /// memory, so the garbage collector never hitches the frame rate.
    /// </summary>
    public static class PerfBudget
    {
        public const float FrameMs = 1000f / 60f;
        public const float TransitionMs = 100f;
        /// <summary>Managed bytes a quiet frame may allocate, on average over a sample.</summary>
        public const float GcBytesPerFrame = 64f;
        /// <summary>Draw batches in a greybox room; the painted rooms will set their own when the art lands.</summary>
        public const int Batches = 400;

        public struct Frames
        {
            public int Count;
            public float AvgMs, P50Ms, P95Ms, P99Ms, MaxMs;
            public float Fps => AvgMs > 0f ? 1000f / AvgMs : 0f;
        }

        /// <summary>Average, median, 95th and 99th percentiles and worst of a sample of frame times (ms).</summary>
        public static Frames Summarise(IReadOnlyList<float> ms)
        {
            var f = new Frames { Count = ms?.Count ?? 0 };
            if (f.Count == 0) return f;
            var sorted = ms.OrderBy(x => x).ToArray();
            f.AvgMs = sorted.Average();
            f.P50Ms = Percentile(sorted, 0.50f);
            f.P95Ms = Percentile(sorted, 0.95f);
            f.P99Ms = Percentile(sorted, 0.99f);
            f.MaxMs = sorted[sorted.Length - 1];
            return f;
        }

        /// <summary>Nearest-rank percentile of an ascending sample.</summary>
        public static float Percentile(float[] ascending, float p)
        {
            if (ascending.Length == 0) return 0f;
            int rank = (int)System.Math.Ceiling((double)p * ascending.Length - 1e-6);   // 0.99f × 100 is 99.000004 in floats
            return ascending[System.Math.Max(0, System.Math.Min(ascending.Length - 1, rank - 1))];
        }

        public static bool FrameWithin(Frames f) => f.Count > 0 && f.P95Ms <= FrameMs;
        public static bool TransitionWithin(float ms) => ms <= TransitionMs;
    }
}
