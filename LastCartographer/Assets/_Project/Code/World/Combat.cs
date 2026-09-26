using System;
using UnityEngine;

namespace OWSBG.World
{
    public struct HitInfo
    {
        public int Damage;
        public Vector2 Direction;
        public GameObject Source;
    }

    /// <summary>Anything the quill can hit: enemies, dummies, bells, breakable floors.</summary>
    public interface IHittable
    {
        /// <returns>true if the hit landed (fills the Inkwell, triggers hitstop and pogo).</returns>
        bool TakeHit(in HitInfo hit);
    }

    /// <summary>Wren's ink resource (combat doc 4): 9 pips, filled by hits, spent on Bind and Flourishes.</summary>
    public sealed class Inkwell : MonoBehaviour
    {
        [SerializeField] int _maxPips = 9;
        [SerializeField] int _pips = 0;

        public int MaxPips => _maxPips;
        public int Pips => _pips;
        public event Action<int> Changed;

        public void Add(int n)
        {
            int next = Mathf.Clamp(_pips + n, 0, _maxPips);
            if (next == _pips) return;
            _pips = next;
            Changed?.Invoke(_pips);
        }

        public bool TrySpend(int n)
        {
            if (_pips < n) return false;
            _pips -= n;
            Changed?.Invoke(_pips);
            return true;
        }

        public void Empty() { if (_pips == 0) return; _pips = 0; Changed?.Invoke(0); }
    }

    /// <summary>Masks (health) and Bind (heal by holding, spends ink). Combat doc 1 and 2.2.</summary>
    [RequireComponent(typeof(Inkwell))]
    public sealed class WrenVitals : MonoBehaviour
    {
        [SerializeField] int _maxMasks = 5;
        [SerializeField] int _masks = 5;
        [SerializeField] int _bindCost = 3;
        [SerializeField] float _bindSeconds = 0.6f;
        [SerializeField] int _invulnFrames = 60;

        Inkwell _ink;
        WrenController _ctrl;
        float _bindHeld;
        int _invulnLeft;

        public int MaxMasks => _maxMasks;
        public int Masks => _masks;
        public bool IsBinding => _bindHeld > 0f;
        public bool IsDead => _masks <= 0;
        public event Action<int> MasksChanged;
        public event Action Bound, Hurt, Died;

        void Awake()
        {
            _ink = GetComponent<Inkwell>();
            _ctrl = GetComponent<WrenController>();
        }

        void FixedUpdate()
        {
            if (_invulnLeft > 0) _invulnLeft--;

            bool wantBind = _ctrl != null && _ctrl.Input != null && _ctrl.Input.BindHeld
                            && _masks < _maxMasks && _ink.Pips >= _bindCost && (_ctrl.IsGrounded);
            if (wantBind)
            {
                _bindHeld += Time.fixedDeltaTime;
                if (_bindHeld >= _bindSeconds)
                {
                    _bindHeld = 0f;
                    if (_ink.TrySpend(_bindCost))
                    {
                        _masks = Mathf.Min(_maxMasks, _masks + 1);
                        MasksChanged?.Invoke(_masks);
                        Bound?.Invoke();
                    }
                }
            }
            else _bindHeld = 0f;
        }

        public bool Damage(int amount)
        {
            if (amount <= 0 || IsDead) return false;
            if (_invulnLeft > 0 || (_ctrl != null && _ctrl.IsInvulnerable)) return false;
            _masks = Mathf.Max(0, _masks - amount);
            _invulnLeft = _invulnFrames;
            _bindHeld = 0f;
            MasksChanged?.Invoke(_masks);
            Hurt?.Invoke();
            if (_masks == 0) { _ink.Empty(); Died?.Invoke(); }
            return true;
        }

        public void RestoreAll()
        {
            _masks = _maxMasks;
            MasksChanged?.Invoke(_masks);
        }
    }

    /// <summary>
    /// The needle-quill. Three directions, startup/active/recovery frames, one hit per target per
    /// swing, pogo on a landed down-strike, hitstop, and ink on hit. Combat doc 2.1.
    /// </summary>
    [RequireComponent(typeof(WrenController))]
    public sealed class QuillStrike : MonoBehaviour
    {
        public float reach = 2.2f;
        public float thickness = 0.9f;
        public float originHeight = 0.6f;
        public int startupFrames = 3;
        public int activeFrames = 4;
        public int recoveryFrames = 8;
        public int damage = 1;
        public int hitstopFrames = 2;
        public LayerMask hitMask;

        public enum Phase { Idle, Startup, Active, Recovery }
        public Phase Current => _phase;
        public Vector2 Direction => _dir;
        public bool IsBusy => _phase != Phase.Idle;
        public event Action<Vector2> Swung;
        public event Action<IHittable> Landed;

        WrenController _ctrl;
        Inkwell _ink;
        Phase _phase;
        int _framesLeft;
        Vector2 _dir = Vector2.right;
        readonly Collider2D[] _overlaps = new Collider2D[16];
        readonly System.Collections.Generic.HashSet<IHittable> _hitThisSwing = new System.Collections.Generic.HashSet<IHittable>();

        void Awake()
        {
            _ctrl = GetComponent<WrenController>();
            _ink = GetComponent<Inkwell>();
        }

        void FixedUpdate()
        {
            if (_ctrl.IsDashing) { _phase = Phase.Idle; return; }   // dash cancels recovery

            if (_phase == Phase.Idle)
            {
                if (_ctrl.Input != null && _ctrl.Input.ConsumeAttack())
                {
                    var move = _ctrl.Input.Move;
                    if (move.y > 0.5f) _dir = Vector2.up;
                    else if (!_ctrl.IsGrounded && move.y < -0.5f) _dir = Vector2.down;
                    else _dir = new Vector2(_ctrl.Facing, 0f);
                    _phase = Phase.Startup;
                    _framesLeft = startupFrames;
                    _hitThisSwing.Clear();
                    Swung?.Invoke(_dir);
                }
                return;
            }

            if (_phase == Phase.Active) DoHits();

            _framesLeft--;
            if (_framesLeft > 0) return;
            switch (_phase)
            {
                case Phase.Startup:  _phase = Phase.Active;   _framesLeft = activeFrames;   DoHits(); break;
                case Phase.Active:   _phase = Phase.Recovery; _framesLeft = recoveryFrames; break;
                case Phase.Recovery: _phase = Phase.Idle; break;
            }
        }

        public void GetHitbox(out Vector2 center, out Vector2 size)
        {
            var origin = _ctrl.Position + Vector2.up * originHeight;
            center = origin + _dir * (reach * 0.5f);
            size = _dir.x != 0f ? new Vector2(reach, thickness) : new Vector2(thickness, reach);
        }

        void DoHits()
        {
            GetHitbox(out var center, out var size);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = hitMask, useTriggers = true };
            int n = Physics2D.OverlapBox(center, size, 0f, filter, _overlaps);
            bool landedAny = false;
            for (int i = 0; i < n; i++)
            {
                var h = _overlaps[i].GetComponentInParent<IHittable>();
                if (h == null || _hitThisSwing.Contains(h)) continue;
                _hitThisSwing.Add(h);
                var info = new HitInfo { Damage = damage, Direction = _dir, Source = gameObject };
                if (!h.TakeHit(info)) continue;
                landedAny = true;
                _ink?.Add(1);
                Landed?.Invoke(h);
            }
            if (!landedAny) return;
            Core.Hitstop.Request(hitstopFrames);
            if (_dir.y < 0f) _ctrl.Pogo();
        }

        void OnDrawGizmosSelected()
        {
            if (_ctrl == null) return;
            GetHitbox(out var c, out var s);
            Gizmos.color = _phase == Phase.Active ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireCube(c, s);
        }
    }

    /// <summary>A greybox target. Counts hits, flashes, and recoils a little.</summary>
    public sealed class TrainingDummy : MonoBehaviour, IHittable
    {
        public int Hits { get; private set; }
        public event Action<HitInfo> WasHit;

        Renderer _renderer;
        MaterialPropertyBlock _mpb;
        float _flashUntil;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        public bool TakeHit(in HitInfo hit)
        {
            Hits++;
            _flashUntil = Time.time + 0.08f;
            WasHit?.Invoke(hit);
            return true;
        }

        void Update()
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, Time.time < _flashUntil ? Color.white : new Color(0.75f, 0.35f, 0.30f));
            _renderer.SetPropertyBlock(_mpb);
        }
    }

    /// <summary>Flips the visual to match facing and squashes on land. Placeholder until CHR-03 animation.</summary>
    public sealed class WrenView : MonoBehaviour
    {
        [SerializeField] Transform _visual;
        WrenController _ctrl;
        Vector3 _baseScale = Vector3.one;
        float _squash;

        void Awake()
        {
            _ctrl = GetComponentInParent<WrenController>();
            if (_visual == null && transform.childCount > 0) _visual = transform.GetChild(0);
            if (_visual != null) _baseScale = _visual.localScale;
            if (_ctrl != null) _ctrl.Landed += () => _squash = 1f;
        }

        void LateUpdate()
        {
            if (_visual == null || _ctrl == null) return;
            float k = 1f;
            if (_squash > 0f)
            {
                _squash = Mathf.MoveTowards(_squash, 0f, Time.deltaTime * 6f);
                k = 1f - 0.15f * Mathf.Sin(_squash * Mathf.PI);
            }
            _visual.localScale = new Vector3(Mathf.Abs(_baseScale.x) * _ctrl.Facing / k, _baseScale.y * k, _baseScale.z);
        }
    }
}
