using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// The UI's drawings (ENV-11, docs/design/ui-art.md): the paper the pages are made of, the masks as feathers, the
    /// Inkwell as a bottle, the boss bar as a brush stroke, and the glyphs the pages point with, all rendered by
    /// tools/ui/ui_art.py with the same ink as the world and packed under Art/UI/Resources/UI/. Loaded through
    /// Resources so the atlas looks the same in a build, in the editor and in a test, with no setup pass; a drawing
    /// that is not there leaves the greybox look (a rule, a diamond, a glyph in text), so a hand pass can replace
    /// them one at a time. The paper frames stand down under high-contrast ink (DES-14): the flat opaque pages come
    /// back, and the glyphs stay.
    /// </summary>
    public static class InkArt
    {
        public const string Folder = "UI/";
        public const string ManifestName = "ui";
        public const string FontsFolder = "UI/Fonts/";
        public const string TitleFontName = "IMFellEnglish-Regular";
        public const string BodyFontName = "AlegreyaSans-Regular";
        public const string BodyItalicName = "AlegreyaSans-Italic";
        public const string BodyBoldName = "AlegreyaSans-Bold";

        /// <summary>Every drawing the pages ask for, by the name ui_art.py gives it.</summary>
        public static readonly string[] Required =
        {
            "UI_Page", "UI_Strip", "UI_Spread", "UI_Portrait",
            "UI_MaskFull", "UI_MaskEmpty", "UI_Inkwell", "UI_InkwellFill", "UI_Lantern",
            "UI_BossBar", "UI_BossFill", "UI_Tick", "UI_Marker", "UI_Seed", "UI_Rose",
            "UI_VantageDrawn", "UI_VantageBlank", "UI_VantageErased", "UI_Lamp", "UI_Desk",
            "UI_Charter_Surveyor", "UI_Charter_Warden", "UI_Charter_Drifter", "UI_Charter_Ferryman", "UI_Charter_Unwriter", "UI_Charter_Remnant",
            "UI_Instrument_CompassDart", "UI_Instrument_PlumbWeight", "UI_Instrument_SightingLens", "UI_Instrument_FieldLantern",
            "UI_Instrument_TetherHook", "UI_Instrument_IrisTincture", "UI_Instrument_WaxSeal",
        };

        [System.Serializable] public sealed class Slices { public int l, t, r, b; public bool Any => l + t + r + b > 0; }
        [System.Serializable] public sealed class Fill { public float bottom, top; public bool Any => top > bottom; }
        /// <summary>One drawing's manifest entry. JsonUtility fills an absent <see cref="slices"/> or <see cref="fill"/> with zeros rather than null, so ask <c>Any</c>.</summary>
        [System.Serializable] public sealed class Piece { public string name; public int w, h; public Slices slices; public Fill fill; }
        [System.Serializable] sealed class Manifest { public Piece[] pieces; }

        /// <summary>The kinds of paper a panel can be made of.</summary>
        public enum Paper { Page, Strip, Spread, Portrait }

        public const string PaperClass = "ink-paper";
        /// <summary>The left half of a spread: it carries the flat divider when the spine is not drawn.</summary>
        public const string SpreadLeftClass = "ink-spread-left";

        static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        static Dictionary<string, Piece> _pieces;
        static Font _title, _body, _bodyItalic, _bodyBold;
        static bool _fontsRead;

        /// <summary>A drawing by name, or null when it is not packed; asked once.</summary>
        public static Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_textures.TryGetValue(name, out var tex)) return tex;
            tex = Resources.Load<Texture2D>(Folder + name);
            _textures[name] = tex;
            return tex;
        }

        /// <summary>What ui.json says about a drawing: its size at 1x, where it slices, where a fill runs.</summary>
        public static Piece PieceOf(string name)
        {
            if (_pieces == null)
            {
                _pieces = new Dictionary<string, Piece>();
                var text = Resources.Load<TextAsset>(Folder + ManifestName);
                if (text != null)
                {
                    var m = JsonUtility.FromJson<Manifest>(text.text);
                    if (m != null && m.pieces != null) foreach (var p in m.pieces) if (p != null && !string.IsNullOrEmpty(p.name)) _pieces[p.name] = p;
                }
            }
            return _pieces.TryGetValue(name ?? "", out var piece) ? piece : null;
        }

        /// <summary>The drawings are packed.</summary>
        public static bool Available => Tex("UI_Page") != null;
        /// <summary>The paper frames are in use: packed, and the player has not asked for high-contrast ink.</summary>
        public static bool Drawn => Available && !Options.HighContrast;

        public static Font TitleFont { get { ReadFonts(); return _title; } }
        public static Font BodyFont { get { ReadFonts(); return _body; } }
        public static Font BodyItalic { get { ReadFonts(); return _bodyItalic; } }
        public static Font BodyBold { get { ReadFonts(); return _bodyBold; } }

        static void ReadFonts()
        {
            if (_fontsRead) return;
            _fontsRead = true;
            _title = Resources.Load<Font>(FontsFolder + TitleFontName);
            _body = Resources.Load<Font>(FontsFolder + BodyFontName);
            _bodyItalic = Resources.Load<Font>(FontsFolder + BodyItalicName);
            _bodyBold = Resources.Load<Font>(FontsFolder + BodyBoldName);
        }

        /// <summary>Drop every cached lookup (a test that swaps the Resources, a domain without reload).</summary>
        public static void Forget()
        {
            _textures.Clear();
            _pieces = null;
            _fontsRead = false;
            _title = _body = _bodyItalic = _bodyBold = null;
        }

        public static Texture2D CharterIcon(CharterKind kind) => Tex("UI_Charter_" + kind);
        public static Texture2D InstrumentIcon(InstrumentKind kind) => kind == InstrumentKind.None ? null : Tex("UI_Instrument_" + kind);

        // ---------------------------------------------------------------- glyphs

        /// <summary>
        /// An element showing one drawing at a height, as wide as the drawing's shape; null texture gives an empty
        /// element of the height, to fill later with <see cref="SetGlyph"/>.
        /// </summary>
        public static VisualElement Glyph(string name, Texture2D tex, float height)
        {
            var e = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            e.AddToClassList("ink-glyph");
            e.style.flexShrink = 0;
            e.style.height = height;
            e.style.width = height;
            e.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            e.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            SetGlyph(e, tex);
            return e;
        }

        public static VisualElement Glyph(string name, string tex, float height) => Glyph(name, Tex(tex), height);

        /// <summary>Change what a glyph shows (the drawing's aspect sets its width); null clears it.</summary>
        public static void SetGlyph(VisualElement e, Texture2D tex)
        {
            if (tex == null) { e.style.backgroundImage = new StyleBackground(StyleKeyword.None); return; }
            if (e.style.backgroundImage.value.texture == tex) return;
            e.style.backgroundImage = new StyleBackground(tex);
            float h = e.style.height.value.value;
            if (h > 0f && tex.height > 0) e.style.width = h * tex.width / tex.height;
        }

        // ---------------------------------------------------------------- paper

        /// <summary>Make an element of a kind of paper: drawn when the drawings are in use, flat otherwise, and again whenever that changes.</summary>
        public static void Paperize(VisualElement e, Paper kind)
        {
            foreach (Paper p in System.Enum.GetValues(typeof(Paper))) e.RemoveFromClassList(ClassOf(p));
            e.AddToClassList(PaperClass);
            e.AddToClassList(ClassOf(kind));
            Skin(e, kind, Drawn);
        }

        /// <summary>Redraw every piece of paper under <paramref name="root"/> for the current palette (DES-14).</summary>
        public static void Reskin(VisualElement root)
        {
            if (root == null) return;
            bool drawn = Drawn;
            root.Query<VisualElement>(className: PaperClass).ForEach(e => Skin(e, KindOf(e), drawn));
        }

        public static string ClassOf(Paper kind) => "ink-paper-" + kind.ToString().ToLowerInvariant();

        public static Paper KindOf(VisualElement e)
        {
            foreach (Paper p in System.Enum.GetValues(typeof(Paper))) if (e.ClassListContains(ClassOf(p))) return p;
            return Paper.Page;
        }

        static string TextureOf(Paper kind) => kind switch
        {
            Paper.Strip => "UI_Strip",
            Paper.Spread => "UI_Spread",
            Paper.Portrait => "UI_Portrait",
            _ => "UI_Page",
        };

        static void Skin(VisualElement e, Paper kind, bool drawn)
        {
            var tex = drawn ? Tex(TextureOf(kind)) : null;
            var st = e.style;
            if (tex != null)
            {
                var piece = PieceOf(TextureOf(kind));
                var s = piece != null && piece.slices != null && piece.slices.Any ? piece.slices : new Slices();
                st.backgroundImage = new StyleBackground(tex);
                st.unitySliceLeft = s.l; st.unitySliceTop = s.t; st.unitySliceRight = s.r; st.unitySliceBottom = s.b;
                st.backgroundColor = Color.clear;
                InkTheme.SetBorder(e, Color.clear, 0f);
                InkTheme.SetRadius(e, 0f);
                switch (kind)
                {
                    case Paper.Strip: InkTheme.SetPadding(e, 14f, 56f); break;
                    case Paper.Spread: InkTheme.SetPadding(e, 56f, 66f); break;
                    case Paper.Portrait: InkTheme.SetPadding(e, 0f, 0f); break;
                    default: InkTheme.SetPadding(e, 40f, 52f); break;
                }
            }
            else
            {
                st.backgroundImage = new StyleBackground(StyleKeyword.None);
                st.unitySliceLeft = 0; st.unitySliceTop = 0; st.unitySliceRight = 0; st.unitySliceBottom = 0;
                switch (kind)
                {
                    case Paper.Strip:
                        st.backgroundColor = InkTheme.Paper;
                        InkTheme.SetBorder(e, InkTheme.InkFaint, 1f);
                        InkTheme.SetRadius(e, 6f);
                        InkTheme.SetPadding(e, 8f, 18f);
                        break;
                    case Paper.Portrait:
                        st.backgroundColor = InkTheme.PaperDark;
                        InkTheme.SetBorder(e, InkTheme.InkFaint, 2f);
                        InkTheme.SetRadius(e, 6f);
                        InkTheme.SetPadding(e, 0f, 0f);
                        break;
                    default:
                        st.backgroundColor = InkTheme.Paper;
                        InkTheme.SetBorder(e, InkTheme.Ink, 2f);
                        InkTheme.SetRadius(e, 6f);
                        InkTheme.SetPadding(e, kind == Paper.Spread ? 24f : 18f, kind == Paper.Spread ? 32f : 24f);
                        break;
                }
            }
            if (kind == Paper.Spread)
            {
                // The halves meet at the spine when it is drawn, and at a faint rule when it is not.
                e.Query<VisualElement>(className: SpreadLeftClass).ForEach(half =>
                {
                    half.style.borderRightWidth = tex != null ? 0f : 1f;
                    half.style.borderRightColor = InkTheme.InkFaint;
                });
            }
        }
    }
}
