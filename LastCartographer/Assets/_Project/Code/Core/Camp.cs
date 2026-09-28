using System;

namespace OWSBG.Core
{
    /// <summary>
    /// The Long Grass Camp, Windreach's hub, which moves (bible 4.5, 8.5; DES-11 §3; PRG-21). Three nights at three
    /// sites: the fire ring (Camp_2), the riverbed (River_2), the high grass past the Gate (Fire_1). The scenes count the
    /// nights (<c>windreach.camp.night</c>); the camp keeps its own site. At first light after a fire has been had where
    /// it stands, the camp walks on, whether Wren walks with it or not. Walking with it costs her the day: she sleeps, walks,
    /// and makes camp with them at dusk at the next site. One wagon stays at the Long Grass (Camp_1) as the walkers' post,
    /// with the desk and the ledger, so the hub's desk is always where the map says.
    /// </summary>
    public static class Camp
    {
        public const string NightKey = "windreach.camp.night";
        public const string SiteKey = "windreach.camp.site";
        public const string PostRoom = "Windreach_Camp_1";
        /// <summary>Until Windreach is built (ENV-07), the camp's rooms are stood in for at runtime by scenes of this prefix.</summary>
        public const string StandInPrefix = "Camp_";
        public static string StandInScene(string room) => StandInPrefix + room;
        public static bool IsStandIn(string scene) => !string.IsNullOrEmpty(scene) && scene.StartsWith(StandInPrefix + "Windreach_");
        /// <summary>The planned room a scene is, or stands in for.</summary>
        public static string RoomOfScene(string scene) => IsStandIn(scene) ? scene.Substring(StandInPrefix.Length) : scene;
        /// <summary>Every room the camp's road runs through, in walking order: the post, then the three sites.</summary>
        public static readonly string[] Road = { PostRoom, "Windreach_Camp_2", "Windreach_River_2", "Windreach_Fire_1" };

        static readonly string[] SiteRooms = { "Windreach_Camp_2", "Windreach_River_2", "Windreach_Fire_1" };
        static readonly string[] SiteNames = { "the fire ring", "the riverbed", "the high grass" };
        /// <summary>The fire's scene at each site (Windreach_Camp_Idrenne.yarn).</summary>
        static readonly string[] FireNodes = { "Camp_Idrenne", "River_Idrenne_Night", "Grass_Idrenne_Night" };

        public static int SiteCount => SiteRooms.Length;
        public static string RoomOfSite(int site) => SiteRooms[Clamp(site)];
        public static string NameOfSite(int site) => SiteNames[Clamp(site)];
        public static string FireNodeOf(int site) => FireNodes[Clamp(site)];
        public static int SiteOf(string room) => Array.IndexOf(SiteRooms, room);

        /// <summary>The camp moved on to a site (its index).</summary>
        public static event Action<int> Moved;

        public static int Nights(WorldState w) => w.Get(NightKey);
        public static int Site(WorldState w) => Clamp(w.Get(SiteKey));
        public static string SiteRoom(WorldState w) => RoomOfSite(Site(w));
        public static bool IsAt(WorldState w, string room) => SiteRoom(w) == room;

        /// <summary>Tonight's fire has been had where the camp stands, and there is somewhere left to walk.</summary>
        public static bool IsReadyToWalk(WorldState w) => Site(w) < SiteCount - 1 && Nights(w) > Site(w);

        /// <summary>
        /// A new day (DayClock calls this whenever the day count turns): at first light the camp walks on, one site,
        /// if its fire was had. True when it moved.
        /// </summary>
        public static bool NewDay(WorldState w)
        {
            if (!IsReadyToWalk(w)) return false;
            int site = Site(w) + 1;
            w.Set(SiteKey, site);
            Moved?.Invoke(site);
            return true;
        }

        /// <summary>
        /// Walk with them: sleep with the camp, walk the day, make camp at dusk at the next site. The day count turns once.
        /// False (and nothing passes) when the camp isn't moving yet.
        /// </summary>
        public static bool WalkWith(WorldState w)
        {
            if (!IsReadyToWalk(w)) return false;
            DayClock.Sleep(w);   // the new day moves the camp
            DayClock.SetPhase(w, DayPhase.Dusk);
            return true;
        }

        static int Clamp(int site) => site < 0 ? 0 : site >= SiteRooms.Length ? SiteRooms.Length - 1 : site;
    }
}
