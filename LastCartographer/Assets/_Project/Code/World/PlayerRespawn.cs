using System;
using System.Collections;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Death and retry (GDD 6, PRG-17 first pass): on death, freeze, wait, return to the last
    /// drafting desk (or the current room's Start), restore masks. Smudge recovery of bound
    /// memories comes later; ink is already emptied by WrenVitals.
    /// </summary>
    public sealed class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] float _delaySeconds = 0.9f;

        WrenController _wren;
        WrenVitals _vitals;
        bool _respawning;

        public bool IsRespawning => _respawning;
        public event Action Respawned;
        public event Action WaxSealUsed;
        /// <summary>The same, for anything that listens without a hand on the component (AUD-11: the sounds).</summary>
        public static event Action AnyRespawned, AnyWaxSealUsed;

        void Start() { Bind(); }

        void Bind()
        {
            if (_vitals != null) return;
            _wren = FindFirstObjectByType<WrenController>();
            _vitals = _wren != null ? _wren.GetComponent<WrenVitals>() : null;
            if (_vitals != null) _vitals.Died += OnDied;
        }

        void OnDestroy() { if (_vitals != null) _vitals.Died -= OnDied; }

        void OnDied()
        {
            if (_respawning) return;
            StartCoroutine(Respawn());
        }

        IEnumerator Respawn()
        {
            _respawning = true;
            _wren.Frozen = true;
            yield return new WaitForSeconds(_delaySeconds);

            var w = GameState.World;
            var current = RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : null;
            var room = string.IsNullOrEmpty(w.RespawnRoom) ? current : w.RespawnRoom;
            var spawn = string.IsNullOrEmpty(w.RespawnSpawn) ? "Start" : w.RespawnSpawn;

            if (w.HasWaxSeal)
            {
                // Wax seal (combat doc 6): a one-shot respawn at a world position, consumed now.
                var sealRoom = w.WaxSealRoom;
                var sealPos = new Vector2(w.WaxSealX, w.WaxSealY);
                w.WaxSealRoom = null;
                if (RoomManager.Instance != null && sealRoom != current && !string.IsNullOrEmpty(current))
                {
                    RoomManager.Instance.Transition(sealRoom, spawn);
                    while (RoomManager.Instance.IsTransitioning) yield return null;
                }
                _wren.Teleport(sealPos);
                WaxSealUsed?.Invoke();
                AnyWaxSealUsed?.Invoke();
            }
            else if (RoomManager.Instance != null && !string.IsNullOrEmpty(room) && room != current)
            {
                RoomManager.Instance.Transition(room, spawn);
                while (RoomManager.Instance.IsTransitioning) yield return null;
            }
            else
            {
                var r = Room.Current;
                var sp = r != null ? r.FindSpawn(spawn) ?? r.FindSpawn("Start") : null;
                if (sp != null) _wren.Teleport(sp.position);
            }

            _vitals.RestoreAll();
            _wren.Frozen = false;
            _respawning = false;
            Respawned?.Invoke();
            AnyRespawned?.Invoke();
        }
    }
}
