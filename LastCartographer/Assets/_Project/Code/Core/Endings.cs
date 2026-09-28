using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>The four endings (bible 9.1–9.4). Unlabelled in-game; named here for production.</summary>
    public enum Ending { None = 0, Fixed = 1, Open = 2, Unwritten = 3, Rest = 4 }

    /// <summary>
    /// What each ending needs, as flag logic (bible 9; DES-12 will prove each reachable), which one was chosen, and the
    /// epilogue walk that follows it (7.4: Halden, one outer region, then Marrow). The frame offers only what is open.
    /// </summary>
    public static class Endings
    {
        public const string ChosenFlag = "ending.chosen";
        public const string CorvinStanceFlag = "corvin.stance";
        public const int CorvinCooperates = 1, CorvinPersuaded = 2, CorvinUnpersuaded = 3;
        public const string RestOfferedFlag = "ending.rest_offered";
        public const string VossChangedFlag = "voss.changed";

        /// <summary>The five names (halden-arc.md §2): one per region, each a way to hold or release a place without the Guild.</summary>
        public static readonly string[] FiveNames = { "sable", "runa", "teodor", "idrenne", "pell" };

        /// <summary>Allied, by what each did with Wren. Nobody in the five dies in v1, so "alive" holds for all.</summary>
        public static bool Allied(WorldState w, string name)
        {
            switch (name)
            {
                case "sable": return w.Is("sable.tether_sold") || w.Is("saltmarrow.sable.walked");   // she went with the tether, or respected the walk
                case "runa": return w.Is("runa.named_wren") || w.Is("emberdown.hollowvein.walked");  // counted her in, or walked Hollowvein beside her
                case "teodor": return w.Is("teodor.keystone_given");                                  // the honest answer
                case "idrenne": return w.Is("windreach.camp.walked");                                 // walked the three fires: known
                case "pell": return w.Is("pell.report_kept");                                         // kept it
                default: return false;
            }
        }

        public static int AlliedCount(WorldState w) => FiveNames.Count(n => Allied(w, n));

        /// <summary>The stones at the frame: what Wren carries, and the Observatory's own, which has not left it in forty-one years.</summary>
        public static int StonesAtTheFrame(WorldState w) => Keystones.Count(w) + 1;

        /// <summary>Bible 9.2: witnessed Idrenne's Fire, and one of Aldermere's last day or Hollowvein's walk.</summary>
        public static bool Witnessed(WorldState w)
            => Steppe.FireWitnessed(w) && (w.Is("verdance.aldermere.attended") || w.Is("emberdown.hollowvein.walked"));

        public static int CorvinStance(WorldState w) => w.Get(CorvinStanceFlag);

        public static bool IsOpen(WorldState w, Ending e)
        {
            switch (e)
            {
                case Ending.Fixed:
                    return StonesAtTheFrame(w) == 7 && CorvinStance(w) == CorvinCooperates;
                case Ending.Open:
                    return Keystones.Count(w) >= Keystones.OpenWorldNeeds && AlliedCount(w) >= 3 && Witnessed(w)
                           && CorvinStance(w) == CorvinPersuaded;
                case Ending.Unwritten:
                    return w.Is("teodor.keystone_given") && w.Is("verdance.aldermere.attended") && !w.Is("verdance.aldermere.stopped");
                case Ending.Rest:
                    return Keystones.Count(w) == 0 && Places.Count(w, PlaceFate.Anchored) == 0 && w.Is(RestOfferedFlag);
                default:
                    return false;
            }
        }

        /// <summary>Whether the frame has anything to offer her yet (the Rest is offered in the Blank, not at the frame).</summary>
        public static bool AnyAtTheFrame(WorldState w) => IsOpen(w, Ending.Fixed) || IsOpen(w, Ending.Open) || IsOpen(w, Ending.Unwritten);

        public static Ending Chosen(WorldState w) => (Ending)w.Get(ChosenFlag);

        public static bool Choose(WorldState w, Ending e)
        {
            if (Chosen(w) != Ending.None || !IsOpen(w, e)) return false;
            w.Set(ChosenFlag, (int)e);
            return true;
        }

        /// <summary>Bible 7.4: Halden, then one outer region, then Marrow. The outer region is the ending's own (endings.md §3).</summary>
        public static string[] EpilogueWalk(Ending e)
        {
            switch (e)
            {
                case Ending.Fixed: return new[] { "Epilogue_Pell", "Epilogue_Sable", "Epilogue_Marrow" };
                case Ending.Open: return new[] { "Epilogue_Pell", "Epilogue_Runa", "Epilogue_Marrow" };
                case Ending.Unwritten: return new[] { "Epilogue_Pell", "Epilogue_Teodor", "Epilogue_Marrow" };
                case Ending.Rest: return new[] { "Epilogue_Pell", "Epilogue_Marrow" };
                default: return new string[0];
            }
        }

        /// <summary>The walk's stops with where each plays, from the cast's placing of the epilogue scenes.</summary>
        public static List<(string Node, string Zone)> EpilogueStops(Ending e)
        {
            var stops = new List<(string, string)>();
            foreach (var node in EpilogueWalk(e))
            {
                var at = Cast.Appearances.FirstOrDefault(a => a.Node == node);
                stops.Add((node, at?.Zone));
            }
            return stops;
        }

        public static bool TryParse(string s, out Ending e)
        {
            e = Ending.None;
            if (string.IsNullOrWhiteSpace(s)) return false;
            return System.Enum.TryParse(s.Trim(), true, out e) && e != Ending.None;
        }
    }
}
