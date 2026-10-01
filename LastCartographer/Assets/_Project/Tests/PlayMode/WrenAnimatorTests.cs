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
    /// The animator picks the clip from what Wren does (CHR-03): idle, run, the jump and the fall, the landing,
    /// the strike following its own frame data, the pogo, the hurt; and the sheet player windows the strip
    /// through the property block.
    /// </summary>
    public class WrenAnimatorTests
    {
        GameObject _floor, _wren, _dummy;
        WrenController _ctrl;
        QuillStrike _strike;
        Flourishes _flourish;
        WrenVitals _vitals;
        InkSheetPlayer _sheet;
        WrenAnimator _anim;
        ScriptedInput _input;
        MeshRenderer _quad;
        readonly List<Texture2D> _textures = new List<Texture2D>();
        static readonly int StId = Shader.PropertyToID("_BaseMap_ST");

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

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f);
            box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            var abilities = _wren.AddComponent<AbilitySet>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Abilities = abilities;
            _ctrl.Recompute();
            _wren.AddComponent<Inkwell>();
            _vitals = _wren.AddComponent<WrenVitals>();
            _strike = _wren.AddComponent<QuillStrike>();
            _strike.hitMask = LayerMask.GetMask("Hittable", "Enemy");

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(_wren.transform, false);
            quad.transform.localPosition = new Vector3(0f, 1f, 0f);
            quad.transform.localScale = new Vector3(2f, 2f, 1f);
            var r = quad.GetComponent<MeshRenderer>();
            _quad = r;
            r.sharedMaterial = new Material(Shader.Find("OWSBG/InkSprite"));
            _sheet = _wren.AddComponent<InkSheetPlayer>();
            _sheet.Configure(r, new[]
            {
                Clip("idle", 8, 12, true), Clip("run", 8, 12, true), Clip("jump", 4, 12, false), Clip("fall", 4, 12, true),
                Clip("glide", 6, 12, true), Clip("glide_rise", 4, 12, true), Clip("land", 3, 12, false),
                Clip("cling", 4, 12, true), Clip("slide", 3, 12, true), Clip("walljump", 3, 24, false), Clip("dash", 4, 24, false),
                Clip("thread_cast", 2, 24, false), Clip("thread", 4, 24, true), Clip("thread_catch", 3, 24, false),
                Clip("strike1", 6, 24, false), Clip("strike2", 6, 24, false), Clip("strike3", 6, 24, false),
                Clip("strike_up", 6, 24, false), Clip("pogo", 4, 24, false), Clip("bind", 8, 12, true), Clip("survey", 6, 12, true),
                Clip("crosshatch", 12, 24, false), Clip("longstroke", 6, 24, false), Clip("blot", 6, 24, false),
                Clip("hurt", 3, 12, false), Clip("death", 8, 12, false),
            });
            _flourish = _wren.AddComponent<Flourishes>();
            _anim = _wren.AddComponent<WrenAnimator>();
            _ctrl.Teleport(new Vector2(0f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_wren);
            Object.Destroy(_floor);
            if (_dummy != null) Object.Destroy(_dummy);
            foreach (var t in _textures) Object.Destroy(t);
            _textures.Clear();
            GameState.NewGame();
        }

        IEnumerator Steps(int n)
        {
            for (int i = 0; i < n; i++) { _input.Tick(); yield return new WaitForFixedUpdate(); yield return null; }
        }

        /// <summary>Past the landing she makes on the floor she is set down on.</summary>
        IEnumerator Settle()
        {
            float t = 0f;
            while ((_anim.Clip == null || _anim.Clip == "land") && t < 2f) { t += Time.deltaTime; yield return Steps(1); }
        }

        float Offset()
        {
            var mpb = new MaterialPropertyBlock();
            _quad.GetPropertyBlock(mpb);   // her sprite's block, not whatever renderer a flourish's visual may add
            return mpb.GetVector(StId).z;
        }

        [UnityTest]
        public IEnumerator SheStandsRunsJumpsFallsAndLands()
        {
            yield return Settle();
            Assert.AreEqual("idle", _anim.Clip, "at rest");
            _input.Move = Vector2.right;
            yield return Steps(6);
            Assert.AreEqual("run", _anim.Clip, "moving");
            _input.Move = Vector2.zero;
            yield return Steps(8);
            Assert.AreEqual("idle", _anim.Clip, "stopped");

            _input.PressJump(); _input.JumpHeld = true;
            yield return Steps(4);
            Assert.AreEqual("jump", _anim.Clip, "rising");
            _input.JumpHeld = false;
            float t = 0f;
            while (_ctrl.Velocity.y > 0.5f && t < 2f) { t += Time.fixedDeltaTime; yield return Steps(1); }
            yield return Steps(3);
            Assert.AreEqual("fall", _anim.Clip, "falling");
            t = 0f;
            while (!_ctrl.IsGrounded && t < 3f) { t += Time.fixedDeltaTime; yield return Steps(1); }
            yield return Steps(1);
            Assert.AreEqual("land", _anim.Clip, "just landed");
            yield return Steps(24);
            Assert.AreEqual("idle", _anim.Clip, "and settled");
        }

        [UnityTest]
        public IEnumerator TheStrikeFollowsItsOwnFramesAndTheDownStrikeIsThePogo()
        {
            yield return Settle();
            _input.PressAttack();
            yield return Steps(1);
            Assert.AreEqual("strike1", _anim.Clip, "the first swing of the combo");
            int first = _sheet.Frame;
            Assert.LessOrEqual(first, 1, "startup shows the anticipation frame");
            yield return Steps(5);
            Assert.AreEqual("strike1", _anim.Clip);
            Assert.Greater(_sheet.Frame, first, "the active frames follow");
            Assert.AreEqual(_sheet.Frame / 6f, Offset(), 0.001f, "the property block windows the frame");
            yield return Steps(12);
            Assert.AreEqual("idle", _anim.Clip, "the swing ends");

            _input.PressJump(); _input.JumpHeld = true;
            yield return Steps(8);
            _input.JumpHeld = false;
            _input.Move = Vector2.down;
            _input.PressAttack();
            yield return Steps(2);
            Assert.AreEqual("pogo", _anim.Clip, "the down-strike in the air");
        }

        [UnityTest]
        public IEnumerator HurtThenIdleThenDeathHoldsItsLastFrame()
        {
            yield return Settle();
            Assert.IsTrue(_vitals.Damage(1, new Vector2(3f, 0f)), "the hit lands");
            yield return null;
            Assert.AreEqual("hurt", _anim.Clip, "the hit");
            yield return Steps(20);
            Assert.AreNotEqual("hurt", _anim.Clip, "the hit passes");
            float t = 0f;
            while (_vitals.IsInvulnerable && t < 3f) { t += Time.fixedDeltaTime; yield return Steps(1); }   // the hit's mercy frames
            Assert.IsTrue(_vitals.Damage(99), "the killing blow lands");
            yield return null;
            Assert.AreEqual("death", _anim.Clip, "the last mask");
            yield return Steps(50);
            Assert.AreEqual("death", _anim.Clip, "and stays");
            Assert.AreEqual(7, _sheet.Frame, "on the last frame");
            Assert.IsTrue(_sheet.Finished);
        }

        // ---- CHR-04: the abilities' own frames and the flourishes

        AbilitySet Abilities => _wren.GetComponent<AbilitySet>();

        /// <summary>Steps until the condition holds (or the time runs out), recording the clip shown each step.</summary>
        IEnumerator StepUntil(System.Func<bool> done, float seconds, List<string> seen)
        {
            float t = 0f;
            while (!done() && t < seconds) { t += Time.fixedDeltaTime; yield return Steps(1); seen?.Add(_anim.Clip); }
        }

        [UnityTest]
        public IEnumerator TheWingbeatHasItsOwnFramesAndTheTalonholdClingsSlidesAndPushesOff()
        {
            Abilities.Unlock(Ability.Wingbeat);
            Abilities.Unlock(Ability.Talonhold);
            yield return Settle();

            // The Wingbeat, in the air.
            _input.PressJump(); _input.JumpHeld = true;
            yield return Steps(6);
            _input.JumpHeld = false;
            _input.PressDash();
            yield return Steps(1);
            Assert.AreEqual("dash", _anim.Clip, "the Wingbeat");
            Assert.AreEqual(0, _sheet.Frame, "from its first frame");
            yield return StepUntil(() => _ctrl.IsGrounded, 3f, null);
            yield return Steps(24);
            Assert.AreEqual("idle", _anim.Clip);

            // A wall to the right; she jumps into it and holds.
            var wall = new GameObject("Wall") { layer = Layer("Ground") };
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 12f);
            wall.transform.position = new Vector3(_ctrl.Position.x + 2.4f, 5f, 0f);   // ahead of wherever the dash left her
            try
            {
                _input.PressJump(); _input.JumpHeld = true;
                _input.Move = Vector2.right;
                yield return Steps(3);
                _input.JumpHeld = false;
                yield return StepUntil(() => _ctrl.IsClinging, 3f, null);
                Assert.IsTrue(_ctrl.IsClinging, "the Talonhold");
                yield return Steps(1);
                Assert.AreEqual("cling", _anim.Clip, "gripping the wall");
                Assert.IsFalse(_ctrl.IsSliding);
                yield return StepUntil(() => _ctrl.IsSliding, _ctrl.clingSeconds + 1f, null);
                Assert.IsTrue(_ctrl.IsSliding, "the hold spent");
                yield return Steps(1);
                Assert.AreEqual("slide", _anim.Clip, "sliding down it");

                // The push off: the wall's jump, then the rise.
                var seen = new List<string>();
                _input.PressJump();
                yield return Steps(1);
                seen.Add(_anim.Clip);
                _input.Move = Vector2.zero;
                Assert.AreEqual("walljump", _anim.Clip, "the push off the wall");
                Assert.IsFalse(_ctrl.IsClinging);
                yield return StepUntil(() => _anim.Clip != "walljump", 1f, seen);
                Assert.IsTrue(seen.Contains("walljump"));
                Assert.IsTrue(seen[seen.Count - 1] == "jump" || seen[seen.Count - 1] == "fall", "then the air: " + seen[seen.Count - 1]);
            }
            finally { Object.Destroy(wall); }
        }

        [UnityTest]
        public IEnumerator TheThreadIsCastPulledAndCaughtAtTheAnchor()
        {
            Abilities.Unlock(Ability.Inkthread);
            _wren.GetComponent<Inkwell>().Add(9);
            yield return Settle();
            var anchor = new GameObject("Anchor");
            anchor.AddComponent<TetherAnchor>().Permanent = true;
            anchor.transform.position = new Vector3(5f, 3.5f, 0f);
            try
            {
                yield return null;
                var seen = new List<string>();
                _input.PressThread();
                yield return Steps(1);
                seen.Add(_anim.Clip);
                Assert.IsTrue(_ctrl.IsThreading, "the thread caught the anchor ahead of her");
                Assert.AreEqual("thread_cast", _anim.Clip, "flung first");
                yield return StepUntil(() => !_ctrl.IsThreading, 3f, seen);
                Assert.IsFalse(_ctrl.IsThreading);
                yield return StepUntil(() => _ctrl.IsGrounded, 3f, seen);
                int cast = seen.IndexOf("thread_cast"), pull = seen.IndexOf("thread"), caught = seen.IndexOf("thread_catch");
                Assert.GreaterOrEqual(cast, 0, "the cast");
                Assert.Greater(pull, cast, "then the pull: " + string.Join(",", seen));
                Assert.Greater(caught, pull, "then the catch at the anchor: " + string.Join(",", seen));
                Assert.IsTrue(seen.Contains("fall") || seen.Contains("jump"), "and the air after the hop");
            }
            finally { Object.Destroy(anchor); }
        }

        [UnityTest]
        public IEnumerator TheGlideRisesInAnUpdraft()
        {
            Abilities.Unlock(Ability.Windmemory);
            yield return Settle();
            _input.PressJump(); _input.JumpHeld = true;
            yield return Steps(2);
            yield return StepUntil(() => _ctrl.Velocity.y <= 0f && !_ctrl.IsGrounded, 2f, null);
            yield return Steps(2);
            Assert.IsTrue(_ctrl.IsGliding, "holding jump on the way down");
            Assert.AreEqual("glide", _anim.Clip, "the Windmemory");
            for (int i = 0; i < 4; i++) { _ctrl.Lift(9f); yield return Steps(1); }
            Assert.IsTrue(_ctrl.IsLifted);
            Assert.AreEqual("glide_rise", _anim.Clip, "carried up");
            yield return Steps(6);
            Assert.IsFalse(_ctrl.IsLifted, "the updraft let go");
            Assert.AreNotEqual("glide_rise", _anim.Clip);
            _input.JumpHeld = false;
            yield return StepUntil(() => _ctrl.IsGrounded, 4f, null);
            Assert.IsTrue(_ctrl.IsGrounded);
        }

        [UnityTest]
        public IEnumerator AFlourishPlaysItsOwnClipAlongItsFrames()
        {
            var ink = _wren.GetComponent<Inkwell>();
            yield return Settle();
            ink.Add(9);
            _input.PressFlourish();
            yield return Steps(1);
            Assert.AreEqual(FlourishKind.Crosshatch, _flourish.Current, "the default");
            Assert.AreEqual("crosshatch", _anim.Clip, "its own clip");
            int first = _sheet.Frame;
            Assert.LessOrEqual(first, 1);
            yield return Steps(10);
            Assert.AreEqual("crosshatch", _anim.Clip);
            Assert.Greater(_sheet.Frame, first, "the frames follow the flourish's");
            Assert.AreEqual(_sheet.Frame / 12f, Offset(), 0.001f, "the property block windows the frame");
            yield return StepUntil(() => !_flourish.IsBusy, 2f, null);
            yield return Steps(2);
            Assert.AreEqual("idle", _anim.Clip, "the flourish ends");

            ink.Add(9);
            _input.Move = Vector2.up;
            _input.PressFlourish();
            yield return Steps(1);
            Assert.AreEqual("blot", _anim.Clip, "up is the Blot");
            _input.Move = Vector2.zero;
            yield return StepUntil(() => !_flourish.IsBusy, 2f, null);
            yield return Steps(2);

            ink.Add(9);
            _input.Move = Vector2.right;
            _input.PressFlourish();
            yield return Steps(1);
            Assert.AreEqual("longstroke", _anim.Clip, "forward is the Longstroke");
            _input.Move = Vector2.zero;
        }
    }
}
