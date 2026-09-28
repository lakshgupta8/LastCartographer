using System;
using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The lantern-radius as the picture sees it (PRG-18). Wren's <see cref="ClarityMeter"/> publishes it every frame; a
    /// fight that takes her radius from her (the Half-Cathedral Bells) holds it instead until it lets go. Three shader
    /// globals for the paper pass: how much of the room is drawn round her lantern, the radius in world units, and her
    /// centre on screen (viewport x, y, the radius in viewport heights, the aspect).
    /// </summary>
    public static class Lantern
    {
        public const string StrengthGlobal = "_OWSBG_Lantern";
        public const string RadiusGlobal = "_OWSBG_LanternRadius";
        public const string CentreGlobal = "_OWSBG_LanternCentre";
        internal static readonly int StrengthId = Shader.PropertyToID(StrengthGlobal);
        internal static readonly int RadiusId = Shader.PropertyToID(RadiusGlobal);
        internal static readonly int CentreId = Shader.PropertyToID(CentreGlobal);

        static UnityEngine.Object _holder;
        static float _held;

        /// <summary>Whether something holds the radius now (a destroyed holder lets go by itself).</summary>
        public static bool IsHeld => _holder != null;
        public static float Held => _held;

        public static void Hold(UnityEngine.Object holder, float radius)
        {
            _holder = holder;
            _held = radius;
            Shader.SetGlobalFloat(RadiusId, radius);
        }

        public static void Release(UnityEngine.Object holder) { if (_holder == holder) _holder = null; }
    }

    /// <summary>
    /// Clarity (bible 10, combat doc §3, PRG-18): Wren's capacity to be in the Blank untethered. Inside an
    /// <see cref="UntetheredZone"/> (a white patch, the white between islands) or the drift, the meter runs down a second
    /// a second; a lost Remnant's touch takes more. Anywhere else it fills again, and the last ground she stood on there is
    /// where the white gives her back: when the meter empties she is drawn back to it for a mask, never her last. Without
    /// Clarity the meter is empty and the white gives her back at once: the gate.
    ///
    /// The meter is also her lantern-radius. The level sets its width; below half the meter it narrows; the Field lantern
    /// widens it while it burns. In the Greyfold and the Blank (or a white patch anywhere) the paper pass draws everything
    /// beyond it white, with outlines only at the edge of the eye.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WrenController))]
    public sealed class ClarityMeter : MonoBehaviour, IRevealable
    {
        [SerializeField] float _strengthPerSecond = 2f;

        public int Level { get; private set; }
        public float Capacity => Clarity.Capacity(Level);
        public float Seconds { get; private set; }
        /// <summary>How full, 0..1. Without Clarity: full where she is tethered, empty where she is not.</summary>
        public float Fraction => Capacity <= 0f ? (IsUntethered ? 0f : 1f) : Mathf.Clamp01(Seconds / Capacity);
        public bool IsFull => Seconds >= Capacity - 0.0001f;
        public bool IsUntethered { get; private set; }
        /// <summary>The radius she sees by, in world units: held by a fight, or her own.</summary>
        public float Radius { get; private set; }
        /// <summary>Whether this room (or the patch she stands in) is drawn round her lantern, and how far the picture has got there.</summary>
        public bool IsLanternLit { get; private set; }
        public float LanternStrength { get; private set; }
        public bool IsRevealed => _revealLeft > 0f;
        public Vector2 LastTethered { get; set; }
        public int Empties { get; private set; }
        public event Action<ClarityMeter> Emptied, Grew;

        WrenController _ctrl;
        WrenVitals _vitals;
        bool _started;
        float _revealLeft;
        string _roomId;

        void Awake() { _ctrl = GetComponent<WrenController>(); }

        void Start()
        {
            Level = ComputeLevel();
            Seconds = Capacity;
            LastTethered = _ctrl.Position;
            Radius = OwnRadius();
            _started = true;
        }

        void OnDisable() { Shader.SetGlobalFloat(Lantern.StrengthId, 0f); }

        int ComputeLevel()
        {
            var a = _ctrl != null ? _ctrl.Abilities : null;
            return Clarity.Level(a != null && a.Has(Ability.Clarity), GameState.World);
        }

        /// <summary>A lost Remnant's touch, or anything else that takes clarity: seconds off the meter.</summary>
        public void Strike(float seconds) { if (seconds > 0f) Seconds = Mathf.Max(0f, Seconds - seconds); }

        /// <summary>The Field lantern: wider for as long as it burns.</summary>
        public void Reveal(float seconds) { _revealLeft = Mathf.Max(_revealLeft, seconds); }

        /// <summary>Tests and tooling: the meter full again.</summary>
        public void Refill() { Seconds = Capacity; }

        void FixedUpdate()
        {
            if (!_started) return;
            if (_vitals == null) _vitals = GetComponent<WrenVitals>();
            float dt = Time.fixedDeltaTime;
            if (_revealLeft > 0f) _revealLeft -= dt;

            int level = ComputeLevel();
            if (level != Level)
            {
                bool grew = level > Level;
                Level = level;
                if (grew)
                {
                    Seconds = Capacity;
                    if (level > 1) Captions.Show("Clarity grows.", 3f);
                    Grew?.Invoke(this);
                }
            }
            if (Seconds > Capacity) Seconds = Capacity;

            var roomId = Room.Current != null ? Room.Current.RoomId : null;
            if (roomId != _roomId)
            {
                // A new room: where she came in is held ground until she finds better.
                _roomId = roomId;
                LastTethered = _ctrl.Position;
            }
            IsUntethered = InUntetheredZone() || Clarity.IsUntetheredRoom(roomId);
            if (IsUntethered)
            {
                Seconds = Mathf.Max(0f, Seconds - Clarity.DrainPerSecond * dt);
                if (Seconds <= 0f) Empty();
            }
            else
            {
                Seconds = Mathf.Min(Capacity, Seconds + Clarity.RefillPerSecond * dt);
                if (_ctrl.IsGrounded) LastTethered = _ctrl.Position;
            }
        }

        bool InUntetheredZone()
        {
            var centre = _ctrl.Position + Vector2.up * 0.55f;
            foreach (var z in UntetheredZone.All) if (z.Contains(centre)) return true;
            return false;
        }

        void Empty()
        {
            Empties++;
            if (_vitals != null && _vitals.Masks > 1) _vitals.Damage(1);
            _ctrl.Teleport(LastTethered);
            IsUntethered = false;
            Seconds = Capacity;
            Captions.Show(Level == 0 ? "The white will not hold her. Not yet." : "Drawn back to held ground.", 2.5f);
            Emptied?.Invoke(this);
        }

        float OwnRadius() => Clarity.Radius(Level, Fraction) + (IsRevealed ? Clarity.LanternExtends : 0f);

        void LateUpdate()
        {
            if (!_started) return;
            Radius = Lantern.IsHeld ? Lantern.Held : OwnRadius();
            var roomId = Room.Current != null ? Room.Current.RoomId : null;
            IsLanternLit = Clarity.IsLanternLit(roomId) || IsUntethered || InUntetheredZone();
            LanternStrength = Mathf.MoveTowards(LanternStrength, IsLanternLit ? 1f : 0f, _strengthPerSecond * Time.deltaTime);
            Publish();
        }

        /// <summary>Tests: the picture as it would be once it has settled, without waiting for it.</summary>
        public void SettleLantern() { LanternStrength = IsLanternLit ? 1f : 0f; Publish(); }

        void Publish()
        {
            float strength = LanternStrength;
            var centre = new Vector4(0.5f, 0.5f, 1f, 16f / 9f);
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 c = _ctrl.Position + Vector2.up * 0.55f;
                var vc = cam.WorldToViewportPoint(c);
                var ve = cam.WorldToViewportPoint(c + Vector3.up * Radius);
                centre = new Vector4(vc.x, vc.y, Mathf.Abs(ve.y - vc.y), cam.aspect);
                if (vc.z <= 0f) strength = 0f;
            }
            else strength = 0f;
            Shader.SetGlobalFloat(Lantern.StrengthId, strength);
            Shader.SetGlobalFloat(Lantern.RadiusId, Radius);
            Shader.SetGlobalVector(Lantern.CentreId, centre);
        }
    }

    /// <summary>
    /// Ground the white has: a patch where she is untethered and the Clarity meter runs down (the white between the
    /// Blank's islands, the drift, the white patches hidden across every region). The room is drawn round her lantern
    /// while she stands in one.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class UntetheredZone : MonoBehaviour
    {
        public static readonly List<UntetheredZone> All = new List<UntetheredZone>();
        BoxCollider2D _box;

        void Awake() { _box = GetComponent<BoxCollider2D>(); _box.isTrigger = true; }
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public bool Contains(Vector2 p) => _box != null && _box.enabled && _box.OverlapPoint(p);

        public static UntetheredZone Make(string name, Transform parent, Vector2 centre, Vector2 size)
        {
            var go = new GameObject(name);
            int layer = LayerMask.NameToLayer("Trigger");
            if (layer >= 0) go.layer = layer;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(centre.x, centre.y, 0f);
            go.AddComponent<BoxCollider2D>().size = size;
            return go.AddComponent<UntetheredZone>();
        }
    }
}
