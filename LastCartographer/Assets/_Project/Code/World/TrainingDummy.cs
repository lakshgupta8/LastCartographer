using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>A greybox target. Counts hits, flashes, and recoils in the hit direction.</summary>
    public sealed class TrainingDummy : MonoBehaviour, IHittable
    {
        [SerializeField] float _flashSeconds = 0.14f;
        [SerializeField] float _recoilDistance = 0.25f;
        [SerializeField] float _recoilSeconds = 0.18f;

        public int Hits { get; private set; }
        public event Action<HitInfo> WasHit;

        Renderer _renderer;
        MaterialPropertyBlock _mpb;
        Vector3 _restPosition;
        Vector3 _restScale;
        Vector2 _recoilDir;
        float _flashUntil, _recoilT = -1f;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color RestColor = new Color(0.75f, 0.35f, 0.30f);
        static readonly Color DrawnRest = Color.white, DrawnFlash = new Color(1.8f, 1.7f, 1.4f);
        static readonly int InkId = Shader.PropertyToID("_Ink");
        Color _rest = RestColor, _flash = Color.white;

        /// <summary>The target is the kit's drawing (ENV-09), not the red block: it rests in its own colours.</summary>
        public bool IsDrawn { get; private set; }

        void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            IsDrawn = _renderer != null && _renderer.sharedMaterial != null && _renderer.sharedMaterial.HasProperty(InkId);
            if (IsDrawn) { _rest = DrawnRest; _flash = DrawnFlash; }
            _restPosition = transform.position;
            _restScale = transform.localScale;
        }

        public bool TakeHit(in HitInfo hit)
        {
            Hits++;
            _flashUntil = Time.time + _flashSeconds;
            _recoilDir = hit.Direction.sqrMagnitude > 0f ? hit.Direction.normalized : Vector2.right;
            _recoilT = 0f;
            WasHit?.Invoke(hit);
            return true;
        }

        void Update()
        {
            if (_recoilT >= 0f)
            {
                _recoilT += Time.deltaTime;
                float k = Mathf.Clamp01(_recoilT / _recoilSeconds);
                float punch = Mathf.Sin(k * Mathf.PI);
                // Sideways hits shove; vertical hits squash instead of moving into the floor.
                var offset = Mathf.Abs(_recoilDir.x) > 0.5f ? (Vector3)(_recoilDir * _recoilDistance * punch) : Vector3.zero;
                transform.position = _restPosition + offset;
                float squash = 1f - 0.18f * punch;
                transform.localScale = new Vector3(_restScale.x / squash, _restScale.y * squash, _restScale.z);
                if (k >= 1f) { _recoilT = -1f; transform.position = _restPosition; transform.localScale = _restScale; }
            }

            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, Time.time < _flashUntil ? _flash : _rest);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
