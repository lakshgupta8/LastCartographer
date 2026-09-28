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

        /// <summary>
        /// The words of each walk (NAR-18): its verse titles and the bounds it calls, in English, by walk id. The set piece
        /// in the room carries the same words (ProjectSetup builds it); these are what the tables are keyed from.
        /// </summary>
        public static readonly System.Collections.Generic.IReadOnlyDictionary<string, (string[] verses, string[] bounds)> Words =
            new System.Collections.Generic.Dictionary<string, (string[], string[])>
            {
                { "merrows_end", (new[] { "the stoop and the post", "back by the steps" },
                                  new[] { "Dotha's stoop", "the tether-post", "the shaft's foot", "the first step", "the second step" }) },
            };

        public static string VerseKey(string walk, int verse) => "walk." + walk + ".verse." + verse;
        public static string BoundKey(string walk, string bound) => "walk." + walk + ".bound." + Loc.Slug(bound);

        /// <summary>A verse's title in the player's language.</summary>
        public static string VerseTitle(string walk, int verse, string english) => string.IsNullOrEmpty(english) ? english : Loc.T(VerseKey(walk, verse), english);
        /// <summary>A bound's name, as the roll-call sings it, in the player's language.</summary>
        public static string BoundName(string walk, string english) => string.IsNullOrEmpty(english) ? english : Loc.T(BoundKey(walk, english), english);

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
