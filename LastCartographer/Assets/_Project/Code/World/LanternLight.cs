using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Her lantern as a light (ENV-10, art-direction 3: real lights for the Blank's lantern-radius): a point light on
    /// Wren that comes up with the lantern-radius (ClarityMeter.LanternStrength) and reaches as far as the radius,
    /// so the paper inside it takes the lamp's warmth while the pass whitens everything beyond. Off on the coast.
    /// </summary>
    public sealed class LanternLight : MonoBehaviour
    {
        [SerializeField] Light _light;
        [SerializeField] float _intensity = 1.8f;
        [SerializeField] float _beyondRadius = 1f;
        [SerializeField] Vector3 _offset = new Vector3(0f, 0.55f, -0.8f);

        public static readonly Color Colour = new Color(1f, 0.90f, 0.70f);

        ClarityMeter _meter;

        public Light Light => _light;
        public bool IsOn => _light != null && _light.enabled;

        public void Configure(Light light) { _light = light; }

        void Awake()
        {
            if (_light == null)
            {
                var go = new GameObject("LanternLight");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = _offset;
                _light = go.AddComponent<Light>();
                _light.type = LightType.Point;
                _light.color = Colour;
                _light.shadows = LightShadows.None;
            }
            _light.enabled = false;
        }

        void LateUpdate()
        {
            if (_meter == null) _meter = GetComponent<ClarityMeter>();
            float strength = _meter != null ? _meter.LanternStrength : 0f;
            bool on = strength > 0.01f;
            if (_light.enabled != on) _light.enabled = on;
            if (!on) return;
            _light.intensity = _intensity * strength;
            _light.range = (_meter != null ? _meter.Radius : 0f) + _beyondRadius;
        }
    }
}
