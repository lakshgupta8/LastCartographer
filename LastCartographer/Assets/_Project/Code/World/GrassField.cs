using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Long grass (ENV-07, bible 4.5: "endless wind-bent grass"). A field is a row of tufts, each the kit's drawing on a
    /// root at its feet; every frame the field leans them with the wind (from the west, so the tips go east, in gusts
    /// that travel across the row) and parts them round Wren as she walks through: a tuft within reach bends away from
    /// her, and springs back when she has passed. One behaviour per field, not per tuft.
    /// </summary>
    public sealed class GrassField : MonoBehaviour
    {
        [Tooltip("How far the wind leans a tuft, in degrees (the tips go east).")]
        public float windLean = 9f;
        [Tooltip("How fast the gusts travel.")]
        public float windSpeed = 1.3f;
        [Tooltip("Reach of her passing, in units.")]
        public float pushRadius = 1.1f;
        [Tooltip("How far a tuft bends away from her at its closest, in degrees.")]
        public float pushLean = 42f;
        [Tooltip("How quickly a tuft follows its lean.")]
        public float follow = 9f;

        public List<Transform> Tufts = new List<Transform>();
        public List<float> Phases = new List<float>();

        float[] _lean;
        WrenController _wren;

        /// <summary>The tuft's lean now, in degrees: positive leans the tip west, negative east.</summary>
        public float LeanOf(int i) => _lean != null && i >= 0 && i < _lean.Length ? _lean[i] : 0f;
        public int Count => Tufts.Count;

        /// <summary>The nearest tuft to an x, or -1.</summary>
        public int Nearest(float x)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < Tufts.Count; i++)
            {
                if (Tufts[i] == null) continue;
                float d = Mathf.Abs(Tufts[i].position.x - x);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        void OnEnable()
        {
            if (_lean == null || _lean.Length != Tufts.Count) _lean = new float[Tufts.Count];
        }

        void Update()
        {
            if (_lean == null || _lean.Length != Tufts.Count) _lean = new float[Tufts.Count];
            if (_wren == null) _wren = FindFirstObjectByType<WrenController>();
            float t = Time.time;
            float dt = Time.deltaTime;
            float k = 1f - Mathf.Exp(-follow * dt);
            Vector2 w = _wren != null ? _wren.Position : new Vector2(float.NaN, 0f);
            for (int i = 0; i < Tufts.Count; i++)
            {
                var tuft = Tufts[i];
                if (tuft == null) continue;
                float phase = i < Phases.Count ? Phases[i] : i * 0.7f;
                var p = tuft.position;
                // The wind: a gust travelling east along the row, a slower swell under it; the tips go east (a negative turn).
                float gust = Mathf.Sin(t * windSpeed * 2.1f - p.x * 0.45f + phase) * 0.6f + Mathf.Sin(t * windSpeed * 0.7f + phase * 1.7f) * 0.4f;
                float target = -windLean * (0.55f + 0.45f * gust);
                // Her passing: a tuft within reach bends away from her, most when she stands in it.
                if (!float.IsNaN(w.x) && Mathf.Abs(w.y - p.y) < 1.6f)
                {
                    float dx = w.x - p.x;
                    float d = Mathf.Abs(dx);
                    if (d < pushRadius) target += (dx >= 0f ? 1f : -1f) * pushLean * (1f - d / pushRadius);
                }
                _lean[i] = Mathf.Lerp(_lean[i], target, k);
                tuft.localRotation = Quaternion.Euler(0f, 0f, _lean[i]);
            }
        }
    }
}
