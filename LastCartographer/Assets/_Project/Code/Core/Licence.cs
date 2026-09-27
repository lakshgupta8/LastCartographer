namespace OWSBG.Core
{
    /// <summary>
    /// Wren's standing with the Guild (bible 3.1, 7.1 step 3; DES-03 §2): a journeyman until Halvard's count at the
    /// lit lamp, unlicensed from then on. Wardens in anchored towns measure a journeyman and hunt an unlicensed
    /// cartographer. Oriel can stand them down in Act 2 (boss 6.8); Pell's report, if sent, sets them on her again.
    /// </summary>
    public static class Licence
    {
        public const string UnlicensedFlag = "act1.unlicensed";
        public const string StoodDownFlag = "halden.oriel.stood_down";
        public const string ReportSentFlag = "pell.report_sent";

        /// <summary>The Guild's count: she is missing, and missing carries no licence.</summary>
        public static bool IsUnlicensed(WorldState w) => w.Is(UnlicensedFlag);

        /// <summary>Halvard's measuring, or any later scene that revokes her.</summary>
        public static bool Revoke(WorldState w)
        {
            if (w.Is(UnlicensedFlag)) return false;
            w.Set(UnlicensedFlag, true);
            return true;
        }

        /// <summary>
        /// Whether the Wardens of an anchored place lower the lance at her: unlicensed, unless Oriel has stood them down;
        /// and always once Pell's report is in the Guild's hands.
        /// </summary>
        public static bool WardensHostile(WorldState w)
        {
            if (w.Is(ReportSentFlag)) return true;
            return IsUnlicensed(w) && !w.Is(StoodDownFlag);
        }

        public static string Describe(WorldState w) => IsUnlicensed(w) ? "unlicensed" : "journeyman";
    }
}
