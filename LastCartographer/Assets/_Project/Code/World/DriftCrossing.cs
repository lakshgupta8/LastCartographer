using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Where she steps off the Hollow's far edge onto the first island going past (ENV-08, PRG-20): a trigger whose
    /// target is not a scene in the catalogue but whichever island drifts first in this world now, built by the island
    /// generator when the room manager asks for it. The island's own west exit leads back here. When nothing drifts,
    /// nothing comes, and the caption says so.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class DriftCrossing : MonoBehaviour
    {
        public string Spawn = "West";
        /// <summary>The island scene it last sent her to.</summary>
        public string Last { get; private set; }

        float _saidAt = -10f;

        void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<WrenController>() == null) return;
            var rm = RoomManager.Instance;
            if (rm == null || rm.IsTransitioning) return;
            var drifts = Islands.Drifting(GameState.World);
            if (drifts.Count == 0)
            {
                if (Time.unscaledTime - _saidAt > 3f) Captions.Show(Loc.T("caption.drift_empty", "Nothing drifts past tonight."), 2.5f);
                _saidAt = Time.unscaledTime;
                return;
            }
            Last = drifts[0].Scene;
            rm.Transition(Last, Spawn);
        }
    }
}
