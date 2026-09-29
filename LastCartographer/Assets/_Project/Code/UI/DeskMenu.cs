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
    /// elsewhere swaps places). The last row is the place: once every vantage here is surveyed it proposes
    /// anchor / hold / release and J seals it, once and for all (PRG-13). Anchoring needs the Guild's licence and a
    /// memory from someone who lives there (survey, bind, seal); holding needs the bounds walked. J / Space / Esc / East closes and saves.
    /// </summary>
    public sealed class DeskMenu : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public int Row { get; private set; }
        /// <summary>Index of the place row (after the Charter and the slots).</summary>
        public int FateRow => 1 + GameState.World.Equipment.SlotCount;
        /// <summary>Vellum rows (DES-05): masks, then the belt's fourth loop.</summary>
        public int MaskRow => FateRow + 1;
        public int BeltRow => FateRow + 2;
        public int RowCount => BeltRow + 1;
        public string PlaceId => Room.Current != null ? Room.Current.RoomId : "";
        public PlaceFate Proposed { get; private set; }
        /// <summary>A place, not yet decided, with every vantage in it surveyed.</summary>
        public bool CanSeal
        {
            get
            {
                var place = PlaceId;
                if (string.IsNullOrEmpty(place) || Places.IsDecided(GameState.World, place)) return false;
                var scene = Room.Current.gameObject.scene;
                foreach (var v in FindObjectsByType<VantagePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (v.gameObject.scene == scene && !v.IsSurveyed) return false;
                return true;
            }
        }

        WrenController _wren;
        CharterSet _charters;
        InstrumentBelt _belt;
        bool _wasFrozen, _built;
        VisualElement _panel, _rows;
        Label _title, _blurb, _flavour;

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
            Proposed = PlaceFate.Unwritten;
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
            int rows = RowCount;
            bool up = (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && (g.dpad.up.wasPressedThisFrame || g.leftStick.up.wasPressedThisFrame));
            bool down = (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && (g.dpad.down.wasPressedThisFrame || g.leftStick.down.wasPressedThisFrame));
            bool left = (k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame)) || (g != null && (g.dpad.left.wasPressedThisFrame || g.leftStick.left.wasPressedThisFrame));
            bool right = (k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)) || (g != null && (g.dpad.right.wasPressedThisFrame || g.leftStick.right.wasPressedThisFrame));
            bool confirm = (k != null && (k.jKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                           || (g != null && g.buttonSouth.wasPressedThisFrame);
            bool close = (k != null && k.escapeKey.wasPressedThisFrame)
                         || (g != null && (g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame));

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
            else if (confirm)
            {
                if (Row == FateRow && CanSeal && Proposed != PlaceFate.Unwritten) Confirm();
                else if (Row == MaskRow || Row == BeltRow) Confirm();
                else Close();
            }
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

        /// <summary>Seal the proposed fate on the place row, or buy on the vellum rows. False when nothing happens.</summary>
        public bool Confirm()
        {
            if (Row == MaskRow || Row == BeltRow)
            {
                bool bought = Row == MaskRow ? Economy.BuyMask(GameState.World) : Economy.BuySlot(GameState.World);
                if (bought) GameState.Save();
                Refresh();
                return bought;
            }
            if (Row != FateRow || !CanSeal || Proposed == PlaceFate.Unwritten) return false;
            if (Proposed == PlaceFate.Held && !BoundsWalks.IsWalked(GameState.World, PlaceId)) return false;   // the people hold it, not the seal (DES-13)
            if (Proposed == PlaceFate.Anchored && !Licence.MayAnchor(GameState.World)) return false;   // the Guild's seal, the Guild's licence
            if (Proposed == PlaceFate.Anchored && Memories.BoundFor(GameState.World, PlaceId) == null) return false;   // survey, bind, seal (bible 1.3)
            bool ok = Places.Decide(GameState.World, PlaceId, Proposed);
            if (ok) GameState.Save();
            Refresh();
            return ok;
        }

        static readonly PlaceFate[] Choices = { PlaceFate.Unwritten, PlaceFate.Anchored, PlaceFate.Held, PlaceFate.Released };

        /// <summary>Move the current row's choice by one (wraps). Row 0 is the Charter; then slots; then the place.</summary>
        public void Step(int dir)
        {
            var e = GameState.World.Equipment;
            if (Row == MaskRow || Row == BeltRow) return;
            if (Row == FateRow)
            {
                if (!CanSeal) return;
                int at = System.Array.IndexOf(Choices, Proposed);
                Proposed = Choices[(at + dir + Choices.Length) % Choices.Length];
                Refresh();
                return;
            }
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
            _title = InkTheme.Say("title", "desk.title", "Drafting desk", 30, InkTheme.Wash, FontStyle.Bold);
            _title.style.marginBottom = 14;
            _rows = new VisualElement { name = "rows", pickingMode = PickingMode.Ignore };
            _blurb = InkTheme.Text("blurb", "", 16, InkTheme.Dim);
            _blurb.style.marginTop = 10;
            // What the thing is, in Wren's hand, under what it does (NAR-17).
            _flavour = InkTheme.Text("flavour", "", 15, InkTheme.Dim, FontStyle.Italic);
            _flavour.style.marginTop = 4;
            var hint = InkTheme.Say("hint", "desk.hint", "↑↓ row    ◂▸ change    1-3 Charter    J seal / buy / leave    Esc leave", 15, InkTheme.Dim);
            hint.style.marginTop = 18;
            _panel.Add(_title); _panel.Add(_rows); _panel.Add(_blurb); _panel.Add(_flavour); _panel.Add(hint);
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
            string charterName = profile != null ? profile.LocalName : e.Charter.ToString();
            _rows.Add(MakeRow(0, Loc.T("desk.row.charter", "Charter"), charterName, ""));
            for (int i = 0; i < e.SlotCount; i++)
            {
                var s = e.Slots[i];
                string name = s.IsEmpty ? Loc.T("desk.slot.empty", "(empty)") : InstrumentInfo.Of(s.Kind).Name;
                string uses = s.IsEmpty ? "" : (s.UsesLeft < 0 ? "∞" : s.UsesLeft + " / " + InstrumentInfo.Of(s.Kind).Uses);
                _rows.Add(MakeRow(i + 1, Loc.F("desk.row.slot", "Slot {0}", i + 1), name, uses));
            }
            _rows.Add(MakeRow(FateRow, Loc.T("desk.row.place", "Place"), FateValue(out bool arrows), "", arrows));
            _rows.Add(MakeRow(MaskRow, Loc.T("desk.row.masks", "Masks"), MaskValue(), Loc.P("desk.scraps", Economy.Scraps(GameState.World), "{0} scrap", "{0} scraps"), false));
            _rows.Add(MakeRow(BeltRow, Loc.T("desk.row.belt", "Belt"), BeltValue(), "", false));
            _flavour.text = "";
            if (Row == 0) { _blurb.text = profile != null ? profile.LocalBlurb : ""; _flavour.text = profile != null ? Flavour.ForCharter(profile.Kind) : ""; }
            else if (Row == FateRow) _blurb.text = FateBlurb();
            else if (Row == MaskRow) _blurb.text = Economy.MasksFull(GameState.World) ? Loc.T("desk.masks.full", "The cowl holds nine. It will not take a tenth.") : Loc.T("desk.masks.buy", "Vellum stitched into the cowl. One more mask, and it is whole at once.");
            else if (Row == BeltRow) _blurb.text = GameState.World.Equipment.FourthSlotUnlocked ? Loc.T("desk.belt.full", "Four loops. A Guild belt.") : Loc.T("desk.belt.buy", "A fourth loop on the belt: one more Instrument carried.");
            else
            {
                var s = e.Slots[Row - 1];
                _blurb.text = s.IsEmpty ? Loc.T("desk.slot.empty_blurb", "An empty loop on the belt.") : InstrumentInfo.Of(s.Kind).Blurb;
                _flavour.text = Flavour.ForInstrument(s.Kind, GameState.World);
            }
        }

        string MaskValue()
        {
            var w = GameState.World;
            var vitals = _wren != null ? _wren.GetComponent<WrenVitals>() : null;
            int masks = vitals != null ? vitals.MaxMasks : 5 + Economy.MaskUpgrades(w);
            if (Economy.MasksFull(w)) return Loc.P("desk.masks.value_full", masks, "{0} mask · full", "{0} masks · full");
            return Loc.F("desk.masks.value", "{0} · {1}", Loc.P("desk.masks.count", masks, "{0} mask", "{0} masks"),
                Loc.P("desk.masks.cost", Economy.MaskUpgradeCost, "{0} scrap for one more", "{0} scraps for one more"));
        }

        string BeltValue()
        {
            var w = GameState.World;
            if (w.Equipment.FourthSlotUnlocked) return Loc.T("desk.belt.value_full", "four loops");
            return Loc.P("desk.belt.value", Economy.SlotUpgradeCost, "three loops · {0} scrap for a fourth", "three loops · {0} scraps for a fourth");
        }

        static string Verb(PlaceFate f) => f == PlaceFate.Anchored ? Loc.T("desk.verb.anchor", "anchor") : f == PlaceFate.Held ? Loc.T("desk.verb.hold", "hold") : f == PlaceFate.Released ? Loc.T("desk.verb.release", "release") : Loc.T("desk.verb.unwritten", "unwritten");

        string FateValue(out bool arrows)
        {
            arrows = false;
            var place = PlaceId;
            if (string.IsNullOrEmpty(place)) return Loc.T("desk.place.none", "(no place)");
            var fate = Places.FateOf(GameState.World, place);
            if (fate != PlaceFate.Unwritten) return Loc.F("desk.place.fate", "{0}: {1}", Atlas.PlaceName(place), Places.Display(fate));
            if (!CanSeal) return Loc.F("desk.place.survey_first", "{0}: survey it first", Atlas.PlaceName(place));
            arrows = true;
            return Loc.F("desk.place.fate", "{0}: {1}", Atlas.PlaceName(place), Verb(Proposed));
        }

        string FateBlurb()
        {
            var place = PlaceId;
            if (string.IsNullOrEmpty(place)) return Loc.T("desk.fate.none", "No place to seal from here.");
            switch (Places.FateOf(GameState.World, place))
            {
                case PlaceFate.Anchored: return Loc.T("desk.fate.anchored", "Sealed. The Guild's way: nothing here fades, and nothing changes.");
                case PlaceFate.Held: return Loc.T("desk.fate.held", "Sealed. Held by the people who live here, for as long as they do.");
                case PlaceFate.Released: return Loc.T("desk.fate.released", "Let go. It will be an island in the Blank, and remember how you left it.");
            }
            if (!CanSeal) return Loc.T("desk.fate.survey_first", "Draw every vantage here before you decide what it becomes.");
            switch (Proposed)
            {
                case PlaceFate.Anchored:
                    if (!Licence.MayAnchor(GameState.World))
                        return Loc.T("desk.propose.anchor_unlicensed", "Unlicensed. The Guild's seal needs the Guild's licence. Hold it, or let it go.");
                    var memory = Memories.BoundFor(GameState.World, PlaceId);
                    return memory != null
                        ? Loc.F("desk.propose.anchor_bound", "Survey, bind, seal: {0}. Nothing fades. Nothing changes. Wardens.", Memories.Name(memory))
                        : Loc.T("desk.propose.anchor_unbound", "Survey, bind, seal. Bind a true memory from someone who lives here first.");
                case PlaceFate.Held: return BoundsWalks.IsWalked(GameState.World, PlaceId)
                    ? Loc.T("desk.propose.hold_walked", "Walked. The people hold it, for as long as they do.")
                    : Loc.T("desk.propose.hold_unwalked", "Walk the bounds first. The people hold a place; a seal only freezes it.");
                case PlaceFate.Released: return Loc.T("desk.propose.release", "Let it go. It will be an island in the Blank, and remember how you left it.");
                default: return Loc.T("desk.propose.choose", "Choose what this place becomes. It is final.");
            }
        }

        VisualElement MakeRow(int index, string label, string value, string extra, bool arrows = true)
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
            var v = InkTheme.Text("value", arrows ? "◂  " + value + "  ▸" : value, 22, InkTheme.Ink);
            v.style.flexGrow = 1;
            var x = InkTheme.Text("extra", extra, 20, InkTheme.Wash, FontStyle.Bold);
            x.style.width = 90;
            x.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(marker); row.Add(l); row.Add(v); row.Add(x);
            return row;
        }
    }
}
