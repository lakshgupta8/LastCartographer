#nullable enable
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The world's sounds and the pages' in play (AUD-11, docs/design/world-sounds.md): a lamp lit while she is there
    /// whumps and one already lit does not; a seed drops pitched by its worth; a decision, a fade, an erasure and a
    /// commission's steps each sound once; a shut way knocks where it is; a pellet lands; an ability is a flourish; the
    /// options page opens, moves and ticks a slider at its level; and paused, the pages are heard and the room is not.
    /// </summary>
    public class WorldSoundsPlayTests
    {
        GameObject? _floor, _wren, _a, _b;
        float _lx;
        InkSoundBank Bank => InkSoundBank.Instance!;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        IEnumerator Until(System.Func<bool> done, float seconds = 2f)
        {
            float t = 0f;
            while (!done() && t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }

        int _mark;
        void Mark() => _mark = Bank.Recent.Count;
        System.Collections.Generic.List<string> Since => Bank.Recent.Skip(_mark).ToList();
        /// <summary>The room's sounds since the mark, the pages' left out: a caption or a journal note is the page's toast as well.</summary>
        System.Collections.Generic.List<string> RoomSince => Since.Where(id => InkSounds.Of(id)?.Bus != Mix.Bus.Ui).ToList();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            Pause.End();
            GameState.NewGame();
            Assert.IsNotNull(InkSoundBank.Instance, "the bank wakes with the game");
            Assert.IsNotNull(WorldSoundHooks.Instance, "and the world's hooks beside it");
            Bank.Hush();
            _lx = Bank.ListenerX;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _a, _b, _wren, _floor }) if (go != null) Object.Destroy(go);
            if (InkSoundBank.Instance != null) InkSoundBank.Instance.Hush();
            Pause.End();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        WrenController MakeWren()
        {
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(_lx, -0.5f, 0f);
            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            var ctrl = _wren.AddComponent<WrenController>();
            ctrl.groundMask = LayerMask.GetMask("Ground");
            ctrl.Input = new ScriptedInput();
            ctrl.Recompute();
            _wren.AddComponent<WrenVitals>();
            _wren.AddComponent<WrenSounds>();
            ctrl.Teleport(new Vector2(_lx, 0f));
            return ctrl;
        }

        TravelPoint MakeLamp(string name, string vantage)
        {
            var go = new GameObject(name) { layer = Layer("Trigger") };
            go.transform.position = new Vector3(_lx + 3f, 0.5f, 0f);
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var tp = go.AddComponent<TravelPoint>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(TravelPoint).GetField("_waypointId", flags)!.SetValue(tp, "lamp." + name);
            typeof(TravelPoint).GetField("_kind", flags)!.SetValue(tp, WaypointKind.Lamp);
            typeof(TravelPoint).GetField("_litByVantage", flags)!.SetValue(tp, vantage);
            return tp;
        }

        [UnityTest]
        public IEnumerator ALampLitWhileSheIsThereWhumpsAndOneAlreadyLitDoesNot()
        {
            GameState.World.MarkSurveyed("Sound_Test/Old");
            var old = MakeLamp("Sound_Old", "Sound_Test/Old");
            _a = old.gameObject;
            var lamp = MakeLamp("Sound_New", "Sound_Test/New");
            _b = lamp.gameObject;
            yield return null;
            yield return null;
            Assert.IsTrue(old.IsLit);
            Assert.IsFalse(Bank.Recent.Contains("lamp_lit"), "a lamp lit before the room woke is simply lit");
            Mark();
            GameState.World.MarkSurveyed("Sound_Test/New");
            yield return Until(() => Since.Contains("lamp_lit"), 1f);
            Assert.AreEqual(1, Since.Count(id => id == "lamp_lit"), "the new one whumps, once");
            Assert.Greater(Bank.LastPan, 0f, "from the right, where it stands");
            yield return null;
            Assert.AreEqual(1, Since.Count(id => id == "lamp_lit"), "and only once");
        }

        [UnityTest]
        public IEnumerator ASeedDropsPitchedByItsWorth()
        {
            MakeWren();
            yield return Frames(2);
            Mark();
            _a = IrisSeed.Spawn(new Vector2(_lx, 0.5f), 3).gameObject;
            yield return Until(() => Since.Contains("seed"), 1f);
            Assert.That(Since, Does.Contain("seed"), "taken: a drop");
            Assert.AreEqual(WorldSounds.SeedPitch(3), Bank.LastPitch, 0.001f, "pitched by its worth");
            Assert.Greater(WorldSounds.SeedPitch(3), 1f);
        }

        [UnityTest]
        public IEnumerator DecisionsFadesErasuresAndCommissionsEachSoundOnce()
        {
            yield return null;
            var w = GameState.World;
            Mark();
            Assert.IsTrue(Places.Decide(w, "Sound_Test/Anchored", PlaceFate.Anchored));
            Assert.IsTrue(Places.Decide(w, "Sound_Test/Held", PlaceFate.Held));
            Assert.IsTrue(Places.Decide(w, "Sound_Test/Released", PlaceFate.Released));
            CollectionAssert.AreEqual(new[] { "anchor", "held", "released" }, RoomSince, "a stake, a hand, a breath");

            Mark();
            Assert.IsTrue(FadeStages.Advance(w, "Sound_Test/Fading", 1));
            CollectionAssert.AreEqual(new[] { "fade_step" }, RoomSince, "a stage: one soft pass");
            yield return null;
            Mark();
            Assert.IsTrue(Atlas.Erase(w, "Sound_Test/Fading"));
            CollectionAssert.AreEqual(new[] { "erased" }, RoomSince, "an erasure is the eraser, not a stage as well");
            yield return null;

            const string id = "Sound_Test/Commission";
            Mark();
            Assert.IsTrue(Commissions.Post(w, id));
            Assert.IsTrue(Commissions.Take(w, id));
            Assert.IsTrue(Commissions.Fulfil(w, id));
            Assert.IsTrue(Commissions.Close(w, id));
            CollectionAssert.AreEqual(new[] { "ledger_take", "commission_done", "stamp" }, RoomSince, "posting is silent; then the tick, the stroke, the stamp");
        }

        [UnityTest]
        public IEnumerator AShutWayKnocksWhereItIsAPelletLandsAndAnAbilityIsAFlourish()
        {
            var ctrl = MakeWren();
            yield return Frames(2);
            _a = new GameObject("Shut") { layer = Layer("Trigger") };
            _a.transform.position = new Vector3(_lx - 4f, 0.5f, 0f);
            _a.AddComponent<BoxCollider2D>().isTrigger = true;
            var gate = _a.AddComponent<RoomTransition>();
            Mark();
            gate.Bump(ctrl);
            gate.Bump(ctrl);
            CollectionAssert.AreEqual(new[] { "barred" }, RoomSince, "one knock, not one a frame");
            Assert.Less(Bank.LastPan, 0f, "from the left, where the way is");

            Mark();
            var pellet = EnemyProjectile.Spawn("Pulp", null, new Vector2(_lx + 2f, 0.05f), new Vector2(0f, -2f), new Vector2(0.3f, 0.3f), 1, null);   // on the floor (in the air it crumbles: AUD-19)
            yield return Until(() => Since.Contains("pellet_land"), 1f);
            Assert.That(Since, Does.Contain("pellet_land"), "a wet pat where it is spent");

            Mark();
            _wren!.GetComponent<AbilitySet>().Unlock(Ability.Wingbeat);
            Assert.That(Since, Does.Contain("ability"), "learned: a flourish rising");
        }

        [UnityTest]
        public IEnumerator ThePropsThatMoveAreHeardWhereTheyAre()
        {
            var ctrl = MakeWren();
            yield return Frames(2);

            Mark();
            _a = new GameObject("WeakFloor") { layer = Layer("Ground") };
            _a.transform.position = new Vector3(_lx + 3f, 0f, 0f);
            _a.AddComponent<BoxCollider2D>().size = new Vector2(2f, 0.5f);
            var floor = _a.AddComponent<WeakFloor>();
            Assert.IsFalse(floor.TakeHit(new HitInfo { Damage = 1, Direction = Vector2.down }), "a quill only scratches it");
            CollectionAssert.IsEmpty(Since, "scratched: nothing gives");
            Assert.IsTrue(floor.TakeHit(new HitInfo { Damage = 3, Direction = Vector2.down }), "the plumb weight's 3");
            CollectionAssert.AreEqual(new[] { "floor_break" }, Since, "the floor gives way");
            Assert.Greater(Bank.LastPan, 0f, "from the right, where it was");
            Object.Destroy(_a); _a = null;

            Mark();
            _a = new GameObject("Hidden") { layer = Layer("Ground") };
            _a.transform.position = new Vector3(_lx - 3f, 2f, 0f);
            _a.AddComponent<BoxCollider2D>().size = new Vector2(2f, 0.4f);
            var hidden = _a.AddComponent<HiddenPlatform>();
            yield return null;
            CollectionAssert.IsEmpty(Since, "not drawn, and silent about it");
            hidden.Reveal(0.2f);
            hidden.Reveal(0.3f);
            CollectionAssert.AreEqual(new[] { "plank_drawn" }, Since, "the lantern draws it, once");
            Assert.Less(Bank.LastPan, 0f, "on the left, where it is");
            yield return Until(() => Since.Contains("plank_undrawn"), 1.5f);
            CollectionAssert.AreEqual(new[] { "plank_drawn", "plank_undrawn" }, Since, "and lets it go");
            Object.Destroy(_a); _a = null;

            Mark();
            _wren!.GetComponent<AbilitySet>().Unlock(Ability.Clarity);
            Mark();
            _b = new GameObject("LanternPlank") { layer = Layer("Ground") };
            _b.transform.position = new Vector3(_lx + 2f, 2f, 0f);
            _b.AddComponent<BoxCollider2D>().size = new Vector2(2f, 0.4f);
            var plank = _b.AddComponent<LanternPlatform>();
            yield return Frames(3);
            Assert.IsTrue(plank.IsDrawn, "within her reach");
            Assert.That(Since, Does.Contain("plank_drawn"), "drawn as she comes near");
            Mark();
            ctrl.Teleport(new Vector2(_lx - 15f, 0f));
            yield return Frames(3);
            Assert.IsFalse(plank.IsDrawn);
            CollectionAssert.AreEqual(new[] { "plank_undrawn" }, Since, "let go as she leaves");
            Object.Destroy(_b); _b = null;
            ctrl.Teleport(new Vector2(_lx, 0f));
            yield return Frames(2);

            Mark();
            _a = new GameObject("Tide");
            var tide = _a.AddComponent<Tide>();
            tide.WestBank = new Vector2(_lx - 4f, 0f); tide.EastBank = new Vector2(_lx + 4f, 0f);
            tide.Sweep(ctrl);
            Assert.That(Since, Does.Contain("tide_sweep"), "the tide takes her back");
            Object.Destroy(_a); _a = null;

            _a = Updraft.Make("Updraft", null, new Vector2(_lx + 1f, 3f), new Vector2(1.5f, 6f), 9f).gameObject;
            var air = _a.GetComponent<UpdraftVoice>();
            Assert.IsNotNull(air, "every updraft is heard");
            yield return Frames(3);
            Assert.AreEqual(WorldSounds.UpdraftCue, air.Looping, "the air rising");
            Assert.AreEqual(UpdraftVoice.Idle, air.Level, 0.05f, "a hiss while it carries no one");
        }

        [UnityTest]
        public IEnumerator TheOptionsPageOpensMovesAndTicksASliderAtItsLevel()
        {
            float music = Options.Get(Options.Volume.Music);
            _a = new GameObject("Options_Test");
            var view = _a.AddComponent<OptionsView>();
            yield return null;
            try
            {
                Mark();
                view.Open();
                Assert.AreEqual("ui_open", Bank.Last, "paper lifted");
                view.MoveRow((int)OptionsView.Item.VolumeMusic);
                Assert.AreEqual("ui_move", Bank.Last, "the nib's tick");
                Assert.AreEqual((int)OptionsView.Item.VolumeMusic, view.Row);
                view.Adjust(-1);
                Assert.AreEqual("ui_tick", Bank.Last);
                Assert.AreEqual(WorldSounds.TickPitch(Options.Get(Options.Volume.Music)), Bank.LastPitch, 0.001f, "the tick carries the level");
                float down = Bank.LastPitch;
                view.Adjust(+1);
                Assert.Greater(Bank.LastPitch, down - 0.001f, "and rises with it");
                view.Close();
                Assert.AreEqual("ui_close", Bank.Last, "paper set down");
                CollectionAssert.AreEqual(new[] { "ui_open", "ui_move", "ui_tick", "ui_tick", "ui_close" }, Since);
            }
            finally
            {
                if (view.IsOpen) view.Close();
                Options.Set(Options.Volume.Music, music);
            }
        }

        [UnityTest]
        public IEnumerator PausedThePagesAreHeardAndTheRoomIsNot()
        {
            yield return null;
            Pause.Begin();
            try
            {
                yield return new WaitForSecondsRealtime(0.5f);
                UiSounds.Open();
                Assert.AreEqual("ui_open", Bank.Last, "the page sounds");
                yield return null;
                Assert.Greater(Bank.UiVolume, 0.5f, "the pages ride the Ui bus, whole while paused");
                Assert.Less(Bank.Volume, 0.05f, "while the room's bus is silent");
                Assert.AreEqual(Mix.Live!.Gain(Mix.Bus.Ui), Bank.UiVolume, 0.01f, "the Ui bus's gain is the pages'");
            }
            finally { Pause.End(); }
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.Greater(Bank.Volume, 0.5f, "unpaused, the room is back");
        }
    }
}
