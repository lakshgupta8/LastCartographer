using OWSBG.Core;
using OWSBG.Narrative;
using OWSBG.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// The options page (DES-14): Esc or Start, whenever no other page or conversation has the screen, stops the world
    /// and opens it. Up/down picks a row, left/right changes it, J / Space / Enter / South activates, Esc / East goes
    /// back. Rows: language, hitstop, shake, captions, high-contrast ink, hold or toggle for Bind, Survey and Glide,
    /// then the controls page (every action's key and button; activating one listens for the next press, and the last
    /// row puts them all back) and Resume. Everything is saved as it changes.
    /// </summary>
    public sealed class OptionsView : MonoBehaviour
    {
        public enum Page { Closed, Options, Controls }
        public enum Item { Language, Hitstop, Shake, Captions, Contrast, ToggleBind, ToggleSurvey, ToggleGlide, VolumeMaster, VolumeMusic, VolumeSound, VolumeVoices, Controls, Resume }
        public const int ItemCount = 14;

        public static OptionsView Instance { get; private set; }

        public Page Current { get; private set; }
        public bool IsOpen => Current != Page.Closed;
        public int Row { get; private set; }
        /// <summary>The controls page's column: the keyboard's keys, or the pad's buttons.</summary>
        public Controls.Device Column { get; private set; }
        /// <summary>Waiting for the key to put an action on.</summary>
        public bool IsListening => _listen != null;
        public int RowCount => Current == Page.Controls ? Controls.Rebindable.Length + 1 : ItemCount;
        /// <summary>The reset row on the controls page.</summary>
        public int ResetRow => Controls.Rebindable.Length;
        public VisualElement Panel => _panel;

        WrenController _wren;
        bool _wasFrozen, _built, _blockedLastFrame = true;
        int _listenEndedFrame = -1;
        VisualElement _panel, _rows;
        Label _title, _hint, _version;
        InputActionRebindingExtensions.RebindingOperation _listen;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; CancelListen(); if (IsOpen) Pause.End(); }
        void OnEnable() { Loc.Changed += OnLocale; Options.Changed += Refresh; Controls.Changed += Refresh; }
        void OnDisable() { Loc.Changed -= OnLocale; Options.Changed -= Refresh; Controls.Changed -= Refresh; }
        void OnLocale(string _) => Refresh();

        /// <summary>Another page, a conversation or a cutscene has the screen.</summary>
        public static bool Blocked()
        {
            if (DialogueService.Instance != null && DialogueService.Instance.IsRunning) return true;
            if (AtlasView.Instance != null && AtlasView.Instance.IsOpen) return true;
            var wren = WrenController.Current;   // asked every frame: no scene search
            return wren != null && wren.Frozen;
        }

        public void Open()
        {
            if (IsOpen) return;
            _wren = FindFirstObjectByType<WrenController>();
            if (_wren != null) { _wasFrozen = _wren.Frozen; _wren.Frozen = true; }
            Pause.Begin();
            Current = Page.Options;
            Row = 0;
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            CancelListen();
            Current = Page.Closed;
            if (_wren != null) _wren.Frozen = _wasFrozen;
            _wren = null;
            Pause.End();
            if (_built) InkTheme.Show(_panel, false);
        }

        public void MoveRow(int delta)
        {
            if (!IsOpen || IsListening) return;
            Row = (Row + delta + RowCount) % RowCount;
            Refresh();
        }

        /// <summary>Left (-1) or right (+1) on the current row.</summary>
        public void Adjust(int dir)
        {
            if (!IsOpen || IsListening) return;
            if (Current == Page.Controls)
            {
                Column = Column == Controls.Device.Keyboard ? Controls.Device.Gamepad : Controls.Device.Keyboard;
                Refresh();
                return;
            }
            switch ((Item)Row)
            {
                case Item.Language:
                {
                    var list = Loc.Choosable;
                    int i = Mathf.Max(0, IndexOf(list, Loc.Locale));
                    Loc.SetLocale(list[(i + dir + list.Count) % list.Count]);
                    break;
                }
                case Item.Hitstop: Options.Hitstop = Mathf.Clamp01(Options.Hitstop + dir * Options.Step); break;
                case Item.Shake: Options.Shake = Mathf.Clamp01(Options.Shake + dir * Options.Step); break;
                case Item.Captions: Options.Captions = (CaptionTime)(((int)Options.Captions + dir + 4) % 4); break;
                case Item.Contrast: Options.HighContrast = !Options.HighContrast; break;
                case Item.ToggleBind: Options.SetToggle(Hold.Bind, !Options.IsToggle(Hold.Bind)); break;
                case Item.ToggleSurvey: Options.SetToggle(Hold.Survey, !Options.IsToggle(Hold.Survey)); break;
                case Item.ToggleGlide: Options.SetToggle(Hold.Glide, !Options.IsToggle(Hold.Glide)); break;
                case Item.VolumeMaster: Options.Set(Options.Volume.Master, Options.Get(Options.Volume.Master) + dir * Options.VolumeStep); break;
                case Item.VolumeMusic: Options.Set(Options.Volume.Music, Options.Get(Options.Volume.Music) + dir * Options.VolumeStep); break;
                case Item.VolumeSound: Options.Set(Options.Volume.Sound, Options.Get(Options.Volume.Sound) + dir * Options.VolumeStep); break;
                case Item.VolumeVoices: Options.Set(Options.Volume.Voices, Options.Get(Options.Volume.Voices) + dir * Options.VolumeStep); break;
            }
            Refresh();
        }

        /// <summary>Confirm on the current row: a switch flips, Controls opens its page, Resume closes, an action listens.</summary>
        public void Activate()
        {
            if (!IsOpen || IsListening) return;
            if (Current == Page.Controls)
            {
                if (Row == ResetRow) { Controls.ResetAll(Controls.Asset); Refresh(); return; }
                Listen(Controls.Rebindable[Row], Column);
                return;
            }
            switch ((Item)Row)
            {
                case Item.Controls: Current = Page.Controls; Row = 0; Refresh(); break;
                case Item.Resume: Close(); break;
                default: Adjust(+1); break;
            }
        }

        /// <summary>Esc / East: out of the controls page, else out of the options.</summary>
        public void Back()
        {
            if (!IsOpen) return;
            if (IsListening) { CancelListen(); Refresh(); return; }
            if (Current == Page.Controls) { Current = Page.Options; Row = (int)Item.Controls; Refresh(); return; }
            Close();
        }

        public void Listen(string action, Controls.Device device)
        {
            if (Controls.Asset == null) return;
            CancelListen();
            _listen = Controls.Listen(Controls.Asset, action, device, _ => { _listen = null; _listenEndedFrame = Time.frameCount; Refresh(); });
            Refresh();
        }

        void CancelListen()
        {
            if (_listen == null) return;
            var op = _listen;
            _listen = null;
            op.Cancel();
        }

        void Update()
        {
            if (!_built && Build() && IsOpen) Refresh();
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool esc = k != null && k.escapeKey.wasPressedThisFrame;
            bool start = g != null && g.startButton.wasPressedThisFrame;
            if (!IsOpen)
            {
                if ((esc || start) && !_blockedLastFrame && !Blocked()) Open();
                return;
            }
            // The rebinding owns the keys while it listens, and the press that ended it is its own (Esc, or the new key).
            if (IsListening || Time.frameCount == _listenEndedFrame) return;
            bool up = (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && (g.dpad.up.wasPressedThisFrame || g.leftStick.up.wasPressedThisFrame));
            bool down = (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && (g.dpad.down.wasPressedThisFrame || g.leftStick.down.wasPressedThisFrame));
            bool left = (k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame)) || (g != null && (g.dpad.left.wasPressedThisFrame || g.leftStick.left.wasPressedThisFrame));
            bool right = (k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)) || (g != null && (g.dpad.right.wasPressedThisFrame || g.leftStick.right.wasPressedThisFrame));
            bool confirm = (k != null && (k.jKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) || (g != null && g.buttonSouth.wasPressedThisFrame);
            bool back = esc || (g != null && g.buttonEast.wasPressedThisFrame);
            if (start && Current == Page.Options) { Close(); return; }
            if (back) { Back(); return; }
            if (up) MoveRow(-1);
            if (down) MoveRow(+1);
            if (left) Adjust(-1);
            if (right) Adjust(+1);
            if (confirm) Activate();
        }

        void LateUpdate() { _blockedLastFrame = IsOpen || Blocked(); }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.OptionsLayer == null) return false;
            var layer = ui.OptionsLayer;
            _panel = InkTheme.Panel("options-page");
            _panel.style.position = Position.Absolute;
            _panel.style.left = new Length(50, LengthUnit.Percent);
            _panel.style.top = new Length(50, LengthUnit.Percent);
            _panel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            _panel.style.width = 760;
            _title = InkTheme.Text("title", "", 30, InkTheme.Ink, FontStyle.Bold);
            _title.style.marginBottom = 12;
            _rows = new VisualElement { name = "rows", pickingMode = PickingMode.Ignore };
            _hint = InkTheme.Text("hint", "", 18, InkTheme.Dim, FontStyle.Italic);
            _hint.style.marginTop = 14;
            _version = InkTheme.Text("version", "", 13, InkTheme.Dim);
            _version.style.marginTop = 6;
            _version.style.unityTextAlign = TextAnchor.MiddleRight;
            _panel.Add(_title); _panel.Add(_rows); _panel.Add(_hint); _panel.Add(_version);
            InkTheme.Show(_panel, false);
            layer.Add(_panel);
            _built = true;
            return true;
        }

        public void Refresh()
        {
            if (!_built) return;
            InkTheme.Show(_panel, IsOpen);
            if (!IsOpen) return;
            _version.text = Loc.F("options.version", "version {0}", BuildInfo.Label)   // what a bug report quotes (PRG-25)
                          + "  ·  " + Loc.T("options.bug_key", "F12 saves a bug report");   // and how to make one (PRO-07)
            _rows.Clear();
            if (Current == Page.Controls)
            {
                _title.text = Loc.T("options.controls.title", "Controls");
                var head = MakeRow(-1, "", Loc.T("options.controls.keyboard", "Keyboard"), Loc.T("options.controls.gamepad", "Gamepad"), true);
                _rows.Add(head);
                var asset = Controls.Asset;
                for (int i = 0; i < Controls.Rebindable.Length; i++)
                {
                    string action = Controls.Rebindable[i];
                    bool listening = IsListening && i == Row;
                    string key = listening && Column == Controls.Device.Keyboard ? Loc.T("options.controls.listening", "press a key…") : Controls.DisplayName(asset, action, Controls.Device.Keyboard);
                    string pad = listening && Column == Controls.Device.Gamepad ? Loc.T("options.controls.listening_pad", "press a button…") : Controls.DisplayName(asset, action, Controls.Device.Gamepad);
                    _rows.Add(MakeRow(i, Controls.Label(action), key, pad, false, i == Row ? (int)Column : -1, verbatim: true));
                }
                _rows.Add(MakeRow(ResetRow, Loc.T("options.controls.reset", "Put every key back"), "", "", false));
                _hint.text = IsListening ? Loc.T("options.controls.hint_listening", "Esc to keep the old key. A key already in use swaps places.")
                                         : Loc.T("options.controls.hint", "Left and right choose keyboard or gamepad. Confirm, then press the new key.");
                return;
            }
            _title.text = Loc.T("options.title", "Options");
            for (int i = 0; i < ItemCount; i++) _rows.Add(MakeRow(i, Label((Item)i), Value((Item)i), "", Arrows((Item)i), verbatim: (Item)i == Item.Language));
            _hint.text = Loc.T("options.hint", "Up and down to choose, left and right to change. Esc to carry on.");
        }

        public static string Label(Item item) => item switch
        {
            Item.Language => Loc.T("options.language", "Language"),
            Item.Hitstop => Loc.T("options.hitstop", "Hitstop"),
            Item.Shake => Loc.T("options.shake", "Screen shake"),
            Item.Captions => Loc.T("options.captions", "Captions stay"),
            Item.Contrast => Loc.T("options.contrast", "High-contrast ink"),
            Item.ToggleBind => Loc.T("options.bind", "Bind"),
            Item.ToggleSurvey => Loc.T("options.survey", "Survey"),
            Item.ToggleGlide => Loc.T("options.glide", "Glide"),
            Item.VolumeMaster => Loc.T("options.volume.master", "Volume"),
            Item.VolumeMusic => Loc.T("options.volume.music", "Music"),
            Item.VolumeSound => Loc.T("options.volume.sound", "Sounds"),
            Item.VolumeVoices => Loc.T("options.volume.voices", "Voices"),
            Item.Controls => Loc.T("options.controls", "Controls"),
            _ => Loc.T("options.resume", "Resume"),
        };

        public static string Value(Item item) => item switch
        {
            Item.Language => Loc.NativeName(Loc.Locale),
            Item.Hitstop => Percent(Options.Hitstop),
            Item.Shake => Percent(Options.Shake),
            Item.Captions => Options.Captions switch
            {
                CaptionTime.Double => Loc.T("options.captions.double", "twice as long"),
                CaptionTime.Triple => Loc.T("options.captions.triple", "three times as long"),
                CaptionTime.UntilDismissed => Loc.T("options.captions.dismiss", "until dismissed"),
                _ => Loc.T("options.captions.normal", "as written"),
            },
            Item.Contrast => Options.HighContrast ? Loc.T("options.on", "on") : Loc.T("options.off", "off"),
            Item.ToggleBind => HoldValue(Hold.Bind),
            Item.ToggleSurvey => HoldValue(Hold.Survey),
            Item.ToggleGlide => HoldValue(Hold.Glide),
            Item.VolumeMaster => Percent(Options.Get(Options.Volume.Master)),
            Item.VolumeMusic => Percent(Options.Get(Options.Volume.Music)),
            Item.VolumeSound => Percent(Options.Get(Options.Volume.Sound)),
            Item.VolumeVoices => Percent(Options.Get(Options.Volume.Voices)),
            Item.Controls => "›",
            _ => "",
        };

        static bool Arrows(Item item) => item != Item.Controls && item != Item.Resume;

        static string Percent(float v) => v <= 0f ? Loc.T("options.off", "off") : Loc.F("options.percent", "{0}%", Mathf.RoundToInt(v * 100f));

        static string HoldValue(Hold h) => Options.IsToggle(h) ? Loc.T("options.toggle", "press to start, press to stop") : Loc.T("options.hold", "hold");

        static int IndexOf(System.Collections.Generic.IReadOnlyList<string> list, string s)
        {
            for (int i = 0; i < list.Count; i++) if (list[i] == s) return i;
            return -1;
        }

        VisualElement MakeRow(int index, string label, string value, string extra, bool arrows, int column = -1, bool verbatim = false)
        {
            bool sel = index == Row;
            var row = InkTheme.Row("row-" + index);
            row.AddToClassList("options-row");
            row.EnableInClassList("selected", sel);
            InkTheme.SetPadding(row, 6f, 10f);
            InkTheme.SetRadius(row, 4f);
            row.style.backgroundColor = sel ? InkTheme.PaperDark : new Color(0f, 0f, 0f, 0f);
            var marker = InkTheme.Text("marker", sel ? "▸" : "", 22, InkTheme.Wash);
            marker.style.width = 26;
            var l = InkTheme.Text("label", label, 22, InkTheme.Dim);
            l.style.width = 260;
            var v = InkTheme.Text("value", arrows && sel ? "◂  " + value + "  ▸" : value, 22, column == 0 ? InkTheme.Wash : InkTheme.Ink, column == 0 ? FontStyle.Bold : FontStyle.Normal);
            v.style.flexGrow = 1;
            v.style.flexBasis = 0;
            if (verbatim) InkTheme.Verbatim(v);   // a language in its own words, a key by its name
            row.Add(marker); row.Add(l); row.Add(v);
            if (Current == Page.Controls)
            {
                var x = InkTheme.Text("extra", extra, 22, column == 1 ? InkTheme.Wash : InkTheme.Ink, column == 1 ? FontStyle.Bold : FontStyle.Normal);
                x.style.flexGrow = 1;
                x.style.flexBasis = 0;
                if (verbatim) InkTheme.Verbatim(x);
                row.Add(x);
            }
            return row;
        }
    }
}
