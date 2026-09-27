using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace OWSBG.World
{
    /// <summary>
    /// One cutscene (PRG-16): a PlayableDirector with a Timeline, an optional Cinemachine shot that takes
    /// over while it plays, and Wren frozen for the duration. Found by id from Yarn (&lt;&lt;cutscene id&gt;&gt;)
    /// or from code. Clips that need the timeline to wait (a dialogue node) call Pause / Resume.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class Cutscene : MonoBehaviour
    {
        [SerializeField] string _id;
        [SerializeField] CinemachineCamera _shot;
        [SerializeField] int _shotPriority = 50;
        [SerializeField] bool _freezeWren = true;

        static readonly Dictionary<string, Cutscene> _byId = new Dictionary<string, Cutscene>();

        /// <summary>The cutscene playing now, if any (the most recently started).</summary>
        public static Cutscene Current { get; private set; }
        public static Cutscene Find(string id) => id != null && _byId.TryGetValue(id, out var c) ? c : null;

        public string Id
        {
            get => _id;
            set { Unregister(); _id = value; Register(); }
        }
        public PlayableDirector Director { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public bool FreezeWren { get => _freezeWren; set => _freezeWren = value; }
        public CinemachineCamera Shot { get => _shot; set => _shot = value; }

        public event Action<Cutscene> Started;
        public event Action<Cutscene> Completed;
        public static event Action<Cutscene> AnyStarted;
        public static event Action<Cutscene> AnyCompleted;

        WrenController _wren;
        bool _wasFrozen;

        void Awake()
        {
            Director = GetComponent<PlayableDirector>();
            Director.playOnAwake = false;
            Director.extrapolationMode = DirectorWrapMode.None;
            Director.timeUpdateMode = DirectorUpdateMode.GameTime;
            Director.stopped += OnStopped;
        }

        void OnEnable() { Register(); }
        void OnDisable() { Unregister(); }

        void OnDestroy()
        {
            if (Director != null) Director.stopped -= OnStopped;
            if (Current == this) Current = null;
        }

        void Register() { if (!string.IsNullOrEmpty(_id)) _byId[_id] = this; }
        void Unregister() { if (!string.IsNullOrEmpty(_id) && _byId.TryGetValue(_id, out var c) && c == this) _byId.Remove(_id); }

        /// <summary>Bind an exposed reference (an actor a clip moves) by name.</summary>
        public void SetReference(string exposedName, UnityEngine.Object value)
            => (Director != null ? Director : GetComponent<PlayableDirector>()).SetReferenceValue(exposedName, value);   // edit mode too

        public void Play()
        {
            if (IsPlaying) return;
            if (Director.playableAsset == null)
            {
                Debug.LogWarning("[OWSBG] Cutscene '" + _id + "' has no timeline");
                return;
            }
            IsPlaying = true;
            IsPaused = false;
            Current = this;
            _wren = FindFirstObjectByType<WrenController>();
            if (_wren != null && _freezeWren) { _wasFrozen = _wren.Frozen; _wren.Frozen = true; }
            if (_shot != null)
            {
                _shot.Priority = _shotPriority;
                _shot.gameObject.SetActive(true);
            }
            // A director that already built its graph (play-on-awake before Awake here could stop it, or an earlier
            // run) would keep stale exposed references; build it fresh every time.
            Director.Stop();
            Director.RebuildGraph();
            Director.time = 0;
            Director.Play();
            Started?.Invoke(this);
            AnyStarted?.Invoke(this);
        }

        /// <summary>Hold the timeline where it is (a clip waiting on dialogue). Resume continues.</summary>
        public void Pause()
        {
            if (!IsPlaying || IsPaused) return;
            IsPaused = true;
            SetSpeed(0.0);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            SetSpeed(1.0);
        }

        void SetSpeed(double speed)
        {
            var g = Director.playableGraph;
            if (g.IsValid() && g.GetRootPlayableCount() > 0) g.GetRootPlayable(0).SetSpeed(speed);
        }

        /// <summary>Stop early; Completed still fires so nothing waits forever.</summary>
        public void Stop()
        {
            if (!IsPlaying) return;
            Director.Stop();
            if (IsPlaying) Finish();   // stopped may not be raised if the graph was never built
        }

        void OnStopped(PlayableDirector d) { if (IsPlaying) Finish(); }

        void Finish()
        {
            IsPlaying = false;
            IsPaused = false;
            if (_wren != null && _freezeWren) _wren.Frozen = _wasFrozen;
            _wren = null;
            if (_shot != null) _shot.gameObject.SetActive(false);
            if (Current == this) Current = null;
            Completed?.Invoke(this);
            AnyCompleted?.Invoke(this);
        }

        void Update()
        {
            // Dialogue inside a cutscene releases Wren when it ends; the cutscene keeps her still.
            if (IsPlaying && _freezeWren && _wren != null && !_wren.Frozen) _wren.Frozen = true;
        }
    }
}
