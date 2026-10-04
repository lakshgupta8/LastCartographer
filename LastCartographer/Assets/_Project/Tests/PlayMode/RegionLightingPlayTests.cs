#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The regions' light in play (ENV-10, docs/design/lighting.md): the room's region lights the sun, the ambient,
    /// the paper and its volume, and the next region blends in over a breath; night dims the coast and not the
    /// white; a lamp is a pool on the paper in a real render; her lantern is a light in the white; a travel lamp's
    /// light comes on with its glow; the fights that burn are lights.
    /// </summary>
    public class RegionLightingPlayTests
    {
        const int W = 640, H = 360;
        GameObject _root = null!;
        Light _sun = null!;
        Camera _cam = null!;
        RenderTexture _rt = null!;
        RegionLighting _lighting = null!;
        Room _room = null!;
        readonly List<GameObject> _made = new List<GameObject>();

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static HitInfo Strike(Vector2 dir) => new HitInfo { Damage = 1, Direction = dir };
        static float Lum(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
        static void Near(Color a, Color b, string what)
        {
            Assert.AreEqual(a.r, b.r, 0.02f, what + " r"); Assert.AreEqual(a.g, b.g, 0.02f, what + " g"); Assert.AreEqual(a.b, b.b, 0.02f, what + " b");
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Shader.SetGlobalFloat(DayCycle.DuskGlobal, 0f);
            Shader.SetGlobalFloat(DayCycle.NightGlobal, 0f);
            Shader.SetGlobalFloat(HeldState.HeldGlobal, 0f);
            Shader.SetGlobalFloat(Lantern.StrengthGlobal, 0f);

            _root = new GameObject("LightingRig");
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(_root.transform, false);
            _sun = sunGo.AddComponent<Light>();
            _sun.type = LightType.Directional;

            _rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var camGo = new GameObject("Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(_root.transform, false);
            camGo.transform.position = new Vector3(0f, 0f, -18f);
            _cam = camGo.AddComponent<Camera>();
            _cam.fieldOfView = 40f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Color.black;
            _cam.targetTexture = _rt;
            _cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            _lighting = _root.AddComponent<RegionLighting>();
            var volumes = new List<Volume>();
            foreach (Region r in System.Enum.GetValues(typeof(Region))) volumes.Add(RegionLighting.MakeVolume(_root.transform, r));
            _lighting.Configure(_sun, _cam, volumes);
            _lighting.BlendSeconds = 0.4f;

            var roomGo = new GameObject("Room_Test");
            _room = roomGo.AddComponent<Room>();
            _room.RoomId = "Saltmarrow_A";
            _made.Add(roomGo);
        }

        [TearDown]
        public void TearDown()
        {
            Lantern.Release(_root);
            if (_cam != null) _cam.targetTexture = null;
            foreach (var go in _made) if (go != null) Object.Destroy(go);
            _made.Clear();
            if (_root != null) Object.Destroy(_root);
            if (_rt != null) _rt.Release();
            Shader.SetGlobalFloat(Lantern.StrengthGlobal, 0f);
            GameState.NewGame();
        }

        IEnumerator Blended()
        {
            float t = 0f;
            while (_lighting.Blend < 1f && t < 5f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheRoomsRegionLightsTheSceneAndTheNextBlendsIn()
        {
            yield return null; yield return null;
            var coast = RegionLight.For(Region.Saltmarrow);
            Assert.AreEqual(Region.Saltmarrow, _lighting.Current);
            Near(coast.Sun, _sun.color, "the coast's sun");
            Assert.AreEqual(coast.SunIntensity, _sun.intensity, 0.001f);
            Assert.AreEqual(LightShadows.Soft, _sun.shadows);
            Assert.AreEqual(1f, _lighting.VolumeOf(Region.Saltmarrow)!.weight, 0.001f, "the coast's post is in");
            Assert.AreEqual(0f, _lighting.VolumeOf(Region.Greyfold)!.weight, 0.001f, "the white's is not");
            Near(coast.Paper, _cam.backgroundColor, "the camera clears to the coast's paper");
            Near(coast.AmbientSky, RenderSettings.ambientSkyColor, "the coast's sky");

            _room.RoomId = "Greyfold_Pool_1";
            yield return null;
            Assert.AreEqual(Region.Greyfold, _lighting.Current, "the room's region changed");
            Assert.AreEqual(Region.Saltmarrow, _lighting.Previous);
            Assert.Less(_lighting.Blend, 1f, "the light blends over a breath, not at once");
            float wCoast = _lighting.VolumeOf(Region.Saltmarrow)!.weight, wWhite = _lighting.VolumeOf(Region.Greyfold)!.weight;
            Assert.AreEqual(1f, wCoast + wWhite, 0.001f, "the two posts share the weight mid-blend");
            Assert.Greater(wWhite, 0f);

            yield return Blended();
            var white = RegionLight.For(Region.Greyfold);
            Assert.AreEqual(1f, _lighting.Blend, 0.001f);
            Near(white.Sun, _sun.color, "a white sun");
            Assert.AreEqual(LightShadows.None, _sun.shadows, "that casts no shadow");
            Assert.AreEqual(white.Elevation, _sun.transform.rotation.eulerAngles.x, 0.5f, "from the front");
            Near(white.Paper, _cam.backgroundColor, "the camera clears to white paper");
            Near(white.AmbientSky, RenderSettings.ambientSkyColor, "the white's sky");
            Assert.AreEqual(1f, _lighting.VolumeOf(Region.Greyfold)!.weight, 0.001f);
            Assert.AreEqual(0f, _lighting.VolumeOf(Region.Saltmarrow)!.weight, 0.001f);
            Assert.IsFalse(_lighting.VolumeOf(Region.Saltmarrow)!.gameObject.activeSelf, "a volume with no weight is off");
            Assert.AreEqual(0f, Shader.GetGlobalVector(RegionLighting.TintGlobal).w, 0.001f, "the white has no tint on the grain");
        }

        [UnityTest]
        public IEnumerator NightDimsTheCoastButNotTheWhite()
        {
            _lighting.NightOverride = 1f;
            yield return null; yield return null;
            var coast = RegionLight.For(Region.Saltmarrow);
            Assert.Less(_sun.intensity, coast.SunIntensity * 0.5f, "the coast's sun at night");
            Assert.Greater(_sun.color.b / _sun.color.r, coast.Sun.b / coast.Sun.r, "cooled");
            Assert.Less(_cam.backgroundColor.r, coast.Paper.r, "the paper darkens");

            _room.RoomId = "Greyfold_Pool_1";
            _lighting.Snap();
            var white = RegionLight.For(Region.Greyfold);
            Assert.AreEqual(white.SunIntensity, _sun.intensity, 0.001f, "the white has no night");
            Near(white.Paper, _cam.backgroundColor, "white paper at night");

            _lighting.NightOverride = -1f;
            _room.RoomId = "Halden_Bridges_1";
            _lighting.Snap();
            Assert.AreEqual(RegionLight.For(Region.Halden).SunIntensity, _sun.intensity, 0.001f, "Halden is always late afternoon");
        }

        Color[] Render(string name)
        {
            _cam.Render();
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            var px = tex.GetPixels();
            var outDir = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath)!.Parent!.FullName, "logs");
            System.IO.Directory.CreateDirectory(outDir);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "lighting-" + name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            return px;
        }

        /// <summary>The mean luminance of a 9 x 9 block of pixels round a world point on the play plane (the grain averages out).</summary>
        float Patch(Color[] px, float x, float y)
        {
            var vp = _cam.WorldToViewportPoint(new Vector3(x, y, 0f));
            int cx = Mathf.Clamp((int)(vp.x * W), 4, W - 5), cy = Mathf.Clamp((int)(vp.y * H), 4, H - 5);
            float sum = 0f;
            for (int dy = -4; dy <= 4; dy++) for (int dx = -4; dx <= 4; dx++) sum += Lum(px[(cy + dy) * W + cx + dx]);
            return sum / 81f;
        }

        [UnityTest]
        public IEnumerator ALampIsAPoolOnThePaper()
        {
            yield return null; yield return null;
            _sun.enabled = false;   // no sun: only the ambient, so the lamp is what lights the paper

            // A sheet of white paper on the ink shader across the play plane, lit fully, no grain of its own.
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _made.Add(quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.position = Vector3.zero;
            quad.transform.localScale = new Vector3(20f, 12f, 1f);
            var white = new Texture2D(4, 4);
            var fill = new Color[16]; for (int i = 0; i < 16; i++) fill[i] = Color.white;
            white.SetPixels(fill); white.Apply();
            var mat = new Material(Shader.Find("OWSBG/InkSprite"));
            mat.SetTexture("_BaseMap", white);
            mat.SetFloat("_Lighting", 1f); mat.SetFloat("_Shadows", 0f); mat.SetFloat("_GrainStrength", 0f); mat.SetFloat("_ShadowStep", 0f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
            yield return null;

            var dark = Render("paper-unlit");
            float centreDark = Patch(dark, 0f, 0f), farDark = Patch(dark, 7f, 0f);

            var lampGo = new GameObject("Lamp");
            _made.Add(lampGo);
            lampGo.transform.position = new Vector3(0f, 0f, -1.2f);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = RegionLight.For(Region.Saltmarrow).Lamp;
            lamp.range = 4f;
            lamp.intensity = 6f;
            lamp.shadows = LightShadows.None;
            yield return null;

            var lit = Render("paper-lamp");
            float centreLit = Patch(lit, 0f, 0f), farLit = Patch(lit, 7f, 0f), ringLit = Patch(lit, 1.5f, 0f);
            Assert.Greater(centreLit - centreDark, 0.15f, "the paper under the lamp takes its light: " + centreDark.ToString("F2") + " -> " + centreLit.ToString("F2"));
            Assert.AreEqual(farDark, farLit, 0.05f, "and the paper beyond its range does not");
            Assert.Greater(ringLit - centreDark, 0.1f, "the pool is a pool, not a point");
            Object.Destroy(white); Object.Destroy(mat);
        }

        [UnityTest]
        public IEnumerator HerLanternIsALightInTheWhite()
        {
            _room.RoomId = "Greyfold_Pool_1";
            var floor = new GameObject("Floor") { layer = Layer("Ground") };
            _made.Add(floor);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
            var wren = new GameObject("Wren") { layer = Layer("Player") };
            _made.Add(wren);
            var box = wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            wren.AddComponent<Rigidbody2D>();
            wren.AddComponent<AbilitySet>().Unlock(Ability.Clarity);
            wren.AddComponent<Inkwell>();
            var ctrl = wren.AddComponent<WrenController>();
            ctrl.groundMask = LayerMask.GetMask("Ground");
            ctrl.Input = new ScriptedInput();
            ctrl.Recompute();
            wren.AddComponent<WrenVitals>();
            var lantern = wren.AddComponent<LanternLight>();
            var meter = wren.GetComponent<ClarityMeter>();
            Assert.IsNotNull(meter, "the controller adds the meter");
            ctrl.Teleport(Vector2.zero);

            for (int i = 0; i < 3; i++) { yield return new WaitForFixedUpdate(); yield return null; }
            meter.SettleLantern();
            yield return null; yield return null;
            Assert.IsTrue(meter.IsLanternLit, "the lantern is lit in the white");
            Assert.IsTrue(lantern.IsOn, "and so is her light");
            Assert.IsNotNull(lantern.Light);
            Assert.AreEqual(LightType.Point, lantern.Light!.type);
            Near(LanternLight.Colour, lantern.Light.color, "lantern gold");
            Assert.GreaterOrEqual(lantern.Light.range, meter.Radius, "it reaches as far as the radius");
            Assert.Greater(lantern.Light.intensity, 0f);
            Assert.AreEqual(wren.transform, lantern.Light.transform.parent, "it rides on her");

            _room.RoomId = "Saltmarrow_A";
            for (int i = 0; i < 3; i++) { yield return new WaitForFixedUpdate(); yield return null; }
            meter.SettleLantern();
            yield return null; yield return null;
            Assert.IsFalse(meter.IsLanternLit, "no lantern-radius on the coast");
            Assert.IsFalse(lantern.IsOn, "so her light is off");
        }

        [UnityTest]
        public IEnumerator TheTravelLampLightsWhenItsVantageIsDrawn()
        {
            var go = new GameObject("Lamp_Test") { layer = Layer("Trigger") };
            _made.Add(go);
            go.AddComponent<BoxCollider2D>().isTrigger = true;   // the travel point requires one
            var tp = go.AddComponent<TravelPoint>();
            Assert.IsNotNull(tp);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(TravelPoint).GetField("_waypointId", flags)!.SetValue(tp, "lamp.Test");
            typeof(TravelPoint).GetField("_kind", flags)!.SetValue(tp, WaypointKind.Lamp);
            typeof(TravelPoint).GetField("_litByVantage", flags)!.SetValue(tp, "Test/Lamp");
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            tp.ConfigureLight(light);
            Assert.IsFalse(tp.IsLit);
            Assert.IsFalse(light.enabled, "unlit until its vantage is drawn");
            yield return null;
            Assert.IsFalse(light.enabled);

            GameState.World.MarkSurveyed("Test/Lamp");
            yield return null;
            Assert.IsTrue(tp.IsLit);
            Assert.IsTrue(light.enabled, "lit with its glow once the vantage is drawn");
            Assert.AreSame(light, tp.Light);
        }

        T MakeBoss<T>(string name, Vector2 pos, Vector2 size) where T : Boss
        {
            var go = new GameObject(name) { layer = Layer("Enemy") };
            _made.Add(go);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.AddComponent<BoxCollider2D>().size = size;
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(go.transform, false);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("OWSBG/InkSprite"));
            return go.AddComponent<T>();
        }

        [UnityTest]
        public IEnumerator TheFightsThatBurnAreLights()
        {
            var floor = new GameObject("Floor") { layer = Layer("Ground") };
            _made.Add(floor);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
            var wren = new GameObject("Wren") { layer = Layer("Player") };
            _made.Add(wren);
            wren.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 1.1f);
            wren.AddComponent<Rigidbody2D>();
            wren.AddComponent<AbilitySet>();
            wren.AddComponent<Inkwell>();
            var ctrl = wren.AddComponent<WrenController>();
            ctrl.groundMask = LayerMask.GetMask("Ground");
            ctrl.Input = new ScriptedInput();
            ctrl.Recompute();
            wren.AddComponent<WrenVitals>();
            ctrl.Teleport(new Vector2(-8f, 0f));
            yield return new WaitForFixedUpdate();

            // Brann: his brass is the light, a quarter at rest.
            var brann = MakeBoss<Brann>("Brann", new Vector2(4f, 1f), new Vector2(0.9f, 1.9f));
            brann.arenaMinX = -6f; brann.arenaMaxX = 14f;
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.IsTrue(brann.GlowLight == null || !brann.GlowLight.enabled, "no light on the perch");
            brann.BeginFight();
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.IsNotNull(brann.GlowLight, "his brass as a light");
            Assert.IsTrue(brann.GlowLight!.enabled);
            Assert.AreEqual(brann.GlowIntensity, brann.GlowLight.intensity, 0.001f, "as bright as his brass");
            Assert.AreEqual(LightShadows.None, brann.GlowLight.shadows);
            Assert.AreEqual(brann.transform, brann.GlowLight.transform.parent);
            brann.ResetFight();
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.IsFalse(brann.GlowLight.enabled, "out when the fight resets");

            // The Fallen Star: no light until it burns.
            var star = MakeBoss<FallenStar>("Star", new Vector2(6f, 1.6f), new Vector2(2.4f, 3.2f));
            star.BeginFight();
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.IsFalse(star.IsBurning);
            Assert.IsTrue(star.GlowLight == null || !star.GlowLight.enabled, "cold in the first phases");
            for (int i = 0, n = BossHits.ToPhase(star, 3); i < n; i++) star.TakeHit(Strike(Vector2.down));   // only a down-strike lands on the seam
            Assert.AreEqual(3, star.Phase);
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.IsTrue(star.IsBurning, "the third phase burns");
            Assert.IsNotNull(star.GlowLight);
            Assert.IsTrue(star.GlowLight!.enabled, "and the room takes the ember light off it");
            Assert.AreEqual(FallenStar.BurnIntensity, star.GlowLight.intensity, 0.001f);
            Assert.Greater(star.GlowLight.color.r, star.GlowLight.color.b, "ember");
        }
    }
}
