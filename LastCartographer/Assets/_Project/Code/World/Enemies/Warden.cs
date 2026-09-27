using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A Guild Warden (bible 3.1, combat doc 7 long-leg family): patrols an anchored town, turns at walls and
    /// edges, and when Wren is in front lowers the sighting-lance (the telegraph) and thrusts. The thrust is a
    /// box ahead of the body for a few frames; contact with the body itself also hurts. Answer: Parry.
    /// </summary>
    public sealed class Warden : Enemy
    {
        [SerializeField] float _walkSpeed = 1.8f;
        [SerializeField] float _lanceRange = 3.2f;
        [SerializeField] float _lanceReach = 2.4f;
        [SerializeField] float _lanceHeight = 0.7f;
        [SerializeField] int _telegraphFrames = 14;
        [SerializeField] int _thrustFrames = 6;
        [SerializeField] int _recoverFrames = 30;
        [SerializeField] float _thrustCooldown = 1.4f;
        [SerializeField] int _lanceDamage = 1;
        [SerializeField] LayerMask _groundMask;

        public enum Move { Patrol, Telegraph, Thrust, Recover }
        public Move State { get; private set; }
        public int Thrusts { get; private set; }
        public bool IsTelegraphing => State == Move.Telegraph;
        public int Dir => Facing;

        int _frames;
        float _cooldown;
        bool _hitThisThrust;
        Vector3 _baseScale;
        readonly Collider2D[] _hits = new Collider2D[4];

        protected override void Awake()
        {
            base.Awake();
            if (_groundMask.value == 0) _groundMask = LayerMask.GetMask("Ground");
            _answer = EnemyAnswer.Parry;
            _baseScale = Visual != null ? Visual.transform.localScale : Vector3.one;
        }

        protected override void Tick(float dt)
        {
            _cooldown -= dt;
            switch (State)
            {
                case Move.Patrol:
                    if (!GroundAhead(0.1f, 0.6f, _groundMask) || WallAhead(0.1f, _groundMask)) Face(-Facing);
                    if (Wren != null && _cooldown <= 0f)
                    {
                        var to = Wren.Position - (Vector2)transform.position;
                        if (Mathf.Abs(to.x) < _lanceRange && Mathf.Abs(to.y) < 1.6f)
                        {
                            Face(to.x >= 0f ? 1 : -1);
                            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                            State = Move.Telegraph;
                            _frames = 0;
                            return;
                        }
                    }
                    Body.linearVelocity = new Vector2(Facing * _walkSpeed, Body.linearVelocity.y);
                    break;

                case Move.Telegraph:
                    Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                    if (++_frames >= _telegraphFrames) { State = Move.Thrust; _frames = 0; _hitThisThrust = false; Thrusts++; }
                    break;

                case Move.Thrust:
                    Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                    LanceCheck();
                    if (++_frames >= _thrustFrames) { State = Move.Recover; _frames = 0; }
                    break;

                case Move.Recover:
                    Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                    if (++_frames >= _recoverFrames) { State = Move.Patrol; _cooldown = _thrustCooldown; }
                    break;
            }
        }

        void LanceCheck()
        {
            if (_hitThisThrust || Wren == null) return;
            var b = Collider.bounds;
            var centre = new Vector2(b.center.x + Facing * (b.extents.x + _lanceReach * 0.5f), b.center.y + 0.2f);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Player"), useTriggers = false };
            int n = Physics2D.OverlapBox(centre, new Vector2(_lanceReach, _lanceHeight), 0f, filter, _hits);
            for (int i = 0; i < n; i++)
            {
                var vitals = _hits[i].GetComponentInParent<WrenVitals>();
                if (vitals == null) continue;
                if (vitals.Damage(_lanceDamage, transform.position)) _hitThisThrust = true;
                return;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (Visual == null || IsDying) return;
            // The lance: lean into the telegraph, stretch on the thrust (on the sprite; Face owns its sign).
            float lean = State == Move.Telegraph ? 0.85f : State == Move.Thrust ? 1.5f : 1f;
            var s = Visual.transform.localScale;
            Visual.transform.localScale = new Vector3(Facing * Mathf.Abs(_baseScale.x) * lean, s.y, s.z);
        }

        protected override Color TintColor() => new Color(0.16f, 0.22f, 0.42f);
    }
}
