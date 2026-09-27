using System.Collections.Generic;
using System.Text;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// The ledger page (PRG-12): opens when Wren reads a hub's board. One row per posted commission with its
    /// state; the blurb below shows the brief, the journal with step progress, or the aftermath. J / Space takes a
    /// posted one or turns in a fulfilled one. Esc / East leaves.
    /// </summary>
    public sealed class LedgerView : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public int Row { get; private set; }
        public VisualElement Panel => _panel;
        public IReadOnlyList<CommissionDef> Entries => _entries;

        CommissionLedger _ledger;
        WrenController _wren;
        bool _wasFrozen, _built;
        VisualElement _panel, _rows;
        Label _title, _blurb, _steps, _reward;
        readonly List<CommissionDef> _entries = new List<CommissionDef>();

        void OnEnable() { CommissionLedger.Opened += OnOpened; Commissions.Changed += OnChanged; }
        void OnDisable() { CommissionLedger.Opened -= OnOpened; Commissions.Changed -= OnChanged; }

        void OnOpened(CommissionLedger ledger) => Open(ledger, FindFirstObjectByType<WrenController>());

        void OnChanged(string id, CommissionState state) { if (IsOpen) Refresh(); }

        public void Open(CommissionLedger ledger, WrenController wren)
        {
            _ledger = ledger;
            _wren = wren;
            if (wren != null) { _wasFrozen = wren.Frozen; wren.Frozen = true; }
            Row = 0;
            IsOpen = true;
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (_wren != null) _wren.Frozen = _wasFrozen;
            if (_built) InkTheme.Show(_panel, false);
        }

        /// <summary>Take a posted row or turn in a fulfilled one. False when the row cannot move.</summary>
        public bool Confirm()
        {
            if (Row < 0 || Row >= _entries.Count) return false;
            var def = _entries[Row];
            var w = GameState.World;
            bool ok;
            switch (Commissions.StateOf(w, def.Id))
            {
                case CommissionState.Posted: ok = Commissions.Take(w, def.Id); break;
                case CommissionState.Fulfilled: ok = Commissions.Close(w, def.Id); break;
                default: ok = false; break;
            }
            Refresh();
            return ok;
        }

        public void SetRow(int row)
        {
            Row = _entries.Count == 0 ? 0 : Mathf.Clamp(row, 0, _entries.Count - 1);
            Refresh();
        }

        void Update()
        {
            if (!_built && Build() && IsOpen) Refresh();
            if (!IsOpen) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool up = (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && (g.dpad.up.wasPressedThisFrame || g.leftStick.up.wasPressedThisFrame));
            bool down = (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && (g.dpad.down.wasPressedThisFrame || g.leftStick.down.wasPressedThisFrame));
            bool confirm = (k != null && (k.jKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                           || (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame));
            bool close = (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && (g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame));

            int n = Mathf.Max(1, _entries.Count);
            if (up) SetRow((Row + n - 1) % n);
            if (down) SetRow((Row + 1) % n);
            if (confirm) Confirm();
            if (close) Close();
        }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.Desk == null) return false;
            _panel = InkTheme.Panel("ledger");
            _panel.style.position = Position.Absolute;
            _panel.style.left = new Length(50, LengthUnit.Percent);
            _panel.style.top = new Length(50, LengthUnit.Percent);
            _panel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            _panel.style.width = 860;
            _panel.style.maxWidth = new Length(92, LengthUnit.Percent);
            _title = InkTheme.Text("title", "Commissions", 30, InkTheme.Wash, FontStyle.Bold);
            _title.style.marginBottom = 14;
            _rows = new VisualElement { name = "rows", pickingMode = PickingMode.Ignore };
            _blurb = InkTheme.Text("blurb", "", 17, InkTheme.Ink);
            _blurb.style.marginTop = 12;
            _steps = InkTheme.Text("steps", "", 16, InkTheme.Dim);
            _steps.style.marginTop = 6;
            _reward = InkTheme.Text("reward", "", 15, InkTheme.Wash);
            _reward.style.marginTop = 8;
            var hint = InkTheme.Text("hint", "↑↓ choose    J take / turn in    Esc leave", 15, InkTheme.Dim);
            hint.style.marginTop = 18;
            _panel.Add(_title); _panel.Add(_rows); _panel.Add(_blurb); _panel.Add(_steps); _panel.Add(_reward); _panel.Add(hint);
            InkTheme.Show(_panel, false);
            ui.Desk.Add(_panel);
            _built = true;
            return true;
        }

        public void Refresh()
        {
            _entries.Clear();
            if (_ledger != null) _entries.AddRange(_ledger.Entries());
            else foreach (var d in CommissionCatalog.All) if (Commissions.StateOf(GameState.World, d.Id) != CommissionState.Unknown) _entries.Add(d);
            if (Row >= _entries.Count) Row = Mathf.Max(0, _entries.Count - 1);
            if (!_built) return;
            InkTheme.Show(_panel, IsOpen);
            if (!IsOpen) return;

            var w = GameState.World;
            _title.text = "Commissions" + (_ledger != null ? " — " + _ledger.HubId : "");
            _rows.Clear();
            for (int i = 0; i < _entries.Count; i++) _rows.Add(MakeRow(i, _entries[i], Commissions.StateOf(w, _entries[i].Id)));

            if (_entries.Count == 0)
            {
                _blurb.text = "Nothing posted. Come back when the coast has asked for something.";
                _steps.text = ""; _reward.text = "";
                return;
            }
            var def = _entries[Row];
            var state = Commissions.StateOf(w, def.Id);
            switch (state)
            {
                case CommissionState.Posted: _blurb.text = def.Brief + (string.IsNullOrEmpty(def.Poster) ? "" : "\n— " + def.Poster); break;
                case CommissionState.Taken: _blurb.text = def.Journal; break;
                case CommissionState.Fulfilled: _blurb.text = "Done. Turn it in."; break;
                case CommissionState.Closed: _blurb.text = def.Aftermath; break;
                default: _blurb.text = "It came to nothing."; break;
            }
            _steps.text = state == CommissionState.Taken || state == CommissionState.Fulfilled ? StepLines(w, def) : "";
            _reward.text = RewardLine(def);
        }

        public static string StepLines(WorldState w, CommissionDef def)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < def.Steps.Length; i++)
            {
                var s = def.Steps[i];
                bool done = Commissions.StepDone(w, def, i);
                sb.Append(done ? "✓ " : "· ").Append(s.Text);
                if (s.Kind == StepKind.Count) sb.Append("   ").Append(Commissions.Progress(w, def, i)).Append(" / ").Append(s.Target);
                if (i < def.Steps.Length - 1) sb.Append('\n');
            }
            if (def.RequiresAbility != Ability.None) sb.Append("\n[needs ").Append(def.RequiresAbility).Append(']');
            return sb.ToString();
        }

        public static string RewardLine(CommissionDef def)
        {
            var parts = new List<string>();
            if (def.RewardScraps > 0) parts.Add(def.RewardScraps + (def.RewardScraps == 1 ? " vellum scrap" : " vellum scraps"));
            if (def.RewardInstrument != InstrumentKind.None) parts.Add(InstrumentInfo.Of(def.RewardInstrument).Name);
            if (def.SeedsIsland) parts.Add("a place in the Blank");
            if (!string.IsNullOrEmpty(def.Foreshadows)) parts.Add("something worth knowing");
            return parts.Count == 0 ? "" : "Reward: " + string.Join(" · ", parts);
        }

        VisualElement MakeRow(int index, CommissionDef def, CommissionState state)
        {
            bool sel = index == Row;
            var row = InkTheme.Row("row-" + index);
            row.AddToClassList("ledger-row");
            row.AddToClassList(Commissions.Describe(state));
            row.EnableInClassList("selected", sel);
            InkTheme.SetPadding(row, 6f, 10f);
            InkTheme.SetRadius(row, 4f);
            row.style.backgroundColor = sel ? InkTheme.PaperDark : new Color(0f, 0f, 0f, 0f);
            var marker = InkTheme.Text("marker", sel ? "▸" : "", 22, InkTheme.Wash);
            marker.style.width = 26;
            bool dim = state == CommissionState.Closed || state == CommissionState.Failed;
            var title = InkTheme.Text("title", def.Title, 22, dim ? InkTheme.Dim : InkTheme.Ink);
            title.style.flexGrow = 1;
            string stateText; Color stateColor; var style = FontStyle.Normal;
            switch (state)
            {
                case CommissionState.Posted: stateText = "posted"; stateColor = InkTheme.Wash; break;
                case CommissionState.Taken: stateText = "taken"; stateColor = InkTheme.Ochre; break;
                case CommissionState.Fulfilled: stateText = "fulfilled — turn in"; stateColor = InkTheme.Ochre; style = FontStyle.Bold; break;
                case CommissionState.Closed: stateText = "closed"; stateColor = InkTheme.Dim; break;
                default: stateText = "failed"; stateColor = InkTheme.Dim; break;
            }
            var st = InkTheme.Text("state", stateText, 18, stateColor, style);
            st.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(marker); row.Add(title); row.Add(st);
            return row;
        }
    }
}
