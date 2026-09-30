using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Draws an enemy from its sheets (CHR-06): each frame it asks the enemy which clip it is in
    /// (<see cref="Enemy.Clip"/>) and, for moves that follow their own frame data, how far
    /// (<see cref="Enemy.ClipProgress"/>). A clip the sheets lack falls back to idle. The death clip's length
    /// becomes the enemy's death fade, so the ink leaves as the last frame lands.
    /// </summary>
    [RequireComponent(typeof(InkSheetPlayer))]
    public sealed class EnemyAnimator : MonoBehaviour
    {
        Enemy _enemy;
        InkSheetPlayer _sheet;

        public string Clip => _sheet != null ? _sheet.Current : null;

        void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _sheet = GetComponent<InkSheetPlayer>();
            var death = _sheet.Find("death");
            if (death != null && _enemy != null) _enemy.DeathSeconds = death.Seconds;
        }

        void LateUpdate()
        {
            if (_enemy == null || _sheet == null) return;
            var clip = _enemy.Clip;
            if (string.IsNullOrEmpty(clip)) return;
            if (!_sheet.Has(clip)) clip = "idle";
            if (!_sheet.Play(clip)) return;
            float p = _enemy.ClipProgress;
            if (p >= 0f) _sheet.Seek(p);
        }
    }
}
