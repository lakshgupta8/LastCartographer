using System.Collections.Generic;
using System.Linq;

namespace OWSBG.Core
{
    /// <summary>One room on an atlas page: its cell on the page's grid, its doors, what is in it.</summary>
    public sealed class MapRoom
    {
        public string Id;
        public string Page;
        public int X, Y;
        /// <summary>Doors to rooms on the same page, by side.</summary>
        public readonly List<(Side side, string to)> Doors = new List<(Side, string)>();
        /// <summary>Ways off the page: the side, and the zone or room it leads to.</summary>
        public readonly List<(Side side, string to)> Leaves = new List<(Side, string)>();
    }

    /// <summary>How a room is on the page: not at all, in pencil (seen from a vantage, or walked but not surveyed), in ink, or erased.</summary>
    public enum MapInk { Unknown, Seen, Walked, Drawn, Erased }

    /// <summary>What a room's box carries besides its vantages: a drafting desk, a lamp, a shop.</summary>
    public enum MapMarkKind { Desk, Lamp, Shop }

    /// <summary>
    /// The atlas's map (ENV-11, docs/design/atlas-map.md): each region's rooms laid out on a page's grid by walking their
    /// exits from its first room, a step west, east, up or down a cell, so the page is the shape she walks. The planned
    /// regions come from <see cref="RoomPlans"/>; the coast, built before the plans, is listed here. Pure data: the pen
    /// that draws it is the UI's (AtlasMapView).
    /// </summary>
    public static class AtlasMap
    {
        /// <summary>The pages in the book's order, as the atlas names its regions.</summary>
        public static readonly string[] Pages = { "The Saltmarrow", "Emberdown", "The Verdance", "Halden", "Windreach", "The Greyfold", "The Blank" };

        /// <summary>The coast's rooms and their ways (the greybox's hand-built scenes and its recipes), first room first.</summary>
        static readonly (string from, Side side, string to)[] Coast =
        {
            ("Saltmarrow_Shore", Side.East, "Saltmarrow_A"), ("Saltmarrow_A", Side.East, "Saltmarrow_Stilts"),
            ("Saltmarrow_Stilts", Side.East, "Saltmarrow_Boardwalk"), ("Saltmarrow_Boardwalk", Side.East, "Saltmarrow_B"),
            ("Saltmarrow_B", Side.East, "Saltmarrow_Tetherline"), ("Saltmarrow_Tetherline", Side.East, "Saltmarrow_Ferry"),
            ("Saltmarrow_Ferry", Side.East, "Saltmarrow_Chain_1"), ("Saltmarrow_Chain_1", Side.East, "Saltmarrow_Chain_2"),
            ("Saltmarrow_Chain_2", Side.East, "Saltmarrow_Chain_3"), ("Saltmarrow_Chain_3", Side.East, "Saltmarrow_Lighthouse"),
            ("Saltmarrow_Lighthouse", Side.East, "Saltmarrow_Chapel"), ("Saltmarrow_Chapel", Side.East, "Saltmarrow_BoneBridge"),
            ("Saltmarrow_Stilts", Side.Up, "Saltmarrow_Roots_1"), ("Saltmarrow_Roots_1", Side.East, "Saltmarrow_Roots_2"),
            ("Saltmarrow_Roots_2", Side.Up, "Saltmarrow_Roots_3"), ("Saltmarrow_Roots_3", Side.East, "Saltmarrow_Roots_4"),
            ("Saltmarrow_Roots_4", Side.East, "Saltmarrow_IrisFields"),
        };
        /// <summary>The coast's ways off its page.</summary>
        static readonly (string from, Side side, string to)[] CoastLeaves =
        {
            ("Saltmarrow_BoneBridge", Side.East, "Emberdown.FurnaceStair"), ("Saltmarrow_IrisFields", Side.East, "Verdance.Road"),
        };

        static Dictionary<string, List<MapRoom>> _pages;
        static Dictionary<string, MapRoom> _rooms;

        public static Side Opposite(Side s) => s == Side.West ? Side.East : s == Side.East ? Side.West : s == Side.Up ? Side.Down : Side.Up;
        static (int dx, int dy) Step(Side s) => s == Side.West ? (-1, 0) : s == Side.East ? (1, 0) : s == Side.Up ? (0, 1) : (0, -1);

        /// <summary>The page a room is drawn on: its region as the atlas names it.</summary>
        public static string PageOfRoom(string room)
        {
            if (string.IsNullOrEmpty(room)) return null;
            if (room.StartsWith("Saltmarrow_")) return Pages[0];
            var region = IslandLooks.RegionOf(room);
            if (!room.StartsWith(region + "_")) return null;
            return region == "Verdance" ? "The Verdance" : region == "Greyfold" ? "The Greyfold" : region == "Blank" ? "The Blank" : region;
        }

        /// <summary>The rooms on a page, laid out.</summary>
        public static IReadOnlyList<MapRoom> Page(string page) { Ensure(); return _pages.TryGetValue(page ?? "", out var l) ? l : new List<MapRoom>(); }
        public static MapRoom Find(string room) { Ensure(); return _rooms.TryGetValue(room ?? "", out var r) ? r : null; }

        static void Ensure()
        {
            if (_pages != null) return;
            _pages = new Dictionary<string, List<MapRoom>>();
            _rooms = new Dictionary<string, MapRoom>();
            // Every room's ways, both directions: the plans list each way once from each side; the coast once.
            var ways = new Dictionary<string, List<(Side side, string to)>>();
            var order = new List<string>();
            void Add(string from, Side side, string to)
            {
                if (!ways.TryGetValue(from, out var l)) { ways[from] = l = new List<(Side, string)>(); order.Add(from); }
                if (!l.Contains((side, to))) l.Add((side, to));
            }
            foreach (var (from, side, to) in Coast) { Add(from, side, to); Add(to, Opposite(side), from); }
            foreach (var (from, side, to) in CoastLeaves) Add(from, side, to);
            foreach (var plan in RoomPlans.All)
            {
                if (!ways.ContainsKey(plan.Id)) { ways[plan.Id] = new List<(Side, string)>(); order.Add(plan.Id); }
                foreach (var e in plan.Exits)
                {
                    Add(plan.Id, e.Side, e.To);
                    if (!e.IsExternal && RoomPlans.Find(e.To) != null) Add(e.To, Opposite(e.Side), plan.Id);
                }
            }

            foreach (var page in Pages)
            {
                var rooms = order.Where(r => PageOfRoom(r) == page).ToList();
                var laid = new List<MapRoom>();
                var taken = new HashSet<(int, int)>();
                int nextX = 0;
                foreach (var start in rooms)
                {
                    if (_rooms.ContainsKey(start)) continue;
                    // A room nothing on the page leads to starts its own piece, to the right of what is laid.
                    var queue = new Queue<MapRoom>();
                    var first = Place(start, page, nextX, 0, taken, Side.East);
                    laid.Add(first); queue.Enqueue(first);
                    while (queue.Count > 0)
                    {
                        var r = queue.Dequeue();
                        foreach (var (side, to) in ways[r.Id])
                        {
                            if (PageOfRoom(to) != page || !ways.ContainsKey(to))
                            {
                                if (!r.Leaves.Contains((side, to))) r.Leaves.Add((side, to));
                                continue;
                            }
                            if (!r.Doors.Contains((side, to))) r.Doors.Add((side, to));
                            if (_rooms.ContainsKey(to)) continue;
                            var (dx, dy) = Step(side);
                            var next = Place(to, page, r.X + dx, r.Y + dy, taken, side);
                            laid.Add(next); queue.Enqueue(next);
                        }
                    }
                    nextX = laid.Max(m => m.X) + 2;
                }
                _pages[page] = laid;
            }
        }

        /// <summary>A room at a cell, or the nearest free one further along the way it was reached by.</summary>
        static MapRoom Place(string id, string page, int x, int y, HashSet<(int, int)> taken, Side along)
        {
            var (dx, dy) = Step(along);
            if (dx == 0 && dy == 0) dx = 1;
            while (taken.Contains((x, y))) { x += dx; y += dy; }
            taken.Add((x, y));
            var room = new MapRoom { Id = id, Page = page, X = x, Y = y };
            _rooms[id] = room;
            return room;
        }

        // ---- the marks ------------------------------------------------------------------------------------------

        /// <summary>Who sells where: a hub's stock (<see cref="Economy.StockAt"/>) and the room its seller stands in.</summary>
        public static readonly (string room, string hub)[] Shops = { ("Saltmarrow_A", "Saltmarrow") };   // Sable's, on the quay

        /// <summary>
        /// A room's marks, in the order they are drawn: its desks and lamps (the atlas's waypoints, by the id she
        /// discovers them by) and the shop whose seller stands there (by its hub).
        /// </summary>
        public static List<(MapMarkKind kind, string id)> MarksOf(string room)
        {
            var marks = new List<(MapMarkKind, string)>();
            if (string.IsNullOrEmpty(room)) return marks;
            string scene = "Greybox_" + room;
            foreach (var wp in Atlas.AllWaypoints)
                if (wp.Room == scene && wp.Kind == WaypointKind.Desk) marks.Add((MapMarkKind.Desk, wp.Id));
            foreach (var wp in Atlas.AllWaypoints)
                if (wp.Room == scene && wp.Kind == WaypointKind.Lamp) marks.Add((MapMarkKind.Lamp, wp.Id));
            foreach (var (r, hub) in Shops)
                if (r == room) marks.Add((MapMarkKind.Shop, hub));
            return marks;
        }

        /// <summary>
        /// Whether a mark is in ink: a desk she has stood at, a lamp she has lit (both known waypoints), a shop in a
        /// room she has walked. Otherwise it is in pencil, seen as she passed.
        /// </summary>
        public static bool IsMarkInked(WorldState w, MapMarkKind kind, string id, string room) =>
            w != null && (kind == MapMarkKind.Shop ? IsWalked(w, room) : Atlas.IsKnown(w, id));

        // ---- the ink ------------------------------------------------------------------------------------------------

        /// <summary>The flag a room's first step writes: the pen has the road she walked.</summary>
        public static string WalkedKey(string room) => Keys.Of("room.", room, ".walked");

        /// <summary>She stood in a room (a scene "Greybox_X" is room X): it is walked from now on.</summary>
        public static void Walk(WorldState w, string scene)
        {
            if (w == null || string.IsNullOrEmpty(scene)) return;
            var room = scene.StartsWith("Greybox_") ? scene.Substring("Greybox_".Length) : scene;
            if (Find(room) != null) w.Set(WalkedKey(room), 1);
        }

        public static bool IsWalked(WorldState w, string room) => w != null && w.Get(WalkedKey(room)) > 0;

        /// <summary>
        /// How a room is on its page. A place erased is erased. A place with a view is in ink once surveyed and in
        /// pencil while only walked; a room with nothing to survey is in ink once walked (the pen has the road). A room
        /// next to one drawn is in pencil though she never walked it: the vantage saw it.
        /// </summary>
        public static MapInk InkOf(WorldState w, string room)
        {
            if (w == null || Find(room) == null) return MapInk.Unknown;
            if (Atlas.FindPlace(room) != null && Atlas.IsErased(w, room)) return MapInk.Erased;
            bool view = Atlas.VantagesOf(room).Count > 0;
            if (view && Atlas.IsDrawn(w, room)) return MapInk.Drawn;
            if (IsWalked(w, room)) return view ? MapInk.Walked : MapInk.Drawn;
            foreach (var (_, to) in Find(room).Doors)
                if (Atlas.VantagesOf(to).Count > 0 && Atlas.IsDrawn(w, to)) return MapInk.Seen;
            return MapInk.Unknown;
        }
    }
}
