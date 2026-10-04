#nullable enable
using System.Collections;
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
    /// Wren on a wall and her feet in a fading place, in play (AUD-17, docs/design/footsteps.md §4): her talons catch as
    /// she clings, scrape while the spent hold slides her down, and scuff as she pushes off; in a place faded to the
    /// third stage she runs on the page, whatever the ground is drawn as.
    /// </summary>
    public class WallSoundsPlayTests
    {
        GameObject? _floor, _wall, _wren;
        WrenController? _ctrl;
        ScriptedInput? _input;
        Footsteps? _feet;
        float _lx;
        InkSoundBank Bank => InkSoundBank.Instance!;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        IEnumerator Until(System.Func<bool> done, float seconds, string what)
        {
            float t = 0f;
            while (!done() && t < seconds) { t += Time.fixedDeltaTime; yield return new WaitForFixedUpdate(); }
            Assert.IsTrue(done(), "timed out waiting for " + what);
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
            foreach (var go in new[] { _wren, _wall, _floor }) if (go != null) Object.Destroy(go);
            if (MixDriver.Instance != null) MixDriver.Instance.RoomOverride = null;
            if (InkSoundBank.Instance != null) InkSoundBank.Instance.Hush();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        void Build(string tile)
        {
            _floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(_floor.GetComponent<BoxCollider>());
            var r = _floor.GetComponent<MeshRenderer>();
            r.sharedMaterial = new Material(r.sharedMaterial) { name = "M_" + tile };
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
            _ctrl.Teleport(new Vector2(_lx - 3f, 0f));
        }

        [UnityTest]
        public IEnumerator HerTalonsCatchScrapeAndPushOff()
        {
            Build("Ground_Stone");
            _wren!.GetComponent<AbilitySet>().Set(Ability.Talonhold);
            _ctrl!.Recompute();
            yield return Frames(10);
            _wall = new GameObject("Wall") { layer = Layer("Ground") };
            _wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 12f);
            _wall.transform.position = new Vector3(_ctrl.Position.x + 2.4f, 5f, 0f);
            yield return Frames(2);
            Bank.Hush();
            _input!.PressJump(); _input.JumpHeld = true;
            _input.Move = Vector2.right;
            yield return Frames(3);
            _input.JumpHeld = false;
            yield return Until(() => _ctrl.IsClinging, 3f, "the Talonhold");
            yield return Frames(1);
            Assert.That(Bank.Recent, Does.Contain(FootstepSounds.ClingCue), "her talons catch the wall");
            Assert.AreEqual(FootstepSounds.ClingCue, _feet!.LastWall);
            Assert.IsNull(_feet.Sliding, "holding, not sliding");
            Assert.AreEqual(1, Bank.Recent.Count(c => c == FootstepSounds.ClingCue), "one catch for one cling");
            yield return Until(() => _ctrl.IsSliding, _ctrl.clingSeconds + 1f, "the hold to be spent");
            yield return Frames(2);
            Assert.AreEqual(FootstepSounds.SlideCue, _feet.Sliding, "the scrape down the wall");
            _input.PressJump();
            yield return Frames(2);
            _input.Move = Vector2.zero;
            Assert.That(Bank.Recent, Does.Contain(FootstepSounds.KickCue), "the push off");
            Assert.AreEqual(FootstepSounds.KickCue, _feet.LastWall);
            Assert.IsNull(_feet.Sliding, "off the wall, the scrape stops");
        }

        [UnityTest]
        public IEnumerator InAPlaceFadedToTheThirdStageSheRunsOnThePage()
        {
            Build("Ground_Iron");
            Assert.IsTrue(FadeStages.Advance(GameState.World, "Sound_Fade", FootstepSounds.PaperStage), "the place faded");
            MixDriver.Instance!.RoomOverride = "Greybox_Sound_Fade";
            yield return Frames(10);
            Assert.AreEqual(FootstepSounds.PaperStage, Footsteps.Stage, "the mix knows how faded the room is");
            Assert.AreEqual(FootstepSounds.Surface.Iron, _feet!.SurfaceUnder(), "the block is still drawn as iron");
            Bank.Hush();
            _input!.Move = Vector2.right;
            yield return new WaitForSeconds(1.2f);
            _input.Move = Vector2.zero;
            var steps = Bank.Recent.Where(id => id.StartsWith("step_")).ToList();
            Assert.That(steps, Is.Not.Empty, "she ran");
            Assert.IsTrue(steps.All(id => id.StartsWith("step_paper_")), "the iron's drawing gone, the page underfoot: " + string.Join(", ", steps));
            Assert.AreEqual(FootstepSounds.Surface.Paper, _feet.LastSurface);
        }
    }
}
