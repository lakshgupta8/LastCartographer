using System;
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
        bool ConsumeFlourish();
        /// <summary>Use the selected Instrument.</summary>
        bool ConsumeInstrument();
        /// <summary>Select the next Instrument slot.</summary>
        bool ConsumeCycleInstrument();
        /// <summary>Inkthread: throw the thread at the nearest anchor ahead.</summary>
        bool ConsumeThread();
        /// <summary>Age the buffers by one fixed frame. Called once per FixedUpdate by the controller.</summary>
        void Tick();
    }

    /// <summary>A press that stays valid for a window of fixed frames.</summary>
    public sealed class ButtonBuffer
    {
        readonly int _window;
        int _framesLeft;

        /// <summary>What the press is ("Jump"); the feel-test's recorder (PRO-03) counts by it. Null buffers say nothing.</summary>
        public string Name { get; }
        public int Window => _window;

        /// <summary>A named press went in.</summary>
        public static event Action<string> Pressed;
        /// <summary>A named press was acted on, this many frames after it went in (0 is the same frame).</summary>
        public static event Action<string, int> Consumed;
        /// <summary>A named press ran out of its window with nothing done: from the player's side, a press that did nothing.</summary>
        public static event Action<string> Dropped;

        public ButtonBuffer(int windowFrames) { _window = windowFrames; }
        public ButtonBuffer(int windowFrames, string name) { _window = windowFrames; Name = name; }

        public bool Pending => _framesLeft > 0;

        public void Press()
        {
            _framesLeft = _window;
            if (Name != null) Pressed?.Invoke(Name);
        }

        public bool Consume()
        {
            if (_framesLeft <= 0) return false;
            int waited = _window - _framesLeft;
            _framesLeft = 0;
            if (Name != null) Consumed?.Invoke(Name, waited);
            return true;
        }

        public void Tick()
        {
            if (_framesLeft <= 0) return;
            _framesLeft--;
            if (_framesLeft == 0 && Name != null) Dropped?.Invoke(Name);
        }
    }
}
