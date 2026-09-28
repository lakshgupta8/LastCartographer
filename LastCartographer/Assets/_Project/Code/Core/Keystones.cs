using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>
    /// The seven keystones (bible 1.3, 4.x, 5.2, 9). Each has a home and, once Wren carries it, a flag
    /// "keystone.&lt;home&gt;" written by the scene that hands it over. The seventh home is Isolde's: the stone she stole
    /// from the Vault's sixth slot to return to Corvin, and still carries in Thessaly Hollow (NAR-13; world-map.md §6).
    /// The Observatory's own stone never leaves the frame: it is counted at the frame, not carried.
    /// </summary>
    public static class Keystones
    {
        /// <summary>Aury's wings, under Hollowvein, Teodor's House, Idrenne's hearth, Isolde's pack, Corvin's talons; and the frame.</summary>
        public static readonly string[] Homes = { "aury", "hollowvein", "quiet_house", "windreach", "isolde", "archivist", "observatory" };

        /// <summary>The stone that stays in the Atlas frame at the Observatory.</summary>
        public const string InTheFrame = "observatory";

        /// <summary>Bible 9.2: the Open World needs at least this many carried.</summary>
        public const int OpenWorldNeeds = 4;

        public static string FlagKey(string home) => Keys.Of("keystone.", home);

        /// <summary>Each stone as the journal names it (NAR-17), in English; "keystone.&lt;home&gt;.name" in the player's language.</summary>
        public static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
        {
            { "aury", "Aury's stone" },
            { "hollowvein", "The Hollowvein stone" },
            { "quiet_house", "The Quiet House's stone" },
            { "windreach", "Idrenne's cooking-stone" },
            { "isolde", "The Vault's sixth" },
            { "archivist", "The seventh stone" },
            { "observatory", "The Observatory's stone" },
        };
        public static string NameKey(string home) => Keys.Of("keystone.", home, ".name");
        public static string NameOf(string home) => English.TryGetValue(home ?? "", out var e) ? Loc.T(NameKey(home), e) : home;
        public static bool Has(WorldState w, string home) => w.Is(FlagKey(home));

        /// <summary>The stones Wren carries (Voss counts these; the frame adds its own).</summary>
        public static int Count(WorldState w) => Homes.Count(h => h != InTheFrame && w.Is(FlagKey(h)));
    }
}
