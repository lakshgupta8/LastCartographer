using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Windreach's burrower (CMB-09, combat doc 7 "pogo the shelled ones"): a shelled thing that passes for a clump of
    /// the long grass. Under the turf it is a moving ridge that travels toward her, unhittable and harmless; close
    /// under her it heaves (the telegraph: the turf lifts, a tell on the first frame), breaches up through the grass,
    /// sits shelled on the surface a moment, then burrows and travels again. Shelled: side and upward strikes glance
    /// off; only the down-strike pogo lands, and only while it is above ground. Walks the floor, with gravity.
    /// Answer: Pogo.
    /// </summary>
    public sealed class Tussock : Enemy
    {
        [SerializeField] float _ridgeSpeed = 2.5f;
        [SerializeField] float _range = 8f;
        [SerializeField] float _underReach = 1.3f;
        [SerializeField] float _heaveSeconds = 0.4f;
        [SerializeField] float _breachSeconds = 0.35f;
        [SerializeField] float _breachSpeed = 7f;
        [SerializeField] float _sitSeconds = 1.2f;
        [SerializeField] float _burrowSeconds = 0.4f;
        [SerializeField] float _cooldown = 1.5f;
        [SerializeField] LayerMask _groundMask;

        public enum Move { Ridge, Heave, Breach, Sit, Burrow }
        public Move State { get; private set; }
        float _t, _wait;

        public bool IsTelegraphing => State == Move.Heave;
        /// <summary>Above the turf: the shell shown, open to a pogo and able to hurt.</summary>
        public bool IsSurfaced => State == Move.Breach || State == Move.Sit;

        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip
            : State == Move.Ridge ? "ridge" : State == Move.Heave ? "heave" : State == Move.Breach ? "breach" : State == Move.Burrow ? "burrow" : "idle";
        public override float ClipProgress => State == Move.Heave ? Mathf.Clamp01(_t / _heaveSeconds) : State == Move.Burrow ? Mathf.Clamp01(_t / _burrowSeconds) : -1f;

        protected override void Awake()
        {
            base.Awake();
            _answer = EnemyAnswer.Pogo;
            if (_groundMask.value == 0) _groundMask = LayerMask.GetMask("Ground");
        }

        protected override void OnRevived() { State = Move.Ridge; _t = 0f; _wait = 0f; }

        /// <summary>Shelled, and under the turf for most of its life: only a pogo on the surfaced shell lands.</summary>
        protected override bool AcceptsHit(in HitInfo hit) => IsSurfaced && hit.Direction.y < -0.5f;
        /// <summary>A ridge in the grass hurts no one.</summary>
        protected override bool ContactHurts => IsSurfaced;

        bool EdgeOrWallAhead => !GroundAhead(0.1f, 0.6f, _groundMask) || WallAhead(0.1f, _groundMask);

        protected override void Tick(float dt)
        {
            _t += dt;
            _wait -= dt;
            float vy = Body.linearVelocity.y;
            switch (State)
            {
                case Move.Ridge:
                {
                    if (Wren == null) { Body.linearVelocity = new Vector2(0f, vy); break; }
                    float dx = Wren.Position.x - transform.position.x;
                    bool near = Mathf.Abs(dx) < _range && Mathf.Abs(Wren.Position.y - transform.position.y) < 2.5f;
                    if (near) Face(dx >= 0f ? 1 : -1);
                    if (near && _wait <= 0f && Mathf.Abs(dx) < _underReach)
                    {
                        Body.linearVelocity = new Vector2(0f, vy);
                        State = Move.Heave;
                        _t = 0f;
                        Tell(AttackKind.Strike);
                        return;
                    }
                    bool travel = near && Mathf.Abs(dx) > 0.4f && !EdgeOrWallAhead;
                    Body.linearVelocity = new Vector2(travel ? Facing * _ridgeSpeed : 0f, vy);
                    break;
                }
                case Move.Heave:
                {
                    Body.linearVelocity = new Vector2(0f, vy);
                    if (_t >= _heaveSeconds) { State = Move.Breach; _t = 0f; Body.linearVelocity = new Vector2(0f, _breachSpeed); }
                    break;
                }
                case Move.Breach:
                {
                    if (_t >= _breachSeconds) { State = Move.Sit; _t = 0f; }
                    break;
                }
                case Move.Sit:
                {
                    Body.linearVelocity = new Vector2(0f, vy);
                    if (_t >= _sitSeconds) { State = Move.Burrow; _t = 0f; }
                    break;
                }
                case Move.Burrow:
                {
                    Body.linearVelocity = new Vector2(0f, vy);
                    if (_t >= _burrowSeconds) { State = Move.Ridge; _t = 0f; _wait = _cooldown; }
                    break;
                }
            }
        }

        protected override Color TintColor() => State == Move.Heave ? new Color(0.72f, 0.64f, 0.38f) : IsSurfaced ? new Color(0.50f, 0.46f, 0.40f) : new Color(0.46f, 0.38f, 0.26f);
    }
}
