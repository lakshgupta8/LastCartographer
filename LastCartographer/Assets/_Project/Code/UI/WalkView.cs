using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace OWSBG.UI
{
    /// <summary>
    /// The roll-call strip (DES-13): while a bounds-walk runs, the called name large, the verse and beat under
    /// it, the misses as three marks, and a thin bar filling to the beat. Sits on the boss layer, top centre,
    /// and lingers a moment on "Held." when the walk is done.
    /// </summary>
    public sealed class WalkView : MonoBehaviour
    {
        [SerializeField] float _lingerSeconds = 2.5f;

        public bool IsVisible => _built && _root.style.display == DisplayStyle.Flex;
        public string CalledText => _name != null ? _name.text : "";

        VisualElement _root, _bar, _fill, _marks;
        Label _name, _line;
        bool _built;
        float _linger, _flash;

        void OnEnable()
        {
            BoundsWalk.Started += OnStarted;
            BoundsWalk.NameCalled += OnCalled;
            BoundsWalk.BeatLanded += OnLanded;
            BoundsWalk.Completed += OnCompleted;
        }

        void OnDisable()
        {
            BoundsWalk.Started -= OnStarted;
            BoundsWalk.NameCalled -= OnCalled;
            BoundsWalk.BeatLanded -= OnLanded;
            BoundsWalk.Completed -= OnCompleted;
        }

        void OnStarted(BoundsWalk w) { _linger = 0f; if (_built) { InkTheme.Show(_root, true); _name.text = "…"; _name.style.color = InkTheme.Ink; } }
        void OnCalled(BoundsWalk w, BoundsWalk.Bound b) { if (_built) _name.text = b.Name; }
        void OnLanded(BoundsWalk w, BoundsWalk.Bound b, bool hit) { _flash = 0.4f; if (_built) _name.style.color = hit ? InkTheme.Wash : InkTheme.Ochre; }
        void OnCompleted(BoundsWalk w) { _linger = Options.CaptionSeconds(_lingerSeconds); if (_built) { _name.text = Loc.T("walk.held", "Held."); _name.style.color = InkTheme.Wash; _fill.style.width = new Length(100, LengthUnit.Percent); } }

        void Update()
        {
            if (!_built && !Build()) return;
            var walk = BoundsWalk.Current;
            if (walk == null || !walk.IsWalking)
            {
                if (float.IsPositiveInfinity(_linger) && PromptView.DismissPressed()) _linger = 0.0001f;
                if (_linger > 0f) { _linger -= Time.deltaTime; InkTheme.Show(_root, true); if (_linger <= 0f) InkTheme.Show(_root, false); }
                else if (IsVisible) InkTheme.Show(_root, false);
                return;
            }
            InkTheme.Show(_root, true);
            if (_flash > 0f) _flash -= Time.deltaTime; else _name.style.color = InkTheme.Ink;
            var verse = walk.CurrentVerse;
            _line.text = (verse != null && !string.IsNullOrEmpty(verse.Title) ? verse.Title + "  ·  " : "")
                         + Loc.F("walk.progress", "verse {0} of {1}  ·  beat {2} of {3}", walk.VerseIndex + 1, walk.Verses.Count, walk.BeatIndex + 1, verse != null ? verse.Beats.Count : 0);
            _fill.style.width = new Length(walk.BeatProgress * 100f, LengthUnit.Percent);
            for (int i = 0; i < _marks.childCount; i++)
                _marks[i].style.backgroundColor = i < walk.Misses ? InkTheme.Ochre : new Color(0f, 0f, 0f, 0f);
        }

        bool Build()
        {
            var ui = UiRoot.Instance;
            if (ui == null || !ui.IsReady || ui.BossLayer == null) return false;
            _root = new VisualElement { name = "walk", pickingMode = PickingMode.Ignore };
            _root.style.position = Position.Absolute;
            _root.style.left = new Length(50, LengthUnit.Percent);
            _root.style.top = 36;
            _root.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            _root.style.alignItems = Align.Center;
            _root.style.backgroundColor = InkTheme.Paper;
            InkTheme.SetPadding(_root, 10f, 26f);
            InkTheme.SetRadius(_root, 6f);
            InkTheme.SetBorder(_root, InkTheme.InkFaint, 1f);
            InkTheme.ApplyFont(_root);
            _name = InkTheme.Text("walk-name", "", 30, InkTheme.Ink, FontStyle.Bold);
            _line = InkTheme.Text("walk-line", "", 15, InkTheme.Dim);
            _line.style.marginTop = 2;
            _bar = new VisualElement { name = "walk-bar", pickingMode = PickingMode.Ignore };
            InkTheme.SetSize(_bar, 320, 4);
            _bar.style.marginTop = 8;
            _bar.style.backgroundColor = InkTheme.InkFaint;
            _fill = new VisualElement { name = "walk-fill", pickingMode = PickingMode.Ignore };
            _fill.style.height = new Length(100, LengthUnit.Percent);
            _fill.style.backgroundColor = InkTheme.Wash;
            _bar.Add(_fill);
            _marks = InkTheme.Row("walk-misses");
            _marks.style.marginTop = 8;
            for (int i = 0; i < 3; i++)
            {
                var m = new VisualElement { pickingMode = PickingMode.Ignore };
                InkTheme.SetSize(m, 12, 12);
                m.style.marginRight = 6;
                m.style.rotate = new Rotate(45f);
                InkTheme.SetBorder(m, InkTheme.Ochre, 1.5f);
                _marks.Add(m);
            }
            _root.Add(_name); _root.Add(_line); _root.Add(_bar); _root.Add(_marks);
            InkTheme.Show(_root, false);
            ui.BossLayer.Add(_root);
            _built = true;
            return true;
        }
    }
}
