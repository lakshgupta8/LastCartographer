using System.Collections;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Room A's opening: when Wren arrives from the Blank (prologue.isolde_entered set, prologue.woke_on_shore
    /// not yet), play the shore cutscene: the white thins, Sable stands over her, the conversation writes the flags.
    /// </summary>
    public sealed class ShoreWake : MonoBehaviour
    {
        [SerializeField] Cutscene _cutscene;
        [SerializeField] string _requiredFlag = "prologue.isolde_entered";
        [SerializeField] string _doneFlag = "prologue.woke_on_shore";

        public Cutscene Cutscene { get => _cutscene; set => _cutscene = value; }
        public bool Played { get; private set; }

        IEnumerator Start()
        {
            var w = GameState.World;
            if (_cutscene == null || !w.Is(_requiredFlag) || w.Is(_doneFlag)) yield break;
            // The room manager places Wren after the scene loads; wait for it.
            while (RoomManager.Instance != null && RoomManager.Instance.IsTransitioning) yield return null;
            yield return null;
            var wren = FindFirstObjectByType<WrenController>();
            if (wren != null) wren.Frozen = false;   // the cutscene's own freeze restores to this
            Played = true;
            _cutscene.Play();
        }
    }
}
