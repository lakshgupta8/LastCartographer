using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The Greyfold's road-thing (CMB-09; rooms doc: "platforms are drawn only inside Wren's lantern-radius; outside it,
    /// outlines"): a bird nobody finished. Outside her lantern-radius it is an outline that paces toward her, harmless
    /// and unhittable; inside it the drawing fills (the telegraph: the neck comes up, a tell on the first frame) and it
    /// lunges low along the ground, stopping at an edge or a wall. Walks, with gravity, like the salamander.
    /// Answer: any hit, inside the radius.
    /// </summary>
    public sealed class Sketch : Enemy
    {
        [SerializeField] float _paceSpeed = 1.4f;
        [SerializeField] float _fillSeconds = 0.3f;
        [SerializeField] float _lungeSpeed = 9f;
        [SerializeField] float _lungeSeconds = 0.35f;
        [SerializeField] float _lungeCooldown = 1.6f;
        [SerializeField] float _lungeRange = 4f;
        [SerializeField] float _fallbackRadius = 3.5f;
        [SerializeField] float _outlineInk = 0.18f;
        [SerializeField] float _inkFadeSpeed = 5f;
        [SerializeField] LayerMask _groundMask;

        public enum Move { Pace, Fill, Lunge }
        public Move State { get; private set; }
        float _t, _cooldown, _ink = 1f, _forcedRadius = -1f;
        ClarityMeter _clarity;
        MaterialPropertyBlock _inkBlock;
        static readonly int InkId = Shader.PropertyToID("_Ink");

        public bool IsTelegraphing => State == Move.Fill;
        /// <summary>How far her lantern draws: the Clarity meter's radius, else the platforms' default.</summary>
        public float DrawnRadius => _forcedRadius > 0f ? _forcedRadius : _clarity != null && _clarity.Radius > 0f ? _clarity.Radius : _fallbackRadius;
        /// <summary>Inside her lantern-radius: drawn, so it can be hit and can hurt.</summary>
        public bool IsDrawn => Wren != null && (Wren.Position + Vector2.up * 0.6f - (Vector2)transform.position).sqrMagnitude <= DrawnRadius * DrawnRadius;
        public float InkLevel => _ink;
        /// <summary>Tests: pin the radius it reads.</summary>
        public void ForceRadius(float radius) { _forcedRadius = radius; }

        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip
            : State == Move.Fill ? "fill" : State == Move.Lunge ? "lunge" : base.Clip;
        public override float ClipProgress => State == Move.Fill ? Mathf.Clamp01(_t / _fillSeconds) : -1f;

        protected override void Awake()
        {
            base.Awake();
            _answer = EnemyAnswer.AnyHit;
            if (_groundMask.value == 0) _groundMask = LayerMask.GetMask("Ground");
            _inkBlock = new MaterialPropertyBlock();
        }

        protected override void OnRevived() { State = Move.Pace; _t = 0f; _cooldown = 0f; }

        /// <summary>An outline cannot be struck; the drawing can.</summary>
        protected override bool AcceptsHit(in HitInfo hit) => IsDrawn;
        /// <summary>An outline cannot hurt.</summary>
        protected override bool ContactHurts => IsDrawn;

        bool EdgeOrWallAhead => !GroundAhead(0.1f, 0.7f, _groundMask) || WallAhead(0.1f, _groundMask);

        protected override void Tick(float dt)
        {
            _t += dt;
            _cooldown -= dt;
            if (_clarity == null && Wren != null) _clarity = Wren.GetComponent<ClarityMeter>();
            float vy = Body.linearVelocity.y;
            switch (State)
            {
                case Move.Pace:
                {
                    if (Wren == null) { Body.linearVelocity = new Vector2(0f, vy); break; }
                    float dx = Wren.Position.x - transform.position.x;
                    Face(dx >= 0f ? 1 : -1);
                    if (IsDrawn && _cooldown <= 0f && Mathf.Abs(dx) < _lungeRange && Mathf.Abs(Wren.Position.y - transform.position.y) < 2f)
                    {
                        Body.linearVelocity = new Vector2(0f, vy);
                        State = Move.Fill;
                        _t = 0f;
                        Tell(AttackKind.Strike);
                        return;
                    }
                    bool walk = Mathf.Abs(dx) > 0.8f && !EdgeOrWallAhead;
                    Body.linearVelocity = new Vector2(walk ? Facing * _paceSpeed : 0f, vy);
                    break;
                }
                case Move.Fill:
                {
                    Body.linearVelocity = new Vector2(0f, vy);
                    if (_t >= _fillSeconds) { State = Move.Lunge; _t = 0f; }
                    break;
                }
                case Move.Lunge:
                {
                    Body.linearVelocity = new Vector2(EdgeOrWallAhead ? 0f : Facing * _lungeSpeed, vy);
                    if (_t >= _lungeSeconds)
                    {
                        State = Move.Pace;
                        _cooldown = _lungeCooldown;
                        _t = 0f;
                    }
                    break;
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            if (IsDying) return;   // the death fade owns the ink
            _ink = Mathf.MoveTowards(_ink, IsDrawn ? 1f : _outlineInk, Time.deltaTime * _inkFadeSpeed);
            if (Visual == null) return;
            Visual.GetPropertyBlock(_inkBlock);
            _inkBlock.SetFloat(InkId, _ink);
            Visual.SetPropertyBlock(_inkBlock);
        }

        protected override Color TintColor() => State == Move.Fill ? new Color(0.55f, 0.55f, 0.58f) : new Color(0.80f, 0.79f, 0.76f);
    }
}
