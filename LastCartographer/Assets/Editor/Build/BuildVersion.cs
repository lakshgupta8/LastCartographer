using System.Text.RegularExpressions;

namespace OWSBG.Build
{
    /// <summary>
    /// A build's version from whatever named it (PRG-25): a tag ("v0.5.0"), <c>git describe</c> ("v0.5.0-3-g1a2b3c4-dirty"),
    /// CI's input, or nothing. The result is semver-shaped: "0.5.0", "0.5.0+3", "0.5.0+3.dirty"; untagged history is
    /// "0.0.0-dev".
    /// </summary>
    public static class BuildVersion
    {
        public const string Untagged = "0.0.0-dev";
        const string DirtySuffix = "-dirty";

        static readonly Regex Described = new Regex(@"^v?(\d+\.\d+\.\d+(?:-[0-9A-Za-z.]+)?)-(\d+)-g[0-9a-f]+$");
        static readonly Regex Tagged = new Regex(@"^v?(\d+\.\d+\.\d+(?:-[0-9A-Za-z.]+)?)$");
        static readonly Regex Sha = new Regex(@"^[0-9a-f]{4,40}$");

        public static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Untagged;
            raw = raw.Trim();
            // git describe's "-dirty" (uncommitted changes) is build metadata, not a pre-release.
            bool dirty = raw.EndsWith(DirtySuffix);
            if (dirty) raw = raw.Substring(0, raw.Length - DirtySuffix.Length);
            var m = Described.Match(raw);
            if (m.Success) return m.Groups[1].Value + "+" + m.Groups[2].Value + (dirty ? ".dirty" : "");
            m = Tagged.Match(raw);
            if (m.Success) return m.Groups[1].Value + (dirty ? "+dirty" : "");
            if (Sha.IsMatch(raw)) return Untagged;
            var cleaned = Regex.Replace(raw.TrimStart('v'), @"[^0-9A-Za-z.+\-]", "-");
            return cleaned.Length == 0 ? Untagged : cleaned;
        }
    }
}
