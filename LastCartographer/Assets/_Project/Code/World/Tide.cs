using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Water under a gap (combat doc §9, the gauntlet rule): falling in is not a death. It costs one mask and puts
    /// Wren back on the nearer bank. A gap wider than a jump but not a jump and a Wingbeat is the coast's soft gate.
    /// </summary>
    public sealed class Tide : MonoBehaviour
    {
        [SerializeField] Vector2 _westBank = new Vector2(-12.6f, 0f);
        [SerializeField] Vector2 _eastBank = new Vector2(-2.4f, 0f);
        [SerializeField] int _maskCost = 1;

        public int Sweeps { get; private set; }
        public Vector2 WestBank { get => _westBank; set => _westBank = value; }
        public Vector2 EastBank { get => _eastBank; set => _eastBank = value; }
        public event Action<WrenController> Swept;

        static bool _captioned;

        void OnTriggerEnter2D(Collider2D other)
        {
            var wren = other.GetComponentInParent<WrenController>();
            if (wren == null) return;
            Sweep(wren);
        }

        public void Sweep(WrenController wren)
        {
            Sweeps++;
            var vitals = wren.GetComponent<WrenVitals>();
            if (vitals != null && _maskCost > 0) vitals.Damage(_maskCost);
            float mid = (_westBank.x + _eastBank.x) * 0.5f;
            wren.Teleport(wren.Position.x < mid ? _westBank : _eastBank);
            if (!_captioned) { _captioned = true; Captions.Show("The tide takes her back to the bank.", 3f); }
            Swept?.Invoke(wren);
        }
    }
}
