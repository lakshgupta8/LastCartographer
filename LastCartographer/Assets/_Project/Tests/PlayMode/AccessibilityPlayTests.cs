#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The comfort options at work (DES-14): hitstop at the player's scale and none at 0; a pause that a hitstop can't
    /// end; the camera shake at their scale, still at 0, and leaving the camera where it found it; a toggled glide that
    /// floats without holding, lets go on landing, and turns a toggle pressed just before landing into the jump it
    /// was; Bind letting go of a toggle once it can't go on; and high-contrast ink drawn in the paper pass.
    /// </summary>
    public class AccessibilityPlayTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            Options.ResetToDefaults();
            GameState.NewGame();
        }

        [TearDown]
        public void TearDown()
        {
            Pause.End();
            Shake.Stop();
            Options.ResetToDefaults();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Fixed(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        static IEnumerator StopFor(int frames, System.Action<float> seconds)
        {
            float start = Time.unscaledTime;
            Hitstop.Request(frames);
            bool stopped = Time.timeScale == 0f;
            while (Hitstop.Active && Time.unscaledTime - start < 2f) yield return null;
            seconds(stopped ? Time.unscaledTime - start : 0f);
        }

        [UnityTest]
        public IEnumerator HitstopFollowsTheSlider()
        {
            float full = 0f, half = 0f, none = -1f;
            yield return StopFor(24, s => full = s);
            Assert.AreEqual(1f, Time.timeScale, "released");
            Options.Hitstop = 0.5f;
            yield return StopFor(24, s => half = s);
            Options.Hitstop = 0f;
            yield return StopFor(24, s => none = s);
            Assert.Greater(full, 0.3f, "24 frames as designed: " + full);
            Assert.Less(half, full * 0.8f, "half as many at a half: " + half + " of " + full);
            Assert.Greater(half, 0.1f);
            Assert.AreEqual(0f, none, "none at all at 0");
            Assert.IsFalse(Hitstop.Active);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator APauseOutlastsAHitstop()
        {
            Hitstop.Request(6);
            Pause.Begin();
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < 0.4f) yield return null;     // well past its six frames
            Assert.IsFalse(Hitstop.Active);
            Assert.AreEqual(0f, Time.timeScale, "the hitstop ended inside the pause, and time stays stopped");
            Pause.End();
            Assert.AreEqual(1f, Time.timeScale);
        }

        IEnumerator MeasureShake(float scale, System.Action<float> max)
        {
            var camGo = new GameObject("ShakeCamera") { tag = "MainCamera" };
            try
            {
                camGo.AddComponent<Camera>();
                var home = new Vector3(3f, 2f, -18f);
                camGo.transform.position = home;
                Options.Shake = scale;
                Shake.Request(FallenStar.SlamShake, 0.35f);
                float biggest = 0f, t = 0f;
                while (t < 0.6f) { t += Time.unscaledDeltaTime; yield return null; biggest = Mathf.Max(biggest, (camGo.transform.position - home).magnitude); }
                Assert.IsFalse(Shake.Active);
                Assert.Less((camGo.transform.position - home).magnitude, 1e-4f, "the camera is left where it was");
                max(biggest);
            }
            finally { Object.Destroy(camGo); }
        }

        [UnityTest]
        public IEnumerator ShakeFollowsTheSliderAndLeavesNothingBehind()
        {
            float full = 0f, quarter = 0f, none = -1f;
            yield return MeasureShake(1f, m => full = m);
            yield return MeasureShake(0.25f, m => quarter = m);
            yield return MeasureShake(0f, m => none = m);
            Assert.Greater(full, 0.05f, "it shakes: " + full);
            Assert.LessOrEqual(full, FallenStar.SlamShake * 1.5f, "within its strength");
            Assert.Less(quarter, full * 0.5f, "a quarter shakes less: " + quarter + " of " + full);
            Assert.AreEqual(0f, none, "still at 0");
        }

        // ---- holds as toggles ---------------------------------------------------------------------------------------

        GameObject _floor = null!, _wren = null!;
        WrenController _ctrl = null!;
        ScriptedInput _input = null!;

        static int Layer(string name) { int l = LayerMask.NameToLayer(name); Assert.GreaterOrEqual(l, 0, name); return l; }

        void MakeWren(bool wings)
        {
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            var abilities = _wren.AddComponent<AbilitySet>();
            if (wings) abilities.Unlock(Ability.Windmemory);
            _wren.AddComponent<Inkwell>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Abilities = abilities;
            _ctrl.Recompute();
            _ctrl.Teleport(Vector2.zero);
        }

        void Unmake()
        {
            if (_wren != null) Object.Destroy(_wren);
            if (_floor != null) Object.Destroy(_floor);
        }

        IEnumerator FallFrom(float y)
        {
            _ctrl.Teleport(new Vector2(0f, y));
            yield return Fixed(12);     // past coyote time, falling
            Assert.IsFalse(_ctrl.IsGrounded);
            Assert.Less(_ctrl.Velocity.y, 0f);
        }

        [UnityTest]
        public IEnumerator AToggledGlideFloatsWithoutHolding()
        {
            MakeWren(wings: true);
            try
            {
                // Held (the default): a press in the air, let go at once, is no glide.
                yield return FallFrom(12f);
                _input.PressJump();
                yield return Fixed(6);
                Assert.IsFalse(_ctrl.IsGliding, "held: no glide without holding");
                Assert.IsFalse(_ctrl.GlideLatched);

                Options.SetToggle(Hold.Glide, true);
                yield return FallFrom(12f);
                _input.PressJump();
                yield return Fixed(6);
                Assert.IsTrue(_ctrl.GlideLatched, "toggled: the press switches it on");
                Assert.IsTrue(_ctrl.IsGliding, "and she glides with nothing held");
                Assert.GreaterOrEqual(_ctrl.Velocity.y, -_ctrl.glideMaxFall - 0.01f, "at a glide's fall");

                _input.PressJump();
                yield return Fixed(3);
                Assert.IsFalse(_ctrl.GlideLatched, "the next press switches it off");
                Assert.IsFalse(_ctrl.IsGliding);

                _input.PressJump();
                yield return Fixed(3);
                Assert.IsTrue(_ctrl.IsGliding);
                float t = 0f;
                while (!_ctrl.IsGrounded && t < 10f) { t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
                Assert.IsTrue(_ctrl.IsGrounded);
                yield return Fixed(2);
                Assert.IsFalse(_ctrl.GlideLatched, "landing lets go");
                Assert.Less(_ctrl.Position.y, 0.1f, "and she stays down");

                // A toggle a moment before landing was a jump pressed early: she jumps when she lands.
                yield return FallFrom(6f);
                _ctrl.Teleport(new Vector2(0f, 0.1f));     // a hand's breadth off the floor, long past coyote time
                _input.PressJump();
                yield return new WaitForFixedUpdate();
                Assert.IsTrue(_ctrl.GlideLatched, "in the air, the press was a toggle");
                float top = 0f;
                for (int i = 0; i < 40; i++) { yield return new WaitForFixedUpdate(); top = Mathf.Max(top, _ctrl.Position.y); }
                Assert.Greater(top, 1.5f, "she jumped on landing (top " + top + ")");
            }
            finally { Unmake(); }
        }

        [UnityTest]
        public IEnumerator BindLetsGoOfAToggleOnceItCantGoOn()
        {
            MakeWren(wings: false);
            var vitals = _wren.AddComponent<WrenVitals>();
            int released = 0;
            System.Action<Hold> on = h => { if (h == Hold.Bind) released++; };
            Controls.Released += on;
            try
            {
                yield return Fixed(10);
                Assert.AreEqual(vitals.MaxMasks, vitals.Masks, "full: there is nothing to bind");
                _input.BindHeld = true;     // as a latched toggle reads
                yield return Fixed(3);
                Assert.Greater(released, 0, "the toggle is told to let go");
            }
            finally { Controls.Released -= on; Unmake(); }
        }

        // ---- high-contrast ink ---------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator HighContrastInkDrawsTheEdgesAndQuietsTheGrain()
        {
            const int W = 320, H = 180;
            Shader.SetGlobalFloat(Lantern.StrengthGlobal, 0f);
            Shader.SetGlobalFloat(DayCycle.DuskGlobal, 0f);
            Shader.SetGlobalFloat(DayCycle.NightGlobal, 0f);
            Shader.SetGlobalFloat(HeldState.HeldGlobal, 0f);
            var root = new GameObject("ContrastRender");
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            try
            {
                Material Unlit(Color c) { var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor", c); return m; }
                var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wall.transform.SetParent(root.transform, false);
                wall.transform.position = new Vector3(0f, 0f, 0.6f);
                wall.transform.localScale = new Vector3(80f, 50f, 1f);
                wall.GetComponent<MeshRenderer>().sharedMaterial = Unlit(new Color(0.62f, 0.60f, 0.56f));
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Quad);
                stripe.transform.SetParent(root.transform, false);
                stripe.transform.position = new Vector3(2f, 0f, 0.5f);
                stripe.transform.localScale = new Vector3(3f, 30f, 1f);
                stripe.GetComponent<MeshRenderer>().sharedMaterial = Unlit(new Color(0.40f, 0.38f, 0.36f));
                var camGo = new GameObject("Camera");
                camGo.transform.SetParent(root.transform, false);
                camGo.transform.position = new Vector3(0f, 0f, -18f);
                var cam = camGo.AddComponent<Camera>();
                cam.fieldOfView = 40f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.targetTexture = rt;
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                yield return null;

                Color[] Render()
                {
                    cam.Render();
                    var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;
                    var px = tex.GetPixels();
                    Object.Destroy(tex);
                    return px;
                }
                static float Lum(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                Vector2Int P(float x) { var vp = cam.WorldToViewportPoint(new Vector3(x, 0f, 0.55f)); return new Vector2Int((int)(vp.x * W), (int)(vp.y * H)); }
                float Darkest(Color[] px, Vector2Int at) { float d = 1f; for (int dx = -4; dx <= 4; dx++) d = Mathf.Min(d, Lum(px[at.y * W + at.x + dx])); return d; }
                float Spread(Color[] px, Vector2Int at)
                {
                    float sum = 0f, sq = 0f; int n = 0;
                    for (int dy = -6; dy <= 6; dy++) for (int dx = -6; dx <= 6; dx++) { float l = Lum(px[(at.y + dy) * W + at.x + dx]); sum += l; sq += l * l; n++; }
                    float mean = sum / n;
                    return Mathf.Sqrt(Mathf.Max(0f, sq / n - mean * mean));
                }

                var edge = P(0.5f);          // the stripe's left edge
                var flat = P(-6f);           // plain wall
                var inStripe = P(2f);
                var warm = Render();
                Options.HighContrast = true;
                Assert.AreEqual(1f, Shader.GetGlobalFloat(Options.ContrastId));
                var high = Render();

                float warmEdge = Darkest(warm, edge), highEdge = Darkest(high, edge);
                Assert.Less(highEdge, 0.12f, "the edge is drawn in near-black (" + highEdge.ToString("F2") + ")");
                Assert.Greater(warmEdge, Lum(warm[inStripe.y * W + inStripe.x]) - 0.05f, "the warm pass draws no line of its own");
                float warmGap = Lum(warm[flat.y * W + flat.x]) - Lum(warm[inStripe.y * W + inStripe.x]);
                float highGap = Lum(high[flat.y * W + flat.x]) - Lum(high[inStripe.y * W + inStripe.x]);
                Assert.Greater(highGap, warmGap * 1.15f, "lights and darks further apart (" + warmGap.ToString("F3") + " → " + highGap.ToString("F3") + ")");
                Assert.Less(Spread(high, flat), Spread(warm, flat) * 0.6f, "less grain on the plain wall");
                cam.targetTexture = null;
            }
            finally
            {
                Object.Destroy(root);
                rt.Release();
            }
        }
    }
}
