#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Yarn presenter that draws through the registered <see cref="IDialogueView"/>.
    /// Advance: J / Space / Enter, or gamepad South/West. Options: 1-3 keys, up/down, d-pad, or a click.
    /// Without a view (tests, tooling) lines pass straight through.
    /// </summary>
    public sealed class ViewDialoguePresenter : DialoguePresenterBase
    {
        DialogueOption[]? _options;
        int _highlighted;
        bool _advance;
        int _chosen = -1;
        IDialogueView? _bound;

        public bool IsShowingLine { get; private set; }
        public bool IsShowingOptions => _options != null;
        public int Highlighted => _highlighted;

        IDialogueView? View
        {
            get
            {
                var v = DialogueViews.Current;
                if (v != _bound)
                {
                    if (_bound != null) _bound.OptionClicked -= OnOptionClicked;
                    _bound = v;
                    if (_bound != null) _bound.OptionClicked += OnOptionClicked;
                }
                return v;
            }
        }

        void OnDisable() { if (_bound != null) { _bound.OptionClicked -= OnOptionClicked; _bound = null; } }

        /// <summary>Tests and tooling: the same as pressing the advance button.</summary>
        public void Advance() { _advance = true; }
        /// <summary>Tests and tooling: pick an option by index.</summary>
        public void Choose(int index) { _chosen = index; }

        void OnOptionClicked(int index) => _chosen = index;

        /// <summary>False once the service has stopped the conversation, so no page outlives it.</summary>
        static bool ServiceRunning => DialogueService.Instance == null || DialogueService.Instance.IsRunning;

        public override YarnTask OnDialogueStartedAsync()
        {
            View?.Clear();
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            IsShowingLine = false; _options = null;
            View?.Clear();
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            var view = View;
            IsShowingLine = true;
            _advance = false;
            if (view == null) { IsShowingLine = false; return; }
            view.ShowLine(line.CharacterName ?? "", line.TextWithoutCharacterName.Text);
            await YarnTask.Yield();                       // swallow the press that started dialogue
            while (!_advance && !token.IsNextContentRequested && ServiceRunning)
            {
                if (AdvancePressed()) break;
                await YarnTask.Yield();
            }
            IsShowingLine = false;
            view.Clear();
        }

        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            var view = View;
            _options = dialogueOptions;
            _highlighted = 0;
            _chosen = -1;
            if (view != null)
            {
                var texts = new string[dialogueOptions.Length];
                var avail = new bool[dialogueOptions.Length];
                for (int i = 0; i < dialogueOptions.Length; i++)
                {
                    texts[i] = dialogueOptions[i].Line.TextWithoutCharacterName.Text;
                    avail[i] = dialogueOptions[i].IsAvailable;
                }
                view.ShowOptions(texts, avail);
                view.Highlight(_highlighted);
            }
            await YarnTask.Yield();
            while (_chosen < 0 && !cancellationToken.IsNextContentRequested && ServiceRunning)
            {
                int before = _highlighted;
                PollOptionInput();
                if (_highlighted != before) view?.Highlight(_highlighted);
                await YarnTask.Yield();
            }
            var picked = _chosen >= 0 && _chosen < dialogueOptions.Length ? dialogueOptions[_chosen] : null;
            _options = null;
            view?.Clear();
            if (picked != null && !picked.IsAvailable) picked = null;
            return picked;
        }

        static bool AdvancePressed()
        {
            var k = Keyboard.current;
            if (k != null && (k.jKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                return true;
            var g = Gamepad.current;
            return g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame);
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
    }
}
