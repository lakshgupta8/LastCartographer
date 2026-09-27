using UnityEngine;
using UnityEngine.InputSystem;

namespace OWSBG.Core
{
    /// <summary>
    /// Reads the "Player" action map of the WrenInput asset and exposes it as buffered
    /// <see cref="IWrenInput"/>. Buffer windows are from the combat doc (6 frames jump/attack, 4 dash).
    /// </summary>
    public sealed class InputReader : MonoBehaviour, IWrenInput
    {
        [SerializeField] InputActionAsset _asset;
        [SerializeField] int _jumpBufferFrames = 6;
        [SerializeField] int _attackBufferFrames = 6;
        [SerializeField] int _dashBufferFrames = 4;
        [SerializeField] int _flourishBufferFrames = 6;
        [SerializeField] int _instrumentBufferFrames = 6;

        InputAction _move, _jump, _attack, _dash, _bind, _survey, _flourish, _instrument, _cycle;
        ButtonBuffer _jumpBuf, _attackBuf, _dashBuf, _flourishBuf, _instrumentBuf, _cycleBuf;

        public InputActionAsset Asset
        {
            get => _asset;
            set { _asset = value; Bind(); }
        }

        public Vector2 Move => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpHeld => _jump != null && _jump.IsPressed();
        public bool BindHeld => _bind != null && _bind.IsPressed();
        public bool SurveyHeld => _survey != null && _survey.IsPressed();

        public bool ConsumeJump() => _jumpBuf.Consume();
        public bool ConsumeDash() => _dashBuf.Consume();
        public bool ConsumeAttack() => _attackBuf.Consume();
        public bool ConsumeFlourish() => _flourishBuf.Consume();
        public bool ConsumeInstrument() => _instrumentBuf.Consume();
        public bool ConsumeCycleInstrument() => _cycleBuf.Consume();

        void Awake()
        {
            _jumpBuf = new ButtonBuffer(_jumpBufferFrames);
            _attackBuf = new ButtonBuffer(_attackBufferFrames);
            _dashBuf = new ButtonBuffer(_dashBufferFrames);
            _flourishBuf = new ButtonBuffer(_flourishBufferFrames);
            _instrumentBuf = new ButtonBuffer(_instrumentBufferFrames);
            _cycleBuf = new ButtonBuffer(_instrumentBufferFrames);
            Bind();
        }

        void Bind()
        {
            Unbind();
            if (_asset == null) return;
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
            if (_jump != null) _jump.performed += OnJump;
            if (_attack != null) _attack.performed += OnAttack;
            if (_dash != null) _dash.performed += OnDash;
            if (_flourish != null) _flourish.performed += OnFlourish;
            if (_instrument != null) _instrument.performed += OnInstrument;
            if (_cycle != null) _cycle.performed += OnCycle;
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
            _move = _jump = _attack = _dash = _bind = _survey = _flourish = _instrument = _cycle = null;
        }

        void OnEnable() { _asset?.FindActionMap("Player", false)?.Enable(); }
        void OnDisable() { _asset?.FindActionMap("Player", false)?.Disable(); }
        void OnDestroy() { Unbind(); }

        void OnJump(InputAction.CallbackContext _) => _jumpBuf.Press();
        void OnAttack(InputAction.CallbackContext _) => _attackBuf.Press();
        void OnDash(InputAction.CallbackContext _) => _dashBuf.Press();
        void OnFlourish(InputAction.CallbackContext _) => _flourishBuf.Press();
        void OnInstrument(InputAction.CallbackContext _) => _instrumentBuf.Press();
        void OnCycle(InputAction.CallbackContext _) => _cycleBuf.Press();

        public void Tick()
        {
            _jumpBuf.Tick();
            _attackBuf.Tick();
            _dashBuf.Tick();
            _flourishBuf.Tick();
            _instrumentBuf.Tick();
            _cycleBuf.Tick();
        }
    }
}
