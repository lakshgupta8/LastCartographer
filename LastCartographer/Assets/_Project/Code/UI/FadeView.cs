using OWSBG.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>Draws <see cref="ScreenFade"/>: a full-screen sheet on the top layer, clear at level 0.</summary>
    public sealed class FadeView : MonoBehaviour
    {
        VisualElement _sheet;
        bool _built;

        public float Alpha => _built ? _sheet.style.backgroundColor.value.a : 0f;

        void Update()
        {
            if (!_built && !Build()) return;
            var c = ScreenFade.Color;
            float a = ScreenFade.Level;
            _sheet.style.backgroundColor = new Color(c.r, c.g, c.b, a);
            InkTheme.Show(_sheet, a > 0.001f);
        }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.Fade == null) return false;
            _sheet = new VisualElement { name = "fade", pickingMode = PickingMode.Ignore };
            _sheet.style.position = Position.Absolute;
            _sheet.style.left = 0; _sheet.style.right = 0; _sheet.style.top = 0; _sheet.style.bottom = 0;
            InkTheme.Show(_sheet, false);
            ui.Fade.Add(_sheet);
            _built = true;
            return true;
        }
    }
}
