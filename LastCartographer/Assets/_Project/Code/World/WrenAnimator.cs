using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Picks Wren's clip from what she is doing (CHR-03, docs/design/wren-animation.md). Priority, top first:
    /// dead, hurt, a swing (its frames follow the strike's own phase), a flourish, the thread, a dash, the bind,
    /// a survey, a cling, the air (glide, rising, falling), the landing, then run or idle. The events for the
    /// jump, the landing, the dash, the hit and the death ask for a restart, so those clips always begin on
    /// their first frame; the clip itself changes only here, once a frame, after everything has moved.
    /// </summary>
    [RequireComponent(typeof(InkSheetPlayer))]
    public sealed class WrenAnimator : MonoBehaviour
    {
        public const float HurtSeconds = 0.25f, LandSeconds = 0.25f;

        InkSheetPlayer _sheet;
        WrenController _ctrl;
        QuillStrike _strike;
        Flourishes _flourish;
        WrenVitals _vitals;
        float _hurtLeft, _landLeft;
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
                _ctrl.Landed += OnLanded;
                _ctrl.Dashed += OnDashed;
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
                _ctrl.Landed -= OnLanded;
                _ctrl.Dashed -= OnDashed;
            }
            if (_vitals != null)
            {
                _vitals.Hurt -= OnHurt;
                _vitals.Died -= OnDied;
            }
        }

        void OnJumped() => _restart = "jump";
        void OnLanded() { _landLeft = LandSeconds; _restart = "land"; }
        void OnDashed() => _restart = "dash";
        void OnHurt() { _hurtLeft = HurtSeconds; _restart = "hurt"; }
        void OnDied() { _dead = true; _restart = "death"; }

        /// <summary>A new life: the death clip lets go.</summary>
        public void Revive() { _dead = false; _hurtLeft = 0f; _landLeft = 0f; _restart = null; }

        void Show(string clip)
        {
            bool restart = _restart == clip;
            if (_sheet.Play(clip, restart) && restart) _restart = null;
        }

        void LateUpdate()
        {
            if (_sheet == null || _ctrl == null) return;
            if (_dead && _vitals != null && !_vitals.IsDead) _dead = false;   // restored elsewhere
            if (_dead) { Show("death"); return; }
            if (_hurtLeft > 0f) { _hurtLeft -= Time.deltaTime; Show("hurt"); return; }
            if (_strike != null && _strike.IsBusy)
            {
                var d = _strike.Direction;
                string clip = d.y < 0f ? "pogo" : d.y > 0f ? "strike_up" : "strike" + (_strike.ComboIndex % 3 + 1);
                if (_sheet.Play(clip)) _sheet.Seek(_strike.Progress);
                return;
            }
            if (_flourish != null && _flourish.IsBusy) { Show("strike2"); return; }
            if (_ctrl.IsThreading) { Show("thread"); return; }
            if (_ctrl.IsDashing) { Show("dash"); return; }
            if (_vitals != null && _vitals.IsBinding) { Show("bind"); return; }
            if (VantagePoint.Surveying != null) { Show("survey"); return; }
            if (_ctrl.IsClinging) { Show("cling"); return; }
            if (!_ctrl.IsGrounded)
            {
                _landLeft = 0f;
                Show(_ctrl.IsGliding ? "glide" : _ctrl.Velocity.y > 0.5f ? "jump" : "fall");
                return;
            }
            if (_landLeft > 0f) { _landLeft -= Time.deltaTime; Show("land"); return; }
            Show(Mathf.Abs(_ctrl.Velocity.x) > 0.5f ? "run" : "idle");
        }
    }
}
