using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Emberdown's crawler (the mine country's salamander; rooms doc §3: "pogo the salamanders"). Patrols its ledge,
    /// turns at edges and walls. When Wren is near it flares (the telegraph: its embers stand up, a tell on the first
    /// frame), rushes low along the ground for a moment, stopping at the ledge's edge, then cools before it crawls
    /// again. Its back burns: side and upward strikes are turned away; only the down-strike pogo lands.
    /// Answer: Pogo.
    /// </summary>
    public sealed class Salamander : Enemy
    {
        [SerializeField] float _crawlSpeed = 1.6f;
        [SerializeField] float _range = 5f;
        [SerializeField] float _flareSeconds = 0.35f;
        [SerializeField] float _rushSpeed = 8f;
        [SerializeField] float _rushSeconds = 0.45f;
        [SerializeField] float _coolSeconds = 0.8f;
        [SerializeField] float _rushCooldown = 2f;
        [SerializeField] LayerMask _groundMask;

        public enum Move { Crawl, Flare, Rush, Cool }
        public Move State { get; private set; }
        float _t, _cooldown;

        public bool IsTelegraphing => State == Move.Flare;
        /// <summary>Flaring or rushing: the embers are up.</summary>
        public bool IsHot => State == Move.Flare || State == Move.Rush;
        /// <summary>Which way it faces (+1 right); the rush goes this way.</summary>
        public int FacingDir => Facing;

        protected override void Awake()
        {
            base.Awake();
            _answer = EnemyAnswer.Pogo;
            if (_groundMask.value == 0) _groundMask = LayerMask.GetMask("Ground");
        }

        protected override void OnRevived() { State = Move.Crawl; _t = 0f; _cooldown = 0f; }

        protected override bool AcceptsHit(in HitInfo hit) => hit.Direction.y < -0.5f;   // its back burns; only a pogo lands

        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip
            : State == Move.Flare ? "flare" : State == Move.Rush ? "rush" : State == Move.Cool ? "cool" : base.Clip;
        public override float ClipProgress => State == Move.Flare ? Mathf.Clamp01(_t / _flareSeconds) : -1f;

        bool EdgeOrWallAhead => !GroundAhead(0.1f, 0.6f, _groundMask) || WallAhead(0.1f, _groundMask);

        protected override void Tick(float dt)
        {
            _t += dt;
            _cooldown -= dt;
            float vy = Body.linearVelocity.y;
            switch (State)
            {
                case Move.Crawl:
                {
                    if (EdgeOrWallAhead) Face(-Facing);
                    if (Wren != null && _cooldown <= 0f)
                    {
                        var toWren = Wren.Position - (Vector2)transform.position;
                        if (Mathf.Abs(toWren.x) < _range && Mathf.Abs(toWren.y) < 1.5f)
                        {
                            Face(toWren.x >= 0f ? 1 : -1);
                            Body.linearVelocity = new Vector2(0f, vy);
                            State = Move.Flare;
                            _t = 0f;
                            Tell(AttackKind.Strike);
                            return;
                        }
                    }
                    Body.linearVelocity = new Vector2(Facing * _crawlSpeed, vy);
                    break;
                }
                case Move.Flare:
                {
                    Body.linearVelocity = new Vector2(0f, vy);
                    if (_t >= _flareSeconds) { State = Move.Rush; _t = 0f; }
                    break;
                }
                case Move.Rush:
                {
                    if (EdgeOrWallAhead || _t >= _rushSeconds)
                    {
                        Body.linearVelocity = new Vector2(0f, vy);
                        State = Move.Cool;
                        _t = 0f;
                        break;
                    }
                    Body.linearVelocity = new Vector2(Facing * _rushSpeed, vy);
                    break;
                }
                case Move.Cool:
                {
                    Body.linearVelocity = new Vector2(0f, vy);
                    if (_t >= _coolSeconds) { State = Move.Crawl; _t = 0f; _cooldown = _rushCooldown; }
                    break;
                }
            }
        }

        protected override Color TintColor() => IsHot ? new Color(0.96f, 0.46f, 0.12f) : new Color(0.14f, 0.12f, 0.13f);
    }
}
