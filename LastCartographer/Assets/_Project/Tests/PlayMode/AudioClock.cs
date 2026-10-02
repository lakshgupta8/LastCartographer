using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// The audio clock against the wall clock, measured once per run. A machine without an audio device (CI's runners)
    /// mixes to nothing and its DSP clock can run far ahead of real time: there a bar-line wait ends at once, stems
    /// started on one sample sit buffers apart when read, a resolution's seconds pass in a frame and a crossfade on the
    /// DSP clock is over before the next frame. A fixture whose tests measure such things yields <see cref="Measure"/>
    /// in its UnitySetUp, and those tests call <see cref="NeedsRealtime"/> first to step aside there, with the rate.
    /// </summary>
    public static class AudioClock
    {
        public static float? Rate { get; private set; }

        public static IEnumerator Measure()
        {
            if (Rate != null) yield break;
            double d0 = AudioSettings.dspTime; float r0 = Time.realtimeSinceStartup;
            yield return new WaitForSecondsRealtime(0.5f);
            Rate = (float)((AudioSettings.dspTime - d0) / (Time.realtimeSinceStartup - r0));
            Debug.Log("[OWSBG] the audio clock runs at " + Rate.Value.ToString("0.00") + "x real time");
        }

        public static bool IsRealtime => Rate == null || (Rate >= 0.8f && Rate <= 1.25f);

        public static void NeedsRealtime()
        {
            if (!IsRealtime)
                Assert.Ignore("the audio clock runs at " + Rate.Value.ToString("0.00") + "x real time here (no audio device?); this measures time on it");
        }
    }
}
