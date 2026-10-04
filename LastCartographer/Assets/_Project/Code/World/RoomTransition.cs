using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A trigger at a room edge. Wren touching it loads the neighbour, if the way is open: the transition carries the
    /// gate the map puts on this way (<see cref="Gate"/>: an ability, a story flag, soft or hard) and, while the gate
    /// is shut, stands its <see cref="Bar"/> in the way, solid ground she can't pass. A soft gap never bars. The bar
    /// follows the world: a flag set or an ability learned opens it at once, and a loaded game rebinds.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class RoomTransition : MonoBehaviour
    {
        public string TargetScene;
        public string TargetSpawn;
        // The gate (world-map.md §2, docs/design/gates.md). Needs is the ability, Flag the story flag, Soft whether skill may cross without the ability.
        public Ability Needs = Ability.None;
        public string Flag = "";
        public bool Soft;
        [SerializeField] GameObject _bar;

        static float _lastBump = -10f;
        WorldState _world;

        /// <summary>The solid thing in the way while the gate is shut (null for a way with no gate, or a soft one).</summary>
        public GameObject Bar { get => _bar; set => _bar = value; }
        public Gate Gate => new Gate { Needs = Needs, Flag = string.IsNullOrEmpty(Flag) ? null : Flag, Soft = Soft };
        public bool HasGate => !Gate.IsNone;
        /// <summary>Whether she may pass now, with what she has and what the world says.</summary>
        public bool IsOpen => Gate.IsOpen(AbilitySet.FromWorld(GameState.World), GameState.World.Is);
        public bool IsBarred => _bar != null && _bar.activeSelf;

        void Reset() { GetComponent<Collider2D>().isTrigger = true; }

        void OnEnable()
        {
            GameState.Loaded += Rebind;
            Rebind();
        }

        void OnDisable()
        {
            GameState.Loaded -= Rebind;
            if (_world != null) _world.FlagChanged -= OnFlag;
            _world = null;
        }

        void Rebind()
        {
            if (_world != null) _world.FlagChanged -= OnFlag;
            _world = GameState.World;
            if (_world != null) _world.FlagChanged += OnFlag;
            Apply();
        }

        void OnFlag(string key, int value) { if (HasGate) Apply(); }

        /// <summary>Stand the bar if the gate is shut, take it away if it is open.</summary>
        public void Apply()
        {
            if (_bar == null) return;
            bool shut = Gate.Bars && !IsOpen;
            if (_bar.activeSelf != shut) _bar.SetActive(shut);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var wren = other.GetComponentInParent<WrenController>();
            if (wren == null) return;
            if (!IsOpen) { Bump(wren); return; }
            RoomManager.Instance?.Transition(TargetScene, TargetSpawn);
        }

        /// <summary>She pressed on the bar (<see cref="GateBar"/>), or reached a shut way: one line, not one every frame.</summary>
        public void Bump(WrenController wren)
        {
            if (Time.time - _lastBump < 2f) return;
            _lastBump = Time.time;
            Captions.Show(ShutLine, 2.5f);
            Bumped?.Invoke(this);
        }

        /// <summary>She pressed on a shut way (once per two seconds): a knock on the page (AUD-11).</summary>
        public static event System.Action<RoomTransition> Bumped;

        /// <summary>What the bar says: the ability the way wants if she lacks it, else that the story has not opened it.</summary>
        public string ShutLine
        {
            get
            {
                bool lacks = Needs != Ability.None && (AbilitySet.FromWorld(GameState.World) & Needs) != Needs;
                return lacks
                    ? Loc.F("gate.needs", "Not without {0}.", AbilityNames.Of(Needs))
                    : Loc.T("gate.shut", "The way is shut. Not yet.");
            }
        }
    }
}
