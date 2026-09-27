using System;
using System.Collections;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Travel between waypoints (GDD 5, DES-02): only from the desk or lit lamp Wren stands at, only to one
    /// whose place is on the page. Paper closes over the screen, the room changes, the paper lifts.
    /// Run the coroutine on any live MonoBehaviour (the atlas page does).
    /// </summary>
    public static class FastTravel
    {
        public static bool IsTravelling { get; private set; }
        public static event Action<Waypoint> Arrived;

        public static bool CanTravel(WorldState w, Waypoint to)
        {
            var from = TravelPoint.Nearby;
            return !IsTravelling && from != null && to != null && to.Id != from.WaypointId && Atlas.CanTravelTo(w, to);
        }

        public static IEnumerator Go(Waypoint to, float fadeSeconds = 0.35f)
        {
            if (IsTravelling || to == null) yield break;
            IsTravelling = true;
            var wren = UnityEngine.Object.FindFirstObjectByType<WrenController>();
            bool wasFrozen = wren != null && wren.Frozen;
            if (wren != null) wren.Frozen = true;

            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
            {
                ScreenFade.Set(t / fadeSeconds, ScreenFade.Paper);
                yield return null;
            }
            ScreenFade.Set(1f, ScreenFade.Paper);

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
            Arrived?.Invoke(to);
        }
    }
}
