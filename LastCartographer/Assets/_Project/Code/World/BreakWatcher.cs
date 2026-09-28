using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Watches the rooms for a sequence break (<see cref="SequenceBreaks"/>): on every room Wren arrives in, and
    /// once on waking, it asks whether she could only have got there by a soft gap, and if so says one line and
    /// lets the rule write the flag and the scrap. It wakes with the game and lives across rooms.
    /// </summary>
    public sealed class BreakWatcher : MonoBehaviour
    {
        public static BreakWatcher Instance { get; private set; }
        /// <summary>The last zone noticed, for the tests.</summary>
        public string LastNoticed { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("~BreakWatcher");
            DontDestroyOnLoad(go);
            go.AddComponent<BreakWatcher>();
        }

        void Awake() { if (Instance == null) Instance = this; }
        void OnEnable() { RoomManager.Transitioned += OnTransitioned; }
        void OnDisable() { RoomManager.Transitioned -= OnTransitioned; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            if (RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom)) OnRoom(RoomManager.Instance.CurrentRoom);
        }

        void OnTransitioned(string room, float _) => OnRoom(room);

        /// <summary>Wren is in this room now. Returns the zone if a break was noticed for the first time.</summary>
        public string OnRoom(string room)
        {
            string zone = SequenceBreaks.Notice(GameState.World, room);
            if (zone == null) return null;
            LastNoticed = zone;
            Captions.Show(Loc.T("break.noticed", "Nobody comes this way on foot. She notes it in the margin, and keeps the scrap."), 4f);
            Debug.Log("[OWSBG] a sequence break, noticed: " + zone);
            return zone;
        }
    }
}
