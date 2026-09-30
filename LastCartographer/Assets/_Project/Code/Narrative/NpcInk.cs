using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>A townsfolk's colour state (CHR-11): drawn in full, fading with its place, or a Remnant.</summary>
    public enum NpcInkState
    {
        /// <summary>Their own colours.</summary>
        Drawn = 0,
        /// <summary>The fills wash toward paper with the place's fade stage; the line stays.</summary>
        Fading = 1,
        /// <summary>The ink removed: paper through the fills, the line grey (art-direction 4).</summary>
        Remnant = 2,
    }

    /// <summary>
    /// Colours a drawn townsfolk by its state (docs/design/npc-animation.md §3). People fade by their own rules
    /// (fade-stages.md): the fills wash toward paper as the place's stage climbs but never leave while the place
    /// stands, and a person of a released place, or anyone met on an island in the Blank, or one the setup
    /// marks a Remnant (Ilse, Corra, Aury, Corvin, Marrow) is drawn with the ink removed. Applied through the
    /// renderer's property block (_Wash, _LineFade on InkSprite), so the sheet player's frame window is kept.
    /// </summary>
    public sealed class NpcInk : MonoBehaviour
    {
        /// <summary>How far the fills wash at the last stage before erasure (a fading person keeps a little colour).</summary>
        public const float FadingWashMax = 0.7f;

        [SerializeField] NpcInkState _rest = NpcInkState.Drawn;
        [SerializeField] bool _followPlace = true;
        [SerializeField] string _placeId;
        [SerializeField] Renderer _renderer;

        static readonly int WashId = Shader.PropertyToID("_Wash");
        static readonly int LineFadeId = Shader.PropertyToID("_LineFade");
        MaterialPropertyBlock _mpb;
        float _wash = -1f, _lineFade = -1f;

        public NpcInkState Rest { get => _rest; set => _rest = value; }
        public bool FollowPlace { get => _followPlace; set => _followPlace = value; }
        public NpcInkState State { get; private set; }
        public float Wash => Mathf.Max(0f, _wash);
        public float LineFade => Mathf.Max(0f, _lineFade);
        public string PlaceId
        {
            get
            {
                if (!string.IsNullOrEmpty(_placeId)) return _placeId;
                var room = GetComponentInParent<Room>();
                return room != null ? room.RoomId : "";
            }
            set => _placeId = value;
        }

        /// <summary>The state for a rest state and a place: a Remnant stays one; a released place or an island makes one; else the stage says.</summary>
        public static NpcInkState Resolve(NpcInkState rest, PlaceFate fate, int stage, bool island)
        {
            if (rest == NpcInkState.Remnant || island || fate == PlaceFate.Released) return NpcInkState.Remnant;
            if (rest == NpcInkState.Fading || stage > 0) return NpcInkState.Fading;
            return NpcInkState.Drawn;
        }

        /// <summary>The shader's wash and line fade for a state at a stage: fading washes by stage / max (never past FadingWashMax); a Remnant is all the way.</summary>
        public static (float wash, float lineFade) Amounts(NpcInkState state, int stage)
        {
            switch (state)
            {
                case NpcInkState.Remnant: return (1f, 1f);
                case NpcInkState.Fading: return (FadingWashMax * Mathf.Clamp01(stage / (float)FadeStages.Max), 0f);
                default: return (0f, 0f);
            }
        }

        void Awake()
        {
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        void OnEnable() { _wash = _lineFade = -1f; }

        void LateUpdate()
        {
            var place = PlaceId;
            var fate = _followPlace ? Places.FateOf(GameState.World, place) : PlaceFate.Unwritten;
            int stage = _followPlace ? FadeStages.Get(GameState.World, place) : 0;
            bool island = _followPlace && Islands.IsIslandScene(place);
            State = Resolve(_rest, fate, stage, island);
            var (wash, line) = Amounts(State, stage);
            if (Mathf.Approximately(wash, _wash) && Mathf.Approximately(line, _lineFade)) return;
            _wash = wash; _lineFade = line;
            if (_renderer == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(WashId, wash);
            _mpb.SetFloat(LineFadeId, line);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
