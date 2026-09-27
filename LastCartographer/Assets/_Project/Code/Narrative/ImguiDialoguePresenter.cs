#nullable enable
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Placeholder dialogue box drawn with IMGUI until the atlas-and-ink UI (ENV-11) exists.
    /// Advance: J / Space / Enter / click, or gamepad South/West. Options: 1-3 keys, d-pad, or click.
    /// </summary>
    public sealed class ImguiDialoguePresenter : DialoguePresenterBase
    {
        [SerializeField] int _fontSize = 22;

        string? _speaker;
        string? _text;
        DialogueOption[]? _options;
        int _highlighted;
        bool _advance;
        int _chosen = -1;
        GUIStyle? _boxStyle, _textStyle, _nameStyle, _optionStyle;

        public bool IsShowing => _text != null || _options != null;

        public override YarnTask OnDialogueStartedAsync()
        {
            _text = null; _options = null; _speaker = null;
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            _text = null; _options = null; _speaker = null;
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            _speaker = line.CharacterName;
            _text = line.TextWithoutCharacterName.Text;
            _advance = false;
            await YarnTask.Yield();                       // swallow the press that started dialogue
            while (!_advance && !token.IsNextContentRequested)
            {
                if (AdvancePressed()) break;
                await YarnTask.Yield();
            }
            _text = null;
            _speaker = null;
        }

        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            _options = dialogueOptions;
            _highlighted = 0;
            _chosen = -1;
            await YarnTask.Yield();
            while (_chosen < 0 && !cancellationToken.IsNextContentRequested)
            {
                PollOptionInput();
                await YarnTask.Yield();
            }
            var picked = _chosen >= 0 && _chosen < dialogueOptions.Length ? dialogueOptions[_chosen] : null;
            _options = null;
            if (picked != null && !picked.IsAvailable) picked = null;
            return picked;
        }

        static bool AdvancePressed()
        {
            var k = Keyboard.current;
            if (k != null && (k.jKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                return true;
            var g = Gamepad.current;
            if (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame))
                return true;
            var m = Mouse.current;
            return m != null && m.leftButton.wasPressedThisFrame;
        }

        void PollOptionInput()
        {
            if (_options == null) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            int count = _options.Length;
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame && count > 0) _chosen = 0;
                if (k.digit2Key.wasPressedThisFrame && count > 1) _chosen = 1;
                if (k.digit3Key.wasPressedThisFrame && count > 2) _chosen = 2;
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) _highlighted = (_highlighted + count - 1) % count;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) _highlighted = (_highlighted + 1) % count;
                if (k.jKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame) _chosen = _highlighted;
            }
            if (g != null)
            {
                if (g.dpad.up.wasPressedThisFrame || g.leftStick.up.wasPressedThisFrame) _highlighted = (_highlighted + count - 1) % count;
                if (g.dpad.down.wasPressedThisFrame || g.leftStick.down.wasPressedThisFrame) _highlighted = (_highlighted + 1) % count;
                if (g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame) _chosen = _highlighted;
            }
        }

        void OnGUI()
        {
            if (!IsShowing) return;
            EnsureStyles();
            float w = Mathf.Min(Screen.width - 64f, 1100f);
            float h = 180f;
            var rect = new Rect((Screen.width - w) * 0.5f, Screen.height - h - 40f, w, h);
            GUI.Box(rect, GUIContent.none, _boxStyle);

            if (_text != null)
            {
                if (!string.IsNullOrEmpty(_speaker))
                    GUI.Label(new Rect(rect.x + 24f, rect.y + 14f, w - 48f, 30f), _speaker, _nameStyle);
                GUI.Label(new Rect(rect.x + 24f, rect.y + 50f, w - 48f, h - 60f), _text, _textStyle);
                GUI.Label(new Rect(rect.xMax - 140f, rect.yMax - 32f, 120f, 24f), "J / Space  ▸", _nameStyle);
            }
            else if (_options != null)
            {
                float y = rect.y + 16f;
                for (int i = 0; i < _options.Length; i++)
                {
                    var o = _options[i];
                    var label = (i == _highlighted ? "▸ " : "   ") + (i + 1) + ".  " + o.Line.TextWithoutCharacterName.Text;
                    var r = new Rect(rect.x + 24f, y, w - 48f, 34f);
                    if (GUI.Button(r, label, _optionStyle) && o.IsAvailable) _chosen = i;
                    y += 38f;
                }
            }
        }

        void EnsureStyles()
        {
            if (_boxStyle != null) return;
            var paper = new Texture2D(1, 1);
            paper.SetPixel(0, 0, new Color(0.96f, 0.93f, 0.85f, 0.96f));
            paper.Apply();
            _boxStyle = new GUIStyle(GUI.skin.box) { normal = { background = paper } };
            var ink = new Color(0.08f, 0.08f, 0.11f);
            _textStyle = new GUIStyle(GUI.skin.label) { fontSize = _fontSize, wordWrap = true, normal = { textColor = ink } };
            _nameStyle = new GUIStyle(GUI.skin.label) { fontSize = _fontSize - 4, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.20f, 0.27f, 0.45f) } };
            _optionStyle = new GUIStyle(GUI.skin.label) { fontSize = _fontSize - 2, normal = { textColor = ink }, hover = { textColor = new Color(0.20f, 0.27f, 0.45f) } };
        }
    }
}
