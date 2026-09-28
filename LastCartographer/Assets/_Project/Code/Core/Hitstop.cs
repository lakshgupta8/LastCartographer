using System.Collections;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// Freezes game time for a few frames on hits (combat doc 1: 2 on hit, 5 on kill, 8 on parry), scaled by the
    /// player's <see cref="Options.Hitstop"/> (DES-14; none at 0). Runs on unscaled time so it always releases, and
    /// leaves time stopped if the game was paused meanwhile. Overlapping requests extend, never stack.
    /// </summary>
    public static class Hitstop
    {
        const float FrameSeconds = 1f / 60f;
        static Runner _runner;
        static float _releaseAt;

        public static bool Active => _runner != null && Time.unscaledTime < _releaseAt;

        public static void Request(int frames)
        {
            frames = Options.HitstopFrames(frames);
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
                Time.timeScale = Pause.Active ? 0f : 1f;
                _co = null;
            }
        }
    }

    /// <summary>The options page stops the world: time stands still until it closes, and hitstop won't start it again.</summary>
    public static class Pause
    {
        public static bool Active { get; private set; }

        public static void Begin()
        {
            Active = true;
            Time.timeScale = 0f;
        }

        public static void End()
        {
            if (!Active) return;
            Active = false;
            if (!Hitstop.Active) Time.timeScale = 1f;
        }
    }
}
