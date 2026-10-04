using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Under every built room (death-and-retry.md §1a): a gap with nothing under it is a fall, not a death. Wren going
    /// past the room's bottom loses a mask, never the last, and is put back on the last ground she stood on in this
    /// room (or, before she has stood anywhere, the nearest spawn). A Down exit, the shallows, a white patch and a
    /// gauntlet's own hazard all sit above the catch and take her first.
    /// </summary>
    public sealed class FallCatch : MonoBehaviour
    {
        public Vector2 LastSafe;
        public int Falls { get; private set; }
        public bool SafeKnown { get; private set; }
        public event Action<FallCatch> Fell;
        /// <summary>Any fall into the margin anywhere (AUD-11: its sound).</summary>
        public static event Action<FallCatch> AnyFell;

        static bool _captioned;
        WrenController _wren;
        readonly RaycastHit2D[] _hits = new RaycastHit2D[4];

        void FixedUpdate()
        {
            if (_wren == null) _wren = FindFirstObjectByType<WrenController>();
            if (_wren == null || !_wren.IsGrounded) return;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.Ground, useTriggers = false };
            int n = Physics2D.Raycast(_wren.Position + Vector2.up * 0.1f, Vector2.down, filter, _hits, 0.4f);
            if (n > 0) { LastSafe = _wren.Position; SafeKnown = true; }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var wren = other.GetComponentInParent<WrenController>();
            if (wren != null) Catch(wren);
        }

        /// <summary>Nothing under her: a mask (never the last) and back to the last ground.</summary>
        public void Catch(WrenController wren)
        {
            Falls++;
            var vitals = wren.GetComponent<WrenVitals>();
            if (vitals != null && vitals.Masks > 1) vitals.Damage(1);
            wren.Teleport(SafeKnown ? LastSafe : NearestSpawn(wren.Position));
            if (!_captioned) { _captioned = true; Captions.Show(Loc.T("caption.gauntlet_fall", "Back to solid ground."), 2.5f); }
            Fell?.Invoke(this);
            AnyFell?.Invoke(this);
        }

        Vector2 NearestSpawn(Vector2 from)
        {
            var room = GetComponentInParent<Room>();
            Transform best = null;
            float bestD = float.MaxValue;
            if (room != null)
                foreach (Transform t in room.transform)
                {
                    if (!t.name.StartsWith("Spawn_", StringComparison.Ordinal)) continue;
                    float d = ((Vector2)t.position - from).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = t; }
                }
            return best != null ? (Vector2)best.position : LastSafe;
        }
    }
}
