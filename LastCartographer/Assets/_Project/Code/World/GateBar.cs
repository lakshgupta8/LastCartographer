using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The solid thing a shut gate stands in the way (<see cref="RoomTransition.Bar"/>): ground she can't pass, and
    /// when she presses on it, the gate's one line. Wren's body is kinematic and reports no collisions, so the bar
    /// watches for her itself: once each time she comes against it, again only after she has stepped away.
    /// </summary>
    public sealed class GateBar : MonoBehaviour
    {
        [SerializeField] RoomTransition _gate;

        WrenController _wren;
        Collider2D _col, _wrenCol;
        bool _pressed;

        public RoomTransition Gate { get => _gate; set => _gate = value; }
        public bool IsPressed => _pressed;

        void Awake() { _col = GetComponent<Collider2D>(); }
        void OnDisable() { _pressed = false; }

        void FixedUpdate()
        {
            if (_gate == null) _gate = GetComponentInParent<RoomTransition>();
            if (_wren == null) { _wren = FindFirstObjectByType<WrenController>(); _wrenCol = _wren != null ? _wren.GetComponentInChildren<Collider2D>() : null; }
            if (_gate == null || _wren == null || _wrenCol == null || _col == null) return;
            var reach = _wrenCol.bounds;
            reach.Expand(0.3f);
            bool near = reach.Intersects(_col.bounds);
            if (!near) { _pressed = false; return; }
            if (_pressed) return;
            _pressed = true;
            _gate.Bump(_wren);
        }
    }
}
