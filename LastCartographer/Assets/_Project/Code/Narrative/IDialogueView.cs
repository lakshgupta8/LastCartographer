using System;

namespace OWSBG.Narrative
{
    /// <summary>
    /// What the dialogue presenter draws through. The UI assembly implements it (UI Toolkit) and
    /// registers in <see cref="DialogueViews"/>; Narrative never references UI, so this stays here.
    /// </summary>
    public interface IDialogueView
    {
        bool IsVisible { get; }
        void ShowLine(string speaker, string text);
        void ShowOptions(string[] texts, bool[] available);
        void Highlight(int index);
        void Clear();
        /// <summary>Raised when the player clicks or taps an option.</summary>
        event Action<int> OptionClicked;
    }

    public static class DialogueViews
    {
        public static IDialogueView Current { get; set; }
    }
}
