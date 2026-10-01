using System;
using System.Collections;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Walking with the Long Grass Camp (PRG-21): paper closes, she sleeps with the clan, walks the day with them and makes
    /// camp at dusk at the next site; the paper lifts there. Built rooms are used when they exist, the stand-ins until
    /// then. <c>&lt;&lt;camp walk&gt;&gt;</c> runs it and waits for it.
    /// </summary>
    public static class CampWalk
    {
        public const string SpawnName = "Camp";
        public static bool IsWalking { get; private set; }
        public static event Action<int> Arrived;

        /// <summary>The scene a camp room loads as: the built room (ENV-07) when the catalogue has it, else its stand-in.</summary>
        public static string SceneFor(string room)
        {
            var rm = RoomManager.Instance;
            string built = WorldGraph.GreyboxPrefix + room;
            if (rm != null && rm.IsBuilt(built)) return built;
            return rm != null && rm.IsAddressable(room) ? room : Camp.StandInScene(room);
        }

        public static IEnumerator Go(float fadeSeconds = 0.5f)
        {
            if (IsWalking || !Camp.IsReadyToWalk(GameState.World)) yield break;
            IsWalking = true;
            var wren = UnityEngine.Object.FindFirstObjectByType<WrenController>();
            bool wasFrozen = wren != null && wren.Frozen;
            if (wren != null) wren.Frozen = true;

            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime) { ScreenFade.Set(t / fadeSeconds, ScreenFade.Paper); yield return null; }
            ScreenFade.Set(1f, ScreenFade.Paper);

            var w = GameState.World;
            Camp.WalkWith(w);
            int site = Camp.Site(w);
            var scene = SceneFor(Camp.RoomOfSite(site));
            var rm = RoomManager.Instance;
            if (rm != null && rm.CurrentRoom != scene)
            {
                rm.Transition(scene, SpawnName);
                while (rm.IsTransitioning) yield return null;
            }
            yield return null;

            for (float t = 0f; t < fadeSeconds; t += Time.deltaTime) { ScreenFade.Set(1f - t / fadeSeconds, ScreenFade.Paper); yield return null; }
            ScreenFade.Clear();
            if (wren != null) wren.Frozen = wasFrozen;
            IsWalking = false;
            Captions.Show(Loc.F("caption.camp_walk", "A day's walk with the clan. {0}, at dusk.", Capital(Camp.NameOfSite(site))), 3.5f);
            Arrived?.Invoke(site);
        }

        static string Capital(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
