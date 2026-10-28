using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A platform that exists only inside Wren's lantern-radius (the Road That Stops, bible 4.6): solid and drawn when she
    /// carries Clarity and stands within reach of it, an outline otherwise. The reach is her Clarity meter's radius
    /// (PRG-18), which narrows as the meter runs down; <see cref="Radius"/> stands in only where she has no meter.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class LanternPlatform : MonoBehaviour
    {
        public float Radius = 3.5f;
        public bool IsDrawn { get; private set; }
        /// <summary>A lantern platform drawn as she comes within reach (true) or let go as she leaves it (false): it sounds as it changes (AUD-25).</summary>
        public static event Action<LanternPlatform, bool> Changed;
        bool _woke;
        /// <summary>How far from her it is drawn: her lantern-radius.</summary>
        public float Reach => _clarity != null && _clarity.Radius > 0f ? _clarity.Radius : Radius;
        BoxCollider2D _box;
        Renderer _visual;
        WrenController _wren;
        ClarityMeter _clarity;

        Vector3 _drawnScale;
        float _minX, _maxX;

        void Awake()
        {
            _box = GetComponent<BoxCollider2D>();
            _visual = GetComponentInChildren<Renderer>();
            if (_visual != null) _drawnScale = _visual.transform.localScale;
            // Its edges from the transform: a disabled collider has no bounds to ask.
            float half = _box.size.x * Mathf.Abs(transform.lossyScale.x) * 0.5f;
            float centre = transform.position.x + _box.offset.x * transform.lossyScale.x;
            _minX = centre - half;
            _maxX = centre + half;
            Set(false);
            _woke = true;
        }

        void FixedUpdate()
        {
            if (_wren == null) { _wren = FindFirstObjectByType<WrenController>(); _clarity = _wren != null ? _wren.GetComponent<ClarityMeter>() : null; }
            bool drawn = false;
            if (_wren != null && _wren.Abilities != null && _wren.Abilities.Has(Ability.Clarity))
            {
                float dx = Mathf.Max(0f, Mathf.Max(_minX - _wren.Position.x, _wren.Position.x - _maxX));
                drawn = dx <= Reach + 0.001f;
            }
            if (drawn != IsDrawn) Set(drawn);
        }

        void Set(bool drawn)
        {
            IsDrawn = drawn;
            _box.enabled = drawn;
            if (_visual != null) _visual.transform.localScale = new Vector3(_drawnScale.x, drawn ? _drawnScale.y : 0.05f, drawn ? _drawnScale.z : 0.05f);   // outline
            if (_woke) Changed?.Invoke(this, drawn);
        }
    }
}
