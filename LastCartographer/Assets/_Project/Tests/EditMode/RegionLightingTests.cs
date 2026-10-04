using System.IO;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OWSBG.Tests
{
    /// <summary>
    /// The light of each region (ENV-10, docs/design/lighting.md): every region has its own hour in the table and
    /// the white has none; the day dims the coast and not the white; the post is written as one profile asset per
    /// region; the persistent scene carries the rig and Wren her lantern; the rooms that burn carry lights; the
    /// ink shader takes the lamps.
    /// </summary>
    public class RegionLightingTests
    {
        const string Scenes = "Assets/_Project/Scenes/Greybox/";
        const string Rendering = "Assets/_Project/Settings/Rendering/";

        static string SceneText(string scene) => File.ReadAllText(Path.GetFullPath(Scenes + "Greybox_" + scene + ".unity"));
        static int Count(string text, string needle) => text.Split(new[] { needle }, System.StringSplitOptions.None).Length - 1;
        /// <summary>How many Light components a scene file carries (class id 108).</summary>
        static int Lights(string scene) => Count(SceneText(scene), "--- !u!108 &");
        static string ScriptGuid(string path) => AssetDatabase.AssetPathToGUID(path);

        static void Near(Color a, Color b, string what)
        {
            Assert.AreEqual(a.r, b.r, 0.01f, what + " r"); Assert.AreEqual(a.g, b.g, 0.01f, what + " g"); Assert.AreEqual(a.b, b.b, 0.01f, what + " b");
        }

        [Test]
        public void EveryRegionHasItsOwnHour()
        {
            var regions = (Region[])System.Enum.GetValues(typeof(Region));
            Assert.AreEqual(regions.Length, RegionLight.Count, "a row per region");
            foreach (var r in regions)
            {
                var p = RegionLight.For(r);
                Assert.AreEqual(r, p.Region, r + " has its own row");
                Assert.IsFalse(string.IsNullOrEmpty(p.Hour), r + " names its hour");
                Assert.Greater(p.SunIntensity, 0f, r + " has a sun");
                Assert.Greater(p.Lamp.r + p.Lamp.g + p.Lamp.b, 0f, r + " has a lamp colour");
            }
            // The coast keeps the sun every room shared before the regions had their own.
            var coast = RegionLight.For(Region.Saltmarrow);
            Near(new Color(1f, 0.95f, 0.86f), coast.Sun, "the coast's sun");
            Assert.AreEqual(1.4f, coast.SunIntensity, 0.001f);
            Assert.AreEqual(40f, coast.Elevation, 0.001f); Assert.AreEqual(-20f, coast.Azimuth, 0.001f);
            Assert.IsTrue(coast.Shadows && coast.FollowsHour);
            // The mine has no sky: a dim shaft, a dark ambient, ember lamps.
            var mine = RegionLight.For(Region.Emberdown);
            Assert.Less(mine.SunIntensity, 0.7f, "no sky in the mine");
            Assert.Less(mine.AmbientSky.r + mine.AmbientSky.g + mine.AmbientSky.b, 1f, "dark between the lamps");
            Assert.IsTrue(mine.Lamp.r > 0.9f && mine.Lamp.b < 0.3f, "its lamps are ember");
            Assert.Greater(mine.Bloom, coast.Bloom, "the embers bloom");
            // Halden is always late afternoon, and anchored: the day does not touch it.
            var plateau = RegionLight.For(Region.Halden);
            Assert.Less(plateau.Elevation, 30f, "a long light low across the plateau");
            Assert.Greater(plateau.Sun.r, plateau.Sun.b, "a warm one");
            Assert.IsFalse(plateau.FollowsHour, "its hour is locked");
            Assert.Greater(plateau.Temperature, 0f, "graded warm");
            // Windreach: storm light, a violet ambient under a gold sun.
            var steppe = RegionLight.For(Region.Windreach);
            Assert.Greater(steppe.AmbientSky.b, steppe.AmbientSky.g, "violet banked on the horizon");
            // The white: a white sun from the front, no shadows, no vignette, the paper nearly white, the colour pulled down.
            foreach (var white in new[] { Region.Greyfold, Region.Blank })
            {
                var p = RegionLight.For(white);
                Near(Color.white, p.Sun, white + " sun");
                Assert.IsFalse(p.Shadows, white + " casts no shadow");
                Assert.IsFalse(p.FollowsHour, white + " has no hour");
                Assert.AreEqual(0f, p.Vignette, 0.001f, white + " has no vignette");
                Assert.IsTrue(p.Paper.r > 0.95f && p.Paper.g > 0.95f && p.Paper.b > 0.95f, white + " is white paper");
                Assert.Less(p.Saturation, 0f, white + " loses colour");
                Assert.Less(p.Elevation, 25f, white + " lights the paper from the front");
            }
            Assert.Greater(RegionLight.For(Region.Blank).Bloom, RegionLight.For(Region.Greyfold).Bloom, "the lantern's colour blooms in the Blank");
            // No two regions share a sun.
            var suns = regions.Select(r => { var p = RegionLight.For(r); return (p.Sun, p.SunIntensity, p.Elevation, p.Azimuth); }).Distinct().Count();
            Assert.AreEqual(regions.Length, suns, "every region's sun is its own");
        }

        [Test]
        public void TheDayDimsTheCoastAndNotTheWhite()
        {
            var day = RegionLight.For(Region.Saltmarrow);
            var night = RegionLight.AtHour(day, 0f, 1f);
            Assert.Less(night.SunIntensity, day.SunIntensity * 0.5f, "night dims the sun to a third");
            Assert.Greater(night.Sun.b / night.Sun.r, day.Sun.b / day.Sun.r, "and cools it");
            Assert.Less(night.AmbientSky.r, day.AmbientSky.r, "the ambient drops with it");
            Assert.Less(night.Paper.r, day.Paper.r, "the paper darkens");
            var dusk = RegionLight.AtHour(day, 1f, 0f);
            Assert.Greater(dusk.Sun.r / dusk.Sun.b, day.Sun.r / day.Sun.b, "dusk warms it");
            Assert.AreEqual(day.SunIntensity, dusk.SunIntensity, 0.001f, "and does not dim it");
            foreach (var white in new[] { Region.Greyfold, Region.Blank, Region.Halden })
            {
                var p = RegionLight.For(white);
                var n = RegionLight.AtHour(p, 1f, 1f);
                Assert.AreEqual(p.SunIntensity, n.SunIntensity, 0.001f, white + " has no night");
                Near(p.Sun, n.Sun, white + " sun at night");
                Near(p.Paper, n.Paper, white + " paper at night");
            }
        }

        [Test]
        public void TheBlendMeetsBothEnds()
        {
            var a = RegionLight.For(Region.Saltmarrow);
            var b = RegionLight.For(Region.Greyfold);
            var at0 = RegionLight.Lerp(a, b, 0f);
            var at1 = RegionLight.Lerp(a, b, 1f);
            var mid = RegionLight.Lerp(a, b, 0.5f);
            Near(a.Sun, at0.Sun, "start"); Assert.IsTrue(at0.Shadows); Assert.AreEqual(a.Elevation, at0.Elevation, 0.001f);
            Near(b.Sun, at1.Sun, "end"); Assert.IsFalse(at1.Shadows); Assert.AreEqual(b.Elevation, at1.Elevation, 0.001f);
            Assert.AreEqual((a.SunIntensity + b.SunIntensity) * 0.5f, mid.SunIntensity, 0.001f, "halfway between the suns");
            Assert.AreEqual((a.Paper.r + b.Paper.r) * 0.5f, mid.Paper.r, 0.001f, "halfway between the papers");
            Assert.IsFalse(mid.Shadows, "halfway, the shadows follow the side being entered");
        }

        [Test]
        public void ARoomLightsByItsRegion()
        {
            Assert.AreEqual(Region.Halden, RegionLight.ForRoom("Greybox_Halden_Bridges_1").Region);
            Assert.AreEqual(Region.Blank, RegionLight.ForRoom("Blank_Hollow_1").Region);
            Assert.AreEqual(Region.Greyfold, RegionLight.ForRoom("Greybox_Greyfold_Edge").Region);
            Assert.AreEqual(Region.Saltmarrow, RegionLight.ForRoom("Greybox_Saltmarrow_A").Region);
            Assert.AreEqual(Region.Saltmarrow, RegionLight.ForRoom("").Region, "no room: the coast");
            Assert.AreEqual(Region.Emberdown, RegionLight.ForRoom("Emberdown_Stair_2").Region);
            Near(RegionLight.For(Region.Emberdown).Lamp, RegionLight.LampColour("Emberdown_Stair_2"), "the mine's lamps are ember");
        }

        [Test]
        public void EachRegionsPostIsAProfileAsset()
        {
            foreach (Region r in System.Enum.GetValues(typeof(Region)))
            {
                var path = Rendering + "PP_" + r + ".asset";
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                Assert.IsNotNull(profile, path);
                var p = RegionLight.For(r);
                Assert.IsTrue(profile.TryGet<Bloom>(out var bloom) && bloom.active, r + " bloom");
                Assert.AreEqual(p.Bloom, bloom.intensity.value, 0.001f, r + " bloom intensity");
                Assert.AreEqual(p.BloomThreshold, bloom.threshold.value, 0.001f, r + " bloom threshold");
                Assert.IsTrue(profile.TryGet<Vignette>(out var vig), r + " vignette");
                Assert.AreEqual(p.Vignette, vig.intensity.value, 0.001f, r + " vignette intensity");
                Assert.IsTrue(profile.TryGet<ColorAdjustments>(out var adj), r + " colour adjustments");
                Assert.AreEqual(p.Saturation, adj.saturation.value, 0.001f, r + " saturation");
                Assert.AreEqual(p.Contrast, adj.contrast.value, 0.001f, r + " contrast");
                Assert.IsTrue(profile.TryGet<WhiteBalance>(out var wb), r + " white balance");
                Assert.AreEqual(p.Temperature, wb.temperature.value, 0.001f, r + " temperature");
                Assert.IsFalse(profile.Has<DepthOfField>(), r + " leaves the depth of field to the base volume");
            }
            // The base volume keeps what every region shares.
            var baseProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Rendering + "PP_Default.asset");
            Assert.IsNotNull(baseProfile);
            Assert.IsTrue(baseProfile.Has<DepthOfField>() && baseProfile.Has<Tonemapping>());
        }

        [Test]
        public void ThePersistentSceneCarriesTheRigAndWrenHerLantern()
        {
            var bootstrap = File.ReadAllText(Path.GetFullPath(EditorBuildSettings.scenes[0].path));
            Assert.GreaterOrEqual(Count(bootstrap, ScriptGuid("Assets/_Project/Code/World/RegionLighting.cs")), 1, "the region lighting rig");
            Assert.GreaterOrEqual(Count(bootstrap, ScriptGuid("Assets/_Project/Code/World/LanternLight.cs")), 1, "her lantern's light");
            foreach (Region r in System.Enum.GetValues(typeof(Region)))
                Assert.GreaterOrEqual(Count(bootstrap, "m_Name: Volume_" + r + "\n"), 1, "a volume for " + r);
            foreach (Region r in System.Enum.GetValues(typeof(Region)))
                Assert.GreaterOrEqual(Count(bootstrap, AssetDatabase.AssetPathToGUID(Rendering + "PP_" + r + ".asset")), 1, r + "'s profile on its volume");
            Assert.GreaterOrEqual(Count(bootstrap, "--- !u!108 &"), 2, "the sun and her lantern");
        }

        [Test]
        public void TheRoomsThatBurnCarryLights()
        {
            Assert.GreaterOrEqual(Lights("Saltmarrow_Lighthouse"), 1, "the fourth lamp");
            Assert.GreaterOrEqual(Lights("Emberdown_Stair_1"), 3, "the furnace doors on the stair");
            Assert.GreaterOrEqual(Lights("Emberdown_Stair_3"), 3);
            Assert.GreaterOrEqual(Lights("Emberdown_Baths_1"), 2, "the springs' sulphur");
            Assert.GreaterOrEqual(Lights("Windreach_Camp_2"), 1, "the camp's fire");
            Assert.GreaterOrEqual(Lights("Windreach_Fire_2"), 1, "Idrenne's hearth");
            Assert.GreaterOrEqual(Lights("Greyfold_LastCamp_1"), 1, "her last camp's lamp");
            Assert.GreaterOrEqual(Lights("Blank_Hollow_1"), 2, "the Lantern's colour");
            Assert.GreaterOrEqual(Lights("Blank_Capital_1"), 1, "the lamp in the doorway");
            Assert.GreaterOrEqual(Lights("Blank_Aury_2"), 1, "Aury's light, still turning");
            Assert.AreEqual(0, Lights("Greyfold_Road_1"), "nothing burns on the Road That Stops");
            Assert.AreEqual(0, Lights("Saltmarrow_Stilts"), "nor on the stilts by day");
            // The lamp in the lighthouse is the travel point's: off until its vantage is drawn.
            var lighthouse = SceneText("Saltmarrow_Lighthouse");
            Assert.GreaterOrEqual(Count(lighthouse, "m_Name: Light\n"), 1, "the lamp's light under the travel point");
            Assert.GreaterOrEqual(Count(lighthouse, "_light: {fileID:"), 1, "wired to it");
        }

        [Test]
        public void TheInkShaderTakesTheLamps()
        {
            var ink = Shader.Find("OWSBG/InkSprite");
            Assert.IsNotNull(ink); Assert.IsTrue(ink.isSupported, "the ink shader compiles with the lamp loop");
            var keywords = ink.keywordSpace.keywordNames;
            CollectionAssert.Contains(keywords, "_ADDITIONAL_LIGHTS", "the lamps are lit per pixel");
            CollectionAssert.Contains(keywords, "_CLUSTER_LIGHT_LOOP", "through the Forward+ clusters");
            var grain = Shader.Find("OWSBG/FullScreen/PaperGrain");
            Assert.IsNotNull(grain); Assert.IsTrue(grain.isSupported, "the grain pass compiles with the region tint");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Rendering + "URP_Pipeline.asset");
            Assert.IsNotNull(pipeline);
            Assert.AreEqual(LightRenderingMode.PerPixel, pipeline.additionalLightsRenderingMode, "the pipeline lights the lamps per pixel");
            Assert.IsFalse(pipeline.supportsAdditionalLightShadows, "no lamp casts a shadow (the 1060 budget)");
        }
    }
}
