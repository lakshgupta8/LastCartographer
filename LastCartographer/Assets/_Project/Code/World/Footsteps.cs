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
    /// On a wall (AUD-17) her talons catch as she clings, scrape while she slides, and scuff as she pushes off, each on
    /// the wall's own surface, read as the ground's is (AUD-21). In a fading place her feet thin with it, a stage at a
    /// time, and from the stage the ground's drawing goes they step on the page, and grip it (<see cref="FootstepSounds.UnderFade"/>).
    /// </summary>
    public sealed class Footsteps : MonoBehaviour
    {
        WrenController _ctrl;
        InkSheetPlayer _sheet;
        float _travel, _air, _stride = -1f;
        int _take;
        bool _wasClinging;
        AudioSource _slide;
        string _slideCue;
        FootstepSounds.Surface _wall = FootstepSounds.Surface.Paper;
        readonly Dictionary<Collider2D, FootstepSounds.Surface> _known = new Dictionary<Collider2D, FootstepSounds.Surface>();

        public int Steps { get; private set; }
        public string LastStep { get; private set; }
        public FootstepSounds.Surface? LastSurface { get; private set; }
        /// <summary>The last of her wall's sounds: the cling's catch or the push off.</summary>
        public string LastWall { get; private set; }
        /// <summary>The slide's scrape, while it plays.</summary>
        public string Sliding => _slide != null && _slide.isPlaying ? _slideCue : null;
        /// <summary>The surface of the wall she last gripped, as she hears it (the page, in a place faded far enough).</summary>
        public FootstepSounds.Surface LastWallSurface => _wall;
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
            if (clinging) _wall = FootstepSounds.UnderFade(SurfaceBeside(_ctrl.WallDirection) ?? _wall, Stage);
            if (clinging && !_wasClinging) Play(FootstepSounds.ClingCue(_wall));
            _wasClinging = clinging;
            if (clinging && _ctrl.IsSliding) StartSlide();
            else StopSlide();
        }

        /// <summary>Off the wall she was on: as the jump is taken the wall is still the one beside her, or the one she held.</summary>
        void OnWallJumped() => Play(FootstepSounds.KickCue(FootstepSounds.UnderFade(SurfaceBeside(_ctrl.WallDirection) ?? _wall, Stage)));

        void Play(string cue)
        {
            InkSoundBank.Play(cue, FootstepSounds.FadeGain(Stage), _ctrl.Position);
            LastWall = cue;
        }

        void StartSlide()
        {
            var bank = InkSoundBank.Instance;
            if (bank == null) return;
            string cue = FootstepSounds.SlideCue(_wall);
            if (_slide == null)
            {
                _slide = gameObject.AddComponent<AudioSource>();
                _slide.playOnAwake = false;
                _slide.spatialBlend = 0f;   // panned by hand, from where she is
                _slide.loop = true;
            }
            if (cue != _slideCue)   // down onto another wall: its own scrape
            {
                var clip = bank.Clip(cue);
                if (clip == null) return;
                _slide.Stop();
                _slide.clip = clip;
                _slideCue = cue;
            }
            if (!_slide.isPlaying) _slide.Play();
            var place = bank.Place(_ctrl.Position);
            var c = InkSounds.Of(cue);
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
            return hit.collider == null ? FootstepSounds.OfRegion(Mix.RegionOf(gameObject.scene.name)) : SurfaceOf(hit.collider);
        }

        /// <summary>What the wall beside her is (dir -1 left, 1 right), read as the ground is; null where there is none.</summary>
        public FootstepSounds.Surface? SurfaceBeside(int dir)
        {
            if (dir == 0) return null;
            var at = _ctrl.Position + Vector2.up * 0.55f;   // her middle, where the talons are
            var hit = Physics2D.Raycast(at, new Vector2(dir, 0f), 0.8f, _ctrl.groundMask);
            return hit.collider == null ? (FootstepSounds.Surface?)null : SurfaceOf(hit.collider);
        }

        /// <summary>A block's surface: its tile, else its room's region's ground. Kept, since blocks do not change.</summary>
        FootstepSounds.Surface SurfaceOf(Collider2D col)
        {
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
