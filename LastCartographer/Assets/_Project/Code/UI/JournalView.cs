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
    /// The journal (the atlas's right-hand page, PRG-12). Lists taken and fulfilled commissions with their
    /// steps, then the closed ones, and the vellum-scrap count. Also owns the small caption that announces
    /// ledger changes ("Commission taken · Lantern Chain"). With an AtlasView on the same object it is hosted
    /// on the atlas spread and opens with it; alone, M or gamepad Select toggles it.
    /// </summary>
    public sealed class JournalView : MonoBehaviour
    {
        [SerializeField] float _toastSeconds = 3f;

        public bool IsOpen { get; private set; }
        public VisualElement Panel => _panel;
        /// <summary>Drawn inside the atlas spread rather than as its own page.</summary>
        public bool IsHosted => _host != null;
        public string ToastText => _toast != null ? _toast.text : _pendingToast;
        public bool IsToastShowing => _toastLeft > 0f;

        WrenController _wren;
        bool _wasFrozen, _built;
        VisualElement _panel, _open, _closed, _host;
        Label _title, _empty, _scraps, _toast, _hint;
        string _pendingToast;
        float _toastLeft;

        void OnEnable() { Commissions.Changed += OnChanged; }
        void OnDisable() { Commissions.Changed -= OnChanged; }

        void OnChanged(string id, CommissionState state)
        {
            var def = CommissionCatalog.Find(id);
            string title = def != null ? Commissions.TitleOf(def) : id;
            switch (state)
            {
                case CommissionState.Taken: Toast(Loc.F("journal.toast.taken", "Commission taken · {0}", title)); break;
                case CommissionState.Fulfilled: Toast(Loc.F("journal.toast.fulfilled", "{0} · fulfilled. Return to the ledger.", title)); break;
                case CommissionState.Closed:
                    var reward = def != null ? LedgerView.RewardLine(def) : "";
                    Toast(reward.Length > 0 ? Loc.F("journal.toast.closed_reward", "{0} · closed.  {1}", title, reward) : Loc.F("journal.toast.closed", "{0} · closed", title));
                    break;
                case CommissionState.Failed: Toast(Loc.F("journal.toast.failed", "{0} · came to nothing.", title)); break;
            }
            if (IsOpen) Refresh();
        }

        public void Toast(string text)
        {
            _pendingToast = text;
            _toastLeft = Options.CaptionSeconds(_toastSeconds);
            if (_built) { _toast.text = text; InkTheme.Show(_toast, true); }
        }

        public void Toggle()
        {
            if (IsHosted && AtlasView.Instance != null) { AtlasView.Instance.Toggle(); return; }
            if (IsOpen) Close(); else Open(FindFirstObjectByType<WrenController>());
        }

        /// <summary>Draw on the atlas's right page: no frame, no own position, opened by the atlas.</summary>
        public void AttachTo(VisualElement host)
        {
            _host = host;
            if (_built) Host();
        }

        void Host()
        {
            _panel.RemoveFromHierarchy();
            _panel.style.position = Position.Relative;
            _panel.style.left = new StyleLength(StyleKeyword.Auto);
            _panel.style.top = new StyleLength(StyleKeyword.Auto);
            _panel.style.translate = new StyleTranslate(StyleKeyword.None);
            _panel.style.width = new Length(100, LengthUnit.Percent);
            _panel.style.maxWidth = new StyleLength(StyleKeyword.None);
            _panel.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            InkTheme.SetBorder(_panel, new Color(0f, 0f, 0f, 0f), 0f);
            InkTheme.SetPadding(_panel, 0f, 0f);
            InkTheme.Show(_hint, false);
            _host.Add(_panel);
        }

        public void Open(WrenController wren)
        {
            _wren = wren;
            if (wren != null) { _wasFrozen = wren.Frozen; wren.Frozen = true; }
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

        void Update()
        {
            if (!_built && Build())
            {
                if (_pendingToast != null && _toastLeft > 0f) { _toast.text = _pendingToast; InkTheme.Show(_toast, true); }
                if (IsOpen) Refresh();
            }
            if (_toastLeft > 0f)
            {
                if (float.IsPositiveInfinity(_toastLeft) && PromptView.DismissPressed()) _toastLeft = 0f;
                _toastLeft -= Time.unscaledDeltaTime;
                if (_toastLeft <= 0f && _built) InkTheme.Show(_toast, false);
            }
            if (IsHosted) return;   // the atlas owns the keys

            var k = Keyboard.current;
            var g = Gamepad.current;
            bool toggle = (k != null && k.mKey.wasPressedThisFrame) || (g != null && g.selectButton.wasPressedThisFrame);
            bool close = (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame);
            if (IsOpen)
            {
                if (toggle || close) Close();
                return;
            }
            if (!toggle) return;
            if (DialogueService.Instance != null && DialogueService.Instance.IsRunning) return;
            var wren = FindFirstObjectByType<WrenController>();
            if (wren != null && wren.Frozen) return;   // a desk or ledger page owns the screen
            Open(wren);
        }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.Desk == null || ui.Caption == null) return false;
            _panel = InkTheme.Panel("journal");
            _panel.style.position = Position.Absolute;
            _panel.style.left = new Length(50, LengthUnit.Percent);
            _panel.style.top = new Length(50, LengthUnit.Percent);
            _panel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            _panel.style.width = 860;
            _panel.style.maxWidth = new Length(92, LengthUnit.Percent);
            _title = InkTheme.Say("title", "journal.title", "Journal", 30, InkTheme.Wash, FontStyle.Bold);
            _title.style.marginBottom = 14;
            _empty = InkTheme.Say("empty", "journal.empty", "No commissions taken. The ledgers are at the hubs.", 17, InkTheme.Dim);
            _open = new VisualElement { name = "open", pickingMode = PickingMode.Ignore };
            _closed = new VisualElement { name = "closed", pickingMode = PickingMode.Ignore };
            _closed.style.marginTop = 12;
            _scraps = InkTheme.Text("scraps", "", 16, InkTheme.Wash);
            _scraps.style.marginTop = 16;
            _hint = InkTheme.Say("hint", "journal.hint", "M / Esc close", 15, InkTheme.Dim);
            _hint.style.marginTop = 12;
            _panel.Add(_title); _panel.Add(_empty); _panel.Add(_open); _panel.Add(_closed); _panel.Add(_scraps); _panel.Add(_hint);
            InkTheme.Show(_panel, false);
            if (_host != null) Host(); else ui.Desk.Add(_panel);

            _toast = InkTheme.Text("journal-toast", "", 21, InkTheme.Ink, FontStyle.Italic);
            _toast.style.position = Position.Absolute;
            _toast.style.left = new Length(50, LengthUnit.Percent);
            _toast.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            _toast.style.top = new Length(12, LengthUnit.Percent);
            _toast.style.backgroundColor = InkTheme.Paper;
            InkTheme.SetPadding(_toast, 8f, 18f);
            InkTheme.SetRadius(_toast, 6f);
            InkTheme.SetBorder(_toast, InkTheme.InkFaint, 1f);
            InkTheme.Show(_toast, false);
            ui.Caption.Add(_toast);
            _built = true;
            return true;
        }

        public void Refresh()
        {
            if (!_built) return;
            InkTheme.Show(_panel, IsOpen);
            if (!IsOpen) return;
            var w = GameState.World;
            _open.Clear(); _closed.Clear();
            int openCount = 0, closedCount = 0;
            foreach (var def in CommissionCatalog.All)
            {
                var state = Commissions.StateOf(w, def.Id);
                if (state == CommissionState.Taken || state == CommissionState.Fulfilled) { _open.Add(Entry(def, state)); openCount++; }
                else if (state == CommissionState.Closed || state == CommissionState.Failed) { _closed.Add(Entry(def, state)); closedCount++; }
            }
            InkTheme.Show(_empty, openCount == 0 && closedCount == 0);
            if (closedCount > 0)
            {
                var head = InkTheme.Text("closed-head", Loc.T("journal.closed", "Closed"), 16, InkTheme.Dim, FontStyle.Bold);
                head.style.marginBottom = 4;
                _closed.Insert(0, head);
            }
            _scraps.text = Loc.F("journal.purse", "Vellum scraps: {0}     Iris seeds: {1}", Commissions.Scraps(w), Economy.Seeds(w));
        }

        VisualElement Entry(CommissionDef def, CommissionState state)
        {
            var box = new VisualElement { name = "entry-" + def.Id, pickingMode = PickingMode.Ignore };
            box.AddToClassList("journal-entry");
            box.AddToClassList(Commissions.Describe(state));
            box.style.marginBottom = 8;
            bool dim = state == CommissionState.Closed || state == CommissionState.Failed;
            var row = InkTheme.Row("head");
            var title = InkTheme.Text("title", Commissions.TitleOf(def), 21, dim ? InkTheme.Dim : InkTheme.Ink, FontStyle.Bold);
            title.style.flexGrow = 1;
            var hub = InkTheme.Text("hub", (state == CommissionState.Fulfilled ? Loc.F("journal.turn_in", "{0} · turn in", Commissions.HubName(def.Hub)) : Commissions.HubName(def.Hub)), 15, dim ? InkTheme.Dim : InkTheme.Ochre);
            row.Add(title); row.Add(hub);
            box.Add(row);
            if (!dim)
            {
                var body = InkTheme.Text("body", Commissions.JournalOf(def), 16, InkTheme.Ink);
                body.style.marginLeft = 12;
                var steps = InkTheme.Text("steps", LedgerView.StepLines(GameState.World, def), 15, InkTheme.Dim);
                steps.style.marginLeft = 12;
                box.Add(body); box.Add(steps);
            }
            else
            {
                var after = InkTheme.Text("body", state == CommissionState.Closed ? Commissions.AftermathOf(def) : Loc.T("commission.failed", "It came to nothing."), 15, InkTheme.Dim);
                after.style.marginLeft = 12;
                box.Add(after);
            }
            return box;
        }
    }
}
