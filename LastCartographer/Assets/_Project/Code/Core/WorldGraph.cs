using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    public enum Region { Saltmarrow, Emberdown, Verdance, Halden, Windreach, Greyfold, Blank }

    /// <summary>A sub-zone of a region (bible 4.x): a handful of rooms with a name, its vantages, and what it holds.</summary>
    public sealed class Zone
    {
        public string Id;
        public Region Region;
        public string Name;
        public int Rooms;
        public int Vantages;
        /// <summary>The region's hub: drafting desk, Commissions ledger, people.</summary>
        public bool IsHub;
        public string Boss;
        public bool Keystone;
        /// <summary>The movement ability found here, if any.</summary>
        public Ability Grants;
        public string Decision;
    }

    /// <summary>A way between two zones and what it asks: an ability, a story flag, or nothing. Two-way.</summary>
    public sealed class Link
    {
        public string From;
        public string To;
        public Ability Needs;
        public string Flag;
        /// <summary>A soft gate can be crossed without the ability by skilled play (pogo, wall tricks); hard ones cannot.</summary>
        public bool Soft;
        public string Note;

        public bool Joins(string zone) => From == zone || To == zone;
        public string Other(string zone) => From == zone ? To : From;
    }

    /// <summary>
    /// The macro map (DES-07, docs/design/world-map.md): regions, their sub-zones, the ways between them and the
    /// gates on those ways. The spine is Wingbeat → (Talonhold or Inkthread) → the Plateau → the other →
    /// Windmemory → Clarity → the Sky; Reachable() proves it from the data. Room and vantage counts are the
    /// scope targets, kept here so the design and the tests read the same numbers.
    /// </summary>
    public static class WorldGraph
    {
        public const string Start = "Saltmarrow.Shore";

        static readonly List<Zone> _zones = new List<Zone>();
        static readonly List<Link> _links = new List<Link>();
        static readonly Dictionary<string, string> _placeZones = new Dictionary<string, string>();
        static bool _built;

        public static IReadOnlyList<Zone> Zones { get { EnsureDefaults(); return _zones; } }
        public static IReadOnlyList<Link> Links { get { EnsureDefaults(); return _links; } }

        public static Zone Find(string id) { EnsureDefaults(); return _zones.Find(z => z.Id == id); }
        public static List<Zone> ZonesOf(Region r) { EnsureDefaults(); return _zones.FindAll(z => z.Region == r); }
        public static Zone HubOf(Region r) { EnsureDefaults(); return _zones.Find(z => z.Region == r && z.IsHub); }
        public static List<Link> LinksOf(string zone) { EnsureDefaults(); return _links.FindAll(l => l.Joins(zone)); }

        /// <summary>The zone a greybox atlas place (room id) belongs to.</summary>
        public static string ZoneOfPlace(string placeId) { EnsureDefaults(); return _placeZones.TryGetValue(placeId ?? "", out var z) ? z : null; }

        public static int RoomCount() { int n = 0; foreach (var z in Zones) n += z.Rooms; return n; }
        public static int VantageCount() { int n = 0; foreach (var z in Zones) n += z.Vantages; return n; }
        /// <summary>Named sub-zones outside the Blank, hubs aside (the bible's "~34").</summary>
        public static int SubZoneCount() { int n = 0; foreach (var z in Zones) if (!z.IsHub && z.Region != Region.Blank) n++; return n; }

        public static bool Passable(Link l, Ability have, Func<string, bool> hasFlag, bool allowSoft)
        {
            if (!string.IsNullOrEmpty(l.Flag) && (hasFlag == null || !hasFlag(l.Flag))) return false;
            if (l.Needs == Ability.None || (have & l.Needs) == l.Needs) return true;
            return allowSoft && l.Soft;
        }

        /// <summary>Every zone reachable from the shore with these abilities and flags.</summary>
        public static HashSet<string> Reachable(Ability have, Func<string, bool> hasFlag = null, bool allowSoft = false, string from = Start)
        {
            EnsureDefaults();
            var seen = new HashSet<string> { from };
            var open = new Stack<string>();
            open.Push(from);
            while (open.Count > 0)
            {
                var at = open.Pop();
                foreach (var l in _links)
                {
                    if (!l.Joins(at) || !Passable(l, have, hasFlag, allowSoft)) continue;
                    var next = l.Other(at);
                    if (seen.Add(next)) open.Push(next);
                }
            }
            return seen;
        }

        public static HashSet<string> Reachable(WorldState w, Ability have, bool allowSoft = false) => Reachable(have, w.Is, allowSoft);

        public static void Reset() { _zones.Clear(); _links.Clear(); _placeZones.Clear(); _built = false; }

        static Zone Add(Region r, string id, string name, int rooms, int vantages, bool hub = false, string boss = null, bool keystone = false, Ability grants = Ability.None, string decision = null)
        {
            var z = new Zone { Id = r + "." + id, Region = r, Name = name, Rooms = rooms, Vantages = vantages, IsHub = hub, Boss = boss, Keystone = keystone, Grants = grants, Decision = decision };
            _zones.Add(z);
            return z;
        }

        static void Way(string from, string to, Ability needs = Ability.None, string flag = null, bool soft = false, string note = null)
            => _links.Add(new Link { From = from, To = to, Needs = needs, Flag = flag, Soft = soft, Note = note });

        public static void EnsureDefaults()
        {
            if (_built) return;
            _built = true;
            const Region S = Region.Saltmarrow, E = Region.Emberdown, V = Region.Verdance, H = Region.Halden, W = Region.Windreach, G = Region.Greyfold, B = Region.Blank;

            // ---- Saltmarrow (bible 4.1): the coast; Act 1's first push.
            Add(S, "Shore", "The Shore", 1, 1);
            Add(S, "Quay", "The Drowned Quay", 3, 1, hub: true);
            Add(S, "Reedmother", "Reedmother's Roots", 4, 2);
            Add(S, "IrisFields", "The Pale Iris Fields", 3, 1, boss: "6.2 Reedmother's Brood (optional)");
            Add(S, "MerrowsEnd", "Merrow's End", 3, 1, decision: "anchor / hold (needs Emberdown) / release");
            Add(S, "LanternChain", "The Lantern Chain", 7, 3, boss: "6.1 The Lamp-Keeper", grants: Ability.Wingbeat);
            Add(S, "SaltChapel", "The Salt Chapel", 3, 1, boss: "6.3 Halvard (first hunt)");
            Add(S, "BoneBridge", "The Bone Bridge", 3, 1);
            Way("Saltmarrow.Shore", "Saltmarrow.Quay");
            Way("Saltmarrow.Quay", "Saltmarrow.Reedmother");
            Way("Saltmarrow.Quay", "Saltmarrow.MerrowsEnd");
            Way("Saltmarrow.Reedmother", "Saltmarrow.IrisFields");
            Way("Saltmarrow.MerrowsEnd", "Saltmarrow.LanternChain");
            Way("Saltmarrow.LanternChain", "Saltmarrow.SaltChapel", Ability.Wingbeat, soft: true, note: "the gap between the fifth and sixth lighthouses");
            Way("Saltmarrow.SaltChapel", "Saltmarrow.BoneBridge", Ability.Wingbeat, soft: true);
            Way("Saltmarrow.LanternChain", "Blank.AurysLighthouse", flag: "saltmarrow.tether", note: "the faded third lighthouse, by tether");

            // ---- Emberdown (4.2): the climb by the old way.
            Add(E, "FurnaceStair", "The Furnace Stair", 3, 1, boss: "6.5 Brann, in the furnace");
            Add(E, "KettilsRest", "Kettil's Rest", 3, 1, hub: true);
            Add(E, "RollCallBell", "The Roll-Call Bell", 2, 1);
            Add(E, "NineChimneys", "The Nine Chimneys", 4, 2, grants: Ability.Talonhold);
            Add(E, "CinderBaths", "The Cinder Baths", 3, 1);
            Add(E, "Overlook", "The Overlook", 2, 1);
            Add(E, "Hollowvein", "Hollowvein", 4, 1, boss: "6.4 the bounds-walk down", keystone: true, decision: "leave it buried / walk it");
            Way("Saltmarrow.BoneBridge", "Emberdown.FurnaceStair", Ability.Wingbeat, soft: true, note: "the climb to Emberdown");
            Way("Emberdown.FurnaceStair", "Emberdown.KettilsRest");
            Way("Emberdown.KettilsRest", "Emberdown.RollCallBell");
            Way("Emberdown.KettilsRest", "Emberdown.NineChimneys", note: "Runa teaches Talonhold in the first chimney");
            Way("Emberdown.NineChimneys", "Emberdown.CinderBaths", Ability.Talonhold);
            Way("Emberdown.CinderBaths", "Emberdown.Overlook", Ability.Talonhold);
            Way("Emberdown.KettilsRest", "Emberdown.Hollowvein", Ability.Talonhold, flag: "emberdown.hollowvein_opened");
            Way("Emberdown.Overlook", "Halden.SevenBridges", Ability.Talonhold, note: "the road to the Plateau");

            // ---- The Verdance (4.3): the climb by the Unwriters' way.
            Add(V, "OldRoad", "The Old Road", 3, 1);
            Add(V, "QuietHouse", "The Quiet House", 3, 1, hub: true, keystone: true);   // Teodor has it
            Add(V, "RootChapel", "The Root Chapel", 2, 1, grants: Ability.Inkthread);
            Add(V, "LanternGrove", "The Lantern Grove", 4, 2);
            Add(V, "SunkenLibrary", "The Sunken Library", 2, 1);
            Add(V, "Aldermere", "Aldermere", 3, 1, boss: "6.6 The Choir (optional)", decision: "attend the last day / stop it");
            Add(V, "OvergrownGate", "The Overgrown Gate", 2, 1, boss: "6.7 The Gatekeeper");
            Way("Saltmarrow.IrisFields", "Verdance.OldRoad", Ability.Wingbeat, soft: true, note: "the iris gap");
            Way("Verdance.OldRoad", "Verdance.QuietHouse");
            Way("Verdance.QuietHouse", "Verdance.RootChapel", note: "Teodor teaches Inkthread");
            Way("Verdance.RootChapel", "Verdance.LanternGrove", Ability.Inkthread);
            Way("Verdance.LanternGrove", "Verdance.SunkenLibrary", Ability.Inkthread);
            Way("Verdance.QuietHouse", "Verdance.Aldermere");
            Way("Verdance.Aldermere", "Verdance.OvergrownGate", Ability.Inkthread);
            Way("Verdance.OvergrownGate", "Halden.PaperMills", Ability.Inkthread, note: "the canopy road to the Plateau");

            // ---- Halden Reach (4.4): the Citadel, mid-game hub of Act 2.
            Add(H, "SevenBridges", "The Seven Bridges", 4, 2, boss: "6.3 Halvard (second hunt)");
            Add(H, "PaperMills", "The Paper Mills", 3, 1);
            Add(H, "Lowmarket", "Lowmarket", 3, 1, decision: "the Paper Mill Strike");
            Add(H, "JourneymansHall", "The Journeyman's Hall", 3, 1, hub: true);
            Add(H, "OldOrchard", "The Old Orchard", 2, 1);
            Add(H, "Bastion", "The Bastion", 3, 1, boss: "6.8 Oriel (if Pell's report is sent)");
            Add(H, "Observatory", "The Observatory", 2, 1, boss: "6.15 The Complete Survey", keystone: true);
            Add(H, "Vault", "The Vault", 1, 0);
            Way("Halden.SevenBridges", "Halden.PaperMills");
            Way("Halden.SevenBridges", "Halden.Lowmarket");
            Way("Halden.PaperMills", "Halden.JourneymansHall");
            Way("Halden.JourneymansHall", "Halden.OldOrchard");
            Way("Halden.OldOrchard", "Halden.Bastion", Ability.Talonhold | Ability.Inkthread, note: "the flyer-towers, no stairs");
            Way("Halden.Bastion", "Halden.Observatory", flag: "act3.started");
            Way("Halden.Observatory", "Halden.Vault", flag: "halden.vault_opened");
            Way("Halden.OldOrchard", "Greyfold.EdgeCamp", flag: "isolde.cache", note: "Act 1's end: back to the Edge");
            Way("Halden.Lowmarket", "Windreach.NineStones", flag: "act2.started", note: "the south road");

            // ---- Windreach (4.5): the proof.
            Add(W, "NineStones", "The Nine Stones", 3, 2, boss: "6.9 Surveyor Hale (optional)");
            Add(W, "LongGrassCamp", "The Long Grass Camp", 2, 1, hub: true);
            Add(W, "DryRiver", "The Dry River", 3, 1);
            Add(W, "WindGate", "The Wind Gate", 2, 1, grants: Ability.Windmemory);
            Add(W, "IdrennesFire", "Idrenne's Fire", 2, 1, keystone: true, decision: "survey Windreach at all");
            Add(W, "FallenStar", "The Fallen Star", 2, 1, boss: "6.10 The Fallen Star (optional)");
            Way("Windreach.NineStones", "Windreach.LongGrassCamp");
            Way("Windreach.LongGrassCamp", "Windreach.DryRiver", Ability.Wingbeat, soft: true);
            Way("Windreach.DryRiver", "Windreach.WindGate", Ability.Talonhold);
            Way("Windreach.WindGate", "Windreach.IdrennesFire", Ability.Windmemory);
            Way("Windreach.IdrennesFire", "Windreach.FallenStar", Ability.Windmemory);
            Way("Windreach.WindGate", "Greyfold.MirrorPool", Ability.Windmemory, note: "the Greyfold approach");

            // ---- The Greyfold (4.6): the threshold.
            Add(G, "EdgeCamp", "The Edge Camp", 2, 1, hub: true);
            Add(G, "HalfCathedral", "The Half-Cathedral", 2, 1, boss: "6.12 The Half-Cathedral Bells");
            Add(G, "IsoldesLastCamp", "Isolde's Last Camp", 1, 1);
            Add(G, "RoadThatStops", "The Road That Stops", 3, 1, grants: Ability.Clarity);
            Add(G, "MirrorPool", "The Mirror Pool", 2, 1);
            Add(G, "Threshold", "The Threshold", 2, 0, boss: "6.11 Voss at the Threshold, 6.3 Halvard (third)");
            Way("Greyfold.EdgeCamp", "Greyfold.HalfCathedral", note: "the prologue's edge");
            Way("Greyfold.HalfCathedral", "Greyfold.RoadThatStops", note: "Act 1's end: she steps in and stays herself");
            Way("Greyfold.EdgeCamp", "Greyfold.IsoldesLastCamp", Ability.Clarity);
            Way("Greyfold.RoadThatStops", "Greyfold.MirrorPool", Ability.Clarity);
            Way("Greyfold.MirrorPool", "Greyfold.Threshold", Ability.Clarity, flag: "act2.threshold");
            Way("Greyfold.Threshold", "Blank.ThessalyHollow", flag: "greyfold.crossed");

            // ---- The Blank (4.7): fixed islands; the rest is generated from WorldState (PRG-20).
            Add(B, "ThessalyHollow", "Thessaly Hollow", 3, 0, hub: true);
            Add(B, "OldCapital", "The Old Capital District", 4, 0, boss: "6.13 Corra, 6.14 The Archivist", keystone: true);
            Add(B, "AurysLighthouse", "Aury's Lighthouse", 2, 0, keystone: true);
            Way("Blank.ThessalyHollow", "Blank.OldCapital");
            Way("Blank.ThessalyHollow", "Blank.AurysLighthouse", Ability.Clarity);

            // ---- The greybox rooms, placed on the map.
            _placeZones["Greyfold_Edge"] = "Greyfold.HalfCathedral";
            _placeZones["Saltmarrow_Shore"] = "Saltmarrow.Shore";
            _placeZones["Saltmarrow_A"] = "Saltmarrow.Quay";
            _placeZones["Saltmarrow_Stilts"] = "Saltmarrow.Quay";
            _placeZones["Saltmarrow_Boardwalk"] = "Saltmarrow.Quay";
            _placeZones["Saltmarrow_Roots_1"] = "Saltmarrow.Reedmother";
            _placeZones["Saltmarrow_Roots_2"] = "Saltmarrow.Reedmother";
            _placeZones["Saltmarrow_Roots_3"] = "Saltmarrow.Reedmother";
            _placeZones["Saltmarrow_Roots_4"] = "Saltmarrow.Reedmother";
            _placeZones["Saltmarrow_B"] = "Saltmarrow.MerrowsEnd";
            _placeZones["Saltmarrow_Tetherline"] = "Saltmarrow.MerrowsEnd";
            _placeZones["Saltmarrow_Ferry"] = "Saltmarrow.MerrowsEnd";
            _placeZones["Saltmarrow_Chain_1"] = "Saltmarrow.LanternChain";
            _placeZones["Saltmarrow_Chain_2"] = "Saltmarrow.LanternChain";
            _placeZones["Saltmarrow_Chain_3"] = "Saltmarrow.LanternChain";
            _placeZones["Saltmarrow_Lighthouse"] = "Saltmarrow.LanternChain";
            _placeZones["Saltmarrow_Chapel"] = "Saltmarrow.SaltChapel";
        }
    }
}
