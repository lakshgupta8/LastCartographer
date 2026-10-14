using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// One of a crowd who turns to watch Wren go by (docs/design/blank-generator.md §3): when she comes within
    /// <see cref="Reach"/> they turn to face her, and once she is past <see cref="LetGo"/> they turn back the way they
    /// were stood. Nobody turns at once: each waits a beat of their own, longer the further off she is, so a crowd turns
    /// one by one, the nearest first. Someone asleep does not turn. The drawing is mirrored, not the object, so the
    /// figure's talker and colliders (if any) are left as they are.
    /// </summary>
    public sealed class CrowdTurns : MonoBehaviour
    {
        /// <summary>How near she must come to be watched, and how far she must go to be let go (units).</summary>
        public const float Reach = 6f, LetGo = 7.5f;
        /// <summary>The beat before turning: a person's own, plus a little for every unit she is away.</summary>
        public const float BaseBeat = 0.12f, BeatPerUnit = 0.05f, MaxOwnBeat = 0.35f;

        Transform _sprite;
        NpcAnimator _animator;
        int _rest;
        bool _watching;
        int _want;
        float _since;
        float _ownBeat;

        /// <summary>The way they face now: +1 right, -1 left.</summary>
        public int Facing => _sprite != null && _sprite.localScale.x < 0f ? -1 : 1;
        /// <summary>The way they were stood.</summary>
        public int RestFacing => _rest;
        /// <summary>Whether they are turned to her.</summary>
        public bool Watching => _watching;

        void Start()
        {
            _sprite = transform.Find("Sprite");
            _animator = GetComponent<NpcAnimator>();
            _rest = Facing;
            _want = _rest;
            _ownBeat = (Hash(name) & 0x7fffffff) % 1000 / 1000f * MaxOwnBeat;
        }

        /// <summary>
        /// Which way someone wants to face, and whether they are watching, given the way they were stood, how far Wren
        /// is to their right (negative: to their left) and whether they were already watching her. Pure.
        /// </summary>
        public static (int facing, bool watching) Want(int rest, float dx, bool watching)
        {
            float d = Mathf.Abs(dx);
            bool watch = watching ? d <= LetGo : d <= Reach;
            return watch ? (dx >= 0f ? 1 : -1, true) : (rest, false);
        }

        /// <summary>The beat someone waits before turning, with her <paramref name="distance"/> away.</summary>
        public static float Beat(float ownBeat, float distance) => BaseBeat + ownBeat + BeatPerUnit * distance;

        void Update()
        {
            if (_sprite == null) return;
            if (IsAsleep()) return;
            var wren = WrenController.Current;
            if (wren == null) return;
            float dx = wren.Position.x - transform.position.x;
            var (facing, watching) = Want(_rest, dx, _watching);
            if (facing != _want) { _want = facing; _since = 0f; }
            _watching = watching;
            if (Facing == _want) return;
            _since += Time.deltaTime;
            if (_since < Beat(_ownBeat, Mathf.Abs(dx))) return;
            var s = _sprite.localScale;
            s.x = Mathf.Abs(s.x) * _want;
            _sprite.localScale = s;
        }

        bool IsAsleep() => _animator != null && NpcAnimator.ActivityClip(_animator.Activity) == "asleep";

        static int Hash(string s)
        {
            int h = 17;
            foreach (var c in s ?? "") h = unchecked(h * 31 + c);
            return h;
        }
    }
}
