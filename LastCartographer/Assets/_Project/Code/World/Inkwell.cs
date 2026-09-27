using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>Wren's ink resource (combat doc 4): 9 pips, filled by hits, spent on Bind and Flourishes.</summary>
    public sealed class Inkwell : MonoBehaviour
    {
        [SerializeField] int _maxPips = 9;
        [SerializeField] int _pips = 0;
        float _carry;

        public int MaxPips => _maxPips;
        public int Pips => _pips;
        /// <summary>Charter passive (Surveyor: 1.25). Applied to hit gains only, never to refunds or tinctures.</summary>
        public float GainMultiplier { get; set; } = 1f;
        public event Action<int> Changed;

        /// <summary>Ink earned by landing a strike: scaled by the Charter, fractions carried to the next hit.</summary>
        public void AddFromHit(int n)
        {
            _carry += n * GainMultiplier;
            int whole = Mathf.FloorToInt(_carry + 0.0001f);
            _carry -= whole;
            if (whole > 0) Add(whole);
        }

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

        public void Empty()
        {
            _carry = 0f;
            if (_pips == 0) return;
            _pips = 0;
            Changed?.Invoke(0);
        }
    }
}
