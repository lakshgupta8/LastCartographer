using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>
    /// The single source of truth for narrative flags. Every quest, fade stage, anchor, and
    /// companion state is a key here. Serialised to JSON by the save system.
    /// See docs/story/story-bible.md section 10 (story-to-mechanic bindings).
    /// </summary>
    [Serializable]
    public sealed class WorldState
    {
        public Dictionary<string, int> Flags = new Dictionary<string, int>();
        public HashSet<string> AnchoredPlaces = new HashSet<string>();
        public HashSet<string> SurveyedLandmarks = new HashSet<string>();
        public List<string> BoundMemories = new List<string>();

        public event Action<string, int> FlagChanged;

        public int Get(string key, int fallback = 0) => Flags.TryGetValue(key, out var v) ? v : fallback;
        public bool Is(string key) => Get(key) != 0;

        public void Set(string key, int value)
        {
            if (Flags.TryGetValue(key, out var old) && old == value) return;
            Flags[key] = value;
            FlagChanged?.Invoke(key, value);
        }

        public void Set(string key, bool value) => Set(key, value ? 1 : 0);
    }
}
