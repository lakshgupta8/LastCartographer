using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Guildmaster Aurelian Voss (bible 6.11, boss sheet 6.11, CMB-15). Tier III, at the Threshold: he will not let
    /// Wren cross. Phase 1, lance and shield, textbook: a thrust (parry it), a lunge, and a guard that turns the quill
    /// from the front (pogo him or get behind). Phase 2, he anchors: the colour grade locks, and he seals sections of
    /// the floor where she stands. A section sealed with her inside holds her for a beat, and holds her again if she
    /// walks back in; a seal breaks to a strike on its edge from outside. Phase 3: the Blank eats the arena from the
    /// west, section by section, seals and all, and he fights from a shrinking island, still holding. The white hurts.
    /// </summary>
    public sealed class Voss : Boss
    {
        public enum Attack { None, Thrust, Lunge, Guard, Anchor }
        public enum Move { Stand, Approach, Telegraph, Thrust, Lunge, Guard, Recover }

        [Header("Voss")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        public int sections = 6;
        public float walkSpeed = 3f, reach = 2.6f, approachSeconds = 1.4f;
        public float lanceReach = 2.6f, lanceHeight = 0.8f;
        public int thrustTelegraphFrames = 14, thrustFrames = 6;
        public int lungeTelegraphFrames = 16, lungeFrames = 10;
        public float lungeDistance = 6f;
        public int guardTelegraphFrames = 10, guardFrames = 60;
        public float guardSpeed = 1.2f;
        public int anchorTelegraphFrames = 24;
        public int maxSeals = 3;
        public float sealHeight = 5f, edgeBand = 0.5f;
        public float holdSeconds = 0.8f, holdCooldownSeconds = 1.2f;
        public float eatSeconds = 1.6f;
        public int islandSections = 2;
        public int recoverFrames = 22;
        public float parriedStaggerSeconds = 1f;
        public float standSeconds = 0.5f;
        public int damage = 1, blankDamage = 1;

        public Move Current { get; private set; } = Move.Stand;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        /// <summary>The tell for the attack being telegraphed (AUD-03): its kind in the kit.</summary>
        protected override AttackKind TelegraphKind => CurrentAttack switch { Attack.Guard => AttackKind.Shape, Attack.Anchor => AttackKind.Window, _ => AttackKind.Strike };
        public bool IsGuarding => Current == Move.Guard;
        public bool IsAnchoring => IsFightActive && Phase >= 2;
        public bool BlankComing => IsFightActive && Phase >= 3;
        /// <summary>The Blank's edge: everything west of it is white.</summary>
        public float EdgeX { get; private set; }
        public float SectionWidth => (arenaMaxX - arenaMinX) / Mathf.Max(1, sections);
        public int SectionOf(float x) => Mathf.Clamp(Mathf.FloorToInt((x - arenaMinX) / SectionWidth), 0, sections - 1);
        public float SectionCentre(int i) => arenaMinX + (i + 0.5f) * SectionWidth;
        public IReadOnlyDictionary<int, BossPart> Seals => _seals;
        public bool IsSealed(int section) => _seals.ContainsKey(section);
        public bool IsHoldingWren => _holdLeft > 0f;
        /// <summary>The section a seal is being drawn over, else -1.</summary>
        public int AnchorTarget => CurrentAttack == Attack.Anchor && Current == Move.Telegraph ? _anchorSection : -1;
        public int Thrusts { get; private set; }
        public int Lunges { get; private set; }
        public int Guards { get; private set; }
        public int Anchors { get; private set; }
        public int Holds { get; private set; }
        public int SealsBroken { get; private set; }
        public int SealsEaten { get; private set; }
        public int SectionsEaten { get; private set; }
        public int Parried { get; private set; }
        public int WhiteBurns { get; private set; }
        public int Dir => Facing;

        static readonly Attack[] Phase1 = { Attack.Thrust, Attack.Guard, Attack.Lunge };
        static readonly Attack[] Phase2 = { Attack.Anchor, Attack.Thrust, Attack.Anchor, Attack.Lunge };
        static readonly Attack[] Phase3 = { Attack.Anchor, Attack.Thrust, Attack.Guard };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            yield return new BossAttack("Thrust", AttackKind.Strike, Read(thrustTelegraphFrames), damage, PhasesOf(Attack.Thrust, PatternFor));
            yield return new BossAttack("Lunge", AttackKind.Strike, Read(lungeTelegraphFrames), damage, PhasesOf(Attack.Lunge, PatternFor));
            yield return new BossAttack("Guard", AttackKind.Shape, Read(guardTelegraphFrames), 0, PhasesOf(Attack.Guard, PatternFor));
            yield return new BossAttack("Anchor", AttackKind.Window, Read(anchorTelegraphFrames), 0, PhasesOf(Attack.Anchor, PatternFor));
            yield return new BossAttack("The Blank", AttackKind.Window, Read(Frames(eatSeconds)), blankDamage, 0b100);
        }

        readonly Dictionary<int, BossPart> _seals = new Dictionary<int, BossPart>();
        int _frames, _patternIndex, _anchorSection = -1;
        float _pause, _approachT, _holdLeft, _holdCooldown, _eatT;
        bool _telegraphStarted, _hitThisAttack, _wrenWasInSeal;
        Transform _rose, _white;
        Attack _forced = Attack.None;
        WrenVitals _vitals;

        float MinX => Mathf.Max(arenaMinX, EdgeX + 0.5f);

        public override IEnumerable<string> PartSkinNames { get { yield return "VossSeal"; } }
        /// <summary>The sheet clip for his move (CHR-10): the guard behind the rose, and the lance planted to anchor.</summary>
        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip : Current switch
        {
            Move.Approach => "move",
            Move.Telegraph => CurrentAttack == Attack.Anchor ? "anchor" : "telegraph",
            Move.Thrust => "thrust",
            Move.Lunge => "lunge",
            Move.Guard => "guard",
            Move.Recover => "recover",
            _ => "idle",
        };

        /// <summary>Tests and tooling: begin a specific attack from wherever he stands.</summary>
        public void ForceAttack(Attack a)
        {
            ClearTelegraph();
            ClearRose();
            BeginAttack(a);
        }

        protected override void OnFightStarted()
        {
            Current = Move.Stand;
            _pause = standSeconds;
            _patternIndex = 0;
            EdgeX = arenaMinX;
            _eatT = 0f;
            FaceWren();
        }

        protected override void OnFightReset()
        {
            Current = Move.Stand;
            CurrentAttack = Attack.None;
            _telegraphStarted = false;
            ClearAll();
        }

        protected override void OnPhaseStarted(int phase)
        {
            _patternIndex = 0;
            if (phase >= 2) HeldState.LockGrade(1f);   // he anchors: the grade locks
            if (phase == 3) { _eatT = 0f; EnsureWhite(); }
        }

        protected override void OnDefeated() { ClearAll(); }

        void ClearAll()
        {
            Release();
            foreach (var s in _seals.Values) if (s != null) Destroy(s.gameObject);
            _seals.Clear();
            ClearRose();
            if (_white != null) { Destroy(_white.gameObject); _white = null; }
            EdgeX = arenaMinX;
            HeldState.LockGrade(0f);
        }

        void FaceWren()
        {
            if (Wren == null) return;
            Face(Wren.Position.x >= transform.position.x ? 1 : -1);
        }

        float DistanceToWren => Wren == null ? 99f : Mathf.Abs(Wren.Position.x - transform.position.x);

        protected override void FixedUpdate()
        {
            if (IsFightActive)
            {
                float dt = Time.fixedDeltaTime;
                Holding(dt);
                if (BlankComing) Eat(dt);
            }
            base.FixedUpdate();
        }

        // ---- the seals and the hold -------------------------------------------------------------------------------------

        void Seal(int section)
        {
            if (_seals.ContainsKey(section) || SectionCentre(section) < EdgeX) return;
            if (_seals.Count >= maxSeals)
            {
                int oldest = -1;
                foreach (var k in _seals.Keys) { oldest = k; break; }
                RemoveSeal(oldest);
            }
            var at = new Vector2(SectionCentre(section), floorY + sealHeight * 0.5f);
            var seal = BossPart.Make("Seal", transform.parent, at, new Vector2(SectionWidth, sealHeight), InkMaterials.Lit("Voss_Seal", new Color(0.72f, 0.66f, 0.46f)));
            Skin(seal, "VossSeal");
            seal.Visual.localScale = new Vector3(SectionWidth, sealHeight, 0.05f);
            int k2 = section;
            seal.OnHit = hit => StrikeSeal(k2);
            _seals[section] = seal;
            if (WrenInSection(section)) Hold();   // sealed with her inside
        }

        /// <summary>A seal breaks to a strike on its edge, from outside; from within, the quill only rings on it.</summary>
        bool StrikeSeal(int section)
        {
            if (!_seals.ContainsKey(section) || Wren == null) return false;
            float inside = Mathf.Abs(Wren.Position.x - SectionCentre(section));
            if (inside < SectionWidth * 0.5f - edgeBand) return false;
            RemoveSeal(section);
            SealsBroken++;
            return true;
        }

        void RemoveSeal(int section)
        {
            if (_seals.TryGetValue(section, out var s) && s != null) Destroy(s.gameObject);
            _seals.Remove(section);
        }

        bool WrenInSection(int section) => Wren != null && SectionOf(Wren.Position.x) == section
            && Wren.Position.x >= arenaMinX && Wren.Position.x <= arenaMaxX;

        void Hold()
        {
            if (Wren == null || _holdLeft > 0f) return;
            Holds++;
            _holdLeft = holdSeconds;
            _holdCooldown = holdCooldownSeconds;
            _wrenWasInSeal = true;   // she must walk out and back in to be held again
            _frozeHer = true;
            Wren.Frozen = true;
        }

        bool _frozeHer;

        void Release()
        {
            _holdLeft = 0f;
            if (!_frozeHer) return;
            _frozeHer = false;
            if (Wren != null) Wren.Frozen = false;
        }

        void Holding(float dt)
        {
            if (_holdCooldown > 0f) _holdCooldown -= dt;   // counted from the hold's start
            if (_holdLeft > 0f)
            {
                _holdLeft -= dt;
                if (_holdLeft <= 0f) Release();
                return;
            }
            bool inSeal = Wren != null && IsSealed(SectionOf(Wren.Position.x)) && WrenInSection(SectionOf(Wren.Position.x));
            if (inSeal && !_wrenWasInSeal && _holdCooldown <= 0f) Hold();   // walking back into a held section
            _wrenWasInSeal = inSeal;
        }

        // ---- the Blank --------------------------------------------------------------------------------------------------

        void EnsureWhite()
        {
            if (_white == null)
                _white = BossPart.Prop("Blank", transform.parent, new Vector2(arenaMinX, floorY + 5f), new Vector2(0.01f, 10f), InkMaterials.Lit("Voss_Blank", new Color(0.98f, 0.97f, 0.95f)), -0.6f);
            float w = Mathf.Max(0.01f, EdgeX - arenaMinX);
            _white.position = new Vector3(arenaMinX + w * 0.5f, floorY + 5f, -0.6f);
            _white.localScale = new Vector3(w, 10f, 0.2f);
        }

        void Eat(float dt)
        {
            int eaten = Mathf.RoundToInt((EdgeX - arenaMinX) / SectionWidth);
            if (eaten < sections - islandSections)
            {
                _eatT += dt;
                if (_eatT >= eatSeconds)
                {
                    _eatT = 0f;
                    EdgeX = arenaMinX + (eaten + 1) * SectionWidth;
                    SectionsEaten++;
                    var gone = new List<int>();
                    foreach (var k in _seals.Keys) if (SectionCentre(k) < EdgeX) gone.Add(k);
                    foreach (var k in gone) { RemoveSeal(k); SealsEaten++; }
                    EnsureWhite();
                    if (transform.position.x < MinX) MoveX(0f);
                }
            }
            // Stay off the west: the white takes a mask at a time.
            if (Wren == null || Wren.Position.x >= EdgeX) return;
            if (_vitals == null) _vitals = Wren.GetComponent<WrenVitals>();
            if (_vitals != null && _vitals.Damage(blankDamage, new Vector2(EdgeX - 2f, Wren.Position.y))) WhiteBurns++;
        }

        // ---- the lance and the shield -----------------------------------------------------------------------------------

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
            if (a == Attack.Anchor && Wren != null && (IsSealed(SectionOf(Wren.Position.x)) || Wren.Position.x < EdgeX)) a = Attack.Thrust;
            CurrentAttack = a;
            _telegraphStarted = false;
            _hitThisAttack = false;
            _frames = 0;
            FaceWren();
            if (a == Attack.Anchor)
            {
                _anchorSection = SectionOf(Wren != null ? Wren.Position.x : transform.position.x);
                _rose = BossPart.Prop("CompassRose", transform.parent, new Vector2(SectionCentre(_anchorSection), floorY + 0.05f), new Vector2(SectionWidth * 0.9f, 0.1f), InkMaterials.Lit("Voss_Rose", new Color(0.80f, 0.66f, 0.30f)), 0f);
            }
            Current = Move.Telegraph;
        }

        int TelegraphFrames(Attack a) => a switch
        {
            Attack.Thrust => thrustTelegraphFrames,
            Attack.Lunge => lungeTelegraphFrames,
            Attack.Guard => guardTelegraphFrames,
            Attack.Anchor => anchorTelegraphFrames,
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
                    if (next == Attack.Thrust && DistanceToWren > reach)
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
                    if (DistanceToWren <= reach || _approachT >= approachSeconds) BeginAttack(NextAttack());
                    break;

                case Move.Telegraph:
                    if (!Telegraph(ref _telegraphStarted, TelegraphFrames(CurrentAttack))) break;
                    _frames = 0;
                    switch (CurrentAttack)
                    {
                        case Attack.Thrust: Current = Move.Thrust; Thrusts++; break;
                        case Attack.Lunge: Current = Move.Lunge; Lunges++; break;
                        case Attack.Guard: Current = Move.Guard; Guards++; break;
                        case Attack.Anchor: Anchors++; ClearRose(); Seal(_anchorSection); Recover(recoverFrames); break;
                        default: Recover(recoverFrames); break;
                    }
                    break;

                case Move.Thrust:
                    Lance(true);
                    if (Current == Move.Thrust && ++_frames >= thrustFrames) Recover(recoverFrames);
                    break;

                case Move.Lunge:
                    MoveX(Facing * lungeDistance / Mathf.Max(1, lungeFrames));
                    Lance(true);
                    if (Current == Move.Lunge && ++_frames >= lungeFrames) Recover(recoverFrames);
                    break;

                case Move.Guard:
                    MoveX(Facing * guardSpeed * dt);
                    if (++_frames >= guardFrames) Recover(recoverFrames);
                    break;

                case Move.Recover:
                    if (++_frames >= recoverFrames) { Current = Move.Stand; _pause = standSeconds; CurrentAttack = Attack.None; _anchorSection = -1; }
                    break;
            }
        }

        void Recover(int frames) { Current = Move.Recover; _frames = recoverFrames - frames; }

        void MoveX(float dx)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x + dx, MinX, arenaMaxX);
            transform.position = p;
            Body.position = p;
        }

        void Lance(bool parryable)
        {
            if (_hitThisAttack) return;
            var b = Collider.bounds;
            var centre = new Vector2(b.center.x + Facing * (b.extents.x + lanceReach * 0.5f), b.center.y - 0.2f);
            var c = HitWren(centre, new Vector2(lanceReach, lanceHeight), damage, parryable);
            if (c == Contact.Landed) _hitThisAttack = true;
            else if (c == Contact.Parried)
            {
                Parried++;
                _hitThisAttack = true;
                Recover(recoverFrames);
                Stagger(parriedStaggerSeconds);
            }
        }

        void ClearRose()
        {
            if (_rose != null) Destroy(_rose.gameObject);
            _rose = null;
        }

        /// <summary>The compass-rose shield turns the quill from the front; over it, or behind him, it lands.</summary>
        protected override bool AcceptsHit(in HitInfo hit)
        {
            if (!IsGuarding) return true;
            if (IsDownStrike(hit)) return true;
            return Wren != null && (Wren.Position.x - transform.position.x) * Facing < 0f;
        }

        void OnDisable() { Release(); }

        protected override Color TintColor() => new Color(0.52f, 0.54f, 0.58f);
    }
}
