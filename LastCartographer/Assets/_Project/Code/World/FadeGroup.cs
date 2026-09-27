using System;
using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The renderers of one place, driven by its fade stage (PRG-14): _Ink on ink materials, a wash toward the
    /// paper colour on lit ones, and per-layer dropout once the stage reaches the layer's threshold. A stage
    /// change while the room is loaded animates over a few seconds; a room that loads mid-fade snaps.
    /// </summary>
    public sealed class FadeGroup : MonoBehaviour
    {
        [Serializable]
        public sealed class Layer
        {
            public Renderer Renderer;
            /// <summary>The stage at which this layer leaves the parallax stack (5 = never).</summary>
            public int DropoutStage = FadeStages.Max;
            [NonSerialized] public Color BaseColor;
            [NonSerialized] public bool Cached, IsInk;
        }

        [SerializeField] string _placeId;
        [SerializeField] List<Layer> _layers = new List<Layer>();
        [SerializeField] float _seconds = 2.5f;
        [SerializeField] float _eraseSeconds = 0.8f;
        [SerializeField] Color _paper = new Color(0.93f, 0.89f, 0.80f);

        static readonly int InkId = Shader.PropertyToID("_Ink");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock _mpb;
        WorldState _world;

        public string PlaceId { get => _placeId; set => _placeId = value; }
        public IReadOnlyList<Layer> Layers => _layers;
        public int Stage => FadeStages.Get(GameState.World, _placeId);
        /// <summary>The ink level being shown now (animates toward the stage's).</summary>
        public float Ink { get; private set; } = 1f;
        public float TargetInk => FadeStages.InkFor(Stage);
        public bool IsAnimating => Mathf.Abs(Ink - TargetInk) > 0.001f;

        public Layer AddLayer(Renderer r, int dropoutStage = FadeStages.Max)
        {
            var l = new Layer { Renderer = r, DropoutStage = dropoutStage };
            _layers.Add(l);
            if (_mpb != null && isActiveAndEnabled) Apply();   // layers added at runtime get the current look at once
            return l;
        }

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (string.IsNullOrEmpty(_placeId))
            {
                var room = GetComponentInParent<Room>();
                if (room != null) _placeId = room.RoomId;
            }
        }

        void OnEnable()
        {
            FadeStages.Changed += OnChanged;
            GameState.Loaded += Snap;
            Snap();
        }

        void OnDisable()
        {
            FadeStages.Changed -= OnChanged;
            GameState.Loaded -= Snap;
        }

        void Start() { Snap(); }

        void OnChanged(string place, int stage) { /* the target moves; Update animates toward it */ }

        /// <summary>Jump straight to the stage's look (room load, save load).</summary>
        public void Snap()
        {
            Ink = TargetInk;
            Apply();
        }

        void Update()
        {
            float target = TargetInk;
            if (Mathf.Abs(Ink - target) > 0.0005f)
            {
                // A bell wipes the page quickly; ink comes back at the drawing pace.
                float seconds = Ink > target && FadeStages.IsErased(GameState.World, _placeId) ? _eraseSeconds : _seconds;
                Ink = Mathf.MoveTowards(Ink, target, Time.deltaTime / Mathf.Max(0.01f, seconds));
                Apply();
            }
        }

        void Apply()
        {
            int stage = Stage;
            bool landed = !IsAnimating;
            foreach (var l in _layers)
            {
                var r = l.Renderer;
                if (r == null) continue;
                if (!l.Cached)
                {
                    var m = r.sharedMaterial;
                    l.IsInk = m != null && m.HasProperty(InkId);
                    l.BaseColor = m != null && m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white;
                    l.Cached = true;
                }
                // Dropout once the stage is there and the ink has finished leaving.
                bool dropped = stage >= l.DropoutStage && landed;
                r.enabled = !dropped;
                if (dropped) continue;
                r.GetPropertyBlock(_mpb);
                if (l.IsInk) _mpb.SetFloat(InkId, Ink);
                else _mpb.SetColor(BaseColorId, Color.Lerp(_paper, l.BaseColor, Mathf.Lerp(0.15f, 1f, Ink)));
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
