using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The rest point (GDD 6, PRG-11): restores masks, sets the respawn point, saves. Charter and
    /// Instrument swapping hang off the Rested event once those systems exist.
    /// </summary>
    public sealed class DraftingDesk : Interactable
    {
        [SerializeField] string _spawnName = "Desk";
        [SerializeField] int _saveSlot = 0;

        public static event Action<DraftingDesk> Rested;

        public override void Interact(Interactor who)
        {
            var vitals = who.GetComponent<WrenVitals>();
            if (vitals != null) vitals.RestoreAll();

            var w = GameState.World;
            w.RespawnRoom = RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : gameObject.scene.name;
            w.RespawnSpawn = _spawnName;
            GameState.Save(_saveSlot);
            Rested?.Invoke(this);
        }
    }
}
