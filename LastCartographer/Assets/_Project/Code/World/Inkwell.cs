using System;
using UnityEngine;

namespace OWSBG.World
{
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

        public void Empty()
        {
            if (_pips == 0) return;
            _pips = 0;
            Changed?.Invoke(0);
        }
    }
}
