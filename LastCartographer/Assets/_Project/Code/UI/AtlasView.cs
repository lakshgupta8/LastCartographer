using System.Collections.Generic;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// The atlas: the pause screen (GDD 5, DES-02). M or gamepad Select opens the spread. The left page is the
    /// map: every place by region, its vantages drawn (●), blank (○) or erased (✕), its fade stage and fate, and
    /// the desks and lamps on the page. Standing at a desk or a lit lamp, the map also lists where Wren can
    /// travel; ↑↓ picks and J goes. The right page hosts the journal (JournalView).
    /// </summary>
    public sealed class AtlasView : MonoBehaviour
    {
        [SerializeField] float _refreshSeconds = 0.25f;

        public static AtlasView Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public int Cursor { get; private set; }
        public VisualElement Panel => _panel;
        public IReadOnlyList<Waypoint> Destinations => _destinations;
        /// <summary>The map's page open (ENV-11, atlas-map.md): an index into <see cref="AtlasMap.Pages"/>.</summary>
        public int Page => _page;
        public AtlasPageMap Map => _map;
        public bool CanTravel => TravelPoint.Nearby != null && _destinations.Count > 0;

        WrenController _wren;
        JournalView _journal;
        bool _wasFrozen, _built;
        float _nextRefresh;
        VisualElement _panel, _places, _travel, _journalHost, _here;
        ScrollView _scroll;
        Label _title, _day, _pageName;
        AtlasPageMap _map;
        int _page;
        readonly List<Waypoint> _destinations = new List<Waypoint>();

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open(FindFirstObjectByType<WrenController>());
        }

        public void Open(WrenController wren)
        {
            _wren = wren;
            if (wren != null) { _wasFrozen = wren.Frozen; wren.Frozen = true; }
            IsOpen = true;
            Cursor = 0;
            // The book opens at the page she stands on, and has the room she stands in.
            string here = Room.Current != null ? Room.Current.RoomId : null;
            AtlasMap.Walk(GameState.World, here);
            int at = System.Array.IndexOf(AtlasMap.Pages, AtlasMap.PageOfRoom(here));
            if (at >= 0) _page = at;
            if (_journal != null) _journal.Open(null);
            Refresh();
            UiSounds.Open();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (_journal != null) _journal.Close();
            if (_wren != null) _wren.Frozen = _wasFrozen;
            if (_built) InkTheme.Show(_panel, false);
            UiSounds.Close();
        }

        public void Move(int dir)
        {
            int n = _destinations.Count;
            if (n == 0) return;
            Cursor = (Cursor + dir + n) % n;
            Refresh();
            if (n > 1) UiSounds.Move();
        }

        /// <summary>Turn the map's page: ← and → (or the bumpers), round the book.</summary>
        public void Turn(int dir)
        {
            int n = AtlasMap.Pages.Length;
            _page = (_page + dir + n) % n;
            Refresh();
            UiSounds.Move();
        }

        /// <summary>J on a destination: close the atlas and go. False when there is nowhere to go from here.</summary>
        public bool Confirm()
        {
            if (!IsOpen || !CanTravel || Cursor >= _destinations.Count) return UiSounds.Did(false);
            var to = _destinations[Cursor];
            if (!FastTravel.CanTravel(GameState.World, to)) return UiSounds.Did(false);
            UiSounds.Select();   // the journey itself sounds from the world when she arrives
            Close();
            StartCoroutine(FastTravel.Go(to));
            return true;
        }

        void Update()
        {
            if (!_built && Build() && IsOpen) Refresh();
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool toggle = (k != null && k.mKey.wasPressedThisFrame) || (g != null && g.selectButton.wasPressedThisFrame);
            bool close = (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame);
            if (IsOpen)
            {
                if (toggle || close) { Close(); return; }
                bool up = (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && (g.dpad.up.wasPressedThisFrame || g.leftStick.up.wasPressedThisFrame));
                bool down = (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && (g.dpad.down.wasPressedThisFrame || g.leftStick.down.wasPressedThisFrame));
                bool confirm = (k != null && (k.jKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) || (g != null && g.buttonSouth.wasPressedThisFrame);
                bool left = (k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame)) || (g != null && (g.dpad.left.wasPressedThisFrame || g.leftShoulder.wasPressedThisFrame));
                bool right = (k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)) || (g != null && (g.dpad.right.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame));
                if (up) Move(-1);
                if (down) Move(1);
                if (left) Turn(-1);
                if (right) Turn(1);
                if (confirm) Confirm();
                if (Time.unscaledTime >= _nextRefresh) { _nextRefresh = Time.unscaledTime + _refreshSeconds; Refresh(); }
                return;
            }
            if (!toggle || FastTravel.IsTravelling) return;
            if (DialogueService.Instance != null && DialogueService.Instance.IsRunning) return;
            var wren = FindFirstObjectByType<WrenController>();
            if (wren != null && wren.Frozen) return;   // a desk, ledger or cutscene owns the screen
            Open(wren);
        }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.Desk == null) return false;
            _panel = InkTheme.Spread("atlas");
            _panel.style.position = Position.Absolute;
            _panel.style.left = new Length(50, LengthUnit.Percent);
            _panel.style.top = new Length(50, LengthUnit.Percent);
            _panel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            _panel.style.width = 1500;
            _panel.style.maxWidth = new Length(96, LengthUnit.Percent);
            _panel.style.maxHeight = new Length(92, LengthUnit.Percent);
            _panel.style.flexDirection = FlexDirection.Row;

            var map = new VisualElement { name = "map", pickingMode = PickingMode.Ignore };
            map.AddToClassList(InkArt.SpreadLeftClass);
            map.style.width = new Length(50, LengthUnit.Percent);
            map.style.paddingRight = 60;
            map.style.borderRightWidth = InkArt.Drawn ? 0 : 1; map.style.borderRightColor = InkTheme.InkFaint;
            _title = InkTheme.Title("title", "atlas.title", "Atlas", 34, InkTheme.Wash);
            var titleRow = InkTheme.Row("title-row");
            titleRow.style.marginBottom = 2;
            var rose = InkTheme.Icon("rose", InkArt.Tex("UI_Rose"), 34f, 10f);
            if (rose != null) titleRow.Add(rose);
            titleRow.Add(_title);
            _day = InkTheme.Text("day", "", 16, InkTheme.Dim);
            _day.style.marginBottom = 8;
            // The map: the region's page drawn as far as she has drawn it, turned with ← →.
            _pageName = InkTheme.TitleText("page", "", 18, InkTheme.Ink);
            _pageName.style.flexShrink = 0;
            _map = new AtlasPageMap();
            // The places are more than a page holds: they scroll, and the page opens at where she stands.
            _scroll = new ScrollView(ScrollViewMode.Vertical) { name = "places-scroll" };
            _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _scroll.style.flexGrow = 1; _scroll.style.flexShrink = 1; _scroll.style.minHeight = 0;
            _places = new VisualElement { name = "places", pickingMode = PickingMode.Ignore };
            _scroll.Add(_places);
            _travel = new VisualElement { name = "travel", pickingMode = PickingMode.Ignore };
            _travel.style.marginTop = 16;
            _travel.style.flexShrink = 0;
            var hint = InkTheme.Say("hint", "atlas.hint", "←→ page    ↑↓ destination    J travel    M / Esc close", 15, InkTheme.Dim);
            hint.style.marginTop = 14;
            hint.style.flexShrink = 0;
            map.style.minHeight = 0;
            map.Add(titleRow); map.Add(_day); map.Add(_pageName); map.Add(_map); map.Add(_scroll); map.Add(_travel); map.Add(hint);

            _journalHost = new VisualElement { name = "journal-host", pickingMode = PickingMode.Ignore };
            _journalHost.style.width = new Length(50, LengthUnit.Percent);
            _journalHost.style.paddingLeft = 60;
            _panel.Add(map); _panel.Add(_journalHost);
            InkTheme.Show(_panel, false);
            ui.Desk.Add(_panel);

            _journal = GetComponent<JournalView>();
            if (_journal != null) _journal.AttachTo(_journalHost);
            _built = true;
            return true;
        }

        public void Refresh()
        {
            if (!_built) return;
            InkTheme.Show(_panel, IsOpen);
            if (!IsOpen) return;
            var w = GameState.World;
            _places.Clear();
            string region = null;
            string here = Room.Current != null ? Room.Current.RoomId : "";
            _day.text = Loc.F("atlas.day", "Day {0} · {1}", DayClock.Day(w), DayClock.Display(DayClock.PhaseIn(w, here)))
                        + (DayClock.IsLocked(w, here) ? Loc.T("atlas.held_hour", "  (held at this hour)") : "");
            _noted.Clear();
            _here = null;
            var page = AtlasMap.Pages[_page];
            _pageName.text = Atlas.RegionName(page) + "   \u25C2 " + (_page + 1) + " / " + AtlasMap.Pages.Length + " \u25B8";
            _map.Show(page, w, here);
            foreach (var place in Atlas.AllPlaces)
            {
                if (place.Region != region)
                {
                    region = place.Region;
                    var head = InkTheme.TitleText("region", Atlas.RegionName(region), 19, InkTheme.Dim);
                    head.style.marginTop = 8; head.style.marginBottom = 4; head.style.flexShrink = 0;
                    _places.Add(head);
                    // The page's heading note, once anything on it is drawn (NAR-17).
                    var note = RegionNote(w, region);
                    if (note.Length > 0) _places.Add(Margin("region-note", note, 0f));
                }
                var entry = PlaceEntry(w, place, place.Id == here);
                if (place.Id == here) _here = entry;
                _places.Add(entry);
            }
            if (_here != null && _scroll != null)
            {
                var target = _here;
                _scroll.schedule.Execute(() => { if (target.panel != null) _scroll.ScrollTo(target); });   // once it has a size
            }

            _travel.Clear();
            _destinations.Clear();
            var from = TravelPoint.Nearby;
            if (from == null)
            {
                _travel.Add(InkTheme.Text("travel-head", Loc.T("atlas.travel.from_nowhere", "Travel from a desk or a lit lamp."), 16, InkTheme.Dim, FontStyle.Italic));
                return;
            }
            _destinations.AddRange(Atlas.Destinations(w, from.WaypointId));
            if (Cursor >= _destinations.Count) Cursor = 0;
            if (_destinations.Count == 0)
            {
                _travel.Add(InkTheme.Text("travel-head", Loc.F("atlas.travel.none", "From {0}: nowhere else is drawn yet.", from.DisplayName), 16, InkTheme.Dim, FontStyle.Italic));
                return;
            }
            _travel.Add(InkTheme.Text("travel-head", Loc.F("atlas.travel.from", "From {0}:", from.DisplayName), 16, InkTheme.Dim, FontStyle.Bold));
            for (int i = 0; i < _destinations.Count; i++)
            {
                var d = _destinations[i];
                bool sel = i == Cursor;
                var row = InkTheme.Row("dest-" + d.Id);
                row.AddToClassList("atlas-dest");
                row.EnableInClassList("selected", sel);
                InkTheme.SetPadding(row, 4f, 10f);
                InkTheme.SetRadius(row, 4f);
                row.style.backgroundColor = sel ? InkTheme.PaperDark : new Color(0f, 0f, 0f, 0f);
                var marker = InkTheme.Marker(sel);
                var kindIcon = InkTheme.Icon("kind", InkArt.Tex(d.Kind == WaypointKind.Lamp ? "UI_Lamp" : "UI_Desk"), 22f, 6f);
                var name = InkTheme.Text("name", (kindIcon != null ? "" : d.Kind == WaypointKind.Lamp ? "☼ " : "▣ ") + Atlas.WaypointName(d.Id), 19, InkTheme.Ink);
                name.style.flexGrow = 1;
                var where = InkTheme.Text("where", Atlas.PlaceName(d.Place), 15, InkTheme.Dim);
                row.Add(marker); if (kindIcon != null) row.Add(kindIcon); row.Add(name); row.Add(where);
                _travel.Add(row);
            }
        }

        readonly HashSet<string> _noted = new HashSet<string>();

        /// <summary>
        /// The place's vantages, each with its mark: drawn, blank or erased as the pen's own marks (ENV-11), or as
        /// ● ○ ✕ before the name without them. Each item carries its state as a class.
        /// </summary>
        static VisualElement VantageLine(WorldState w, AtlasPlace place, bool drawn)
        {
            var color = drawn ? InkTheme.Ink : InkTheme.Dim;
            bool glyphs = InkArt.Tex("UI_VantageDrawn") != null;
            var row = InkTheme.Row("vantages");
            row.style.marginLeft = 12;
            row.style.flexWrap = Wrap.Wrap;
            int n = 0;
            foreach (var v in Atlas.VantagesOf(place.Id))
            {
                string state = w.IsErased(v.Id) ? "Erased" : w.IsSurveyed(v.Id) ? "Drawn" : "Blank";
                var item = InkTheme.Row("vantage-" + v.Id);
                item.AddToClassList("vantage");
                item.AddToClassList(state.ToLowerInvariant());
                item.style.marginRight = 16;
                var mark = glyphs ? InkTheme.Icon("mark", InkArt.Tex("UI_Vantage" + state), 16f, 5f) : null;
                if (mark != null) item.Add(mark);
                string prefix = mark != null ? "" : state == "Erased" ? "✕ " : state == "Drawn" ? "● " : "○ ";
                item.Add(InkTheme.Text("name", prefix + Atlas.VantageName(v.Id), 16, color));
                row.Add(item);
                n++;
            }
            if (n == 0) row.Add(InkTheme.Text("none", Loc.T("atlas.no_vantage", "no vantage here"), 16, color));
            return row;
        }

        /// <summary>The vantage line as text, mark then name, whichever way it is drawn: "● the Reedmother    ○ the Quay".</summary>
        public static string VantageText(VisualElement place)
        {
            var v = place != null ? place.Q("vantages") : null;
            if (v == null) return "";
            if (v is Label l) return l.text;
            var sb = new System.Text.StringBuilder();
            foreach (var item in v.Children())
            {
                if (sb.Length > 0) sb.Append("    ");
                if (item.ClassListContains("erased")) sb.Append("✕ ");
                else if (item.ClassListContains("drawn")) sb.Append("● ");
                else if (item.ClassListContains("blank")) sb.Append("○ ");
                var name = item is Label il ? il : item.Q<Label>("name");
                if (name != null) sb.Append(name.text.TrimStart('●', '○', '✕', ' '));
            }
            return sb.ToString();
        }

        /// <summary>A line in Wren's hand in the atlas's margin (NAR-17).</summary>
        static Label Margin(string name, string text, float indent)
        {
            var l = InkTheme.Text(name, text, 14, InkTheme.Dim, FontStyle.Italic);
            l.AddToClassList("atlas-margin");
            l.style.marginLeft = indent;
            l.style.marginBottom = 4;
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        /// <summary>The region's note, once a place on its page is drawn.</summary>
        static string RegionNote(WorldState w, string region)
        {
            foreach (var p in Atlas.AllPlaces)
            {
                if (p.Region != region || !Atlas.IsDrawn(w, p.Id)) continue;
                var zone = WorldGraph.Find(WorldGraph.ZoneOfPlace(p.Id) ?? "");
                return zone != null ? Flavour.ForRegion(zone.Region) : "";
            }
            return "";
        }

        /// <summary>A zone's note under the first drawn place in it: the margin fills in as the page is surveyed.</summary>
        string ZoneNote(WorldState w, AtlasPlace place)
        {
            if (!Atlas.IsDrawn(w, place.Id)) return "";
            var zone = WorldGraph.ZoneOfPlace(place.Id);
            if (string.IsNullOrEmpty(zone) || !_noted.Add(zone)) return "";
            return Flavour.ForZone(zone);
        }

        VisualElement PlaceEntry(WorldState w, AtlasPlace place, bool here)
        {
            bool drawn = Atlas.IsDrawn(w, place.Id);
            bool erased = Atlas.IsErased(w, place.Id);
            var box = new VisualElement { name = "place-" + place.Id, pickingMode = PickingMode.Ignore };
            box.AddToClassList("atlas-place");
            box.style.marginBottom = 8;
            box.style.flexShrink = 0;
            var head = InkTheme.Row("head");
            var name = InkTheme.Text("name", Atlas.PlaceName(place.Id), 21, drawn ? InkTheme.Ink : InkTheme.Dim, FontStyle.Bold);
            head.Add(name);
            if (here)
            {
                var mark = InkTheme.Text("here", Loc.T("atlas.here", "  · here"), 15, InkTheme.Ochre);
                head.Add(mark);
            }
            box.Add(head);

            box.Add(VantageLine(w, place, drawn));

            var status = new System.Text.StringBuilder();
            int stage = FadeStages.Get(w, place.Id);
            status.Append(erased ? Loc.T("atlas.erased", "erased. Draw it again.") : FadeStages.Display(stage));
            var fate = Places.FateOf(w, place.Id);
            if (fate != PlaceFate.Unwritten) status.Append(" · ").Append(Places.Display(fate));
            foreach (var wp in Atlas.WaypointsOf(place.Id))
            {
                if (!Atlas.IsKnown(w, wp.Id)) continue;
                status.Append(" · ").Append(wp.Kind == WaypointKind.Lamp ? "☼ " : "▣ ").Append(Atlas.WaypointName(wp.Id));
                if (!Atlas.CanTravelTo(w, wp)) status.Append(Loc.T("atlas.off_page", " (off the page)"));
            }
            var line = InkTheme.Text("status", status.ToString(), 15, erased ? InkTheme.Ochre : InkTheme.Dim);
            line.style.marginLeft = 12;
            box.Add(line);
            var note = ZoneNote(w, place);
            if (note.Length > 0) box.Add(Margin("zone-note", note, 12f));
            return box;
        }
    }
}
