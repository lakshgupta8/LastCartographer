using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>One region's gauntlet (combat doc §9): a pure platforming sequence built around the region's ability.</summary>
    public sealed class GauntletPlan
    {
        public string Id;
        public string Name;
        public Region Region;
        /// <summary>The <see cref="WorldGraph"/> zone it stands in.</summary>
        public string Zone;
        /// <summary>The planned room it is (<see cref="RoomPlans"/>), or null where it is a line off a built room.</summary>
        public string Room;
        /// <summary>What it is built around: without these it cannot be crossed.</summary>
        public Ability Needs;
        public string Purpose;
        public string FlagKey => Gauntlets.FlagKey(Id);
    }

    /// <summary>
    /// The gauntlets as data (CMB-18): one per region but the Blank, whose traversal is the drift and Clarity. The
    /// Lantern Chain jumps, the Furnace Stair's shafts, the canopy threads, the flyer-tower, the Windreach updrafts, and
    /// the Road That Stops, where the platforms exist only in her lantern-radius. Crossing one writes its flag and
    /// pays a scrap, once.
    /// </summary>
    public static class Gauntlets
    {
        public const string ScenePrefix = "Gauntlet_";
        static readonly List<GauntletPlan> _all = new List<GauntletPlan>();

        public static IReadOnlyList<GauntletPlan> All { get { EnsureDefaults(); return _all; } }
        public static GauntletPlan Find(string id) { EnsureDefaults(); return _all.Find(g => g.Id == id); }

        /// <summary>A gauntlet's name in the player's language ("gauntlet.&lt;id&gt;"; NAR-18).</summary>
        public static string NameOf(GauntletPlan plan) => plan == null ? "" : Loc.T("gauntlet." + plan.Id, plan.Name);
        public static string FlagKey(string id) => Keys.Of("gauntlet.", id, ".done");
        public static string SceneFor(string id) => ScenePrefix + id;
        public static bool IsGauntletScene(string scene) => !string.IsNullOrEmpty(scene) && scene.StartsWith(ScenePrefix);

        static void G(string id, string name, Region region, string zone, string room, Ability needs, string purpose)
            => _all.Add(new GauntletPlan { Id = id, Name = name, Region = region, Zone = zone, Room = room, Needs = needs, Purpose = purpose });

        static void EnsureDefaults()
        {
            if (_all.Count > 0) return;
            G("lamp_posts", "The lamp posts", Region.Saltmarrow, "Saltmarrow.LanternChain", null, Ability.Wingbeat,
                "The old lamp posts between the lighthouses, over the tide: a jump and a Wingbeat each, and the tide below.");
            G("furnace_shafts", "The furnace shafts", Region.Emberdown, "Emberdown.NineChimneys", "Emberdown_Chimneys_2", Ability.Talonhold,
                "Wall to wall up a chimney too tall to jump; the vents on the walls are hot, and a hand on one lets go.");
            G("canopy_threads", "The canopy threads", Region.Verdance, "Verdance.LanternGrove", "Verdance_Grove_1", Ability.Inkthread,
                "Anchor to anchor over the forest floor, too wide for any jump; thorns below.");
            G("flyer_tower", "The flyer-tower", Region.Halden, "Halden.Bastion", "Halden_Bastion_1", Ability.Talonhold | Ability.Inkthread,
                "A tower built for flyers: no stairs. Talonhold up the walls, Inkthread across the gap; the mill race below.");
            G("updrafts", "The updrafts", Region.Windreach, "Windreach.WindGate", "Windreach_Gate_2", Ability.Windmemory,
                "The first glide course: ink-swirls to ride over the long grass, up to a ledge no jump reaches.");
            G("road_that_stops", "The Road That Stops", Region.Greyfold, "Greyfold.RoadThatStops", "Greyfold_Road_1", Ability.Clarity,
                "Cobbles that exist only in her lantern-radius; outside it, outlines, and under them the white.");
        }
    }
}
