using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A piece of environmental storytelling that changes with its place (ENV-06, <c>docs/story/environment.md</c>):
    /// two drawings under one object, and the world decides which stands. The cups turn right side up after the
    /// walk, the gallery's lamps light, Merrow's lintels take chalk when the place is held, Lowmarket's board says
    /// COMPLETE once anchored, a door opens on its flag. The Yarn scene that reads the piece branches on the same
    /// <see cref="DressingChange"/>, so what she sees and what she reads never disagree. Reading changes nothing;
    /// only the world does.
    /// </summary>
    public sealed class DressingProp : MonoBehaviour
    {
        [SerializeField] string _piece;
        [SerializeField] GameObject _before;
        [SerializeField] GameObject _after;
        [SerializeField] DressingChange _change;

        WorldState _bound;

        /// <summary>The catalog piece (or the asker) this stands for.</summary>
        public string Piece => _piece;
        public GameObject Before => _before;
        public GameObject After => _after;
        public DressingChange Change => _change;
        /// <summary>Whether the second drawing stands now.</summary>
        public bool IsChanged { get; private set; }

        public void Configure(string piece, GameObject before, GameObject after, DressingChange change)
        {
            _piece = piece; _before = before; _after = after; _change = change;
        }

        void OnEnable()
        {
            GameState.Loaded += Rebind;
            Rebind();
        }

        void OnDisable()
        {
            GameState.Loaded -= Rebind;
            Unbind();
        }

        void Rebind()
        {
            Unbind();
            _bound = GameState.World;
            if (_bound != null) _bound.FlagChanged += OnFlag;
            Apply();
        }

        void Unbind()
        {
            if (_bound == null) return;
            _bound.FlagChanged -= OnFlag;
            _bound = null;
        }

        void OnFlag(string key, int value) => Apply();

        /// <summary>Show the drawing the world calls for.</summary>
        public void Apply()
        {
            IsChanged = _change.IsMet(GameState.World);
            if (_before != null && _before.activeSelf == IsChanged) _before.SetActive(!IsChanged);
            if (_after != null && _after.activeSelf != IsChanged) _after.SetActive(IsChanged);
        }
    }
}
