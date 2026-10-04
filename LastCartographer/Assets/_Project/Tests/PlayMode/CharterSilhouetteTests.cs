using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Her silhouette changes with her Charter (CHR-05, combat doc 5): on a synthetic Wren with the real controller,
    /// strike, flourishes, animator and Charters, wearing a Charter swaps the sheet player to that Charter's
    /// drawing of the same clips without restarting what plays, the tint stands down while a drawing exists, and a
    /// Charter not yet drawn falls back to the base sheets under its tint.
    /// </summary>
    public class CharterSilhouetteTests
    {
        GameObject _floor, _wren;
        WrenController _ctrl;
        CharterSet _charters;
        InkSheetPlayer _sheet;
        MeshRenderer _quad;
        ScriptedInput _input;
        readonly List<Texture2D> _textures = new List<Texture2D>();
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly string[] ClipNames = { "idle", "run", "jump", "fall", "land", "strike1", "strike2", "strike3", "strike_up", "pogo", "hurt", "death" };

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        List<SheetClip> Clips(string set)
        {
            var list = new List<SheetClip>();
            foreach (var name in ClipNames)
            {
                bool once = name != "idle" && name != "run" && name != "fall";
                var tex = new Texture2D(32, 4) { name = (set ?? "Wren") + "_" + name };
                _textures.Add(tex);
                list.Add(new SheetClip { Name = name, Sheet = tex, Frames = 8, Fps = name.StartsWith("strike") ? 24f : 12f, Loop = !once });
            }
            return list;
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            _wren.SetActive(false);   // everything on before anything wakes, as in the built scene
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _wren.AddComponent<WrenVitals>();
            _wren.AddComponent<QuillStrike>().hitMask = LayerMask.GetMask("Hittable", "Enemy");
            _wren.AddComponent<Flourishes>().hitMask = LayerMask.GetMask("Hittable", "Enemy");
            _charters = _wren.AddComponent<CharterSet>();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(_wren.transform, false);
            _quad = quad.GetComponent<MeshRenderer>();
            _sheet = _wren.AddComponent<InkSheetPlayer>();
            _sheet.Configure(_quad, Clips(null));
            // Every Charter drawn but the Remnant, which waits on the base sheets under its grey.
            var sets = new List<SheetSet>();
            foreach (var kind in new[] { CharterKind.Warden, CharterKind.Drifter, CharterKind.Ferryman, CharterKind.Unwriter })
                sets.Add(new SheetSet { Name = CharterSet.SheetSetOf(kind), Clips = Clips(CharterSet.SheetSetOf(kind)) });
            _sheet.ConfigureSets(sets);
            _wren.AddComponent<WrenAnimator>();
            _wren.SetActive(true);
            _ctrl.Recompute();
            _ctrl.Teleport(Vector2.zero);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_wren); Object.Destroy(_floor);
            foreach (var t in _textures) Object.Destroy(t);
            _textures.Clear();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        Texture Shown()
        {
            var mpb = new MaterialPropertyBlock();
            _quad.GetPropertyBlock(mpb);
            return mpb.GetTexture(BaseMapId);
        }

        /// <summary>The strip shown is the clip playing, drawn by the set worn (the base is "Wren").</summary>
        void AssertDrawnBy(string set, string because)
        {
            Assert.AreEqual((set ?? "Wren") + "_" + _sheet.Current, Shown().name, because);
        }

        Color Tint()
        {
            var mpb = new MaterialPropertyBlock();
            _quad.GetPropertyBlock(mpb);
            return mpb.GetColor(BaseColorId);
        }

        [UnityTest]
        public IEnumerator EachCharterIsWornInItsOwnDrawing()
        {
            yield return Frames(30);   // past the landing on the floor
            Assert.AreEqual(CharterKind.Surveyor, _charters.Current.Kind);
            Assert.IsNull(_sheet.Set, "the Surveyor wears the base sheets");
            Assert.AreEqual("idle", _sheet.Current);
            AssertDrawnBy(null, "the Surveyor's idle");
            Assert.AreEqual(Color.white, Tint(), "drawn: no tint over the drawing");

            foreach (var kind in new[] { CharterKind.Warden, CharterKind.Drifter, CharterKind.Ferryman, CharterKind.Unwriter })
            {
                GameState.World.Equipment.OwnedCharters.Add(kind);
                GameState.World.Equipment.SetCharter(kind);
                yield return Frames(2);
                Assert.AreEqual(kind, _charters.Current.Kind);
                Assert.AreEqual(kind.ToString(), _sheet.Set, kind + "'s set is worn");
                AssertDrawnBy(kind.ToString(), kind + "'s own drawing");
                Assert.AreEqual(Color.white, Tint(), kind + " drawn: no tint");
            }

            GameState.World.Equipment.SetCharter(CharterKind.Surveyor);
            yield return Frames(2);
            Assert.IsNull(_sheet.Set);
            AssertDrawnBy(null, "back in the Surveyor's cowl");
        }

        [UnityTest]
        public IEnumerator ASwapCarriesTheClipOnInTheNewDrawing()
        {
            _input.Move = new Vector2(1f, 0f);
            yield return Frames(30);
            Assert.AreEqual("run", _sheet.Current);
            int frame = _sheet.Frame;
            _charters.Apply(CharterKind.Drifter);
            Assert.AreEqual("run", _sheet.Current, "the clip carries on");
            Assert.AreEqual(frame, _sheet.Frame, "at the same frame");
            Assert.AreEqual("Drifter_run", Shown().name, "drawn from the Drifter's strip at once");
            _input.Move = Vector2.zero;
            yield return Frames(30);
            Assert.AreEqual("idle", _sheet.Current);
            Assert.AreEqual("Drifter_idle", Shown().name, "the next clip is the Drifter's too");
        }

        [UnityTest]
        public IEnumerator ACharterNotYetDrawnWearsTheBaseUnderItsTint()
        {
            GameState.World.Equipment.OwnedCharters.Add(CharterKind.Remnant);
            GameState.World.Equipment.SetCharter(CharterKind.Remnant);
            yield return Frames(3);
            Assert.AreEqual(CharterKind.Remnant, _charters.Current.Kind);
            Assert.IsNull(_sheet.Set, "no Remnant set here");
            AssertDrawnBy(null, "the base sheets");
            Assert.AreEqual(_charters.Current.Tint, Tint(), "the tint stands in");
        }
    }
}
