using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A survey spot (bible 10, PRG-10). Stand in the trigger and hold Survey; after HoldSeconds the
    /// vantage is inked into the atlas (WorldState.SurveyedVantages). Ink refills slowly here.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class VantagePoint : MonoBehaviour
    {
        [SerializeField] string _vantageId;
        [SerializeField] string _displayName = "Vantage";
        [SerializeField] float _holdSeconds = 1.2f;
        [SerializeField] float _inkRefillSeconds = 10f;

        public string VantageId { get => _vantageId; set => _vantageId = value; }
        public string DisplayName => _displayName;
        public bool IsSurveyed => GameState.World.IsSurveyed(_vantageId);
        /// <summary>0..1 while holding; 0 when idle or done.</summary>
        public float Progress { get; private set; }
        public event Action<VantagePoint> Surveyed;

        WrenController _wren;
        Inkwell _ink;
        float _refillT;

        void Reset() { GetComponent<Collider2D>().isTrigger = true; _vantageId = gameObject.scene.name + "/" + name; }

        void OnTriggerEnter2D(Collider2D other)
        {
            var c = other.GetComponentInParent<WrenController>();
            if (c == null) return;
            _wren = c;
            _ink = c.GetComponent<Inkwell>();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<WrenController>() != _wren) return;
            _wren = null;
            _ink = null;
            Progress = 0f;
        }

        void FixedUpdate()
        {
            if (_wren == null) return;
            float dt = Time.fixedDeltaTime;

            // Slow refill while standing at a vantage (combat doc 4).
            if (_ink != null && _wren.IsGrounded)
            {
                _refillT += dt;
                if (_refillT >= _inkRefillSeconds) { _refillT = 0f; _ink.Add(1); }
            }

            if (IsSurveyed) { Progress = 0f; return; }
            bool holding = _wren.Input != null && _wren.Input.SurveyHeld && _wren.IsGrounded;
            if (!holding) { Progress = 0f; return; }
            Progress = Mathf.Clamp01(Progress + dt / Mathf.Max(0.01f, _holdSeconds));
            if (Progress >= 1f)
            {
                Progress = 0f;
                if (GameState.World.MarkSurveyed(_vantageId)) Surveyed?.Invoke(this);
            }
        }
    }
}
