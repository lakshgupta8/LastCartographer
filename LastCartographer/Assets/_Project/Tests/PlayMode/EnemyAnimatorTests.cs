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
    /// An enemy drawn from sheets (CHR-06): the animator follows Enemy.Clip (a crab scuttles, hops when off the
    /// ground, is hurt, dies), its death runs the death clip's length with the ink leaving instead of the
    /// placeholder's shrink, and the placeholder's tint stands down to white.
    /// </summary>
    public class EnemyAnimatorTests
    {
        GameObject _floor, _crab;
        MarshCrab _enemy;
        InkSheetPlayer _sheet;
        EnemyAnimator _anim;
        readonly List<Texture2D> _textures = new List<Texture2D>();
        static readonly int InkId = Shader.PropertyToID("_Ink");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static int Layer(string name)
        {
            int l = LayerMask.NameToLayer(name);
            Assert.GreaterOrEqual(l, 0, "Layer missing: " + name);
            return l;
        }

        SheetClip Clip(string name, int frames, float fps, bool loop)
        {
            var tex = new Texture2D(frames * 4, 4);
            _textures.Add(tex);
            return new SheetClip { Name = name, Sheet = tex, Frames = frames, Fps = fps, Loop = loop };
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _crab = new GameObject("Crab") { layer = Layer("Enemy") };
            _crab.transform.position = new Vector3(0f, 0.36f, 0f);
            _crab.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.7f);
            _crab.AddComponent<Rigidbody2D>().freezeRotation = true;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(_crab.transform, false);
            quad.transform.localScale = new Vector3(-1.6f, 1.6f, 1f);
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = new Material(Shader.Find("OWSBG/InkSprite"));
            _sheet = _crab.AddComponent<InkSheetPlayer>();
            _sheet.Configure(r, new[] { Clip("idle", 4, 12, true), Clip("move", 6, 12, true), Clip("hop", 3, 12, false), Clip("hurt", 2, 12, false), Clip("death", 4, 12, false) });
            _enemy = _crab.AddComponent<MarshCrab>();
            _anim = _crab.AddComponent<EnemyAnimator>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_crab != null) Object.Destroy(_crab);
            Object.Destroy(_floor);
            foreach (var t in _textures) Object.Destroy(t);
            _textures.Clear();
            GameState.NewGame();
        }

        IEnumerator Steps(int n)
        {
            for (int i = 0; i < n; i++) { yield return new WaitForFixedUpdate(); yield return null; }
        }

        float Ink()
        {
            var mpb = new MaterialPropertyBlock();
            _crab.GetComponentInChildren<Renderer>().GetPropertyBlock(mpb);
            return mpb.HasFloat(InkId) ? mpb.GetFloat(InkId) : 1f;
        }

        [UnityTest]
        public IEnumerator TheCrabScuttlesHopsIsHurtAndDies()
        {
            Assert.IsTrue(_enemy.HasSheets, "the sheets are seen");
            Assert.AreEqual(4f / 12f, _enemy.DeathSeconds, 0.001f, "the death fade is the death clip's length");
            yield return Steps(12);
            Assert.AreEqual("move", _anim.Clip, "a crab on a floor scuttles");
            var mpb = new MaterialPropertyBlock();
            _crab.GetComponentInChildren<Renderer>().GetPropertyBlock(mpb);
            Assert.AreEqual(Color.white, mpb.GetColor(BaseColorId), "no placeholder tint over the drawing");

            _enemy.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0f, 9f);
            yield return Steps(3);
            Assert.AreEqual("hop", _anim.Clip, "off the ground");
            float t = 0f;
            while (_anim.Clip == "hop" && t < 3f) { t += Time.fixedDeltaTime; yield return Steps(1); }
            Assert.AreNotEqual("hop", _anim.Clip, "back down");

            Assert.IsTrue(_enemy.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.down, Source = _floor }), "a down-strike lands on a crab");
            yield return null;
            Assert.AreEqual("hurt", _anim.Clip, "the hit");
            yield return Steps(15);
            Assert.AreNotEqual("hurt", _anim.Clip, "the hit passes");

            _enemy.SetMaxHealth(1);
            Assert.IsTrue(_enemy.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.down, Source = _floor }), "the killing blow lands");
            yield return null;
            Assert.AreEqual("death", _anim.Clip, "the death clip");
            yield return Steps(6);
            Assert.Less(Ink(), 0.9f, "the ink leaves as it dies");
            Assert.AreEqual(Vector3.one, _crab.transform.localScale, "no placeholder shrink");
            t = 0f;
            while (_crab != null && t < 2f) { t += Time.fixedDeltaTime; yield return Steps(1); }
            Assert.IsTrue(_crab == null, "gone when the clip is done");
        }
    }
}
