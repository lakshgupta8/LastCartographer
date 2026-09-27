using System;

namespace OWSBG.Core
{
    /// <summary>
    /// Fade stages per place (bible 10, docs/design/fade-stages.md, PRG-14). A place is a room id or a
    /// named sub-zone; its stage is the int flag "fade.&lt;place&gt;", 0 (fully drawn) to 4 (blank paper).
    /// Stages only advance, and only from story beats (Yarn's &lt;&lt;fade&gt;&gt; or code); never on a clock.
    /// Anchored places do not fade. Erasure (a Cantor's bell, DES-02) is the one thing that blanks a place
    /// outside a story beat; it remembers the stage it took, and Recover (re-surveying) brings it back.
    /// </summary>
    public static class FadeStages
    {
        public const int Max = 4;
        public const string Prefix = "fade.";

        public static event Action<string, int> Changed;
        public static event Action<string> Erased;
        public static event Action<string> Recovered;

        public static string Key(string place) => Prefix + place;
        /// <summary>Set while erased: the stage the place had, plus one (so 0 means not erased).</summary>
        public static string ErasedKey(string place) => Prefix + place + ".erased";

        public static int Get(WorldState w, string place) => Clamp(w.Get(Key(place)));

        /// <summary>Anchored or held (Places): the fade is stopped.</summary>
        public static bool IsAnchored(WorldState w, string place) => Places.IsFadeStopped(w, place);

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

        /// <summary>A Cantor's bell: the ink goes blank now, the stage it had is kept for Recover. False when
        /// already erased or anchored (the Guild's seal holds against a field bell).</summary>
        public static bool Erase(WorldState w, string place)
        {
            if (string.IsNullOrEmpty(place) || IsErased(w, place)) return false;
            if (Places.FateOf(w, place) == PlaceFate.Anchored || w.AnchoredPlaces.Contains(place)) return false;
            w.Set(ErasedKey(place), Get(w, place) + 1);
            w.Set(Key(place), Max);
            Erased?.Invoke(place);
            Changed?.Invoke(place, Max);
            return true;
        }

        public static bool IsErased(WorldState w, string place) => !string.IsNullOrEmpty(place) && w.Get(ErasedKey(place)) > 0;

        /// <summary>The stage an erased place will come back to.</summary>
        public static int StageBeforeErasure(WorldState w, string place) => Clamp(w.Get(ErasedKey(place)) - 1);

        /// <summary>Re-surveyed: the ink returns to the stage it had. False when not erased.</summary>
        public static bool Recover(WorldState w, string place)
        {
            if (!IsErased(w, place)) return false;
            int stage = StageBeforeErasure(w, place);
            w.Set(ErasedKey(place), 0);
            w.Set(Key(place), stage);
            Recovered?.Invoke(place);
            Changed?.Invoke(place, stage);
            return true;
        }

        /// <summary>Bring ink back a step by story fiat. False when not a step back, or erased (use Recover).</summary>
        public static bool Restore(WorldState w, string place, int stage)
        {
            if (string.IsNullOrEmpty(place) || IsErased(w, place)) return false;
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
