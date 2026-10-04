using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Emberdown's flier (the mine country's bat; rooms doc §3: "strike the bats as they dive"). Hangs at its roost
    /// until Wren comes within reach, unfurls (the telegraph: a tell on its first frame), swoops in one arc from the
    /// roost through where she stood and up the far side, then flaps back to the roost. No gravity.
    /// Answer: any hit, best taken as it swoops past.
    /// </summary>
    public sealed class CaveBat : Enemy
    {
        [SerializeField] float _aggroRange = 6f;
        [SerializeField] float _unfurlSeconds = 0.4f;
        [SerializeField] float _swoopSeconds = 0.8f;
        [SerializeField] float _swoopMaxSpeed = 22f;
        [SerializeField] float _returnSpeed = 6f;
        [SerializeField] float _swoopCooldown = 2.2f;

        public enum Move { Roost, Unfurl, Swoop, Return }
        public Move State { get; private set; }
        Vector2 _roost, _p0, _c, _p2;
        float _t, _cooldown;

        /// <summary>Where it hangs: its position when the room was built.</summary>
        public Vector2 Roost => _roost;
        public bool IsTelegraphing => State == Move.Unfurl;
        /// <summary>The arc's point at k in 0..1 (a quadratic curve from the roost through the target to the far side).</summary>
        public Vector2 Arc(float k) { float u = 1f - k; return u * u * _p0 + 2f * u * k * _c + k * k * _p2; }

        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip
            : State == Move.Unfurl ? "unfurl" : State == Move.Swoop ? "swoop" : State == Move.Return ? "move" : "idle";
        public override float ClipProgress => State == Move.Unfurl ? Mathf.Clamp01(_t / _unfurlSeconds) : -1f;

        protected override void Awake()
        {
            base.Awake();
            Body.gravityScale = 0f;
            _roost = transform.position;
        }

        protected override void OnRevived()
        {
            State = Move.Roost;
            _t = 0f; _cooldown = 0f;
            transform.position = _roost;
        }

        protected override void Tick(float dt)
        {
            _cooldown -= dt;
            _t += dt;
            Vector2 pos = transform.position;
            switch (State)
            {
                case Move.Roost:
                {
                    Body.linearVelocity = (_roost - pos) * 6f;
                    if (Wren != null) Face(Wren.Position.x >= pos.x ? 1 : -1);
                    if (Wren != null && _cooldown <= 0f && (Wren.Position - pos).sqrMagnitude < _aggroRange * _aggroRange)
                    {
                        State = Move.Unfurl;
                        _t = 0f;
                        Tell(AttackKind.Strike);
                    }
                    break;
                }
                case Move.Unfurl:
                {
                    Body.linearVelocity = Vector2.zero;
                    if (_t >= _unfurlSeconds)
                    {
                        var target = Wren != null ? Wren.Position + Vector2.up * 0.5f : pos + Vector2.down * 3f;
                        _p0 = pos;
                        _p2 = new Vector2(target.x + (target.x - pos.x), pos.y);        // up the far side, to the roost's height
                        _c = 2f * target - 0.5f * (_p0 + _p2);                          // so the arc passes through the target at its middle
                        Face(_p2.x >= _p0.x ? 1 : -1);
                        State = Move.Swoop;
                        _t = 0f;
                    }
                    break;
                }
                case Move.Swoop:
                {
                    float k = Mathf.Clamp01(_t / _swoopSeconds);
                    var want = (Arc(k) - pos) / Mathf.Max(dt, 0.0001f);
                    Body.linearVelocity = Vector2.ClampMagnitude(want, _swoopMaxSpeed);
                    if (k >= 1f)
                    {
                        State = Move.Return;
                        _cooldown = _swoopCooldown;
                        _t = 0f;
                    }
                    break;
                }
                case Move.Return:
                {
                    var to = _roost - pos;
                    Body.linearVelocity = to.magnitude > 0.2f ? to.normalized * _returnSpeed : Vector2.zero;
                    Face(to.x >= 0f ? 1 : -1);
                    if (to.sqrMagnitude < 0.3f * 0.3f || _t > 4f) { State = Move.Roost; _t = 0f; }
                    break;
                }
            }
        }

        protected override Color TintColor() => State == Move.Unfurl ? new Color(0.95f, 0.55f, 0.25f) : new Color(0.30f, 0.25f, 0.26f);
    }
}
