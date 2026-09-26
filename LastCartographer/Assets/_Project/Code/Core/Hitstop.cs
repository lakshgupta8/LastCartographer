using System.Collections;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// Freezes game time for a few frames on hits (combat doc 1: 2 on hit, 5 on kill, 8 on parry).
    /// Runs on unscaled time so it always releases. Overlapping requests extend, never stack.
    /// </summary>
    public static class Hitstop
    {
        const float FrameSeconds = 1f / 60f;
        static Runner _runner;
        static float _releaseAt;

        public static bool Active => _runner != null && Time.unscaledTime < _releaseAt;

        public static void Request(int frames)
        {
            if (frames <= 0 || !Application.isPlaying) return;
            var until = Time.unscaledTime + frames * FrameSeconds;
            if (until <= _releaseAt) return;
            _releaseAt = until;
            if (_runner == null)
            {
                var go = new GameObject("~Hitstop") { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<Runner>();
            }
            _runner.Begin();
        }

        sealed class Runner : MonoBehaviour
        {
            Coroutine _co;
            public void Begin()
            {
                if (_co == null) _co = StartCoroutine(Run());
            }
            IEnumerator Run()
            {
                Time.timeScale = 0f;
                while (Time.unscaledTime < _releaseAt) yield return null;
                Time.timeScale = 1f;
                _co = null;
            }
        }
    }
}
