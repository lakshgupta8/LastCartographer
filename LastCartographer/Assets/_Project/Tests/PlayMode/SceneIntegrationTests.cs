using System.Collections;
using NUnit.Framework;
using OWSBG.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OWSBG.Tests
{
    /// <summary>
    /// End-to-end: the shipped Persistent scene loads its start room, a simulated keyboard drives
    /// the real InputReader → WrenController path, and walking into the east edge transitions rooms.
    /// This is the "press Play and it works" guarantee.
    /// </summary>
    public class SceneIntegrationTests
    {
        Keyboard _keyboard;
        InputSettings.EditorInputBehaviorInPlayMode _previousBehavior;
        InputSettings.BackgroundBehavior _previousBackground;

        [SetUp]
        public void SetUp()
        {
            // In the editor, input only reaches Play mode when the Game view has focus, and devices
            // are disabled while the player is unfocused. Batch mode is never focused, so deliver
            // everything regardless for the duration of the test.
            OWSBG.Core.GameState.NewGame();
            Bootstrap.SkipPrologueOverride = true;   // this test is about Saltmarrow; the Edge has its own
            _previousBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            _previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_keyboard != null) { InputSystem.RemoveDevice(_keyboard); _keyboard = null; }
            InputSystem.settings.editorInputBehaviorInPlayMode = _previousBehavior;
            InputSystem.settings.backgroundBehavior = _previousBackground;
            Bootstrap.SkipPrologueOverride = null;
            Time.timeScale = 1f;

            // Leave a clean slate for other tests even if this one failed mid-way.
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_"))
                    yield return SceneManager.UnloadSceneAsync(s);
            }
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return new WaitForFixedUpdate();
        }

        void Hold(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            InputSystem.Update();
        }

        void Release()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.Update();
        }

        [UnityTest]
        public IEnumerator PersistentSceneLoadsRoomAndKeyboardDrivesWrenThroughATransition()
        {
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            float t = 0f;
            while ((RoomManager.Instance == null || string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom)) && t < 8f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.IsNotNull(RoomManager.Instance, "RoomManager should exist in the persistent scene");
            Assert.AreEqual("Greybox_Saltmarrow_A", RoomManager.Instance.CurrentRoom, "start room should load");

            var wren = Object.FindFirstObjectByType<WrenController>();
            Assert.IsNotNull(wren, "Wren should exist in the persistent scene");
            Assert.IsNotNull(wren.Input, "Wren should have an input source (InputReader)");

            yield return Frames(40);
            Assert.IsTrue(wren.IsGrounded, "Wren should land on the room floor");

            _keyboard = InputSystem.AddDevice<Keyboard>();
            float x0 = wren.Position.x;
            Hold(Key.D);
            yield return Frames(2);
            var reader = wren.GetComponent<OWSBG.Core.InputReader>();
            Assert.IsNotNull(reader, "Wren should have an InputReader");
            Assert.IsNotNull(reader.Asset, "InputReader should reference the WrenInput asset");
            var map = reader.Asset.FindActionMap("Player");
            Assert.IsTrue(_keyboard.enabled, "simulated keyboard should be enabled (background behaviour)");
            Assert.IsTrue(_keyboard.dKey.isPressed, "simulated keyboard should report D pressed");
            Assert.IsTrue(map.enabled, "Player action map should be enabled");
            Assert.AreEqual(1f, map.FindAction("Move").ReadValue<Vector2>().x, 0.01f, "Move action should read D as +x");
            Assert.AreEqual(1f, wren.Input.Move.x, 0.01f, "InputReader.Move should read +x");
            yield return Frames(28);
            Release();
            Assert.Greater(wren.Position.x - x0, 2.5f, "holding D should move Wren right through the real input path");

            // Jump via the real path.
            yield return Frames(10);
            Hold(Key.Space);
            yield return Frames(4);
            Assert.Greater(wren.Velocity.y, 5f, "Space should jump");
            Release();
            yield return Frames(60);

            // Strike the training dummy through the real input path (J).
            var dummy = Object.FindFirstObjectByType<TrainingDummy>();
            Assert.IsNotNull(dummy, "room A should contain the training dummy");
            var dummyX = dummy.transform.position.x;
            wren.Teleport(new Vector2(dummyX - 1.6f, 0f));
            yield return Frames(5);
            Hold(Key.D);                           // face right toward the dummy
            yield return Frames(2);
            Release();
            yield return Frames(6);                // stop before swinging
            int hitsBefore = dummy.Hits;
            var strike = wren.GetComponent<QuillStrike>();
            Assert.IsNotNull(strike, "Wren should have a QuillStrike");
            bool swung = false; Vector2 swingDir = Vector2.zero;
            strike.Swung += d => { swung = true; swingDir = d; };
            Hold(Key.J);
            yield return Frames(2);
            Release();
            yield return Frames(15);
            strike.GetHitbox(out var hbCenter, out var hbSize);
            var diag = $"swung={swung} dir={swingDir} wren={wren.Position} facing={wren.Facing} frozen={wren.Frozen} " +
                       $"dummy={(Vector2)dummy.transform.position} layer={LayerMask.LayerToName(dummy.gameObject.layer)} " +
                       $"hitbox={hbCenter}/{hbSize} mask={strike.hitMask.value} grounded={wren.IsGrounded}";
            Assert.IsTrue(swung, "J should start a swing. " + diag);
            Assert.AreEqual(hitsBefore + 1, dummy.Hits, "pressing J should land one quill strike on the dummy. " + diag);
            Assert.Greater(wren.GetComponent<Inkwell>().Pips, 0, "a landed strike should fill ink");

            // Walk into the east transition.
            wren.Teleport(new Vector2(17.5f, 0f));
            yield return Frames(5);
            Hold(Key.D);
            t = 0f;
            while (RoomManager.Instance.CurrentRoom != "Greybox_Saltmarrow_B" && t < 8f)
            {
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            Release();
            Assert.AreEqual("Greybox_Saltmarrow_B", RoomManager.Instance.CurrentRoom, "east edge should transition to room B");
            Assert.Less(wren.Position.x, -10f, "Wren should arrive at room B's west spawn");
            Assert.IsFalse(SceneManager.GetSceneByName("Greybox_Saltmarrow_A").isLoaded, "room A should unload");
        }
    }
}
