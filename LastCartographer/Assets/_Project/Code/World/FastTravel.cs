using System;
using System.Collections;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Travel between waypoints (GDD 5, DES-02): only from the desk or lit lamp Wren stands at, only to one
    /// whose place is on the page. Paper closes over the screen, the room changes, the paper lifts. The road takes
    /// time (PRG-21, <see cref="Travel"/>): the day moves on by the journey's hours while the paper is closed.
    /// Run the coroutine on any live MonoBehaviour (the atlas page does).
    /// </summary>
    public static class FastTravel
    {
        public static bool IsTravelling { get; private set; }
        public static event Action<Waypoint> Arrived;
        /// <summary>How long the last journey took, in hours.</summary>
        public static float LastHours { get; private set; }

        public static bool CanTravel(WorldState w, Waypoint to)
        {
            var from = TravelPoint.Nearby;
            return !IsTravelling && from != null && to != null && to.Id != from.WaypointId && Atlas.CanTravelTo(w, to);
        }

        public static IEnumerator Go(Waypoint to, float fadeSeconds = 0.35f)
        {
            if (IsTravelling || to == null) yield break;
            IsTravelling = true;
            var from = TravelPoint.Nearby != null ? TravelPoint.Nearby.Definition : null;
            var wren = UnityEngine.Object.FindFirstObjectByType<WrenController>();
            bool wasFrozen = wren != null && wren.Frozen;
            if (wren != null) wren.Frozen = true;

            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
            {
                ScreenFade.Set(t / fadeSeconds, ScreenFade.Paper);
                yield return null;
            }
            ScreenFade.Set(1f, ScreenFade.Paper);
            LastHours = Travel.Journey(GameState.World, from, to);

            var rm = RoomManager.Instance;
            if (rm != null && rm.CurrentRoom != to.Room)
            {
                rm.Transition(to.Room, to.Spawn);
                while (rm.IsTransitioning) yield return null;
            }
            else
            {
                var room = Room.Current;
                var sp = room != null ? room.FindSpawn(to.Spawn) : null;
                if (sp != null && wren != null) wren.Teleport(sp.position);
            }
            yield return null;

            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
            {
                ScreenFade.Set(1f - t / fadeSeconds, ScreenFade.Paper);
                yield return null;
            }
            ScreenFade.Clear();
            if (wren != null) wren.Frozen = wasFrozen;
            IsTravelling = false;
            var w = GameState.World;
            Captions.Show(Loc.F("caption.travelled", "{0} on the road. {1}, day {2}.", Travel.Describe(LastHours), Capital(DayClock.Display(DayClock.PhaseIn(w, to.Place))), DayClock.Day(w)), 3f);
            Arrived?.Invoke(to);
        }

        static string Capital(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
