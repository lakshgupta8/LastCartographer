using System.Collections.Generic;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// Masks as inked feathers, the Inkwell as a bottle that visibly fills with its nine pips as the marks up its
    /// glass (art-direction 6, ENV-11), the Clarity meter (a thin wash by her lantern, shown only while it runs or
    /// fills), the Charter's cowl and name, and the Instrument slots with their drawings and uses, top-left. The
    /// death caption sits mid-screen. UI Toolkit, built in code; without the drawings the masks are diamonds and
    /// the pips a row.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] float _refreshSeconds = 0.1f;

        /// <summary>The bottle's height on the HUD, px at the reference resolution.</summary>
        public const float WellHeight = 76f;

        WrenVitals _vitals;
        Inkwell _ink;
        CharterSet _charters;
        InstrumentBelt _belt;
        ClarityMeter _clarity;
        VisualElement _root, _masks, _pips, _slots, _clarityBar, _clarityFill, _clarityLantern, _well, _wellClip, _charterIcon, _seedGlyph;
        Label _charter, _death, _seeds;
        float _nextRefresh, _wellBottom, _wellSpan;
        bool _built;
        System.Func<VisualElement> _makeMask, _makePip, _makeSlot;

        public VisualElement Root => _root;
        public bool IsBound => _vitals != null;
        /// <summary>The bottle, when the Inkwell is drawn.</summary>
        public VisualElement Well => _well;
        /// <summary>How much of the bottle's fill shows, 0 to 1 of the ink's run, or -1 without the bottle.</summary>
        public float WellFraction => _wellClip != null && _wellSpan > 0f ? _wellClip.style.height.value.value / (_wellSpan * WellHeight) : -1f;

        void OnEnable() { Loc.Changed += OnLocale; }
        void OnDisable() { Loc.Changed -= OnLocale; }
        void OnLocale(string _) => Refresh();   // names from the catalogs change with the language, not only with the belt

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
            _makeMask = MakeMask; _makePip = MakePip; _makeSlot = MakeSlot;

            _root = new VisualElement { name = "hud", pickingMode = PickingMode.Ignore };
            _root.style.position = Position.Absolute;
            _root.style.left = 28; _root.style.top = 20;
            InkTheme.ApplyFont(_root);

            _masks = InkTheme.Row("hud-masks");
            _masks.style.marginBottom = 10;
            _root.Add(_masks);

            // Beside the Inkwell: the Clarity meter, then the Charter.
            var beside = new VisualElement { name = "hud-beside", pickingMode = PickingMode.Ignore };

            // The Inkwell: the bottle with the pips as marks up its glass when it is drawn, nine pips in a row otherwise.
            _pips = InkTheme.Row("hud-ink");
            var bottle = InkArt.Tex("UI_Inkwell");
            if (bottle != null)
            {
                var fill = InkArt.PieceOf("UI_InkwellFill")?.fill;
                _wellBottom = fill != null ? fill.bottom : 0.10f;
                float top = fill != null ? fill.top : 0.80f;
                _wellSpan = Mathf.Max(0.01f, top - _wellBottom);
                _well = InkArt.Glyph("hud-well", bottle, WellHeight);
                _well.style.marginRight = 14;
                _wellClip = new VisualElement { name = "hud-well-clip", pickingMode = PickingMode.Ignore };
                _wellClip.style.position = Position.Absolute;
                _wellClip.style.left = 0; _wellClip.style.right = 0;
                _wellClip.style.bottom = _wellBottom * WellHeight;
                _wellClip.style.height = 0f;
                _wellClip.style.overflow = Overflow.Hidden;
                var ink = InkArt.Glyph("hud-well-ink", InkArt.Tex("UI_InkwellFill"), WellHeight);
                ink.style.position = Position.Absolute;
                ink.style.left = 0;
                ink.style.bottom = -_wellBottom * WellHeight;   // the ink's bottom sits on the bottle's
                _wellClip.Add(ink);
                _well.Add(_wellClip);
                _pips.style.position = Position.Absolute;
                _pips.style.flexDirection = FlexDirection.ColumnReverse;
                _pips.style.justifyContent = Justify.SpaceBetween;
                _pips.style.alignItems = Align.FlexEnd;
                _pips.style.right = 7;
                _pips.style.top = (1f - top) * WellHeight + 2f;
                _pips.style.bottom = _wellBottom * WellHeight + 2f;
                _well.Add(_pips);
                var wellRow = InkTheme.Row("hud-well-row");
                wellRow.style.alignItems = Align.FlexStart;
                wellRow.style.marginBottom = 8;
                wellRow.Add(_well); wellRow.Add(beside);
                _root.Add(wellRow);
            }
            else
            {
                _pips.style.marginBottom = 8;
                _root.Add(_pips);
                _root.Add(beside);
            }

            // Clarity: a bar the width of the Inkwell, the colour of her lantern, by its drawing; hidden while it is full and she is held.
            var clarityRow = InkTheme.Row("hud-clarity-row");
            clarityRow.style.marginBottom = 8;
            _clarityLantern = InkTheme.Icon("hud-lantern", InkArt.Tex("UI_Lantern"), 18f, 6f);
            if (_clarityLantern != null) { InkTheme.Show(_clarityLantern, false); clarityRow.Add(_clarityLantern); }
            _clarityBar = new VisualElement { name = "hud-clarity", pickingMode = PickingMode.Ignore };
            InkTheme.SetSize(_clarityBar, 130, 7);
            InkTheme.SetBorder(_clarityBar, InkTheme.Ochre, 1.5f);
            InkTheme.SetRadius(_clarityBar, 3f);
            _clarityFill = new VisualElement { name = "hud-clarity-fill", pickingMode = PickingMode.Ignore };
            _clarityFill.style.height = new Length(100, LengthUnit.Percent);
            _clarityFill.style.width = new Length(100, LengthUnit.Percent);
            _clarityFill.style.backgroundColor = InkTheme.Ochre;
            _clarityBar.Add(_clarityFill);
            InkTheme.Show(_clarityBar, false);
            clarityRow.Add(_clarityBar);
            beside.Add(clarityRow);

            // The Charter: her cowl, then its name.
            var charterRow = InkTheme.Row("hud-charter-row");
            charterRow.style.marginBottom = 6;
            if (InkArt.Tex("UI_Charter_Surveyor") != null)
            {
                _charterIcon = InkArt.Glyph("hud-charter-icon", (Texture2D)null, 26f);
                _charterIcon.style.marginRight = 6;
                charterRow.Add(_charterIcon);
            }
            _charter = InkTheme.Text("hud-charter", "", 18, InkTheme.Dim);
            charterRow.Add(_charter);
            beside.Add(charterRow);

            _slots = InkTheme.Row("hud-slots");
            _root.Add(_slots);

            var purse = InkTheme.Row("hud-purse");
            purse.style.marginTop = 6;
            _seedGlyph = InkTheme.Icon("hud-seed", InkArt.Tex("UI_Seed"), 18f, 5f);
            if (_seedGlyph != null) purse.Add(_seedGlyph);
            _seeds = InkTheme.Text("hud-seeds", "", 16, InkTheme.Ochre);
            purse.Add(_seeds);
            _root.Add(purse);

            _death = InkTheme.Title("hud-death", "hud.death", "the ink runs out", 38, InkTheme.Ink, FontStyle.Italic);
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

        // What the last refresh wrote, so a quiet refresh writes nothing: the strings it builds are the HUD's only
        // garbage, and a quiet frame allocates nothing (PRG-24).
        int _shownSeeds = int.MinValue;
        string _shownCharter;
        readonly List<int> _shownUses = new List<int>();

        public void Refresh()
        {
            if (!_built || _vitals == null) return;
            SyncCount(_masks, _vitals.MaxMasks, _makeMask);
            for (int i = 0; i < _masks.childCount; i++) SetMask(_masks[i], i < _vitals.Masks);

            if (_ink != null)
            {
                SyncCount(_pips, _ink.MaxPips, _makePip);
                for (int i = 0; i < _pips.childCount; i++) SetPip(_pips[i], i < _ink.Pips);
                if (_wellClip != null)
                {
                    float fraction = _ink.MaxPips > 0 ? Mathf.Clamp01(_ink.Pips / (float)_ink.MaxPips) : 0f;
                    _wellClip.style.height = fraction * _wellSpan * WellHeight;
                }
            }

            if (_clarity != null)
            {
                bool shown = _clarity.Level > 0 && (_clarity.IsUntethered || !_clarity.IsFull);
                InkTheme.Show(_clarityBar, shown);
                if (_clarityLantern != null) InkTheme.Show(_clarityLantern, shown);
                _clarityFill.style.width = new Length(_clarity.Fraction * 100f, LengthUnit.Percent);
            }

            var profile = _charters != null ? _charters.Current : null;
            var charter = profile != null ? profile.LocalName : "";
            if (!ReferenceEquals(charter, _shownCharter) && charter != _shownCharter) _charter.text = charter;
            _shownCharter = charter;
            if (_charterIcon != null) InkArt.SetGlyph(_charterIcon, profile != null ? InkArt.CharterIcon(profile.Kind) : null);

            if (_belt != null)
            {
                var e = _belt.Equipment;
                SyncCount(_slots, e.SlotCount, _makeSlot);
                while (_shownUses.Count < e.SlotCount) _shownUses.Add(int.MinValue);
                for (int i = 0; i < e.SlotCount; i++)
                {
                    var slot = _slots[i];
                    var s = e.Slots[i];
                    var name = (Label)slot.userData;
                    var uses = (Label)name.userData;
                    var icon = uses.userData as VisualElement;
                    name.text = s.IsEmpty ? "—" : InstrumentInfo.Of(s.Kind).Name;
                    if (icon != null)
                    {
                        var tex = InkArt.InstrumentIcon(s.Kind);
                        InkArt.SetGlyph(icon, tex);
                        InkTheme.Show(icon, tex != null);
                    }
                    // The count's string is made when the count changes; a cooldown or the endless mark is a constant.
                    int usesNow = s.IsEmpty ? -2 : s.UsesLeft < 0 ? (_belt.CooldownLeft(i) > 0f ? -3 : -4) : s.UsesLeft;
                    if (usesNow != _shownUses[i])
                    {
                        _shownUses[i] = usesNow;
                        uses.text = usesNow == -2 ? "" : usesNow == -3 ? "…" : usesNow == -4 ? "∞" : usesNow.ToString();
                    }
                    bool sel = i == e.SelectedSlot;
                    slot.EnableInClassList("selected", sel);
                    InkTheme.SetBorder(slot, sel ? InkTheme.Ink : InkTheme.InkFaint, sel ? 2f : 1f);
                    slot.style.backgroundColor = sel ? InkTheme.Paper : new Color(0.96f, 0.93f, 0.85f, 0.55f);
                }
            }

            int seeds = Economy.Seeds(GameState.World);
            if (seeds != _shownSeeds) { _shownSeeds = seeds; _seeds.text = _seedGlyph != null ? seeds.ToString() : "✿ " + seeds; }
            InkTheme.Show(_death, _vitals.IsDead);
        }

        static void SyncCount(VisualElement parent, int count, System.Func<VisualElement> make)
        {
            while (parent.childCount < count) parent.Add(make());
            while (parent.childCount > count) parent.RemoveAt(parent.childCount - 1);
        }

        /// <summary>A mask: the full feather or the empty one when drawn, an inked or an outlined diamond otherwise.</summary>
        static void SetMask(VisualElement e, bool filled)
        {
            if (e.ClassListContains("filled") == filled) return;
            e.EnableInClassList("filled", filled);
            if (e.ClassListContains("ink-glyph")) InkArt.SetGlyph(e, InkArt.Tex(filled ? "UI_MaskFull" : "UI_MaskEmpty"));
            else e.style.backgroundColor = filled ? InkTheme.Ink : new Color(0f, 0f, 0f, 0f);
        }

        /// <summary>A pip: a mark on the glass inked or faint, or a pip in the row filled or empty.</summary>
        void SetPip(VisualElement e, bool filled)
        {
            if (e.ClassListContains("filled") == filled) return;
            e.EnableInClassList("filled", filled);
            e.style.backgroundColor = filled ? InkTheme.Wash : _well != null ? InkTheme.InkFaint : new Color(0f, 0f, 0f, 0f);
        }

        static VisualElement MakeMask()
        {
            var empty = InkArt.Tex("UI_MaskEmpty");
            if (empty != null && InkArt.Tex("UI_MaskFull") != null)
            {
                var g = InkArt.Glyph("mask", empty, 34f);
                g.AddToClassList("mask");
                g.style.marginRight = 6;
                return g;
            }
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("mask");
            InkTheme.SetSize(e, 20, 20);
            e.style.marginRight = 14;
            e.style.rotate = new Rotate(45f);
            InkTheme.SetBorder(e, InkTheme.Ink, 2f);
            return e;
        }

        VisualElement MakePip()
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("pip");
            if (_well != null)
            {
                InkTheme.SetSize(e, 12, 3);
                e.style.backgroundColor = InkTheme.InkFaint;
                return e;
            }
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
            VisualElement icon = null;
            if (InkArt.Tex("UI_Instrument_CompassDart") != null)
            {
                icon = InkArt.Glyph("icon", (Texture2D)null, 22f);
                icon.style.marginRight = 6;
                InkTheme.Show(icon, false);
                e.Add(icon);
            }
            var name = InkTheme.Text("name", "", 16, InkTheme.Ink);
            var uses = InkTheme.Text("uses", "", 16, InkTheme.Wash, FontStyle.Bold);
            uses.style.marginLeft = 8;
            e.Add(name); e.Add(uses);
            e.userData = name; name.userData = uses; uses.userData = icon;   // found once here, not queried every refresh
            return e;
        }
    }
}
