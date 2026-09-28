using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The Half-Cathedral Bells (bible 6.12, boss sheet 6.12, CMB-16). Tier III, environmental: the nave of the faded
    /// cathedral, white. Each ring shrinks the radius Wren can see (her lantern-radius); once it is as small as it goes,
    /// a ring costs a mask. She silences a bell by cutting its rope, and a rope can only be cut when she can see it:
    /// within her radius, or while her Field lantern shows everything. Standing still at the vantage Isolde taught her
    /// steadies the radius back to full. Phase 1: one bell. Phase 2: two, in canon. Phase 3: the great bell, whose rope
    /// is in the white and shows only to a wide radius or the lantern. The bells are the boss: four ropes, four cuts.
    /// </summary>
    public sealed class HalfCathedralBells : Boss, IRevealable
    {
        public const string RadiusGlobal = Lantern.RadiusGlobal;
        public const int RopeCount = 4;

        [Header("The Bells")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        /// <summary>The four ropes: phase 1's, phase 2's two, the great bell's; set by the kit.</summary>
        public List<float> ropeXs = new List<float>();
        public float ropeHeight = 6f;
        public float vantageX = 1.5f;
        public float fullRadius = 7f, minRadius = 1.2f, shrinkPerRing = 1.6f;
        public float greatRopeNeedsRadius = 4f;
        public int ringFrames = 40, canonOffsetFrames = 20;
        public float gapSeconds = 1.4f;
        public float steadySeconds = 1f, steadyReach = 1f;
        public int whiteDamage = 1;

        public float Radius { get; private set; }
        public bool IsRevealed => _revealLeft > 0f;
        public IReadOnlyList<BossPart> Ropes => _ropes;
        public int Rings { get; private set; }
        public int Cuts { get; private set; }
        public int Steadies { get; private set; }
        public int WhiteTakes { get; private set; }
        public float SteadyProgress => steadySeconds <= 0f ? 1f : Mathf.Clamp01(_steadyT / steadySeconds);
        public int RingFramesOrFloor => Mathf.Max(ringFrames, MinTelegraphFrames);

        /// <summary>The bells ringing in a phase: the first; the two; the great one.</summary>
        public static IReadOnlyList<int> BellsOf(int phase) => phase >= 3 ? Great : phase >= 2 ? Two : One;
        static readonly int[] One = { 0 }, Two = { 1, 2 }, Great = { 3 };

        readonly List<BossPart> _ropes = new List<BossPart>();
        readonly bool[] _cut = new bool[RopeCount];
        readonly int[] _ring = { -1, -1, -1, -1 };
        float _gap, _revealLeft, _steadyT;
        bool _routing, _canonDue;
        int _turn;

        /// <summary>Whether this bell is one of the current phase's.</summary>
        public bool RingsNow(int bell) { foreach (var b in BellsOf(Phase)) if (b == bell) return true; return false; }
        public bool IsCut(int rope) => rope >= 0 && rope < RopeCount && _cut[rope];
        public bool IsRinging(int bell) => bell >= 0 && bell < RopeCount && _ring[bell] >= 0;
        public int RingingCount { get { int n = 0; foreach (var r in _ring) if (r >= 0) n++; return n; } }

        protected override bool ContactHurts => false;
        protected override bool AcceptsHit(in HitInfo hit) => _routing;
        /// <summary>Only the ropes move the fight on.</summary>
        protected override void OnHealthChanged() { }

        public void Reveal(float seconds) { _revealLeft = Mathf.Max(_revealLeft, seconds); }

        protected override void Start()
        {
            base.Start();
            EnsureRopes();
            SetRadius(fullRadius);
        }

        void EnsureRopes()
        {
            if (_ropes.Count == ropeXs.Count) return;
            for (int i = 0; i < ropeXs.Count; i++)
            {
                int k = i;
                var r = BossPart.Make(i == 3 ? "GreatRope" : "Rope_" + (i + 1), transform.parent, new Vector2(ropeXs[i], floorY + ropeHeight * 0.5f), new Vector2(0.3f, ropeHeight),
                    InkMaterials.Lit("Bell_Rope", new Color(0.70f, 0.66f, 0.58f)));
                r.OnHit = hit => CutRope(k);
                _ropes.Add(r);
            }
        }

        /// <summary>She can see a rope when it is in her radius, or when the lantern shows everything; the great rope needs a wide radius too.</summary>
        public bool CanSee(int rope)
        {
            if (rope < 0 || rope >= ropeXs.Count) return false;
            if (IsRevealed) return true;
            if (Wren == null) return false;
            if (rope == 3 && Radius < greatRopeNeedsRadius) return false;
            return Mathf.Abs(Wren.Position.x - ropeXs[rope]) <= Radius;
        }

        bool CutRope(int rope)
        {
            if (!IsFightActive || IsCut(rope) || !RingsNow(rope) || !CanSee(rope)) return false;
            _cut[rope] = true;
            _ring[rope] = -1;
            Cuts++;
            if (rope < _ropes.Count && _ropes[rope] != null) _ropes[rope].gameObject.SetActive(false);
            _routing = true;
            try { TakeHit(new HitInfo { Damage = 1, Direction = Vector2.up }); }
            finally { _routing = false; }
            // Silenced: the next bells, when this phase's are all cut.
            bool allCut = true;
            foreach (var b in BellsOf(Phase)) if (!IsCut(b)) allCut = false;
            if (allCut && !IsDead) AdvanceToPhase(Phase + 1);
            return true;
        }

        void SetRadius(float r)
        {
            Radius = Mathf.Clamp(r, minRadius, fullRadius);
            // The bells hold her lantern-radius while they ring; her own comes back when they stop.
            if (IsFightActive) Lantern.Hold(this, Radius);
            else Lantern.Release(this);
        }

        void OnDisable() { Lantern.Release(this); }

        protected override void OnFightStarted()
        {
            EnsureRopes();
            for (int i = 0; i < RopeCount; i++) { _cut[i] = false; _ring[i] = -1; }
            foreach (var r in _ropes) if (r != null) r.gameObject.SetActive(true);
            SetRadius(fullRadius);
            _gap = gapSeconds;
            _turn = 0;
            _canonDue = false;
        }

        protected override void OnFightReset()
        {
            for (int i = 0; i < RopeCount; i++) { _cut[i] = false; _ring[i] = -1; }
            foreach (var r in _ropes) if (r != null) r.gameObject.SetActive(true);
            SetRadius(fullRadius);
        }

        protected override void OnPhaseStarted(int phase)
        {
            for (int i = 0; i < RopeCount; i++) _ring[i] = -1;
            _gap = gapSeconds;
            _turn = 0;
            _canonDue = false;
        }

        protected override void OnDefeated()
        {
            SetRadius(fullRadius);
            for (int i = 0; i < RopeCount; i++) _ring[i] = -1;
        }

        /// <summary>Tests and tooling: ring this bell now.</summary>
        public void ForceRing(int bell) { if (bell >= 0 && bell < RopeCount && !IsCut(bell)) _ring[bell] = 0; }

        protected override void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_revealLeft > 0f) _revealLeft -= dt;
            if (IsFightActive) Steady(dt);
            base.FixedUpdate();
        }

        /// <summary>Standing still at the vantage steadies the radius back to full.</summary>
        void Steady(float dt)
        {
            bool at = Wren != null && Wren.IsGrounded && Mathf.Abs(Wren.Velocity.x) < 0.1f && Mathf.Abs(Wren.Position.x - vantageX) <= steadyReach;
            if (!at) { _steadyT = 0f; return; }
            _steadyT += dt;
            if (_steadyT < steadySeconds) return;
            _steadyT = 0f;
            if (Radius < fullRadius) { SetRadius(fullRadius); Steadies++; }
        }

        protected override void Tick(float dt)
        {
            var bells = BellsOf(Phase);
            for (int i = 0; i < RopeCount; i++)
            {
                if (_ring[i] < 0) continue;
                if (IsCut(i)) { _ring[i] = -1; continue; }
                if (++_ring[i] >= RingFramesOrFloor) Toll(i);
            }

            // Phase 2's canon: the second bell comes in halfway through the first.
            if (_canonDue)
            {
                foreach (var b in bells)
                {
                    if (!IsRinging(b) || _ring[b] < canonOffsetFrames) continue;
                    foreach (var other in bells) if (other != b && !IsCut(other) && !IsRinging(other)) ForceRing(other);
                    _canonDue = false;
                    break;
                }
                if (RingingCount == 0) _canonDue = false;
            }

            if (RingingCount > 0) return;
            _gap -= dt;
            if (_gap > 0f) return;
            _gap = gapSeconds;
            for (int k = 0; k < bells.Count; k++)
            {
                int b = bells[(_turn + k) % bells.Count];
                if (IsCut(b)) continue;
                _turn++;
                ForceRing(b);
                _canonDue = bells.Count > 1;
                break;
            }
        }

        void Toll(int bell)
        {
            _ring[bell] = -1;
            Rings++;
            if (Radius <= minRadius + 0.001f)
            {
                // Nothing left to take but her: the white costs a mask.
                var vitals = Wren != null ? Wren.GetComponent<WrenVitals>() : null;
                if (vitals != null && vitals.Damage(whiteDamage, (Vector2)Wren.transform.position + Vector2.up)) WhiteTakes++;
            }
            SetRadius(Radius - shrinkPerRing);
        }

        protected override Color TintColor() => new Color(0.94f, 0.93f, 0.90f);
    }
}
