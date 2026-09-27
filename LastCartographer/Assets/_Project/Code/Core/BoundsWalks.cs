using System;

namespace OWSBG.Core
{
    /// <summary>
    /// The record of walks (DES-13): a place whose bounds have been walked is <b>held</b> by its people. The walk
    /// itself is a set piece in the room (World.BoundsWalk); this is what it writes.
    /// </summary>
    public static class BoundsWalks
    {
        public const string LearnedFlag = "holdfast.walk_learned";

        public static event Action<string> Walked;

        public static string DoneKey(string place) => "walk." + place + ".done";

        public static bool IsWalked(WorldState w, string place) => !string.IsNullOrEmpty(place) && w.Is(DoneKey(place));

        /// <summary>Wren knows the walk: taught at Kettil's Rest, or heard in the whale's song on the coast.</summary>
        public static bool IsLearned(WorldState w) => w.Is(LearnedFlag) || w.Is("saltmarrow.bone_bridge.heard");

        /// <summary>The walk is complete: the place is held. False when already walked; a decided place stays as it was.</summary>
        public static bool Complete(WorldState w, string place)
        {
            if (IsWalked(w, place)) return false;
            w.Set(DoneKey(place), true);
            Places.Hold(w, place);
            Walked?.Invoke(place);
            return true;
        }
    }
}
