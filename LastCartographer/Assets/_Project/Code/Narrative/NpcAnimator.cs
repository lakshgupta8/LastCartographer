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

        InkSheetPlayer _sheet;
        NpcSchedule _schedule;
        NpcTalker _talker;
        float _lastX;
        bool _hasLast;

        public string Clip => _sheet != null ? _sheet.Current : null;

        void Awake()
        {
            _sheet = GetComponent<InkSheetPlayer>();
            _schedule = GetComponent<NpcSchedule>();
            _talker = GetComponent<NpcTalker>();
        }

        void OnEnable() { _hasLast = false; }

        /// <summary>The clip name a post's activity asks for: its first word, lower-case ("reading the ledger" → reading).</summary>
        public static string ActivityClip(string activity)
        {
            if (string.IsNullOrWhiteSpace(activity)) return null;
            var s = activity.Trim();
            int end = s.IndexOfAny(new[] { ' ', ',', '.', ';', ':' });
            var word = (end > 0 ? s.Substring(0, end) : s).ToLowerInvariant();
            return word.Length > 0 ? word : null;
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
            _sheet.Play(Choose(talking, walking, activity, _sheet.Has));
        }
    }
}
