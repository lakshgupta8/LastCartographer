using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Halden's spitter (CMB-09, combat doc 7 "Longstroke the lined-up ones"; late-charters.md: the roster's spitters
    /// throw <see cref="EnemyProjectile"/>s): a paper-wasp of the mills that hovers a stand-off from Wren, backing away
    /// as she comes and closing as she goes, and spits a pellet of pulp at her after a telegraph (the sac swells, a
    /// tell on the first frame). The quill's reach is short of it; the Longstroke's is not, and a line of them is one
    /// stroke. No gravity. Answer: Longstroke.
    /// </summary>
    public sealed class Pulpwasp : Enemy
    {
        [SerializeField] float _hoverSpeed = 2.5f;
        [SerializeField] float _range = 9f;
        [SerializeField] float _standOff = 4.5f;
        [SerializeField] float _spitSeconds = 0.4f;
        [SerializeField] float _spitCooldown = 2.4f;
        [SerializeField] float _pelletSpeed = 9f;
        [SerializeField] float _pelletSeconds = 2.5f;
        [SerializeField] float _wobble = 0.35f;

        public enum Move { Hover, Spit }
        public Move State { get; private set; }
        Vector2 _home;
        float _t, _cooldown, _phase;

        /// <summary>Where it hovers when she is out of reach: its position when the room was built.</summary>
        public Vector2 Home => _home;
        /// <summary>How far it keeps from her: past the quill, within the Longstroke.</summary>
        public float StandOff => _standOff;
        public bool IsTelegraphing => State == Move.Spit;
        /// <summary>The last pellet it spat.</summary>
        public EnemyProjectile LastPellet { get; private set; }
        public int Spat { get; private set; }
        public event Action<Pulpwasp, EnemyProjectile> SpatAt;

        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip : State == Move.Spit ? "spit" : base.Clip;
        public override float ClipProgress => State == Move.Spit ? Mathf.Clamp01(_t / _spitSeconds) : -1f;

        protected override void Awake()
        {
            base.Awake();
            _answer = EnemyAnswer.Longstroke;
            Body.gravityScale = 0f;
            _home = transform.position;
            _phase = UnityEngine.Random.Range(0f, 6.28f);
        }

        protected override void OnRevived()
        {
            State = Move.Hover;
            _t = 0f; _cooldown = 0f;
            transform.position = _home;
        }

        protected override void Tick(float dt)
        {
            _t += dt;
            _cooldown -= dt;
            Vector2 pos = transform.position;
            switch (State)
            {
                case Move.Hover:
                {
                    float bob = _wobble * Mathf.Sin(Time.time * 3f + _phase);
                    if (Wren == null || (Wren.Position - pos).sqrMagnitude > _range * _range)
                    {
                        var home = _home - pos;
                        Body.linearVelocity = (home.magnitude > 0.3f ? home.normalized * _hoverSpeed : Vector2.zero) + Vector2.up * bob;
                        break;
                    }
                    float dx = Wren.Position.x - pos.x;
                    Face(dx >= 0f ? 1 : -1);
                    // Hold the stand-off along the floor, and its own height above her.
                    float vx = Mathf.Abs(dx) < _standOff - 0.4f ? -Mathf.Sign(dx) * _hoverSpeed
                             : Mathf.Abs(dx) > _standOff + 0.6f ? Mathf.Sign(dx) * _hoverSpeed : 0f;
                    float wantY = Wren.Position.y + 1.6f;
                    float vy = Mathf.Clamp((wantY - pos.y) * 2f, -_hoverSpeed, _hoverSpeed) + bob;
                    Body.linearVelocity = new Vector2(vx, vy);
                    if (_cooldown <= 0f && Mathf.Abs(dx) <= _standOff + 2f)
                    {
                        Body.linearVelocity = Vector2.zero;
                        State = Move.Spit;
                        _t = 0f;
                        Tell(AttackKind.Strike);
                    }
                    break;
                }
                case Move.Spit:
                {
                    Body.linearVelocity = Vector2.zero;
                    if (_t >= _spitSeconds)
                    {
                        var at = pos + new Vector2(Facing * 0.45f, -0.1f);
                        var target = Wren != null ? Wren.Position + Vector2.up * 0.6f : at + Vector2.right * Facing;
                        var dir = (target - at).sqrMagnitude > 0.01f ? (target - at).normalized : Vector2.right * Facing;
                        LastPellet = EnemyProjectile.Spawn("Pulp", transform.parent, at, dir * _pelletSpeed, new Vector2(0.35f, 0.35f), ContactDamage, null);
                        LastPellet.LifeLeft = _pelletSeconds;
                        Spat++;
                        SpatAt?.Invoke(this, LastPellet);
                        State = Move.Hover;
                        _cooldown = _spitCooldown;
                        _t = 0f;
                    }
                    break;
                }
            }
        }

        protected override Color TintColor() => State == Move.Spit ? new Color(0.90f, 0.86f, 0.74f) : new Color(0.62f, 0.50f, 0.32f);
    }
}
