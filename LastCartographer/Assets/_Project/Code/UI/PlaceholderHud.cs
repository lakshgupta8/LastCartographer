using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.UI
{
    /// <summary>Masks, Inkwell, Charter and Instrument slots drawn with IMGUI until the atlas-and-ink UI (ENV-11) exists.</summary>
    public sealed class PlaceholderHud : MonoBehaviour
    {
        [SerializeField] int _fontSize = 26;

        WrenVitals _vitals;
        Inkwell _ink;
        CharterSet _charters;
        InstrumentBelt _belt;
        GUIStyle _style, _small;

        void Update()
        {
            if (_vitals != null) return;
            var wren = FindFirstObjectByType<WrenController>();
            if (wren == null) return;
            _vitals = wren.GetComponent<WrenVitals>();
            _ink = wren.GetComponent<Inkwell>();
            _charters = wren.GetComponent<CharterSet>();
            _belt = wren.GetComponent<InstrumentBelt>();
        }

        void OnGUI()
        {
            if (_vitals == null) return;
            if (_style == null)
            {
                var ink = new Color(0.08f, 0.08f, 0.11f);
                _style = new GUIStyle(GUI.skin.label) { fontSize = _fontSize, normal = { textColor = ink } };
                _small = new GUIStyle(GUI.skin.label) { fontSize = _fontSize - 8, normal = { textColor = ink } };
            }

            var masks = "";
            for (int i = 0; i < _vitals.MaxMasks; i++) masks += i < _vitals.Masks ? "◆ " : "◇ ";
            GUI.Label(new Rect(24f, 16f, 400f, 40f), masks, _style);

            if (_ink != null)
            {
                var ink = "";
                for (int i = 0; i < _ink.MaxPips; i++) ink += i < _ink.Pips ? "▮" : "▯";
                GUI.Label(new Rect(24f, 50f, 400f, 40f), ink, _style);
            }

            float y = 88f;
            if (_charters != null && _charters.Current != null)
            {
                GUI.Label(new Rect(24f, y, 500f, 30f), _charters.Current.DisplayName, _small);
                y += 26f;
            }
            if (_belt != null)
            {
                var e = _belt.Equipment;
                var line = "";
                for (int i = 0; i < e.SlotCount; i++)
                {
                    var s = e.Slots[i];
                    string name = s.IsEmpty ? "—" : InstrumentInfo.Of(s.Kind).Name;
                    string uses = s.IsEmpty ? "" : (s.UsesLeft < 0 ? (_belt.CooldownLeft(i) > 0f ? " …" : " ∞") : " " + s.UsesLeft);
                    bool sel = i == e.SelectedSlot;
                    line += (sel ? "[" : " ") + name + uses + (sel ? "]" : " ") + "   ";
                }
                GUI.Label(new Rect(24f, y, 900f, 30f), line, _small);
            }

            if (_vitals.IsDead)
                GUI.Label(new Rect(Screen.width * 0.5f - 120f, Screen.height * 0.4f, 240f, 40f), "the ink runs out", _style);
        }
    }
}
