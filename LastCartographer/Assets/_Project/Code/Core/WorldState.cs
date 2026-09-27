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
        public HashSet<string> SurveyedVantages = new HashSet<string>();
        /// <summary>Drawn vantages a Cantor's bell has wiped from the page; re-surveying clears them (DES-02).</summary>
        public HashSet<string> ErasedVantages = new HashSet<string>();
        /// <summary>Desks stood at and lamps lit: the atlas's travel points (Atlas).</summary>
        public HashSet<string> Waypoints = new HashSet<string>();
        public List<string> BoundMemories = new List<string>();
        /// <summary>Yarn "$" variables that are not booleans (booleans live in Flags).</summary>
        public Dictionary<string, float> Numbers = new Dictionary<string, float>();
        public Dictionary<string, string> Strings = new Dictionary<string, string>();
        public string RespawnRoom;
        public string RespawnSpawn;
        /// <summary>Wax seal: a one-shot respawn point at a world position, consumed on death.</summary>
        public string WaxSealRoom;
        public float WaxSealX, WaxSealY;
        public bool HasWaxSeal => !string.IsNullOrEmpty(WaxSealRoom);
        /// <summary>Charter and Instruments (combat doc 5 and 6).</summary>
        public Equipment Equipment = new Equipment();

        public event Action<string, int> FlagChanged;
        public event Action<string> VantageSurveyed;
        public event Action<string> VantageErased;

        /// <summary>On the page now: drawn and not erased.</summary>
        public bool IsSurveyed(string vantageId) => SurveyedVantages.Contains(vantageId) && !ErasedVantages.Contains(vantageId);
        /// <summary>Drawn at some point, erased or not (story gates that should not undo themselves).</summary>
        public bool IsEverSurveyed(string vantageId) => SurveyedVantages.Contains(vantageId);
        public bool IsErased(string vantageId) => ErasedVantages.Contains(vantageId);

        /// <summary>Draw a vantage: true when it was blank or erased.</summary>
        public bool MarkSurveyed(string vantageId)
        {
            if (string.IsNullOrEmpty(vantageId)) return false;
            bool added = SurveyedVantages.Add(vantageId);
            bool redrawn = ErasedVantages.Remove(vantageId);
            if (!added && !redrawn) return false;
            VantageSurveyed?.Invoke(vantageId);
            return true;
        }

        /// <summary>Wipe a drawn vantage from the page (a Cantor's bell). True when it was on the page.</summary>
        public bool MarkErased(string vantageId)
        {
            if (string.IsNullOrEmpty(vantageId) || !SurveyedVantages.Contains(vantageId) || !ErasedVantages.Add(vantageId)) return false;
            VantageErased?.Invoke(vantageId);
            return true;
        }

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
