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
            public int version = 4;
            public List<string> flagKeys = new List<string>();
            public List<int> flagValues = new List<int>();
            public List<string> anchoredPlaces = new List<string>();
            public List<string> surveyedVantages = new List<string>();
            public List<string> erasedVantages = new List<string>();
            public List<string> waypoints = new List<string>();
            public List<string> boundMemories = new List<string>();
            public List<string> droppedMemories = new List<string>();
            public string dropRoom;
            public float dropX, dropY;
            public List<string> numberKeys = new List<string>();
            public List<float> numberValues = new List<float>();
            public List<string> stringKeys = new List<string>();
            public List<string> stringValues = new List<string>();
            public string respawnRoom;
            public string respawnSpawn;
            public string waxSealRoom;
            public float waxSealX, waxSealY;
            public string charter = "Surveyor";
            public List<string> ownedCharters = new List<string>();
            public List<string> ownedInstruments = new List<string>();
            public List<string> slotKinds = new List<string>();
            public List<int> slotUses = new List<int>();
            public bool fourthSlot;
            public int selectedSlot;

            public static SaveData From(WorldState w)
            {
                var d = new SaveData();
                foreach (var kv in w.Flags) { d.flagKeys.Add(kv.Key); d.flagValues.Add(kv.Value); }
                d.anchoredPlaces.AddRange(w.AnchoredPlaces);
                d.surveyedVantages.AddRange(w.SurveyedVantages);
                d.erasedVantages.AddRange(w.ErasedVantages);
                d.waypoints.AddRange(w.Waypoints);
                d.boundMemories.AddRange(w.BoundMemories);
                d.droppedMemories.AddRange(w.DroppedMemories);
                d.dropRoom = w.DropRoom;
                d.dropX = w.DropX; d.dropY = w.DropY;
                foreach (var kv in w.Numbers) { d.numberKeys.Add(kv.Key); d.numberValues.Add(kv.Value); }
                foreach (var kv in w.Strings) { d.stringKeys.Add(kv.Key); d.stringValues.Add(kv.Value); }
                d.respawnRoom = w.RespawnRoom;
                d.respawnSpawn = w.RespawnSpawn;
                d.waxSealRoom = w.WaxSealRoom;
                d.waxSealX = w.WaxSealX; d.waxSealY = w.WaxSealY;
                var e = w.Equipment;
                d.charter = e.Charter.ToString();
                foreach (var c in e.OwnedCharters) d.ownedCharters.Add(c.ToString());
                foreach (var i in e.OwnedInstruments) d.ownedInstruments.Add(i.ToString());
                foreach (var s in e.Slots) { d.slotKinds.Add(s.Kind.ToString()); d.slotUses.Add(s.UsesLeft); }
                d.fourthSlot = e.FourthSlotUnlocked;
                d.selectedSlot = e.SelectedSlot;
                return d;
            }

            public WorldState ToWorldState()
            {
                var w = new WorldState();
                for (int i = 0; i < flagKeys.Count && i < flagValues.Count; i++) w.Flags[flagKeys[i]] = flagValues[i];
                foreach (var p in anchoredPlaces) w.AnchoredPlaces.Add(p);
                foreach (var v in surveyedVantages) w.SurveyedVantages.Add(v);
                foreach (var v in erasedVantages) w.ErasedVantages.Add(v);
                foreach (var p in waypoints) w.Waypoints.Add(p);
                w.BoundMemories.AddRange(boundMemories);
                w.DroppedMemories.AddRange(droppedMemories);
                w.DropRoom = dropRoom;
                w.DropX = dropX; w.DropY = dropY;
                for (int i = 0; i < numberKeys.Count && i < numberValues.Count; i++) w.Numbers[numberKeys[i]] = numberValues[i];
                for (int i = 0; i < stringKeys.Count && i < stringValues.Count; i++) w.Strings[stringKeys[i]] = stringValues[i];
                w.RespawnRoom = respawnRoom;
                w.RespawnSpawn = respawnSpawn;
                w.WaxSealRoom = waxSealRoom;
                w.WaxSealX = waxSealX; w.WaxSealY = waxSealY;
                var e = w.Equipment;
                if (Enum.TryParse(charter, out CharterKind ck)) e.Charter = ck;
                e.OwnedCharters.Clear();
                foreach (var c in ownedCharters) if (Enum.TryParse(c, out CharterKind k)) e.OwnedCharters.Add(k);
                e.OwnedCharters.Add(CharterKind.Surveyor);
                if (!e.OwnedCharters.Contains(e.Charter)) e.Charter = CharterKind.Surveyor;
                foreach (var i in ownedInstruments) if (Enum.TryParse(i, out InstrumentKind k)) e.OwnedInstruments.Add(k);
                for (int i = 0; i < e.Slots.Length && i < slotKinds.Count; i++)
                {
                    if (!Enum.TryParse(slotKinds[i], out InstrumentKind k)) continue;
                    e.Slots[i] = new InstrumentSlot { Kind = k, UsesLeft = i < slotUses.Count ? slotUses[i] : 0 };
                }
                e.FourthSlotUnlocked = fourthSlot;
                e.SelectedSlot = selectedSlot;
                return w;
            }
        }
    }
}
