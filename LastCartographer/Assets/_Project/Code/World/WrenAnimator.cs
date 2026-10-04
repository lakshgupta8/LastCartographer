using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Picks Wren's clip from what she is doing (CHR-03, CHR-04, docs/design/wren-animation.md). Priority, top first:
    /// dead, hurt, a swing (its frames follow the strike's own phase), a flourish (its own clip, following the
    /// flourish's frames), the thread (the cast, then the pull, then the catch at the anchor), a dash, the bind,
    /// a survey, the push off a wall, the cling (the hold, then the slide), the air (carried up, gliding, rising,
    /// falling), the landing, then run or idle. The events for the jump, the wall-jump, the landing, the dash, the
    /// thread's cast and catch, the hit and the death ask for a restart, so those clips always begin on their
    /// first frame; the clip itself changes only here, once a frame, after everything has moved.
    /// </summary>
    [RequireComponent(typeof(InkSheetPlayer))]
    public sealed class WrenAnimator : MonoBehaviour
    {
        public const float HurtSeconds = 0.25f, LandSeconds = 0.25f;
        /// <summary>How long the push off a wall shows before the rise takes over.</summary>
        public const float WallJumpSeconds = 0.15f;
        /// <summary>The thread's fling before the pull, and the hop at the anchor after it.</summary>
        public const float ThreadCastSeconds = 0.09f, ThreadCatchSeconds = 0.15f;

        InkSheetPlayer _sheet;
        WrenController _ctrl;
        QuillStrike _strike;
        Flourishes _flourish;
        WrenVitals _vitals;
        float _hurtLeft, _landLeft, _wallJumpLeft, _castLeft, _catchLeft;
        bool _dead;
        string _restart;

        public string Clip => _sheet != null ? _sheet.Current : null;

        void Awake()
        {
            _sheet = GetComponent<InkSheetPlayer>();
            _ctrl = GetComponent<WrenController>();
            _strike = GetComponent<QuillStrike>();
            _flourish = GetComponent<Flourishes>();
            _vitals = GetComponent<WrenVitals>();
        }

        void OnEnable()
        {
            if (_ctrl != null)
            {
                _ctrl.Jumped += OnJumped;
                _ctrl.WallJumped += OnWallJumped;
                _ctrl.Landed += OnLanded;
                _ctrl.Dashed += OnDashed;
                _ctrl.Threaded += OnThreaded;
                _ctrl.ThreadArrived += OnThreadArrived;
            }
            if (_vitals != null)
            {
                _vitals.Hurt += OnHurt;
                _vitals.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (_ctrl != null)
            {
                _ctrl.Jumped -= OnJumped;
                _ctrl.WallJumped -= OnWallJumped;
                _ctrl.Landed -= OnLanded;
                _ctrl.Dashed -= OnDashed;
                _ctrl.Threaded -= OnThreaded;
                _ctrl.ThreadArrived -= OnThreadArrived;
            }
            if (_vitals != null)
            {
                _vitals.Hurt -= OnHurt;
                _vitals.Died -= OnDied;
            }
        }

        void OnJumped() => _restart = "jump";
        void OnWallJumped() { _wallJumpLeft = WallJumpSeconds; _restart = "walljump"; }
        void OnLanded() { _landLeft = LandSeconds; _wallJumpLeft = 0f; _catchLeft = 0f; _restart = "land"; }
        void OnDashed() { _catchLeft = 0f; _restart = "dash"; }
        void OnThreaded() { _castLeft = ThreadCastSeconds; _catchLeft = 0f; _restart = "thread_cast"; }
        void OnThreadArrived() { _catchLeft = ThreadCatchSeconds; _restart = "thread_catch"; }
        void OnHurt() { _hurtLeft = HurtSeconds; _restart = "hurt"; }
        void OnDied() { _dead = true; _restart = "death"; }

        /// <summary>A new life: the death clip lets go.</summary>
        public void Revive() { _dead = false; _hurtLeft = 0f; _landLeft = 0f; _wallJumpLeft = 0f; _castLeft = 0f; _catchLeft = 0f; _restart = null; }

        /// <summary>The clip a flourish plays: its own name, lower-case.</summary>
        public static string FlourishClip(FlourishKind kind) => kind switch
        {
            FlourishKind.Crosshatch => "crosshatch",
            FlourishKind.Longstroke => "longstroke",
            FlourishKind.Blot => "blot",
            _ => null,
        };

        void Show(string clip)
        {
            bool restart = _restart == clip;
            if (_sheet.Play(clip, restart) && restart) _restart = null;
        }

        void LateUpdate()
        {
            if (_sheet == null || _ctrl == null) return;
            float dt = Time.deltaTime;
            if (_dead && _vitals != null && !_vitals.IsDead) _dead = false;   // restored elsewhere
            if (_dead) { Show("death"); return; }
            if (_hurtLeft > 0f) { _hurtLeft -= dt; Show("hurt"); return; }
            if (_strike != null && _strike.IsBusy)
            {
                var d = _strike.Direction;
                string clip = d.y < 0f ? "pogo" : d.y > 0f ? "strike_up" : "strike" + (_strike.ComboIndex % 3 + 1);
                if (_sheet.Play(clip)) _sheet.Seek(_strike.Progress);
                return;
            }
            if (_flourish != null && _flourish.IsBusy)
            {
                // Its own clip, following the flourish's frames; a sheet without it keeps the rising slash (CHR-03).
                string clip = FlourishClip(_flourish.Current);
                if (clip != null && _sheet.Play(clip)) _sheet.Seek(_flourish.Progress);
                else Show("strike2");
                return;
            }
            if (_ctrl.IsThreading)
            {
                if (_castLeft > 0f && _sheet.Has("thread_cast")) { _castLeft -= dt; Show("thread_cast"); }
                else Show("thread");
                return;
            }
            _castLeft = 0f;
            if (_catchLeft > 0f && !_ctrl.IsGrounded && _sheet.Has("thread_catch")) { _catchLeft -= dt; Show("thread_catch"); return; }
            _catchLeft = 0f;
            if (_ctrl.IsDashing) { Show("dash"); return; }
            if (_vitals != null && _vitals.IsBinding) { Show("bind"); return; }
            if (VantagePoint.Surveying != null) { Show("survey"); return; }
            if (_wallJumpLeft > 0f && !_ctrl.IsGrounded && !_ctrl.IsClinging && _sheet.Has("walljump")) { _wallJumpLeft -= dt; Show("walljump"); return; }
            _wallJumpLeft = 0f;
            if (_ctrl.IsClinging) { Show(_ctrl.IsSliding && _sheet.Has("slide") ? "slide" : "cling"); return; }
            if (!_ctrl.IsGrounded)
            {
                _landLeft = 0f;
                if (_ctrl.IsLifted && _sheet.Has("glide_rise")) { Show("glide_rise"); return; }
                Show(_ctrl.IsGliding ? "glide" : _ctrl.Velocity.y > 0.5f ? "jump" : "fall");
                return;
            }
            if (_landLeft > 0f) { _landLeft -= dt; Show("land"); return; }
            Show(Mathf.Abs(_ctrl.Velocity.x) > 0.5f ? "run" : "idle");
        }
    }
}
