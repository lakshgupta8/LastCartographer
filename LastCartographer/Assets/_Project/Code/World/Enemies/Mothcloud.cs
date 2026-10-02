using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The Verdance's swarm (CMB-09, combat doc 7 "Blot the swarms"): a cloud of lantern-moths drawn to Wren's lantern.
    /// It drifts after her, and close in it flares (the telegraph: the eye-spots shown, a tell on the first frame) and
    /// darts through where she stood. The quill passes through a cloud: no strike lands until a Blot has gathered it
    /// (the Blot's slow), and then any strike does. No gravity.
    /// Answer: Blot.
    /// </summary>
    public sealed class Mothcloud : Enemy
    {
        [SerializeField] float _driftSpeed = 2.2f;
        [SerializeField] float _range = 7f;
        [SerializeField] float _dartRange = 2.6f;
        [SerializeField] float _flareSeconds = 0.3f;
        [SerializeField] float _dartSpeed = 7f;
        [SerializeField] float _dartSeconds = 0.4f;
        [SerializeField] float _dartCooldown = 1.8f;
        [SerializeField] float _wobble = 0.5f;

        public enum Move { Drift, Flare, Dart }
        public Move State { get; private set; }
        Vector2 _home, _dartDir;
        float _t, _cooldown, _phase;

        /// <summary>Where it hangs when she is out of reach: its position when the room was built.</summary>
        public Vector2 Home => _home;
        public bool IsTelegraphing => State == Move.Flare;
        /// <summary>A Blot has pulled the cloud tight: while it holds, the strikes land.</summary>
        public bool IsGathered => IsSlowed;

        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip
            : IsGathered ? "gather" : State == Move.Flare ? "flare" : State == Move.Dart ? "dart" : base.Clip;
        public override float ClipProgress => State == Move.Flare ? Mathf.Clamp01(_t / _flareSeconds) : -1f;

        protected override void Awake()
        {
            base.Awake();
            _answer = EnemyAnswer.Blot;
            Body.gravityScale = 0f;
            _home = transform.position;
            _phase = Random.Range(0f, 6.28f);
        }

        protected override void OnRevived()
        {
            State = Move.Drift;
            _t = 0f; _cooldown = 0f;
            transform.position = _home;
        }

        /// <summary>The quill passes through a cloud; only a gathered one can be struck.</summary>
        protected override bool AcceptsHit(in HitInfo hit) => IsGathered;

        protected override void Tick(float dt)
        {
            _t += dt;
            _cooldown -= dt;
            Vector2 pos = transform.position;
            switch (State)
            {
                case Move.Drift:
                {
                    var bob = Vector2.up * (_wobble * Mathf.Sin(Time.time * 2.4f + _phase));
                    Vector2 goal = _home;
                    if (Wren != null)
                    {
                        var toWren = Wren.Position + Vector2.up * 0.6f - pos;
                        if (toWren.sqrMagnitude < _range * _range) goal = Wren.Position + Vector2.up * 0.6f;
                        Face(toWren.x >= 0f ? 1 : -1);
                        if (_cooldown <= 0f && toWren.sqrMagnitude < _dartRange * _dartRange)
                        {
                            Body.linearVelocity = Vector2.zero;
                            _dartDir = toWren.sqrMagnitude > 0.01f ? toWren.normalized : Vector2.right * Facing;
                            State = Move.Flare;
                            _t = 0f;
                            Tell(AttackKind.Strike);
                            return;
                        }
                    }
                    var to = goal - pos;
                    Body.linearVelocity = (to.magnitude > 0.3f ? to.normalized * _driftSpeed : Vector2.zero) + bob;
                    break;
                }
                case Move.Flare:
                {
                    Body.linearVelocity = Vector2.zero;
                    if (_t >= _flareSeconds)
                    {
                        if (Wren != null) { var d = Wren.Position + Vector2.up * 0.6f - pos; if (d.sqrMagnitude > 0.01f) _dartDir = d.normalized; }
                        State = Move.Dart;
                        _t = 0f;
                    }
                    break;
                }
                case Move.Dart:
                {
                    Body.linearVelocity = _dartDir * _dartSpeed;
                    if (_t >= _dartSeconds)
                    {
                        State = Move.Drift;
                        _cooldown = _dartCooldown;
                        _t = 0f;
                    }
                    break;
                }
            }
        }

        protected override Color TintColor() => State == Move.Flare ? new Color(0.95f, 0.66f, 0.28f) : IsGathered ? new Color(0.55f, 0.48f, 0.36f) : new Color(0.86f, 0.80f, 0.62f);
    }
}
