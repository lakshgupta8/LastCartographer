using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OWSBG.World
{
    /// <summary>
    /// Lights the room Wren is in with its region's light (ENV-10, docs/design/lighting.md): one sun, one ambient,
    /// one paper and one volume per region in the persistent scene, blended over a breath when she crosses from
    /// one region to the next. The day (DayCycle) dims and warms the regions that have an hour. The grain pass gets
    /// the region's tint as a global.
    /// </summary>
    public sealed class RegionLighting : MonoBehaviour
    {
        public const string TintGlobal = "_OWSBG_RegionTint";
        static readonly int TintId = Shader.PropertyToID(TintGlobal);

        [SerializeField] Light _sun;
        [SerializeField] Camera _camera;
        [Tooltip("One global volume per region, in the enum's order.")]
        [SerializeField] List<Volume> _volumes = new List<Volume>();
        [SerializeField] float _blendSeconds = 1.2f;

        /// <summary>Tests and the capture: the hour to light by instead of the day's; negative follows DayCycle.</summary>
        public float DuskOverride { get; set; } = -1f;
        public float NightOverride { get; set; } = -1f;

        public static RegionLighting Instance { get; private set; }

        public Region Current { get; private set; } = Region.Saltmarrow;
        public Region Previous { get; private set; } = Region.Saltmarrow;
        /// <summary>0 at the previous region's light, 1 at the current's.</summary>
        public float Blend { get; private set; } = 1f;
        public RegionLight.Profile Applied { get; private set; }
        public float BlendSeconds { get => _blendSeconds; set => _blendSeconds = value; }
        public Light Sun => _sun;
        public IReadOnlyList<Volume> Volumes => _volumes;

        bool _started;

        public void Configure(Light sun, Camera camera, IEnumerable<Volume> volumes)
        {
            _sun = sun;
            _camera = camera;
            _volumes.Clear();
            if (volumes != null) _volumes.AddRange(volumes);
        }

        /// <summary>The volume that carries a region's post, when the rig has one.</summary>
        public Volume VolumeOf(Region region)
        {
            int i = (int)region;
            return i >= 0 && i < _volumes.Count ? _volumes[i] : null;
        }

        void OnEnable() { Instance = this; }
        void OnDisable()
        {
            if (Instance == this) Instance = null;
            Shader.SetGlobalVector(TintId, Vector4.zero);
        }

        void Update()
        {
            var region = RegionHere();
            if (!_started) { Snap(region); return; }
            if (region != Current)
            {
                // Mid-blend, start from where the light is: the previous becomes what is applied now.
                Previous = Current;
                Current = region;
                Blend = 0f;
            }
            if (Blend < 1f) Blend = _blendSeconds <= 0f ? 1f : Mathf.Min(1f, Blend + Time.unscaledDeltaTime / _blendSeconds);
            Apply();
        }

        /// <summary>The region of the room Wren is in (Room.Current); the last known when there is none yet.</summary>
        public Region RegionHere()
        {
            var room = Room.Current;
            return room != null ? Mix.RegionOf(room.RoomId) ?? Current : Current;
        }

        /// <summary>Light the region now, with no blend (room load, the capture, tests).</summary>
        public void Snap(Region region)
        {
            Previous = region;
            Current = region;
            Blend = 1f;
            _started = true;
            Apply();
        }

        /// <summary>Light the room that is loaded now, with no blend.</summary>
        public void Snap() => Snap(RegionHere());

        void Apply()
        {
            float dusk = DuskOverride >= 0f ? DuskOverride : DayCycle.Dusk;
            float night = NightOverride >= 0f ? NightOverride : DayCycle.Night;
            var from = RegionLight.AtHour(RegionLight.For(Previous), dusk, night);
            var to = RegionLight.AtHour(RegionLight.For(Current), dusk, night);
            var p = RegionLight.Lerp(from, to, Blend);
            Applied = p;

            if (_sun != null)
            {
                _sun.color = p.Sun;
                _sun.intensity = p.SunIntensity;
                _sun.transform.rotation = p.SunRotation;
                _sun.shadows = p.Shadows ? LightShadows.Soft : LightShadows.None;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = p.AmbientSky;
            RenderSettings.ambientEquatorColor = p.AmbientEquator;
            RenderSettings.ambientGroundColor = p.AmbientGround;
            if (_camera != null) _camera.backgroundColor = p.Paper;
            Shader.SetGlobalVector(TintId, new Vector4(p.Tint.r, p.Tint.g, p.Tint.b, p.TintAmount));

            for (int i = 0; i < _volumes.Count; i++)
            {
                var v = _volumes[i];
                if (v == null) continue;
                float w = i == (int)Current ? Blend : i == (int)Previous ? 1f - Blend : 0f;
                v.weight = w;
                if (v.gameObject.activeSelf != w > 0f) v.gameObject.SetActive(w > 0f);
            }
        }

        /// <summary>
        /// The post a region grades through, written into a profile (the setup saves one per region as an asset;
        /// tests make them in memory): bloom, the vignette, saturation and contrast, a colour filter, a warmth.
        /// </summary>
        public static void FillProfile(VolumeProfile profile, RegionLight.Profile p)
        {
            var bloom = profile.TryGet<Bloom>(out var b) ? b : profile.Add<Bloom>(true);
            bloom.intensity.Override(p.Bloom);
            bloom.threshold.Override(p.BloomThreshold);
            var vig = profile.TryGet<Vignette>(out var v) ? v : profile.Add<Vignette>(true);
            vig.intensity.Override(p.Vignette);
            vig.color.Override(p.VignetteColour);
            var adj = profile.TryGet<ColorAdjustments>(out var a) ? a : profile.Add<ColorAdjustments>(true);
            adj.saturation.Override(p.Saturation);
            adj.contrast.Override(p.Contrast);
            adj.colorFilter.Override(p.Filter);
            var wb = profile.TryGet<WhiteBalance>(out var w) ? w : profile.Add<WhiteBalance>(true);
            wb.temperature.Override(p.Temperature);
        }

        /// <summary>A region's volume, made in memory (tests, runtime rigs): global, over the base volume, weight 0.</summary>
        public static Volume MakeVolume(Transform parent, Region region)
        {
            var go = new GameObject("Volume_" + region);
            go.transform.SetParent(parent, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "PP_" + region;
            FillProfile(profile, RegionLight.For(region));
            vol.sharedProfile = profile;
            return vol;
        }
    }
}
