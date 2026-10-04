using System;

namespace OWSBG.Core
{
    /// <summary>
    /// A gate on a way between two rooms (world-map.md §2): the ability the way needs, the story flag it waits for, and
    /// whether skill may cross it without the ability (soft: only Wingbeat gaps are). A story gate is hard, never soft,
    /// and a gate applies in both directions.
    /// </summary>
    [Serializable]
    public struct Gate
    {
        public Ability Needs;
        public string Flag;
        public bool Soft;

        public bool IsNone => Needs == Ability.None && string.IsNullOrEmpty(Flag);

        /// <summary>
        /// Whether the way is open with this kit and these flags: the flag set, and the ability held unless the gate is
        /// soft (a skilled pogo crosses a Wingbeat gap; the game notices, <see cref="SequenceBreaks"/>).
        /// </summary>
        public bool IsOpen(Ability have, Func<string, bool> hasFlag)
        {
            if (!string.IsNullOrEmpty(Flag) && (hasFlag == null || !hasFlag(Flag))) return false;
            if (Needs == Ability.None || (have & Needs) == Needs) return true;
            return Soft;
        }

        /// <summary>Whether something stands in the way while the gate is shut: a story gate, or a hard ability gate. A soft gap never bars.</summary>
        public bool Bars => !string.IsNullOrEmpty(Flag) || (Needs != Ability.None && !Soft);

        public override string ToString()
        {
            if (IsNone) return "open";
            string s = Needs == Ability.None ? "" : Needs.ToString() + (Soft ? " (soft)" : "");
            if (!string.IsNullOrEmpty(Flag)) s += (s.Length > 0 ? ", " : "") + Flag;
            return s;
        }
    }

    /// <summary>
    /// The gate on the way from one built room to another, as the plans and the macro map say it. A planned room's exit
    /// carries its own (<see cref="RoomPlans"/>); the coast's rooms, which were built before the plans, take the map's link
    /// between their zones (<see cref="WorldGraph"/>), and two rooms in one zone with no planned gate have none. The
    /// Edge's doors are shut while the prologue plays. Pure: the builder stands the gate on the transition, the
    /// transition reads it.
    /// </summary>
    public static class Gates
    {
        /// <summary>The prologue's end (PrologueDirector.Crossed): until it, the Edge's two doors are shut.</summary>
        public const string PrologueFlag = "prologue.crossed";
        public const string EdgeRoom = "Greyfold_Edge";

        public static Gate Between(string fromPlace, string toPlace)
        {
            var g = new Gate { Flag = null };
            if (string.IsNullOrEmpty(fromPlace) || string.IsNullOrEmpty(toPlace)) return g;
            var plan = RoomPlans.Find(fromPlace);
            string toZone = WorldGraph.ZoneOfPlace(toPlace) ?? RoomPlans.Find(toPlace)?.Zone;
            RoomExit exit = null;
            if (plan != null)
            {
                exit = plan.Exits.Find(e => e.To == toPlace);
                if (exit == null && toZone != null) exit = plan.Exits.Find(e => e.IsExternal && e.To == toZone);
            }
            if (exit != null)
            {
                g.Needs = exit.Needs; g.Flag = exit.Flag; g.Soft = exit.Soft;
            }
            else
            {
                string fromZone = WorldGraph.ZoneOfPlace(fromPlace) ?? plan?.Zone;
                if (fromZone != null && toZone != null && fromZone != toZone)
                {
                    foreach (var link in WorldGraph.Links)
                    {
                        if (!link.Joins(fromZone) || !link.Joins(toZone)) continue;
                        g.Needs = link.Needs; g.Flag = link.Flag; g.Soft = link.Soft;
                        break;
                    }
                }
            }
            if (fromPlace == EdgeRoom && string.IsNullOrEmpty(g.Flag)) g.Flag = PrologueFlag;
            if (string.IsNullOrEmpty(g.Flag)) g.Flag = null;
            return g;
        }

        /// <summary>The place a room scene draws: its id without the greybox prefix.</summary>
        public static string PlaceOfScene(string scene)
        {
            if (string.IsNullOrEmpty(scene)) return scene;
            return scene.StartsWith(WorldGraph.GreyboxPrefix, StringComparison.Ordinal) ? scene.Substring(WorldGraph.GreyboxPrefix.Length) : scene;
        }
    }
}
