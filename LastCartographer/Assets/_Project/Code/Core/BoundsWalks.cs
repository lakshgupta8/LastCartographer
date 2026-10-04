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

        public static string DoneKey(string place) => Keys.Of("walk.", place, ".done");

        public static bool IsWalked(WorldState w, string place) => !string.IsNullOrEmpty(place) && w.Is(DoneKey(place));

        /// <summary>Wren knows the walk: taught at Kettil's Rest, or heard in the whale's song on the coast.</summary>
        public static bool IsLearned(WorldState w) => w.Is(LearnedFlag) || w.Is("saltmarrow.bone_bridge.heard");

        /// <summary>
        /// A walk that goes down through rooms (bounds-walk.md §2, Hollowvein): one verse a room, the chorus going down with
        /// Wren. A script begins it (<c>&lt;&lt;walk id&gt;&gt;</c> wherever she is); each leg's room picks the walk up when she
        /// comes in and it is that leg's turn. The last leg may be a fight instead of a verse: walking the leg before it
        /// lets the fight wake (<see cref="WakeFlag"/>), and the fight won walks it. Every leg walked and all of its rooms are held.
        /// </summary>
        public sealed class Relay
        {
            public readonly string Id, Name;
            public readonly string[] Legs;
            /// <summary>The last leg is walked by winning this fight (its boss flag), or null when every leg is a verse.</summary>
            public readonly string FightFlag;
            public readonly string BegunKey, LegsKey, WakeFlag, LastDoneKey;

            public Relay(string id, string name, string[] legs, string fightFlag)
            {
                Id = id; Name = name; Legs = legs; FightFlag = fightFlag;
                BegunKey = "walk." + id + ".begun";
                LegsKey = "walk." + id + ".legs";
                WakeFlag = "walk." + id + ".wakes";
                LastDoneKey = DoneKey(legs[legs.Length - 1]);
            }

            public int LegOf(string place) => Array.IndexOf(Legs, place);
        }

        public static readonly Relay[] Relays =
        {
            // The long roll-call: the adit, the first gallery, the flooded gallery, then the bottom, where the Collapse is
            // the fourth verse (boss 6.4).
            new Relay("hollowvein", "Hollowvein", new[] { "Emberdown_Hollow_1", "Emberdown_Hollow_2", "Emberdown_Hollow_3", "Emberdown_Hollow_4" },
                      Bosses.FlagKey("collapse")),
        };

        public static Relay FindRelay(string id)
        {
            foreach (var r in Relays) if (r.Id == id) return r;
            return null;
        }

        /// <summary>Begin a walk that goes down through rooms; false when it is walked already.</summary>
        public static bool BeginRelay(WorldState w, Relay r)
        {
            if (r == null || w.Is(r.LastDoneKey)) return false;
            w.Set(r.BegunKey, 1);
            return true;
        }

        public static int LegsWalked(WorldState w, Relay r) => w.Get(r.LegsKey);

        /// <summary>It is this leg's turn: the walk is begun, the legs before it are walked, and it is not over.</summary>
        public static bool IsLegDue(WorldState w, Relay r, int leg) => r != null && w.Is(r.BegunKey) && w.Get(r.LegsKey) == leg && !w.Is(r.LastDoneKey);

        /// <summary>
        /// One leg walked: the next is due, the fight may wake once only it is left, and the last walks the whole: every
        /// room is held. True when that was the last leg.
        /// </summary>
        public static bool WalkLeg(WorldState w, Relay r, int leg)
        {
            if (!IsLegDue(w, r, leg)) return false;
            w.Set(r.LegsKey, leg + 1);
            if (r.FightFlag != null && leg + 1 == r.Legs.Length - 1) w.Set(r.WakeFlag, 1);
            if (leg + 1 < r.Legs.Length) return false;
            foreach (var place in r.Legs)
            {
                w.Set(DoneKey(place), true);
                Places.Hold(w, place);
            }
            Walked?.Invoke(r.Legs[0]);
            return true;
        }

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
