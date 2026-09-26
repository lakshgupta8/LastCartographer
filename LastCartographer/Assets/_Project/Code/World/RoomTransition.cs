using UnityEngine;

namespace OWSBG.World
{
    /// <summary>A trigger at a room edge. Wren touching it loads the neighbour.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class RoomTransition : MonoBehaviour
    {
        public string TargetScene;
        public string TargetSpawn;

        void Reset() { GetComponent<Collider2D>().isTrigger = true; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<WrenController>() == null) return;
            RoomManager.Instance?.Transition(TargetScene, TargetSpawn);
        }
    }
}
