using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Saltmarrow flier. Hovers around its anchor; when Wren comes near it rises for a beat (the
    /// telegraph), then dives through Wren's position and climbs back. No gravity. Answer: any hit,
    /// best taken as it dives past.
    /// </summary>
    public sealed class ReedSkimmer : Enemy
    {
        [SerializeField] float _hoverRadius = 1.2f;
        [SerializeField] float _hoverSpeed = 2.5f;
        [SerializeField] float _aggroRange = 7f;
        [SerializeField] float _riseSeconds = 0.45f;
        [SerializeField] float _riseHeight = 1.5f;
        [SerializeField] float _diveSpeed = 14f;
        [SerializeField] float _returnSpeed = 5f;
        [SerializeField] float _diveCooldown = 2.0f;

        enum State { Hover, Rise, Dive, Return }
        State _state;
        Vector2 _anchor, _diveTarget, _diveDir;
        float _t, _cooldown;

        public bool IsTelegraphing => _state == State.Rise;

        protected override void Awake()
        {
            base.Awake();
            Body.gravityScale = 0f;
            _anchor = transform.position;
        }

        protected override void Tick(float dt)
        {
            _cooldown -= dt;
            _t += dt;
            Vector2 pos = transform.position;
            switch (_state)
            {
                case State.Hover:
                {
                    var target = _anchor + new Vector2(Mathf.Cos(_t * _hoverSpeed), Mathf.Sin(_t * _hoverSpeed * 1.7f) * 0.5f) * _hoverRadius;
                    Body.linearVelocity = (target - pos) * 4f;
                    if (Wren != null) Face(Wren.Position.x >= pos.x ? 1 : -1);
                    if (Wren != null && _cooldown <= 0f && (Wren.Position - pos).sqrMagnitude < _aggroRange * _aggroRange)
                    {
                        _state = State.Rise;
                        _t = 0f;
                    }
                    break;
                }
                case State.Rise:
                {
                    Body.linearVelocity = new Vector2(0f, _riseHeight / _riseSeconds);
                    if (_t >= _riseSeconds)
                    {
                        _diveTarget = Wren != null ? Wren.Position + Vector2.up * 0.6f : pos + Vector2.down * 3f;
                        _diveDir = (_diveTarget - pos).normalized;
                        Face(_diveDir.x >= 0f ? 1 : -1);
                        _state = State.Dive;
                        _t = 0f;
                    }
                    break;
                }
                case State.Dive:
                {
                    Body.linearVelocity = _diveDir * _diveSpeed;
                    bool passed = Vector2.Dot(_diveTarget - pos, _diveDir) < -0.8f;
                    if (passed || _t > 1.2f)
                    {
                        _state = State.Return;
                        _cooldown = _diveCooldown;
                    }
                    break;
                }
                case State.Return:
                {
                    var to = _anchor - pos;
                    Body.linearVelocity = to.magnitude > 0.2f ? to.normalized * _returnSpeed : Vector2.zero;
                    if (to.sqrMagnitude < 0.3f * 0.3f) { _state = State.Hover; _t = 0f; }
                    break;
                }
            }
        }

        protected override Color TintColor() => _state == State.Rise ? new Color(0.95f, 0.85f, 0.35f) : new Color(0.35f, 0.55f, 0.50f);
    }
}
