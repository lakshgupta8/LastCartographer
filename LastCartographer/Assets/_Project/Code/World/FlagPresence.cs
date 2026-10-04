using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Someone who stands in a room only while the world says so: there once a flag is set (<see cref="Requires"/>, to
    /// <see cref="RequiresValue"/> or to anything but 0), and gone once another flag reads a value (<see cref="HiddenWhen"/>,
    /// <see cref="HiddenValue"/>; 0 means any value). The widow at the tether-post: there once the fourth lamp is lit, gone
    /// once she went in. Sable at the Bone Bridge: there while the whale's commission reads taken, gone once she has rowed.
    /// The object stays so it keeps listening; its children (the drawing), its trigger and its talker are what come and go.
    /// </summary>
    public sealed class FlagPresence : MonoBehaviour
    {
        [SerializeField] string _requires;
        [SerializeField] int _requiresValue;
        [SerializeField] string _hiddenWhen;
        [SerializeField] int _hiddenValue;

        WorldState _world;

        public string Requires => _requires;
        public int RequiresValue => _requiresValue;
        public string HiddenWhen => _hiddenWhen;
        public int HiddenValue => _hiddenValue;
        public bool Standing { get; private set; } = true;

        public void Configure(string requires, string hiddenWhen = null, int hiddenValue = 0, int requiresValue = 0)
        {
            _requires = requires; _requiresValue = requiresValue; _hiddenWhen = hiddenWhen; _hiddenValue = hiddenValue;
            if (isActiveAndEnabled) Apply();
        }

        public bool ShouldStand(WorldState w)
        {
            if (w == null) return true;
            if (!string.IsNullOrEmpty(_requires) && (_requiresValue == 0 ? !w.Is(_requires) : w.Get(_requires) != _requiresValue)) return false;
            if (!string.IsNullOrEmpty(_hiddenWhen))
            {
                int v = w.Get(_hiddenWhen);
                if (_hiddenValue == 0 ? v != 0 : v == _hiddenValue) return false;
            }
            return true;
        }

        void OnEnable()
        {
            GameState.Loaded += OnLoaded;
            Listen(GameState.World);
            Apply();
        }

        void OnDisable()
        {
            GameState.Loaded -= OnLoaded;
            Listen(null);
        }

        void OnLoaded() { Listen(GameState.World); Apply(); }

        void Listen(WorldState w)
        {
            if (_world != null) _world.FlagChanged -= OnFlag;
            _world = w;
            if (_world != null) _world.FlagChanged += OnFlag;
        }

        void OnFlag(string key, int value)
        {
            if (key == _requires || key == _hiddenWhen) Apply();
        }

        public void Apply()
        {
            bool on = ShouldStand(GameState.World);
            Standing = on;
            foreach (Transform child in transform) child.gameObject.SetActive(on);
            foreach (var col in GetComponents<Collider2D>()) col.enabled = on;
            foreach (var b in GetComponents<MonoBehaviour>()) if (b != this) b.enabled = on;
        }
    }
}
