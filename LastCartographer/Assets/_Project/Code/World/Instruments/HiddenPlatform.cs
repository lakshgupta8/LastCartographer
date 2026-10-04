using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A platform that is not drawn until a Field lantern reveals it: collider off and the visual's
    /// _Ink (or colour) faded. Drawn for the lantern's duration, then fades again.
    /// </summary>
    public sealed class HiddenPlatform : MonoBehaviour, IRevealable
    {
        [SerializeField] float _fadeSpeed = 4f;

        public bool IsRevealed => _revealLeft > 0f;
        float _revealLeft, _ink;
        Renderer _renderer;
        Collider2D[] _colliders;
        MaterialPropertyBlock _mpb;
        static readonly int InkId = Shader.PropertyToID("_Ink");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>On the kit's lantern-drawn planks (ENV-12) rather than the greybox block.</summary>
        public bool IsDrawn { get; private set; }

        void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _colliders = GetComponentsInChildren<Collider2D>();
            _mpb = new MaterialPropertyBlock();
            IsDrawn = _renderer != null && _renderer.sharedMaterial != null && _renderer.sharedMaterial.HasProperty(InkId);
            SetSolid(false);
        }

        public void Reveal(float seconds) { _revealLeft = Mathf.Max(_revealLeft, seconds); SetSolid(true); }

        void SetSolid(bool on) { foreach (var c in _colliders) c.enabled = on; }

        void Update()
        {
            if (_revealLeft > 0f)
            {
                _revealLeft -= Time.deltaTime;
                if (_revealLeft <= 0f) SetSolid(false);
            }
            _ink = Mathf.MoveTowards(_ink, IsRevealed ? 1f : 0f, Time.deltaTime * _fadeSpeed);
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(InkId, Mathf.Max(0.02f, _ink));
            _mpb.SetColor(BaseColorId, IsDrawn ? Color.white : new Color(0.52f, 0.46f, 0.36f, 1f) * Mathf.Lerp(0.15f, 1f, _ink) + new Color(0f, 0f, 0f, 1f));
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
