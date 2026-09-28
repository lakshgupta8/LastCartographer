using OWSBG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// The paper-and-ink palette and element helpers for the atlas UI (art direction 5, ENV-11).
    /// Everything is built in code with explicit fonts so it works with or without a theme asset;
    /// the hand-drawn frames and the ink font replace these when the UI art lands.
    /// </summary>
    public static class InkTheme
    {
        public static readonly Color Paper = new Color(0.96f, 0.93f, 0.85f, 0.97f);
        public static readonly Color PaperDark = new Color(0.88f, 0.84f, 0.74f, 1f);
        public static readonly Color Ink = new Color(0.08f, 0.08f, 0.11f, 1f);
        public static readonly Color InkFaint = new Color(0.08f, 0.08f, 0.11f, 0.25f);
        public static readonly Color Wash = new Color(0.20f, 0.27f, 0.45f, 1f);
        public static readonly Color Dim = new Color(0.35f, 0.35f, 0.40f, 1f);
        public static readonly Color Ochre = new Color(0.86f, 0.70f, 0.30f, 1f);

        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static void ApplyFont(VisualElement e)
        {
            var f = Font;
            if (f != null) e.style.unityFontDefinition = new StyleFontDefinition(f);
        }

        /// <summary>A full-screen, click-through layer.</summary>
        public static VisualElement Layer(string name)
        {
            var e = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.bottom = 0;
            return e;
        }

        /// <summary>A paper panel: warm ground, thin ink rule, soft corners.</summary>
        public static VisualElement Panel(string name)
        {
            var e = new VisualElement { name = name };
            e.style.backgroundColor = Paper;
            SetBorder(e, Ink, 2f);
            SetRadius(e, 6f);
            SetPadding(e, 18f, 24f);
            ApplyFont(e);
            return e;
        }

        public static Label Text(string name, string text, float size, Color color, FontStyle style = FontStyle.Normal)
        {
            var l = new Label(text) { name = name, pickingMode = PickingMode.Ignore };
            l.style.fontSize = size;
            l.style.color = color;
            l.style.unityFontStyleAndWeight = style;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginBottom = 0; l.style.marginTop = 0; l.style.marginLeft = 0; l.style.marginRight = 0;
            l.style.paddingBottom = 0; l.style.paddingTop = 0; l.style.paddingLeft = 0; l.style.paddingRight = 0;
            ApplyFont(l);
            return l;
        }

        /// <summary>A fixed label in the player's language (PRG-19): <see cref="Loc.T"/> now, and again when the locale changes.</summary>
        public static Label Say(string name, string key, string english, float size, Color color, FontStyle style = FontStyle.Normal)
        {
            var l = Text(name, "", size, color, style);
            Relabel(l, () => Loc.T(key, english));
            return l;
        }

        /// <summary>Keeps a label's text in the player's language: set now, and again whenever the locale changes while it is shown.</summary>
        public static void Relabel(TextElement label, System.Func<string> text)
        {
            label.text = text();
            System.Action<string> on = _ => label.text = text();
            label.RegisterCallback<AttachToPanelEvent>(_ => { Loc.Changed -= on; Loc.Changed += on; label.text = text(); });
            label.RegisterCallback<DetachFromPanelEvent>(_ => Loc.Changed -= on);
            if (label.panel != null) Loc.Changed += on;
        }

        public static VisualElement Row(string name)
        {
            var e = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            e.style.flexDirection = FlexDirection.Row;
            e.style.alignItems = Align.Center;
            return e;
        }

        public static void SetBorder(VisualElement e, Color c, float w)
        {
            e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c;
            e.style.borderTopWidth = w; e.style.borderBottomWidth = w; e.style.borderLeftWidth = w; e.style.borderRightWidth = w;
        }

        public static void SetRadius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r; e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        public static void SetPadding(VisualElement e, float vertical, float horizontal)
        {
            e.style.paddingTop = vertical; e.style.paddingBottom = vertical; e.style.paddingLeft = horizontal; e.style.paddingRight = horizontal;
        }

        public static void SetSize(VisualElement e, float w, float h) { e.style.width = w; e.style.height = h; }

        public static void Show(VisualElement e, bool on) { e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None; }
    }
}
