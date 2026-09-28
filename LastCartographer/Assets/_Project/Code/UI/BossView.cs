using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>Boss name and an ink health bar with phase marks, top centre; phase lines as a caption.</summary>
    public sealed class BossView : MonoBehaviour
    {
        [SerializeField] float _lineSeconds = 2.6f;

        Boss _boss;
        string _line;
        float _lineUntil;
        bool _built;
        VisualElement _panel, _bar, _fill, _marks;
        Label _name, _caption;

        public bool IsShowing => _boss != null;
        public string NameText => _built ? _name.text : "";
        public string CaptionText => _built && _caption.style.display == DisplayStyle.Flex ? _caption.text : "";
        public float FillFraction => _boss != null && _boss.MaxHealth > 0 ? (float)_boss.Health / _boss.MaxHealth : 0f;

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
            if (arena.State == BossArena.ArenaState.Won) { _line = Loc.T("boss.won", "…keep it lit."); _lineUntil = Time.unscaledTime + _lineSeconds; }
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

        void Update()
        {
            if (!_built && !Build()) return;
            bool showLine = _line != null && Time.unscaledTime < _lineUntil;
            InkTheme.Show(_caption, showLine);
            if (showLine) _caption.text = _line;
            InkTheme.Show(_panel, _boss != null);
            if (_boss == null) return;
            _name.text = _boss.BossName;
            _fill.style.width = new Length(FillFraction * 100f, LengthUnit.Percent);
            int marks = _boss.PhaseCount - 1;
            while (_marks.childCount < marks) _marks.Add(MakeMark());
            while (_marks.childCount > marks) _marks.RemoveAt(_marks.childCount - 1);
            for (int i = 0; i < marks; i++)
                _marks[i].style.left = new Length((1f - (float)(i + 1) / _boss.PhaseCount) * 100f, LengthUnit.Percent);
        }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.BossLayer == null) return false;
            _panel = new VisualElement { name = "boss", pickingMode = PickingMode.Ignore };
            _panel.style.position = Position.Absolute;
            _panel.style.top = 24;
            _panel.style.left = new Length(50, LengthUnit.Percent);
            _panel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            _panel.style.width = 620;
            _panel.style.alignItems = Align.Center;
            InkTheme.ApplyFont(_panel);

            _name = InkTheme.Text("name", "", 24, InkTheme.Ink, FontStyle.Bold);
            _name.style.marginBottom = 8;
            _bar = new VisualElement { name = "bar", pickingMode = PickingMode.Ignore };
            _bar.style.width = new Length(100, LengthUnit.Percent);
            _bar.style.height = 12;
            _bar.style.backgroundColor = InkTheme.Paper;
            InkTheme.SetBorder(_bar, InkTheme.Ink, 1.5f);
            _fill = new VisualElement { name = "fill", pickingMode = PickingMode.Ignore };
            _fill.style.height = new Length(100, LengthUnit.Percent);
            _fill.style.backgroundColor = InkTheme.Ink;
            _marks = new VisualElement { name = "marks", pickingMode = PickingMode.Ignore };
            _marks.style.position = Position.Absolute;
            _marks.style.left = 0; _marks.style.right = 0; _marks.style.top = -4; _marks.style.bottom = -4;
            _bar.Add(_fill); _bar.Add(_marks);
            _panel.Add(_name); _panel.Add(_bar);
            InkTheme.Show(_panel, false);
            ui.BossLayer.Add(_panel);

            _caption = InkTheme.Text("boss-line", "", 26, InkTheme.Wash, FontStyle.Italic);
            _caption.style.position = Position.Absolute;
            _caption.style.left = 0; _caption.style.right = 0; _caption.style.top = new Length(22, LengthUnit.Percent);
            _caption.style.unityTextAlign = TextAnchor.MiddleCenter;
            InkTheme.Show(_caption, false);
            ui.Caption.Add(_caption);
            _built = true;
            return true;
        }

        static VisualElement MakeMark()
        {
            var m = new VisualElement { pickingMode = PickingMode.Ignore };
            m.style.position = Position.Absolute;
            m.style.top = 0; m.style.bottom = 0;
            m.style.width = 2;
            m.style.backgroundColor = InkTheme.Ink;
            return m;
        }
    }
}
