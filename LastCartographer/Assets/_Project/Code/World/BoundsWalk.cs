using System;
using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The bounds-walk (combat doc 10, DES-13, PRG-22): a rhythm traversal. The chorus sings the roll-call; every
    /// beat names a bound (a spot in the room) and Wren must be standing in it when the beat lands. The name is
    /// called half a beat ahead. Three misses in a verse and the verse starts again. Every verse walked and the
    /// place is <b>held</b> (BoundsWalks.Complete). Slow and warm: the beat is seconds, not frames.
    /// </summary>
    public sealed class BoundsWalk : MonoBehaviour
    {
        [Serializable]
        public sealed class Bound
        {
            public string Name;
            public Vector2 Position;
            public float Radius = 1.6f;
            public Renderer Marker;
        }

        [Serializable]
        public sealed class Verse
        {
            public string Title;
            public List<Bound> Beats = new List<Bound>();
        }

        [SerializeField] string _id;
        [SerializeField] string _placeId;
        [SerializeField] float _secondsPerBeat = 3f;
        [SerializeField] int _missesAllowed = 3;
        [SerializeField] List<Verse> _verses = new List<Verse>();
        [Tooltip("Things that should stand still while the walk runs (NPC schedules of the chorus).")]
        [SerializeField] List<Behaviour> _pauseWhileWalking = new List<Behaviour>();
        [SerializeField] string _completeFlag;
        [SerializeField] int _completeFlagValue = 1;

        public enum Phase { Idle, Pending, Walking, Done }
        public Phase State { get; private set; }
        public string Id { get => _id; set => _id = value; }
        public string PlaceId { get => string.IsNullOrEmpty(_placeId) ? RoomId : _placeId; set => _placeId = value; }
        public int VerseIndex { get; private set; }
        public int BeatIndex { get; private set; }
        public int Misses { get; private set; }
        public int Hits { get; private set; }
        public int Restarts { get; private set; }
        public IReadOnlyList<Verse> Verses => _verses;
        public float SecondsPerBeat { get => _secondsPerBeat; set => _secondsPerBeat = Mathf.Max(0.05f, value); }
        public int MissesAllowed => _missesAllowed;
        /// <summary>The bound whose name has been called and whose beat is coming.</summary>
        public Bound Called { get; private set; }
        /// <summary>0..1 through the current beat.</summary>
        public float BeatProgress => State == Phase.Walking ? Mathf.Clamp01(_t / _secondsPerBeat) : 0f;
        public bool IsWalking => State == Phase.Walking;

        public static BoundsWalk Current { get; private set; }
        public static event Action<BoundsWalk> Started;
        public static event Action<BoundsWalk, Bound> NameCalled;
        public static event Action<BoundsWalk, Bound, bool> BeatLanded;
        public static event Action<BoundsWalk> VerseRestarted;
        public static event Action<BoundsWalk> Completed;

        static readonly Dictionary<string, BoundsWalk> Registry = new Dictionary<string, BoundsWalk>();
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color CalledTint = new Color(0.95f, 0.72f, 0.25f);
        static readonly Color RestTint = new Color(0.20f, 0.27f, 0.45f);

        WrenController _wren;
        float _t;
        bool _called;
        MaterialPropertyBlock _mpb;

        string RoomId { get { var room = GetComponentInParent<Room>(); return room != null ? room.RoomId : (Room.Current != null ? Room.Current.RoomId : ""); } }

        public static BoundsWalk Find(string id) => !string.IsNullOrEmpty(id) && Registry.TryGetValue(id, out var w) && w != null ? w : null;

        void Awake() { _mpb = new MaterialPropertyBlock(); }
        void OnEnable() { if (!string.IsNullOrEmpty(_id)) Registry[_id] = this; TintAll(RestTint); }
        void OnDisable()
        {
            if (!string.IsNullOrEmpty(_id) && Registry.TryGetValue(_id, out var w) && w == this) Registry.Remove(_id);
            if (Current == this) Current = null;
            SetPaused(false);
        }

        public Verse CurrentVerse => VerseIndex < _verses.Count ? _verses[VerseIndex] : null;
        public Bound CurrentBeat => CurrentVerse != null && BeatIndex < CurrentVerse.Beats.Count ? CurrentVerse.Beats[BeatIndex] : null;

        /// <summary>Add a verse from code (tests, tooling).</summary>
        public Verse AddVerse(string title, params (string name, Vector2 pos)[] beats)
        {
            var v = new Verse { Title = title };
            foreach (var b in beats) v.Beats.Add(new Bound { Name = b.name, Position = b.pos });
            _verses.Add(v);
            return v;
        }

        /// <summary>Start the walk. It waits until Wren is free (a conversation may be ending), then counts in.</summary>
        public bool Begin()
        {
            if (State == Phase.Walking || State == Phase.Pending || _verses.Count == 0) return false;
            if (BoundsWalks.IsWalked(GameState.World, PlaceId)) return false;
            State = Phase.Pending;
            VerseIndex = 0; BeatIndex = 0; Misses = 0; Hits = 0; Restarts = 0;
            Called = null;
            _t = 0f;
            Current = this;
            return true;
        }

        public void Abort()
        {
            if (State == Phase.Idle || State == Phase.Done) return;
            State = Phase.Idle;
            Called = null;
            SetPaused(false);
            TintAll(RestTint);
            if (Current == this) Current = null;
        }

        void Update()
        {
            if (State == Phase.Pending)
            {
                if (_wren == null) _wren = FindFirstObjectByType<WrenController>();
                if (_wren == null || _wren.Frozen) return;
                State = Phase.Walking;
                SetPaused(true);
                _t = _secondsPerBeat * 0.5f;   // a half-beat count-in before the first name
                _called = false;
                Started?.Invoke(this);
                Captions.Show(Loc.F("caption.rollcall", "The roll-call: {0}", CurrentVerse.Title ?? Loc.T("caption.rollcall.first", "verse one")), 2.5f);
                return;
            }
            if (State != Phase.Walking) return;
            if (_wren == null) return;

            _t += Time.deltaTime;
            var beat = CurrentBeat;
            if (beat == null) { Finish(); return; }

            // The name is called half a beat before it lands.
            if (!_called && _t >= _secondsPerBeat * 0.5f)
            {
                _called = true;
                Called = beat;
                TintAll(RestTint);
                Tint(beat, CalledTint);
                NameCalled?.Invoke(this, beat);
            }
            if (_t < _secondsPerBeat) return;

            // The beat lands.
            _t -= _secondsPerBeat;
            _called = false;
            bool hit = Vector2.Distance(_wren.Position, beat.Position) <= beat.Radius;
            if (hit) Hits++; else Misses++;
            BeatLanded?.Invoke(this, beat, hit);
            Called = null;
            if (!hit && Misses >= _missesAllowed)
            {
                Misses = 0;
                BeatIndex = 0;
                Restarts++;
                TintAll(RestTint);
                VerseRestarted?.Invoke(this);
                Captions.Show(Loc.T("caption.rollcall.again", "Again, from the top of the verse."), 2f);
                return;
            }
            BeatIndex++;
            if (BeatIndex >= CurrentVerse.Beats.Count)
            {
                BeatIndex = 0;
                Misses = 0;
                VerseIndex++;
                if (VerseIndex >= _verses.Count) { Finish(); return; }
                Captions.Show(Loc.F("caption.rollcall", "The roll-call: {0}", CurrentVerse.Title ?? Loc.T("caption.rollcall.next", "next verse")), 2.5f);
            }
        }

        void Finish()
        {
            State = Phase.Done;
            Called = null;
            SetPaused(false);
            TintAll(CalledTint);
            var w = GameState.World;
            BoundsWalks.Complete(w, PlaceId);
            if (!string.IsNullOrEmpty(_completeFlag)) w.Set(_completeFlag, _completeFlagValue);
            Captions.Show(Loc.F("caption.walked", "Walked. {0} is held.", Atlas.PlaceName(PlaceId)), 4f);
            if (Current == this) Current = null;
            Completed?.Invoke(this);
        }

        void SetPaused(bool paused)
        {
            foreach (var b in _pauseWhileWalking) if (b != null) b.enabled = !paused;
        }

        void TintAll(Color c)
        {
            foreach (var v in _verses) foreach (var b in v.Beats) Tint(b, c);
        }

        void Tint(Bound b, Color c)
        {
            if (b?.Marker == null || _mpb == null) return;
            b.Marker.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            b.Marker.SetPropertyBlock(_mpb);
        }
    }
}
