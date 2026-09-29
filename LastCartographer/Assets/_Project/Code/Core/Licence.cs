namespace OWSBG.Core
{
    /// <summary>
    /// Wren's standing with the Guild (bible 3.1, 7.1 step 3; DES-03 §2): a journeyman until Halvard's count at the
    /// lit lamp, unlicensed from then on. Wardens in anchored towns measure a journeyman and hunt an unlicensed
    /// cartographer. Pell's report, if sent, sets them on her; Oriel fights her only once she has read it (boss 6.8),
    /// and beaten without a mask lost she stands them down. Her word comes after the report and outranks it.
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
        /// Whether the Wardens of an anchored place lower the lance at her: once Pell's report is in the Guild's hands, or
        /// when she is unlicensed; never once Oriel has stood them down.
        /// </summary>
        public static bool WardensHostile(WorldState w)
        {
            if (w.Is(StoodDownFlag)) return false;
            return w.Is(ReportSentFlag) || IsUnlicensed(w);
        }

        /// <summary>
        /// The Guild's seal needs the Guild's licence (anchoring §9): an unlicensed cartographer can't anchor a place at
        /// the desk. Holding and releasing were never the Guild's to allow.
        /// </summary>
        public static bool MayAnchor(WorldState w) => !IsUnlicensed(w);

        public static string Describe(WorldState w) => IsUnlicensed(w) ? "unlicensed" : "journeyman";
    }
}
