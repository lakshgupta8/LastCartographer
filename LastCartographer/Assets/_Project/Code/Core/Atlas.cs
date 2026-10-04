using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>A place on the atlas page: a room id or named sub-zone, with the name the page prints.</summary>
    public sealed class AtlasPlace
    {
        public string Id;
        public string Name;
        public string Region;
    }

    /// <summary>A vantage on the page. Ids are "Place/Name"; the place is everything before the slash.</summary>
    public sealed class AtlasVantage
    {
        public string Id;
        public string Name;
        public string Place => Atlas.PlaceOf(Id);
    }

    public enum WaypointKind { Desk, Lamp }

    /// <summary>A travel point: a drafting desk or a lit lamp. Room is the scene, Spawn the point in it.</summary>
    public sealed class Waypoint
    {
        public string Id;
        public string Name;
        public string Place;
        public string Room;
        public string Spawn;
        public WaypointKind Kind;
    }

    /// <summary>
    /// The atlas (bible 10, docs/design/survey.md, PRG-10): what is drawn, what a Cantor has erased, and where
    /// Wren can travel. Surveying a vantage draws it. Erasing a place wipes every drawn vantage in it from the
    /// page and blanks its ink (FadeStages.Erase); re-surveying any vantage there brings the ink back to the
    /// stage it had. Desks and lit lamps are waypoints: known once stood at, and a destination while their
    /// place is on the page.
    /// </summary>
    public static class Atlas
    {
        public static event Action<string> PlaceErased;
        public static event Action<string> PlaceRecovered;
        public static event Action<Waypoint> WaypointFound;

        public static string PlaceOf(string vantageId)
        {
            if (string.IsNullOrEmpty(vantageId)) return "";
            int i = vantageId.IndexOf('/');
            return i < 0 ? vantageId : vantageId.Substring(0, i);
        }

        /// <summary>Draw a vantage. True when it was blank or erased; recovers an erased place's ink.</summary>
        public static bool Survey(WorldState w, string vantageId)
        {
            if (!w.MarkSurveyed(vantageId)) return false;
            var place = PlaceOf(vantageId);
            if (FadeStages.IsErased(w, place) && FadeStages.Recover(w, place)) PlaceRecovered?.Invoke(place);
            return true;
        }

        /// <summary>A Cantor's bell: every drawn vantage of the place leaves the page and its ink goes blank.
        /// False when already erased or anchored (the Guild's seal holds against a field bell).</summary>
        public static bool Erase(WorldState w, string place)
        {
            if (!FadeStages.Erase(w, place)) return false;
            foreach (var v in new List<string>(w.SurveyedVantages))
                if (PlaceOf(v) == place) w.MarkErased(v);
            PlaceErased?.Invoke(place);
            return true;
        }

        public static bool IsErased(WorldState w, string place) => FadeStages.IsErased(w, place);

        /// <summary>At least one vantage of the place is drawn and not erased.</summary>
        public static bool IsDrawn(WorldState w, string place)
        {
            if (string.IsNullOrEmpty(place)) return false;
            foreach (var v in w.SurveyedVantages)
                if (PlaceOf(v) == place && !w.IsErased(v)) return true;
            return false;
        }

        public static int DrawnCount(WorldState w, string place)
        {
            int n = 0;
            foreach (var v in w.SurveyedVantages)
                if (PlaceOf(v) == place && !w.IsErased(v)) n++;
            return n;
        }

        // ---- Waypoints -------------------------------------------------------------------------------

        /// <summary>Wren stood at a desk or under a lit lamp: it is on the page from now on.</summary>
        public static bool Discover(WorldState w, string waypointId)
        {
            var wp = FindWaypoint(waypointId);
            if (wp == null || !w.Waypoints.Add(waypointId)) return false;
            WaypointFound?.Invoke(wp);
            return true;
        }

        public static bool IsKnown(WorldState w, string waypointId) => !string.IsNullOrEmpty(waypointId) && w.Waypoints.Contains(waypointId);

        /// <summary>A destination: known, and its place drawn on the page (erasure takes it off again).</summary>
        public static bool CanTravelTo(WorldState w, Waypoint wp) => wp != null && IsKnown(w, wp.Id) && IsDrawn(w, wp.Place);

        /// <summary>Every destination other than the point Wren is travelling from.</summary>
        public static List<Waypoint> Destinations(WorldState w, string fromId)
        {
            var list = new List<Waypoint>();
            foreach (var wp in AllWaypoints)
                if (wp.Id != fromId && CanTravelTo(w, wp)) list.Add(wp);
            return list;
        }

        // ---- Catalog -----------------------------------------------------------------------------------

        static readonly List<AtlasPlace> _places = new List<AtlasPlace>();
        static readonly List<AtlasVantage> _vantages = new List<AtlasVantage>();
        static readonly List<Waypoint> _waypoints = new List<Waypoint>();
        static bool _defaults;

        public static IReadOnlyList<AtlasPlace> AllPlaces { get { EnsureDefaults(); return _places; } }
        public static IReadOnlyList<AtlasVantage> AllVantages { get { EnsureDefaults(); return _vantages; } }
        public static IReadOnlyList<Waypoint> AllWaypoints { get { EnsureDefaults(); return _waypoints; } }

        public static AtlasPlace FindPlace(string id) { EnsureDefaults(); return _places.Find(p => p.Id == id); }
        public static AtlasVantage FindVantage(string id) { EnsureDefaults(); return _vantages.Find(v => v.Id == id); }
        public static Waypoint FindWaypoint(string id) { EnsureDefaults(); return _waypoints.Find(p => p.Id == id); }

        public static List<AtlasVantage> VantagesOf(string place)
        {
            EnsureDefaults();
            return _vantages.FindAll(v => v.Place == place);
        }

        public static List<Waypoint> WaypointsOf(string place)
        {
            EnsureDefaults();
            return _waypoints.FindAll(p => p.Place == place);
        }

        /// <summary>The page's name for a place in the player's language ("place.&lt;id&gt;"; NAR-18): the catalog's, else the id with its underscores opened.</summary>
        public static string PlaceName(string place) => Loc.T("place." + place, EnglishPlaceName(place));

        public static string EnglishPlaceName(string place)
        {
            var p = FindPlace(place);
            return p != null ? p.Name : (place ?? "").Replace('_', ' ');
        }

        /// <summary>A region's heading on the page in the player's language ("region.&lt;slug&gt;").</summary>
        public static string RegionName(string region) => string.IsNullOrEmpty(region) ? "" : Loc.T(RegionKey(region), region);
        public static string RegionKey(string region) => "region." + Loc.Slug(region);

        /// <summary>A vantage's name in the player's language ("vantage.&lt;id&gt;").</summary>
        public static string VantageName(string vantageId) => Loc.T("vantage." + vantageId, EnglishVantageName(vantageId));

        public static string EnglishVantageName(string vantageId)
        {
            var v = FindVantage(vantageId);
            if (v != null) return v.Name;
            int i = (vantageId ?? "").IndexOf('/');
            return i < 0 ? vantageId ?? "" : vantageId.Substring(i + 1);
        }

        /// <summary>A desk's or lamp's name in the player's language ("waypoint.&lt;id&gt;").</summary>
        public static string WaypointName(string waypointId)
        {
            var wp = FindWaypoint(waypointId);
            return Loc.T("waypoint." + waypointId, wp != null ? wp.Name : waypointId ?? "");
        }

        public static void RegisterPlace(AtlasPlace place)
        {
            EnsureDefaults();
            int i = _places.FindIndex(p => p.Id == place.Id);
            if (i >= 0) _places[i] = place; else _places.Add(place);
        }

        public static void RegisterVantage(AtlasVantage vantage)
        {
            EnsureDefaults();
            int i = _vantages.FindIndex(v => v.Id == vantage.Id);
            if (i >= 0) _vantages[i] = vantage; else _vantages.Add(vantage);
        }

        public static void RegisterWaypoint(Waypoint wp)
        {
            EnsureDefaults();
            int i = _waypoints.FindIndex(p => p.Id == wp.Id);
            if (i >= 0) _waypoints[i] = wp; else _waypoints.Add(wp);
        }

        public static void Reset()
        {
            _places.Clear(); _vantages.Clear(); _waypoints.Clear();
            _defaults = false;
        }

        /// <summary>The greybox's page: the Edge and the three Saltmarrow rooms.</summary>
        public static void EnsureDefaults()
        {
            if (_defaults) return;
            _defaults = true;
            _places.Add(new AtlasPlace { Id = "Greyfold_Edge", Name = "The Edge", Region = "The Greyfold" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_Shore", Name = "The Shore", Region = "The Saltmarrow" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_A", Name = "The Drowned Quay", Region = "The Saltmarrow" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_Roots_2", Name = "Reedmother's Roots, the bole", Region = "The Saltmarrow" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_Roots_4", Name = "Reedmother's Roots, the crown", Region = "The Saltmarrow" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_B", Name = "Merrow's End", Region = "The Saltmarrow" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_Chain_1", Name = "The First Lighthouse", Region = "The Saltmarrow" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_Chain_2", Name = "The Second Lighthouse", Region = "The Saltmarrow" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_Chain_3", Name = "The Third Lighthouse, faded", Region = "The Saltmarrow" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_Lighthouse", Name = "The Fourth Lighthouse", Region = "The Saltmarrow" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_Chapel", Name = "The Salt Chapel", Region = "The Saltmarrow" });

            _vantages.Add(new AtlasVantage { Id = "Greyfold_Edge/HalfCathedral", Name = "the half-cathedral" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_Shore/Tideline", Name = "the tideline" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_A/Reedmother", Name = "the Reedmother" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_Roots_2/Bole", Name = "the bole" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_Roots_4/Crown", Name = "the crown" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_Chain_1/FirstLamp", Name = "the first lamp" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_Chain_2/SecondLamp", Name = "the second lamp" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_B/Tetherpost", Name = "the tether-post" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_Lighthouse/Lamp", Name = "the lamp" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_Chapel/Altar", Name = "the altar" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_BoneBridge", Name = "The Bone Bridge", Region = "The Saltmarrow" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_BoneBridge/Whale", Name = "the whale" });
            _places.Add(new AtlasPlace { Id = "Saltmarrow_IrisFields", Name = "The Pale Iris Fields", Region = "The Saltmarrow" });
            _vantages.Add(new AtlasVantage { Id = "Saltmarrow_IrisFields/Irises", Name = "the irises" });
            // Emberdown's (ENV-03), the Verdance's (ENV-04), Halden's (ENV-05), Windreach's (ENV-07), the Greyfold's and the Blank's (ENV-08) pages
            // are the plan's: every room a place, every planned vantage, a desk waypoint where the plan puts a desk. The Edge is its own entry above
            // (the prologue's room keeps its hand-built page, and its desk is Isolde's, not a waypoint).
            foreach (var plan in RoomPlans.All)
            {
                string region = plan.Id.StartsWith("Emberdown_") ? "Emberdown" : plan.Id.StartsWith("Verdance_") ? "The Verdance" : plan.Id.StartsWith("Halden_") ? "Halden" : plan.Id.StartsWith("Windreach_") ? "Windreach"
                              : plan.Id.StartsWith("Greyfold_") ? "The Greyfold" : plan.Id.StartsWith("Blank_") ? "The Blank" : null;
                if (region == null || plan.Id == "Greyfold_Edge") continue;
                _places.Add(new AtlasPlace { Id = plan.Id, Name = plan.Name, Region = region });
                if (plan.Vantage != null) _vantages.Add(new AtlasVantage { Id = plan.VantageId, Name = "the " + plan.Vantage.ToLowerInvariant() });
                if (plan.Desk) _waypoints.Add(new Waypoint { Id = "desk." + plan.Id, Kind = WaypointKind.Desk, Place = plan.Id, Room = "Greybox_" + plan.Id, Spawn = "Desk", Name = Possessive(plan.Name.ToLowerInvariant()) + " desk" });
            }

            static string Possessive(string name) => name.EndsWith("s") ? name + "'" : name + "'s";   // the wagons' desk, the hall's desk

            _waypoints.Add(new Waypoint { Id = "desk.Saltmarrow_A", Kind = WaypointKind.Desk, Place = "Saltmarrow_A", Room = "Greybox_Saltmarrow_A", Spawn = "Desk", Name = "the quay's desk" });
            _waypoints.Add(new Waypoint { Id = "desk.Saltmarrow_Lighthouse", Kind = WaypointKind.Desk, Place = "Saltmarrow_Lighthouse", Room = "Greybox_Saltmarrow_Lighthouse", Spawn = "Desk", Name = "the lighthouse desk" });
            _waypoints.Add(new Waypoint { Id = "desk.Saltmarrow_Chapel", Kind = WaypointKind.Desk, Place = "Saltmarrow_Chapel", Room = "Greybox_Saltmarrow_Chapel", Spawn = "Desk", Name = "the chapel's desk" });
            _waypoints.Add(new Waypoint { Id = "lamp.Saltmarrow_Lighthouse", Kind = WaypointKind.Lamp, Place = "Saltmarrow_Lighthouse", Room = "Greybox_Saltmarrow_Lighthouse", Spawn = "Lamp", Name = "the fourth lamp" });
        }
    }
}
