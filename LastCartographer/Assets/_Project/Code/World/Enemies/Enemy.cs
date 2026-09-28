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
        [SerializeField] protected EnemyAnswer _answer = EnemyAnswer.AnyHit;
        [SerializeField] LayerMask _playerMask;
        [SerializeField] float _deathSeconds = 0.25f;

        public int MaxHealth => _maxHealth;
        public int Health { get; private set; }
        public bool IsDead => Health <= 0;
        public EnemyAnswer Answer => _answer;
        public int HurtstunLeft { get; private set; }
        /// <summary>1 in full colour, 0 grey. The Remnant Charter's strikes drain it; it comes back when they stop.</summary>
        public float Colour { get; private set; } = 1f;
        public bool IsGrey => Colour <= 0.001f;
        /// <summary>Seconds after the last drain before colour starts to come back, and how long the way back takes.</summary>
        public float RecolourDelay { get; set; } = 3f;
        public float RecolourSeconds { get; set; } = 2f;
        /// <summary>A grey enemy is slowed this long by every strike (the Remnant Charter's answer).</summary>
        public float GreySlowSeconds { get; set; } = 1.5f;
        float _recolourWait;
        /// <summary>Seconds of slow remaining (Blot). Velocity is scaled by SlowFactor while active.</summary>
        public float SlowLeft { get; private set; }
        public bool IsSlowed => SlowLeft > 0f;
        public float SlowFactor { get; set; } = 0.35f;
        public event Action<Enemy, HitInfo> WasHit;
        public event Action<Enemy> Died;
        /// <summary>Every enemy death in the game (commission counters, later stats).</summary>
        public static event Action<Enemy> AnyDied;
        /// <summary>Roster name used by counters ("kill.MarshCrab"); the type name unless overridden.</summary>
        public virtual string Family => GetType().Name;

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
                if (k >= 1f)
                {
                    if (DestroyOnDeath) Destroy(gameObject);
                    else { _deathT = -1f; gameObject.SetActive(false); }
                }
                return;
            }
            if (Wren == null) Wren = FindFirstObjectByType<WrenController>();
            if (MarkLeft > 0f) MarkLeft -= Time.fixedDeltaTime;
            if (_recolourWait > 0f) _recolourWait -= Time.fixedDeltaTime;
            else if (Colour < 1f) Colour = Mathf.MoveTowards(Colour, 1f, Time.fixedDeltaTime / Mathf.Max(0.01f, RecolourSeconds));
            if (_staggerLeft > 0f) { _staggerLeft -= Time.fixedDeltaTime; Body.linearVelocity *= 0.8f; return; }
            if (HurtstunLeft > 0) { HurtstunLeft--; return; }
            Tick(Time.fixedDeltaTime);
            if (SlowLeft > 0f)
            {
                SlowLeft -= Time.fixedDeltaTime;
                Body.linearVelocity *= SlowFactor;
            }
            ContactCheck();
        }

        public void ApplySlow(float seconds) { if (!IsDead) SlowLeft = Mathf.Max(SlowLeft, seconds); }

        /// <summary>Compass-dart: marked enemies are Inkthread targets and draw a faint ring in the greybox.</summary>
        public float MarkLeft { get; private set; }
        public bool IsMarked => MarkLeft > 0f;
        public void Mark(float seconds) { if (!IsDead) MarkLeft = Mathf.Max(MarkLeft, seconds); }

        /// <summary>Sighting lens parry: stop for a while (contact damage off, behaviour paused).</summary>
        public bool IsStaggered => _staggerLeft > 0f;
        float _staggerLeft;
        public void Stagger(float seconds)
        {
            if (IsDead) return;
            _staggerLeft = Mathf.Max(_staggerLeft, seconds);
            Body.linearVelocity = Vector2.zero;
            _flashUntil = Time.time + 0.15f;
        }

        /// <summary>Behaviour step, skipped during hurtstun and death.</summary>
        protected abstract void Tick(float dt);

        /// <summary>Whether this hit lands. Override for shells, undrawn smudges, armour.</summary>
        protected virtual bool AcceptsHit(in HitInfo hit) => true;
        /// <summary>Bosses hold their ground: no shove on hit.</summary>
        protected virtual bool AcceptsKnockback => true;
        /// <summary>Whether touching the body hurts right now (a Warden measuring a journeyman withholds it).</summary>
        protected virtual bool ContactHurts => true;
        /// <summary>Bosses deactivate instead so the arena can revive them for a retry.</summary>
        protected virtual bool DestroyOnDeath => true;
        public bool IsDying => _deathT >= 0f;

        /// <summary>Tooling and tests: resize the health pool and refill it.</summary>
        public void SetMaxHealth(int max) { _maxHealth = Mathf.Max(1, max); Health = _maxHealth; }

        /// <summary>Back up by <paramref name="amount"/>, never past the pool (a boss's Bind).</summary>
        protected void Heal(int amount) { if (!IsDead) Health = Mathf.Min(_maxHealth, Health + Mathf.Max(0, amount)); }

        /// <summary>Back to full health and the starting pose (boss retry, respawning rooms).</summary>
        public void Revive()
        {
            Health = _maxHealth;
            _deathT = -1f;
            HurtstunLeft = 0;
            SlowLeft = 0f;
            MarkLeft = 0f;
            _staggerLeft = 0f;
            Colour = 1f;
            _recolourWait = 0f;
            transform.localScale = _baseScale;
            gameObject.SetActive(true);
            Body.simulated = true;
            Body.linearVelocity = Vector2.zero;
            Collider.enabled = true;
            OnRevived();
        }

        protected virtual void OnRevived() { }
        /// <summary>The last of its colour drained.</summary>
        protected virtual void OnGrey() { }
        /// <summary>After a landed hit changed Health (bosses check phase thresholds here).</summary>
        protected virtual void OnHealthChanged() { }

        public bool TakeHit(in HitInfo hit)
        {
            if (IsDead || _deathT >= 0f) return false;
            if (!AcceptsHit(hit)) { OnHitBlocked(hit); return false; }
            Health = Mathf.Max(0, Health - hit.Damage);
            if (hit.Drain > 0f)
            {
                bool wasGrey = IsGrey;
                Colour = Mathf.Max(0f, Colour - hit.Drain);
                _recolourWait = RecolourDelay;
                if (IsGrey) ApplySlow(GreySlowSeconds);
                if (IsGrey && !wasGrey) OnGrey();
            }
            OnHealthChanged();
            _flashUntil = Time.time + 0.1f;
            HurtstunLeft = _hurtstunFrames;
            var away = hit.Direction.sqrMagnitude > 0f ? hit.Direction.normalized : Vector2.right;
            if (hit.Pulls) away = new Vector2(-away.x, 0f);   // reeled in
            float kb = _hitKnockback * hit.KnockbackOrDefault;
            if (AcceptsKnockback)
                Body.linearVelocity = new Vector2(away.x * kb, Mathf.Max(Body.linearVelocity.y, away.y > 0f ? kb : 2f));
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
            AnyDied?.Invoke(this);
        }

        void ContactCheck()
        {
            if (_contactDamage <= 0 || Wren == null || !ContactHurts) return;
            var b = Collider.bounds;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = _playerMask, useTriggers = false };
            int n = Physics2D.OverlapBox(b.center, b.size + Vector3.one * 0.05f, 0f, filter, _overlaps);
            for (int i = 0; i < n; i++)
            {
                var vitals = _overlaps[i].GetComponentInParent<WrenVitals>();
                if (vitals == null) continue;
                var parry = vitals.GetComponent<InstrumentBelt>();
                if (parry != null && parry.TryParry(this)) break;
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
            var tint = Color.Lerp(new Color(0.55f, 0.55f, 0.55f), TintColor(), Colour);
            if (IsMarked) tint = Color.Lerp(tint, new Color(0.95f, 0.62f, 0.15f), 0.5f + 0.3f * Mathf.Sin(Time.time * 12f));
            _mpb.SetColor(BaseColorId, Time.time < _flashUntil ? FlashColor : tint);
            Visual.SetPropertyBlock(_mpb);
        }

        /// <summary>Resting tint for the placeholder visual.</summary>
        protected virtual Color TintColor() => new Color(0.55f, 0.30f, 0.28f);
    }
}
