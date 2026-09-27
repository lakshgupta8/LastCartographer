using System;

namespace OWSBG.Core
{
    /// <summary>
    /// Fade stages per place (bible 10, docs/design/fade-stages.md, PRG-14). A place is a room id or a
    /// named sub-zone; its stage is the int flag "fade.&lt;place&gt;", 0 (fully drawn) to 4 (blank paper).
    /// Stages only advance, and only from story beats (Yarn's &lt;&lt;fade&gt;&gt; or code); never on a clock.
    /// Anchored places do not fade. Restore is the one way back and belongs to re-surveying after erasure.
    /// </summary>
    public static class FadeStages
    {
        public const int Max = 4;
        public const string Prefix = "fade.";

        public static event Action<string, int> Changed;

        public static string Key(string place) => Prefix + place;

        public static int Get(WorldState w, string place) => Clamp(w.Get(Key(place)));

        public static bool IsAnchored(WorldState w, string place) => w.AnchoredPlaces.Contains(place);

        /// <summary>Move a place further toward blank. False when anchored, out of range, or not an advance.</summary>
        public static bool Advance(WorldState w, string place, int stage)
        {
            if (string.IsNullOrEmpty(place) || IsAnchored(w, place)) return false;
            stage = Clamp(stage);
            if (stage <= Get(w, place)) return false;
            w.Set(Key(place), stage);
            Changed?.Invoke(place, stage);
            return true;
        }

        /// <summary>Bring ink back (re-survey after a Cantor's erasure). False when not a step back.</summary>
        public static bool Restore(WorldState w, string place, int stage)
        {
            if (string.IsNullOrEmpty(place)) return false;
            stage = Clamp(stage);
            if (stage >= Get(w, place)) return false;
            w.Set(Key(place), stage);
            Changed?.Invoke(place, stage);
            return true;
        }

        /// <summary>The shader's _Ink for a stage: 1 drawn, 0 paper.</summary>
        public static float InkFor(int stage)
        {
            switch (Clamp(stage))
            {
                case 0: return 1f;
                case 1: return 0.8f;
                case 2: return 0.55f;
                case 3: return 0.3f;
                default: return 0f;
            }
        }

        public static string Describe(int stage)
        {
            switch (Clamp(stage))
            {
                case 0: return "drawn";
                case 1: return "thinning";
                case 2: return "washing";
                case 3: return "softening";
                default: return "blank";
            }
        }

        static int Clamp(int s) => s < 0 ? 0 : (s > Max ? Max : s);
    }
}
