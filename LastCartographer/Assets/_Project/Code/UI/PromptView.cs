using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// Tutorial prompts and world captions on a paper strip above the dialogue page: &lt;&lt;tutorial name&gt;&gt;
    /// from Yarn maps to a control hint here (the prologue's only tutorial text, GDD 10), and
    /// <see cref="Captions"/> shows anything the world wants said ("Bound: ...").
    /// </summary>
    public sealed class PromptView : MonoBehaviour
    {
        [SerializeField] float _tutorialSeconds = 6f;

        public string Text => _label != null ? _label.text : _pending;
        public bool IsShowing => _left > 0f;

        Label _label;
        bool _built;
        string _pending;
        float _left;
        DialogueService _bound;

        void OnEnable() { Captions.Shown += Show; }
        void OnDisable() { Captions.Shown -= Show; if (_bound != null) { _bound.Tutorial -= OnTutorial; _bound = null; } }

        public static string TutorialText(string name) => name switch
        {
            "survey" => "Hold Q at the marker. Let the pen find the line.",
            "bind" => "Hold E to bind. It costs ink.",
            "seal" => "Sealed. A wax seal marks where you would come back to.",
            "strike" => "J strikes. In the air, hold down and strike to bounce off what you hit.",
            "dash" => "Shift dashes. Wingbeat is a memory of the sky.",
            _ => name,
        };

        void OnTutorial(string name) => Show(TutorialText(name), _tutorialSeconds);

        public void Show(string text, float seconds)
        {
            _pending = text;
            _left = seconds;
            if (_built) { _label.text = text; InkTheme.Show(_label, true); }
        }

        void Update()
        {
            if (_bound == null && DialogueService.Instance != null)
            {
                _bound = DialogueService.Instance;
                _bound.Tutorial += OnTutorial;
            }
            if (!_built && Build() && _pending != null && _left > 0f) { _label.text = _pending; InkTheme.Show(_label, true); }
            if (_left > 0f)
            {
                _left -= Time.unscaledDeltaTime;
                if (_left <= 0f && _built) InkTheme.Show(_label, false);
            }
        }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.Caption == null) return false;
            _label = InkTheme.Text("prompt", "", 20, InkTheme.Ink, FontStyle.Italic);
            _label.style.position = Position.Absolute;
            _label.style.left = new Length(50, LengthUnit.Percent);
            _label.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            _label.style.bottom = new Length(24, LengthUnit.Percent);
            _label.style.backgroundColor = InkTheme.Paper;
            InkTheme.SetPadding(_label, 8f, 18f);
            InkTheme.SetRadius(_label, 6f);
            InkTheme.SetBorder(_label, InkTheme.InkFaint, 1f);
            InkTheme.Show(_label, false);
            ui.Caption.Add(_label);
            _built = true;
            return true;
        }
    }
}
