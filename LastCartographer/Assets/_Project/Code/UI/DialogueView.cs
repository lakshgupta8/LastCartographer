using System;
using OWSBG.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// The dialogue page: a paper panel along the bottom with the speaker in wash blue, the line in
    /// ink, and options as numbered rows. Registers as the presenter's view.
    /// </summary>
    public sealed class DialogueView : MonoBehaviour, IDialogueView
    {
        VisualElement _panel, _options;
        Label _speaker, _text, _prompt;
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

        void OnEnable() { DialogueViews.Current = this; }
        void OnDisable() { if (DialogueViews.Current == (IDialogueView)this) DialogueViews.Current = null; }

        void Update()
        {
            if (_built && IsVisible && DialogueService.Instance != null && !DialogueService.Instance.IsRunning) { Clear(); return; }
            if (_built || !Build()) return;
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

            _panel.Add(_speaker); _panel.Add(_text); _panel.Add(_options); _panel.Add(_prompt);
            InkTheme.Show(_panel, false);
            ui.Dialogue.Add(_panel);
            _built = true;
            return true;
        }

        public void ShowLine(string speaker, string text)
        {
            _pendingSpeaker = speaker; _pendingLine = text; _pendingTexts = null;
            if (!_built) return;
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
            _options.Clear();
        }
    }
}
