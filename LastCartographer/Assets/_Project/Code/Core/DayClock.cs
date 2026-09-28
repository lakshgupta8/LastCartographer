using System;

namespace OWSBG.Core
{
    public enum DayPhase { Dawn, Day, Dusk, Night }

    /// <summary>
    /// The day (PRG-15, docs/design/hub-life.md): a fraction 0..1 in Numbers["$day_time"] and a count in
    /// Numbers["$day"]. It advances with play (DayCycle), never with the story's fade. Resting at a desk sleeps
    /// to the next dawn. An <b>anchored</b> place keeps the time it was sealed at: TimeIn / PhaseIn read that
    /// lock, so the held state is the same hour every visit.
    /// </summary>
    public static class DayClock
    {
        public const string TimeKey = "$day_time";
        public const string DayKey = "$day";
        public const float DefaultTime = 0.15f;   // a new game starts in the morning
        public const float DawnEnd = 0.10f, DayEnd = 0.50f, DuskEnd = 0.62f;

        /// <summary>The global phase changed (Advance, SetPhase, Sleep).</summary>
        public static event Action<DayPhase> PhaseChanged;

        public static float Time(WorldState w) => w.Numbers.TryGetValue(TimeKey, out var t) ? Frac(t) : DefaultTime;
        public static int Day(WorldState w) => w.Numbers.TryGetValue(DayKey, out var d) && d >= 1f ? (int)d : 1;
        public static DayPhase Phase(WorldState w) => PhaseAt(Time(w));

        public static DayPhase PhaseAt(float t)
        {
            t = Frac(t);
            return t < DawnEnd ? DayPhase.Dawn : t < DayEnd ? DayPhase.Day : t < DuskEnd ? DayPhase.Dusk : DayPhase.Night;
        }

        public static float PhaseStart(DayPhase p)
        {
            switch (p)
            {
                case DayPhase.Day: return DawnEnd;
                case DayPhase.Dusk: return DayEnd;
                case DayPhase.Night: return DuskEnd;
                default: return 0f;
            }
        }

        /// <summary>Move the day on by a fraction; midnight rolls the count.</summary>
        public static void Advance(WorldState w, float dayFraction)
        {
            if (dayFraction <= 0f) return;
            var before = Phase(w);
            float t = Time(w) + dayFraction;
            int day = Day(w), turned = 0;
            while (t >= 1f) { t -= 1f; day++; turned++; }
            w.Numbers[TimeKey] = t;
            w.Numbers[DayKey] = day;
            for (int i = 0; i < turned; i++) Camp.NewDay(w);   // first light: the Windreach camp walks on
            var after = PhaseAt(t);
            if (after != before) PhaseChanged?.Invoke(after);
        }

        /// <summary>Jump to the start of a phase today (story beats: the Edge is at dusk, the shore at dawn).</summary>
        public static void SetPhase(WorldState w, DayPhase p)
        {
            var before = Phase(w);
            w.Numbers[TimeKey] = PhaseStart(p) + 0.01f;
            if (!w.Numbers.ContainsKey(DayKey)) w.Numbers[DayKey] = 1f;
            if (p != before) PhaseChanged?.Invoke(p);
        }

        /// <summary>Rest at a desk: the next dawn.</summary>
        public static void Sleep(WorldState w)
        {
            w.Numbers[DayKey] = Day(w) + 1;
            var before = Phase(w);
            w.Numbers[TimeKey] = 0.01f;
            Camp.NewDay(w);
            if (before != DayPhase.Dawn) PhaseChanged?.Invoke(DayPhase.Dawn);
        }

        // ---- Anchored places keep their hour -------------------------------------------------------------

        public static string LockKey(string place) => "place." + place + ".locked_time";
        public static bool IsLocked(WorldState w, string place) => !string.IsNullOrEmpty(place) && w.Numbers.ContainsKey(LockKey(place));
        public static void Lock(WorldState w, string place) { if (!string.IsNullOrEmpty(place)) w.Numbers[LockKey(place)] = Time(w); }
        public static void Unlock(WorldState w, string place) { if (!string.IsNullOrEmpty(place)) w.Numbers.Remove(LockKey(place)); }

        /// <summary>The time in a place: its locked hour when anchored, else the world's.</summary>
        public static float TimeIn(WorldState w, string place) =>
            !string.IsNullOrEmpty(place) && w.Numbers.TryGetValue(LockKey(place), out var t) ? Frac(t) : Time(w);

        public static DayPhase PhaseIn(WorldState w, string place) => PhaseAt(TimeIn(w, place));

        /// <summary>The phase's name in Yarn and saves: "dawn", "day", "dusk", "night". Never translated.</summary>
        public static string Describe(DayPhase p) => p.ToString().ToLowerInvariant();

        /// <summary>The phase's name for the player, in their language (PRG-19).</summary>
        public static string Display(DayPhase p) => p switch
        {
            DayPhase.Dawn => Loc.T("phase.dawn", "dawn"),
            DayPhase.Day => Loc.T("phase.day", "day"),
            DayPhase.Dusk => Loc.T("phase.dusk", "dusk"),
            _ => Loc.T("phase.night", "night"),
        };

        public static bool TryParse(string s, out DayPhase phase)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "dawn": case "morning": phase = DayPhase.Dawn; return true;
                case "day": case "noon": phase = DayPhase.Day; return true;
                case "dusk": case "evening": phase = DayPhase.Dusk; return true;
                case "night": phase = DayPhase.Night; return true;
                default: phase = DayPhase.Day; return false;
            }
        }

        static float Frac(float t) { t -= (float)Math.Floor(t); return t < 0f ? 0f : (t >= 1f ? 0f : t); }
    }
}
