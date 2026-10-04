using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// One page of the atlas's map (docs/design/atlas-map.md), drawn with the pen: each room of the region a box on the
    /// page's grid (<see cref="AtlasMap"/>), in ink and wash where she has drawn it, in pencil where she has only
    /// walked it or a vantage saw it, rubbed out where it was erased, and blank where she has never been. Doors are
    /// strokes between boxes, the ways off the page are arrows with where they lead, a vantage is a ring (filled when
    /// drawn), and the nib marks where she stands. The strokes wander a little, by each room's name, as a hand's do.
    /// </summary>
    public sealed class AtlasPageMap : VisualElement
    {
        public const float Pad = 18f, MaxCell = 64f;

        WorldState _world;
        string _here;
        readonly List<Label> _labels = new List<Label>();
        readonly Dictionary<MapInk, int> _counts = new Dictionary<MapInk, int>();

        public string PageName { get; private set; }
        public string Here => _here;
        /// <summary>How many of the page's rooms are on it in each ink (tests, and the page's note).</summary>
        public int Count(MapInk ink) => _counts.TryGetValue(ink, out var n) ? n : 0;
        /// <summary>The marks on the page (desks, lamps, shops): in ink where she has used them, in pencil where she passed them.</summary>
        public int Marks(MapMarkKind kind, bool inked) => _marks.TryGetValue((kind, inked), out var n) ? n : 0;
        readonly Dictionary<(MapMarkKind, bool), int> _marks = new Dictionary<(MapMarkKind, bool), int>();
        public IReadOnlyList<Label> Labels => _labels;

        public AtlasPageMap()
        {
            name = "atlas-map";
            pickingMode = PickingMode.Ignore;
            style.flexShrink = 0;
            style.height = 290;
            style.marginBottom = 10;
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => PlaceLabels());
        }

        /// <summary>Show a page as the world has it, with her in <paramref name="here"/> (a room id, or null).</summary>
        public void Show(string page, WorldState world, string here)
        {
            PageName = page;
            _world = world;
            _here = here;
            _counts.Clear();
            _marks.Clear();
            foreach (var r in AtlasMap.Page(page))
            {
                var ink = AtlasMap.InkOf(world, r.Id);
                _counts[ink] = Count(ink) + 1;
                if (!Marked(ink)) continue;
                foreach (var (kind, id) in AtlasMap.MarksOf(r.Id))
                {
                    var key = (kind, AtlasMap.IsMarkInked(world, kind, id, r.Id));
                    _marks[key] = _marks.TryGetValue(key, out var n) ? n + 1 : 1;
                }
            }
            foreach (var l in _labels) l.RemoveFromHierarchy();
            _labels.Clear();
            foreach (var r in AtlasMap.Page(page))
            {
                if (AtlasMap.InkOf(world, r.Id) == MapInk.Unknown) continue;
                foreach (var (side, to) in r.Leaves)
                {
                    var label = InkTheme.Text("leave-" + r.Id + "-" + side, LeaveName(to), 12, InkTheme.Dim, FontStyle.Italic);
                    label.style.position = Position.Absolute;
                    label.pickingMode = PickingMode.Ignore;
                    label.userData = (r, side);
                    _labels.Add(label);
                    Add(label);
                }
            }
            PlaceLabels();
            MarkDirtyRepaint();
        }

        /// <summary>Where a way off the page leads, as the page writes it: the region it opens on.</summary>
        static string LeaveName(string to)
        {
            var zone = RoomPlans.ZoneOf(to) ?? to;
            int dot = zone.IndexOf('.');
            var region = dot > 0 ? zone.Substring(0, dot) : zone;
            return "to " + Atlas.RegionName(AtlasMap.PageOfRoom(region + "_x") ?? region);
        }

        // ---- the grid on the page -------------------------------------------------------------------------------------

        int _x0, _x1, _y0, _y1;
        float _cell, _ox, _oy;

        /// <summary>
        /// The page's frame: what she has drawn, and a cell round it for the stubs and the arrows, so the page grows
        /// as she draws instead of a few boxes lost in a corner of the region's blank.
        /// </summary>
        bool Measure()
        {
            var rooms = AtlasMap.Page(PageName);
            var rect = contentRect;
            if (rooms.Count == 0 || rect.width <= 0f || rect.height <= 0f) return false;
            _x0 = _y0 = int.MaxValue; _x1 = _y1 = int.MinValue;
            foreach (var r in rooms)
            {
                if (_world != null && AtlasMap.InkOf(_world, r.Id) == MapInk.Unknown) continue;
                _x0 = Mathf.Min(_x0, r.X - 1); _x1 = Mathf.Max(_x1, r.X + 1); _y0 = Mathf.Min(_y0, r.Y - 1); _y1 = Mathf.Max(_y1, r.Y + 1);
            }
            if (_x0 > _x1) return false;   // a blank page: nothing to frame
            int cols = _x1 - _x0 + 1, rows = _y1 - _y0 + 1;
            _cell = Mathf.Min(MaxCell, (rect.width - 2 * Pad) / cols, (rect.height - 2 * Pad) / rows);
            _ox = rect.x + (rect.width - cols * _cell) * 0.5f;
            _oy = rect.y + (rect.height - rows * _cell) * 0.5f;
            return true;
        }

        /// <summary>A cell's centre on the page: x east, y up the page.</summary>
        Vector2 Centre(int x, int y) => new Vector2(_ox + (x - _x0 + 0.5f) * _cell, _oy + (_y1 - y + 0.5f) * _cell);
        Vector2 Half => new Vector2(_cell * 0.39f, _cell * 0.28f);

        /// <summary>A room carries its marks once she has been in it (a vantage's glimpse shows the box, not what is in it).</summary>
        static bool Marked(MapInk ink) => ink == MapInk.Walked || ink == MapInk.Drawn;

        static Vector2 Out(Side s) => s == Side.West ? Vector2.left : s == Side.East ? Vector2.right : s == Side.Up ? Vector2.down : Vector2.up;

        void PlaceLabels()
        {
            if (!Measure()) return;
            foreach (var l in _labels)
            {
                var (room, side) = ((MapRoom, Side))l.userData;
                var tip = Centre(room.X, room.Y) + Vector2.Scale(Out(side), Half) + Out(side) * (_cell * 0.32f);
                l.style.left = side == Side.West ? tip.x - 70f : tip.x + 2f;
                l.style.top = tip.y - 8f;
                l.style.width = 68f;
                l.style.unityTextAlign = side == Side.West ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            }
        }

        // ---- the pen ---------------------------------------------------------------------------------------------------

        /// <summary>A hand's wander for a room's corner, ±1 px, the same every time the page is drawn.</summary>
        static float Wobble(string id, int k)
        {
            int h = 17 + k * 7919;
            foreach (var ch in id) h = unchecked(h * 31 + ch);
            return ((h & 1023) / 1023f - 0.5f) * 2f;
        }

        void Draw(MeshGenerationContext ctx)
        {
            if (_world == null || !Measure()) return;
            var p = ctx.painter2D;
            p.lineJoin = LineJoin.Round;
            p.lineCap = LineCap.Round;
            var rooms = AtlasMap.Page(PageName);
            var half = Half;

            // The doors first, under the rooms: a stroke from box to box where both are on the page.
            foreach (var r in rooms)
            {
                var ink = AtlasMap.InkOf(_world, r.Id);
                if (ink == MapInk.Unknown) continue;
                foreach (var (side, to) in r.Doors)
                {
                    var other = AtlasMap.Find(to);
                    if (other == null || string.CompareOrdinal(r.Id, to) > 0 && AtlasMap.InkOf(_world, to) != MapInk.Unknown) continue;   // once a door
                    var otherInk = AtlasMap.InkOf(_world, to);
                    bool inked = ink == MapInk.Drawn && otherInk == MapInk.Drawn;
                    var a = Centre(r.X, r.Y) + Vector2.Scale(Out(side), half);
                    var b = otherInk == MapInk.Unknown ? a + Out(side) * (_cell * 0.18f) : Centre(other.X, other.Y) - Vector2.Scale(Out(side), half);
                    p.strokeColor = inked ? InkTheme.Ink : InkTheme.Dim;
                    p.lineWidth = inked ? 2.2f : 1.2f;
                    p.BeginPath(); p.MoveTo(a);
                    if (Mathf.Abs(a.x - b.x) > 0.5f && Mathf.Abs(a.y - b.y) > 0.5f) p.LineTo(new Vector2(b.x, a.y));   // round a corner when the boxes are not in line
                    p.LineTo(b); p.Stroke();
                }
            }

            foreach (var r in rooms)
            {
                var ink = AtlasMap.InkOf(_world, r.Id);
                if (ink == MapInk.Unknown) continue;
                var c = Centre(r.X, r.Y);
                var corners = new[]
                {
                    c + new Vector2(-half.x + Wobble(r.Id, 0), -half.y + Wobble(r.Id, 1)),
                    c + new Vector2(half.x + Wobble(r.Id, 2), -half.y + Wobble(r.Id, 3)),
                    c + new Vector2(half.x + Wobble(r.Id, 4), half.y + Wobble(r.Id, 5)),
                    c + new Vector2(-half.x + Wobble(r.Id, 6), half.y + Wobble(r.Id, 7)),
                };
                if (ink == MapInk.Drawn)
                {
                    var wash = InkTheme.Wash; wash.a = 0.22f;
                    p.fillColor = wash;
                    Box(p, corners); p.Fill();
                    p.strokeColor = InkTheme.Ink; p.lineWidth = 2.2f;
                    Box(p, corners); p.Stroke();
                }
                else if (ink == MapInk.Walked)
                {
                    p.strokeColor = InkTheme.Dim; p.lineWidth = 1.2f;
                    Box(p, corners); p.Stroke();
                }
                else if (ink == MapInk.Seen)
                {
                    p.strokeColor = InkTheme.Dim; p.lineWidth = 1f;
                    for (int i = 0; i < 4; i++) Dashes(p, corners[i], corners[(i + 1) % 4], 4f, 4f);
                }
                else if (ink == MapInk.Erased)
                {
                    p.strokeColor = InkTheme.InkFaint; p.lineWidth = 1.2f;
                    Box(p, corners); p.Stroke();
                    // rubbed out: the eraser's strokes across it
                    p.lineWidth = 3f;
                    for (int k = -1; k <= 1; k++)
                    {
                        p.BeginPath();
                        p.MoveTo(c + new Vector2(-half.x * 0.8f + k * half.x * 0.5f, half.y * 0.7f));
                        p.LineTo(c + new Vector2(half.x * 0.3f + k * half.x * 0.5f, -half.y * 0.7f));
                        p.Stroke();
                    }
                }

                // Its vantages, rings along the top edge: filled once drawn.
                if (ink == MapInk.Drawn || ink == MapInk.Walked)
                {
                    int i = 0;
                    foreach (var v in Atlas.VantagesOf(r.Id))
                    {
                        var at = c + new Vector2(-half.x + 6f + i * 9f, -half.y + 6f);
                        bool drawn = _world.SurveyedVantages.Contains(v.Id) && !_world.IsErased(v.Id);
                        p.BeginPath(); p.Arc(at, Mathf.Max(2.5f, _cell * 0.05f), Angle.Degrees(0f), Angle.Degrees(360f));
                        if (drawn) { p.fillColor = InkTheme.Ink; p.Fill(); }
                        else { p.strokeColor = InkTheme.Dim; p.lineWidth = 1f; p.Stroke(); }
                        i++;
                    }
                }

                // Its marks, up the right edge from the bottom corner (clear of the nib at the middle and the rings
                // along the top): a desk, a lamp, a shop; in ink once used.
                if (Marked(ink))
                {
                    var marks = AtlasMap.MarksOf(r.Id);
                    float s = Mathf.Clamp(_cell * 0.075f, 3f, 5f);
                    for (int k = 0; k < marks.Count; k++)
                    {
                        var (kind, id) = marks[k];
                        var at = c + new Vector2(half.x - 4f - s, half.y - 3f - s - k * (2.4f * s));
                        Mark(p, kind, at, s, AtlasMap.IsMarkInked(_world, kind, id, r.Id));
                    }
                }

                // The ways off the page: an arrow out of the box's side.
                foreach (var (side, _) in r.Leaves)
                {
                    var from = c + Vector2.Scale(Out(side), half);
                    var tip = from + Out(side) * (_cell * 0.3f);
                    var across = new Vector2(-Out(side).y, Out(side).x) * (_cell * 0.07f);
                    p.strokeColor = InkTheme.Dim; p.lineWidth = 1.2f;
                    p.BeginPath(); p.MoveTo(from); p.LineTo(tip); p.Stroke();
                    p.BeginPath(); p.MoveTo(tip - Out(side) * (_cell * 0.08f) + across); p.LineTo(tip); p.LineTo(tip - Out(side) * (_cell * 0.08f) - across); p.Stroke();
                }
            }

            // The nib where she stands: a filled point under her room, pointing at it.
            var here = AtlasMap.Find(_here);
            if (here != null && here.Page == PageName)
            {
                var c = Centre(here.X, here.Y);
                float s = Mathf.Max(5f, _cell * 0.12f);
                var tip = c + new Vector2(0f, half.y * 0.25f);
                p.fillColor = InkTheme.Ink;
                p.BeginPath(); p.MoveTo(tip); p.LineTo(tip + new Vector2(-s * 0.6f, s * 1.6f)); p.LineTo(tip + new Vector2(s * 0.6f, s * 1.6f)); p.ClosePath(); p.Fill();
                p.strokeColor = InkTheme.Paper; p.lineWidth = 1f;
                p.BeginPath(); p.MoveTo(tip + new Vector2(0f, s * 0.5f)); p.LineTo(tip + new Vector2(0f, s * 1.4f)); p.Stroke();   // the nib's slit
            }
        }

        /// <summary>
        /// One mark, about 2s across, centred on <paramref name="at"/>: a drafting desk (its slanted board on two legs), a
        /// lamp (a post with the flame on it, lit in ochre), a shop (an iris seed, the coast's coin). Ink when used, pencil otherwise.
        /// </summary>
        static void Mark(Painter2D p, MapMarkKind kind, Vector2 at, float s, bool inked)
        {
            p.strokeColor = inked ? InkTheme.Ink : InkTheme.Dim;
            p.lineWidth = inked ? 1.5f : 1f;
            switch (kind)
            {
                case MapMarkKind.Desk:
                    p.BeginPath(); p.MoveTo(at + new Vector2(-s, -s * 0.1f)); p.LineTo(at + new Vector2(s, -s * 0.7f)); p.Stroke();
                    p.BeginPath(); p.MoveTo(at + new Vector2(-s * 0.6f, -s * 0.2f)); p.LineTo(at + new Vector2(-s * 0.6f, s)); p.Stroke();
                    p.BeginPath(); p.MoveTo(at + new Vector2(s * 0.6f, -s * 0.55f)); p.LineTo(at + new Vector2(s * 0.6f, s)); p.Stroke();
                    break;
                case MapMarkKind.Lamp:
                    p.BeginPath(); p.MoveTo(at + new Vector2(0f, s)); p.LineTo(at + new Vector2(0f, -s * 0.3f)); p.Stroke();
                    p.BeginPath(); p.MoveTo(at + new Vector2(-s * 0.45f, s)); p.LineTo(at + new Vector2(s * 0.45f, s)); p.Stroke();
                    p.BeginPath(); p.Arc(at + new Vector2(0f, -s * 0.65f), s * 0.38f, Angle.Degrees(0f), Angle.Degrees(360f));
                    if (inked) { p.fillColor = InkTheme.Ochre; p.Fill(); }
                    p.Stroke();
                    break;
                default:
                    var top = at + new Vector2(s * 0.3f, -s);
                    var bottom = at + new Vector2(-s * 0.3f, s);
                    p.BeginPath(); p.MoveTo(top);
                    p.BezierCurveTo(top + new Vector2(s * 0.9f, s * 0.6f), bottom + new Vector2(s * 0.5f, -s * 0.2f), bottom);
                    p.BezierCurveTo(bottom + new Vector2(-s * 0.9f, -s * 0.6f), top + new Vector2(-s * 0.5f, s * 0.2f), top);
                    p.ClosePath();
                    if (inked) { p.fillColor = InkTheme.Ink; p.Fill(); }
                    p.Stroke();
                    break;
            }
        }

        static void Box(Painter2D p, Vector2[] c)
        {
            p.BeginPath(); p.MoveTo(c[0]); p.LineTo(c[1]); p.LineTo(c[2]); p.LineTo(c[3]); p.ClosePath();
        }

        static void Dashes(Painter2D p, Vector2 a, Vector2 b, float on, float off)
        {
            float len = Vector2.Distance(a, b);
            var dir = (b - a) / Mathf.Max(0.001f, len);
            for (float t = 0f; t < len; t += on + off)
            {
                p.BeginPath(); p.MoveTo(a + dir * t); p.LineTo(a + dir * Mathf.Min(len, t + on)); p.Stroke();
            }
        }
    }
}
