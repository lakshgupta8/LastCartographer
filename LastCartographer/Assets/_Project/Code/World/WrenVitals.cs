using System;
using UnityEngine;

namespace OWSBG.World
{
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
}
