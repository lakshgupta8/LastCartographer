using NUnit.Framework;
using OWSBG.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OWSBG.Tests
{
    /// <summary>
    /// Remapping on a copy of the shipped WrenInput asset (DES-14): every action has a key and a pad button; a key in use
    /// swaps places, so nothing is left without one; the menu and movement keys can't be taken; a pad's own layout is
    /// made generic; remaps are kept and laid back over a fresh asset; a reset puts every key back; and the prompts name
    /// the key the player has, held or pressed.
    /// </summary>
    public class ControlsTests
    {
        const string AssetPath = "Assets/_Project/Settings/Input/WrenInput.inputactions";
        InputActionAsset _asset;

        static InputActionAsset Fresh() =>
            InputActionAsset.FromJson(AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath).ToJson());

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(Controls.PrefsKey);
            Options.ResetToDefaults();
            Loc.Reset(); PlayerPrefs.DeleteKey(Loc.PrefsKey);
            _asset = Fresh();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(Controls.PrefsKey);
            Options.ResetToDefaults();
            Controls.Asset = null;
            Controls.LastDevice = Controls.Device.Keyboard;
            Loc.Reset();
            Object.DestroyImmediate(_asset);
        }

        string Key(string action) => Controls.PathOf(_asset, action, Controls.Device.Keyboard);
        string Pad(string action) => Controls.PathOf(_asset, action, Controls.Device.Gamepad);

        [Test]
        public void EveryActionHasAKeyAndAButton()
        {
            foreach (var action in Controls.Rebindable)
            {
                Assert.IsNotNull(Key(action), action + " on the keyboard");
                Assert.IsNotNull(Pad(action), action + " on a pad");
                Assert.AreNotEqual("-", Controls.DisplayName(_asset, action, Controls.Device.Keyboard), action);
                Assert.IsFalse(string.IsNullOrEmpty(Controls.Label(action)), action + " has a name");
            }
            Assert.AreEqual("<Keyboard>/space", Key("Jump"));
            Assert.AreEqual("<Keyboard>/leftShift", Key("Dash"), "the first of Dash's keys");
        }

        [Test]
        public void AKeyInUseSwapsPlaces()
        {
            Assert.IsTrue(Controls.Rebind(_asset, "Jump", Controls.Device.Keyboard, "<Keyboard>/j"));
            Assert.AreEqual("<Keyboard>/j", Key("Jump"));
            Assert.AreEqual("<Keyboard>/space", Key("Attack"), "Strike takes Jump's old key");

            // Dash's second key (K) is not its first, and it swaps all the same.
            Assert.IsTrue(Controls.Rebind(_asset, "Jump", Controls.Device.Keyboard, "<Keyboard>/k"));
            var dash = Controls.Find(_asset, "Dash");
            Assert.AreEqual("<Keyboard>/j", dash.bindings[1].effectivePath, "Dash's K goes to J");
            Assert.AreEqual("<Keyboard>/leftShift", Key("Dash"));

            // The pad is separate: a button taken on it swaps there only.
            Assert.IsTrue(Controls.Rebind(_asset, "Attack", Controls.Device.Gamepad, "<Gamepad>/buttonSouth"));
            Assert.AreEqual("<Gamepad>/buttonSouth", Pad("Attack"));
            Assert.AreEqual("<Gamepad>/buttonWest", Pad("Jump"));
            Assert.AreEqual("<Keyboard>/space", Key("Attack"), "the keyboard is untouched");

            // No two actions share a key afterwards.
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var action in Controls.Rebindable)
                foreach (var b in Controls.Find(_asset, action).bindings)
                    if (!b.isComposite && !b.isPartOfComposite) Assert.IsTrue(seen.Add(b.effectivePath), b.effectivePath + " used twice");
        }

        [Test]
        public void MenuAndMovementKeysCantBeTaken()
        {
            foreach (var path in new[] { "<Keyboard>/escape", "<Keyboard>/m", "<Keyboard>/w", "<Keyboard>/leftArrow" })
                Assert.IsFalse(Controls.Rebind(_asset, "Jump", Controls.Device.Keyboard, path), path);
            foreach (var path in new[] { "<Gamepad>/start", "<Gamepad>/select", "<Gamepad>/dpad/up", "<Gamepad>/leftStick/left" })
                Assert.IsFalse(Controls.Rebind(_asset, "Jump", Controls.Device.Gamepad, path), path);
            Assert.AreEqual("<Keyboard>/space", Key("Jump"));
            Assert.AreEqual("<Gamepad>/buttonSouth", Pad("Jump"));
            Assert.IsFalse(PlayerPrefs.HasKey(Controls.PrefsKey), "nothing saved");
        }

        [Test]
        public void APadsOwnLayoutIsMadeGeneric()
        {
            Assert.AreEqual("<Gamepad>/buttonNorth", Controls.Normalise("<XInputControllerWindows>/buttonNorth", Controls.Device.Gamepad));
            Assert.AreEqual("<Gamepad>/buttonNorth", Controls.Normalise("/XInputControllerWindows/buttonNorth", Controls.Device.Gamepad));
            Assert.AreEqual("<Keyboard>/r", Controls.Normalise("/Keyboard/r", Controls.Device.Keyboard));
            Assert.IsTrue(Controls.Rebind(_asset, "Thread", Controls.Device.Gamepad, "<DualShockGamepad>/buttonNorth"));
            Assert.AreEqual("<Gamepad>/buttonNorth", Pad("Thread"));
            Assert.AreEqual("<Gamepad>/rightTrigger", Pad("Survey"), "Survey had it: it takes the trigger");
        }

        [Test]
        public void RemapsAreKeptAndAResetPutsThemBack()
        {
            Controls.Rebind(_asset, "Bind", Controls.Device.Keyboard, "<Keyboard>/r");
            Assert.IsTrue(PlayerPrefs.HasKey(Controls.PrefsKey));

            var next = Fresh();
            try
            {
                Assert.AreEqual("<Keyboard>/e", Controls.PathOf(next, "Bind", Controls.Device.Keyboard), "the asset itself is unchanged");
                Controls.Load(next);
                Assert.AreEqual("<Keyboard>/r", Controls.PathOf(next, "Bind", Controls.Device.Keyboard), "the next session has it");

                Controls.ResetAll(next);
                Assert.AreEqual("<Keyboard>/e", Controls.PathOf(next, "Bind", Controls.Device.Keyboard));
                Assert.IsFalse(PlayerPrefs.HasKey(Controls.PrefsKey));
            }
            finally { Object.DestroyImmediate(next); }
        }

        [Test]
        public void PromptsNameTheKeyThePlayerHas()
        {
            Controls.Asset = _asset;
            Assert.AreEqual("Hold E", Controls.Prompt(Hold.Bind));
            Assert.AreEqual("Hold Q", Controls.Prompt(Hold.Survey));
            Assert.AreEqual("Hold Space", Controls.Prompt(Hold.Glide), "glide is held on Jump");
            Options.SetToggle(Hold.Bind, true);
            Assert.AreEqual("Press E", Controls.Prompt(Hold.Bind));
            Controls.Rebind(_asset, "Bind", Controls.Device.Keyboard, "<Keyboard>/r");
            Assert.AreEqual("Press R", Controls.Prompt(Hold.Bind));
            Controls.LastDevice = Controls.Device.Gamepad;
            StringAssert.StartsWith("Press ", Controls.Prompt(Hold.Bind));
            Assert.AreNotEqual("Press R", Controls.Prompt(Hold.Bind), "on a pad, the pad's button");
        }
    }
}
