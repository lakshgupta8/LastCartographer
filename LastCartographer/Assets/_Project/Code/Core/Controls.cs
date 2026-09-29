using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OWSBG.Core
{
    /// <summary>
    /// Remapping (DES-14): every press Wren makes can be moved to another key or button, one per device. The overrides
    /// are kept in PlayerPrefs as the Input System's own JSON and laid over the WrenInput asset whenever a reader binds it.
    /// A key already used by another action swaps with it, so no action is ever left without one; the menu keys
    /// (Esc, M, Start, Select) and the movement keys can't be taken. Also where a toggled hold is let go
    /// (<see cref="Release"/>), and how the prompts name a key ("Hold E", "Press E").
    /// </summary>
    public static class Controls
    {
        public const string PrefsKey = "owsbg.bindings";
        public const string MapName = "Player";
        public enum Device { Keyboard, Gamepad }

        /// <summary>The actions the options page lists, in its order. Move stays on WASD, the arrows and the sticks.</summary>
        public static readonly string[] Rebindable = { "Jump", "Attack", "Dash", "Bind", "Survey", "Flourish", "Instrument", "CycleInstrument", "Thread" };

        /// <summary>Keys the menus read directly, which no action may take.</summary>
        public static readonly string[] Reserved = { "<Keyboard>/escape", "<Keyboard>/m", "<Gamepad>/start", "<Gamepad>/select" };

        /// <summary>Raised after the bindings change (a rebind, a swap, a reset).</summary>
        public static event Action Changed;
        /// <summary>A toggled hold should let go: the thing it was doing is done or can't go on.</summary>
        public static event Action<Hold> Released;

        /// <summary>The asset the game reads, once a reader has bound it; the options page remaps this one.</summary>
        public static InputActionAsset Asset { get; set; }
        /// <summary>The device the player last pressed something on; prompts name that device's buttons.</summary>
        public static Device LastDevice { get; set; }

        public static void Release(Hold hold) => Released?.Invoke(hold);

        /// <summary>The player's name for an action, in their language.</summary>
        public static string Label(string action) => action switch
        {
            "Jump" => Loc.T("controls.action.jump", "Jump"),
            "Attack" => Loc.T("controls.action.attack", "Strike"),
            "Dash" => Loc.T("controls.action.dash", "Dash"),
            "Bind" => Loc.T("controls.action.bind", "Bind"),
            "Survey" => Loc.T("controls.action.survey", "Survey"),
            "Flourish" => Loc.T("controls.action.flourish", "Flourish"),
            "Instrument" => Loc.T("controls.action.instrument", "Instrument"),
            "CycleInstrument" => Loc.T("controls.action.cycleinstrument", "Next Instrument"),
            "Thread" => Loc.T("controls.action.thread", "Inkthread"),
            _ => action,
        };

        /// <summary>Lay the saved overrides over <paramref name="asset"/> (and nothing else: overrides not saved are dropped).</summary>
        public static void Load(InputActionAsset asset)
        {
            if (asset == null) return;
            asset.RemoveAllBindingOverrides();
            var json = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(json)) asset.LoadBindingOverridesFromJson(json);
        }

        public static void Save(InputActionAsset asset)
        {
            if (asset == null) return;
            PlayerPrefs.SetString(PrefsKey, asset.SaveBindingOverridesAsJson());
            Changed?.Invoke();
        }

        /// <summary>Every action back to the asset's keys.</summary>
        public static void ResetAll(InputActionAsset asset)
        {
            if (asset == null) return;
            asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PrefsKey);
            Changed?.Invoke();
        }

        public static InputAction Find(InputActionAsset asset, string action) =>
            asset != null ? asset.FindActionMap(MapName, false)?.FindAction(action) : null;

        static string Prefix(Device d) => d == Device.Keyboard ? "<Keyboard>/" : "<Gamepad>/";

        /// <summary>Index of the action's first binding on the device (by the asset's own path), or -1.</summary>
        public static int BindingIndex(InputAction action, Device d)
        {
            if (action == null) return -1;
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isComposite || b.isPartOfComposite) continue;
                if (b.path != null && b.path.StartsWith(Prefix(d), StringComparison.Ordinal)) return i;
            }
            return -1;
        }

        /// <summary>The key or button the action is on now, as an Input System path ("&lt;Keyboard&gt;/space"), or null.</summary>
        public static string PathOf(InputActionAsset asset, string action, Device d)
        {
            var a = Find(asset, action);
            int i = BindingIndex(a, d);
            return i < 0 ? null : a.bindings[i].effectivePath;
        }

        /// <summary>The key or button as the player would say it ("Space", "E", "A").</summary>
        public static string DisplayName(InputActionAsset asset, string action, Device d)
        {
            var a = Find(asset, action);
            int i = BindingIndex(a, d);
            if (i < 0) return "-";
            var s = a.GetBindingDisplayString(i);
            if (!string.IsNullOrWhiteSpace(s)) return s;
            // No device of that kind is attached (a headless machine, a pad unplugged), so the layout had no name
            // to give: name the binding from its path instead ("<Keyboard>/space" is "Space").
            s = InputControlPath.ToHumanReadableString(a.bindings[i].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
            if (!string.IsNullOrWhiteSpace(s)) return s;
            string path = a.bindings[i].effectivePath ?? "";
            int slash = path.LastIndexOf('/');
            string control = slash >= 0 ? path.Substring(slash + 1) : path;
            return control.Length == 0 ? "-" : char.ToUpperInvariant(control[0]) + control.Substring(1);
        }

        /// <summary>The key for <paramref name="action"/> on the device the player is using.</summary>
        public static string KeyName(string action) => Asset != null ? DisplayName(Asset, action, LastDevice) : action;

        /// <summary>"Hold E" or "Press E", as the player has chosen for that hold.</summary>
        public static string Prompt(Hold hold)
        {
            string key = KeyName(hold == Hold.Glide ? "Jump" : hold.ToString());
            return Options.IsToggle(hold) ? Loc.F("controls.press", "Press {0}", key) : Loc.F("controls.hold", "Hold {0}", key);
        }

        /// <summary>
        /// A path from the rebinding operation, made generic for its device: a pad's layout ("&lt;XInputController&gt;")
        /// becomes "&lt;Gamepad&gt;" so the binding works on any pad, and a keyboard's stays "&lt;Keyboard&gt;".
        /// </summary>
        public static string Normalise(string path, Device d)
        {
            if (string.IsNullOrEmpty(path)) return path;
            int slash = path.IndexOf('/', path.StartsWith("/") ? 1 : 0);
            string control = slash >= 0 ? path.Substring(slash + 1) : path;
            return Prefix(d) + control;
        }

        public static bool IsReserved(string path)
        {
            foreach (var r in Reserved) if (string.Equals(r, path, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Put <paramref name="action"/> on <paramref name="path"/> for the device. Whatever else was on that key takes
        /// the action's old key. Refuses a reserved key, a movement key, or another device's path. Saved on success.
        /// </summary>
        public static bool Rebind(InputActionAsset asset, string action, Device d, string path)
        {
            var a = Find(asset, action);
            int i = BindingIndex(a, d);
            if (i < 0 || string.IsNullOrEmpty(path)) return false;
            path = Normalise(path, d);
            if (IsReserved(path) || IsMovement(asset, path)) return false;
            string old = a.bindings[i].effectivePath;
            if (string.Equals(old, path, StringComparison.OrdinalIgnoreCase)) return true;

            // Swap anything already there, this action's other keys included.
            var swaps = new List<(InputAction, int)>();
            foreach (var name in Rebindable)
            {
                var other = Find(asset, name);
                if (other == null) continue;
                var bs = other.bindings;
                for (int j = 0; j < bs.Count; j++)
                {
                    if (other == a && j == i) continue;
                    if (bs[j].isComposite || bs[j].isPartOfComposite) continue;
                    if (string.Equals(bs[j].effectivePath, path, StringComparison.OrdinalIgnoreCase)) swaps.Add((other, j));
                }
            }
            a.ApplyBindingOverride(i, path);
            foreach (var (other, j) in swaps) other.ApplyBindingOverride(j, old);
            Save(asset);
            return true;
        }

        static bool IsMovement(InputActionAsset asset, string path)
        {
            var move = Find(asset, "Move");
            if (move == null) return false;
            foreach (var b in move.bindings)
            {
                if (b.isComposite || string.IsNullOrEmpty(b.effectivePath)) continue;
                // A whole stick or d-pad is movement, and so is each of its directions.
                if (string.Equals(b.effectivePath, path, StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith(b.effectivePath + "/", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>
        /// Wait for the next key (or button) on the device and put the action there. Esc cancels. The action map is
        /// switched off while it listens so the press isn't also a jump. <paramref name="done"/> gets whether it changed.
        /// </summary>
        public static InputActionRebindingExtensions.RebindingOperation Listen(InputActionAsset asset, string action, Device d, Action<bool> done)
        {
            var a = Find(asset, action);
            int i = BindingIndex(a, d);
            if (i < 0) { done?.Invoke(false); return null; }
            var map = a.actionMap;
            bool wasEnabled = map.enabled;
            map.Disable();
            bool changed = false;
            InputActionRebindingExtensions.RebindingOperation op = null;
            void Finish(bool ok)
            {
                op?.Dispose();
                if (wasEnabled) map.Enable();
                done?.Invoke(ok && changed);
            }
            op = a.PerformInteractiveRebinding(i)
                .WithControlsHavingToMatchPath(d == Device.Keyboard ? "<Keyboard>" : "<Gamepad>")
                .WithControlsExcluding("<Keyboard>/anyKey")
                .WithControlsExcluding("<Mouse>")
                .WithCancelingThrough("<Keyboard>/escape")
                .WithExpectedControlType("Button")
                .OnMatchWaitForAnother(0.05f)
                .OnPotentialMatch(o => { if (IsReserved(Normalise(o.selectedControl.path, d))) o.RemoveCandidate(o.selectedControl); })
                .OnApplyBinding((o, path) => changed = Rebind(asset, action, d, path))
                .OnComplete(_ => Finish(true))
                .OnCancel(_ => Finish(false));
            return op.Start();
        }
    }
}
