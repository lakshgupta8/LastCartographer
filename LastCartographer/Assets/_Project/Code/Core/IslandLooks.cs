using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>One prop on an island: the kit's drawing and where its feet stand.</summary>
    public sealed class IslandProp
    {
        public string Name;
        public float X;
        public IslandProp(string name, float x) { Name = name; X = x; }
    }

    /// <summary>
    /// How an island is drawn (docs/design/blank-generator.md §3): a piece of the place it was, torn out of its own
    /// region's kit and greyed, standing in the Blank's white. The strips, the tile and the props are the region's
    /// own drawings (materials under Art/Materials, loaded by name at runtime); the people are the bodies they were
    /// met in, and the crowd is the region's townsfolk standing round as Remnant.
    /// </summary>
    public sealed class IslandLook
    {
        public string Region;          // the kit: "Emberdown"
        public string Mid, Far;        // its strips: "Mid_Gallery", "Far_Dark"
        public string Tile;            // its floor: "Ground_Timber"
        public IslandProp[] Props = new IslandProp[0];
        public string Speaker;         // the talker's character: "Dotha", "Folk_Chough"
        public string[] Crowd = new string[0];   // townsfolk looks standing round: "Raven"
        public string CrowdActivity = "";        // what they stand doing: "watching", "cheering", "asleep"
        public float Wash;             // how far the place's colour has gone toward the paper (0 drawn, 1 white)

        public string MidMaterial => "M_Paper_" + Mid;
        public string FarMaterial => "M_Paper_" + Far;
        public string TileMaterial => "M_" + Tile;
        public string PropMaterial(IslandProp p) => IslandLooks.PropMaterial(Region, p.Name);
    }

    public static class IslandLooks
    {
        /// <summary>The Blank's own far layers behind every island: its islands going past, and the grey.</summary>
        public const string BlankFar = "M_Paper_Far_Islands", BlankFarther = "M_Paper_Farther_Grey";
        /// <summary>The white either side of the island: the Blank's floor.</summary>
        public const string WhiteTile = "M_Ground_Grey";
        /// <summary>The ink material the people are drawn with; each body's sheet is set on its own renderer.</summary>
        public const string PeopleMaterial = "M_Folk_Chough";

        /// <summary>A kit's prop material: the coast's, the first kit, carry no region ("M_Prop_Stoop"); the others do ("M_Prop_Wheel_Halden").</summary>
        public static string PropMaterial(string region, string prop) => region == "Saltmarrow" ? "M_Prop_" + prop : "M_Prop_" + prop + "_" + region;

        /// <summary>The five authored islands, drawn as the place each was (blank-islands.md §1).</summary>
        public static readonly IReadOnlyDictionary<string, IslandLook> Authored = new Dictionary<string, IslandLook>
        {
            // Dotha's stoop on the boardwalk, nets and moorings; the village stands round to hear the eleventh song.
            ["Merrows_End"] = new IslandLook
            {
                Region = "Saltmarrow", Mid = "Mid_Reeds", Far = "Far_Roosts", Tile = "Ground_Boardwalk",
                Props = new[] { new IslandProp("Stoop", 1.2f), new IslandProp("Nets", 8f), new IslandProp("Moorings", -9f) },
                Speaker = "Dotha", Crowd = new[] { "Gull", "Eider", "Tern" }, CrowdActivity = "watching", Wash = 0.45f,
            },
            // Thirty-one miners standing in the gallery's timber, the lamps on their hooks, the tally on the wall.
            ["Hollowvein"] = new IslandLook
            {
                Region = "Emberdown", Mid = "Mid_Gallery", Far = "Far_Dark", Tile = "Ground_Timber",
                Props = new[] { new IslandProp("HookLamps", -7.5f), new IslandProp("TallyWall", 8.5f) },
                Speaker = Townsfolk.Prefix + "Chough",
                Crowd = new[] { "Raven", "Dipper", "Ptarmigan", "Ouzel", "Grouse", "Chough", "Dipper", "Raven" }, CrowdActivity = "watching", Wash = 0.55f,
            },
            // The festival, tonight all night: bunting and lanterns, the village cheering.
            ["Aldermere"] = new IslandLook
            {
                Region = "Verdance", Mid = "Mid_Village", Far = "Far_Lanterns", Tile = "Ground_Lane",
                Props = new[] { new IslandProp("Bunting", -4f), new IslandProp("Lantern", 8.5f), new IslandProp("Lantern", -9.5f) },
                Speaker = "Hollin", Crowd = new[] { "Finch", "Jay", "Woodpecker", "Owlet", "Thrush" }, CrowdActivity = "cheering", Wash = 0.4f,
            },
            // The inn at the road's end: a table and a chair by the lantern, travellers asleep over the same soup.
            ["Overgrown_Inn"] = new IslandLook
            {
                Region = "Verdance", Mid = "Mid_Gate", Far = "Far_Canopy", Tile = "Ground_Flag",
                Props = new[] { new IslandProp("Desk", 6.5f), new IslandProp("Lantern", -6f) },
                Speaker = Townsfolk.Prefix + "Nuthatch", Crowd = new[] { "Thrush", "Crane" }, CrowdActivity = "asleep", Wash = 0.55f,
            },
            // Lowmarket with no owners: the mill's wheel turning, the notice nobody reads.
            ["Lowmarket"] = new IslandLook
            {
                Region = "Halden", Mid = "Mid_Lowmarket", Far = "Far_Citadel", Tile = "Ground_Cobble",
                Props = new[] { new IslandProp("Wheel", -8f), new IslandProp("Notice", 8.5f) },
                Speaker = Townsfolk.Prefix + "Starling", Crowd = new[] { "Pigeon", "Sparrow", "Rook" }, CrowdActivity = "idle", Wash = 0.5f,
            },
        };

        /// <summary>A region's look for a released place no island is written for: its strips, floor and two props.</summary>
        sealed class RegionKit
        {
            public string Mid, Far, Tile; public string[] Props;
            public RegionKit(string mid, string far, string tile, params string[] props) { Mid = mid; Far = far; Tile = tile; Props = props; }
        }

        static readonly Dictionary<string, RegionKit> Kits = new Dictionary<string, RegionKit>
        {
            ["Saltmarrow"] = new RegionKit("Mid_Reeds", "Far_Roosts", "Ground_Boardwalk", "Stoop", "Nets"),
            ["Emberdown"] = new RegionKit("Mid_Roosts", "Far_Chimneys", "Ground_Basalt", "Porch", "Cups"),
            ["Verdance"] = new RegionKit("Mid_Trunks", "Far_Canopy", "Ground_Root", "Lantern", "Milestone"),
            ["Halden"] = new RegionKit("Mid_Mills", "Far_Citadel", "Ground_Cobble", "Wheel", "Notice"),
            ["Windreach"] = new RegionKit("Mid_Camp", "Far_Steppe", "Ground_Turf", "Wagon", "Stone"),
            ["Greyfold"] = new RegionKit("Mid_Road", "Far_White", "Ground_Cobbles", "Milepost", "Tent"),
            ["Blank"] = new RegionKit("Mid_Drift", "Far_Islands", "Ground_Grey", "Chair", "Well"),
        };

        /// <summary>A place's own strip where its name says which (the baths draw the springs): the room it was.</summary>
        static readonly (string word, string region, string mid)[] Places =
        {
            ("Baths", "Emberdown", "Mid_Springs"), ("Furnace", "Emberdown", "Mid_Furnaces"), ("Hollow", "Emberdown", "Mid_Gallery"),
            ("Library", "Verdance", "Mid_Shelves"), ("Grove", "Verdance", "Mid_Branches"), ("House", "Verdance", "Mid_Roots"),
            ("Gate", "Verdance", "Mid_Gate"), ("Aldermere", "Verdance", "Mid_Village"),
            ("Bridges", "Halden", "Mid_Bridges"), ("Mills", "Halden", "Mid_Mills"), ("Lowmarket", "Halden", "Mid_Lowmarket"),
            ("Hall", "Halden", "Mid_Hall"), ("Orchard", "Halden", "Mid_Orchard"), ("Tower", "Halden", "Mid_Tower"), ("Observatory", "Halden", "Mid_Dome"),
            ("Stones", "Windreach", "Mid_Stones"), ("Camp", "Windreach", "Mid_Camp"), ("River", "Windreach", "Mid_River"),
            ("CutBank", "Windreach", "Mid_Cliff"), ("WindGate", "Windreach", "Mid_WindGate"), ("HighGrass", "Windreach", "Mid_HighGrass"),
            ("Hearth", "Windreach", "Mid_Hearth"), ("Crater", "Windreach", "Mid_Crater"),
            ("Iris", "Saltmarrow", "Mid_Irises"), ("Bone", "Saltmarrow", "Mid_Bones"), ("Salt", "Saltmarrow", "Mid_Salt"),
        };

        /// <summary>The region a place id is in: the part before its first underscore ("Emberdown_Baths_2").</summary>
        public static string RegionOf(string place)
        {
            if (string.IsNullOrEmpty(place)) return "Blank";
            int u = place.IndexOf('_');
            var region = u > 0 ? place.Substring(0, u) : place;
            return Kits.ContainsKey(region) ? region : "Blank";
        }

        static int Hash(string s) { int h = 17; foreach (var c in s ?? "") h = unchecked(h * 31 + c); return h & 0x7fffffff; }

        /// <summary>The townsfolk looks a place's people wear: their region's, or any living region's for the Greyfold and the Blank.</summary>
        static List<TownsfolkLook> LooksFor(string region)
        {
            var looks = Townsfolk.Looks.Where(l => l.Region.ToString() == region).ToList();
            return looks.Count > 0 ? looks : Townsfolk.Looks.ToList();
        }

        /// <summary>
        /// How a drifting island is drawn. An authored island is the place it was; a generic island is its place's region,
        /// its own strip where its name says which, one of its townsfolk to speak and two more standing; a half-island is
        /// the Blank's own white with a doorframe and a chair, for people whose place is still standing somewhere else.
        /// </summary>
        public static IslandLook For(Islands.Drift drift)
        {
            if (drift == null) return null;
            if (drift.Island != null) return Authored.TryGetValue(drift.Island.Id, out var a) ? a : null;

            int seed = Hash(drift.Scene);
            var placeRegion = RegionOf(drift.PlaceId);
            var looks = LooksFor(placeRegion);
            var speaker = looks[seed % looks.Count];
            var crowd = new[] { looks[(seed / 7 + 1) % looks.Count].Id, looks[(seed / 49 + 2) % looks.Count].Id };
            if (drift.IsHalf)
            {
                return new IslandLook
                {
                    Region = "Blank", Mid = "Mid_Drift", Far = "Far_Islands", Tile = "Ground_Grey",
                    Props = new[] { new IslandProp("Doorframe", -4.5f), new IslandProp("Chair", 7f) },
                    Speaker = speaker.Character, Crowd = new[] { crowd[0] }, CrowdActivity = "idle", Wash = 0f,
                };
            }
            var kit = Kits[placeRegion];
            string mid = kit.Mid;
            foreach (var p in Places)
                if (p.region == placeRegion && drift.PlaceId.Contains(p.word)) { mid = p.mid; break; }
            return new IslandLook
            {
                Region = placeRegion, Mid = mid, Far = kit.Far, Tile = kit.Tile,
                Props = new[] { new IslandProp(kit.Props[seed % kit.Props.Length], -6f - seed % 3), new IslandProp(kit.Props[(seed + 1) % kit.Props.Length], 7.5f + (seed / 3) % 2) },
                Speaker = speaker.Character, Crowd = crowd, CrowdActivity = "idle", Wash = 0.65f,
            };
        }

        /// <summary>Every material any island can ask for, for the tests to find on disk.</summary>
        public static IEnumerable<string> Materials()
        {
            yield return BlankFar; yield return BlankFarther; yield return WhiteTile; yield return PeopleMaterial;
            foreach (var look in Authored.Values)
            {
                yield return look.MidMaterial; yield return look.FarMaterial; yield return look.TileMaterial;
                foreach (var p in look.Props) yield return look.PropMaterial(p);
            }
            foreach (var kv in Kits)
            {
                yield return "M_Paper_" + kv.Value.Mid; yield return "M_Paper_" + kv.Value.Far; yield return "M_" + kv.Value.Tile;
                foreach (var p in kv.Value.Props) yield return PropMaterial(kv.Key, p);
            }
            foreach (var p in Places) yield return "M_Paper_" + p.mid;
            yield return PropMaterial("Blank", "Doorframe"); yield return PropMaterial("Blank", "Chair");
        }

        /// <summary>Every character any island can draw: the authored speakers, every townsfolk look.</summary>
        public static IEnumerable<string> Characters()
        {
            foreach (var look in Authored.Values) yield return look.Speaker;
            foreach (var l in Townsfolk.Looks) yield return l.Character;
        }
    }
}
