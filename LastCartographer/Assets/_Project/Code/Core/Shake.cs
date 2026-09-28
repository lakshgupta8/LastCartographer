using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// Camera shake (combat doc: a hard shake only on boss slams), scaled by the player's <see cref="Options.Shake"/>
    /// and off at 0. The main camera is nudged after Cinemachine has placed it, a decaying offset on unscaled time;
    /// overlapping requests keep the stronger. Nothing is left behind when it ends.
    /// </summary>
    public static class Shake
    {
        static Runner _runner;
        static float _amplitude, _seconds, _left;

        /// <summary>The offset applied this frame (world units); zero when still.</summary>
        public static Vector3 Offset { get; private set; }
        public static bool Active => _left > 0f;
        /// <summary>The strength of the current shake after the player's scale.</summary>
        public static float Amplitude => _amplitude;

        public static void Request(float amplitude, float seconds)
        {
            amplitude *= Options.Shake;
            if (amplitude <= 0f || seconds <= 0f || !Application.isPlaying) return;
            if (Active && _amplitude * (_left / _seconds) >= amplitude) return;
            _amplitude = amplitude; _seconds = seconds; _left = seconds;
            if (_runner == null)
            {
                var go = new GameObject("~Shake") { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<Runner>();
            }
        }

        public static void Stop() { _left = 0f; _amplitude = 0f; }

        /// <summary>Runs after everything else in LateUpdate, so after the Cinemachine brain has written the camera.</summary>
        [DefaultExecutionOrder(32000)]
        sealed class Runner : MonoBehaviour
        {
            Transform _cam;
            Vector3 _applied, _written;

            void LateUpdate()
            {
                var cam = Camera.main != null ? Camera.main.transform : null;
                if (cam != _cam) { _cam = cam; _applied = Vector3.zero; }
                if (_cam == null) return;
                // If nothing moved the camera since we did, take our last nudge off before the next.
                var basePos = _cam.position == _written ? _cam.position - _applied : _cam.position;
                Vector3 offset = Vector3.zero;
                if (_left > 0f)
                {
                    _left -= Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(_left / Mathf.Max(0.0001f, _seconds));
                    float t = Time.unscaledTime * 38f;
                    offset = new Vector3(Mathf.PerlinNoise(t, 0.3f) * 2f - 1f, Mathf.PerlinNoise(0.7f, t) * 2f - 1f, 0f) * (_amplitude * k);
                }
                Offset = offset;
                _cam.position = basePos + offset;
                _applied = offset;
                _written = _cam.position;
            }

            void OnDestroy() { if (_runner == this) _runner = null; }
        }
    }
}
