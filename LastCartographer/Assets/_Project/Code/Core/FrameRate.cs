using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// The 60 fps lock (combat doc: "60 fps locked, physics at 60 Hz fixed step"; PRG-24). On a display whose refresh is
    /// a multiple of 60 (60, 120, 180, 240 Hz) it syncs to every first, second, third or fourth refresh, so frames never tear. On anything else (75,
    /// 144, 165 Hz) it turns vsync off and caps at 60, because vsync alone would run at 144.
    /// The performance probe lifts the cap to see what a frame really costs (<see cref="Uncapped"/>).
    /// </summary>
    public static class FrameRate
    {
        public const int Target = 60;

        /// <summary>True while the cap is lifted (the <c>-perf</c> probe).</summary>
        public static bool Uncapped { get; private set; }

        /// <summary>The vsync count and frame-rate target for a display's refresh rate.</summary>
        public static (int vSyncCount, int targetFrameRate) For(double refreshHz)
        {
            if (refreshHz <= 0) return (0, Target);
            double ratio = refreshHz / Target;
            int whole = (int)System.Math.Round(ratio);
            // 59.94 Hz and 119.88 Hz count as 60 and 120.
            if (whole >= 1 && whole <= 4 && System.Math.Abs(ratio - whole) < 0.01) return (whole, Target);
            return (0, Target);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            if (Uncapped) return;
            var (vsync, target) = For(Screen.currentResolution.refreshRateRatio.value);
            QualitySettings.vSyncCount = vsync;
            Application.targetFrameRate = target;
        }

        /// <summary>Lift the cap (no vsync, no target) to measure; <see cref="Apply"/> puts it back.</summary>
        public static void Uncap()
        {
            Uncapped = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        public static void Recap()
        {
            Uncapped = false;
            Apply();
        }
    }
}
