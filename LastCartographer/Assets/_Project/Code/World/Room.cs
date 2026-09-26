using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// One room of the interconnected map: an additive scene. Holds the camera bounds and named
    /// spawn points (children named "Spawn_<name>"). Engine doc, 2.5D recipe step 6.
    /// </summary>
    public sealed class Room : MonoBehaviour
    {
        public string RoomId;
        public Collider2D CameraBounds;

        public static Room Current { get; private set; }

        void OnEnable() { Current = this; }
        void OnDisable() { if (Current == this) Current = null; }

        public Transform FindSpawn(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = transform.Find("Spawn_" + name);
            if (t != null) return t;
            foreach (var child in GetComponentsInChildren<Transform>(true))
                if (child.name == "Spawn_" + name) return child;
            return null;
        }
    }
}
