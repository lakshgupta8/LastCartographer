using OWSBG.World;
using UnityEngine;

namespace OWSBG.UI
{
    /// <summary>Boss name, health bar with phase marks, and the phase line, drawn with IMGUI until ENV-11.</summary>
    public sealed class BossHud : MonoBehaviour
    {
        [SerializeField] int _fontSize = 22;
        [SerializeField] float _lineSeconds = 2.6f;

        Boss _boss;
        string _line;
        float _lineUntil;
        GUIStyle _name, _lineStyle;
        Texture2D _ink, _paper;

        void OnEnable()
        {
            BossArena.FightStarted += OnFightStarted;
            BossArena.FightWon += OnFightEnded;
            BossArena.FightReset += OnFightEnded;
        }

        void OnDisable()
        {
            BossArena.FightStarted -= OnFightStarted;
            BossArena.FightWon -= OnFightEnded;
            BossArena.FightReset -= OnFightEnded;
            Unbind();
        }

        void OnFightStarted(BossArena arena)
        {
            Unbind();
            _boss = arena.Boss;
            if (_boss != null) _boss.PhaseStarted += OnPhase;
            _line = null;
        }

        void OnFightEnded(BossArena arena)
        {
            if (arena.State == BossArena.ArenaState.Won) { _line = "…keep it lit."; _lineUntil = Time.unscaledTime + _lineSeconds; }
            Unbind();
        }

        void Unbind()
        {
            if (_boss != null) _boss.PhaseStarted -= OnPhase;
            _boss = null;
        }

        void OnPhase(Boss b, int phase, string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            _line = line;
            _lineUntil = Time.unscaledTime + _lineSeconds;
        }

        void OnGUI()
        {
            bool showLine = _line != null && Time.unscaledTime < _lineUntil;
            if (_boss == null && !showLine) return;
            EnsureStyles();
            if (_boss != null)
            {
                float w = Mathf.Min(Screen.width * 0.5f, 620f);
                float x = (Screen.width - w) * 0.5f, y = 26f;
                GUI.Label(new Rect(x, y, w, 30f), _boss.BossName, _name);
                var bar = new Rect(x, y + 32f, w, 12f);
                GUI.DrawTexture(bar, _paper);
                float f = _boss.MaxHealth > 0 ? (float)_boss.Health / _boss.MaxHealth : 0f;
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * f, bar.height), _ink);
                for (int i = 1; i < _boss.PhaseCount; i++)
                {
                    float px = bar.x + bar.width * (1f - (float)i / _boss.PhaseCount);
                    GUI.DrawTexture(new Rect(px - 1f, bar.y - 3f, 2f, bar.height + 6f), _ink);
                }
            }
            if (showLine)
                GUI.Label(new Rect(0f, Screen.height * 0.22f, Screen.width, 40f), _line, _lineStyle);
        }

        void EnsureStyles()
        {
            if (_name != null) return;
            var ink = new Color(0.08f, 0.08f, 0.11f);
            _ink = new Texture2D(1, 1); _ink.SetPixel(0, 0, ink); _ink.Apply();
            _paper = new Texture2D(1, 1); _paper.SetPixel(0, 0, new Color(0.96f, 0.93f, 0.85f, 0.9f)); _paper.Apply();
            _name = new GUIStyle(GUI.skin.label) { fontSize = _fontSize, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = ink } };
            _lineStyle = new GUIStyle(GUI.skin.label) { fontSize = _fontSize + 2, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.20f, 0.27f, 0.45f) } };
        }
    }
}
