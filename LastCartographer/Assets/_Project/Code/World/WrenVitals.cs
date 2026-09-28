using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>Masks (health), Bind (heal by holding, spends ink), knockback on hit. Combat doc 1 and 2.2.</summary>
    [RequireComponent(typeof(Inkwell))]
    public sealed class WrenVitals : MonoBehaviour
    {
        [SerializeField] int _maxMasks = 5;
        [SerializeField] int _masks = 5;
        [SerializeField] int _bindCost = Tuning.BindCost;
        [SerializeField] float _bindSeconds = 0.6f;
        [SerializeField] int _invulnFrames = 60;
        [SerializeField] Vector2 _hitKnockback = new Vector2(7f, 9f);

        Inkwell _ink;
        WrenController _ctrl;
        float _bindHeld;
        int _invulnLeft;

        public int MaxMasks => _maxMasks;
        /// <summary>The serialized base plus vellum upgrades (DES-05); Charter passives apply on top.</summary>
        public int BaseMaxMasks => (_baseMaxMasks > 0 ? _baseMaxMasks : _maxMasks) + Economy.MaskUpgrades(GameState.World);
        int _baseMaxMasks = -1;

        /// <summary>Charter passive: Warden +1 mask, Drifter capped at 4. Current masks are clamped, never refilled.</summary>
        public void SetMaxMasks(int max)
        {
            if (_baseMaxMasks < 0) _baseMaxMasks = _maxMasks;
            max = Mathf.Max(1, max);
            if (max == _maxMasks) return;
            _maxMasks = max;
            if (_masks > _maxMasks) _masks = _maxMasks;
            MasksChanged?.Invoke(_masks);
        }
        public void ResetMaxMasks() { if (_baseMaxMasks > 0) SetMaxMasks(_baseMaxMasks); }
        public int Masks => _masks;
        /// <summary>Ink a Bind spends (combat doc 2.2); the Unwriter's Charter makes it 4.</summary>
        public int BindCost { get => _bindCost; set => _bindCost = Mathf.Max(1, value); }
        public bool IsBinding => _bindHeld > 0f;
        public bool IsDead => _masks <= 0;
        public bool IsInvulnerable => _invulnLeft > 0 || (_ctrl != null && _ctrl.IsInvulnerable);
        public event Action<int> MasksChanged;
        public event Action Bound, Hurt, Died;

        void Awake()
        {
            _ink = GetComponent<Inkwell>();
            _ctrl = GetComponent<WrenController>();
            if (_baseMaxMasks < 0) _baseMaxMasks = _maxMasks;
        }

        void OnEnable() { Economy.MaskBought += OnMaskBought; }
        void OnDisable() { Economy.MaskBought -= OnMaskBought; }

        /// <summary>A vellum cache stitched in at the desk: one more mask, and it is full.</summary>
        void OnMaskBought(int upgrades)
        {
            SetMaxMasks(_maxMasks + 1);
            _masks = Mathf.Min(_maxMasks, _masks + 1);
            MasksChanged?.Invoke(_masks);
        }

        void FixedUpdate()
        {
            if (_invulnLeft > 0) _invulnLeft--;

            bool wantBind = _ctrl != null && _ctrl.Input != null && !_ctrl.Frozen && _ctrl.Input.BindHeld
                            && _masks < _maxMasks && _ink.Pips >= _bindCost && _ctrl.IsGrounded;
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
                        Mix.Note(Mix.Duck.Bind);
                        Bound?.Invoke();
                    }
                }
            }
            else
            {
                _bindHeld = 0f;
                // A toggled Bind lets go once it can't go on (full, dry, airborne): the next press starts it again.
                if (_ctrl != null && _ctrl.Input != null && _ctrl.Input.BindHeld) Controls.Release(Hold.Bind);
            }
        }

        /// <summary>Take damage from a source at a world position (knockback points away from it).</summary>
        public bool Damage(int amount, Vector2 sourcePosition)
        {
            if (!Damage(amount)) return false;
            if (_ctrl != null)
            {
                float dir = _ctrl.Position.x >= sourcePosition.x ? 1f : -1f;
                _ctrl.Knockback(new Vector2(dir * _hitKnockback.x, _hitKnockback.y));
            }
            return true;
        }

        public bool Damage(int amount)
        {
            if (amount <= 0 || IsDead) return false;
            if (IsInvulnerable) return false;
            _masks = Mathf.Max(0, _masks - amount);
            _invulnLeft = _invulnFrames;
            _bindHeld = 0f;
            MasksChanged?.Invoke(_masks);
            Mix.Note(Mix.Duck.Hurt);
            Hurt?.Invoke();
            if (_masks == 0) { _ink.Empty(); Died?.Invoke(); }
            return true;
        }

        public void RestoreAll()
        {
            _masks = _maxMasks;
            _invulnLeft = 0;
            MasksChanged?.Invoke(_masks);
        }
    }
}
