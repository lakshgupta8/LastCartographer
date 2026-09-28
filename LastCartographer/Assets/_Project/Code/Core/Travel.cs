using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>
    /// Travel takes time (PRG-21; survey.md's open "the story may want a day to pass"). Travelling between waypoints walks
    /// the road on the macro map: an hour for every way between zones, half an hour within one. The day moves on by
    /// that much, and past midnight the day count turns (and the Windreach camp may walk on). Gates don't lengthen the
    /// road: a destination is only offered when it is on the page, and the page is only drawn where she has been.
    /// </summary>
    public static class Travel
    {
        public const float HoursPerWay = 1f;
        public const float HoursWithinZone = 0.5f;

        /// <summary>The zone a waypoint stands in: a greybox place's, else its planned room's.</summary>
        public static string ZoneOf(Waypoint wp)
        {
            if (wp == null) return null;
            return WorldGraph.ZoneOfPlace(wp.Place) ?? RoomPlans.ZoneOf(wp.Room) ?? RoomPlans.ZoneOf(wp.Place);
        }

        /// <summary>The fewest ways between two zones on the macro map; -1 if either is unknown or they don't join.</summary>
        public static int Ways(string fromZone, string toZone)
        {
            if (string.IsNullOrEmpty(fromZone) || string.IsNullOrEmpty(toZone)) return -1;
            if (WorldGraph.Find(fromZone) == null || WorldGraph.Find(toZone) == null) return -1;
            if (fromZone == toZone) return 0;
            var dist = new Dictionary<string, int> { { fromZone, 0 } };
            var open = new Queue<string>();
            open.Enqueue(fromZone);
            while (open.Count > 0)
            {
                var at = open.Dequeue();
                foreach (var l in WorldGraph.Links)
                {
                    if (!l.Joins(at)) continue;
                    var next = l.Other(at);
                    if (dist.ContainsKey(next)) continue;
                    dist[next] = dist[at] + 1;
                    if (next == toZone) return dist[next];
                    open.Enqueue(next);
                }
            }
            return -1;
        }

        public static float Hours(string fromZone, string toZone)
        {
            int ways = Ways(fromZone, toZone);
            return ways <= 0 ? HoursWithinZone : ways * HoursPerWay;
        }

        public static float Hours(Waypoint from, Waypoint to) => Hours(ZoneOf(from), ZoneOf(to));

        /// <summary>Make the journey: the day moves on by its hours. Returns the hours.</summary>
        public static float Journey(WorldState w, Waypoint from, Waypoint to)
        {
            float h = Hours(from, to);
            DayClock.Advance(w, h / 24f);
            return h;
        }

        /// <summary>The page's words for it: "half an hour", "an hour", "3 hours".</summary>
        public static string Describe(float hours)
        {
            if (hours < 0.75f) return "half an hour";
            int h = (int)Math.Round(hours);
            return h == 1 ? "an hour" : h + " hours";
        }
    }
}
