using System;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>The three presses that are held to act. Each can be held (the default) or toggled (DES-14).</summary>
    public enum Hold { Bind, Survey, Glide }

    /// <summary>How long world captions, tutorial prompts and toasts stay (DES-14: no text leaves before it is read).</summary>
    public enum CaptionTime { Normal, Double, Triple, UntilDismissed }

    /// <summary>
    /// The player's accessibility and comfort settings (DES-14), kept in PlayerPrefs beside the language (PRG-19): they
    /// belong to the player, not to a save. Hitstop and shake are scales from 0 (off) to 1 (as designed), in quarters;
    /// captions can stay two or three times as long, or until dismissed; the ink can be drawn in high contrast; Bind,
    /// Survey and Glide can each be toggled instead of held. <see cref="Changed"/> fires once per change.
    /// </summary>
    public static class Options
    {
        public const string Prefix = "owsbg.opt.";
        public const float Step = 0.25f;
        /// <summary>Global shader float: 1 when the world is drawn in high contrast (PaperGrain).</summary>
        public static readonly int ContrastId = Shader.PropertyToID("_OWSBG_Contrast");

        public static event Action Changed;

        static bool _loaded;
        static float _hitstop = 1f, _shake = 1f;
        static CaptionTime _captions;
        static bool _highContrast;
        static readonly bool[] _toggle = new bool[3];

        static void Ensure() { if (!_loaded) Load(); }

        /// <summary>Read everything from PlayerPrefs (defaults where nothing is saved) and push the shader global.</summary>
        public static void Load()
        {
            _loaded = true;
            _hitstop = Quarter(PlayerPrefs.GetFloat(Prefix + "hitstop", 1f));
            _shake = Quarter(PlayerPrefs.GetFloat(Prefix + "shake", 1f));
            _captions = (CaptionTime)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "captions", 0), 0, 3);
            _highContrast = PlayerPrefs.GetInt(Prefix + "contrast", 0) != 0;
            for (int i = 0; i < _toggle.Length; i++) _toggle[i] = PlayerPrefs.GetInt(Prefix + "toggle." + (Hold)i, 0) != 0;
            ApplyGlobals();
        }

        /// <summary>Every option back to its default, saved; tests use it to leave nothing behind.</summary>
        public static void ResetToDefaults()
        {
            foreach (var k in Keys) PlayerPrefs.DeleteKey(k);
            Load();
            Changed?.Invoke();
        }

        public static string[] Keys => new[]
        {
            Prefix + "hitstop", Prefix + "shake", Prefix + "captions", Prefix + "contrast",
            Prefix + "toggle.Bind", Prefix + "toggle.Survey", Prefix + "toggle.Glide",
        };

        /// <summary>How much of each hitstop is kept: 0 is none, 1 is the combat doc's frames.</summary>
        public static float Hitstop
        {
            get { Ensure(); return _hitstop; }
            set { Ensure(); Set(ref _hitstop, Quarter(value), "hitstop"); }
        }

        /// <summary>How much of each camera shake is kept: 0 is a still camera.</summary>
        public static float Shake
        {
            get { Ensure(); return _shake; }
            set { Ensure(); Set(ref _shake, Quarter(value), "shake"); }
        }

        public static CaptionTime Captions
        {
            get { Ensure(); return _captions; }
            set
            {
                Ensure();
                if (_captions == value) return;
                _captions = value;
                PlayerPrefs.SetInt(Prefix + "captions", (int)value);
                Changed?.Invoke();
            }
        }

        public static bool HighContrast
        {
            get { Ensure(); return _highContrast; }
            set
            {
                Ensure();
                if (_highContrast == value) return;
                _highContrast = value;
                PlayerPrefs.SetInt(Prefix + "contrast", value ? 1 : 0);
                ApplyGlobals();
                Changed?.Invoke();
            }
        }

        public static bool IsToggle(Hold hold) { Ensure(); return _toggle[(int)hold]; }

        public static void SetToggle(Hold hold, bool toggle)
        {
            Ensure();
            if (_toggle[(int)hold] == toggle) return;
            _toggle[(int)hold] = toggle;
            PlayerPrefs.SetInt(Prefix + "toggle." + hold, toggle ? 1 : 0);
            Changed?.Invoke();
        }

        /// <summary>Frames of hitstop after the scale, rounded but never below one while any is kept; at 0 there is none.</summary>
        public static int HitstopFrames(int frames) => frames <= 0 || Hitstop <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(frames * Hitstop));

        /// <summary>How long a caption meant for <paramref name="seconds"/> stays; infinite when it waits to be dismissed.</summary>
        public static float CaptionSeconds(float seconds) => Captions switch
        {
            CaptionTime.Double => seconds * 2f,
            CaptionTime.Triple => seconds * 3f,
            CaptionTime.UntilDismissed => float.PositiveInfinity,
            _ => seconds,
        };

        public static void ApplyGlobals() => Shader.SetGlobalFloat(ContrastId, _highContrast ? 1f : 0f);

        static float Quarter(float v) => Mathf.Clamp01(Mathf.Round(v / Step) * Step);

        static void Set(ref float field, float value, string key)
        {
            if (Mathf.Approximately(field, value)) return;
            field = value;
            PlayerPrefs.SetFloat(Prefix + key, value);
            Changed?.Invoke();
        }
    }
}
