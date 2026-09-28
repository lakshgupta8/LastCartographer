using UnityEngine;
using UnityEngine.InputSystem;

namespace OWSBG.Core
{
    /// <summary>
    /// Reads the "Player" action map of the WrenInput asset and exposes it as buffered
    /// <see cref="IWrenInput"/>. Buffer windows are from the combat doc (6 frames jump/attack, 4 dash).
    /// The player's remapped keys are laid over the asset when it binds (<see cref="Controls"/>), and Bind and Survey
    /// read as held while latched when the player has chosen to toggle them (DES-14): a press starts, the next press
    /// or the end of what it was doing (<see cref="Controls.Release"/>) stops.
    /// </summary>
    public sealed class InputReader : MonoBehaviour, IWrenInput
    {
        [SerializeField] InputActionAsset _asset;
        [SerializeField] int _jumpBufferFrames = 6;
        [SerializeField] int _attackBufferFrames = 6;
        [SerializeField] int _dashBufferFrames = 4;
        [SerializeField] int _flourishBufferFrames = 6;
        [SerializeField] int _instrumentBufferFrames = 6;

        InputAction _move, _jump, _attack, _dash, _bind, _survey, _flourish, _instrument, _cycle, _thread;
        bool _bindLatch, _surveyLatch;
        ButtonBuffer _jumpBuf, _attackBuf, _dashBuf, _flourishBuf, _instrumentBuf, _cycleBuf, _threadBuf;

        public InputActionAsset Asset
        {
            get => _asset;
            set { _asset = value; Bind(); }
        }

        public Vector2 Move => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpHeld => _jump != null && _jump.IsPressed();
        public bool BindHeld => Options.IsToggle(Hold.Bind) ? _bindLatch : _bind != null && _bind.IsPressed();
        public bool SurveyHeld => Options.IsToggle(Hold.Survey) ? _surveyLatch : _survey != null && _survey.IsPressed();

        public bool ConsumeJump() => _jumpBuf.Consume();
        public bool ConsumeDash() => _dashBuf.Consume();
        public bool ConsumeAttack() => _attackBuf.Consume();
        public bool ConsumeFlourish() => _flourishBuf.Consume();
        public bool ConsumeInstrument() => _instrumentBuf.Consume();
        public bool ConsumeCycleInstrument() => _cycleBuf.Consume();
        public bool ConsumeThread() => _threadBuf.Consume();

        void Awake()
        {
            _jumpBuf = new ButtonBuffer(_jumpBufferFrames);
            _attackBuf = new ButtonBuffer(_attackBufferFrames);
            _dashBuf = new ButtonBuffer(_dashBufferFrames);
            _flourishBuf = new ButtonBuffer(_flourishBufferFrames);
            _instrumentBuf = new ButtonBuffer(_instrumentBufferFrames);
            _cycleBuf = new ButtonBuffer(_instrumentBufferFrames);
            _threadBuf = new ButtonBuffer(_dashBufferFrames);
            Bind();
        }

        void Bind()
        {
            Unbind();
            if (_asset == null) return;
            Controls.Load(_asset);
            Controls.Asset = _asset;
            var map = _asset.FindActionMap("Player", throwIfNotFound: false);
            if (map == null) { Debug.LogError("[OWSBG] WrenInput asset has no 'Player' map"); return; }
            _move = map.FindAction("Move");
            _jump = map.FindAction("Jump");
            _attack = map.FindAction("Attack");
            _dash = map.FindAction("Dash");
            _bind = map.FindAction("Bind");
            _survey = map.FindAction("Survey");
            _flourish = map.FindAction("Flourish");
            _instrument = map.FindAction("Instrument");
            _cycle = map.FindAction("CycleInstrument");
            _thread = map.FindAction("Thread");
            if (_jump != null) _jump.performed += OnJump;
            if (_attack != null) _attack.performed += OnAttack;
            if (_dash != null) _dash.performed += OnDash;
            if (_flourish != null) _flourish.performed += OnFlourish;
            if (_instrument != null) _instrument.performed += OnInstrument;
            if (_cycle != null) _cycle.performed += OnCycle;
            if (_thread != null) _thread.performed += OnThread;
            if (_bind != null) _bind.performed += OnBind;
            if (_survey != null) _survey.performed += OnSurvey;
            map.actionTriggered += OnAny;
            if (isActiveAndEnabled) map.Enable();
        }

        void Unbind()
        {
            if (_jump != null) _jump.performed -= OnJump;
            if (_attack != null) _attack.performed -= OnAttack;
            if (_dash != null) _dash.performed -= OnDash;
            if (_flourish != null) _flourish.performed -= OnFlourish;
            if (_instrument != null) _instrument.performed -= OnInstrument;
            if (_cycle != null) _cycle.performed -= OnCycle;
            if (_thread != null) _thread.performed -= OnThread;
            if (_bind != null) _bind.performed -= OnBind;
            if (_survey != null) _survey.performed -= OnSurvey;
            var map = _move != null ? _move.actionMap : null;
            if (map != null) map.actionTriggered -= OnAny;
            _move = _jump = _attack = _dash = _bind = _survey = _flourish = _instrument = _cycle = _thread = null;
        }

        void OnEnable()
        {
            _asset?.FindActionMap("Player", false)?.Enable();
            Controls.Released += OnReleased;
            Options.Changed += OnOptions;
        }

        void OnDisable()
        {
            _asset?.FindActionMap("Player", false)?.Disable();
            Controls.Released -= OnReleased;
            Options.Changed -= OnOptions;
        }

        void OnDestroy() { Unbind(); if (Controls.Asset == _asset) Controls.Asset = null; }

        void OnBind(InputAction.CallbackContext _) { if (Options.IsToggle(Hold.Bind)) _bindLatch = !_bindLatch; }
        void OnSurvey(InputAction.CallbackContext _) { if (Options.IsToggle(Hold.Survey)) _surveyLatch = !_surveyLatch; }

        void OnReleased(Hold hold)
        {
            if (hold == Hold.Bind) _bindLatch = false;
            if (hold == Hold.Survey) _surveyLatch = false;
        }

        void OnOptions() { if (!Options.IsToggle(Hold.Bind)) _bindLatch = false; if (!Options.IsToggle(Hold.Survey)) _surveyLatch = false; }

        static void OnAny(InputAction.CallbackContext ctx)
        {
            var device = ctx.control != null ? ctx.control.device : null;
            if (device is Keyboard) Controls.LastDevice = Controls.Device.Keyboard;
            else if (device is Gamepad) Controls.LastDevice = Controls.Device.Gamepad;
        }

        void OnJump(InputAction.CallbackContext _) => _jumpBuf.Press();
        void OnAttack(InputAction.CallbackContext _) => _attackBuf.Press();
        void OnDash(InputAction.CallbackContext _) => _dashBuf.Press();
        void OnFlourish(InputAction.CallbackContext _) => _flourishBuf.Press();
        void OnInstrument(InputAction.CallbackContext _) => _instrumentBuf.Press();
        void OnCycle(InputAction.CallbackContext _) => _cycleBuf.Press();
        void OnThread(InputAction.CallbackContext _) => _threadBuf.Press();

        public void Tick()
        {
            _jumpBuf.Tick();
            _attackBuf.Tick();
            _dashBuf.Tick();
            _flourishBuf.Tick();
            _instrumentBuf.Tick();
            _cycleBuf.Tick();
            _threadBuf.Tick();
        }
    }
}
