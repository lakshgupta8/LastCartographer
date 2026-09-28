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
    /// The lantern-radius as a picture (PRG-18), measured on a real render through the project's renderer: in the
    /// Greyfold, a red wall stays red round Wren and turns to white paper beyond her radius; a dark stripe out there is
    /// white too, but keeps an outline at the edge of the eye; a narrower radius whitens what was in it; and Saltmarrow
    /// keeps its colour.
    /// </summary>
    public class LanternRenderTests
    {
        const int W = 640, H = 360;
        GameObject _root = null!, _wren = null!;
        Room _room = null!;
        Camera _cam = null!;
        RenderTexture _rt = null!;
        ClarityMeter _meter = null!;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        static Material Unlit(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", c);
            return m;
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            Shader.SetGlobalFloat(DayCycle.DuskGlobal, 0f);
            Shader.SetGlobalFloat(DayCycle.NightGlobal, 0f);
            Shader.SetGlobalFloat(HeldState.HeldGlobal, 0f);

            _root = new GameObject("Room_LanternRender");
            _room = _root.AddComponent<Room>();
            _room.RoomId = "Greyfold_Pool_1";
            var floor = new GameObject("Floor") { layer = Layer("Ground") };
            floor.transform.SetParent(_root.transform, false);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

            // A red wall just behind the gameplay plane, and a dark stripe far to the right of her.
            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(wall.GetComponent<Collider>());
            wall.transform.SetParent(_root.transform, false);
            wall.transform.position = new Vector3(0f, 0.55f, 0.6f);
            wall.transform.localScale = new Vector3(80f, 50f, 1f);
            wall.GetComponent<MeshRenderer>().sharedMaterial = Unlit(new Color(0.9f, 0.1f, 0.1f));
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(stripe.GetComponent<Collider>());
            stripe.transform.SetParent(_root.transform, false);
            stripe.transform.position = new Vector3(10f, 0.55f, 0.5f);
            stripe.transform.localScale = new Vector3(1f, 30f, 1f);
            stripe.GetComponent<MeshRenderer>().sharedMaterial = Unlit(new Color(0.05f, 0.05f, 0.08f));

            _rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var camGo = new GameObject("Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(_root.transform, false);
            camGo.transform.position = new Vector3(0f, 0.55f, -18f);
            _cam = camGo.AddComponent<Camera>();
            _cam.fieldOfView = 40f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Color.black;
            _cam.targetTexture = _rt;
            _cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>().Unlock(Ability.Clarity);
            _wren.AddComponent<Inkwell>();
            var ctrl = _wren.AddComponent<WrenController>();
            ctrl.groundMask = LayerMask.GetMask("Ground");
            ctrl.Input = new ScriptedInput();
            ctrl.Recompute();
            _wren.AddComponent<WrenVitals>();
            _meter = _wren.GetComponent<ClarityMeter>();
            ctrl.Teleport(Vector2.zero);
        }

        [TearDown]
        public void TearDown()
        {
            Lantern.Release(_root);
            if (_cam != null) _cam.targetTexture = null;
            if (_wren != null) Object.Destroy(_wren);
            if (_root != null) Object.Destroy(_root);
            if (_rt != null) _rt.Release();
            GameState.NewGame();
        }

        IEnumerator Settle()
        {
            for (int i = 0; i < 3; i++) { yield return new WaitForFixedUpdate(); yield return null; }
            _meter.SettleLantern();
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
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, "lantern-" + name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            return px;
        }

        Vector2Int Pixel(float x, float y)
        {
            var vp = _cam.WorldToViewportPoint(new Vector3(x, y, 0.6f));
            return new Vector2Int(Mathf.Clamp((int)(vp.x * W), 0, W - 1), Mathf.Clamp((int)(vp.y * H), 0, H - 1));
        }

        static Color At(Color[] px, Vector2Int p) => px[p.y * W + p.x];
        static float Lum(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
        static bool IsRed(Color c) => c.r > 0.5f && c.g < 0.35f && c.b < 0.35f;
        static bool IsPaper(Color c) => c.r > 0.8f && c.g > 0.8f && c.b > 0.75f;
        static string Say(Color c) => "(" + c.r.ToString("F2") + ", " + c.g.ToString("F2") + ", " + c.b.ToString("F2") + ")";

        [UnityTest]
        public IEnumerator BeyondHerRadiusTheGreyfoldIsWhitePaper()
        {
            yield return Settle();
            Assert.AreEqual(1, _meter.Level);
            Assert.AreEqual(1f, Shader.GetGlobalFloat(Lantern.StrengthGlobal), 0.001f);
            float r = _meter.Radius;
            Assert.AreEqual(Clarity.FullRadius(1), r, 0.001f);
            var px = Render("greyfold");

            var her = At(px, Pixel(0f, 0.55f));
            var near = At(px, Pixel(r * 0.5f, 0.55f));
            var far = At(px, Pixel(r + 2.5f, 0.55f));
            var above = At(px, Pixel(0f, 0.55f + r + 1.2f));
            Assert.IsTrue(IsRed(her), "colour round her " + Say(her));
            Assert.IsTrue(IsRed(near), "colour within her radius " + Say(near));
            Assert.IsTrue(IsPaper(far), "white paper beyond it " + Say(far));
            Assert.IsTrue(IsPaper(above), "in every direction " + Say(above));

            // The dark stripe far out: white inside, an outline at its edges.
            var mid = At(px, Pixel(10f, 0.55f));
            Assert.IsTrue(IsPaper(mid), "even ink goes to paper out there " + Say(mid));
            var edge = Pixel(9.5f, 0.55f);
            float darkest = 1f;
            for (int dx = -6; dx <= 6; dx++) darkest = Mathf.Min(darkest, Lum(At(px, new Vector2Int(Mathf.Clamp(edge.x + dx, 0, W - 1), edge.y))));
            Assert.Less(darkest, 0.8f, "but its outline survives at the edge of the eye (darkest " + darkest.ToString("F2") + ")");
            Assert.Greater(darkest, 0.2f, "as an outline, not the stripe itself");
        }

        [UnityTest]
        public IEnumerator ANarrowerRadiusWhitensWhatWasInIt()
        {
            yield return Settle();
            var inside = Pixel(3f, 0.55f);
            Assert.IsTrue(IsRed(At(Render("wide"), inside)), "three units out, inside five");
            Lantern.Hold(_root, 1.5f);
            yield return Settle();
            Assert.AreEqual(1.5f, _meter.Radius, 0.001f);
            var c = At(Render("narrow"), inside);
            Assert.IsTrue(IsPaper(c), "the bells take it, and it goes white " + Say(c));
            Assert.IsTrue(IsRed(At(Render("narrow2"), Pixel(0f, 0.55f))), "she still has her own colour");
        }

        [UnityTest]
        public IEnumerator SaltmarrowKeepsItsColour()
        {
            _room.RoomId = "Greybox_Saltmarrow_A";
            yield return Settle();
            Assert.AreEqual(0f, Shader.GetGlobalFloat(Lantern.StrengthGlobal), 0.001f);
            var px = Render("saltmarrow");
            var far = At(px, Pixel(_meter.Radius + 2.5f, 0.55f));
            Assert.IsTrue(IsRed(far), "no lantern-radius outside the Greyfold and the Blank " + Say(far));
        }
    }
}
