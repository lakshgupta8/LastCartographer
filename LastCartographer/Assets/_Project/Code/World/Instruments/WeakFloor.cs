using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Ground that gives way to a heavy enough hit (the plumb weight's 3). Lives on the Ground layer
    /// so Wren stands on it; the quill scratches it (flash) but cannot break it.
    /// </summary>
    public sealed class WeakFloor : MonoBehaviour, IHittable
    {
        [SerializeField] int _breakDamage = 3;
        [SerializeField] float _crumbleSeconds = 0.35f;

        public bool IsBroken { get; private set; }
        public event Action<WeakFloor> Broke;

        Renderer _renderer;
        MaterialPropertyBlock _mpb;
        float _flashUntil, _crumbleT = -1f;
        Vector3 _baseScale;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
        }

        public bool TakeHit(in HitInfo hit)
        {
            if (IsBroken) return false;
            _flashUntil = Time.time + 0.1f;
            if (hit.Damage < _breakDamage) return false;   // scratched, not broken: no ink, no pogo
            IsBroken = true;
            _crumbleT = 0f;
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            Broke?.Invoke(this);
            return true;
        }

        void Update()
        {
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, Time.time < _flashUntil ? Color.white : new Color(0.58f, 0.50f, 0.36f));
                _renderer.SetPropertyBlock(_mpb);
            }
            if (_crumbleT < 0f) return;
            _crumbleT += Time.deltaTime;
            float k = Mathf.Clamp01(_crumbleT / _crumbleSeconds);
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y * (1f - k), _baseScale.z);
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
