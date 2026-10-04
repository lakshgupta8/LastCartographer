#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Wren's footsteps in play (AUD-12, docs/design/footsteps.md): she wakes with feet; running on a block that wears
    /// the iron tile steps on iron, a stride apart, the takes turning; standing still or dashing makes none; a bare
    /// block somewhere with no region is paper; a jump lands on the ground's own landing under the pen set down.
    /// </summary>
    public class FootstepsTests
    {
        GameObject? _floor, _wren;
        WrenController? _ctrl;
        ScriptedInput? _input;
        Footsteps? _feet;
        float _lx;
        InkSoundBank Bank => InkSoundBank.Instance!;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        IEnumerator Until(System.Func<bool> done, float seconds = 2f)
        {
            float t = 0f;
            while (!done() && t < seconds) { t += Time.deltaTime; yield return null; }
        }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            Pause.End();
            GameState.NewGame();
            Assert.IsNotNull(InkSoundBank.Instance);
            Bank.Hush();
            _lx = Bank.ListenerX;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _wren, _floor }) if (go != null) Object.Destroy(go);
            if (InkSoundBank.Instance != null) InkSoundBank.Instance.Hush();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        /// <summary>A floor 60 units wide; with a tile, it wears that tile's material as the builder names it.</summary>
        void Build(string? tile)
        {
            if (tile != null)
            {
                _floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(_floor.GetComponent<BoxCollider>());
                var r = _floor.GetComponent<MeshRenderer>();
                r.sharedMaterial = new Material(r.sharedMaterial) { name = "M_" + tile };
            }
            else _floor = new GameObject("Floor");
            _floor.name = "Floor";
            _floor.layer = Layer("Ground");
            _floor.transform.position = new Vector3(_lx, -0.5f, 0f);
            _floor.transform.localScale = new Vector3(60f, 1f, 1f);
            _floor.AddComponent<BoxCollider2D>().size = Vector2.one;

            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _input = new ScriptedInput();
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _wren.AddComponent<WrenVitals>();
            _wren.AddComponent<WrenSounds>();
            _feet = _wren.GetComponent<Footsteps>();
            _ctrl.Teleport(new Vector2(_lx - 12f, 0f));
        }

        [UnityTest]
        public IEnumerator RunningOnIronStepsOnIronAStrideApartTheTakesTurning()
        {
            Build("Ground_Iron");
            Assert.IsNotNull(_feet, "she wakes with feet");
            yield return Frames(10);
            Assert.AreEqual(0, _feet!.Steps, "standing, she makes no steps");
            Assert.AreEqual(FootstepSounds.Surface.Iron, _feet.SurfaceUnder(), "the block wears the iron tile");

            float x0 = _ctrl!.Position.x;
            _input!.Move = Vector2.right;
            yield return new WaitForSeconds(1.5f);
            _input.Move = Vector2.zero;
            float run = _ctrl.Position.x - x0;
            var steps = Bank.Recent.Where(id => id.StartsWith("step_")).ToList();
            Assert.Greater(run, 8f, "she ran");
            Assert.That(steps, Is.Not.Empty);
            Assert.IsTrue(steps.All(id => id.StartsWith("step_iron_")), "every step on iron: " + string.Join(", ", steps));
            int expected = Mathf.FloorToInt(run / _feet.Stride + 0.5f);
            Assert.That(_feet.Steps, Is.InRange(expected - 1, expected + 1), "a step a stride (" + _feet.Stride + " units over " + run + ")");
            for (int i = 1; i < steps.Count; i++) Assert.AreNotEqual(steps[i - 1], steps[i], "no two takes in a row alike");
            Assert.AreEqual(FootstepSounds.Surface.Iron, _feet.LastSurface);

            yield return Frames(20);
            int n = _feet.Steps;
            yield return Frames(30);
            Assert.AreEqual(n, _feet.Steps, "stopped, the steps stop");
        }

        [UnityTest]
        public IEnumerator ABareBlockNowhereIsPaperAndAJumpLandsOnTheGround()
        {
            Build(null);
            yield return Frames(10);
            Assert.AreEqual(FootstepSounds.Surface.Paper, _feet!.SurfaceUnder(), "no tile and no region: paper");

            Bank.Hush();
            _input!.PressJump(); _input.JumpHeld = true;
            yield return Frames(12);
            _input.JumpHeld = false;
            yield return Until(() => _ctrl!.IsGrounded && Bank.Recent.Contains("landing_paper"), 3f);
            var recent = Bank.Recent.ToList();
            Assert.That(recent, Does.Contain("landing_paper"), "the ground's own landing");
            Assert.That(recent, Does.Contain("land"), "under the pen set down");
            Assert.AreEqual(0, _feet.Steps, "a jump in place is no step");
        }

        [UnityTest]
        public IEnumerator ADashMakesNoSteps()
        {
            Build("Ground_Boardwalk");
            _wren!.GetComponent<AbilitySet>().Set(Ability.Wingbeat);
            _ctrl!.Recompute();
            yield return Frames(10);
            _input!.Move = Vector2.right;
            _input.PressDash();
            yield return Until(() => _ctrl!.IsDashing, 0.5f);
            Assert.IsTrue(_ctrl!.IsDashing, "dashing");
            int during = _feet!.Steps;
            yield return Until(() => !_ctrl.IsDashing, 1f);
            Assert.AreEqual(during, _feet.Steps, "a dash is a page turned, not footfalls");
            yield return new WaitForSeconds(0.8f);
            Assert.Greater(_feet.Steps, during, "running on, the steps come back");
            Assert.AreEqual(FootstepSounds.Surface.Wood, _feet.LastSurface, "on the boardwalk: boards");
        }
    }
}
