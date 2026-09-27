using System;
using System.Collections.Generic;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Hub life (PRG-15, docs/design/hub-life.md): where an NPC stands at each phase of the day, and what it
    /// says there. On a phase change it walks along the floor to the new post; a room that loads mid-day
    /// snaps. In an <b>anchored</b> place the day is locked, so the schedule becomes a loop: the same few posts
    /// in the same order forever (the bible's "schedules loop"). No NavMesh: rooms are floors, and a post is
    /// somewhere the NPC can walk to on this one.
    /// </summary>
    public sealed class NpcSchedule : MonoBehaviour
    {
        [Serializable]
        public sealed class Post
        {
            public DayPhase Phase;
            public Vector2 Position;
            /// <summary>Yarn node to start here (empty keeps the talker's own).</summary>
            public string Node;
            /// <summary>What the NPC is doing ("mending nets"): the page and tests read it.</summary>
            public string Activity;
            /// <summary>Facing at the post: -1 left, 1 right, 0 keep.</summary>
            public int Face;
        }

        [SerializeField] List<Post> _posts = new List<Post>();
        [SerializeField] float _walkSpeed = 1.6f;
        [SerializeField] float _loopSeconds = 24f;
        [SerializeField] string _placeId;
        [SerializeField] LayerMask _groundMask;

        public IReadOnlyList<Post> Posts => _posts;
        public Post Current { get; private set; }
        public bool IsWalking { get; private set; }
        public string Activity => Current != null ? Current.Activity : "";
        public float WalkSpeed { get => _walkSpeed; set => _walkSpeed = value; }
        public float LoopSeconds { get => _loopSeconds; set => _loopSeconds = value; }
        public string PlaceId
        {
            get
            {
                if (!string.IsNullOrEmpty(_placeId)) return _placeId;
                var room = GetComponentInParent<Room>();
                return room != null ? room.RoomId : "";
            }
            set => _placeId = value;
        }
        /// <summary>The day is locked here (anchored): posts loop instead of following the clock.</summary>
        public bool IsLooping => Places.FateOf(GameState.World, PlaceId) == PlaceFate.Anchored;
        public int LoopIndex { get; private set; }
        public event Action<Post> Arrived;

        NpcTalker _talker;
        string _ownNode;
        float _loopT;
        bool _snapped;

        public Post AddPost(DayPhase phase, Vector2 position, string node = "", string activity = "", int face = 0)
        {
            var p = new Post { Phase = phase, Position = position, Node = node, Activity = activity, Face = face };
            _posts.Add(p);
            return p;
        }

        void Awake()
        {
            if (_groundMask.value == 0) _groundMask = LayerMask.GetMask("Ground");
            _talker = GetComponent<NpcTalker>();
            _ownNode = _talker != null ? _talker.StartNode : null;
        }

        void OnEnable() { _snapped = false; }

        /// <summary>The post for a phase: an exact match, else the latest earlier one in the day, else the last.</summary>
        public Post PostFor(DayPhase phase)
        {
            if (_posts.Count == 0) return null;
            foreach (var p in _posts) if (p.Phase == phase) return p;
            Post best = null;
            foreach (var p in _posts) if (p.Phase < phase && (best == null || p.Phase > best.Phase)) best = p;
            if (best != null) return best;
            foreach (var p in _posts) if (best == null || p.Phase > best.Phase) best = p;
            return best;
        }

        Post Target()
        {
            if (_posts.Count == 0) return null;
            if (IsLooping)
            {
                // Dwell at the post for the period, then the next one: the walk itself is not on the clock.
                if (Current != null && !IsWalking)
                {
                    _loopT += Time.deltaTime;
                    if (_loopT >= _loopSeconds) { _loopT = 0f; LoopIndex = (LoopIndex + 1) % _posts.Count; }
                }
                return _posts[LoopIndex];
            }
            return PostFor(DayClock.PhaseIn(GameState.World, PlaceId));
        }

        void Update()
        {
            if (_posts.Count == 0) return;
            var target = Target();
            if (target == null) return;
            if (!_snapped)
            {
                _snapped = true;
                Place(target.Position);
                Arrive(target);
                return;
            }
            if (DialogueService.Instance != null && DialogueService.Instance.IsRunning) return;   // mid-conversation: stay

            Vector2 pos = transform.position;
            float dx = target.Position.x - pos.x;
            if (Mathf.Abs(dx) <= 0.05f)
            {
                if (Current != target || IsWalking) { Place(target.Position); Arrive(target); }
                return;
            }
            IsWalking = true;
            Current = null;
            float step = Mathf.Min(Mathf.Abs(dx), _walkSpeed * Time.deltaTime);
            Face(dx < 0f ? -1 : 1);
            Place(new Vector2(pos.x + Mathf.Sign(dx) * step, pos.y));
        }

        void Place(Vector2 p)
        {
            // Keep the feet on the floor under the new x (sync first: a room may have been built this frame).
            Physics2D.SyncTransforms();
            var hit = Physics2D.Raycast(new Vector2(p.x, p.y + 1.5f), Vector2.down, 4f, _groundMask);
            float y = hit.collider != null ? hit.point.y : p.y;
            transform.position = new Vector3(p.x, y, transform.position.z);
        }

        void Arrive(Post p)
        {
            IsWalking = false;
            Current = p;
            if (p.Face != 0) Face(p.Face);
            if (_talker != null) _talker.StartNode = string.IsNullOrEmpty(p.Node) ? _ownNode : p.Node;
            Arrived?.Invoke(p);
        }

        void Face(int dir)
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * dir;
            transform.localScale = s;
        }
    }
}
