using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// Masks as ink diamonds, the Inkwell as nine pips, the Clarity meter (a thin wash, shown only while it runs or
    /// fills), the Charter's name, and the Instrument slots with uses, top-left. The death caption sits mid-screen.
    /// UI Toolkit, built in code (ENV-11 greybox).
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] float _refreshSeconds = 0.1f;

        WrenVitals _vitals;
        Inkwell _ink;
        CharterSet _charters;
        InstrumentBelt _belt;
        ClarityMeter _clarity;
        VisualElement _root, _masks, _pips, _slots, _clarityBar, _clarityFill;
        Label _charter, _death, _seeds;
        float _nextRefresh;
        bool _built;

        public VisualElement Root => _root;
        public bool IsBound => _vitals != null;

        void Update()
        {
            if (!_built && !Build()) return;
            if (_vitals == null && !Bind()) return;
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + _refreshSeconds;
            Refresh();
        }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady) return false;
            var layer = ui.Hud;
            if (layer == null) return false;

            _root = new VisualElement { name = "hud", pickingMode = PickingMode.Ignore };
            _root.style.position = Position.Absolute;
            _root.style.left = 28; _root.style.top = 20;
            InkTheme.ApplyFont(_root);

            _masks = InkTheme.Row("hud-masks");
            _masks.style.marginBottom = 10;
            _root.Add(_masks);

            _pips = InkTheme.Row("hud-ink");
            _pips.style.marginBottom = 8;
            _root.Add(_pips);

            // Clarity: a bar the width of the Inkwell, the colour of her lantern; hidden while it is full and she is held.
            _clarityBar = new VisualElement { name = "hud-clarity", pickingMode = PickingMode.Ignore };
            InkTheme.SetSize(_clarityBar, 130, 7);
            InkTheme.SetBorder(_clarityBar, InkTheme.Ochre, 1.5f);
            InkTheme.SetRadius(_clarityBar, 3f);
            _clarityBar.style.marginBottom = 8;
            _clarityFill = new VisualElement { name = "hud-clarity-fill", pickingMode = PickingMode.Ignore };
            _clarityFill.style.height = new Length(100, LengthUnit.Percent);
            _clarityFill.style.width = new Length(100, LengthUnit.Percent);
            _clarityFill.style.backgroundColor = InkTheme.Ochre;
            _clarityBar.Add(_clarityFill);
            InkTheme.Show(_clarityBar, false);
            _root.Add(_clarityBar);

            _charter = InkTheme.Text("hud-charter", "", 18, InkTheme.Dim);
            _charter.style.marginBottom = 6;
            _root.Add(_charter);

            _slots = InkTheme.Row("hud-slots");
            _root.Add(_slots);

            _seeds = InkTheme.Text("hud-seeds", "", 16, InkTheme.Ochre);
            _seeds.style.marginTop = 6;
            _root.Add(_seeds);

            _death = InkTheme.Text("hud-death", "the ink runs out", 34, InkTheme.Ink, FontStyle.Italic);
            _death.style.position = Position.Absolute;
            _death.style.left = 0; _death.style.right = 0; _death.style.top = new Length(40, LengthUnit.Percent);
            _death.style.unityTextAlign = TextAnchor.MiddleCenter;
            InkTheme.Show(_death, false);

            layer.Add(_root);
            layer.Add(_death);
            _built = true;
            return true;
        }

        bool Bind()
        {
            var wren = FindFirstObjectByType<WrenController>();
            if (wren == null) return false;
            _vitals = wren.GetComponent<WrenVitals>();
            _ink = wren.GetComponent<Inkwell>();
            _charters = wren.GetComponent<CharterSet>();
            _belt = wren.GetComponent<InstrumentBelt>();
            _clarity = wren.GetComponent<ClarityMeter>();
            if (_vitals == null) return false;
            _vitals.MasksChanged += _ => Refresh();
            if (_ink != null) _ink.Changed += _ => Refresh();
            if (_belt != null) _belt.SelectionChanged += _ => Refresh();
            Refresh();
            return true;
        }

        public void Refresh()
        {
            if (!_built || _vitals == null) return;
            SyncCount(_masks, _vitals.MaxMasks, MakeMask);
            for (int i = 0; i < _masks.childCount; i++) SetFilled(_masks[i], i < _vitals.Masks, InkTheme.Ink);

            if (_ink != null)
            {
                SyncCount(_pips, _ink.MaxPips, MakePip);
                for (int i = 0; i < _pips.childCount; i++) SetFilled(_pips[i], i < _ink.Pips, InkTheme.Wash);
            }

            if (_clarity != null)
            {
                bool shown = _clarity.Level > 0 && (_clarity.IsUntethered || !_clarity.IsFull);
                InkTheme.Show(_clarityBar, shown);
                _clarityFill.style.width = new Length(_clarity.Fraction * 100f, LengthUnit.Percent);
            }

            _charter.text = _charters != null && _charters.Current != null ? _charters.Current.DisplayName : "";

            if (_belt != null)
            {
                var e = _belt.Equipment;
                SyncCount(_slots, e.SlotCount, MakeSlot);
                for (int i = 0; i < e.SlotCount; i++)
                {
                    var slot = _slots[i];
                    var s = e.Slots[i];
                    var name = (Label)slot.Q("name");
                    var uses = (Label)slot.Q("uses");
                    name.text = s.IsEmpty ? "—" : InstrumentInfo.Of(s.Kind).Name;
                    uses.text = s.IsEmpty ? "" : s.UsesLeft < 0 ? (_belt.CooldownLeft(i) > 0f ? "…" : "∞") : s.UsesLeft.ToString();
                    bool sel = i == e.SelectedSlot;
                    slot.EnableInClassList("selected", sel);
                    InkTheme.SetBorder(slot, sel ? InkTheme.Ink : InkTheme.InkFaint, sel ? 2f : 1f);
                    slot.style.backgroundColor = sel ? InkTheme.Paper : new Color(0.96f, 0.93f, 0.85f, 0.55f);
                }
            }

            _seeds.text = "✿ " + Economy.Seeds(GameState.World);
            InkTheme.Show(_death, _vitals.IsDead);
        }

        static void SyncCount(VisualElement parent, int count, System.Func<VisualElement> make)
        {
            while (parent.childCount < count) parent.Add(make());
            while (parent.childCount > count) parent.RemoveAt(parent.childCount - 1);
        }

        static void SetFilled(VisualElement e, bool filled, Color ink)
        {
            e.EnableInClassList("filled", filled);
            e.style.backgroundColor = filled ? ink : new Color(0f, 0f, 0f, 0f);
        }

        static VisualElement MakeMask()
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("mask");
            InkTheme.SetSize(e, 20, 20);
            e.style.marginRight = 14;
            e.style.rotate = new Rotate(45f);
            InkTheme.SetBorder(e, InkTheme.Ink, 2f);
            return e;
        }

        static VisualElement MakePip()
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("pip");
            InkTheme.SetSize(e, 10, 24);
            e.style.marginRight = 5;
            InkTheme.SetBorder(e, InkTheme.Wash, 1.5f);
            InkTheme.SetRadius(e, 2f);
            return e;
        }

        static VisualElement MakeSlot()
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("slot");
            e.style.flexDirection = FlexDirection.Row;
            e.style.alignItems = Align.Center;
            e.style.marginRight = 8;
            InkTheme.SetPadding(e, 4f, 10f);
            InkTheme.SetRadius(e, 4f);
            var name = InkTheme.Text("name", "", 16, InkTheme.Ink);
            var uses = InkTheme.Text("uses", "", 16, InkTheme.Wash, FontStyle.Bold);
            uses.style.marginLeft = 8;
            e.Add(name); e.Add(uses);
            return e;
        }
    }
}
