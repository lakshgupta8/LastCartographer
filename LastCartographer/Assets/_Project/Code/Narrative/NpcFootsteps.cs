using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Another bird's feet (AUD-23, docs/design/footsteps.md §2b): a townsfolk walking between posts, a crowd's bird, an
    /// ally crossing a room. <see cref="NpcAnimator"/> puts one on every bird drawn from sheets and tells it each frame
    /// whether the bird walks; while it does, a footfall comes every stride for its size, on the surface under its feet
    /// read as hers is (<see cref="Footsteps.SurfaceOf"/>), lighter than hers and pitched by its size (a sparrow higher,
    /// a goose lower), thinning with a fading place as hers do. A bird in the air, or one far off the screen, makes none.
    /// </summary>
    public sealed class NpcFootsteps : MonoBehaviour
    {
        /// <summary>How quiet a place on the room must be before a step there is not worth a voice.</summary>
        public const float Unheard = 0.05f;

        float _travel, _height = -1f, _lastX;
        bool _has;
        int _take;
        static int _ground = -1;

        public int Steps { get; private set; }
        public string LastStep { get; private set; }
        /// <summary>The bird's height in units: its drawing's, else a sparrow's.</summary>
        public float Height
        {
            get
            {
                if (_height > 0f) return _height;
                var r = GetComponentInChildren<Renderer>();
                float h = r != null ? r.bounds.size.y : 0f;
                if (h > 0.2f) _height = h;
                return h > 0.2f ? h : 1.4f;
            }
        }

        void OnEnable() { _has = false; }

        /// <summary>Called each frame by the animator: is the bird walking.</summary>
        public void Tick(bool walking)
        {
            float x = transform.position.x;
            float dx = _has ? Mathf.Abs(x - _lastX) : 0f;
            _lastX = x;
            _has = true;
            float stride = FootstepSounds.NpcStride(Height);
            if (!walking) { _travel = stride * 0.5f; return; }   // a walk begins half a stride from its first step
            _travel += dx;
            if (_travel < stride) return;
            _travel -= stride;
            Step();
        }

        void Step()
        {
            var bank = InkSoundBank.Instance;
            if (bank == null) return;
            var feet = (Vector2)transform.position;
            if (bank.Place(feet).gain < Unheard) return;
            if (_ground < 0) _ground = LayerMask.GetMask("Ground");
            var hit = Physics2D.Raycast(feet + Vector2.up * 0.3f, Vector2.down, 0.8f, _ground);
            if (hit.collider == null) return;   // in the air, or on nothing drawn
            int stage = Footsteps.Stage;
            var s = FootstepSounds.UnderFade(Footsteps.SurfaceOf(hit.collider), stage);
            var id = FootstepSounds.StepCue(s, _take++);
            InkSoundBank.Play(id, FootstepSounds.NpcGain(Height) * FootstepSounds.FadeGain(stage), feet, FootstepSounds.NpcPitch(Height));
            Steps++;
            LastStep = id;
        }
    }
}
