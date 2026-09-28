using OWSBG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// The one UI Toolkit document for the game (persistent scene). Owns the stacked layers the
    /// views draw into: HUD at the back, then boss bar, dialogue, desk, captions on top.
    /// Builds a runtime PanelSettings if the document has none (tests, tooling). Redraws everything in the other
    /// palette when the player turns high-contrast ink on or off (DES-14), and makes sure the options page is here.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiRoot : MonoBehaviour
    {
        public static UiRoot Instance { get; private set; }

        UIDocument _doc;
        bool _contrast;
        VisualElement _hud, _boss, _dialogue, _desk, _caption, _options, _fade;

        public VisualElement Root => _doc != null ? _doc.rootVisualElement : null;
        public VisualElement Hud => Layer(ref _hud, "layer-hud");
        public VisualElement BossLayer => Layer(ref _boss, "layer-boss");
        public VisualElement Dialogue => Layer(ref _dialogue, "layer-dialogue");
        public VisualElement Desk => Layer(ref _desk, "layer-desk");
        public VisualElement Caption => Layer(ref _caption, "layer-caption");
        public VisualElement OptionsLayer => Layer(ref _options, "layer-options");
        public VisualElement Fade => Layer(ref _fade, "layer-fade");
        public bool IsReady => Root != null;

        void Awake()
        {
            Instance = this;   // last one wins: a root being torn down must not block the next
            _doc = GetComponent<UIDocument>();
            if (_doc.panelSettings == null)
            {
                var ps = Resources.Load<PanelSettings>("OWSBG_PanelSettings");
                if (ps == null)
                {
                    ps = ScriptableObject.CreateInstance<PanelSettings>();
                    ps.name = "PanelSettings (runtime)";
                    ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                    ps.referenceResolution = new Vector2Int(1920, 1080);
                    ps.match = 0.5f;
                }
                _doc.panelSettings = ps;
            }
            if (GetComponent<OptionsView>() == null) gameObject.AddComponent<OptionsView>();
            _contrast = Options.HighContrast;
        }

        void OnEnable() { Options.Changed += OnOptions; }
        void OnDisable() { Options.Changed -= OnOptions; }

        void OnOptions()
        {
            if (_contrast == Options.HighContrast) return;
            _contrast = Options.HighContrast;
            InkTheme.Recolour(Root, _contrast);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        VisualElement Layer(ref VisualElement field, string name)
        {
            var root = Root;
            if (root == null) return null;
            if (field != null && field.parent == root) return field;
            field = root.Q(name);
            if (field == null)
            {
                // Keep the stacking order stable whichever view asks first.
                string[] order = { "layer-hud", "layer-boss", "layer-dialogue", "layer-desk", "layer-caption", "layer-options", "layer-fade" };
                foreach (var n in order) if (root.Q(n) == null) root.Add(InkTheme.Layer(n));
                field = root.Q(name);
            }
            return field;
        }
    }
}
