#nullable enable
using System.Collections;
using NUnit.Framework;
using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.UI;
using OWSBG.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace OWSBG.Tests
{
    /// <summary>
    /// DES-14 through a keyboard (the Input System's test fixture, so the presses are real device events): toggled holds
    /// latch until pressed again or let go; a remapped key is the one that works and the swapped one moves; listening
    /// takes the next key and Esc keeps the old; and in the game, Esc opens the options page, which stops the world,
    /// changes what it says, and gives the world back. Captions wait for the player when asked, and a line of dialogue
    /// never moves on by itself.
    /// </summary>
    public class InputAccessibilityTests : InputTestFixture
    {
        Keyboard _kb = null!;
        InputActionAsset? _copy;
        GameObject? _readerGo;

        public override void Setup()
        {
            base.Setup();
            _kb = InputSystem.AddDevice<Keyboard>();
            PlayerPrefs.DeleteKey(Controls.PrefsKey);
            Options.ResetToDefaults();
            Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey);
            Time.timeScale = 1f;
            GameState.NewGame();
        }

        public override void TearDown()
        {
            if (_readerGo != null) Object.DestroyImmediate(_readerGo);
            if (_copy != null) Object.DestroyImmediate(_copy);
            Pause.End();
            PlayerPrefs.DeleteKey(Controls.PrefsKey);
            Options.ResetToDefaults();
            Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey);
            Controls.Asset = null;
            Time.timeScale = 1f;
            base.TearDown();
        }

        static InputActionAsset Shipped()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/WrenInput.inputactions");
#else
            return null!;
#endif
        }

        InputReader Reader()
        {
            _copy = InputActionAsset.FromJson(Shipped().ToJson());
            _readerGo = new GameObject("Reader");
            _readerGo.SetActive(false);
            var r = _readerGo.AddComponent<InputReader>();
            r.Asset = _copy;
            _readerGo.SetActive(true);
            Assert.AreSame(_copy, Controls.Asset);
            return r;
        }

        /// <summary>A press the game sees in its own frame, as a player's would be.</summary>
        IEnumerator Tap(ButtonControl b)
        {
            Press(b, queueEventOnly: true);
            yield return null;
            Release(b, queueEventOnly: true);
            yield return null;
        }

        IEnumerator Down(ButtonControl b) { Press(b, queueEventOnly: true); yield return null; }
        IEnumerator Up(ButtonControl b) { Release(b, queueEventOnly: true); yield return null; }

        [UnityTest]
        public IEnumerator ToggledHoldsLatchUntilLetGo()
        {
            var r = Reader();
            yield return null;

            yield return Down(_kb.eKey);
            Assert.IsTrue(r.BindHeld, "held: down is held");
            yield return Up(_kb.eKey);
            Assert.IsFalse(r.BindHeld, "and up is not");

            Options.SetToggle(Hold.Bind, true);
            yield return Tap(_kb.eKey);
            Assert.IsTrue(r.BindHeld, "toggled: one press and it stays on");
            yield return null; yield return null;
            Assert.IsTrue(r.BindHeld);
            yield return Tap(_kb.eKey);
            Assert.IsFalse(r.BindHeld, "the next press stops it");
            yield return Tap(_kb.eKey);
            Assert.IsTrue(r.BindHeld);
            Controls.Release(Hold.Bind);
            Assert.IsFalse(r.BindHeld, "or the end of what it was doing");

            Options.SetToggle(Hold.Survey, true);
            yield return Tap(_kb.qKey);
            Assert.IsTrue(r.SurveyHeld);
            Assert.IsFalse(r.BindHeld, "each hold is its own");
            Options.SetToggle(Hold.Survey, false);
            Assert.IsFalse(r.SurveyHeld, "back to holding: nothing is left latched");
        }

        [UnityTest]
        public IEnumerator ARemappedKeyIsTheOneThatWorks()
        {
            var r = Reader();
            yield return null;
            Assert.IsTrue(Controls.Rebind(_copy!, "Jump", Controls.Device.Keyboard, "<Keyboard>/k"));

            yield return Tap(_kb.kKey);
            Assert.IsTrue(r.ConsumeJump(), "K jumps now");
            Assert.IsFalse(r.ConsumeDash());
            yield return Tap(_kb.spaceKey);
            Assert.IsFalse(r.ConsumeJump(), "Space doesn't");
            Assert.IsTrue(r.ConsumeDash(), "Space took Dash's K");

            // The next session lays the remap over a fresh asset.
            Object.DestroyImmediate(_readerGo);
            Object.DestroyImmediate(_copy);
            r = Reader();
            yield return null;
            yield return Tap(_kb.kKey);
            Assert.IsTrue(r.ConsumeJump(), "kept");
            Assert.AreEqual("K", Controls.KeyName("Jump"));
        }

        [UnityTest]
        public IEnumerator ListeningTakesTheNextKeyAndEscKeepsTheOld()
        {
            var r = Reader();
            yield return null;
            bool? result = null;
            var map = _copy!.FindActionMap("Player");
            Controls.Listen(_copy, "Attack", Controls.Device.Keyboard, ok => result = ok);
            Assert.IsFalse(map.enabled, "the map is off while it listens: the key is not a strike");
            yield return Tap(_kb.mKey);          // a menu key: passed over
            yield return Down(_kb.rKey);
            float t = 0f;
            while (result == null && t < 2f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return Up(_kb.rKey);
            Assert.AreEqual(true, result, "it took the key");
            Assert.AreEqual("<Keyboard>/r", Controls.PathOf(_copy, "Attack", Controls.Device.Keyboard));
            Assert.IsTrue(map.enabled, "and switched the map back on");
            yield return Tap(_kb.rKey);
            Assert.IsTrue(r.ConsumeAttack(), "R strikes");

            result = null;
            Controls.Listen(_copy, "Attack", Controls.Device.Keyboard, ok => result = ok);
            yield return Tap(_kb.escapeKey);
            t = 0f;
            while (result == null && t < 2f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(false, result, "Esc: nothing changed");
            Assert.AreEqual("<Keyboard>/r", Controls.PathOf(_copy, "Attack", Controls.Device.Keyboard));
            Assert.IsTrue(map.enabled);
        }

        // ---- in the game ---------------------------------------------------------------------------------------------

        static IEnumerator Until(System.Func<bool> cond, float seconds, string what)
        {
            float t = 0f;
            while (!cond() && t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        IEnumerator Boot()
        {
            Bootstrap.SkipPrologueOverride = true;
            SceneManager.LoadScene(Bootstrap.PersistentSceneName, LoadSceneMode.Single);
            yield return null; yield return null;
            yield return Until(() => RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom) && !RoomManager.Instance.IsTransitioning, 10f, "the game");
            yield return Until(() => OptionsView.Instance != null && OptionsView.Instance.Panel != null, 2f, "the options page");
        }

        IEnumerator Unboot()
        {
            Bootstrap.SkipPrologueOverride = null;
            DialogueService.Instance?.Stop();
            var empty = SceneManager.CreateScene("TestEmpty_" + Random.Range(0, 1 << 20));
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s == empty || !s.isLoaded) continue;
                if (s.name == Bootstrap.PersistentSceneName || s.name.StartsWith("Greybox_")) yield return SceneManager.UnloadSceneAsync(s);
            }
        }

        [UnityTest]
        public IEnumerator EscOpensTheOptionsWhichStopTheWorldAndChangeIt()
        {
            yield return Boot();
            try
            {
                var view = OptionsView.Instance!;
                var wren = Object.FindFirstObjectByType<WrenController>();
                Assert.IsFalse(view.IsOpen);
                yield return Tap(_kb.escapeKey);
                Assert.IsTrue(view.IsOpen, "Esc opens it");
                Assert.IsTrue(Pause.Active);
                Assert.AreEqual(0f, Time.timeScale, "the world stops");
                Assert.IsTrue(wren.Frozen);
                Assert.AreEqual(OptionsView.ItemCount, view.Panel!.Q("rows").childCount);

                // Down to Hitstop, left twice: half of it.
                yield return Tap(_kb.downArrowKey);
                Assert.AreEqual((int)OptionsView.Item.Hitstop, view.Row);
                yield return Tap(_kb.leftArrowKey);
                yield return Tap(_kb.leftArrowKey);
                Assert.AreEqual(0.5f, Options.Hitstop);
                StringAssert.Contains("50%", view.Panel.Q("row-1").Q<Label>("value").text);

                // High-contrast ink: the page itself is redrawn in it, and so is the HUD.
                yield return Tap(_kb.downArrowKey);
                yield return Tap(_kb.downArrowKey);
                yield return Tap(_kb.downArrowKey);
                Assert.AreEqual((int)OptionsView.Item.Contrast, view.Row);
                yield return Tap(_kb.enterKey);
                Assert.IsTrue(Options.HighContrast);
                Assert.AreEqual(InkTheme.HighContrast[(int)InkTheme.Swatch.Paper], view.Panel.style.backgroundColor.value, "the page, redrawn");
                Assert.AreEqual(1f, Shader.GetGlobalFloat(Options.ContrastId), "and the world");

                // Language: the page speaks the new one at once.
                for (int i = 0; i < 4; i++) yield return Tap(_kb.upArrowKey);
                Assert.AreEqual((int)OptionsView.Item.Language, view.Row);
                string before = view.Panel.Q<Label>("title").text;
                yield return Tap(_kb.rightArrowKey);
                Assert.AreEqual(Loc.Pseudo, Loc.Locale);
                Assert.IsTrue(Loc.LooksPseudo(view.Panel.Q<Label>("title").text), "was " + before);
                yield return Tap(_kb.leftArrowKey);
                Assert.AreEqual(Loc.Base, Loc.Locale);

                // The controls page, then back out, then away.
                for (int i = 0; i < 2; i++) yield return Tap(_kb.upArrowKey);
                Assert.AreEqual((int)OptionsView.Item.Controls, view.Row);
                yield return Tap(_kb.enterKey);
                Assert.AreEqual(OptionsView.Page.Controls, view.Current);
                Assert.AreEqual(Controls.Rebindable.Length + 2, view.Panel.Q("rows").childCount, "a heading, each action, the reset");
                yield return Tap(_kb.escapeKey);
                Assert.AreEqual(OptionsView.Page.Options, view.Current, "Esc: back a page");
                yield return Tap(_kb.escapeKey);
                Assert.IsFalse(view.IsOpen, "and away");
                Assert.IsFalse(Pause.Active);
                Assert.AreEqual(1f, Time.timeScale, "the world goes on");
                Assert.IsFalse(wren.Frozen);
                yield return null; yield return null;
                Assert.IsFalse(view.IsOpen, "the Esc that closed it didn't open it again");

                // Not over a conversation: that has the screen.
                Assert.IsTrue(DialogueService.Instance!.StartNode("Camp_Ashes_Ahead"));
                yield return Until(() => DialogueService.Instance.IsRunning, 2f, "the conversation");
                yield return Tap(_kb.escapeKey);
                Assert.IsFalse(view.IsOpen);
            }
            finally
            {
                OptionsView.Instance?.Close();
            }
            yield return Unboot();
        }

        [UnityTest]
        public IEnumerator CaptionsWaitWhenAskedAndLinesNeverMoveOnByThemselves()
        {
            yield return Boot();
            var prompt = UiRoot.Instance.GetComponent<PromptView>();

            prompt.Show("A caption as written", 0.2f);
            yield return Until(() => !prompt.IsShowing, 2f, "the caption to go");

            Options.Captions = CaptionTime.UntilDismissed;
            prompt.Show("A caption that waits", 0.2f);
            float t = 0f;
            while (t < 1f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(prompt.IsShowing, "still there, long after its time");
            yield return Tap(_kb.enterKey);
            yield return null;
            Assert.IsFalse(prompt.IsShowing, "until the player dismisses it");

            // Dialogue waits for the player however long they take.
            var view = UiRoot.Instance.GetComponent<DialogueView>();
            Assert.IsTrue(DialogueService.Instance!.StartNode("Camp_Ashes_Ahead"));
            yield return Until(() => view.IsVisible && !string.IsNullOrEmpty(view.LineText), 5f, "the first line");
            string line = view.LineText;
            for (int i = 0; i < 300; i++) yield return null;
            Assert.AreEqual(line, view.LineText, "the same line, 300 frames on");
            Assert.IsTrue(DialogueService.Instance.IsRunning);
            yield return Tap(_kb.enterKey);
            yield return Until(() => view.LineText != line || !DialogueService.Instance.IsRunning, 2f, "the press to move it on");
            DialogueService.Instance.Stop();
            yield return Unboot();
        }
    }
}
