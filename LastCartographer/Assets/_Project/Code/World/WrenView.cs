using UnityEngine;

namespace OWSBG.World
{
    /// <summary>Flips the visual to match facing and squashes on land. Placeholder until CHR-03 animation.</summary>
    public sealed class WrenView : MonoBehaviour
    {
        [SerializeField] Transform _visual;
        WrenController _ctrl;
        Vector3 _baseScale = Vector3.one;
        float _squash;

        void Awake()
        {
            _ctrl = GetComponentInParent<WrenController>();
            if (_visual == null && transform.childCount > 0) _visual = transform.GetChild(0);
            if (_visual != null) _baseScale = _visual.localScale;
            if (_ctrl != null) _ctrl.Landed += () => _squash = 1f;
        }

        void LateUpdate()
        {
            if (_visual == null || _ctrl == null) return;
            float k = 1f;
            if (_squash > 0f)
            {
                _squash = Mathf.MoveTowards(_squash, 0f, Time.deltaTime * 6f);
                k = 1f - 0.15f * Mathf.Sin(_squash * Mathf.PI);
            }
            _visual.localScale = new Vector3(Mathf.Abs(_baseScale.x) * _ctrl.Facing / k, _baseScale.y * k, _baseScale.z);
        }
    }
}
