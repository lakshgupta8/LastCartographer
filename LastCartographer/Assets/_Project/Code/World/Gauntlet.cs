using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A traversal gauntlet (combat doc §9, CMB-18). Remembers the last solid ground Wren stood on; a hazard returns her
    /// there for one mask, never a full death (her last mask is not taken); the goal writes the gauntlet's flag and pays
    /// a scrap the first time.
    /// </summary>
    public sealed class Gauntlet : MonoBehaviour
    {
        public string Id;
        public Ability Needs;
        public Vector2 LastSafe;
        public int Falls { get; private set; }
        public bool Finished { get; private set; }
        public string FlagKey => Gauntlets.FlagKey(Id);
        public event Action<Gauntlet> Fell, Done;

        static bool _captioned;
        WrenController _wren;
        readonly RaycastHit2D[] _hits = new RaycastHit2D[4];

        void FixedUpdate()
        {
            if (_wren == null) _wren = FindFirstObjectByType<WrenController>();
            if (_wren == null || !_wren.IsGrounded) return;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Ground"), useTriggers = false };
            int n = Physics2D.Raycast(_wren.Position + Vector2.up * 0.1f, Vector2.down, filter, _hits, 0.4f);
            for (int i = 0; i < n; i++)
                if (_hits[i].collider.GetComponent<SolidGround>() != null) { LastSafe = _wren.Position; return; }
        }

        /// <summary>A hazard took her: a mask (never the last) and back to solid ground.</summary>
        public void Fall(WrenController wren)
        {
            Falls++;
            var vitals = wren.GetComponent<WrenVitals>();
            if (vitals != null && vitals.Masks > 1) vitals.Damage(1);
            wren.Teleport(LastSafe);
            if (!_captioned) { _captioned = true; Captions.Show("Back to solid ground.", 2.5f); }
            Fell?.Invoke(this);
        }

        public void Finish()
        {
            if (Finished) return;
            Finished = true;
            var w = GameState.World;
            if (!w.Is(FlagKey))
            {
                w.Set(FlagKey, true);
                w.Numbers.TryGetValue("$vellum_scraps", out var s);
                w.Numbers["$vellum_scraps"] = s + 1;
                var plan = Gauntlets.Find(Id);
                Captions.Show((plan != null ? plan.Name : Id) + ": crossed.", 3f);
            }
            Done?.Invoke(this);
        }
    }

    /// <summary>Ground the gauntlet counts as solid: a fall returns her to the last of it she stood on.</summary>
    public sealed class SolidGround : MonoBehaviour { }

    /// <summary>A zone that sends her back: water, thorns, a vent, the white, the long grass.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GauntletZone : MonoBehaviour
    {
        public enum Kind { Hazard, Goal }
        public Kind Role;
        public Gauntlet Gauntlet;
        BoxCollider2D _box;
        readonly Collider2D[] _hits = new Collider2D[4];

        void Awake() { _box = GetComponent<BoxCollider2D>(); _box.isTrigger = true; }

        void FixedUpdate()
        {
            if (Gauntlet == null) return;
            var b = _box.bounds;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Player"), useTriggers = false };
            int n = Physics2D.OverlapBox(b.center, b.size, 0f, filter, _hits);
            for (int i = 0; i < n; i++)
            {
                var wren = _hits[i].GetComponentInParent<WrenController>();
                if (wren == null) continue;
                if (Role == Kind.Hazard) Gauntlet.Fall(wren);
                else Gauntlet.Finish();
                return;
            }
        }
    }

    /// <summary>
    /// A platform that exists only inside Wren's lantern-radius (the Road That Stops, bible 4.6): solid and drawn when she
    /// carries Clarity and stands within reach of it, an outline otherwise. The radius will be the Clarity meter's (PRG-18).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class LanternPlatform : MonoBehaviour
    {
        public float Radius = 3.5f;
        public bool IsDrawn { get; private set; }
        BoxCollider2D _box;
        Renderer _visual;
        WrenController _wren;

        Vector3 _drawnScale;
        float _minX, _maxX;

        void Awake()
        {
            _box = GetComponent<BoxCollider2D>();
            _visual = GetComponentInChildren<Renderer>();
            if (_visual != null) _drawnScale = _visual.transform.localScale;
            // Its edges from the transform: a disabled collider has no bounds to ask.
            float half = _box.size.x * Mathf.Abs(transform.lossyScale.x) * 0.5f;
            float centre = transform.position.x + _box.offset.x * transform.lossyScale.x;
            _minX = centre - half;
            _maxX = centre + half;
            Set(false);
        }

        void FixedUpdate()
        {
            if (_wren == null) _wren = FindFirstObjectByType<WrenController>();
            bool drawn = false;
            if (_wren != null && _wren.Abilities != null && _wren.Abilities.Has(Ability.Clarity))
            {
                float dx = Mathf.Max(0f, Mathf.Max(_minX - _wren.Position.x, _wren.Position.x - _maxX));
                drawn = dx <= Radius + 0.001f;
            }
            if (drawn != IsDrawn) Set(drawn);
        }

        void Set(bool drawn)
        {
            IsDrawn = drawn;
            _box.enabled = drawn;
            if (_visual != null) _visual.transform.localScale = new Vector3(_drawnScale.x, drawn ? _drawnScale.y : 0.05f, drawn ? _drawnScale.z : 0.05f);   // outline
        }
    }
}
