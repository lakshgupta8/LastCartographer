using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The Choir (bible 6.6, boss sheet 6.6, CMB-13). Tier II, optional: three Cantor doves in unison over Aldermere's
    /// square on its last evening, one health between them. Each finished ring hurts under the bell, erases one of the
    /// square's platforms and erases Aldermere from the atlas (re-survey between verses). Phase 1: three doves ring in
    /// turn. Phase 2: two, in canon (the second starts halfway through the first). Phase 3: one dove, alone. A hit on a
    /// ringing dove stops its bell (the Cantor rule); the doves hover in a line, so a Longstroke finds all of them.
    /// Only the doves can be struck: the Choir itself is the song.
    /// </summary>
    public sealed class Choir : Boss
    {
        [Header("Choir")]
        public float floorY = 0f;
        public float centreX = 9f;
        public float hoverY = 2.6f, doveSpacing = 4f;
        public Vector2 doveSize = new Vector2(0.9f, 0.8f);
        public int ringFrames = 32;
        public int canonOffsetFrames = 16;
        public float gapSeconds = 0.9f, aloneGapSeconds = 0.55f;
        public float bellRadius = 3.2f;
        public int bellDamage = 1;
        /// <summary>The place the bells erase: Aldermere's square.</summary>
        public string placeId = "Verdance_Aldermere_2";
        /// <summary>The square's platforms, erased one per finished ring; set by the kit.</summary>
        public List<GameObject> platforms = new List<GameObject>();

        public const int DoveCount = 3;
        public IReadOnlyList<BossPart> Doves => _doves;
        public int Tolls { get; private set; }
        public int Cancelled { get; private set; }
        public int Erasures { get; private set; }
        public int PlatformsErased { get { int n = 0; foreach (var p in platforms) if (p != null && !p.activeSelf) n++; return n; } }
        /// <summary>Three doves, then two, then one.</summary>
        public int ActiveDoves => Phase >= 3 ? 1 : Phase >= 2 ? 2 : DoveCount;
        public int RingFrames => Mathf.Max(ringFrames, MinTelegraphFrames);

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            yield return new BossAttack("Bell", AttackKind.Window, RingFrames, bellDamage, BossAttack.All);
        }

        readonly List<BossPart> _doves = new List<BossPart>();
        readonly int[] _ring = { -1, -1, -1 };   // frames into each dove's ring, -1 when quiet
        int _turn;
        float _gap;
        bool _routing, _canonDue;

        public bool IsDoveActive(int i) => i >= 0 && i < _doves.Count && _doves[i] != null && _doves[i].gameObject.activeSelf;
        public bool IsRinging(int i) => i >= 0 && i < _ring.Length && _ring[i] >= 0;
        public int RingingCount { get { int n = 0; foreach (var r in _ring) if (r >= 0) n++; return n; } }
        /// <summary>Dove 0 hangs in the middle and is the last to sing; 1 west, 2 east.</summary>
        public Vector2 DoveHome(int i) => new Vector2(centreX + (i == 0 ? 0f : i == 1 ? -doveSpacing : doveSpacing), hoverY);

        protected override bool ContactHurts => false;
        protected override bool AcceptsHit(in HitInfo hit) => _routing;
        /// <summary>The Choir is the song: the doves are the drawing (CHR-08).</summary>
        protected override bool Bodiless => true;
        public override IEnumerable<string> PartSkinNames { get { yield return "ChoirDove"; } }
        /// <summary>How far a dove is through its ring, 0..1; its bell rises through the ring clip.</summary>
        public float RingProgress(int dove) => IsRinging(dove) ? Mathf.Clamp01((float)_ring[dove] / Mathf.Max(1, RingFrames)) : 0f;

        protected override void Start()
        {
            base.Start();
            EnsureDoves();
        }

        void EnsureDoves()
        {
            if (_doves.Count == DoveCount) return;
            for (int i = 0; i < DoveCount; i++)
            {
                int k = i;
                var d = BossPart.Make("Dove_" + i, transform.parent, DoveHome(i), doveSize, DoveMaterial(false));
                d.OnHit = hit => HitDove(k, hit);
                Skin(d, "ChoirDove");
                _doves.Add(d);
            }
        }

        static Material DoveMaterial(bool ringing) => ringing
            ? InkMaterials.Lit("Choir_Dove_Ringing", new Color(0.98f, 0.93f, 0.70f))
            : InkMaterials.Lit("Choir_Dove", new Color(0.88f, 0.86f, 0.82f));

        /// <summary>Tests and tooling: start this dove's bell now.</summary>
        public void ForceRing(int dove)
        {
            if (!IsDoveActive(dove)) return;
            _ring[dove] = 0;
            _doves[dove].SetMaterial(DoveMaterial(true));
            _doves[dove].Play("ring", true);
        }

        bool HitDove(int i, HitInfo hit)
        {
            if (!IsDoveActive(i) || !IsFightActive) return false;
            bool ringing = IsRinging(i);
            _routing = true;
            bool landed;
            try { landed = TakeHit(hit); }
            finally { _routing = false; }
            if (landed && ringing && IsRinging(i))
            {
                Cancelled++;
                Quiet(i);
            }
            return landed;
        }

        protected override void OnFightStarted()
        {
            EnsureDoves();
            for (int i = 0; i < DoveCount; i++) { Quiet(i); SetDove(i, true); }
            _turn = 0;
            _gap = gapSeconds;
            _canonDue = false;
        }

        protected override void OnFightReset()
        {
            for (int i = 0; i < _doves.Count; i++) { Quiet(i); SetDove(i, true); }
            foreach (var p in platforms) if (p != null) p.SetActive(true);
        }

        protected override void OnPhaseStarted(int phase)
        {
            // Doves leave from the east: two in canon, then the middle one alone.
            for (int i = 0; i < _doves.Count; i++)
            {
                bool stays = i < ActiveDoves;
                if (!stays) Quiet(i);
                SetDove(i, stays);
            }
            _turn = 0;
            _gap = phase >= 3 ? aloneGapSeconds : gapSeconds;
            _canonDue = false;
        }

        protected override void OnDefeated()
        {
            for (int i = 0; i < _doves.Count; i++) { Quiet(i); SetDove(i, false); }
        }

        void SetDove(int i, bool on)
        {
            if (i < _doves.Count && _doves[i] != null) _doves[i].gameObject.SetActive(on);
        }

        void Quiet(int i)
        {
            _ring[i] = -1;
            if (i < _doves.Count && _doves[i] != null) { _doves[i].SetMaterial(DoveMaterial(false)); _doves[i].Play("idle"); }
        }

        protected override void Tick(float dt)
        {
            // Bob in the evening air.
            for (int i = 0; i < _doves.Count; i++)
            {
                if (!IsDoveActive(i)) continue;
                _doves[i].MoveTo(DoveHome(i) + Vector2.up * Mathf.Sin(Time.time * 2f + i) * 0.1f);
                if (IsRinging(i)) _doves[i].Seek(RingProgress(i));   // the bell rises through the ring
            }

            // The song: advance each bell; a finished ring tolls.
            for (int i = 0; i < DoveCount; i++)
            {
                if (!IsRinging(i)) continue;
                if (!IsDoveActive(i)) { Quiet(i); continue; }
                _ring[i]++;
                if (_ring[i] >= RingFrames) Toll(i);
            }

            // Phase 2's canon: the second voice comes in halfway through the first.
            if (_canonDue)
            {
                for (int i = 0; i < ActiveDoves; i++)
                {
                    if (!IsRinging(i) || _ring[i] < canonOffsetFrames) continue;
                    int other = NextDove(i);
                    if (other != i && !IsRinging(other)) ForceRing(other);
                    _canonDue = false;
                    break;
                }
                if (RingingCount == 0) _canonDue = false;
            }

            if (RingingCount > 0) return;
            _gap -= dt;
            if (_gap > 0f) return;
            int d = _turn % ActiveDoves;
            _turn++;
            ForceRing(d);
            _canonDue = Phase == 2 && ActiveDoves > 1;
            _gap = Phase >= 3 ? aloneGapSeconds : gapSeconds;
        }

        int NextDove(int i) => (i + 1) % Mathf.Max(1, ActiveDoves);

        void Toll(int i)
        {
            Tolls++;
            var at = _doves[i].Position;
            Quiet(i);
            HitWrenInCircle(at, bellRadius, bellDamage);
            foreach (var p in platforms)
                if (p != null && p.activeSelf) { p.SetActive(false); break; }
            if (Atlas.Erase(GameState.World, placeId))
            {
                Erasures++;
                Captions.Show(Loc.F("caption.erased", "Erased: {0}. Draw it again.", Atlas.PlaceName(placeId)), 3f);
            }
        }

        protected override Color TintColor() => new Color(0.88f, 0.86f, 0.82f);
    }
}
