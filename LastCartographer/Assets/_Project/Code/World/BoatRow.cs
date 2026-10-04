using System;
using System.Collections;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Rowing with Sable (<see cref="Boat"/>): paper closes, the day goes by on the water, the paper lifts at the other
    /// berth. <c>&lt;&lt;row windreach&gt;&gt;</c> and <c>&lt;&lt;row quay&gt;&gt;</c> run it and wait for it.
    /// </summary>
    public static class BoatRow
    {
        public static bool IsRowing { get; private set; }
        public static event Action<int> Arrived;

        public static IEnumerator Go(string destination, float fadeSeconds = 0.5f)
        {
            if (IsRowing || !Boat.TryDestination(destination, out var room, out var spawn, out int berth)) yield break;
            IsRowing = true;
            var wren = UnityEngine.Object.FindFirstObjectByType<WrenController>();
            bool wasFrozen = wren != null && wren.Frozen;
            if (wren != null) wren.Frozen = true;

            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime) { ScreenFade.Set(t / fadeSeconds, ScreenFade.Paper); yield return null; }
            ScreenFade.Set(1f, ScreenFade.Paper);

            var w = GameState.World;
            float hours = Boat.Row(w, berth);
            var scene = WorldGraph.GreyboxPrefix + room;
            var rm = RoomManager.Instance;
            if (rm != null && rm.CurrentRoom != scene)
            {
                rm.Transition(scene, spawn);
                while (rm.IsTransitioning) yield return null;
            }
            yield return null;

            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime) { ScreenFade.Set(1f - t / fadeSeconds, ScreenFade.Paper); yield return null; }
            ScreenFade.Clear();
            if (wren != null) wren.Frozen = wasFrozen;
            IsRowing = false;
            string where = berth == Boat.AtRiver ? Loc.T("caption.boat_river", "The Dry River") : Loc.T("caption.boat_quay", "The Drowned Quay");
            Captions.Show(Loc.F("caption.boat", "{0} on the water. {1}, {2}.", Travel.Describe(hours), where, DayClock.Display(DayClock.PhaseIn(w, room))), 3.5f);
            Arrived?.Invoke(berth);
        }
    }
}
