using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Wren's footsteps (AUD-12, docs/design/footsteps.md). While she runs on the ground a footfall comes every stride
    /// (the run clip's loop holds two), and each is a step on the surface under her: the paper-kit tile the block she
    /// stands on wears (<see cref="FootstepSounds.OfMaterial"/>), or the region's ground where it wears none. The three
    /// takes turn in order. A landing is the surface's landing under the pen set down, louder the longer she was in the
    /// air. A dash, a glide, a cling or a frozen Wren makes no steps. <see cref="WrenSounds"/> puts it on her.
    /// On a wall (AUD-17) her talons catch as she clings, scrape while she slides, and scuff as she pushes off. In a
    /// fading place her feet thin with it, a stage at a time, and from the stage the ground's drawing goes they step on
    /// the page (<see cref="FootstepSounds.UnderFade"/>).
    /// </summary>
    public sealed class Footsteps : MonoBehaviour
    {
        WrenController _ctrl;
        InkSheetPlayer _sheet;
        float _travel, _air, _stride = -1f;
        int _take;
        bool _wasClinging;
        AudioSource _slide;
        readonly Dictionary<Collider2D, FootstepSounds.Surface> _known = new Dictionary<Collider2D, FootstepSounds.Surface>();

        public int Steps { get; private set; }
        public string LastStep { get; private set; }
        public FootstepSounds.Surface? LastSurface { get; private set; }
        /// <summary>The last of her wall's sounds: the cling's catch or the push off.</summary>
        public string LastWall { get; private set; }
        /// <summary>The slide's scrape, while it plays.</summary>
        public string Sliding => _slide != null && _slide.isPlaying ? FootstepSounds.SlideCue : null;
        /// <summary>The fade stage of the place she is in, as the mix has it.</summary>
        public static int Stage => Mix.Live != null ? Mix.Live.Stage : 0;
        /// <summary>How far she runs between footfalls.</summary>
        public float Stride
        {
            get
            {
                if (_stride > 0f) return _stride;
                if (_sheet == null) _sheet = GetComponentInChildren<InkSheetPlayer>();
                var run = _sheet != null ? _sheet.Find("run") : null;
                _stride = FootstepSounds.Stride(_ctrl != null ? _ctrl.runSpeed : 9f, run != null ? run.Seconds : 0f);
                return _stride;
            }
        }

        void Awake() { _ctrl = GetComponent<WrenController>(); }

        void OnEnable() { if (_ctrl != null) { _ctrl.Landed += OnLanded; _ctrl.WallJumped += OnWallJumped; } }
        void OnDisable()
        {
            if (_ctrl != null) { _ctrl.Landed -= OnLanded; _ctrl.WallJumped -= OnWallJumped; }
            StopSlide();
            _wasClinging = false;
        }

        void FixedUpdate()
        {
            if (_ctrl == null) return;
            float dt = Time.fixedDeltaTime;
            Wall();
            if (!_ctrl.IsGrounded) { _air += dt; return; }
            float vx = Mathf.Abs(_ctrl.Velocity.x);
            if (_ctrl.Frozen || _ctrl.IsDashing || vx < 0.5f)
            {
                _travel = Stride * 0.5f;   // starting off, the first foot comes down half a stride in
                return;
            }
            _travel += vx * dt;
            if (_travel < Stride) return;
            _travel -= Stride;
            Step();
        }

        void Step()
        {
            int stage = Stage;
            var s = FootstepSounds.UnderFade(SurfaceUnder(), stage);
            var cue = FootstepSounds.StepCue(s, _take++);
            InkSoundBank.Play(cue, FootstepSounds.FadeGain(stage), _ctrl.Position);
            Steps++;
            LastStep = cue;
            LastSurface = s;
        }

        void OnLanded()
        {
            float air = _air;
            _air = 0f;
            _travel = 0f;   // the landing is a footfall: the next comes a stride on
            if (air < 0.08f) return;   // a step off a lip, not a landing
            int stage = Stage;
            var s = FootstepSounds.UnderFade(SurfaceUnder(), stage);
            InkSoundBank.Play(FootstepSounds.LandCue(s), FootstepSounds.LandGain(air) * FootstepSounds.FadeGain(stage), _ctrl.Position);
            LastSurface = s;
        }

        // ---- the wall ----

        void Wall()
        {
            bool clinging = _ctrl.IsClinging && !_ctrl.Frozen;
            if (clinging && !_wasClinging) Play(FootstepSounds.ClingCue);
            _wasClinging = clinging;
            if (clinging && _ctrl.IsSliding) StartSlide();
            else StopSlide();
        }

        void OnWallJumped() => Play(FootstepSounds.KickCue);

        void Play(string cue)
        {
            InkSoundBank.Play(cue, FootstepSounds.FadeGain(Stage), _ctrl.Position);
            LastWall = cue;
        }

        void StartSlide()
        {
            var bank = InkSoundBank.Instance;
            if (bank == null) return;
            if (_slide == null)
            {
                var clip = bank.Clip(FootstepSounds.SlideCue);
                if (clip == null) return;
                _slide = gameObject.AddComponent<AudioSource>();
                _slide.playOnAwake = false;
                _slide.spatialBlend = 0f;   // panned by hand, from where she is
                _slide.loop = true;
                _slide.clip = clip;
            }
            if (!_slide.isPlaying) _slide.Play();
            var place = bank.Place(_ctrl.Position);
            var c = InkSounds.Of(FootstepSounds.SlideCue);
            _slide.panStereo = place.pan;
            _slide.volume = bank.Volume * (c != null ? c.Gain : 1f) * place.gain * FootstepSounds.FadeGain(Stage);
        }

        void StopSlide()
        {
            if (_slide != null && _slide.isPlaying) _slide.Stop();
        }

        /// <summary>What she is standing on: the tile of the block under her feet, else that block's region's ground, else paper.</summary>
        public FootstepSounds.Surface SurfaceUnder()
        {
            var feet = _ctrl.Position;
            var hit = Physics2D.Raycast(feet + Vector2.up * 0.1f, Vector2.down, 0.5f, _ctrl.groundMask);
            var col = hit.collider;
            if (col == null) return FootstepSounds.OfRegion(Mix.RegionOf(gameObject.scene.name));
            if (_known.TryGetValue(col, out var known)) return known;
            var r = col.GetComponent<Renderer>();
            if (r == null) r = col.GetComponentInParent<Renderer>();
            var s = (r != null && r.sharedMaterial != null ? FootstepSounds.OfMaterial(r.sharedMaterial.name) : null)
                    ?? FootstepSounds.OfRegion(Mix.RegionOf(col.gameObject.scene.name));
            if (_known.Count > 256) _known.Clear();   // rooms come and go; their blocks with them
            _known[col] = s;
            return s;
        }
    }
}
