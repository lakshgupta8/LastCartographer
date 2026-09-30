using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// An Unwriter Cantor (bible 3.3, combat doc 7): a dove with a bell. Hovers out of the quill's reach and
    /// keeps its distance; when Wren is near it raises the bell (the telegraph), then tolls. The toll hurts
    /// anyone under it and <b>erases</b> the place: every drawn vantage here leaves the atlas and the ink
    /// goes blank until Wren surveys again (Atlas.Erase, DES-02). A hit during the ring stops it.
    /// Answer: Longstroke (the forward Flourish reaches it).
    /// </summary>
    public sealed class Cantor : Enemy
    {
        [SerializeField] float _hoverHeight = 2.6f;
        [SerializeField] float _driftSpeed = 2.2f;
        [SerializeField] float _keepDistance = 2.5f;
        [SerializeField] float _noticeRange = 10f;
        [SerializeField] float _bellRange = 5.5f;
        [SerializeField] float _bellRadius = 4f;
        [SerializeField] int _ringFrames = 26;
        [SerializeField] int _recoverFrames = 40;
        [SerializeField] float _bellCooldown = 4f;
        [SerializeField] int _bellDamage = 1;
        [SerializeField] string _placeId;
        [SerializeField] LayerMask _groundMask;

        public enum Move { Drift, Ring, Recover }
        public Move State { get; private set; }
        public int Tolls { get; private set; }
        public bool IsRinging => State == Move.Ring;
        public float RingProgress => State == Move.Ring ? Mathf.Clamp01((float)_frames / Mathf.Max(1, _ringFrames)) : 0f;
        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip : State == Move.Ring ? "ring" : State == Move.Recover ? "recover" : base.Clip;
        public override float ClipProgress => State == Move.Ring && !IsDying && HurtstunLeft == 0 ? RingProgress : -1f;
        /// <summary>The place the bell erases: set in the scene, else the room the Cantor lives in.</summary>
        public string PlaceId
        {
            get
            {
                if (!string.IsNullOrEmpty(_placeId)) return _placeId;
                var room = GetComponentInParent<Room>();
                return room != null ? room.RoomId : (Room.Current != null ? Room.Current.RoomId : "");
            }
            set => _placeId = value;
        }
        /// <summary>After a toll: the Cantor and the place it rang over.</summary>
        public static event Action<Cantor, string> Tolled;

        int _frames;
        float _cooldown, _bob;
        Vector3 _baseScale;
        readonly Collider2D[] _hits = new Collider2D[4];

        protected override void Awake()
        {
            base.Awake();
            Body.gravityScale = 0f;
            if (_groundMask.value == 0) _groundMask = LayerMask.GetMask("Ground");
            _answer = EnemyAnswer.Longstroke;
            _baseScale = Visual != null ? Visual.transform.localScale : Vector3.one;
        }

        protected override void Tick(float dt)
        {
            _cooldown -= dt;
            _bob += dt;
            Vector2 pos = transform.position;
            switch (State)
            {
                case Move.Drift:
                {
                    var target = HoverPoint(pos);
                    var to = target - pos;
                    Body.linearVelocity = to.magnitude > 0.05f ? Vector2.ClampMagnitude(to * 3f, _driftSpeed) : Vector2.zero;
                    if (Wren != null)
                    {
                        var d = Wren.Position - pos;
                        Face(d.x >= 0f ? 1 : -1);
                        if (_cooldown <= 0f && d.sqrMagnitude < _bellRange * _bellRange)
                        {
                            State = Move.Ring;
                            _frames = 0;
                            Body.linearVelocity = Vector2.zero;
                        }
                    }
                    break;
                }
                case Move.Ring:
                    Body.linearVelocity = new Vector2(0f, Mathf.Sin(_bob * 30f) * 0.15f);
                    if (++_frames >= _ringFrames) Toll();
                    break;
                case Move.Recover:
                    Body.linearVelocity = Vector2.zero;
                    if (++_frames >= _recoverFrames) { State = Move.Drift; _cooldown = _bellCooldown; }
                    break;
            }
        }

        Vector2 HoverPoint(Vector2 pos)
        {
            float floor = pos.y - _hoverHeight;
            var hit = Physics2D.Raycast(pos, Vector2.down, 20f, _groundMask);
            if (hit.collider != null) floor = hit.point.y;
            float x = pos.x;
            if (Wren != null && Mathf.Abs(Wren.Position.x - pos.x) < _noticeRange)
            {
                float side = pos.x >= Wren.Position.x ? 1f : -1f;
                x = Wren.Position.x + side * _keepDistance;
            }
            return new Vector2(x, floor + _hoverHeight + Mathf.Sin(_bob * 2f) * 0.2f);
        }

        void Toll()
        {
            Tolls++;
            State = Move.Recover;
            _frames = 0;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.Player, useTriggers = false };
            int n = Physics2D.OverlapCircle(transform.position, _bellRadius, filter, _hits);
            for (int i = 0; i < n; i++)
            {
                var vitals = _hits[i].GetComponentInParent<WrenVitals>();
                if (vitals == null) continue;
                vitals.Damage(_bellDamage, transform.position);
                break;
            }
            var place = PlaceId;
            if (Atlas.Erase(GameState.World, place))
                Captions.Show(Loc.F("caption.erased", "Erased: {0}. Draw it again.", Atlas.PlaceName(place)), 4f);
            Tolled?.Invoke(this, place);
        }

        /// <summary>A hit stops the bell.</summary>
        protected override void OnHealthChanged()
        {
            if (State != Move.Ring) return;
            State = Move.Drift;
            _frames = 0;
            _cooldown = 1f;
        }

        protected override void Update()
        {
            base.Update();
            if (Visual == null || IsDying || HasSheets) return;
            // The bell rises through the ring: the sprite stretches upward, then drops back on the toll.
            float rise = State == Move.Ring ? 1f + 0.4f * RingProgress : State == Move.Recover ? 0.9f : 1f;
            var s = Visual.transform.localScale;
            Visual.transform.localScale = new Vector3(s.x, Mathf.Abs(_baseScale.y) * rise, s.z);
        }

        protected override Color TintColor() => new Color(0.88f, 0.86f, 0.82f);
    }
}
