using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>Scripted input for tests: presses are buffered exactly like the real reader.</summary>
    sealed class ScriptedInput : IWrenInput
    {
        public Vector2 Move { get; set; }
        public bool JumpHeld { get; set; }
        public bool BindHeld { get; set; }
        public bool SurveyHeld { get; set; }
        readonly ButtonBuffer _jump = new ButtonBuffer(6), _dash = new ButtonBuffer(4), _attack = new ButtonBuffer(6), _flourish = new ButtonBuffer(6),
            _instrument = new ButtonBuffer(6), _cycle = new ButtonBuffer(6);
        public void PressJump() => _jump.Press();
        public void PressDash() => _dash.Press();
        public void PressAttack() => _attack.Press();
        public void PressFlourish() => _flourish.Press();
        public void PressInstrument() => _instrument.Press();
        public void PressCycleInstrument() => _cycle.Press();
        public bool ConsumeJump() => _jump.Consume();
        public bool ConsumeDash() => _dash.Consume();
        public bool ConsumeAttack() => _attack.Consume();
        public bool ConsumeFlourish() => _flourish.Consume();
        public bool ConsumeInstrument() => _instrument.Consume();
        public bool ConsumeCycleInstrument() => _cycle.Consume();
        public void Tick() { _jump.Tick(); _dash.Tick(); _attack.Tick(); _flourish.Tick(); _instrument.Tick(); _cycle.Tick(); }
    }

    /// <summary>
    /// Verifies the combat doc's frame data on the real controller: heights, coyote time, dash
    /// distance, and the down-strike pogo. Runs headless in batch mode (-runTests -testPlatform PlayMode).
    /// </summary>
    public class WrenControllerTests
    {
        GameObject _floor, _wren;
        WrenController _ctrl;
        ScriptedInput _input;
        AbilitySet _abilities;

        static int Layer(string name)
        {
            int l = LayerMask.NameToLayer(name);
            Assert.GreaterOrEqual(l, 0, "Layer missing: " + name + " (run ProjectSetup.Configure)");
            return l;
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            var fc = _floor.AddComponent<BoxCollider2D>();
            fc.size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f);
            box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _abilities = _wren.AddComponent<AbilitySet>();
            _input = new ScriptedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Abilities = _abilities;
            _ctrl.Recompute();
            _ctrl.Teleport(new Vector2(0f, 0.0f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_wren);
            Object.Destroy(_floor);
            Time.timeScale = 1f;
        }

        IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate();
        }

        IEnumerator SettleOnFloor()
        {
            _ctrl.Teleport(new Vector2(0f, 1.5f));
            yield return Frames(90);
            Assert.IsTrue(_ctrl.IsGrounded, "should be grounded after settling");
            Assert.AreEqual(0f, _ctrl.Position.y, 0.06f, "should rest on the floor");
        }

        [UnityTest]
        public IEnumerator FallsAndLandsOnFloor()
        {
            yield return SettleOnFloor();
        }

        [UnityTest]
        public IEnumerator HeldJumpReachesMaxHeight()
        {
            yield return SettleOnFloor();
            _input.JumpHeld = true;
            _input.PressJump();
            float peak = 0f;
            for (int i = 0; i < 70; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, _ctrl.Position.y);
            }
            Assert.AreEqual(_ctrl.maxJumpHeight, peak, 0.35f, "held jump apex");
        }

        [UnityTest]
        public IEnumerator TappedJumpIsShorterButAtLeastMin()
        {
            yield return SettleOnFloor();
            _input.JumpHeld = false;
            _input.PressJump();
            float peak = 0f;
            for (int i = 0; i < 70; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, _ctrl.Position.y);
            }
            Assert.GreaterOrEqual(peak, _ctrl.minJumpHeight - 0.15f, "tap must reach the minimum height");
            Assert.Less(peak, _ctrl.maxJumpHeight - 1.0f, "tap must be clearly lower than a held jump");
        }

        [UnityTest]
        public IEnumerator CoyoteTimeAllowsJumpAfterLeavingLedge()
        {
            // Narrow floor: walk off the right edge, then press jump 3 frames later.
            _floor.GetComponent<BoxCollider2D>().size = new Vector2(4f, 1f);
            yield return SettleOnFloor();
            _input.Move = Vector2.right;
            // Run until airborne.
            int guard = 0;
            while (_ctrl.IsGrounded && guard++ < 120) yield return new WaitForFixedUpdate();
            Assert.IsFalse(_ctrl.IsGrounded, "should have run off the ledge");
            yield return Frames(3);
            _input.JumpHeld = true;
            _input.PressJump();
            yield return Frames(2);
            Assert.Greater(_ctrl.Velocity.y, 5f, "coyote jump should launch upward");
        }

        [UnityTest]
        public IEnumerator WingbeatDashCoversFiveUnitsAndIsGated()
        {
            yield return SettleOnFloor();
            _input.Move = Vector2.right;
            yield return Frames(2);
            _input.Move = Vector2.zero;
            yield return Frames(8);               // decelerate to a stop
            float x0 = _ctrl.Position.x;

            _input.PressDash();                    // no ability yet: nothing happens
            yield return Frames(_ctrl.dashFrames + 2);
            Assert.AreEqual(x0, _ctrl.Position.x, 0.05f, "dash must be gated by Wingbeat");

            _abilities.Unlock(Ability.Wingbeat);
            _input.PressDash();
            yield return Frames(_ctrl.dashFrames + 1);
            float moved = _ctrl.Position.x - x0;
            Assert.AreEqual(_ctrl.dashDistance, moved, 0.4f, "dash distance");
        }

        [UnityTest]
        public IEnumerator DownStrikeOnDummyPogos()
        {
            var ink = _wren.AddComponent<Inkwell>();
            var strike = _wren.AddComponent<QuillStrike>();
            strike.hitMask = LayerMask.GetMask("Hittable");

            var dummy = new GameObject("Dummy") { layer = Layer("Hittable") };
            var dc = dummy.AddComponent<BoxCollider2D>();
            dc.size = Vector2.one;
            dummy.transform.position = new Vector3(0f, 1.5f, 0f);
            var td = dummy.AddComponent<TrainingDummy>();

            _ctrl.Teleport(new Vector2(0f, 3.2f));
            _input.Move = Vector2.down;
            yield return Frames(1);
            _input.PressAttack();
            // Startup 3 + active frames; hitstop pauses fixed updates briefly, so use a generous window.
            float maxVy = float.MinValue;
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForFixedUpdate();
                maxVy = Mathf.Max(maxVy, _ctrl.Velocity.y);
            }
            Assert.AreEqual(1, td.Hits, "dummy should be hit once per swing");
            Assert.Greater(maxVy, 10f, "down-strike should pogo upward");
            Assert.AreEqual(1, ink.Pips, "a landed hit fills one pip");
            Object.Destroy(dummy);
        }
    }
}
