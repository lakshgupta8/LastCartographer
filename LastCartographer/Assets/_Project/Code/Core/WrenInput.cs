using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>
    /// What the controller reads each fixed frame. Presses are buffered (combat doc 1):
    /// a press stays "pending" for a few frames and is consumed by whoever can act on it.
    /// Implemented by <see cref="InputReader"/> for real devices and by scripted inputs in tests.
    /// </summary>
    public interface IWrenInput
    {
        Vector2 Move { get; }
        bool JumpHeld { get; }
        bool BindHeld { get; }
        bool SurveyHeld { get; }
        /// <summary>Consume a buffered jump press, if any.</summary>
        bool ConsumeJump();
        bool ConsumeDash();
        bool ConsumeAttack();
        /// <summary>Age the buffers by one fixed frame. Called once per FixedUpdate by the controller.</summary>
        void Tick();
    }

    /// <summary>A press that stays valid for a window of fixed frames.</summary>
    public sealed class ButtonBuffer
    {
        readonly int _window;
        int _framesLeft;

        public ButtonBuffer(int windowFrames) { _window = windowFrames; }

        public bool Pending => _framesLeft > 0;
        public void Press() => _framesLeft = _window;

        public bool Consume()
        {
            if (_framesLeft <= 0) return false;
            _framesLeft = 0;
            return true;
        }

        public void Tick()
        {
            if (_framesLeft > 0) _framesLeft--;
        }
    }
}
