using System;
using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>One clip of a character's sprite sheet: a horizontal strip of equal frames.</summary>
    [Serializable]
    public sealed class SheetClip
    {
        public string Name;
        public Texture2D Sheet;
        public int Frames = 1;
        public float Fps = 12f;
        public bool Loop = true;
        public float Seconds => Frames / Mathf.Max(1f, Fps);
    }

    /// <summary>A second drawing of the same clips under a name (CHR-05: Wren in another Charter's cowl and grip).</summary>
    [Serializable]
    public sealed class SheetSet
    {
        public string Name;
        public List<SheetClip> Clips = new List<SheetClip>();
    }

    /// <summary>
    /// Plays sprite-sheet clips on an InkSprite quad (CHR-03): the current frame is a window into the strip,
    /// set through the renderer's property block (_BaseMap and _BaseMap_ST), so the material and the ink
    /// state stay shared. A looping clip cycles; a one-shot clip holds its last frame. A clip can also be
    /// seeked to a fraction, for moves whose frames follow the game's own frame data rather than the clock.
    /// A character drawn more than one way carries the other drawings as named sets of the same clips; wearing
    /// one swaps the strips under whatever is playing without restarting it (CHR-05).
    /// </summary>
    public sealed class InkSheetPlayer : MonoBehaviour
    {
        [SerializeField] Renderer _renderer;
        [SerializeField] List<SheetClip> _clips = new List<SheetClip>();
        [SerializeField] List<SheetSet> _sets = new List<SheetSet>();

        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int StId = Shader.PropertyToID("_BaseMap_ST");
        MaterialPropertyBlock _mpb;
        SheetClip _current;
        SheetSet _set;
        float _t;
        int _frame = -1;

        public IReadOnlyList<SheetClip> Clips => _clips;
        public IReadOnlyList<SheetSet> Sets => _sets;
        /// <summary>The set being worn, or null on the base clips.</summary>
        public string Set => _set?.Name;
        public string Current => _current?.Name;
        public int Frame => _frame;
        public float Speed { get; set; } = 1f;
        /// <summary>A one-shot clip has shown its last frame.</summary>
        public bool Finished => _current != null && !_current.Loop && _t >= _current.Seconds;

        /// <summary>Editor setup: the quad to drive and the clips to play (serialised with the scene).</summary>
        public void Configure(Renderer renderer, IEnumerable<SheetClip> clips)
        {
            _renderer = renderer;
            _clips = new List<SheetClip>(clips);
        }

        /// <summary>Editor setup: the other drawings of these clips (serialised with the scene).</summary>
        public void ConfigureSets(IEnumerable<SheetSet> sets) => _sets = new List<SheetSet>(sets);

        /// <summary>A clip by name: from the set being worn, else the base (a set mid-way through a redraw).</summary>
        public SheetClip Find(string name)
        {
            if (_set != null)
                for (int i = 0; i < _set.Clips.Count; i++) if (_set.Clips[i].Name == name) return _set.Clips[i];
            for (int i = 0; i < _clips.Count; i++) if (_clips[i].Name == name) return _clips[i];
            return null;
        }

        public bool HasSet(string name) => FindSet(name) != null;

        SheetSet FindSet(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < _sets.Count; i++) if (_sets[i].Name == name) return _sets[i];
            return null;
        }

        /// <summary>
        /// Wear a named set: the clip playing carries on at its time, drawn from the set. Null or a name with no
        /// set goes back to the base clips. True when the named set is worn.
        /// </summary>
        public bool UseSet(string name)
        {
            var s = FindSet(name);
            if (s != _set)
            {
                _set = s;
                if (_current != null)
                {
                    _current = Find(_current.Name) ?? _current;
                    _frame = -1;
                    Apply();
                }
            }
            return s != null;
        }

        public bool Has(string name) => Find(name) != null;

        /// <summary>Switch to a clip; the same clip keeps playing unless restarted.</summary>
        public bool Play(string name, bool restart = false)
        {
            var c = Find(name);
            if (c == null) return false;
            if (c == _current && !restart) return true;
            _current = c;
            _t = 0f;
            _frame = -1;
            Apply();
            return true;
        }

        /// <summary>Show the frame at a fraction of the current clip (0 first, 1 last).</summary>
        public void Seek(float progress)
        {
            if (_current == null) return;
            _t = Mathf.Clamp01(progress) * _current.Seconds;
            Apply();
        }

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        }

        void Update()
        {
            if (_current == null) return;
            _t += Time.deltaTime * Speed;
            Apply();
        }

        void Apply()
        {
            if (_current == null) return;
            int n = Mathf.Max(1, _current.Frames);
            int idx = (int)(_t * _current.Fps);
            idx = _current.Loop ? ((idx % n) + n) % n : Mathf.Clamp(idx, 0, n - 1);
            if (idx == _frame) return;
            _frame = idx;
            if (_renderer == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            if (_current.Sheet != null) _mpb.SetTexture(BaseMapId, _current.Sheet);
            _mpb.SetVector(StId, new Vector4(1f / n, 1f, (float)_frame / n, 0f));
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
