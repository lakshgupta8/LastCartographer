using System.Linq;

namespace OWSBG.Core
{
    /// <summary>
    /// The keystones Wren carries (bible 1.3, 4.x, 9). Each has a home and is a flag, "keystone.&lt;home&gt;", written by the
    /// scene that hands it over. Six homes are named (world-map.md §6: the seventh's is open). The endings count them:
    /// the Fixed World needs seven, the Open World at least four. Voss counts them at the Threshold.
    /// </summary>
    public static class Keystones
    {
        /// <summary>Aury's wings, under Hollowvein, Teodor's House, Idrenne's hearth, the Observatory's frame, Corvin's talons.</summary>
        public static readonly string[] Homes = { "aury", "hollowvein", "quiet_house", "windreach", "observatory", "archivist" };

        /// <summary>Bible 9.2: the Open World needs at least this many.</summary>
        public const int OpenWorldNeeds = 4;

        public static string FlagKey(string home) => "keystone." + home;
        public static bool Has(WorldState w, string home) => w.Is(FlagKey(home));
        public static int Count(WorldState w) => Homes.Count(h => w.Is(FlagKey(h)));
    }
}
