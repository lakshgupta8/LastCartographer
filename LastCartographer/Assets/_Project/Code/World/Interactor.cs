using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>On Wren. Tracks the nearest overlapping Interactable and fires it on an up-press.</summary>
    [RequireComponent(typeof(WrenController), typeof(BoxCollider2D))]
    public sealed class Interactor : MonoBehaviour
    {
        [SerializeField] LayerMask _interactMask = ~0;
        [SerializeField] float _upThreshold = 0.6f;

        WrenController _ctrl;
        BoxCollider2D _box;
        readonly Collider2D[] _overlaps = new Collider2D[8];
        bool _upWasHeld;

        public Interactable Current { get; private set; }
        public event Action<Interactable> CurrentChanged;
        public event Action<Interactable> Interacted;

        void Awake()
        {
            _ctrl = GetComponent<WrenController>();
            _box = GetComponent<BoxCollider2D>();
        }

        void FixedUpdate()
        {
            var next = FindNearest();
            if (next != Current)
            {
                Current = next;
                CurrentChanged?.Invoke(Current);
            }

            bool upHeld = !_ctrl.Frozen && _ctrl.Input != null && _ctrl.Input.Move.y > _upThreshold && Mathf.Abs(_ctrl.Input.Move.x) < 0.5f;
            bool upPressed = upHeld && !_upWasHeld;
            _upWasHeld = upHeld;

            if (upPressed && Current != null && _ctrl.IsGrounded && Current.CanInteract(this))
            {
                Current.Interact(this);
                Interacted?.Invoke(Current);
            }
        }

        Interactable FindNearest()
        {
            var scale = transform.lossyScale;
            var center = _ctrl.Position + new Vector2(_box.offset.x * scale.x, _box.offset.y * scale.y);
            var size = new Vector2(_box.size.x * Mathf.Abs(scale.x), _box.size.y * Mathf.Abs(scale.y));
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = _interactMask, useTriggers = true };
            int n = Physics2D.OverlapBox(center, size, 0f, filter, _overlaps);
            Interactable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var it = _overlaps[i].GetComponentInParent<Interactable>();
                if (it == null || !it.CanInteract(this)) continue;
                float d = ((Vector2)it.transform.position - _ctrl.Position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = it; }
            }
            return best;
        }
    }
}
