using System;
using System.Collections.Generic;
using OWSBG.Core;
using OWSBG.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>One speaker's portrait strip (CHR-13): rest, talk, and the two again in the Remnant's grey.</summary>
    [Serializable]
    public sealed class PortraitSheet
    {
        public string Speaker;
        public Texture2D Sheet;
    }

    /// <summary>
    /// The dialogue page: a paper panel along the bottom with the speaker's portrait on the left (CHR-13), the
    /// speaker in wash blue, the line in ink, and options as numbered rows. Registers as the presenter's view.
    /// A portrait opens and shuts its beak while a line is new, then rests; a speaker who is a Remnant (or is
    /// fading, in the room they are met in) has the grey drawing laid over the drawn one as far as their wash.
    /// </summary>
    public sealed class DialogueView : MonoBehaviour, IDialogueView
    {
        /// <summary>The portrait's side, px (the strip's cells are 256).</summary>
        public const float PortraitSize = 168f;
        /// <summary>Beak frames a second while the line is new.</summary>
        public const float TalkFps = 8f;

        [SerializeField] List<PortraitSheet> _portraits = new List<PortraitSheet>();

        VisualElement _panel, _options, _portrait, _column;
        Image _face, _grey;
        Label _speaker, _text, _prompt;
        PortraitSheet _shownPortrait;
        float _talkUntil, _wash;
        int _frame;
        bool _built;
        string[] _pendingTexts; bool[] _pendingAvail; int _pendingHighlight = -1;
        string _pendingSpeaker, _pendingLine;

        public bool IsVisible => _built && _panel.style.display == DisplayStyle.Flex;
        public bool IsShowingOptions => _built && _options.style.display == DisplayStyle.Flex;
        public int OptionCount => _built ? _options.childCount : 0;
        /// <summary>Whether an offered option can be taken (a failed &lt;&lt;if&gt;&gt; shows it dimmed, not hidden).</summary>
        public bool IsOptionAvailable(int index) => _pendingAvail != null && index >= 0 && index < _pendingAvail.Length && _pendingAvail[index];
        public string SpeakerText => _built ? _speaker.text : _pendingSpeaker;
        public string LineText => _built ? _text.text : _pendingLine;
        public string OptionText(int index) => _pendingTexts != null && index >= 0 && index < _pendingTexts.Length ? _pendingTexts[index] : "";
        public event Action<int> OptionClicked;

        public IReadOnlyList<PortraitSheet> PortraitSheets => _portraits;
        /// <summary>The speaker whose portrait shows, or null when the page shows none.</summary>
        public string PortraitSpeaker => _built && _shownPortrait != null && _portrait.style.display == DisplayStyle.Flex ? _shownPortrait.Speaker : null;
        /// <summary>The frame showing: <see cref="Portraits.Rest"/> or <see cref="Portraits.Talk"/>.</summary>
        public int PortraitFrame => _frame;
        /// <summary>How far the grey drawing lies over the drawn one (0 drawn, 1 a Remnant).</summary>
        public float PortraitGrey => _wash;
        public bool IsTalking => PortraitSpeaker != null && Time.unscaledTime < _talkUntil;

        /// <summary>Setup: the portraits the page can show, one strip per speaker.</summary>
        public void ConfigurePortraits(IEnumerable<PortraitSheet> sheets)
        {
            _portraits = new List<PortraitSheet>(sheets);
        }

        public PortraitSheet FindPortrait(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return null;
            foreach (var p in _portraits) if (p != null && p.Speaker == speaker && p.Sheet != null) return p;
            return null;
        }

        /// <summary>How long a line is "being said": a beat per few letters, never under half a second or over three and a half.</summary>
        public static float TalkSeconds(string text) => Mathf.Clamp((text ?? "").Length * 0.04f, 0.5f, 3.5f);

        /// <summary>
        /// The speaker's grey, from the bird in the room when there is one (the talker first, then anyone of that name
        /// with an <see cref="NpcInk"/>): its wash, all the way for a Remnant. Otherwise the speaker's rest state.
        /// </summary>
        public static float WashOf(string speaker)
        {
            NpcInk ink = null;
            var talking = NpcTalker.Talking;
            if (talking != null && Portraits.IsSpeaker(talking.name, speaker)) ink = talking.GetComponentInChildren<NpcInk>();
            if (ink == null)
                foreach (var i in FindObjectsByType<NpcInk>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (Portraits.IsSpeaker(i.name, speaker) || (i.transform.parent != null && Portraits.IsSpeaker(i.transform.parent.name, speaker))) { ink = i; break; }
            if (ink != null) return ink.State == NpcInkState.Remnant ? 1f : Mathf.Clamp01(ink.Wash);
            return Portraits.RemnantAtRest.Contains(speaker) ? 1f : 0f;
        }

        void OnEnable() { DialogueViews.Current = this; }
        void OnDisable() { if (DialogueViews.Current == (IDialogueView)this) DialogueViews.Current = null; }

        void Update()
        {
            if (_built && IsVisible && DialogueService.Instance != null && !DialogueService.Instance.IsRunning) { Clear(); return; }
            if (_built) { AnimatePortrait(); return; }
            if (!Build()) return;
            // Replay anything shown before the document was ready.
            if (_pendingTexts != null) { ShowOptions(_pendingTexts, _pendingAvail); if (_pendingHighlight >= 0) Highlight(_pendingHighlight); }
            else if (_pendingLine != null) ShowLine(_pendingSpeaker, _pendingLine);
        }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.Dialogue == null) return false;

            _panel = InkTheme.Panel("dialogue");
            _panel.style.position = Position.Absolute;
            _panel.style.bottom = 48;
            _panel.style.left = new Length(50, LengthUnit.Percent);
            _panel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            _panel.style.width = 1100;
            _panel.style.maxWidth = new Length(92, LengthUnit.Percent);
            _panel.style.minHeight = 150;

            _speaker = InkTheme.Text("speaker", "", 20, InkTheme.Wash, FontStyle.Bold);
            _speaker.style.marginBottom = 8;
            _text = InkTheme.Text("line", "", 24, InkTheme.Ink);
            _prompt = InkTheme.Say("prompt", "dialogue.continue", "J / Space  ▸", 16, InkTheme.Dim);
            _prompt.style.alignSelf = Align.FlexEnd;
            _prompt.style.marginTop = 10;
            _options = new VisualElement { name = "options" };
            InkTheme.Show(_options, false);

            // The portrait: a framed square of darker paper, the drawing and its grey stacked in it.
            _portrait = new VisualElement { name = "portrait" };
            _portrait.style.width = PortraitSize;
            _portrait.style.height = PortraitSize;
            _portrait.style.flexShrink = 0;
            _portrait.style.marginRight = 18;
            _portrait.style.alignSelf = Align.FlexStart;
            _portrait.style.backgroundColor = InkTheme.PaperDark;
            _portrait.style.overflow = Overflow.Hidden;
            InkTheme.SetBorder(_portrait, InkTheme.InkFaint, 2f);
            InkTheme.SetRadius(_portrait, 6f);
            _face = PortraitImage("face");
            _grey = PortraitImage("grey");
            _portrait.Add(_face); _portrait.Add(_grey);
            InkTheme.Show(_portrait, false);

            _column = new VisualElement { name = "column" };
            _column.style.flexGrow = 1;
            _column.style.flexShrink = 1;
            _column.Add(_speaker); _column.Add(_text); _column.Add(_options); _column.Add(_prompt);
            var body = InkTheme.Row("body");
            body.style.alignItems = Align.FlexStart;
            body.Add(_portrait); body.Add(_column);
            _panel.Add(body);
            InkTheme.Show(_panel, false);
            ui.Dialogue.Add(_panel);
            _built = true;
            return true;
        }

        static Image PortraitImage(string name)
        {
            var image = new Image { name = name, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.position = Position.Absolute;
            image.style.left = 0; image.style.top = 0; image.style.right = 0; image.style.bottom = 0;
            return image;
        }

        void ShowPortrait(string speaker, string text)
        {
            _shownPortrait = FindPortrait(speaker);
            InkTheme.Show(_portrait, _shownPortrait != null);
            if (_shownPortrait == null) { _wash = 0f; _talkUntil = 0f; return; }
            _face.image = _shownPortrait.Sheet;
            _grey.image = _shownPortrait.Sheet;
            _wash = WashOf(speaker);
            _grey.style.opacity = _wash;
            InkTheme.Show(_grey, _wash > 0f);
            _talkUntil = Time.unscaledTime + TalkSeconds(text);
            SetFrame(Portraits.Talk);
        }

        void AnimatePortrait()
        {
            if (_shownPortrait == null || !IsVisible) return;
            float left = _talkUntil - Time.unscaledTime;
            SetFrame(left > 0f && Mathf.FloorToInt(left * TalkFps) % 2 == 0 ? Portraits.Talk : Portraits.Rest);
        }

        void SetFrame(int frame)
        {
            _frame = frame;
            int n = Portraits.Frames.Length;
            _face.uv = new Rect(frame / (float)n, 0f, 1f / n, 1f);
            _grey.uv = new Rect((frame + Portraits.RemnantOffset) / (float)n, 0f, 1f / n, 1f);
        }

        public void ShowLine(string speaker, string text)
        {
            _pendingSpeaker = speaker; _pendingLine = text; _pendingTexts = null;
            if (!_built) return;
            ShowPortrait(speaker, text);
            _speaker.text = speaker ?? "";
            InkTheme.Show(_speaker, !string.IsNullOrEmpty(speaker));
            _text.text = text ?? "";
            InkTheme.Show(_text, true);
            InkTheme.Show(_options, false);
            InkTheme.Show(_prompt, true);
            InkTheme.Show(_panel, true);
        }

        public void ShowOptions(string[] texts, bool[] available)
        {
            _pendingTexts = texts; _pendingAvail = available; _pendingLine = null;
            if (!_built) return;
            _options.Clear();
            for (int i = 0; i < texts.Length; i++)
            {
                int index = i;
                var row = InkTheme.Row("option-" + i);
                row.pickingMode = PickingMode.Position;
                row.AddToClassList("option");
                InkTheme.SetPadding(row, 6f, 10f);
                InkTheme.SetRadius(row, 4f);
                var marker = InkTheme.Text("marker", "▸", 22, InkTheme.Wash);
                marker.style.width = 26;
                marker.style.visibility = Visibility.Hidden;
                var label = InkTheme.Text("text", (i + 1) + ".  " + texts[i], 22, available[i] ? InkTheme.Ink : InkTheme.Dim);
                row.Add(marker); row.Add(label);
                if (available[i]) row.RegisterCallback<ClickEvent>(_ => OptionClicked?.Invoke(index));
                _options.Add(row);
            }
            InkTheme.Show(_speaker, false);
            InkTheme.Show(_text, false);
            InkTheme.Show(_prompt, false);
            InkTheme.Show(_portrait, false);   // her choice: Wren has no portrait of her own
            _shownPortrait = null;
            InkTheme.Show(_options, true);
            InkTheme.Show(_panel, true);
        }

        public void Highlight(int index)
        {
            _pendingHighlight = index;
            if (!_built) return;
            for (int i = 0; i < _options.childCount; i++)
            {
                var row = _options[i];
                bool on = i == index;
                row.EnableInClassList("highlighted", on);
                row.Q("marker").style.visibility = on ? Visibility.Visible : Visibility.Hidden;
                row.style.backgroundColor = on ? InkTheme.PaperDark : new Color(0f, 0f, 0f, 0f);
            }
        }

        public void Clear()
        {
            _pendingTexts = null; _pendingLine = null; _pendingHighlight = -1;
            if (!_built) return;
            InkTheme.Show(_panel, false);
            InkTheme.Show(_portrait, false);
            _shownPortrait = null;
            _options.Clear();
        }
    }
}
