using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Cinder Warden Brann (bible 6.5, boss sheet 6.5, CMB-13). Tier II, in a cooling furnace on the Furnace Stair.
    /// The floor is in sections, red-hot or cool; standing on a hot one burns. The pattern shifts on a clock and the
    /// sections about to heat glow first. Phase 1: two sections in six are cool, one lance (thrust, charge). Phase 2:
    /// half the floor is cool; both lances: the cross-cut (jump it, pogo him) and the hold, a line he walks forward
    /// behind both lances that only a Longstroke (or a pogo) gets through. Phase 3: the furnace is dark, nothing
    /// burns, and only his brass glows; his telegraphs are the glow. Lance thrusts and charges can be parried.
    /// </summary>
    public sealed class Brann : Boss
    {
        public enum Attack { None, Thrust, Charge, CrossCut, Hold }
        public enum Move { Stand, Approach, Telegraph, Thrust, Charge, CrossCut, Hold, Recover }

        [Header("Brann")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        public int sections = 6;
        public float shiftSeconds = 3.2f, warnSeconds = 0.8f;
        public int heatDamage = 1;
        public float walkSpeed = 3f, reach = 2.6f, approachSeconds = 1.4f;
        public float lanceReach = 2.4f, lanceHeight = 0.8f;
        public int thrustTelegraphFrames = 14, thrustFrames = 6;
        public int chargeTelegraphFrames = 16, chargeFrames = 40;
        public float chargeSpeed = 11f;
        public int crossTelegraphFrames = 16, crossFrames = 8;
        public float crossReach = 2.6f, crossHeight = 1.4f;
        public int holdTelegraphFrames = 12, holdFrames = 70;
        public float holdSpeed = 1.6f;
        public int recoverFrames = 24;
        public float parriedStaggerSeconds = 1f, brokenHoldStaggerSeconds = 1.2f;
        public float standSeconds = 0.5f;
        public int lanceDamage = 1;

        public Move Current { get; private set; } = Move.Stand;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        /// <summary>The tell for the attack being telegraphed (AUD-03): its kind in the kit.</summary>
        protected override AttackKind TelegraphKind => CurrentAttack switch { Attack.Hold => AttackKind.Window, _ => AttackKind.Strike };
        public bool IsDark => IsFightActive && Phase >= 3;
        public bool IsHolding => Current == Move.Hold;

        /// <summary>The sheet clip for his move (CHR-07).</summary>
        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip : Current switch
        {
            Move.Approach => "move",
            Move.Telegraph => "telegraph",
            Move.Thrust => "thrust",
            Move.Charge => "charge",
            Move.CrossCut => "crosscut",
            Move.Hold => "hold",
            Move.Recover => "recover",
            _ => "idle",
        };
        /// <summary>0..1: how bright his brass is. In the dark it is the only telegraph.</summary>
        public float Glow => IsTelegraphing ? 1f : IsHolding ? 0.6f : 0.25f;
        public int Thrusts { get; private set; }
        public int Charges { get; private set; }
        public int CrossCuts { get; private set; }
        public int Holds { get; private set; }
        public int HoldsBroken { get; private set; }
        public int Parried { get; private set; }
        public int Burns { get; private set; }
        public int Shifts { get; private set; }
        public int Dir => Facing;

        static readonly Attack[] Phase1 = { Attack.Thrust, Attack.Charge, Attack.Thrust };
        static readonly Attack[] Phase2 = { Attack.CrossCut, Attack.Hold, Attack.Thrust };
        static readonly Attack[] Phase3 = { Attack.Hold, Attack.CrossCut, Attack.Thrust, Attack.Charge };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            yield return new BossAttack("Thrust", AttackKind.Strike, Read(thrustTelegraphFrames), lanceDamage, PhasesOf(Attack.Thrust, PatternFor));
            yield return new BossAttack("Charge", AttackKind.Strike, Read(chargeTelegraphFrames), lanceDamage, PhasesOf(Attack.Charge, PatternFor));
            yield return new BossAttack("Cross-cut", AttackKind.Strike, Read(crossTelegraphFrames), lanceDamage, PhasesOf(Attack.CrossCut, PatternFor));
            yield return new BossAttack("Hold", AttackKind.Strike, Read(holdTelegraphFrames), lanceDamage, PhasesOf(Attack.Hold, PatternFor));
            yield return new BossAttack("Hot floor", AttackKind.Window, Read(Frames(warnSeconds)), heatDamage, 0b011);
        }

        bool[] _hot = new bool[0], _next = new bool[0];
        readonly List<Transform> _tiles = new List<Transform>();
        int _frames, _patternIndex, _shiftK;
        float _pause, _approachT, _shiftT;
        bool _telegraphStarted, _hitThisAttack, _warned;
        Attack _forced = Attack.None;
        WrenVitals _vitals;

        public float SectionWidth => (arenaMaxX - arenaMinX) / Mathf.Max(1, sections);
        public int SectionOf(float x) => Mathf.Clamp(Mathf.FloorToInt((x - arenaMinX) / SectionWidth), 0, sections - 1);
        public float SectionCentre(int i) => arenaMinX + (i + 0.5f) * SectionWidth;
        public bool IsHot(int i) => i >= 0 && i < _hot.Length && _hot[i];
        /// <summary>About to heat: it glows before it burns.</summary>
        public bool IsWarming(int i) => _warned && i >= 0 && i < _next.Length && _next[i] && !IsHot(i);
        public int HotCount { get { int n = 0; foreach (var h in _hot) if (h) n++; return n; } }

        protected override void Start()
        {
            base.Start();
            EnsureFloor();
        }

        void EnsureFloor()
        {
            if (_hot.Length != sections) { _hot = new bool[sections]; _next = new bool[sections]; }
            if (_tiles.Count == sections) return;
            for (int i = 0; i < sections; i++)
                _tiles.Add(BossPart.Prop("Furnace_" + i, transform.parent, new Vector2(SectionCentre(i), floorY + 0.04f), new Vector2(SectionWidth * 0.96f, 0.1f), TileMaterial(false, false), 0f));
        }

        /// <summary>Which sections are hot at shift <paramref name="k"/> of <paramref name="phase"/>: a third cool, then half, then none hot.</summary>
        public static bool[] HotPattern(int phase, int k, int sections)
        {
            var hot = new bool[sections];
            if (phase >= 3) return hot;
            for (int i = 0; i < sections; i++) hot[i] = true;
            if (phase == 2)
            {
                for (int i = k & 1; i < sections; i += 2) hot[i] = false;
            }
            else
            {
                hot[k % sections] = false;
                hot[(k + sections / 2) % sections] = false;
            }
            return hot;
        }

        /// <summary>Tests and tooling: begin a specific attack from wherever he stands.</summary>
        public void ForceAttack(Attack a)
        {
            ClearTelegraph();
            BeginAttack(a);
        }

        protected override void OnFightStarted()
        {
            EnsureFloor();
            Current = Move.Stand;
            _pause = standSeconds;
            _patternIndex = 0;
            _shiftK = 0;
            _shiftT = 0f;
            ApplyPattern(HotPattern(1, 0, sections));
            FaceWren();
        }

        protected override void OnFightReset()
        {
            Current = Move.Stand;
            CurrentAttack = Attack.None;
            _telegraphStarted = false;
            ApplyPattern(new bool[sections]);
        }

        protected override void OnPhaseStarted(int phase)
        {
            _patternIndex = 0;
            if (phase <= 1) return;
            _shiftK++;
            _shiftT = 0f;
            ApplyPattern(HotPattern(phase, _shiftK, sections));
        }

        protected override void OnDefeated() { ApplyPattern(new bool[sections]); }

        void ApplyPattern(bool[] hot)
        {
            EnsureFloor();
            for (int i = 0; i < sections; i++) _hot[i] = hot[i];
            _warned = false;
            RefreshTiles();
        }

        void RefreshTiles()
        {
            for (int i = 0; i < _tiles.Count; i++)
                if (_tiles[i] != null) _tiles[i].GetComponent<MeshRenderer>().sharedMaterial = IsDark ? TileMaterial(false, false, true) : TileMaterial(IsHot(i), IsWarming(i));
        }

        static Material TileMaterial(bool hot, bool warming, bool dark = false)
        {
            if (dark) return InkMaterials.Lit("Furnace_Dark", new Color(0.08f, 0.07f, 0.07f));
            if (hot) return InkMaterials.Lit("Furnace_Hot", new Color(0.86f, 0.24f, 0.10f));
            if (warming) return InkMaterials.Lit("Furnace_Warming", new Color(0.92f, 0.56f, 0.20f));
            return InkMaterials.Lit("Furnace_Cool", new Color(0.36f, 0.34f, 0.34f));
        }

        protected override void FixedUpdate()
        {
            if (IsFightActive)
            {
                AdvanceFloor(Time.fixedDeltaTime);
                Burn();
            }
            base.FixedUpdate();
        }

        void AdvanceFloor(float dt)
        {
            if (IsDark) return;
            _shiftT += dt;
            if (!_warned && _shiftT >= shiftSeconds - warnSeconds)
            {
                var n = HotPattern(Phase, _shiftK + 1, sections);
                for (int i = 0; i < sections; i++) _next[i] = n[i];
                _warned = true;
                RefreshTiles();
            }
            if (_shiftT < shiftSeconds) return;
            _shiftT = 0f;
            _shiftK++;
            Shifts++;
            ApplyPattern(HotPattern(Phase, _shiftK, sections));
        }

        /// <summary>Standing on a red-hot section costs a mask (the usual invulnerability after spaces them out).</summary>
        void Burn()
        {
            if (Wren == null || IsDark) return;
            if (!Wren.IsGrounded || Wren.Position.y > floorY + 0.3f) return;
            if (!IsHot(SectionOf(Wren.Position.x))) return;
            if (_vitals == null) _vitals = Wren.GetComponent<WrenVitals>();
            if (_vitals != null && _vitals.Damage(heatDamage, new Vector2(Wren.Position.x, floorY - 1f))) Burns++;
        }

        void FaceWren()
        {
            if (Wren == null) return;
            Face(Wren.Position.x >= transform.position.x ? 1 : -1);
        }

        Attack NextAttack()
        {
            if (_forced != Attack.None) { var f = _forced; _forced = Attack.None; return f; }
            var p = PatternFor(Phase);
            var a = p[_patternIndex % p.Count];
            _patternIndex++;
            return a;
        }

        void BeginAttack(Attack a)
        {
            CurrentAttack = a;
            _telegraphStarted = false;
            _hitThisAttack = false;
            _frames = 0;
            FaceWren();
            Current = Move.Telegraph;
        }

        int TelegraphFrames(Attack a) => a switch
        {
            Attack.Thrust => thrustTelegraphFrames,
            Attack.Charge => chargeTelegraphFrames,
            Attack.CrossCut => crossTelegraphFrames,
            Attack.Hold => holdTelegraphFrames,
            _ => thrustTelegraphFrames,
        };

        protected override void Tick(float dt)
        {
            switch (Current)
            {
                case Move.Stand:
                    _pause -= dt;
                    if (_pause > 0f) break;
                    FaceWren();
                    var next = NextAttack();
                    if ((next == Attack.Thrust || next == Attack.CrossCut) && Wren != null && Mathf.Abs(Wren.Position.x - transform.position.x) > reach)
                    {
                        _forced = next;
                        Current = Move.Approach;
                        _approachT = 0f;
                    }
                    else BeginAttack(next);
                    break;

                case Move.Approach:
                    _approachT += dt;
                    FaceWren();
                    MoveX(Facing * walkSpeed * dt);
                    if (Wren == null || Mathf.Abs(Wren.Position.x - transform.position.x) <= reach || _approachT >= approachSeconds)
                        BeginAttack(NextAttack());
                    break;

                case Move.Telegraph:
                    if (!Telegraph(ref _telegraphStarted, TelegraphFrames(CurrentAttack))) break;
                    _frames = 0;
                    switch (CurrentAttack)
                    {
                        case Attack.Thrust: Current = Move.Thrust; Thrusts++; break;
                        case Attack.Charge: Current = Move.Charge; Charges++; break;
                        case Attack.CrossCut: Current = Move.CrossCut; CrossCuts++; break;
                        case Attack.Hold: Current = Move.Hold; Holds++; break;
                        default: Recover(recoverFrames); break;
                    }
                    break;

                case Move.Thrust:
                    Lance(true);
                    if (Current == Move.Thrust && ++_frames >= thrustFrames) Recover(recoverFrames);
                    break;

                case Move.Charge:
                    MoveX(Facing * chargeSpeed * dt);
                    Lance(true);
                    if (Current != Move.Charge) break;
                    bool passed = Wren != null && (Wren.Position.x - transform.position.x) * Facing < -2f;
                    bool wall = transform.position.x <= arenaMinX + 0.01f || transform.position.x >= arenaMaxX - 0.01f;
                    if (++_frames >= chargeFrames || passed || wall) Recover(recoverFrames);
                    break;

                case Move.CrossCut:
                {
                    var b = Collider.bounds;
                    var centre = new Vector2(b.center.x, floorY + crossHeight * 0.5f);
                    if (!_hitThisAttack && HitWren(centre, new Vector2(b.size.x + crossReach * 2f, crossHeight), lanceDamage, false) == Contact.Landed) _hitThisAttack = true;
                    if (++_frames >= crossFrames) Recover(recoverFrames);
                    break;
                }

                case Move.Hold:
                    MoveX(Facing * holdSpeed * dt);
                    Lance(false);
                    if (++_frames >= holdFrames) Recover(recoverFrames);
                    break;

                case Move.Recover:
                    if (++_frames >= recoverFrames) { Current = Move.Stand; _pause = standSeconds; CurrentAttack = Attack.None; }
                    break;
            }
        }

        void Recover(int frames) { Current = Move.Recover; _frames = recoverFrames - frames; }

        void MoveX(float dx)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x + dx, arenaMinX, arenaMaxX);
            transform.position = p;
            Body.position = p;
        }

        void Lance(bool parryable)
        {
            if (_hitThisAttack) return;
            var b = Collider.bounds;
            var centre = new Vector2(b.center.x + Facing * (b.extents.x + lanceReach * 0.5f), b.center.y - 0.2f);
            var c = HitWren(centre, new Vector2(lanceReach, lanceHeight), lanceDamage, parryable);
            if (c == Contact.Landed) _hitThisAttack = true;
            else if (c == Contact.Parried)
            {
                Parried++;
                _hitThisAttack = true;
                Recover(recoverFrames);
                Stagger(parriedStaggerSeconds);
            }
        }

        /// <summary>Behind both lances only a Longstroke or a pogo lands; a Longstroke breaks the hold.</summary>
        protected override bool AcceptsHit(in HitInfo hit)
        {
            if (!IsHolding) return true;
            if (IsLongstroke(hit))
            {
                HoldsBroken++;
                Recover(recoverFrames);
                Stagger(brokenHoldStaggerSeconds);
                return true;
            }
            return IsDownStrike(hit);
        }

        protected override void Update()
        {
            base.Update();
            if (IsFightActive) RefreshTiles();
        }

        protected override Color TintColor()
        {
            var brass = new Color(0.62f, 0.46f, 0.20f);
            var black = new Color(0.10f, 0.09f, 0.08f);
            return IsDark ? Color.Lerp(black, brass, Glow) : Color.Lerp(black, brass, 0.35f + 0.4f * (IsTelegraphing ? 1f : 0f));
        }
    }
}
