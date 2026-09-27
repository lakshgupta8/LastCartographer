using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// An ink-beast made of something forgotten (bible 6). Flickers between drawn and undrawn on a
    /// fixed period; only drawn frames can be hit or hurt Wren. Lunges when drawn and close.
    /// The visual's _Ink parameter follows the state so the fade shader does the work. Answer: any hit, timed.
    /// </summary>
    public class Smudge : Enemy, IRevealable
    {
        [SerializeField] float _drawnSeconds = 0.9f;
        [SerializeField] float _undrawnSeconds = 0.7f;
        [SerializeField] float _lungeRange = 4f;
        [SerializeField] float _lungeSpeed = 9f;
        [SerializeField] float _driftSpeed = 1.2f;
        [SerializeField] float _inkFadeSpeed = 6f;

        float _t, _ink = 1f, _lungeT;
        bool _forced;
        bool _forcedValue;
        MaterialPropertyBlock _inkBlock;
        static readonly int InkId = Shader.PropertyToID("_Ink");

        public bool IsDrawn => _forced ? _forcedValue : _revealLeft > 0f || (_t % (_drawnSeconds + _undrawnSeconds)) < _drawnSeconds;
        public float InkLevel => _ink;

        /// <summary>Tests and cutscenes: pin the state.</summary>
        public void ForceDrawn(bool drawn) { _forced = true; _forcedValue = drawn; }
        public void ReleaseForce() { _forced = false; }

        float _revealLeft;
        /// <summary>Field lantern: pinned drawn for a while, then back to the flicker.</summary>
        public void Reveal(float seconds) { _revealLeft = Mathf.Max(_revealLeft, seconds); }
        public bool IsRevealed => _revealLeft > 0f;

        protected override void Awake()
        {
            base.Awake();
            Body.gravityScale = 0f;
            _inkBlock = new MaterialPropertyBlock();
        }

        protected override bool AcceptsHit(in HitInfo hit) => IsDrawn;

        protected override void Tick(float dt)
        {
            _t += dt;
            _lungeT -= dt;
            Vector2 pos = transform.position;
            if (!IsDrawn)
            {
                Body.linearVelocity = Vector2.zero;
                return;
            }
            if (Wren == null) return;
            var to = Wren.Position + Vector2.up * 0.6f - pos;
            Face(to.x >= 0f ? 1 : -1);
            if (_lungeT <= 0f && to.magnitude < _lungeRange)
            {
                Body.linearVelocity = to.normalized * _lungeSpeed;
                _lungeT = _drawnSeconds;
            }
            else if (_lungeT <= 0f)
            {
                Body.linearVelocity = to.normalized * _driftSpeed;
            }
            else
            {
                Body.linearVelocity *= 0.9f;
            }
        }

        protected override void Update()
        {
            base.Update();
            _ink = Mathf.MoveTowards(_ink, IsDrawn ? 1f : 0.05f, Time.deltaTime * _inkFadeSpeed);
            if (Visual == null) return;
            Visual.GetPropertyBlock(_inkBlock);
            _inkBlock.SetFloat(InkId, _ink);
            Visual.SetPropertyBlock(_inkBlock);
        }

        // Contact damage only while drawn: the base check runs after Tick, so gate it here.
        protected override void FixedUpdate()
        {
            if (_revealLeft > 0f) _revealLeft -= Time.fixedDeltaTime;
            if (!IsDrawn && !IsDead)
            {
                _t += Time.fixedDeltaTime;   // keep the clock running through the undrawn phase
                Body.linearVelocity = Vector2.zero;
                return;
            }
            base.FixedUpdate();
        }

        protected override Color TintColor() => new Color(0.12f, 0.12f, 0.16f);
    }
}
