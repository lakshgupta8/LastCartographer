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
    /// Halvard drawn (CHR-07): his moves name their clips, the survey and the count with telegraphs of their own, and
    /// the animator plays what he names; the placeholder's lean stands down when he wears sheets.
    /// </summary>
    public class WardenClipPlayTests
    {
        GameObject _go;
        Halvard _boss;
        EnemyAnimator _anim;
        readonly List<Texture2D> _textures = new List<Texture2D>();

        SheetClip Clip(string name, int frames, float fps = 12f, bool loop = false)
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
            _go = new GameObject("Halvard") { layer = LayerMask.NameToLayer("Enemy") };
            _go.transform.position = new Vector3(4f, 0.9f, 0f);
            _go.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.8f);
            _go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(_go.transform, false);
            quad.transform.localScale = new Vector3(-2.8f, 2.8f, 1f);
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = new Material(Shader.Find("OWSBG/InkSprite"));
            _go.AddComponent<InkSheetPlayer>().Configure(r, new[]
            {
                Clip("idle", 4, 12, true), Clip("move", 8, 12, true), Clip("telegraph", 3), Clip("thrust", 3, 24), Clip("lunge", 4, 24),
                Clip("survey", 4), Clip("call", 4), Clip("count", 3, 24), Clip("recover", 3), Clip("hurt", 2), Clip("death", 6),
            });
            _boss = _go.AddComponent<Halvard>();
            _anim = _go.AddComponent<EnemyAnimator>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            foreach (var t in _textures) Object.Destroy(t);
            _textures.Clear();
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator HisMovesNameTheirClipsAndTheAnimatorPlaysThem()
        {
            yield return null;
            Assert.IsTrue(_boss.HasSheets, "the sheets are seen");
            Assert.AreEqual("idle", _boss.Clip, "standing");
            Assert.AreEqual("idle", _anim.Clip);
            var scale = _go.GetComponentInChildren<MeshRenderer>().transform.localScale;

            _boss.ForceAttack(Halvard.Attack.Thrust);
            Assert.AreEqual(Halvard.Move.Telegraph, _boss.Current);
            Assert.AreEqual("telegraph", _boss.Clip, "a thrust's telegraph");
            yield return null;
            Assert.AreEqual("telegraph", _anim.Clip);
            Assert.AreEqual(scale.y, _go.GetComponentInChildren<MeshRenderer>().transform.localScale.y, 0.001f, "no lean on the drawing");
            Assert.AreEqual(Mathf.Abs(scale.x), Mathf.Abs(_go.GetComponentInChildren<MeshRenderer>().transform.localScale.x), 0.001f, "the frames carry the lean");

            _boss.ForceAttack(Halvard.Attack.Survey);
            Assert.AreEqual("survey", _boss.Clip, "the lance planted");
            yield return null;
            Assert.AreEqual("survey", _anim.Clip);

            _boss.ForceAttack(Halvard.Attack.Count);
            Assert.AreEqual("call", _boss.Clip, "the count called");
            yield return null;
            Assert.AreEqual("call", _anim.Clip);

            Assert.AreEqual(6f / 12f, _boss.DeathSeconds, 0.001f, "his withdrawal runs the death clip's length");
        }
    }
}
