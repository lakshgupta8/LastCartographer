#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// Wren's sounds in play (AUD-03): the bank wakes with the game; a swing is a stroke, a landed strike adds the hit
    /// layer and a death the kill layer; a jump, a landing and a dash are paper; the survey hatches while held and
    /// ends drawn; hurt is a smudge, a Bind a word, the last mask the long smudge; every boss telegraph plays the tell
    /// of its kind on its first frame; and the volume is the Sfx bus's.
    /// </summary>
    public class WrenSoundsTests
    {
        sealed class Target : Enemy { protected override void Tick(float dt) { } protected override bool ContactHurts => false; }

        GameObject? _floor, _wren, _enemy, _vantage, _boss;
        WrenController? _ctrl;
        WrenVitals? _vitals;
        Inkwell? _ink;
        ScriptedInput? _input;
        InkSoundBank Bank => InkSoundBank.Instance!;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            Pause.End();
            GameState.NewGame();
            Assert.IsNotNull(InkSoundBank.Instance, "booted with the first scene");
            Bank.Hush();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>().Set(Ability.Wingbeat | Ability.Talonhold);
            _ink = _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _input = new ScriptedInput();
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _vitals = _wren.AddComponent<WrenVitals>();
            var strike = _wren.AddComponent<QuillStrike>();
            strike.hitMask = LayerMask.GetMask("Enemy");
            var flourishes = _wren.AddComponent<Flourishes>();
            flourishes.hitMask = LayerMask.GetMask("Enemy");
            _wren.AddComponent<WrenSounds>();
            _ctrl.Teleport(new Vector2(0f, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _boss, _vantage, _enemy, _wren, _floor }) if (go != null) Object.Destroy(go);
            if (InkSoundBank.Instance != null) InkSoundBank.Instance.Hush();
            Pause.End();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        IEnumerator Until(System.Func<bool> done, float seconds = 2f)
        {
            float t = 0f;
            while (!done() && t < seconds) { t += Time.deltaTime; yield return null; }
        }

        [UnityTest]
        public IEnumerator ASwingIsAStrokeAHitAddsItsLayerAndAKillItsOwn()
        {
            yield return Frames(3);
            int before = Bank.Played;
            _input!.PressAttack();
            yield return Until(() => Bank.Played > before);
            Assert.AreEqual("stroke", Bank.Last, "the swing");
            Assert.AreEqual(before + 1, Bank.Played, "a swing in the air is one stroke: no hit layer");
            yield return Frames(30);

            _enemy = new GameObject("Target") { layer = Layer("Enemy") };
            _enemy.transform.position = new Vector3(1.5f, 0.5f, 0f);   // in the quill's reach, not underfoot
            _enemy.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
            _enemy.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var target = _enemy.AddComponent<Target>();
            yield return Frames(2);
            int hp = target.Health;
            before = Bank.Played;
            _input.PressAttack();
            yield return Until(() => Bank.Played >= before + 2);
            Assert.AreEqual("hit", Bank.Last, "the strike landed: the hit layer over the stroke");
            Assert.AreEqual(before + 2, Bank.Played);
            Assert.Less(target.Health, hp);

            before = Bank.Played;
            for (int i = 0; i < 10 && !target.IsDead; i++) target.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right });
            Assert.IsTrue(target.IsDead);
            Assert.AreEqual("kill", Bank.Last, "the kill layer");
            Assert.AreEqual(before + 1, Bank.Played, "a hit from nowhere is not her stroke: only the death sounds");
        }

        [UnityTest]
        public IEnumerator JumpsLandingsAndDashesArePaper()
        {
            yield return Frames(3);
            int before = Bank.Played;
            _input!.PressJump(); _input.JumpHeld = true;
            yield return Until(() => Bank.Played > before);
            Assert.AreEqual("jump", Bank.Last);
            _input.JumpHeld = false;
            yield return Until(() => Bank.Last == "land", 3f);
            Assert.AreEqual("land", Bank.Last, "and set down");
            before = Bank.Played;
            _input.Move = Vector2.right;
            _input.PressDash();
            yield return Until(() => Bank.Played > before);
            Assert.AreEqual("dash", Bank.Last, "the page turned fast");
            Assert.Less(InkSounds.Of("jump")!.Gain, InkSounds.Of("dash")!.Gain);
        }

        [UnityTest]
        public IEnumerator TheSurveyHatchesWhileHeldAndEndsDrawn()
        {
            _vantage = new GameObject("Vantage");
            _vantage.transform.position = new Vector3(0f, 0.5f, 0f);
            _vantage.AddComponent<BoxCollider2D>().isTrigger = true;
            _vantage.GetComponent<BoxCollider2D>().size = new Vector2(2f, 2f);
            var v = _vantage.AddComponent<VantagePoint>();
            v.VantageId = "Sound_Test/Hatch";
            yield return Frames(3);
            Assert.IsNull(Bank.Looping);
            _input!.SurveyHeld = true;
            yield return Until(() => Bank.Looping != null, 1f);
            Assert.AreEqual("survey", Bank.Looping, "hatching while she holds it");
            Assert.IsTrue(_wren!.GetComponent<WrenSounds>().IsSurveyLooping);
            yield return Until(() => v.IsSurveyed, 3f);
            Assert.IsTrue(v.IsSurveyed, "drawn");
            yield return null;
            Assert.AreEqual("drawn", Bank.Last, "one long stroke and a tap");
            _input.SurveyHeld = false;
            yield return Until(() => Bank.Looping == null, 1f);
            Assert.IsNull(Bank.Looping, "the hatching stops when the pen lifts");
        }

        [UnityTest]
        public IEnumerator HurtIsASmudgeABindIsAWordAndTheLastMaskTheLongOne()
        {
            yield return Frames(3);
            int before = Bank.Played;
            Assert.IsTrue(_vitals!.Damage(1, new Vector2(2f, 0f)));
            Assert.AreEqual("hurt", Bank.Last, "a smudge, not a cry");
            Assert.AreEqual(before + 1, Bank.Played);
            _ink!.Add(_ink.MaxPips);
            _input!.BindHeld = true;
            yield return Until(() => Bank.Last == "bind", 2f);
            Assert.AreEqual("bind", Bank.Last, "a word being written");
            _input.BindHeld = false;
            yield return Frames(70);   // past the hurt's invulnerability
            while (_vitals.Masks > 0) { _vitals.Damage(_vitals.Masks); yield return Frames(70); }
            Assert.AreEqual("died", Bank.Last, "the smudge drawn out to paper");
        }

        [UnityTest]
        public IEnumerator EveryTelegraphIsItsTellAndTheVolumeIsTheBuss()
        {
            _boss = new GameObject("Halvard") { layer = Layer("Enemy") };
            _boss.transform.position = new Vector3(5f, 0.9f, 0f);
            _boss.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.8f);
            _boss.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var halvard = _boss.AddComponent<Halvard>();
            yield return Frames(2);
            try
            {
                halvard.BeginFight();
                Bank.Hush();
                halvard.ForceAttack(Halvard.Attack.Survey);
                yield return Until(() => Bank.LastTell.HasValue, 1f);
                Assert.AreEqual(AudioDirection.Tell.Shape, Bank.LastTell, "the survey is a shape: a swell");
                Assert.AreEqual(InkSounds.TellCue(AudioDirection.Tell.Shape), Bank.Last);
                Bank.Hush();
                halvard.ForceAttack(Halvard.Attack.Thrust);
                yield return Until(() => Bank.LastTell.HasValue, 1f);
                Assert.AreEqual(AudioDirection.Tell.Strike, Bank.LastTell, "a thrust is a strike: a scratch");

                Assert.IsNotNull(Mix.Live, "the mix is running");
                Assert.AreEqual(Mix.Live!.Gain(Mix.Bus.Sfx), Bank.Volume, 0.01f, "the Sfx bus's gain is the sounds'");
                Assert.GreaterOrEqual(Bank.Volume, AudioDirection.TellBusFloor, "a fight never lowers the tells");
            }
            finally { Object.Destroy(_boss); _boss = null; }
            yield return null;
            Pause.Begin();
            try
            {
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.Less(Bank.Volume, 0.05f, "paused: nothing in the room plays");
            }
            finally { Pause.End(); }
        }
    }
}
