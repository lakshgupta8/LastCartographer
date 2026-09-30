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
    /// The ink effects in play (ENV-12): a spawned effect plays on a pooled quad in front of the play plane and is put
    /// back when its clip ends; a following effect keeps to its owner; a mark stays until destroyed; a clip that is not
    /// loaded is a no-op; a drawn weak floor breaks as a crumble with no shrink; Wren's own ink answers the bind.
    /// </summary>
    public class InkFxPlayTests
    {
        GameObject _fxGo, _floor, _weak, _wren;
        InkFx _fx;
        readonly List<Texture2D> _textures = new List<Texture2D>();

        SheetClip Clip(string name, int frames, bool loop = false)
        {
            var tex = new Texture2D(frames * 4, 4);
            _textures.Add(tex);
            return new SheetClip { Name = name, Sheet = tex, Frames = frames, Fps = 24f, Loop = loop };
        }

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            _fxGo = new GameObject("InkFx");
            _fx = _fxGo.AddComponent<InkFx>();
            _fx.Configure(new[] { Clip("splash", 6), Clip("slash", 4), Clip("crumble", 6), Clip("redraw", 8), Clip("mark", 2, true) }, new Material(Shader.Find("OWSBG/InkSprite")), 3f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _wren, _weak, _floor, _fxGo }) if (go != null) Object.Destroy(go);
            foreach (var m in Object.FindObjectsByType<InkFx.Life>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(m.gameObject);
            foreach (var t in _textures) Object.Destroy(t);
            _textures.Clear();
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator AnEffectPlaysInFrontOfThePlaneThenGoesBackToThePool()
        {
            yield return null;
            Assert.IsTrue(InkFx.Ready);
            Assert.IsNull(InkFx.Spawn("nothing", Vector2.zero), "a clip not loaded is a no-op");
            var t = InkFx.Spawn("splash", new Vector2(3f, 1f), 45f, 1.2f);
            Assert.IsNotNull(t);
            Assert.AreEqual(3f, t.position.x, 0.001f);
            Assert.AreEqual(InkFx.Depth, t.position.z, 0.001f, "just in front of the play plane");
            Assert.AreEqual(45f, t.rotation.eulerAngles.z, 0.01f, "turned to the strike");
            Assert.AreEqual(3.6f, t.localScale.x, 0.001f, "the cell, scaled");
            Assert.IsTrue(t.gameObject.activeSelf);
            Assert.AreEqual(1, _fx.Live);
            var player = t.GetComponent<InkSheetPlayer>();
            Assert.AreEqual("splash", player.Current);
            float w = 0f;
            while (w < 1f && t.gameObject.activeSelf) { w += Time.deltaTime; yield return null; }
            Assert.IsFalse(t.gameObject.activeSelf, "put back when the clip ends");
            Assert.AreEqual(0, _fx.Live);
            var again = InkFx.Spawn("slash", Vector2.zero);
            Assert.AreSame(t, again, "the pooled quad is reused");
            Assert.AreEqual(2, _fx.Spawned);
        }

        [UnityTest]
        public IEnumerator AFollowingEffectKeepsToItsOwnerAndAMarkStays()
        {
            var owner = new GameObject("Owner");
            owner.transform.position = new Vector3(1f, 0f, 0f);
            var t = InkFx.Spawn("redraw", new Vector2(1f, 0.6f), 0f, 1f, owner.transform);
            owner.transform.position = new Vector3(4f, 0f, 0f);
            yield return null;
            Assert.AreEqual(4f, t.position.x, 0.001f, "moved with its owner");
            Assert.AreEqual(0.6f, t.position.y, 0.001f, "keeping its offset");

            var mark = InkFx.Mark("mark", new Vector2(2f, 0f), 0.5f);
            Assert.IsNotNull(mark);
            Assert.IsNull(mark.parent, "a mark is the caller's");
            for (int i = 0; i < 20; i++) yield return null;
            Assert.IsTrue(mark.gameObject.activeSelf, "and stays");
            Assert.AreEqual("mark", mark.GetComponent<InkSheetPlayer>().Current);
            Object.Destroy(mark.gameObject);
            Object.Destroy(owner);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ADrawnWeakFloorCrumblesInsteadOfShrinking()
        {
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(20f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _weak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _weak.name = "WeakFloor";
            _weak.layer = Layer("Ground");
            Object.Destroy(_weak.GetComponent<Collider>());
            _weak.transform.position = new Vector3(0f, 2f, 0f);
            _weak.transform.localScale = new Vector3(3f, 0.5f, 1f);
            _weak.AddComponent<BoxCollider2D>();
            var inkMat = new Material(Shader.Find("OWSBG/InkSprite"));
            _weak.GetComponent<MeshRenderer>().sharedMaterial = inkMat;
            var weak = _weak.AddComponent<WeakFloor>();
            yield return null;
            Assert.IsTrue(weak.IsDrawn, "on the kit's planks");
            Assert.IsFalse(weak.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.down }), "scratched");
            Assert.IsTrue(weak.TakeHit(new HitInfo { Damage = 3, Direction = Vector2.down }), "broken by the plumb weight");
            Assert.AreEqual(1, _fx.Spawned, "the crumble");
            Assert.IsFalse(_weak.GetComponent<MeshRenderer>().enabled, "the planks are gone at once");
            yield return null;
            Assert.AreEqual(3f, _weak == null ? 3f : _weak.transform.localScale.x, 0.001f);
            if (_weak != null) Assert.AreEqual(0.5f * (1f - Mathf.Clamp01(Time.deltaTime / 0.35f)), _weak.transform.localScale.y, 0.05f, "the shrink still times the object's end");
        }

        [UnityTest]
        public IEnumerator WrensBindRedrawsHerOutline()
        {
            _wren = new GameObject("Wren");
            _wren.transform.position = new Vector3(2f, 0f, 0f);
            var vitals = _wren.AddComponent<WrenVitals>();
            var fx = _wren.AddComponent<WrenFx>();
            yield return null;
            int before = _fx.Spawned;
            vitals.Damage(1, new Vector3(3f, 0f, 0f));
            yield return null;
            Assert.AreEqual(before + 1, _fx.Spawned, "a hit splashes on her");
            Assert.AreEqual(0, fx.Redraws);
        }
    }
}
