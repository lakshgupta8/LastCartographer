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
        /// <summary>A line, with its hashtags (Yarn's metadata, e.g. <c>face:grave</c>) for the page to read.</summary>
        void ShowLine(string speaker, string text, string[] tags = null);
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
