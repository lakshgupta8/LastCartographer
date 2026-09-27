using System.Collections.Generic;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OWSBG.UI
{
    /// <summary>
    /// Placeholder drafting-desk screen (PRG-11) drawn with IMGUI until the atlas UI (ENV-11) exists.
    /// Opens when Wren rests. Row 0 is the Charter (left/right or 1-3), the rows below are the
    /// Instrument slots (left/right cycles what is in the slot). J / Space / Esc / East closes.
    /// </summary>
    public sealed class DeskMenu : MonoBehaviour
    {
        [SerializeField] int _fontSize = 22;

        public bool IsOpen { get; private set; }
        public int Row { get; private set; }

        WrenController _wren;
        CharterSet _charters;
        InstrumentBelt _belt;
        GUIStyle _boxStyle, _textStyle, _titleStyle, _dimStyle;
        bool _wasFrozen;

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
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (_wren != null) _wren.Frozen = _wasFrozen;
            GameState.Save();
        }

        void Update()
        {
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

            if (up) Row = (Row + rows - 1) % rows;
            if (down) Row = (Row + 1) % rows;
            if (left) Step(-1);
            if (right) Step(1);
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame) PickCharter(0);
                if (k.digit2Key.wasPressedThisFrame) PickCharter(1);
                if (k.digit3Key.wasPressedThisFrame) PickCharter(2);
            }
            if (close) Close();
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
                return;
            }
            int slot = Row - 1;
            if (_belt == null || slot >= e.SlotCount) return;
            // Choices: None, then every owned Instrument (one held elsewhere swaps places with this slot).
            var choices = new List<InstrumentKind> { InstrumentKind.None };
            foreach (var info in InstrumentInfo.All)
                if (e.OwnsInstrument(info.Kind)) choices.Add(info.Kind);
            int cur = Mathf.Max(0, choices.IndexOf(e.Slots[slot].Kind));
            var next = choices[(cur + dir + choices.Count) % choices.Count];
            _belt.Equip(slot, next);
        }

        void OnGUI()
        {
            if (!IsOpen) return;
            EnsureStyles();
            var e = GameState.World.Equipment;
            float w = Mathf.Min(Screen.width - 64f, 760f);
            float h = 120f + 44f * (1 + e.SlotCount) + 40f;
            var rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(rect, GUIContent.none, _boxStyle);
            float x = rect.x + 28f, y = rect.y + 18f;
            GUI.Label(new Rect(x, y, w - 56f, 34f), "Drafting desk", _titleStyle);
            y += 44f;

            var profile = _charters != null ? _charters.Current : null;
            string charterName = profile != null ? profile.DisplayName : e.Charter.ToString();
            GUI.Label(new Rect(x, y, w - 56f, 30f), (Row == 0 ? "▸ " : "   ") + "Charter    ◂ " + charterName + " ▸", _textStyle);
            y += 30f;
            GUI.Label(new Rect(x + 28f, y, w - 84f, 26f), profile != null ? profile.Blurb : "", _dimStyle);
            y += 40f;

            for (int i = 0; i < e.SlotCount; i++)
            {
                var s = e.Slots[i];
                string name = s.IsEmpty ? "(empty)" : InstrumentInfo.Of(s.Kind).Name;
                string uses = s.IsEmpty ? "" : (s.UsesLeft < 0 ? "  ∞" : "  " + s.UsesLeft + "/" + InstrumentInfo.Of(s.Kind).Uses);
                GUI.Label(new Rect(x, y, w - 56f, 30f), (Row == i + 1 ? "▸ " : "   ") + "Slot " + (i + 1) + "     ◂ " + name + uses + " ▸", _textStyle);
                y += 30f;
                if (Row == i + 1 && !s.IsEmpty)
                {
                    GUI.Label(new Rect(x + 28f, y, w - 84f, 26f), InstrumentInfo.Of(s.Kind).Blurb, _dimStyle);
                }
                y += 14f;
            }
            GUI.Label(new Rect(x, rect.yMax - 36f, w - 56f, 26f), "↑↓ row   ◂▸ change   1-3 Charter   J / Esc leave", _dimStyle);
        }

        void EnsureStyles()
        {
            if (_boxStyle != null) return;
            var paper = new Texture2D(1, 1);
            paper.SetPixel(0, 0, new Color(0.96f, 0.93f, 0.85f, 0.97f));
            paper.Apply();
            _boxStyle = new GUIStyle(GUI.skin.box) { normal = { background = paper } };
            var ink = new Color(0.08f, 0.08f, 0.11f);
            _textStyle = new GUIStyle(GUI.skin.label) { fontSize = _fontSize, normal = { textColor = ink } };
            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = _fontSize + 6, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.20f, 0.27f, 0.45f) } };
            _dimStyle = new GUIStyle(GUI.skin.label) { fontSize = _fontSize - 6, normal = { textColor = new Color(0.35f, 0.35f, 0.40f) } };
        }
    }
}
