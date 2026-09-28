using System;
using System.Collections.Generic;
using OWSBG.Core;

namespace OWSBG.World
{
    /// <summary>
    /// The World assembly's catalog strings with their keys (NAR-18): Instrument names and blurbs, Charter names and
    /// blurbs. With <see cref="DataText"/> and the code's own <c>Loc.T</c> calls, the whole English table.
    /// </summary>
    public static class WorldText
    {
        public static List<(string key, string english, string source)> All()
        {
            var list = new List<(string, string, string)>();
            foreach (InstrumentKind k in Enum.GetValues(typeof(InstrumentKind)))
            {
                if (k == InstrumentKind.None) continue;
                var info = InstrumentInfo.Of(k);
                if (!string.IsNullOrEmpty(info.EnglishName)) list.Add((InstrumentInfo.NameKey(k), info.EnglishName, "InstrumentBelt"));
                if (!string.IsNullOrEmpty(info.EnglishBlurb)) list.Add((InstrumentInfo.BlurbKey(k), info.EnglishBlurb, "InstrumentBelt"));
            }
            foreach (CharterKind k in Enum.GetValues(typeof(CharterKind)))
            {
                var p = CharterProfile.For(k);
                if (!string.IsNullOrEmpty(p.DisplayName)) list.Add(("charter." + k + ".name", p.DisplayName, "CharterSet"));
                if (!string.IsNullOrEmpty(p.Blurb)) list.Add(("charter." + k + ".blurb", p.Blurb, "CharterSet"));
            }
            return list;
        }

        /// <summary>Everything data-driven the player reads: the Core catalogs and this assembly's.</summary>
        public static List<(string key, string english, string source)> Everything()
        {
            var list = DataText.All();
            list.AddRange(All());
            return list;
        }
    }
}
