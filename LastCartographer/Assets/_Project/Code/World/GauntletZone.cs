using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>A zone that sends her back: water, thorns, a vent, the white, the long grass.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GauntletZone : MonoBehaviour
    {
        public enum Kind { Hazard, Goal }
        public Kind Role;
        public Gauntlet Gauntlet;
        BoxCollider2D _box;
        readonly Collider2D[] _hits = new Collider2D[4];

        void Awake() { _box = GetComponent<BoxCollider2D>(); _box.isTrigger = true; }

        void FixedUpdate()
        {
            if (Gauntlet == null) return;
            var b = _box.bounds;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.Player, useTriggers = false };
            int n = Physics2D.OverlapBox(b.center, b.size, 0f, filter, _hits);
            for (int i = 0; i < n; i++)
            {
                var wren = _hits[i].GetComponentInParent<WrenController>();
                if (wren == null) continue;
                if (Role == Kind.Hazard) Gauntlet.Fall(wren);
                else Gauntlet.Finish();
                return;
            }
        }
    }
}
