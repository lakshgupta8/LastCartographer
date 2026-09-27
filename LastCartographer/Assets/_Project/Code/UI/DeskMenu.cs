using System.Collections.Generic;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// The drafting-desk page (PRG-11): opens when Wren rests. Row 0 is the Charter (left/right or
    /// 1-3), the rows below are the Instrument slots (left/right cycles what sits there; a tool held
    /// elsewhere swaps places). J / Space / Esc / East closes and saves.
    /// </summary>
    public sealed class DeskMenu : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public int Row { get; private set; }

        WrenController _wren;
        CharterSet _charters;
        InstrumentBelt _belt;
        bool _wasFrozen, _built;
        VisualElement _panel, _rows;
        Label _title, _blurb;

        public VisualElement Panel => _panel;

        void OnEnable() { DraftingDesk.Rested += OnRested; }
        void OnDisable() { DraftingDesk.Rested -= OnRested; }

        void OnRested(DraftingDesk desk)
        {
            var w = FindFirstObjectByType<WrenController>();
            if (w == null) return;
            Open(w);
        }

        public void Open(WrenController wren)
        {
            _wren = wren;
            _charters = wren.GetComponent<CharterSet>();
            _belt = wren.GetComponent<InstrumentBelt>();
            _wasFrozen = wren.Frozen;
            wren.Frozen = true;
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

        void Update()
        {
            if (!_built && Build() && IsOpen) Refresh();
            if (!IsOpen) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            int rows = 1 + GameState.World.Equipment.SlotCount;
            bool up = (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && (g.dpad.up.wasPressedThisFrame || g.leftStick.up.wasPressedThisFrame));
            bool down = (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && (g.dpad.down.wasPressedThisFrame || g.leftStick.down.wasPressedThisFrame));
            bool left = (k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame)) || (g != null && (g.dpad.left.wasPressedThisFrame || g.leftStick.left.wasPressedThisFrame));
            bool right = (k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)) || (g != null && (g.dpad.right.wasPressedThisFrame || g.leftStick.right.wasPressedThisFrame));
            bool close = (k != null && (k.jKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                         || (g != null && (g.buttonEast.wasPressedThisFrame || g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame));

            bool changed = false;
            if (up) { Row = (Row + rows - 1) % rows; changed = true; }
            if (down) { Row = (Row + 1) % rows; changed = true; }
            if (left) { Step(-1); changed = true; }
            if (right) { Step(1); changed = true; }
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame) { PickCharter(0); changed = true; }
                if (k.digit2Key.wasPressedThisFrame) { PickCharter(1); changed = true; }
                if (k.digit3Key.wasPressedThisFrame) { PickCharter(2); changed = true; }
            }
            if (close) Close();
            else if (changed) Refresh();
        }

        List<CharterKind> OwnedCharters()
        {
            var list = new List<CharterKind>();
            if (_charters == null) return list;
            foreach (var p in _charters.Profiles) if (GameState.World.Equipment.OwnsCharter(p.Kind)) list.Add(p.Kind);
            return list;
        }

        void PickCharter(int index)
        {
            var owned = OwnedCharters();
            if (index >= 0 && index < owned.Count) GameState.World.Equipment.SetCharter(owned[index]);
        }

        /// <summary>Move the current row's choice by one (wraps). Row 0 is the Charter; others are slots.</summary>
        public void Step(int dir)
        {
            var e = GameState.World.Equipment;
            if (Row == 0)
            {
                var owned = OwnedCharters();
                if (owned.Count == 0) return;
                int i = Mathf.Max(0, owned.IndexOf(e.Charter));
                e.SetCharter(owned[(i + dir + owned.Count) % owned.Count]);
                Refresh();
                return;
            }
            int slot = Row - 1;
            if (_belt == null || slot >= e.SlotCount) return;
            var choices = new List<InstrumentKind> { InstrumentKind.None };
            foreach (var info in InstrumentInfo.All) if (e.OwnsInstrument(info.Kind)) choices.Add(info.Kind);
            int cur = Mathf.Max(0, choices.IndexOf(e.Slots[slot].Kind));
            var next = choices[(cur + dir + choices.Count) % choices.Count];
            _belt.Equip(slot, next);
            Refresh();
        }

        public void SetRow(int row) { Row = row; Refresh(); }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.Desk == null) return false;
            _panel = InkTheme.Panel("desk");
            _panel.style.position = Position.Absolute;
            _panel.style.left = new Length(50, LengthUnit.Percent);
            _panel.style.top = new Length(50, LengthUnit.Percent);
            _panel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            _panel.style.width = 760;
            _panel.style.maxWidth = new Length(92, LengthUnit.Percent);
            _title = InkTheme.Text("title", "Drafting desk", 30, InkTheme.Wash, FontStyle.Bold);
            _title.style.marginBottom = 14;
            _rows = new VisualElement { name = "rows", pickingMode = PickingMode.Ignore };
            _blurb = InkTheme.Text("blurb", "", 16, InkTheme.Dim);
            _blurb.style.marginTop = 10;
            var hint = InkTheme.Text("hint", "↑↓ row    ◂▸ change    1-3 Charter    J / Esc leave", 15, InkTheme.Dim);
            hint.style.marginTop = 18;
            _panel.Add(_title); _panel.Add(_rows); _panel.Add(_blurb); _panel.Add(hint);
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
            var e = GameState.World.Equipment;
            _rows.Clear();
            var profile = _charters != null ? _charters.Current : null;
            string charterName = profile != null ? profile.DisplayName : e.Charter.ToString();
            _rows.Add(MakeRow(0, "Charter", charterName, ""));
            for (int i = 0; i < e.SlotCount; i++)
            {
                var s = e.Slots[i];
                string name = s.IsEmpty ? "(empty)" : InstrumentInfo.Of(s.Kind).Name;
                string uses = s.IsEmpty ? "" : (s.UsesLeft < 0 ? "∞" : s.UsesLeft + " / " + InstrumentInfo.Of(s.Kind).Uses);
                _rows.Add(MakeRow(i + 1, "Slot " + (i + 1), name, uses));
            }
            if (Row == 0) _blurb.text = profile != null ? profile.Blurb : "";
            else
            {
                var s = e.Slots[Row - 1];
                _blurb.text = s.IsEmpty ? "An empty loop on the belt." : InstrumentInfo.Of(s.Kind).Blurb;
            }
        }

        VisualElement MakeRow(int index, string label, string value, string extra)
        {
            bool sel = index == Row;
            var row = InkTheme.Row("row-" + index);
            row.AddToClassList("desk-row");
            row.EnableInClassList("selected", sel);
            InkTheme.SetPadding(row, 6f, 10f);
            InkTheme.SetRadius(row, 4f);
            row.style.backgroundColor = sel ? InkTheme.PaperDark : new Color(0f, 0f, 0f, 0f);
            var marker = InkTheme.Text("marker", sel ? "▸" : "", 22, InkTheme.Wash);
            marker.style.width = 26;
            var l = InkTheme.Text("label", label, 22, InkTheme.Dim);
            l.style.width = 130;
            var v = InkTheme.Text("value", "◂  " + value + "  ▸", 22, InkTheme.Ink);
            v.style.flexGrow = 1;
            var x = InkTheme.Text("extra", extra, 20, InkTheme.Wash, FontStyle.Bold);
            x.style.width = 90;
            x.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(marker); row.Add(l); row.Add(v); row.Add(x);
            return row;
        }
    }
}
