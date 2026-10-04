using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Surveyor Hale (bible 6.9, boss sheet 6.9, CMB-14). Tier III, optional: the only duel in the game, at the Nine
    /// Stones at dusk. He surveys during the fight: he raises his lens at a stone, and when the sighting finishes the
    /// stone is his, and strikes when he calls the count. Wren can survey a stone first (stand still at it) to deny it;
    /// a stone she has drawn is safe. The phases follow the stones, not the wounds: phase 2 at five stones sighted,
    /// phase 3 at eight, whoever's they are. His quill is a jab that the lens can parry; the stones' strikes can be
    /// pogoed.
    /// </summary>
    public sealed class Hale : Boss
    {
        public enum Attack { None, Sight, Call, Quill, Flick }
        public enum Move { Stand, Approach, Telegraph, Quill, Call, Recover }
        public enum Owner { None, Hale, Wren }

        public const int StoneCount = 9, PhaseTwoAt = 5, PhaseThreeAt = 8;

        [Header("Hale")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        /// <summary>The nine stones along the floor; set by the kit.</summary>
        public List<float> stoneXs = new List<float>();
        public float claimSeconds = 0.8f, claimRadius = 0.9f;
        public int sightFrames = 36;
        public int callTelegraphFrames = 18, eruptFrames = 10;
        public float eruptHeight = 3f, stoneWidth = 1f;
        public int quillTelegraphFrames = 12, quillFrames = 5;
        public float quillReach = 2.2f;
        /// <summary>The flick: ink thrown off his quill at her, from range (an <see cref="EnemyProjectile"/>).</summary>
        public int flickTelegraphFrames = 14;
        public float flickSpeed = 9f;
        public float walkSpeed = 3f, reach = 2.2f, approachSeconds = 1.2f;
        public int recoverFrames = 20;
        public float standSeconds = 0.5f, parriedStaggerSeconds = 1f;
        public int damage = 1;

        public Move Current { get; private set; } = Move.Stand;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        /// <summary>The tell for the attack being telegraphed (AUD-03): its kind in the kit.</summary>
        protected override AttackKind TelegraphKind => CurrentAttack switch { Attack.Sight => AttackKind.Window, _ => AttackKind.Strike };
        public int Claimed { get { int n = 0; foreach (var o in _owner) if (o != Owner.None) n++; return n; } }
        public int HaleStones => Count(Owner.Hale);
        public int WrenStones => Count(Owner.Wren);
        /// <summary>The stone his lens is on while he sights, else -1.</summary>
        public int SightTarget => CurrentAttack == Attack.Sight && Current == Move.Telegraph ? _target : -1;
        /// <summary>Wren's survey of the stone she stands at, 0..1.</summary>
        public float ClaimProgress => claimSeconds <= 0f ? 1f : Mathf.Clamp01(_claimT / claimSeconds);
        public IReadOnlyList<BossPart> Pillars => _pillars;
        public int Sights { get; private set; }
        public int SightsDenied { get; private set; }
        public int Calls { get; private set; }
        public int Quills { get; private set; }
        public int Parried { get; private set; }
        public int Flicks { get; private set; }
        public EnemyProjectile LastFlick { get; private set; }
        public int Dir => Facing;

        static readonly Attack[] Phase1 = { Attack.Sight, Attack.Quill, Attack.Sight };
        static readonly Attack[] Phase2 = { Attack.Sight, Attack.Call, Attack.Flick, Attack.Quill };
        static readonly Attack[] Phase3 = { Attack.Call, Attack.Flick, Attack.Sight, Attack.Quill };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            yield return new BossAttack("Sight", AttackKind.Window, Read(sightFrames), 0, PhasesOf(Attack.Sight, PatternFor));
            yield return new BossAttack("The count", AttackKind.Strike, Read(callTelegraphFrames), damage, PhasesOf(Attack.Call, PatternFor));
            yield return new BossAttack("Quill", AttackKind.Strike, Read(quillTelegraphFrames), damage, PhasesOf(Attack.Quill, PatternFor));
            yield return new BossAttack("Flick", AttackKind.Strike, Read(flickTelegraphFrames), damage, PhasesOf(Attack.Flick, PatternFor));
        }

        Owner[] _owner = new Owner[StoneCount];
        readonly List<Transform> _stones = new List<Transform>();
        readonly List<BossPart> _pillars = new List<BossPart>();
        int _frames, _patternIndex, _target = -1, _claimStone = -1;
        float _pause, _approachT, _claimT;
        bool _telegraphStarted, _hitThisAttack;
        Attack _forced = Attack.None;

        public Owner OwnerOf(int stone) => stone >= 0 && stone < _owner.Length ? _owner[stone] : Owner.None;
        int Count(Owner o) { int n = 0; foreach (var x in _owner) if (x == o) n++; return n; }

        public override IEnumerable<string> PartSkinNames { get { yield return "Stone"; yield return "StoneStrike"; } }
        /// <summary>The sheet clip for his move (CHR-10): the lens up for a sighting, the quill raised for the count.</summary>
        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip : Current switch
        {
            Move.Approach => "move",
            Move.Telegraph => CurrentAttack == Attack.Sight ? "sight" : CurrentAttack == Attack.Call ? "call" : "telegraph",
            Move.Quill => "quill",
            Move.Call => "count",
            Move.Recover => "recover",
            _ => "idle",
        };
        /// <summary>The sighting is sought by its window: the lens is at his eye as the stone becomes his.</summary>
        public override float ClipProgress => SightTarget >= 0 && _telegraphStarted && !IsDying && HurtstunLeft == 0 ? TelegraphProgress(sightFrames) : -1f;
        static string StoneClip(Owner o) => o == Owner.Hale ? "hale" : o == Owner.Wren ? "wren" : "bare";

        protected override void Start()
        {
            base.Start();
            EnsureStones();
        }

        void EnsureStones()
        {
            if (_owner.Length != stoneXs.Count) _owner = new Owner[stoneXs.Count];
            if (_stones.Count == stoneXs.Count) return;
            for (int i = 0; i < stoneXs.Count; i++)
            {
                var stone = BossPart.Prop("Stone_" + (i + 1), transform.parent, new Vector2(stoneXs[i], floorY + 0.6f), new Vector2(0.6f, 1.2f), StoneMaterial(Owner.None), 0.8f);
                SkinProp(stone, "Stone", "bare");
                _stones.Add(stone);
            }
        }

        static Material StoneMaterial(Owner o) => o switch
        {
            Owner.Hale => InkMaterials.Lit("Stone_Hale", new Color(0.66f, 0.54f, 0.26f)),
            Owner.Wren => InkMaterials.Lit("Stone_Wren", new Color(0.14f, 0.16f, 0.24f)),
            _ => InkMaterials.Lit("Stone_Bare", new Color(0.58f, 0.58f, 0.54f)),
        };

        void SetOwner(int i, Owner o)
        {
            _owner[i] = o;
            if (i < _stones.Count) BossPart.Show(_stones[i], StoneClip(o), StoneMaterial(o));
            if (Claimed >= PhaseTwoAt) AdvanceToPhase(2);
            if (Claimed >= PhaseThreeAt) AdvanceToPhase(3);
        }

        /// <summary>Tests and tooling: begin a specific attack now.</summary>
        public void ForceAttack(Attack a)
        {
            ClearTelegraph();
            ClearPillars();
            BeginAttack(a);
        }

        protected override void OnFightStarted()
        {
            EnsureStones();
            for (int i = 0; i < _owner.Length; i++) SetOwner(i, Owner.None);
            Current = Move.Stand;
            _pause = standSeconds;
            _patternIndex = 0;
            _claimT = 0f;
            _claimStone = -1;
            FaceWren();
        }

        protected override void OnFightReset()
        {
            Current = Move.Stand;
            CurrentAttack = Attack.None;
            _telegraphStarted = false;
            ClearPillars();
            for (int i = 0; i < _owner.Length; i++) _owner[i] = Owner.None;
            for (int i = 0; i < _stones.Count; i++) BossPart.Show(_stones[i], StoneClip(Owner.None), StoneMaterial(Owner.None));
        }

        protected override void OnPhaseStarted(int phase) { _patternIndex = 0; }
        protected override void OnDefeated() { ClearPillars(); }

        /// <summary>The stones set the phases, not the wounds.</summary>
        protected override void OnHealthChanged() { }

        protected override void FixedUpdate()
        {
            if (IsFightActive) SurveyByWren(Time.fixedDeltaTime);   // her survey runs through his hurtstun
            base.FixedUpdate();
        }

        /// <summary>Standing still at a bare stone draws it: it is hers, and he can no longer sight it.</summary>
        void SurveyByWren(float dt)
        {
            int at = -1;
            if (Wren != null && Wren.IsGrounded && Mathf.Abs(Wren.Velocity.x) < 0.1f)
                for (int i = 0; i < stoneXs.Count; i++)
                    if (_owner[i] == Owner.None && Mathf.Abs(Wren.Position.x - stoneXs[i]) <= claimRadius) { at = i; break; }
            if (at != _claimStone) { _claimStone = at; _claimT = 0f; }
            if (at < 0) return;
            _claimT += dt;
            if (_claimT < claimSeconds) return;
            SetOwner(at, Owner.Wren);
            _claimStone = -1;
            _claimT = 0f;
        }

        void FaceWren()
        {
            if (Wren == null) return;
            Face(Wren.Position.x >= transform.position.x ? 1 : -1);
        }

        float DistanceToWren => Wren == null ? 99f : Mathf.Abs(Wren.Position.x - transform.position.x);

        Attack NextAttack()
        {
            if (_forced != Attack.None) { var f = _forced; _forced = Attack.None; return f; }
            var p = PatternFor(Phase);
            var a = p[_patternIndex % p.Count];
            _patternIndex++;
            return a;
        }

        /// <summary>The bare stone nearest him, or -1 when every stone is drawn.</summary>
        int NearestBareStone()
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < stoneXs.Count; i++)
            {
                if (_owner[i] != Owner.None) continue;
                float d = Mathf.Abs(stoneXs[i] - transform.position.x);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        void BeginAttack(Attack a)
        {
            if (a == Attack.Sight && NearestBareStone() < 0) a = HaleStones > 0 ? Attack.Call : Attack.Quill;   // nothing left to sight
            if (a == Attack.Call && HaleStones == 0) a = Attack.Quill;                                          // nothing to call
            CurrentAttack = a;
            _telegraphStarted = false;
            _hitThisAttack = false;
            _frames = 0;
            FaceWren();
            if (a == Attack.Sight) _target = NearestBareStone();
            Current = Move.Telegraph;
        }

        int TelegraphFrames(Attack a) => a switch
        {
            Attack.Sight => sightFrames,
            Attack.Call => callTelegraphFrames,
            Attack.Flick => flickTelegraphFrames,
            _ => quillTelegraphFrames,
        };

        /// <summary>Ink off the nib, straight at where she stands.</summary>
        void Flick()
        {
            Flicks++;
            var from = (Vector2)Collider.bounds.center + new Vector2(Facing * (Collider.bounds.extents.x + 0.3f), 0.2f);
            var to = Wren != null ? Wren.Position + Vector2.up * 0.55f : from + new Vector2(Facing, 0f);
            LastFlick = EnemyProjectile.Spawn("Flick", transform.parent, from, (to - from).normalized * flickSpeed, new Vector2(0.35f, 0.35f), damage,
                InkMaterials.Lit("Hale_Ink", new Color(0.18f, 0.20f, 0.30f)));
        }

        protected override void Tick(float dt)
        {
            switch (Current)
            {
                case Move.Stand:
                    _pause -= dt;
                    if (_pause > 0f) break;
                    FaceWren();
                    var next = NextAttack();
                    if (next == Attack.Quill && DistanceToWren > reach)
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
                        case Attack.Sight:
                            if (_target >= 0 && _owner[_target] == Owner.None) { Sights++; SetOwner(_target, Owner.Hale); }
                            else SightsDenied++;   // she drew it first
                            Recover();
                            break;
                        case Attack.Call: Calls++; RaisePillars(); Current = Move.Call; break;
                        case Attack.Quill: Quills++; Current = Move.Quill; break;
                        case Attack.Flick: Flick(); Recover(); break;
                        default: Recover(); break;
                    }
                    break;

                case Move.Quill:
                {
                    var b = Collider.bounds;
                    var centre = new Vector2(b.center.x + Facing * (b.extents.x + quillReach * 0.5f), b.center.y);
                    if (!_hitThisAttack)
                    {
                        var c = HitWren(centre, new Vector2(quillReach, 0.6f), damage, true);
                        if (c == Contact.Landed) _hitThisAttack = true;
                        else if (c == Contact.Parried) { Parried++; Stagger(parriedStaggerSeconds); Recover(); break; }
                    }
                    if (++_frames >= quillFrames) Recover();
                    break;
                }

                case Move.Call:
                    foreach (var p in _pillars)
                    {
                        if (_hitThisAttack || p == null) break;
                        if (HitWren(p.Position, new Vector2(stoneWidth, eruptHeight), damage, false) == Contact.Landed) _hitThisAttack = true;
                    }
                    if (++_frames >= eruptFrames) { ClearPillars(); Recover(); }
                    break;

                case Move.Recover:
                    if (++_frames >= recoverFrames) { Current = Move.Stand; _pause = standSeconds; CurrentAttack = Attack.None; _target = -1; }
                    break;
            }
        }

        void Recover() { Current = Move.Recover; _frames = 0; }

        void MoveX(float dx)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x + dx, arenaMinX, arenaMaxX);
            transform.position = p;
            Body.position = p;
        }

        /// <summary>Every stone of his strikes at once: a column of light above it, solid enough to pogo from.</summary>
        void RaisePillars()
        {
            ClearPillars();
            for (int i = 0; i < stoneXs.Count; i++)
            {
                if (_owner[i] != Owner.Hale) continue;
                var p = BossPart.Make("Strike_" + (i + 1), transform.parent, new Vector2(stoneXs[i], floorY + eruptHeight * 0.5f), new Vector2(stoneWidth, eruptHeight),
                    InkMaterials.Lit("Stone_Strike", new Color(0.96f, 0.84f, 0.50f)));
                p.OnHit = hit => IsDownStrike(hit);   // pogo the stones' strikes
                Skin(p, "StoneStrike", "erupt");
                _pillars.Add(p);
            }
        }

        void ClearPillars()
        {
            foreach (var p in _pillars) if (p != null) Destroy(p.gameObject);
            _pillars.Clear();
        }

        protected override Color TintColor() => new Color(0.46f, 0.40f, 0.30f);
    }
}
