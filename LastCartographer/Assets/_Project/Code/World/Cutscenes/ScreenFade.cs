using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The full-screen paper fade, as game state: cutscene clips and the Blank's edge write it, the UI
    /// draws it. Level 0 is clear, 1 is solid.
    /// </summary>
    public static class ScreenFade
    {
        public static readonly Color Paper = new Color(0.96f, 0.93f, 0.85f);
        public static readonly Color White = new Color(0.97f, 0.96f, 0.93f);

        public static float Level { get; private set; }
        public static Color Color { get; private set; } = Paper;
        public static event Action Changed;

        public static void Set(float level, Color color)
        {
            level = Mathf.Clamp01(level);
            if (Mathf.Approximately(level, Level) && color == Color) return;
            Level = level;
            Color = color;
            Changed?.Invoke();
        }

        public static void Set(float level) => Set(level, Color);
        public static void Clear() => Set(0f, Paper);
    }
}
