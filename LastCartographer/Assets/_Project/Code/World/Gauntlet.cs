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
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.Ground, useTriggers = false };
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
            if (!_captioned) { _captioned = true; Captions.Show(Loc.T("caption.gauntlet_fall", "Back to solid ground."), 2.5f); }
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
                Captions.Show(Loc.F("caption.gauntlet_crossed", "{0}: crossed.", plan != null ? Gauntlets.NameOf(plan) : Id), 3f);
            }
            Done?.Invoke(this);
        }
    }
}
