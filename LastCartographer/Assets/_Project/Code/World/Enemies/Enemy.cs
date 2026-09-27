using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>Which of Wren's tools is the intended solution (combat doc 7). Purely descriptive for design and tests.</summary>
    public enum EnemyAnswer { AnyHit, Pogo, Longstroke, Blot, Parry }

    /// <summary>
    /// Base for every enemy (CMB-08): health, hurt flash, knockback, contact damage to Wren, death.
    /// Subclasses implement behaviour in Tick() and can veto hits (a shelled crab ignores side hits).
    /// Enemies are dynamic 2D bodies on the Enemy layer; Wren is kinematic and never pushes them
    /// (the Player/Enemy layer pair is set to ignore collisions in project setup).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public abstract class Enemy : MonoBehaviour, IHittable
    {
        [Header("Enemy")]
        [SerializeField] int _maxHealth = 3;
        [SerializeField] int _contactDamage = 1;
        [SerializeField] float _hitKnockback = 4f;
        [SerializeField] int _hurtstunFrames = 6;
        [SerializeField] EnemyAnswer _answer = EnemyAnswer.AnyHit;
        [SerializeField] LayerMask _playerMask;
        [SerializeField] float _deathSeconds = 0.25f;

        public int MaxHealth => _maxHealth;
        public int Health { get; private set; }
        public bool IsDead => Health <= 0;
        public EnemyAnswer Answer => _answer;
        public int HurtstunLeft { get; private set; }
        public event Action<Enemy, HitInfo> WasHit;
        public event Action<Enemy> Died;

        protected Rigidbody2D Body { get; private set; }
        protected Collider2D Collider { get; private set; }
        protected Renderer Visual { get; private set; }
        protected WrenController Wren { get; private set; }
        protected int Facing = -1;

        MaterialPropertyBlock _mpb;
        float _flashUntil, _deathT = -1f;
        Vector3 _baseScale;
        readonly Collider2D[] _overlaps = new Collider2D[4];
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color FlashColor = Color.white;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Body.freezeRotation = true;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Collider = GetComponent<Collider2D>();
            Visual = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
            Health = _maxHealth;
            if (_playerMask.value == 0) _playerMask = LayerMask.GetMask("Player");
        }

        protected virtual void Start()
        {
            Wren = FindFirstObjectByType<WrenController>();
        }

        protected virtual void FixedUpdate()
        {
            if (_deathT >= 0f)
            {
                _deathT += Time.fixedDeltaTime;
                float k = Mathf.Clamp01(_deathT / _deathSeconds);
                transform.localScale = _baseScale * (1f - k);
                if (k >= 1f) Destroy(gameObject);
                return;
            }
            if (Wren == null) Wren = FindFirstObjectByType<WrenController>();
            if (HurtstunLeft > 0) { HurtstunLeft--; return; }
            Tick(Time.fixedDeltaTime);
            ContactCheck();
        }

        /// <summary>Behaviour step, skipped during hurtstun and death.</summary>
        protected abstract void Tick(float dt);

        /// <summary>Whether this hit lands. Override for shells, undrawn smudges, armour.</summary>
        protected virtual bool AcceptsHit(in HitInfo hit) => true;

        public bool TakeHit(in HitInfo hit)
        {
            if (IsDead || _deathT >= 0f) return false;
            if (!AcceptsHit(hit)) { OnHitBlocked(hit); return false; }
            Health = Mathf.Max(0, Health - hit.Damage);
            _flashUntil = Time.time + 0.1f;
            HurtstunLeft = _hurtstunFrames;
            var away = hit.Direction.sqrMagnitude > 0f ? hit.Direction.normalized : Vector2.right;
            Body.linearVelocity = new Vector2(away.x * _hitKnockback, Mathf.Max(Body.linearVelocity.y, away.y > 0f ? _hitKnockback : 2f));
            WasHit?.Invoke(this, hit);
            if (Health == 0) Die();
            return true;
        }

        protected virtual void OnHitBlocked(in HitInfo hit) { _flashUntil = Time.time + 0.05f; }

        protected virtual void Die()
        {
            _deathT = 0f;
            Body.simulated = false;
            Collider.enabled = false;
            Died?.Invoke(this);
        }

        void ContactCheck()
        {
            if (_contactDamage <= 0 || Wren == null) return;
            var b = Collider.bounds;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = _playerMask, useTriggers = false };
            int n = Physics2D.OverlapBox(b.center, b.size + Vector3.one * 0.05f, 0f, filter, _overlaps);
            for (int i = 0; i < n; i++)
            {
                var vitals = _overlaps[i].GetComponentInParent<WrenVitals>();
                if (vitals == null) continue;
                vitals.Damage(_contactDamage, transform.position);
                break;
            }
        }

        protected void Face(int dir)
        {
            if (dir == 0 || dir == Facing) return;
            Facing = dir;
            if (Visual != null)
            {
                var s = Visual.transform.localScale;
                s.x = Mathf.Abs(s.x) * dir;
                Visual.transform.localScale = s;
            }
        }

        protected bool GroundAhead(float lookAhead, float depth, LayerMask groundMask)
        {
            var b = Collider.bounds;
            var origin = new Vector2(b.center.x + Facing * (b.extents.x + lookAhead), b.min.y + 0.05f);
            return Physics2D.Raycast(origin, Vector2.down, depth, groundMask).collider != null;
        }

        protected bool WallAhead(float distance, LayerMask groundMask)
        {
            var b = Collider.bounds;
            var origin = new Vector2(b.center.x, b.center.y);
            return Physics2D.Raycast(origin, new Vector2(Facing, 0f), b.extents.x + distance, groundMask).collider != null;
        }

        protected virtual void Update()
        {
            if (Visual == null) return;
            Visual.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, Time.time < _flashUntil ? FlashColor : TintColor());
            Visual.SetPropertyBlock(_mpb);
        }

        /// <summary>Resting tint for the placeholder visual.</summary>
        protected virtual Color TintColor() => new Color(0.55f, 0.30f, 0.28f);
    }
}
