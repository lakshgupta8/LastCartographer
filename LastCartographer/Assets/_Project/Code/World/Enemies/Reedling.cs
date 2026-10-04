using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A half-drawn marsh chick of the Reedmother's brood (CMB-09; bible 6.2, the Pale Iris Fields). They come in
    /// clutches of three: each scurries after her in hops, keeping a step from its siblings, rears (the telegraph: a
    /// tell on the first frame) and lunges to peck; and when one is struck the clutch scatters for a moment and
    /// regroups. One is fodder; the clutch is a swarm, and a Blot's slow holds all three where the strikes find them.
    /// Walks the floor, with gravity. Answer: Blot.
    /// </summary>
    public sealed class Reedling : Enemy
    {
        [SerializeField] float _scurrySpeed = 3f;
        [SerializeField] float _hopSpeed = 4.5f;
        [SerializeField] float _hopEvery = 0.5f;
        [SerializeField] float _range = 7f;
        [SerializeField] float _peckReach = 1.3f;
        [SerializeField] float _peckSeconds = 0.25f;
        [SerializeField] float _lungeSeconds = 0.3f;
        [SerializeField] float _lungeSpeed = 7f;
        [SerializeField] float _peckCooldown = 1.4f;
        [SerializeField] float _scatterSeconds = 0.6f;
        [SerializeField] float _spacing = 0.8f;
        [SerializeField] float _clutchReach = 6f;
        [SerializeField] LayerMask _groundMask;

        public enum Move { Scurry, Peck, Lunge, Scatter }
        public Move State { get; private set; }
        float _t, _wait, _hopT;
        static readonly List<Reedling> s_live = new List<Reedling>();

        public bool IsTelegraphing => State == Move.Peck;
        /// <summary>The siblings within the clutch's reach, itself left out.</summary>
        public IEnumerable<Reedling> Clutch
        {
            get
            {
                foreach (var r in s_live)
                    if (r != this && r != null && !r.IsDead && (r.transform.position - transform.position).sqrMagnitude < _clutchReach * _clutchReach) yield return r;
            }
        }

        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip
            : State == Move.Peck ? "peck" : State == Move.Lunge ? "lunge" : State == Move.Scatter ? "move" : base.Clip;
        public override float ClipProgress => State == Move.Peck ? Mathf.Clamp01(_t / _peckSeconds) : -1f;

        protected override void Awake()
        {
            base.Awake();
            _answer = EnemyAnswer.Blot;
            if (_groundMask.value == 0) _groundMask = LayerMask.GetMask("Ground");
        }

        void OnEnable() { if (!s_live.Contains(this)) s_live.Add(this); }
        void OnDisable() { s_live.Remove(this); }

        protected override void OnRevived() { State = Move.Scurry; _t = 0f; _wait = 0f; }

        /// <summary>A struck chick scatters, and so does its clutch.</summary>
        protected override void OnHealthChanged()
        {
            if (IsDead) return;
            Scatter();
            foreach (var r in Clutch) r.Scatter();
        }

        public void Scatter()
        {
            if (IsDead || State == Move.Scatter) return;
            State = Move.Scatter;
            _t = 0f;
        }

        bool Grounded => GroundAhead(-0.2f, 0.45f, _groundMask) || GroundAhead(0.2f, 0.45f, _groundMask);
        bool EdgeOrWallAhead => !GroundAhead(0.15f, 0.6f, _groundMask) || WallAhead(0.1f, _groundMask);

        float Separation()
        {
            float push = 0f;
            foreach (var r in Clutch)
            {
                float d = transform.position.x - r.transform.position.x;
                if (Mathf.Abs(d) < _spacing) push += (d >= 0f ? 1f : -1f) * (_spacing - Mathf.Abs(d));
            }
            return push;
        }

        protected override void Tick(float dt)
        {
            _t += dt;
            _wait -= dt;
            _hopT += dt;
            float vy = Body.linearVelocity.y;
            switch (State)
            {
                case Move.Scurry:
                {
                    if (Wren == null) { Body.linearVelocity = new Vector2(0f, vy); break; }
                    float dx = Wren.Position.x - transform.position.x;
                    bool near = Mathf.Abs(dx) < _range && Mathf.Abs(Wren.Position.y - transform.position.y) < 2.5f;
                    if (near) Face(dx >= 0f ? 1 : -1);
                    if (near && _wait <= 0f && Mathf.Abs(dx) < _peckReach && Grounded)
                    {
                        Body.linearVelocity = new Vector2(0f, vy);
                        State = Move.Peck;
                        _t = 0f;
                        Tell(AttackKind.Strike);
                        return;
                    }
                    float vx = near && Mathf.Abs(dx) > 0.5f && !EdgeOrWallAhead ? Facing * _scurrySpeed : 0f;
                    vx += Separation() * 2f;
                    if (near && Grounded && _hopT >= _hopEvery) { _hopT = 0f; vy = _hopSpeed; }
                    Body.linearVelocity = new Vector2(vx, vy);
                    break;
                }
                case Move.Peck:
                {
                    Body.linearVelocity = new Vector2(0f, vy);
                    if (_t >= _peckSeconds) { State = Move.Lunge; _t = 0f; }
                    break;
                }
                case Move.Lunge:
                {
                    Body.linearVelocity = new Vector2(EdgeOrWallAhead ? 0f : Facing * _lungeSpeed, vy);
                    if (_t >= _lungeSeconds) { State = Move.Scurry; _t = 0f; _wait = _peckCooldown; }
                    break;
                }
                case Move.Scatter:
                {
                    int away = Wren != null ? (Wren.Position.x >= transform.position.x ? -1 : 1) : -Facing;
                    Body.linearVelocity = new Vector2(EdgeOrWallAhead && away == Facing ? 0f : away * _scurrySpeed * 1.3f, vy);
                    if (_t >= _scatterSeconds) { State = Move.Scurry; _t = 0f; }
                    break;
                }
            }
        }

        protected override Color TintColor() => State == Move.Peck ? new Color(0.80f, 0.62f, 0.36f) : new Color(0.90f, 0.86f, 0.72f);
    }
}
