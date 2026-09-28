#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// The feel-test's recorder and course (PRO-03): presses counted as acted on, buffered, coyote or dropped on the
    /// real controller; a fall into a pit put back at the station; and the course built from its numbers with its
    /// spawns, its ledges and its three crabs.
    /// </summary>
    public class FeelRecorderTests
    {
        /// <summary>Scripted input whose buffers are named, like the real reader's.</summary>
        sealed class NamedInput : IWrenInput
        {
            public Vector2 Move { get; set; }
            public bool JumpHeld { get; set; }
            public bool BindHeld { get; set; }
            public bool SurveyHeld { get; set; }
            readonly ButtonBuffer _jump = new ButtonBuffer(6, "Jump"), _dash = new ButtonBuffer(4, "Dash"), _attack = new ButtonBuffer(6, "Attack"),
                _flourish = new ButtonBuffer(6), _instrument = new ButtonBuffer(6), _cycle = new ButtonBuffer(6), _thread = new ButtonBuffer(4);
            public void PressJump() => _jump.Press();
            public void PressDash() => _dash.Press();
            public bool ConsumeJump() => _jump.Consume();
            public bool ConsumeDash() => _dash.Consume();
            public bool ConsumeAttack() => _attack.Consume();
            public bool ConsumeFlourish() => _flourish.Consume();
            public bool ConsumeInstrument() => _instrument.Consume();
            public bool ConsumeCycleInstrument() => _cycle.Consume();
            public bool ConsumeThread() => _thread.Consume();
            public void Tick() { _jump.Tick(); _dash.Tick(); _attack.Tick(); _flourish.Tick(); _instrument.Tick(); _cycle.Tick(); _thread.Tick(); }
        }

        GameObject? _floor, _wren, _rec;
        WrenController _ctrl = null!;
        NamedInput _input = null!;
        FeelRecorder _recorder = null!;

        static int Layer(string n) { int l = LayerMask.NameToLayer(n); Assert.GreaterOrEqual(l, 0, "layer " + n); return l; }

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            _floor = new GameObject("Floor") { layer = Layer("Ground") };
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _wren = new GameObject("Wren") { layer = Layer("Player") };
            var box = _wren.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 1.1f); box.offset = new Vector2(0f, 0.55f);
            _wren.AddComponent<Rigidbody2D>();
            _wren.AddComponent<AbilitySet>();
            _input = new NamedInput();
            _ctrl = _wren.AddComponent<WrenController>();
            _ctrl.groundMask = LayerMask.GetMask("Ground");
            _ctrl.Input = _input;
            _ctrl.Recompute();
            _ctrl.Teleport(new Vector2(0f, 0f));
            _rec = new GameObject("Recorder");
            _recorder = _rec.AddComponent<FeelRecorder>();
            _recorder.Wren = _ctrl;
            _recorder.OnCourse = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in new[] { _rec, _wren, _floor }) if (go != null) Object.Destroy(go);
            var course = SceneManager.GetSceneByName(FeelCourseRooms.SceneName);
            if (course.IsValid() && course.isLoaded) yield return SceneManager.UnloadSceneAsync(course);
        }

        static IEnumerator Fixed(int frames) { for (int i = 0; i < frames; i++) yield return new WaitForFixedUpdate(); }

        [UnityTest]
        public IEnumerator PressesAreCountedAsActedBufferedCoyoteOrDropped()
        {
            yield return Fixed(3);
            Assert.IsTrue(_ctrl.IsGrounded);
            // On the ground: acted on the same frame.
            _input.PressJump();
            yield return Fixed(2);
            Assert.IsFalse(_ctrl.IsGrounded, "she jumped");
            var s = _recorder.Session();
            Assert.AreEqual(1, s.Presses("Jump")); Assert.AreEqual(1, s.jumpWaits[0]); Assert.AreEqual(0, s.BufferedJumps);
            // In the air, pressed too early to land within the window: a press that did nothing.
            _input.PressJump();
            yield return Fixed(8);
            s = _recorder.Session();
            Assert.AreEqual(1, s.Dropped("Jump"), "six frames in the air with nowhere to jump");
            // Wait for the landing, then press a few frames before the next landing: buffered.
            yield return Fixed(60);
            Assert.IsTrue(_ctrl.IsGrounded);
            int landings = _recorder.Landings;
            _input.PressJump();
            yield return Fixed(1);
            // Find the frame she is about to land, press, and see the jump taken on landing.
            for (int i = 0; i < 90 && _recorder.Landings == landings; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(_recorder.Landings, landings, "landed from the second jump");
            // Now: jump, press three frames early, land, and see it buffered.
            _input.PressJump();
            yield return Fixed(2);
            Assert.IsFalse(_ctrl.IsGrounded);
            int before = _recorder.Session().BufferedJumps;
            // Press every three frames until she is up again: the last press before landing waits in the buffer.
            for (int i = 0; i < 40 && _ctrl.IsGrounded == false; i++)
            {
                if (i % 3 == 0) _input.PressJump();
                yield return new WaitForFixedUpdate();
            }
            yield return Fixed(2);
            s = _recorder.Session();
            Assert.Greater(s.BufferedJumps, before, "a press before the landing was kept and taken: " + string.Join(",", s.jumpWaits));
            Assert.GreaterOrEqual(s.Dropped("Jump"), 1, "a press three frames after a press refreshes the window, so only the first was dropped");
            Assert.Greater(s.Presses("Jump"), s.acted.Sum(), "presses are more than acts");
            Assert.LessOrEqual(s.acted[0] + s.dropped[0], s.Presses("Jump"), "a press is acted on, dropped, or still pending, never two of those");

            // Off the edge, a press within the coyote window still jumps.
            yield return Fixed(60);
            _ctrl.Teleport(new Vector2(19.6f, 0f));
            yield return Fixed(3);
            _input.Move = Vector2.right;
            for (int i = 0; i < 30 && _ctrl.IsGrounded; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(_ctrl.IsGrounded, "she ran off the edge");
            yield return Fixed(2);
            _input.PressJump();
            yield return Fixed(2);
            _input.Move = Vector2.zero;
            Assert.AreEqual(1, _recorder.CoyoteJumps, "the coyote window let her");
            Assert.Greater(_recorder.Seconds, 0f);
            var json = _recorder.Session().ToJson();
            StringAssert.Contains("\"coyoteJumps\": 1", json);
        }

        [UnityTest]
        public IEnumerator AFallIntoAPitIsPutBackAtTheStation()
        {
            _recorder.OnCourse = true;
            _floor!.transform.position = new Vector3(45f, FeelTest.PitY - 0.5f, 0f);   // the catch floor under the hops
            _ctrl.Teleport(new Vector2(45f, FeelTest.PitY + 0.2f));
            yield return Fixed(2);
            Assert.AreEqual(1, _recorder.Falls);
            Assert.AreEqual(FeelTest.Station.Hops, _recorder.Station);
            var back = FeelTest.RestartOf(FeelTest.Station.Hops);
            Assert.AreEqual(back.x, _ctrl.Position.x, 0.5f, "put back at the hops' start");
            Assert.Greater(_ctrl.Position.y, FeelTest.PitY + 2f);
            var s = _recorder.Session();
            Assert.AreEqual(1, s.stationFalls[(int)FeelTest.Station.Hops]);
            Assert.Greater(s.stationSeconds[(int)FeelTest.Station.Hops], 0f);
        }

        [UnityTest]
        public IEnumerator TheCourseBuildsFromItsNumbers()
        {
            Assert.IsTrue(RoomManager.Generators.Contains(FeelCourseRooms.Build), "registered with the room manager");
            Assert.IsNull(FeelCourseRooms.Build("Greybox_Saltmarrow_A"), "only its own name");
            var room = FeelCourseRooms.Build(FeelCourseRooms.SceneName);
            yield return null;
            Assert.IsNotNull(room);
            var t = room!.transform;
            foreach (var l in FeelTest.Ledges()) Assert.IsNotNull(t.Find(l.Name) ?? Find(t, l.Name), l.Name + " is drawn");
            Assert.AreEqual(FeelTest.TargetXs.Length, room.GetComponentsInChildren<MarshCrab>().Length, "three crabs");
            Assert.AreEqual(FeelTest.TargetXs.Length, room.GetComponentsInChildren<FeelTarget>().Length, "each counted");
            Assert.IsNotNull(Find(t, "Start"), "a start spawn");
            Assert.IsNotNull(Find(t, "PitFloor"), "nobody falls for ever");
            Assert.IsNotNull(Find(t, "EndPost"));
        }

        static Transform? Find(Transform root, string name)
        {
            foreach (var c in root.GetComponentsInChildren<Transform>(true)) if (c.name == name) return c;
            return null;
        }
    }
}
