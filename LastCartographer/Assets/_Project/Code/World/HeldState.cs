using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The visible held state of an anchored place (bible 10, PRG-13): the colour grade locks (a global the
    /// paper-grain pass reads) and the Guild's Wardens patrol. Sits on a room; Wardens are placed in the room
    /// inactive and come on with the anchoring. Held places (the Holdfast way) get neither.
    /// </summary>
    public sealed class HeldState : MonoBehaviour
    {
        public const string HeldGlobal = "_OWSBG_Held";

        [SerializeField] string _placeId;
        [SerializeField] List<GameObject> _wardens = new List<GameObject>();
        [SerializeField] float _seconds = 2f;

        static readonly int HeldId = Shader.PropertyToID(HeldGlobal);

        public string PlaceId { get => _placeId; set => _placeId = value; }
        public IReadOnlyList<GameObject> Wardens => _wardens;
        public bool IsAnchored => Places.FateOf(GameState.World, _placeId) == PlaceFate.Anchored;
        /// <summary>0 free, 1 fully held; animates.</summary>
        public float Level { get; private set; }
        public static float CurrentLevel { get; private set; }

        public void AddWarden(GameObject w) { _wardens.Add(w); }

        void Awake()
        {
            if (string.IsNullOrEmpty(_placeId))
            {
                var room = GetComponentInParent<Room>();
                if (room != null) _placeId = room.RoomId;
            }
        }

        void OnEnable()
        {
            Places.FateChanged += OnFate;
            GameState.Loaded += Snap;
            Snap();
        }

        void OnDisable()
        {
            Places.FateChanged -= OnFate;
            GameState.Loaded -= Snap;
            SetGlobal(0f);   // leaving the room takes the grade with it
        }

        void OnFate(string place, PlaceFate fate) { if (place == _placeId) SetWardens(fate == PlaceFate.Anchored); }

        void Snap()
        {
            bool held = IsAnchored;
            Level = held ? 1f : 0f;
            SetWardens(held);
            SetGlobal(Level);
        }

        void SetWardens(bool on)
        {
            foreach (var w in _wardens) if (w != null) w.SetActive(on);
        }

        void Update()
        {
            float target = IsAnchored ? 1f : 0f;
            if (Mathf.Abs(Level - target) < 0.0005f) return;
            Level = Mathf.MoveTowards(Level, target, Time.deltaTime / Mathf.Max(0.01f, _seconds));
            SetGlobal(Level);
        }

        /// <summary>Lock or free the grade from outside a held room (Voss anchors the Threshold's arena, boss 6.11).</summary>
        public static void LockGrade(float level) { SetGlobal(Mathf.Clamp01(level)); }

        static void SetGlobal(float level)
        {
            CurrentLevel = level;
            Shader.SetGlobalFloat(HeldId, level);
        }
    }
}
