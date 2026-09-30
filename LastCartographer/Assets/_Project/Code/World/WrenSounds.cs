using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Wren's sounds (AUD-03, docs/design/wren-sounds.md): her quill is ink and paper. The swing is a stroke and a hit
    /// adds its layer; a pogo is the nib's tap; a dash a page turned fast; the thread a line drawn out; a Bind a word
    /// being written; the survey hatches while held and ends on a long stroke; hurt is a smudge, never a cry; the
    /// Flourishes have their own. Everything goes through <see cref="InkSoundBank"/>, and without it nothing happens.
    /// </summary>
    public sealed class WrenSounds : MonoBehaviour
    {
        WrenController _ctrl;
        QuillStrike _strike;
        Flourishes _flourishes;
        WrenVitals _vitals;
        bool _surveying;

        public bool IsSurveyLooping => _surveying;

        void Awake()
        {
            _ctrl = GetComponent<WrenController>();
            _strike = GetComponent<QuillStrike>();
            _flourishes = GetComponent<Flourishes>();
            _vitals = GetComponent<WrenVitals>();
        }

        void OnEnable()
        {
            if (_ctrl != null)
            {
                _ctrl.Jumped += OnJumped; _ctrl.Landed += OnLanded; _ctrl.Dashed += OnDashed;
                _ctrl.Pogoed += OnPogoed; _ctrl.Threaded += OnThreaded; _ctrl.ThreadRefused += OnRefused;
            }
            if (_strike != null) { _strike.Swung += OnSwung; _strike.Landed += OnHit; }
            if (_flourishes != null) { _flourishes.Performed += OnFlourish; _flourishes.Refused += OnFlourishRefused; }
            if (_vitals != null) { _vitals.Bound += OnBound; _vitals.Hurt += OnHurt; _vitals.Died += OnDied; }
            VantagePoint.AnySurveyed += OnSurveyed;
        }

        void OnDisable()
        {
            if (_ctrl != null)
            {
                _ctrl.Jumped -= OnJumped; _ctrl.Landed -= OnLanded; _ctrl.Dashed -= OnDashed;
                _ctrl.Pogoed -= OnPogoed; _ctrl.Threaded -= OnThreaded; _ctrl.ThreadRefused -= OnRefused;
            }
            if (_strike != null) { _strike.Swung -= OnSwung; _strike.Landed -= OnHit; }
            if (_flourishes != null) { _flourishes.Performed -= OnFlourish; _flourishes.Refused -= OnFlourishRefused; }
            if (_vitals != null) { _vitals.Bound -= OnBound; _vitals.Hurt -= OnHurt; _vitals.Died -= OnDied; }
            VantagePoint.AnySurveyed -= OnSurveyed;
            if (_surveying) { _surveying = false; InkSoundBank.Loop(null); }
        }

        void Update()
        {
            // The survey hatches while she holds it (VantagePoint.Surveying), and stops the moment she lets go.
            bool surveying = VantagePoint.Surveying != null;
            if (surveying == _surveying) return;
            _surveying = surveying;
            InkSoundBank.Loop(surveying ? "survey" : null);
        }

        void OnJumped() => InkSoundBank.Play("jump");
        void OnLanded() => InkSoundBank.Play("land");
        void OnDashed() => InkSoundBank.Play("dash");
        void OnPogoed() => InkSoundBank.Play("pogo");
        void OnThreaded() => InkSoundBank.Play("thread");
        void OnRefused() => InkSoundBank.Play("refused");
        void OnSwung(Vector2 dir) => InkSoundBank.Play("stroke");
        void OnHit(IHittable h) => InkSoundBank.Play("hit");
        void OnBound() => InkSoundBank.Play("bind");
        void OnHurt() => InkSoundBank.Play("hurt");
        void OnDied() => InkSoundBank.Play("died");
        void OnFlourishRefused(FlourishKind k) => InkSoundBank.Play("refused");
        void OnSurveyed(VantagePoint v) => InkSoundBank.Play("drawn");

        void OnFlourish(FlourishKind kind)
        {
            switch (kind)
            {
                case FlourishKind.Crosshatch: InkSoundBank.Play("crosshatch"); break;
                case FlourishKind.Longstroke: InkSoundBank.Play("longstroke"); break;
                case FlourishKind.Blot: InkSoundBank.Play("blot"); break;
            }
        }
    }
}
