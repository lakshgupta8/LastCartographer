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
    /// A hub's shop (DES-05): opens when a conversation asks for it (Shops.Requested, after the talk ends). Rows are
    /// the hub's stock with prices in iris seeds; ↑↓ picks, J buys, Esc leaves. Ferrymen prices are the
    /// economy's, so a burned iris field shows here.
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public string Hub { get; private set; }
        public int Row { get; private set; }
        public VisualElement Panel => _panel;
        public IReadOnlyList<StockItem> Items => _items;

        WrenController _wren;
        bool _wasFrozen, _built;
        string _pendingHub;
        VisualElement _panel, _rows;
        Label _title, _blurb, _seeds;
        readonly List<StockItem> _items = new List<StockItem>();

        void OnEnable() { Shops.Requested += OnRequested; }
        void OnDisable() { Shops.Requested -= OnRequested; }

        void OnRequested(string hub) { _pendingHub = hub; }

        public void Open(string hub)
        {
            Hub = hub;
            _items.Clear();
            _items.AddRange(Economy.StockAt(hub));
            _wren = FindFirstObjectByType<WrenController>();
            if (_wren != null) { _wasFrozen = _wren.Frozen; _wren.Frozen = true; }
            Row = 0;
            IsOpen = true;
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (_wren != null) _wren.Frozen = _wasFrozen;
            GameState.Save();
            if (_built) InkTheme.Show(_panel, false);
        }

        public void SetRow(int row) { Row = Mathf.Clamp(row, 0, Mathf.Max(0, _items.Count - 1)); Refresh(); }

        /// <summary>Buy the selected item. False when owned or unaffordable.</summary>
        public bool Confirm()
        {
            if (!IsOpen || Row >= _items.Count) return false;
            bool ok = Economy.Buy(GameState.World, _items[Row]);
            Refresh();
            return ok;
        }

        void Update()
        {
            if (!_built && Build() && IsOpen) Refresh();
            if (!IsOpen && _pendingHub != null && (DialogueService.Instance == null || !DialogueService.Instance.IsRunning))
            {
                var hub = _pendingHub;
                _pendingHub = null;
                Open(hub);
            }
            if (!IsOpen) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool up = (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && (g.dpad.up.wasPressedThisFrame || g.leftStick.up.wasPressedThisFrame));
            bool down = (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && (g.dpad.down.wasPressedThisFrame || g.leftStick.down.wasPressedThisFrame));
            bool confirm = (k != null && (k.jKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) || (g != null && g.buttonSouth.wasPressedThisFrame);
            bool close = (k != null && (k.escapeKey.wasPressedThisFrame || k.mKey.wasPressedThisFrame)) || (g != null && (g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
            int n = _items.Count;
            if (up && n > 0) { Row = (Row + n - 1) % n; Refresh(); }
            if (down && n > 0) { Row = (Row + 1) % n; Refresh(); }
            if (close) Close();
            else if (confirm) Confirm();
        }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.Desk == null) return false;
            _panel = InkTheme.Panel("shop");
            _panel.style.position = Position.Absolute;
            _panel.style.left = new Length(50, LengthUnit.Percent);
            _panel.style.top = new Length(50, LengthUnit.Percent);
            _panel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            _panel.style.width = 760;
            _panel.style.maxWidth = new Length(92, LengthUnit.Percent);
            _title = InkTheme.TitleText("title", Loc.T("shop.title.saltmarrow", "Sable's table"), 34, InkTheme.Wash);
            _title.style.marginBottom = 14;
            _rows = new VisualElement { name = "rows", pickingMode = PickingMode.Ignore };
            _blurb = InkTheme.Text("blurb", "", 16, InkTheme.Dim);
            _blurb.style.marginTop = 10;
            _seeds = InkTheme.Text("seeds", "", 16, InkTheme.Ochre);
            _seeds.style.marginTop = 12;
            var hint = InkTheme.Say("hint", "shop.hint", "↑↓ row    J buy    Esc leave", 15, InkTheme.Dim);
            hint.style.marginTop = 12;
            _panel.Add(_title); _panel.Add(_rows); _panel.Add(_blurb); _panel.Add(_seeds); _panel.Add(hint);
            InkTheme.Show(_panel, false);
            ui.Desk.Add(_panel);
            _built = true;
            return true;
        }

        public void Refresh()
        {
            if (!_built) return;
            InkTheme.Show(_panel, IsOpen);
            if (!IsOpen) return;
            var w = GameState.World;
            _title.text = Hub == "Saltmarrow" ? Loc.T("shop.title.saltmarrow", "Sable's table") : Loc.F("shop.title", "{0} · stock", Hub);
            _rows.Clear();
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var info = InstrumentInfo.Of(item.Kind);
                bool sel = i == Row;
                bool owned = Economy.Owns(w, item);
                bool can = Economy.CanBuy(w, item);
                var row = InkTheme.Row("row-" + i);
                row.AddToClassList("shop-row");
                row.EnableInClassList("selected", sel);
                row.EnableInClassList("owned", owned);
                InkTheme.SetPadding(row, 6f, 10f);
                InkTheme.SetRadius(row, 4f);
                row.style.backgroundColor = sel ? InkTheme.PaperDark : new Color(0f, 0f, 0f, 0f);
                var marker = InkTheme.Marker(sel);
                var name = InkTheme.Text("name", info.Name ?? item.Kind.ToString(), 22, owned ? InkTheme.Dim : can ? InkTheme.Ink : InkTheme.Dim);
                name.style.flexGrow = 1;
                var price = InkTheme.Text("price", owned ? Loc.T("shop.owned", "owned") : Loc.F("shop.price", "{0} ✿", Economy.PriceOf(w, item)), 20, owned ? InkTheme.Dim : can ? InkTheme.Ochre : InkTheme.Dim, FontStyle.Bold);
                price.style.width = 110;
                price.style.unityTextAlign = TextAnchor.MiddleRight;
                var icon = InkTheme.Icon("icon", InkArt.InstrumentIcon(item.Kind), 26f);
                row.Add(marker); if (icon != null) row.Add(icon); row.Add(name); row.Add(price);
                _rows.Add(row);
            }
            if (Row < _items.Count)
            {
                var item = _items[Row];
                _blurb.text = Economy.PitchOf(item) + "  " + (InstrumentInfo.Of(item.Kind).Blurb ?? "");
            }
            else _blurb.text = Loc.T("shop.empty", "Nothing on the table today.");
            _seeds.text = Economy.IrisBurned(w) ? Loc.F("shop.seeds_burned", "Iris seeds: {0}   (the fields burned; prices are up)", Economy.Seeds(w)) : Loc.F("shop.seeds", "Iris seeds: {0}", Economy.Seeds(w));
        }
    }
}
