using OWSBG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// The paper-and-ink palette and element helpers for the atlas UI (art direction 5, ENV-11).
    /// Everything is built in code with explicit fonts so it works with or without a theme asset;
    /// the hand-drawn frames and the ink font replace these when the UI art lands.
    /// Two palettes: the warm one, and high-contrast ink (DES-14) where every text colour reads at 7:1 or better on the
    /// paper and the faint rules are drawn firmly. The colours follow <see cref="Options.HighContrast"/>, and
    /// <see cref="Recolour"/> moves what is already drawn from one palette to the other.
    /// </summary>
    public static class InkTheme
    {
        public enum Swatch { Paper, PaperDark, Ink, InkFaint, Wash, Dim, Ochre }

        public static readonly Color[] Warm =
        {
            new Color(0.96f, 0.93f, 0.85f, 0.97f),  // Paper
            new Color(0.88f, 0.84f, 0.74f, 1f),     // PaperDark
            new Color(0.08f, 0.08f, 0.11f, 1f),     // Ink
            new Color(0.08f, 0.08f, 0.11f, 0.25f),  // InkFaint
            new Color(0.20f, 0.27f, 0.45f, 1f),     // Wash
            new Color(0.35f, 0.35f, 0.40f, 1f),     // Dim
            new Color(0.86f, 0.70f, 0.30f, 1f),     // Ochre
        };

        public static readonly Color[] HighContrast =
        {
            new Color(1f, 1f, 1f, 1f),              // Paper: opaque white, no world showing through
            new Color(0.80f, 0.80f, 0.80f, 1f),     // PaperDark
            new Color(0f, 0f, 0f, 1f),              // Ink
            new Color(0f, 0f, 0f, 0.65f),           // InkFaint: rules you can see
            new Color(0.02f, 0.16f, 0.58f, 1f),     // Wash
            new Color(0.16f, 0.16f, 0.18f, 1f),     // Dim
            new Color(0.52f, 0.29f, 0.00f, 1f),     // Ochre
        };

        public static Color[] Palette => Options.HighContrast ? HighContrast : Warm;
        public static Color Of(Swatch s) => Palette[(int)s];

        public static Color Paper => Of(Swatch.Paper);
        public static Color PaperDark => Of(Swatch.PaperDark);
        public static Color Ink => Of(Swatch.Ink);
        public static Color InkFaint => Of(Swatch.InkFaint);
        public static Color Wash => Of(Swatch.Wash);
        public static Color Dim => Of(Swatch.Dim);
        public static Color Ochre => Of(Swatch.Ochre);

        /// <summary>
        /// Move every inline colour under <paramref name="root"/> that is one of the palette's to the same swatch in the
        /// other palette. Colours that aren't swatches (a region's tint, a boss's bar) are left as they are.
        /// </summary>
        public static void Recolour(VisualElement root, bool high)
        {
            if (root == null) return;
            var from = high ? Warm : HighContrast;
            var to = high ? HighContrast : Warm;
            root.Query<VisualElement>().ForEach(e =>
            {
                var st = e.style;
                if (Map(st.color, from, to, out var c)) st.color = c;
                if (Map(st.backgroundColor, from, to, out c)) st.backgroundColor = c;
                if (Map(st.borderTopColor, from, to, out c)) st.borderTopColor = c;
                if (Map(st.borderBottomColor, from, to, out c)) st.borderBottomColor = c;
                if (Map(st.borderLeftColor, from, to, out c)) st.borderLeftColor = c;
                if (Map(st.borderRightColor, from, to, out c)) st.borderRightColor = c;
            });
        }

        static bool Map(StyleColor style, Color[] from, Color[] to, out Color mapped)
        {
            mapped = default;
            if (style.keyword != StyleKeyword.Undefined) return false;
            for (int i = 0; i < from.Length; i++)
                if (Same(style.value, from[i])) { mapped = to[i]; return true; }
            return false;
        }

        static bool Same(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f && Mathf.Abs(a.b - b.b) < 0.002f && Mathf.Abs(a.a - b.a) < 0.002f;

        /// <summary>WCAG contrast ratio of a text colour over a ground (both opaque), 1 to 21.</summary>
        public static float ContrastRatio(Color text, Color ground)
        {
            float la = Luminance(text), lb = Luminance(ground);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        static float Luminance(Color c)
        {
            static float Lin(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * Lin(c.r) + 0.7152f * Lin(c.g) + 0.0722f * Lin(c.b);
        }

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
