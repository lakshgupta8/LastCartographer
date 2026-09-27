using OWSBG.World;
using UnityEngine;

namespace OWSBG.UI
{
    /// <summary>Masks and Inkwell drawn with IMGUI until the atlas-and-ink UI (ENV-11) exists.</summary>
    public sealed class PlaceholderHud : MonoBehaviour
    {
        [SerializeField] int _fontSize = 26;

        WrenVitals _vitals;
        Inkwell _ink;
        GUIStyle _style;

        void Update()
        {
            if (_vitals != null) return;
            var wren = FindFirstObjectByType<WrenController>();
            if (wren == null) return;
            _vitals = wren.GetComponent<WrenVitals>();
            _ink = wren.GetComponent<Inkwell>();
        }

        void OnGUI()
        {
            if (_vitals == null) return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = _fontSize, normal = { textColor = new Color(0.08f, 0.08f, 0.11f) } };

            var masks = "";
            for (int i = 0; i < _vitals.MaxMasks; i++) masks += i < _vitals.Masks ? "◆ " : "◇ ";
            GUI.Label(new Rect(24f, 16f, 400f, 40f), masks, _style);

            if (_ink != null)
            {
                var ink = "";
                for (int i = 0; i < _ink.MaxPips; i++) ink += i < _ink.Pips ? "▮" : "▯";
                GUI.Label(new Rect(24f, 50f, 400f, 40f), ink, _style);
            }

            if (_vitals.IsDead)
                GUI.Label(new Rect(Screen.width * 0.5f - 120f, Screen.height * 0.4f, 240f, 40f), "the ink runs out", _style);
        }
    }
}
