using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Draws a townsfolk from its sheets (CHR-11, docs/design/npc-animation.md). Each frame, top first: talking
    /// (the talker Wren is speaking with), walking (the schedule between posts, or anything else moving the
    /// transform: a cutscene's walk into the white), the post's activity when the sheets have a clip named
    /// after its first word ("mending nets" → mending, "singing to the water" → singing, "asleep" → asleep),
    /// then idle. A clip the sheets lack falls through to the next.
    /// </summary>
    [RequireComponent(typeof(InkSheetPlayer))]
    public sealed class NpcAnimator : MonoBehaviour
    {
        /// <summary>Units per second the transform must move before the walk shows without a schedule saying so.</summary>
        public const float WalkSpeed = 0.3f;

        /// <summary>What it does when no schedule says (a crowd's bird, CHR-12): "watching the leap", "cheering". Its first word names the clip.</summary>
        [SerializeField] string _activity = "";

        InkSheetPlayer _sheet;
        System.Func<string, bool> _has;   // the sheet's Has, bound once: a method group passed each frame is a delegate each frame
        NpcSchedule _schedule;
        NpcTalker _talker;
        float _lastX;
        bool _hasLast;

        public string Clip => _sheet != null ? _sheet.Current : null;
        /// <summary>The standing activity, used when there is no schedule (or the schedule has no post).</summary>
        public string Activity { get => _activity; set => _activity = value ?? ""; }

        void Awake()
        {
            _sheet = GetComponent<InkSheetPlayer>();
            _has = _sheet != null ? _sheet.Has : null;
            _schedule = GetComponent<NpcSchedule>();
            _talker = GetComponent<NpcTalker>();
        }

        void OnEnable() { _hasLast = false; }

        /// <summary>The clip name a post's activity asks for: its first word, lower-case ("reading the ledger" → reading).</summary>
        static readonly char[] s_wordEnd = { ' ', ',', '.', ';', ':' };
        static readonly System.Collections.Generic.Dictionary<string, string> s_clipOf = new System.Collections.Generic.Dictionary<string, string>();

        public static string ActivityClip(string activity)
        {
            if (string.IsNullOrWhiteSpace(activity)) return null;
            if (s_clipOf.TryGetValue(activity, out var kept)) return kept;   // asked every frame for every bird with a post
            var s = activity.Trim();
            int end = s.IndexOfAny(s_wordEnd);
            var word = (end > 0 ? s.Substring(0, end) : s).ToLowerInvariant();
            kept = word.Length > 0 ? word : null;
            s_clipOf[activity] = kept;
            return kept;
        }

        /// <summary>The clip for a state, given which clips exist; pure, so the order can be tested without a scene.</summary>
        public static string Choose(bool talking, bool walking, string activity, System.Func<string, bool> has)
        {
            if (talking && has("talk")) return "talk";
            if (walking && has("walk")) return "walk";
            var act = ActivityClip(activity);
            if (act != null && has(act)) return act;
            return "idle";
        }

        void LateUpdate()
        {
            if (_sheet == null) return;
            float x = transform.position.x;
            float vx = _hasLast && Time.deltaTime > 0f ? Mathf.Abs(x - _lastX) / Time.deltaTime : 0f;
            _lastX = x;
            _hasLast = true;

            bool talking = _talker != null && NpcTalker.Talking == _talker;
            bool walking = (_schedule != null && _schedule.IsWalking) || vx > WalkSpeed;
            string activity = _schedule != null && !_schedule.IsWalking ? _schedule.Activity : null;
            if (string.IsNullOrEmpty(activity)) activity = _activity;   // a crowd's bird: what it stands doing
            _sheet.Play(Choose(talking, walking, activity, _has));
        }
    }
}
