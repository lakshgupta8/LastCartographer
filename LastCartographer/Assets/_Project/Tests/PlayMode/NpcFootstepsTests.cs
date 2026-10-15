using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Other birds' feet in play (AUD-23, docs/design/footsteps.md §2b): a bird drawn from sheets that walks across a
    /// block drawn as iron steps on iron a stride for its size apart, lighter and pitched by its size; standing, it
    /// makes no steps; walking in the air, none.
    /// </summary>
    public class NpcFootstepsTests
    {
        GameObject _floor, _npc;
        NpcFootsteps _feet;
        float _lx;
        readonly List<Texture2D> _textures = new List<Texture2D>();
        InkSoundBank Bank => InkSoundBank.Instance;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        SheetClip Clip(string name, int frames)
        {
            var tex = new Texture2D(frames * 4, 4);
            _textures.Add(tex);
            return new SheetClip { Name = name, Sheet = tex, Frames = frames, Fps = 12f, Loop = true };
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            GameState.NewGame();
            NpcTalker.Talking = null;
            Assert.IsNotNull(InkSoundBank.Instance, "the bank wakes with the game");
            Bank.Hush();
            _lx = Bank.ListenerX;
            _floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(_floor.GetComponent<BoxCollider>());
            var fr = _floor.GetComponent<MeshRenderer>();
            fr.sharedMaterial = new Material(fr.sharedMaterial) { name = "M_Ground_Iron" };
            _floor.name = "Floor";
            _floor.layer = Layer("Ground");
            _floor.transform.position = new Vector3(_lx, -0.5f, 0f);
            _floor.transform.localScale = new Vector3(30f, 1f, 1f);
            _floor.AddComponent<BoxCollider2D>().size = Vector2.one;

            _npc = new GameObject("Folk_Goose_1");
            _npc.transform.position = new Vector3(_lx - 3f, 0f, 0f);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(_npc.transform, false);
            quad.transform.localScale = new Vector3(2f, 2f, 1f);
            quad.transform.localPosition = new Vector3(0f, 1f, 0f);
            var r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = new Material(Shader.Find("OWSBG/InkSprite"));
            _npc.AddComponent<InkSheetPlayer>().Configure(r, new[] { Clip("idle", 8), Clip("walk", 8) });
            _npc.AddComponent<NpcAnimator>();
            _feet = _npc.GetComponent<NpcFootsteps>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _npc, _floor }) if (go != null) Object.Destroy(go);
            foreach (var t in _textures) Object.Destroy(t);
            _textures.Clear();
            if (InkSoundBank.Instance != null) InkSoundBank.Instance.Hush();
            GameState.NewGame();
        }

        IEnumerator WalkTo(float x, float speed)
        {
            while (Mathf.Abs(_npc.transform.position.x - x) > 0.01f)
            {
                var p = _npc.transform.position;
                _npc.transform.position = new Vector3(Mathf.MoveTowards(p.x, x, speed * Time.deltaTime), p.y, p.z);
                yield return null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator AGooseWalkingOnIronStepsOnIronAStrideApart()
        {
            Assert.IsNotNull(_feet, "every drawn bird's animator gives it feet");
            yield return null;
            yield return null;
            Assert.AreEqual(0, _feet.Steps, "standing, no steps");
            float h = _feet.Height;
            Assert.AreEqual(2f, h, 0.05f, "its drawing's height");
            int mark = Bank.Recent.Count;
            yield return WalkTo(_lx + 3f, 2.5f);
            var steps = Bank.Recent.Skip(mark).Where(id => id.StartsWith("step_")).ToList();
            int expected = Mathf.FloorToInt((6f + FootstepSounds.NpcStride(h) * 0.5f) / FootstepSounds.NpcStride(h));
            Assert.That(_feet.Steps, Is.InRange(expected - 1, expected + 1), "a step a stride over six units: " + string.Join(", ", steps));
            Assert.AreEqual(_feet.Steps, steps.Count);
            Assert.IsTrue(steps.All(id => id.StartsWith("step_iron_")), "on iron: " + string.Join(", ", steps));
            Assert.Greater(steps.Distinct().Count(), 1, "the takes turn");
            Assert.AreEqual(FootstepSounds.NpcPitch(h), Bank.LastPitch, 1e-4f, "pitched for its size");

            int after = _feet.Steps;
            for (int i = 0; i < 20; i++) yield return null;
            Assert.AreEqual(after, _feet.Steps, "it stopped; so did its feet");

            _npc.transform.position = new Vector3(_lx + 3f, 5f, 0f);
            yield return null;
            yield return WalkTo(_lx - 3f, 2.5f);
            Assert.AreEqual(after, _feet.Steps, "walking in the air: no steps");
        }
    }
}
