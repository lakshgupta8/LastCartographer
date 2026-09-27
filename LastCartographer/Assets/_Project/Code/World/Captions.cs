using System;

namespace OWSBG.World
{
    /// <summary>World-side captions ("Bound: the first time she saw you"); the UI's prompt view draws them.</summary>
    public static class Captions
    {
        public static event Action<string, float> Shown;

        public static void Show(string text, float seconds = 3.5f) => Shown?.Invoke(text, seconds);
    }
}
