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
    /// The enemies' voices in play (AUD-10, docs/design/enemy-sounds.md): every enemy wakes with one; a crab's hop tells
    /// and then flicks, its walk ticks; a hit is the material's hurt, a death its own under the kill layer, a strike a
    /// shell turns away clacks; the wasp hums while it lives and not after; a Cantor's ring is a window and its toll a
    /// bell; too many voices at once keep the tells and drop from the bottom; a sound stands where its enemy is.
    /// </summary>
    public class EnemyVoiceTests
    {
        GameObject? _floor, _wren, _enemy, _other;
        WrenController? _ctrl;
        ScriptedInput? _input;
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
            Assert.IsNotNull(InkSoundBank.Instance, "booted with the first scene");
            Bank.Hush();
            _lx = Bank.ListenerX;   // the room is built under the listener, so every sound here is on screen
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(_lx, -0.5f, 0f);
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
            _ctrl.Teleport(new Vector2(_lx, 0f));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _other, _enemy, _wren, _floor }) if (go != null) Object.Destroy(go);
            if (InkSoundBank.Instance != null) InkSoundBank.Instance.Hush();
            Pause.End();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        T Make<T>(Vector2 pos, bool other = false) where T : Enemy
        {
            var go = new GameObject(typeof(T).Name) { layer = Layer("Enemy") };
            go.transform.position = pos;
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
            go.AddComponent<Rigidbody2D>();
            if (other) _other = go; else _enemy = go;
            return go.AddComponent<T>();
        }

        [UnityTest]
        public IEnumerator ACrabWakesWithAVoiceTicksAsItWalksAndTellsBeforeItHops()
        {
            var crab = Make<MarshCrab>(new Vector2(_lx + 2f, 0.5f));
            var voice = crab.GetComponent<EnemyVoice>();
            Assert.IsNotNull(voice, "every enemy wakes with a voice");
            Assert.AreEqual("MarshCrab", voice.Voice.Family);
            Assert.AreEqual(EnemySounds.Material.Shell, voice.Voice.Material);
            yield return Until(() => voice.Looping == "crab_scuttle", 1.5f);
            Assert.AreEqual("crab_scuttle", voice.Looping, "claws ticking as it walks");
            Assert.Greater(voice.LoopVolume, 0.05f, "heard, under the Sfx bus");
            Assert.IsNull(voice.LastMove, "stood in the room, it has made no move yet");
            yield return Until(() => Bank.Recent.Contains("tell_strike"), 4f);
            yield return Until(() => Bank.Recent.Contains("crab_hop"), 1f);
            var recent = Bank.Recent.ToList();
            Assert.That(recent, Does.Contain("tell_strike"), "the hop is its telegraph: a strike's tell");
            Assert.That(recent, Does.Contain("crab_hop"), "and the flick off the paper follows");
            Assert.Less(recent.IndexOf("tell_strike"), recent.IndexOf("crab_hop"), "the tell comes first");
            Assert.AreEqual(AudioDirection.Tell.Strike, Bank.LastTell);
            Assert.AreEqual("crab_hop", voice.LastMove);
        }

        [UnityTest]
        public IEnumerator AHitIsTheMaterialsHurtADeathItsOwnUnderTheKillLayerAndAShellTurnsTheQuillAway()
        {
            var smudge = Make<Smudge>(new Vector2(_lx + 2f, 2f));
            smudge.ForceDrawn(true);
            var crab = Make<MarshCrab>(new Vector2(_lx - 2f, 0.5f), other: true);
            yield return Frames(3);
            int mark = Bank.Recent.Count;
            Assert.IsTrue(smudge.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }));
            Assert.AreEqual("ink_hurt", Bank.Recent.Last(), "a smudge struck is wet ink");
            Assert.Greater(Bank.LastGain, 0.5f, "two units off: on screen, heard whole");
            mark = Bank.Recent.Count;
            while (!smudge.IsDead) smudge.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right });
            var since = Bank.Recent.Skip(mark).ToList();
            Assert.That(since, Does.Contain("ink_death"), "its own death");
            Assert.That(since, Does.Contain("kill"), "under the kill layer");
            Assert.Less(since.IndexOf("ink_death"), since.IndexOf("kill"), "the drawing goes, then the ink drops");

            mark = Bank.Recent.Count;
            Assert.IsFalse(crab.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right }), "a side strike bounces off the shell");
            Assert.AreEqual("shell_block", Bank.Recent.Last(), "a dry clack");
            Assert.Less(Bank.LastPan, 0f, "from the left, where it is");
            Assert.IsTrue(crab.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.down }), "the pogo lands");
            Assert.AreEqual("shell_hurt", Bank.Recent.Last(), "and the shell ticks");
            Assert.AreEqual(2, Bank.Recent.Skip(mark).Count(), "nothing else said anything");
        }

        [UnityTest]
        public IEnumerator TheWaspHumsWhileItLivesAndNotAfter()
        {
            var wasp = Make<Pulpwasp>(new Vector2(_lx + 3f, 2f));
            var voice = wasp.GetComponent<EnemyVoice>();
            yield return Until(() => voice.Looping == "wasp_buzz", 1f);
            Assert.AreEqual("wasp_buzz", voice.Looping, "the hum");
            Assert.That(voice.LoopVolume, Is.InRange(0.05f, 0.5f), "under the room, at the cue's gain and the bus's");
            Assert.AreEqual(0, Bank.Recent.Count(id => id == "wasp_buzz"), "a loop is the voice's own, not a one-shot of the bank's");
            while (!wasp.IsDead) wasp.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.right });
            yield return null;
            Assert.IsNull(voice.Looping, "dead, it is quiet");
            Assert.That(Bank.Recent, Does.Contain("pulp_death"), "the sac bursting");
        }

        [UnityTest]
        public IEnumerator ACantorsRingIsAWindowAndItsTollABell()
        {
            var cantor = Make<Cantor>(new Vector2(_lx + 2f, 3f));
            cantor.PlaceId = "Sound_Test/Nowhere";
            yield return Until(() => Bank.LastTell.HasValue, 2f);
            Assert.AreEqual(AudioDirection.Tell.Window, Bank.LastTell, "the ring is the opening to strike into: a chime");
            Assert.IsTrue(cantor.IsRinging);
            yield return Until(() => Bank.Recent.Contains("cantor_toll"), 2f);
            Assert.That(Bank.Recent, Does.Contain("cantor_toll"), "the toll, as the ring ends");
            Assert.AreEqual(1, cantor.Tolls);
            Assert.Less(Bank.Recent.ToList().IndexOf("tell_window"), Bank.Recent.ToList().IndexOf("cantor_toll"));
        }

        [UnityTest]
        public IEnumerator TooManyVoicesKeepTheTellsAndDropFromTheBottom()
        {
            yield return null;
            Bank.Hush();
            for (int i = 0; i < 30; i++) InkSoundBank.Play("stroke");
            Assert.LessOrEqual(Bank.Active, AudioDirection.VoiceLimit, "never more than the limit at once");
            Assert.AreEqual(0, Bank.Dropped, "a stroke among strokes takes the oldest's place: nothing new was refused");
            Assert.IsNotNull(InkSoundBank.Play(InkSounds.TellCue(AudioDirection.Tell.Strike)), "a tell is always heard");
            Assert.AreEqual(InkSounds.TellCue(AudioDirection.Tell.Strike), Bank.Last);
            Assert.LessOrEqual(Bank.Active, AudioDirection.VoiceLimit);

            Bank.Hush();
            for (int i = 0; i < AudioDirection.VoiceLimit; i++) InkSoundBank.Play(InkSounds.TellCue(AudioDirection.Tell.Strike));
            Assert.AreEqual(AudioDirection.VoiceLimit, Bank.Active, "full of tells");
            int played = Bank.Played;
            Assert.IsNull(InkSoundBank.Play("crab_hop"), "a hop has no place among tells: dropped");
            Assert.IsNull(InkSoundBank.Play("ink_hurt"), "nor an enemy's hurt");
            Assert.IsNull(InkSoundBank.Play("hurt"), "nor even hers: the tells are kept first");
            Assert.AreEqual(3, Bank.Dropped);
            Assert.AreEqual(played, Bank.Played, "nothing dropped is counted as played");
            Assert.IsNotNull(InkSoundBank.Play(InkSounds.TellCue(AudioDirection.Tell.Slam)), "another tell takes the oldest tell's place");
            Assert.AreEqual(AudioDirection.VoiceLimit, Bank.Active);
            Assert.AreEqual(InkSounds.TellCue(AudioDirection.Tell.Slam), Bank.Last);
        }

        [UnityTest]
        public IEnumerator ASoundStandsWhereItsEnemyIs()
        {
            yield return null;
            float hw = Bank.HalfWidth;
            Assert.Greater(hw, 1f);
            Assert.IsNotNull(InkSoundBank.Play("crab_hop", 1f, new Vector2(_lx, 0f)));
            Assert.AreEqual(0f, Bank.LastPan, 0.01f, "under the listener: centred");
            Assert.AreEqual(1f, Bank.LastGain, 0.001f, "and whole");
            Assert.IsNotNull(InkSoundBank.Play("crab_hop", 1f, new Vector2(_lx + 2f * hw, 0f)));
            Assert.Greater(Bank.LastPan, 0.3f, "a screen to the right: panned right");
            Assert.AreEqual(0.5f, Bank.LastGain, 0.01f, "and half as loud");
            int played = Bank.Played;
            Assert.IsNull(InkSoundBank.Play("crab_hop", 1f, new Vector2(_lx - 4f * hw, 0f)), "three screens off: not heard");
            Assert.AreEqual(played, Bank.Played, "and not counted");
            Assert.AreEqual(0, Bank.Dropped, "nor dropped: it was simply too far");
            Assert.IsNotNull(InkSoundBank.Play("crab_hop"), "a sound from nowhere in particular is centred");
            Assert.AreEqual(0f, Bank.LastPan, 0.001f);
        }
    }
}
