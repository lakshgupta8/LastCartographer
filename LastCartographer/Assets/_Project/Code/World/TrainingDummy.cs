using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>A greybox target. Counts hits and flashes.</summary>
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
}
