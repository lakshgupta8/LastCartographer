using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// The light of each region (ENV-10, docs/design/lighting.md; art-direction 5): an art-directed hour that never
    /// moves, as a sun, an ambient, the paper the camera clears to, and the post the region grades through. The day
    /// (hub-life, DayCycle) only dims and warms it; the white has no hour. Data only: RegionLighting (World) blends
    /// and applies it.
    /// </summary>
    public static class RegionLight
    {
        [System.Serializable]
        public struct Profile
        {
            public Region Region;
            /// <summary>The hour the region is always at, for the designer reading the table.</summary>
            public string Hour;
            public Color Sun;
            public float SunIntensity;
            /// <summary>Degrees above the horizon, and the sun's turn about the up axis (0 = from in front of the paper).</summary>
            public float Elevation, Azimuth;
            public bool Shadows;
            public Color AmbientSky, AmbientEquator, AmbientGround;
            /// <summary>What the camera clears to: the region's paper.</summary>
            public Color Paper;
            /// <summary>The grain pass's tint over everything, and how much of it.</summary>
            public Color Tint;
            public float TintAmount;
            public float Bloom, BloomThreshold;
            public float Vignette;
            public Color VignetteColour;
            public float Saturation, Contrast;
            public Color Filter;
            public float Temperature;
            /// <summary>What a lamp, a hearth or a furnace in this region casts.</summary>
            public Color Lamp;
            /// <summary>Whether the day's dusk and night touch it. The white has no hour.</summary>
            public bool FollowsHour;

            public Quaternion SunRotation => Quaternion.Euler(Elevation, Azimuth, 0f);
        }

        static readonly Profile[] Table =
        {
            new Profile
            {
                Region = Region.Saltmarrow, Hour = "morning, the sea's light",
                Sun = new Color(1f, 0.95f, 0.86f), SunIntensity = 1.4f, Elevation = 40f, Azimuth = -20f, Shadows = true,
                AmbientSky = new Color(0.70f, 0.72f, 0.78f), AmbientEquator = new Color(0.55f, 0.52f, 0.48f), AmbientGround = new Color(0.32f, 0.30f, 0.27f),
                Paper = new Color(0.93f, 0.89f, 0.80f), Tint = new Color(1f, 0.98f, 0.94f), TintAmount = 0f,
                Bloom = 0.25f, BloomThreshold = 1.1f, Vignette = 0.22f, VignetteColour = new Color(0.10f, 0.10f, 0.14f),
                Saturation = 0f, Contrast = 0f, Filter = Color.white, Temperature = 0f,
                Lamp = new Color(1f, 0.82f, 0.50f), FollowsHour = true,
            },
            new Profile
            {
                Region = Region.Emberdown, Hour = "no sky: lamplight and the furnaces",
                Sun = new Color(1f, 0.74f, 0.52f), SunIntensity = 0.55f, Elevation = 62f, Azimuth = 10f, Shadows = true,
                AmbientSky = new Color(0.30f, 0.26f, 0.26f), AmbientEquator = new Color(0.26f, 0.22f, 0.20f), AmbientGround = new Color(0.20f, 0.14f, 0.10f),
                Paper = new Color(0.82f, 0.80f, 0.78f), Tint = new Color(1f, 0.90f, 0.82f), TintAmount = 0.25f,
                Bloom = 0.6f, BloomThreshold = 0.9f, Vignette = 0.38f, VignetteColour = new Color(0.06f, 0.04f, 0.03f),
                Saturation = -8f, Contrast = 8f, Filter = new Color(1f, 0.94f, 0.90f), Temperature = -5f,
                Lamp = new Color(1f, 0.52f, 0.20f), FollowsHour = true,
            },
            new Profile
            {
                Region = Region.Verdance, Hour = "noon under the canopy, the light in shafts",
                Sun = new Color(1f, 0.97f, 0.80f), SunIntensity = 1.25f, Elevation = 62f, Azimuth = 15f, Shadows = true,
                AmbientSky = new Color(0.62f, 0.70f, 0.52f), AmbientEquator = new Color(0.50f, 0.56f, 0.38f), AmbientGround = new Color(0.30f, 0.32f, 0.22f),
                Paper = new Color(0.94f, 0.90f, 0.72f), Tint = new Color(0.98f, 1f, 0.90f), TintAmount = 0.15f,
                Bloom = 0.35f, BloomThreshold = 1.0f, Vignette = 0.24f, VignetteColour = new Color(0.08f, 0.10f, 0.06f),
                Saturation = 6f, Contrast = 0f, Filter = new Color(0.98f, 1f, 0.92f), Temperature = 0f,
                Lamp = new Color(1f, 0.95f, 0.80f), FollowsHour = true,
            },
            new Profile
            {
                Region = Region.Halden, Hour = "always late afternoon, a long light low across the plateau",
                Sun = new Color(1f, 0.86f, 0.66f), SunIntensity = 1.5f, Elevation = 22f, Azimuth = -55f, Shadows = true,
                AmbientSky = new Color(0.66f, 0.70f, 0.78f), AmbientEquator = new Color(0.62f, 0.56f, 0.48f), AmbientGround = new Color(0.34f, 0.30f, 0.26f),
                Paper = new Color(0.92f, 0.92f, 0.87f), Tint = new Color(1f, 0.94f, 0.84f), TintAmount = 0.2f,
                Bloom = 0.3f, BloomThreshold = 1.1f, Vignette = 0.26f, VignetteColour = new Color(0.10f, 0.09f, 0.12f),
                Saturation = -4f, Contrast = 4f, Filter = new Color(1f, 0.96f, 0.90f), Temperature = 12f,
                Lamp = new Color(1f, 0.80f, 0.45f), FollowsHour = false,   // Halden is anchored: its hour is locked (hub-life)
            },
            new Profile
            {
                Region = Region.Windreach, Hour = "storm light: gold under a violet sky",
                Sun = new Color(1f, 0.90f, 0.68f), SunIntensity = 1.3f, Elevation = 28f, Azimuth = 40f, Shadows = true,
                AmbientSky = new Color(0.46f, 0.42f, 0.60f), AmbientEquator = new Color(0.60f, 0.56f, 0.50f), AmbientGround = new Color(0.36f, 0.34f, 0.26f),
                Paper = new Color(0.93f, 0.88f, 0.70f), Tint = new Color(0.96f, 0.94f, 1f), TintAmount = 0.15f,
                Bloom = 0.3f, BloomThreshold = 1.0f, Vignette = 0.30f, VignetteColour = new Color(0.10f, 0.08f, 0.16f),
                Saturation = 4f, Contrast = 6f, Filter = new Color(0.98f, 0.96f, 1f), Temperature = 0f,
                Lamp = new Color(1f, 0.82f, 0.50f), FollowsHour = true,
            },
            new Profile
            {
                Region = Region.Greyfold, Hour = "no hour: a white that is everywhere at once",
                Sun = Color.white, SunIntensity = 1.0f, Elevation = 20f, Azimuth = 0f, Shadows = false,
                AmbientSky = new Color(0.90f, 0.90f, 0.90f), AmbientEquator = new Color(0.86f, 0.86f, 0.87f), AmbientGround = new Color(0.80f, 0.80f, 0.80f),
                Paper = new Color(0.98f, 0.98f, 0.97f), Tint = Color.white, TintAmount = 0f,
                Bloom = 0.1f, BloomThreshold = 1.3f, Vignette = 0f, VignetteColour = Color.black,
                Saturation = -20f, Contrast = -6f, Filter = Color.white, Temperature = 0f,
                Lamp = new Color(1f, 0.88f, 0.60f), FollowsHour = false,
            },
            new Profile
            {
                Region = Region.Blank, Hour = "no hour: the lantern's colour blooming in the white",
                Sun = Color.white, SunIntensity = 0.95f, Elevation = 15f, Azimuth = 0f, Shadows = false,
                AmbientSky = new Color(0.92f, 0.92f, 0.92f), AmbientEquator = new Color(0.88f, 0.88f, 0.89f), AmbientGround = new Color(0.82f, 0.82f, 0.82f),
                Paper = new Color(0.985f, 0.985f, 0.98f), Tint = Color.white, TintAmount = 0f,
                Bloom = 0.55f, BloomThreshold = 0.95f, Vignette = 0f, VignetteColour = Color.black,
                Saturation = -10f, Contrast = -8f, Filter = Color.white, Temperature = 0f,
                Lamp = new Color(0.85f, 0.88f, 1f), FollowsHour = false,
            },
        };

        public static int Count => Table.Length;

        /// <summary>The region's light. Every region in the enum has a row.</summary>
        public static Profile For(Region region)
        {
            foreach (var p in Table) if (p.Region == region) return p;
            return Table[0];
        }

        /// <summary>The light of the room's region, from its id ("Greybox_Halden_Bridges_1", "Saltmarrow_Quay"); the coast's when it says nothing.</summary>
        public static Profile ForRoom(string roomId) => For(OWSBG.Core.Mix.RegionOf(roomId) ?? Region.Saltmarrow);

        /// <summary>What a lamp casts in the room's region.</summary>
        public static Color LampColour(string roomId) => ForRoom(roomId).Lamp;

        /// <summary>
        /// The hour over the region (hub-life 2): night dims the sun to a third and cools it, dusk warms it, the
        /// ambient drops with the sun. A region without an hour (the white; anchored Halden) is left as it is.
        /// </summary>
        public static Profile AtHour(Profile p, float dusk, float night)
        {
            if (!p.FollowsHour) return p;
            dusk = Mathf.Clamp01(dusk); night = Mathf.Clamp01(night);
            var r = p;
            r.SunIntensity = p.SunIntensity * (1f - 0.65f * night);
            r.Sun = Color.Lerp(Color.Lerp(p.Sun, new Color(1f, 0.78f, 0.58f), dusk * 0.5f), new Color(0.62f, 0.70f, 0.92f), night * 0.5f);
            float amb = 1f - 0.5f * night;
            r.AmbientSky = p.AmbientSky * amb;
            r.AmbientEquator = p.AmbientEquator * amb;
            r.AmbientGround = p.AmbientGround * amb;
            r.Paper = Color.Lerp(p.Paper, p.Paper * new Color(0.62f, 0.68f, 0.86f), night * 0.6f);
            return r;
        }

        /// <summary>A weighted mix of profiles, for the blend between two rooms' regions. Weights need not sum to one.</summary>
        public static Profile Lerp(Profile a, Profile b, float t)
        {
            t = Mathf.Clamp01(t);
            var r = t < 0.5f ? a : b;   // the discrete parts (shadows, hour) follow the nearer side
            r.Sun = Color.Lerp(a.Sun, b.Sun, t);
            r.SunIntensity = Mathf.Lerp(a.SunIntensity, b.SunIntensity, t);
            r.Elevation = Mathf.Lerp(a.Elevation, b.Elevation, t);
            r.Azimuth = Mathf.LerpAngle(a.Azimuth, b.Azimuth, t);
            r.AmbientSky = Color.Lerp(a.AmbientSky, b.AmbientSky, t);
            r.AmbientEquator = Color.Lerp(a.AmbientEquator, b.AmbientEquator, t);
            r.AmbientGround = Color.Lerp(a.AmbientGround, b.AmbientGround, t);
            r.Paper = Color.Lerp(a.Paper, b.Paper, t);
            r.Tint = Color.Lerp(a.Tint, b.Tint, t);
            r.TintAmount = Mathf.Lerp(a.TintAmount, b.TintAmount, t);
            r.Lamp = Color.Lerp(a.Lamp, b.Lamp, t);
            return r;
        }
    }
}
