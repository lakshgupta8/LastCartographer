#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The roll-call in the game (AUD-02): the singer wakes with the game; a bounds-walk is sung live (the call at the
    /// walk's beat when a name is called, the chorus faltering on a miss, the answer at the region's beat when a verse
    /// stands); the scripts' uses play with their captions; the whale sings when the Reedmother is drawn; and the
    /// voices ride the Dialogue bus's gain.
    /// </summary>
    public class RollCallSingerTests
    {
        GameObject? _floor, _wren, _room;
        WrenController? _ctrl;
        BoundsWalk? _walk;
        RollCallSinger Singer => RollCallSinger.Instance!;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            Pause.End();
            GameState.NewGame();
            Assert.IsNotNull(RollCallSinger.Instance, "booted with the first scene");
            Singer.Hush();
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _wren.AddComponent<Inkwell>();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = new ScriptedInput();
            _ctrl.Recompute();
            _ctrl.Teleport(new Vector2(-15f, 0f));

            _room = new GameObject("Room_Sing");
            _room.AddComponent<Room>().RoomId = "Saltmarrow_Sing";
            var walkGo = new GameObject("Walk");
            walkGo.transform.SetParent(_room.transform, false);
            _walk = walkGo.AddComponent<BoundsWalk>();
            _walk.Id = "merrows_end";
            _walk.SecondsPerBeat = 0.4f;
            _walk.AddVerse("one", ("the stoop", new Vector2(-8f, 0f)), ("the post", new Vector2(0f, 0f)));
            _walk.AddVerse("two", ("the post", new Vector2(0f, 0f)));
            walkGo.SetActive(false); walkGo.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { _room, _wren, _floor }) if (go != null) Object.Destroy(go);
            if (RollCallSinger.Instance != null) RollCallSinger.Instance.Hush();
            Pause.End();
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        [UnityTest]
        public IEnumerator TheWalkIsSungAndAMissFalters()
        {
            int called = 0, landed = 0, versesDone = 0;
            bool? faltered = null; string? onLanding = null, onVerse = null; AudioClip? callClip = null; float answerLength = 0f;
            void OnCalled(BoundsWalk w, BoundsWalk.Bound b) { called++; callClip = Singer.Calling; }
            void OnLanded(BoundsWalk w, BoundsWalk.Bound b, bool hit) { landed++; faltered = Singer.Faltered; onLanding = Singer.Last; }
            void OnVerse(BoundsWalk w, int v) { versesDone++; onVerse = Singer.Last; answerLength = Singer.LastClip != null ? Singer.LastClip.length : 0f; }
            BoundsWalk.NameCalled += OnCalled; BoundsWalk.BeatLanded += OnLanded; BoundsWalk.VerseDone += OnVerse;
            try
            {
                Assert.AreEqual(Region.Saltmarrow, RollCallSinger.RegionOf(_walk!), "the walk sings in the coast's key");
                int before = Singer.Sung;
                Assert.IsTrue(_walk!.Begin());
                float t = 0f;
                while (called < 1 && t < 3f) { t += Time.deltaTime; yield return null; }
                Assert.AreEqual(1, called, "the first name was called");
                Assert.IsNotNull(callClip, "and sung as it was called");
                Assert.AreEqual("call", Singer.Last);
                Assert.GreaterOrEqual(callClip!.length, _walk.SecondsPerBeat, "the call is one walk beat: the pickup, then the name held");
                Assert.Less(callClip.length, _walk.SecondsPerBeat + 1f);
                Assert.AreEqual(1, callClip.channels);

                // She is far from the stoop: the beat lands without her and the chorus falters.
                while (landed < 1 && t < 4f) { t += Time.deltaTime; yield return null; }
                Assert.AreEqual(1, landed);
                Assert.IsTrue(faltered, "the name should land and she is not there");
                Assert.AreEqual("falter", onLanding);
                Assert.IsNotNull(Singer.Calling, "the faltering name is on the caller");

                // On the post for the second beat: the name holds, and the verse's end is answered.
                _ctrl!.Teleport(new Vector2(0f, 0f));
                while (landed < 2 && t < 5f) { t += Time.deltaTime; yield return null; }
                Assert.AreEqual(2, landed);
                Assert.IsFalse(faltered, "she was there: no falter");
                Assert.AreEqual(1, versesDone, "the verse stands");
                Assert.AreEqual("call", onLanding, "the name holds as it lands");
                Assert.AreEqual("answer", onVerse, "and the chorus answers as the verse stands");
                float coast = AudioDirection.BeatOf(Region.Saltmarrow);
                Assert.GreaterOrEqual(answerLength, RollCallSong.Seconds(RollCallSong.Form.Answer, coast), "the answer in the region's own time");
                Assert.IsTrue(Singer.VoiceIsPlaying);
                Assert.Greater(Singer.Sung, before + 3, "call, falter, call, answer");
            }
            finally { BoundsWalk.NameCalled -= OnCalled; BoundsWalk.BeatLanded -= OnLanded; BoundsWalk.VerseDone -= OnVerse; _walk!.Abort(); }
        }

        [UnityTest]
        public IEnumerator TheUsesSingWithTheirCaptions()
        {
            string? caption = null;
            void OnShown(string text, float seconds) => caption = text;
            Captions.Shown += OnShown;
            try
            {
                Assert.IsFalse(Singer.Sing("nothing"), "no such use");
                Assert.IsTrue(Singer.Sing("whale"));
                yield return null;
                Assert.AreEqual("whale", Singer.Last);
                Assert.IsNotNull(caption); StringAssert.Contains("song", caption, "a deaf player reads it");
                float blank = AudioDirection.BeatOf(Region.Blank);
                Assert.GreaterOrEqual(Singer.LastClip!.length, RollCallSong.Seconds(RollCallSong.Form.Whale, blank), "each note twice as long at the Blank's beat");
                Assert.AreEqual(RollCallSong.SampleRate, Singer.LastClip.frequency);
                Assert.IsTrue(Singer.VoiceIsPlaying);
                int cached = Singer.Cached;
                var first = Singer.LastClip;
                Assert.IsTrue(Singer.Sing("whale"));
                Assert.AreEqual(cached, Singer.Cached, "rendered once and kept");
                Assert.AreSame(first, Singer.LastClip);

                GameState.World.Set("emberdown.kettil.met", true);
                GameState.World.Set("corra.decided", true);
                Assert.IsTrue(Singer.Sing("chorus"));
                float ember = AudioDirection.BeatOf(Region.Emberdown);
                Assert.GreaterOrEqual(Singer.LastClip!.length, RollCallSong.Seconds(RollCallSong.Form.Verse, ember, 4), "four voices, four names, at Runa's beat");
                Assert.IsTrue(Singer.Sing("runa"));
                Assert.GreaterOrEqual(Singer.LastClip!.length, RollCallSong.Seconds(RollCallSong.Form.Verse, ember, 3));
            }
            finally { Captions.Shown -= OnShown; }
        }

        [UnityTest]
        public IEnumerator TheWhaleSingsWhenTheReedmotherIsDrawn()
        {
            int before = Singer.Sung;
            Assert.IsTrue(GameState.World.MarkSurveyed(RollCallSinger.WhaleVantage));
            yield return null;
            Assert.AreEqual(before, Singer.Sung, "not at once: the drawing's caption first");
            float t = 0f;
            while (Singer.Sung == before && t < RollCallSinger.WhaleDelay + 1f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual("whale", Singer.Last, "under the bridge, late and slow");
            Assert.AreEqual(before + 1, Singer.Sung);
            Assert.IsFalse(GameState.World.MarkSurveyed("Saltmarrow_A/Somewhere") && Singer.Last != "whale", "another vantage is not the bridge");
        }

        [UnityTest]
        public IEnumerator TheVoicesRideTheDialogueBus()
        {
            Assert.IsNotNull(MixDriver.Instance);
            MixDriver.Instance!.Mixer.Snap(Mix.Snapshot.Explore);
            yield return null;
            Assert.AreEqual(MixDriver.Instance.Mixer.Gain(Mix.Bus.Dialogue), Singer.Volume, 0.01f, "the bus's gain is the voices'");
            Pause.Begin();
            try
            {
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.Less(Singer.Volume, 0.05f, "paused: nothing in the room, the voices with it");
                Assert.AreEqual(MixDriver.Instance.Mixer.Gain(Mix.Bus.Dialogue), Singer.Volume, 0.01f);
            }
            finally { Pause.End(); }
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.Greater(Singer.Volume, 0.9f, "and back");
        }
    }
}
