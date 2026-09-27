using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// The one WorldState for the running game, plus JSON save and load (PRG-09).
    /// Everything narrative and world-shaped reads and writes through here.
    /// </summary>
    public static class GameState
    {
        public static WorldState World { get; private set; } = new WorldState();
        public static event Action Loaded;

        public static string SaveDirectory => Path.Combine(Application.persistentDataPath, "saves");
        public static string SlotPath(int slot) => Path.Combine(SaveDirectory, "slot" + slot + ".json");

        public static void NewGame()
        {
            World = new WorldState();
            Loaded?.Invoke();
        }

        public static string ToJson(WorldState state) => JsonUtility.ToJson(SaveData.From(state), true);

        public static WorldState FromJson(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            return data != null ? data.ToWorldState() : new WorldState();
        }

        public static void Save(int slot = 0)
        {
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(SlotPath(slot), ToJson(World));
        }

        public static bool Load(int slot = 0)
        {
            var path = SlotPath(slot);
            if (!File.Exists(path)) return false;
            World = FromJson(File.ReadAllText(path));
            Loaded?.Invoke();
            return true;
        }

        public static bool HasSave(int slot = 0) => File.Exists(SlotPath(slot));

        /// <summary>JsonUtility cannot serialise dictionaries or hash sets, so the file uses flat lists.</summary>
        [Serializable]
        sealed class SaveData
        {
            public int version = 1;
            public List<string> flagKeys = new List<string>();
            public List<int> flagValues = new List<int>();
            public List<string> anchoredPlaces = new List<string>();
            public List<string> surveyedVantages = new List<string>();
            public List<string> boundMemories = new List<string>();
            public List<string> numberKeys = new List<string>();
            public List<float> numberValues = new List<float>();
            public List<string> stringKeys = new List<string>();
            public List<string> stringValues = new List<string>();
            public string respawnRoom;
            public string respawnSpawn;

            public static SaveData From(WorldState w)
            {
                var d = new SaveData();
                foreach (var kv in w.Flags) { d.flagKeys.Add(kv.Key); d.flagValues.Add(kv.Value); }
                d.anchoredPlaces.AddRange(w.AnchoredPlaces);
                d.surveyedVantages.AddRange(w.SurveyedVantages);
                d.boundMemories.AddRange(w.BoundMemories);
                foreach (var kv in w.Numbers) { d.numberKeys.Add(kv.Key); d.numberValues.Add(kv.Value); }
                foreach (var kv in w.Strings) { d.stringKeys.Add(kv.Key); d.stringValues.Add(kv.Value); }
                d.respawnRoom = w.RespawnRoom;
                d.respawnSpawn = w.RespawnSpawn;
                return d;
            }

            public WorldState ToWorldState()
            {
                var w = new WorldState();
                for (int i = 0; i < flagKeys.Count && i < flagValues.Count; i++) w.Flags[flagKeys[i]] = flagValues[i];
                foreach (var p in anchoredPlaces) w.AnchoredPlaces.Add(p);
                foreach (var v in surveyedVantages) w.SurveyedVantages.Add(v);
                w.BoundMemories.AddRange(boundMemories);
                for (int i = 0; i < numberKeys.Count && i < numberValues.Count; i++) w.Numbers[numberKeys[i]] = numberValues[i];
                for (int i = 0; i < stringKeys.Count && i < stringValues.Count; i++) w.Strings[stringKeys[i]] = stringValues[i];
                w.RespawnRoom = respawnRoom;
                w.RespawnSpawn = respawnSpawn;
                return w;
            }
        }
    }
}
