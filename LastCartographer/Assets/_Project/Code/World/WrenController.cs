using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Wren's kinematic 2D controller. No Rigidbody forces: we integrate velocity ourselves and
    /// resolve collisions with box casts against the Ground mask, axis by axis, at a fixed 60 Hz.
    /// Frame data and heights come from docs/design/combat-and-movement.md sections 1 to 3.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
    public sealed class WrenController : MonoBehaviour
    {
        [Header("Run")]
        public float runSpeed = 9f;
        public int decelFrames = 4;

        [Header("Jump")]
        public float maxJumpHeight = 4.5f;
        public float minJumpHeight = 2.0f;
        public float timeToApex = 0.38f;
        public float fallGravityMultiplier = 1.3f;
        public float maxFallSpeed = 30f;
        public int apexHangFrames = 3;
        public float apexThreshold = 1.5f;
        public int coyoteFrames = 5;

        [Header("Wingbeat (air dash)")]
        public float dashDistance = 5f;
        public int dashFrames = 8;
        public int dashInvulnStart = 2;
        public int dashInvulnEnd = 5;

        [Header("Talonhold (wall)")]
        public float clingSeconds = 2.5f;
        public float slideSpeed = 4f;
        public Vector2 wallJumpVelocity = new Vector2(12f, 20f);
        public int wallJumpLockFrames = 8;

        [Header("Pogo")]
        public float pogoHeight = 3f;

        [Header("Collision")]
        public LayerMask groundMask;
        public float skin = 0.02f;

        public IWrenInput Input { get; set; }
        public AbilitySet Abilities { get; set; }
        /// <summary>While true (dialogue, cutscenes) input is ignored and Wren stops; gravity still applies.</summary>
        public bool Frozen { get; set; }

        public Vector2 Velocity => _vel;
        public bool IsGrounded => _grounded;
        public int Facing => _facing;
        public bool IsDashing => _dashFramesLeft > 0;
        public bool IsClinging => _clinging;
        public int WallDirection => _wallDir;
        public bool IsInvulnerable => IsDashing && _dashElapsed >= dashInvulnStart && _dashElapsed <= dashInvulnEnd;
        public Vector2 Position => _rb != null ? _rb.position : (Vector2)transform.position;

        public event Action Jumped, Landed, Dashed, Pogoed;

        Rigidbody2D _rb;
        BoxCollider2D _box;
        ContactFilter2D _filter;
        readonly RaycastHit2D[] _hits = new RaycastHit2D[8];

        Vector2 _vel;
        bool _grounded, _clinging;
        int _facing = 1;
        int _coyoteLeft, _apexLeft, _inputLock;
        bool _jumpCutApplied;
        bool _dashAvailable = true;
        int _dashFramesLeft, _dashElapsed, _dashDir, _wallDir;
        float _clingTimer;
        float _gravity, _jumpVelocity, _minJumpVelocity, _pogoVelocity;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.simulated = true;
            _rb.interpolation = RigidbodyInterpolation2D.None;
            _rb.useFullKinematicContacts = true;   // kinematic bodies only touch static triggers with this on
            _box = GetComponent<BoxCollider2D>();
            if (Abilities == null) Abilities = GetComponent<AbilitySet>();
            if (Input == null) Input = GetComponent<IWrenInput>();
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = groundMask, useTriggers = false };
            Recompute();
        }

        /// <summary>Derive gravity and launch speeds from the designer-facing heights.</summary>
        public void Recompute()
        {
            _gravity = 2f * maxJumpHeight / (timeToApex * timeToApex);
            _jumpVelocity = _gravity * timeToApex;
            _minJumpVelocity = Mathf.Sqrt(2f * _gravity * minJumpHeight);
            _pogoVelocity = Mathf.Sqrt(2f * _gravity * pogoHeight);
        }

        public void Teleport(Vector2 position)
        {
            _rb.position = position;
            transform.position = position;
            _vel = Vector2.zero;
        }

        /// <summary>Called by the strike when a down-strike lands: bounce and refresh the dash.</summary>
        public void Pogo()
        {
            _vel.y = _pogoVelocity;
            _dashAvailable = true;
            _dashFramesLeft = 0;
            _jumpCutApplied = true;
            _apexLeft = apexHangFrames;
            Pogoed?.Invoke();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 move = Input != null && !Frozen ? Input.Move : Vector2.zero;
            bool wasGrounded = _grounded;
            if (Frozen && Input != null)
            {
                // Drain buffered presses so nothing fires the moment dialogue ends.
                Input.ConsumeJump(); Input.ConsumeDash(); Input.ConsumeAttack();
            }

            _grounded = Probe(Vector2.down);
            _wallDir = !_grounded ? (Probe(Vector2.right) ? 1 : Probe(Vector2.left) ? -1 : 0) : 0;

            if (_grounded)
            {
                _coyoteLeft = coyoteFrames;
                _dashAvailable = true;
                _clingTimer = 0f;
                if (_vel.y < 0f) _vel.y = 0f;
            }

            if (_inputLock > 0) _inputLock--;
            _clinging = false;

            if (_dashFramesLeft > 0)
            {
                _dashFramesLeft--;
                _dashElapsed++;
                _vel = new Vector2(_dashDir * dashDistance / (dashFrames * dt), 0f);
                if (_dashFramesLeft == 0) _vel.x = _dashDir * runSpeed;
            }
            else
            {
                // Horizontal: instant accel, short decel (combat doc 1).
                if (_inputLock == 0)
                {
                    if (Mathf.Abs(move.x) > 0.1f)
                    {
                        _facing = move.x > 0f ? 1 : -1;
                        _vel.x = _facing * runSpeed;
                    }
                    else _vel.x = Mathf.MoveTowards(_vel.x, 0f, runSpeed / Mathf.Max(1, decelFrames));
                }

                // Talonhold: cling while pushing into a wall in the air.
                bool hasTalon = Abilities != null && Abilities.Has(Ability.Talonhold);
                if (!_grounded && _wallDir != 0 && hasTalon && Mathf.Abs(move.x) > 0.1f && Mathf.Sign(move.x) == _wallDir)
                {
                    _clinging = true;
                    _clingTimer += dt;
                    _dashAvailable = true;
                    _vel.y = _clingTimer < clingSeconds ? 0f : -slideSpeed;
                }

                // Jump: ground, coyote, or wall. Only consume the buffer when we can act on it.
                bool canGroundJump = _grounded || _coyoteLeft > 0;
                bool canWallJump = !_grounded && _wallDir != 0 && hasTalon;
                if ((canGroundJump || canWallJump) && Input != null && Input.ConsumeJump())
                {
                    if (canGroundJump)
                    {
                        _vel.y = _jumpVelocity;
                    }
                    else
                    {
                        _vel = new Vector2(-_wallDir * wallJumpVelocity.x, wallJumpVelocity.y);
                        _facing = -_wallDir;
                        _inputLock = wallJumpLockFrames;
                        _clinging = false;
                    }
                    _grounded = false;
                    _coyoteLeft = 0;
                    _jumpCutApplied = false;
                    _apexLeft = apexHangFrames;
                    Jumped?.Invoke();
                }

                // Variable height: releasing early clamps upward speed to the min-height launch speed.
                if (!_jumpCutApplied && _vel.y > 0f && !(Input != null && Input.JumpHeld))
                {
                    _vel.y = Mathf.Min(_vel.y, _minJumpVelocity);
                    _jumpCutApplied = true;
                }

                // Gravity with apex hang and heavier fall.
                if (!_clinging && !_grounded)
                {
                    float g = _gravity;
                    if (_vel.y < 0f) g *= fallGravityMultiplier;
                    if (_apexLeft > 0 && Mathf.Abs(_vel.y) < apexThreshold) { g *= 0.5f; _apexLeft--; }
                    _vel.y -= g * dt;
                    if (_vel.y < -maxFallSpeed) _vel.y = -maxFallSpeed;
                }

                // Wingbeat: one per airtime; resets on ground, wall, or pogo.
                if (_dashAvailable && Abilities != null && Abilities.Has(Ability.Wingbeat) && Input != null && Input.ConsumeDash())
                {
                    _dashAvailable = false;
                    _dashFramesLeft = dashFrames;
                    _dashElapsed = 0;
                    _dashDir = Mathf.Abs(move.x) > 0.1f ? (move.x > 0f ? 1 : -1) : _facing;
                    _facing = _dashDir;
                    _vel = new Vector2(_dashDir * dashDistance / (dashFrames * dt), 0f);
                    _apexLeft = 0;
                    Dashed?.Invoke();
                }
            }

            // Coyote time counts down after the jump check, so all coyoteFrames are usable.
            if (!_grounded && _coyoteLeft > 0) _coyoteLeft--;

            MoveBy(_vel * dt);

            if (!wasGrounded && _grounded) Landed?.Invoke();
            Input?.Tick();
        }

        void MoveBy(Vector2 delta)
        {
            MoveAxis(new Vector2(delta.x, 0f));
            MoveAxis(new Vector2(0f, delta.y));
        }

        void MoveAxis(Vector2 delta)
        {
            float dist = delta.magnitude;
            if (dist <= 0f) return;
            Vector2 dir = delta / dist;
            int n = Cast(dir, dist + skin);
            float allowed = dist;
            for (int i = 0; i < n; i++)
            {
                float d = _hits[i].distance - skin;
                if (d < allowed) allowed = Mathf.Max(0f, d);
            }
            _rb.position += dir * allowed;
            if (allowed < dist)
            {
                if (dir.x != 0f) _vel.x = 0f;
                else _vel.y = 0f;
            }
        }

        bool Probe(Vector2 dir) => Cast(dir, skin * 2f) > 0;

        int Cast(Vector2 dir, float distance)
        {
            var scale = transform.lossyScale;
            var size = new Vector2(_box.size.x * Mathf.Abs(scale.x), _box.size.y * Mathf.Abs(scale.y));
            size -= Vector2.one * (skin * 2f);
            var origin = _rb.position + new Vector2(_box.offset.x * scale.x, _box.offset.y * scale.y);
            _filter.SetLayerMask(groundMask);   // mask may be assigned after Awake (tests, tooling)
            return Physics2D.BoxCast(origin, size, 0f, dir, _filter, _hits, distance);
        }
    }
}
