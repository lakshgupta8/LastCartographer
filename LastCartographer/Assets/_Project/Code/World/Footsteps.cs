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
    /// </summary>
    public sealed class Footsteps : MonoBehaviour
    {
        WrenController _ctrl;
        InkSheetPlayer _sheet;
        float _travel, _air, _stride = -1f;
        int _take;
        readonly Dictionary<Collider2D, FootstepSounds.Surface> _known = new Dictionary<Collider2D, FootstepSounds.Surface>();

        public int Steps { get; private set; }
        public string LastStep { get; private set; }
        public FootstepSounds.Surface? LastSurface { get; private set; }
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

        void OnEnable() { if (_ctrl != null) _ctrl.Landed += OnLanded; }
        void OnDisable() { if (_ctrl != null) _ctrl.Landed -= OnLanded; }

        void FixedUpdate()
        {
            if (_ctrl == null) return;
            float dt = Time.fixedDeltaTime;
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
            var s = SurfaceUnder();
            var cue = FootstepSounds.StepCue(s, _take++);
            InkSoundBank.Play(cue, 1f, _ctrl.Position);
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
            var s = SurfaceUnder();
            InkSoundBank.Play(FootstepSounds.LandCue(s), FootstepSounds.LandGain(air), _ctrl.Position);
            LastSurface = s;
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
