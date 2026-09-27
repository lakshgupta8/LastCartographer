using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The white at a room's edge (bible 4.6). While active, Wren's x between StartX and EndX drives the
    /// screen fade to white; reaching EndX raises Crossed once. The prologue director turns it on after
    /// Isolde walks in and answers Crossed with the shore.
    /// </summary>
    public sealed class BlankEdge : MonoBehaviour
    {
        [SerializeField] float _startX = 13f;
        [SerializeField] float _endX = 22f;

        public bool Active { get; set; }
        public float Level { get; private set; }
        public float StartX { get => _startX; set => _startX = value; }
        public float EndX { get => _endX; set => _endX = value; }
        public event Action Crossed;

        WrenController _wren;
        bool _crossed;

        public void ResetCrossing() { _crossed = false; }

        void Update()
        {
            if (!Active) return;
            if (_wren == null) _wren = FindFirstObjectByType<WrenController>();
            if (_wren == null) return;
            float t = Mathf.Clamp01((_wren.Position.x - _startX) / Mathf.Max(0.01f, _endX - _startX));
            Level = t;
            ScreenFade.Set(t, ScreenFade.White);
            if (t >= 1f && !_crossed)
            {
                _crossed = true;
                Crossed?.Invoke();
            }
        }
    }
}
