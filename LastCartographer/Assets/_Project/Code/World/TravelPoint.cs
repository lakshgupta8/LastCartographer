using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A place Wren can travel from and to (GDD 5, DES-02): a drafting desk, or a lamp once it is lit. Standing
    /// in the trigger makes it the atlas's origin and puts it on the page (Atlas.Discover). A lamp is lit once
    /// its vantage has been drawn (the Lamp-Keeper's beacon); an unlit lamp is nothing yet.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class TravelPoint : MonoBehaviour
    {
        [SerializeField] string _waypointId;
        [SerializeField] WaypointKind _kind = WaypointKind.Desk;
        [SerializeField] string _displayName = "the desk";
        [SerializeField] string _spawn = "Desk";
        [Tooltip("Lamps: lit once this vantage has ever been drawn.")]
        [SerializeField] string _litByVantage = "";
        [SerializeField] Renderer _glow;
        [Tooltip("Lamps: the light it casts once lit (ENV-10).")]
        [SerializeField] Light _light;

        /// <summary>The point Wren is standing at, or null.</summary>
        public static TravelPoint Nearby { get; private set; }
        public static event Action<TravelPoint> NearbyChanged;

        public string WaypointId => _waypointId;
        public WaypointKind Kind => _kind;
        /// <summary>The atlas's name for it, in the player's language (NAR-18).</summary>
        public string DisplayName => Atlas.WaypointName(_waypointId);
        public string Spawn => _spawn;
        public string PlaceId { get { var room = GetComponentInParent<Room>(); return room != null ? room.RoomId : ""; } }
        public bool IsLit => _kind == WaypointKind.Desk || string.IsNullOrEmpty(_litByVantage) || GameState.World.IsEverSurveyed(_litByVantage);
        public Waypoint Definition => Atlas.FindWaypoint(_waypointId);
        public Light Light => _light;

        /// <summary>Setup and tests: the light a lamp casts once it is lit.</summary>
        public void ConfigureLight(Light light) { _light = light; if (_light != null) _light.enabled = IsLit; }

        WrenController _wren;

        void Awake()
        {
            if (string.IsNullOrEmpty(_waypointId)) _waypointId = (_kind == WaypointKind.Desk ? "desk." : "lamp.") + PlaceId;
            var known = Atlas.FindWaypoint(_waypointId);   // the catalog's name wins: it is what the tables are keyed from
            Atlas.RegisterWaypoint(new Waypoint
            {
                Id = _waypointId, Kind = _kind, Place = PlaceId, Room = gameObject.scene.name, Spawn = _spawn,
                Name = known != null && !string.IsNullOrEmpty(known.Name) ? known.Name : _displayName,
            });
        }

        void Reset() { GetComponent<Collider2D>().isTrigger = true; }

        void OnDisable()
        {
            if (Nearby != this) return;
            Nearby = null;
            _wren = null;
            NearbyChanged?.Invoke(null);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var c = other.GetComponentInParent<WrenController>();
            if (c == null) return;
            _wren = c;
            if (Nearby != this) { Nearby = this; NearbyChanged?.Invoke(this); }
            if (IsLit) Atlas.Discover(GameState.World, _waypointId);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (_wren == null || other.GetComponentInParent<WrenController>() != _wren) return;
            _wren = null;
            if (Nearby == this) { Nearby = null; NearbyChanged?.Invoke(null); }
        }

        void Update()
        {
            bool lit = IsLit;
            if (_glow != null) _glow.enabled = lit;
            if (_light != null && _light.enabled != lit) _light.enabled = lit;
        }
    }
}
