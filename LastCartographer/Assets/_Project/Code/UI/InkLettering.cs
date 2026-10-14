using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// Words lettered by her hand on the atlas's pages (ENV-11, atlas-map.md §5): a region's name as an old map letters
    /// it, in spaced capitals with each word's first a size larger, from the title serif's own glyphs. Every letter
    /// leans and sits a little off the line, by a hash of the words, so the hand is the same each time the page is
    /// drawn; the pen runs a little dry along a word and is dipped again at the next. A flourish or a short rule is
    /// drawn under them with the pen. The words themselves stay in their <see cref="Source"/> label, unshown, so the
    /// player's language (<see cref="InkTheme.Relabel"/>) and anything reading the text find them as before; the
    /// lettering follows its source whenever it changes. The glyphs keep the palette's colours exactly, so high
    /// contrast recolours them with the rest of the page (<see cref="InkTheme.Recolour"/>).
    /// </summary>
    public sealed class InkLettering : VisualElement
    {
        public enum Rule { None, Short, Flourish }

        /// <summary>One letter as the hand puts it down: its character, its size against the lettering's, lean (°), rise (px), ink.</summary>
        public struct Letter
        {
            public char Char;
            public bool Initial;
            public int Word;
            public float Scale, Lean, Rise, Ink;
        }

        /// <summary>A small capital against a word's first letter.</summary>
        public const float SmallCaps = 0.78f;
        /// <summary>How far a letter leans either way (°) and rises or drops off the line (em).</summary>
        public const float MaxLean = 2.4f, MaxRise = 0.07f;
        /// <summary>The ink left at the end of a dip (the pen is dipped again every <see cref="Dip"/> letters and at each word).</summary>
        public const float DryInk = 0.8f;
        public const int Dip = 7;

        public readonly Label Source;
        readonly VisualElement _row;
        readonly List<Label> _glyphs = new List<Label>();
        readonly float _size;
        readonly Color _color;
        readonly Rule _rule;
        string _built;
        Color _drawnIn;

        public IReadOnlyList<Label> Glyphs => _glyphs;
        /// <summary>The words as lettered (the source's text when it was last lettered).</summary>
        public string Text => _built ?? "";

        /// <summary>
        /// Letter <paramref name="source"/> (an <see cref="InkTheme"/> label) at <paramref name="size"/> px in a palette
        /// colour: the returned element takes the label's place in the layout and holds it, unshown.
        /// </summary>
        public static InkLettering Over(Label source, float size, Color color, Rule rule) => new InkLettering(source, size, color, rule);

        InkLettering(Label source, float size, Color color, Rule rule)
        {
            Source = source;
            _size = size;
            _color = color;
            _rule = rule;
            name = source.name + "-lettering";
            pickingMode = PickingMode.Ignore;
            style.flexShrink = 0;
            style.alignSelf = Align.FlexStart;
            style.paddingBottom = rule == Rule.None ? 0 : Mathf.Round(size * 0.3f);
            style.paddingRight = rule == Rule.Flourish ? Mathf.Round(size * 0.35f) : 0;   // the curl past the last letter

            source.style.position = Position.Absolute;
            source.style.left = 0; source.style.top = 0;
            source.style.visibility = Visibility.Hidden;
            source.pickingMode = PickingMode.Ignore;
            Add(source);

            _row = new VisualElement { name = "letters", pickingMode = PickingMode.Ignore };
            _row.style.flexDirection = FlexDirection.Row;
            _row.style.flexWrap = Wrap.Wrap;
            _row.style.alignItems = Align.FlexEnd;
            Add(_row);

            generateVisualContent += Draw;
            RegisterCallback<AttachToPanelEvent>(_ => Follow());
            // The source changes under it (the player's language, a turned page): follow it, and the palette.
            schedule.Execute(Follow).Every(200);
            Follow();
        }

        /// <summary>Letter the source again if its words have changed since, and redraw if the palette has.</summary>
        public void Follow()
        {
            if (Source.text != _built) Build();
            else if (_glyphs.Count > 0 && _glyphs[0].style.color.value != _drawnIn) MarkDirtyRepaint();
        }

        /// <summary>The hand's letters for some words: pure, the same every time for the same words.</summary>
        public static List<Letter> Plan(string text)
        {
            var letters = new List<Letter>();
            if (string.IsNullOrEmpty(text)) return letters;
            int word = 0, inWord = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsWhiteSpace(c))
                {
                    if (inWord > 0) { word++; inWord = 0; }
                    continue;
                }
                letters.Add(new Letter
                {
                    Char = char.ToUpperInvariant(c),
                    Initial = inWord == 0,
                    Word = word,
                    Scale = inWord == 0 ? 1f : SmallCaps,
                    Lean = Wobble(text, i, 0) * MaxLean,
                    Rise = Wobble(text, i, 1) * MaxRise,
                    Ink = 1f - (1f - DryInk) * (inWord % Dip) / (Dip - 1f),
                });
                inWord++;
            }
            return letters;
        }

        /// <summary>A hand's wander, -1..1, for a letter of some words.</summary>
        static float Wobble(string text, int i, int k)
        {
            int h = 23 + k * 7919 + i * 104729;
            foreach (var ch in text) h = unchecked(h * 31 + ch);
            h ^= h >> 13; h = unchecked(h * 1274126177); h ^= h >> 16;
            return ((h & 1023) / 1023f - 0.5f) * 2f;
        }

        void Build()
        {
            _built = Source.text ?? "";
            _row.Clear();
            _glyphs.Clear();
            VisualElement wordBox = null;
            int word = -1;
            foreach (var l in Plan(_built))
            {
                if (l.Word != word)
                {
                    word = l.Word;
                    wordBox = new VisualElement { name = "word", pickingMode = PickingMode.Ignore };
                    wordBox.style.flexDirection = FlexDirection.Row;
                    wordBox.style.alignItems = Align.FlexEnd;
                    wordBox.style.marginRight = Mathf.Round(_size * 0.42f);
                    _row.Add(wordBox);
                }
                var g = InkTheme.TitleText("letter", l.Char.ToString(), Mathf.Round(_size * l.Scale), _color);
                g.style.whiteSpace = WhiteSpace.NoWrap;
                g.style.marginRight = Mathf.Round(_size * 0.1f);
                g.style.paddingLeft = 0; g.style.paddingRight = 0;
                g.style.rotate = new Rotate(new Angle(l.Lean, AngleUnit.Degree));
                g.style.translate = new Translate(0, -l.Rise * _size);
                g.style.opacity = l.Ink;
                wordBox.Add(g);
                _glyphs.Add(g);
            }
            if (wordBox != null) wordBox.style.marginRight = 0;
            MarkDirtyRepaint();
        }

        // ---- the pen under the words ---------------------------------------------------------------------------------

        void Draw(MeshGenerationContext ctx)
        {
            if (_rule == Rule.None || _glyphs.Count == 0) return;
            var box = _row.layout;
            if (box.width <= 0f) return;
            _drawnIn = _glyphs[0].style.color.value;   // the palette as it is now: high contrast moves it
            var p = ctx.painter2D;
            p.fillColor = _drawnIn;
            p.strokeColor = _drawnIn;
            float x0 = box.xMin + 1f, x1 = box.xMax - Mathf.Round(_size * 0.1f), y = box.yMax + _size * 0.12f;
            if (_rule == Rule.Short)
            {
                float mid = (x0 + x1) * 0.5f, half = Mathf.Min((x1 - x0) * 0.3f, _size * 2.5f);
                Taper(p, new Vector2(mid - half, y), new Vector2(mid + half, y), 1.3f);
                return;
            }
            // The flourish: a swelling stroke under the words, lifting at its end into a curl.
            var a = new Vector2(x0, y + 1f);
            var b = new Vector2(x1, y - _size * 0.04f);
            Taper(p, a, b, Mathf.Max(1.6f, _size * 0.075f));
            float r = Mathf.Max(2.5f, _size * 0.12f);
            p.lineWidth = Mathf.Max(1f, _size * 0.035f);
            p.lineCap = LineCap.Round;
            p.BeginPath();
            p.Arc(b + new Vector2(r * 0.6f, -r), r, Angle.Degrees(110f), Angle.Degrees(-160f), ArcDirection.CounterClockwise);
            p.Stroke();
        }

        /// <summary>A pen stroke from a to b, fine at both ends and <paramref name="width"/> at its swell, a third along.</summary>
        static void Taper(Painter2D p, Vector2 a, Vector2 b, float width)
        {
            var d = b - a;
            var n = new Vector2(-d.y, d.x).normalized * width;
            var swell = a + d * 0.35f;
            p.BeginPath();
            p.MoveTo(a);
            p.BezierCurveTo(swell - n, swell + d * 0.3f - n * 0.8f, b);
            p.BezierCurveTo(swell + d * 0.3f + n * 0.2f, swell + n * 0.25f, a);
            p.ClosePath();
            p.Fill();
        }
    }
}
