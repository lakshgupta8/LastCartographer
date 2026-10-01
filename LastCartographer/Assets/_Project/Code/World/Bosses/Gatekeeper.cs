using System.Collections.Generic;
using UnityEngine;
using OWSBG.Core;

namespace OWSBG.World
{
    /// <summary>
    /// The Gatekeeper (bible 6.7, boss sheet 6.7, CMB-13). Tier II, at the Overgrown Gate: a flying-age statue of a great
    /// eagle with roots for wings. The roots are Inkthread anchors. Phase 1, on the ground: wing sweeps along the floor
    /// (jump them) and stone feathers that fall where their shadows are (pogo them). Phase 2: it rises to the top of the
    /// gate and the fight goes vertical on the threads; the sweeps come at perch height. Phase 3: the roots tear free
    /// and it flies, badly, for the first time since the Grounding: heavy passes across the gate and hard landings. It
    /// is stone on top and open underneath: in the air only an up-strike to the belly lands.
    /// </summary>
    public sealed class Gatekeeper : Boss
    {
        public enum Attack { None, Sweep, Feathers, Pass, Land }
        public enum Move { Stand, Rise, Telegraph, Sweep, Pass, Land, Recover }

        [Header("Gatekeeper")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        public float perchY = 7f, passY = 2.4f;
        public float riseSeconds = 1f;
        /// <summary>Where the roots hold (Inkthread anchors); set by the kit.</summary>
        public List<Vector2> rootPoints = new List<Vector2>();
        public int sweepTelegraphFrames = 16, sweepFrames = 10;
        public float sweepReach = 3.2f, sweepHeight = 1.2f;
        public int featherTelegraphFrames = 14;
        public float featherSpacing = 2.5f, featherDrop = 9f, featherSpeed = 10f;
        public Vector2 featherSize = new Vector2(0.5f, 0.9f);
        public int passTelegraphFrames = 18;
        public float passSpeed = 8f;
        public int landTelegraphFrames = 16, landFrames = 8;
        public float landReach = 2.5f;
        public int recoverFrames = 26;
        public float standSeconds = 0.6f;
        public int damage = 1;
        /// <summary>The heavy landing comes down on the shadow it throws first: a slam (CMB-19).</summary>
        public int slamDamage = Tuning.Slam;

        public Move Current { get; private set; } = Move.Stand;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        /// <summary>The tell for the attack being telegraphed (AUD-03): its kind in the kit.</summary>
        protected override AttackKind TelegraphKind => CurrentAttack switch { Attack.Land => AttackKind.Slam, _ => AttackKind.Strike };
        public IReadOnlyList<TetherAnchor> Roots => _roots;
        public bool RootsTorn => IsFightActive && Phase >= 3;
        public bool IsAloft => IsFightActive && Phase >= 2;
        public IReadOnlyList<BossPart> Feathers => _feathers;
        public int Sweeps { get; private set; }
        public int FeatherVolleys { get; private set; }
        public int FeathersPogoed { get; private set; }
        public int Passes { get; private set; }
        public int Landings { get; private set; }
        public int Dir => Facing;

        static readonly Attack[] Phase1 = { Attack.Sweep, Attack.Feathers };
        static readonly Attack[] Phase2 = { Attack.Feathers, Attack.Sweep, Attack.Feathers };
        static readonly Attack[] Phase3 = { Attack.Pass, Attack.Land, Attack.Pass };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            yield return new BossAttack("Wing sweep", AttackKind.Strike, Read(sweepTelegraphFrames), damage, PhasesOf(Attack.Sweep, PatternFor));
            yield return new BossAttack("Stone feathers", AttackKind.Strike, Read(featherTelegraphFrames), damage, PhasesOf(Attack.Feathers, PatternFor));
            yield return new BossAttack("Pass", AttackKind.Strike, Read(passTelegraphFrames), ContactDamage, PhasesOf(Attack.Pass, PatternFor));
            yield return new BossAttack("Landing", AttackKind.Slam, Read(landTelegraphFrames), slamDamage, PhasesOf(Attack.Land, PatternFor));
        }

        readonly List<TetherAnchor> _roots = new List<TetherAnchor>();
        readonly List<BossPart> _feathers = new List<BossPart>();
        readonly HashSet<BossPart> _feathersThatHit = new HashSet<BossPart>();
        readonly List<float> _shadowXs = new List<float>();
        readonly List<Transform> _shadows = new List<Transform>();
        int _frames, _patternIndex;
        float _pause, _riseT;
        Vector2 _riseFrom;
        bool _telegraphStarted, _hitThisAttack;
        Attack _forced = Attack.None;

        float RestY => Collider != null ? floorY + Collider.bounds.extents.y : floorY + 1.2f;

        protected override bool ContactHurts => Phase < 3 || Current == Move.Pass;

        /// <summary>Stone on top, open underneath: once it flies, only the belly.</summary>
        protected override bool AcceptsHit(in HitInfo hit) => Phase < 3 || IsUpStrike(hit);

        public override IEnumerable<string> PartSkinNames { get { yield return "Feather"; } }
        /// <summary>The sheet clip for its move (CHR-10): on the plinth, hanging from the roots, or flying, badly.</summary>
        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip : Current switch
        {
            Move.Rise => "rise",
            Move.Telegraph => CurrentAttack == Attack.Feathers ? "shake" : CurrentAttack == Attack.Sweep ? "telegraph" : "fly",
            Move.Sweep => "sweep",
            Move.Pass => "pass",
            Move.Land => "land",
            Move.Recover => RootsTorn ? "fly" : "recover",
            _ => RootsTorn ? "fly" : IsAloft ? "perch" : "idle",
        };

        protected override void Start()
        {
            base.Start();
            EnsureRoots();
        }

        void EnsureRoots()
        {
            _roots.RemoveAll(r => r == null);
            if (_roots.Count > 0) return;
            foreach (var p in rootPoints)
            {
                var a = TetherAnchor.Spawn(p, float.PositiveInfinity);
                a.name = "Root";
                if (transform.parent != null) a.transform.SetParent(transform.parent, true);
                _roots.Add(a);
            }
        }

        void TearRoots()
        {
            foreach (var r in _roots) if (r != null) Destroy(r.gameObject);
            _roots.Clear();
        }

        /// <summary>Tests and tooling: begin a specific attack from wherever it is.</summary>
        public void ForceAttack(Attack a)
        {
            ClearTelegraph();
            BeginAttack(a);
        }

        protected override void OnFightStarted()
        {
            EnsureRoots();
            Current = Move.Stand;
            _pause = standSeconds;
            _patternIndex = 0;
            FaceWren();
        }

        protected override void OnFightReset()
        {
            Current = Move.Stand;
            CurrentAttack = Attack.None;
            _telegraphStarted = false;
            ClearFeathers();
            EnsureRoots();
        }

        protected override void OnPhaseStarted(int phase)
        {
            _patternIndex = 0;
            ClearTelegraph();
            ClearFeathers();
            if (phase == 2)
            {
                // It rises to the top of the gate; the fight goes vertical on the threads.
                Current = Move.Rise;
                _riseT = 0f;
                _riseFrom = transform.position;
            }
            else if (phase == 3)
            {
                // The roots tear free. It drops to flying height at the end farther from her.
                TearRoots();
                float wx = Wren != null ? Wren.Position.x : arenaMinX;
                float x = wx - arenaMinX > arenaMaxX - wx ? arenaMinX : arenaMaxX;
                SetPos(new Vector2(x, passY));
                Current = Move.Stand;
                _pause = standSeconds;
                FaceWren();
            }
        }

        protected override void OnDefeated() { ClearFeathers(); }

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
            if (a == Attack.Feathers) MarkShadows();
        }

        int TelegraphFrames(Attack a) => a switch
        {
            Attack.Sweep => sweepTelegraphFrames,
            Attack.Feathers => featherTelegraphFrames,
            Attack.Pass => passTelegraphFrames,
            Attack.Land => landTelegraphFrames,
            _ => sweepTelegraphFrames,
        };

        protected override void Tick(float dt)
        {
            FallFeathers(dt);
            switch (Current)
            {
                case Move.Stand:
                    _pause -= dt;
                    if (_pause > 0f) break;
                    BeginAttack(NextAttack());
                    break;

                case Move.Rise:
                    _riseT += dt;
                    float k = Mathf.Clamp01(_riseT / Mathf.Max(0.01f, riseSeconds));
                    SetPos(Vector2.Lerp(_riseFrom, new Vector2((arenaMinX + arenaMaxX) * 0.5f, perchY), k));
                    if (k >= 1f) { Current = Move.Stand; _pause = standSeconds; }
                    break;

                case Move.Telegraph:
                    if (!Telegraph(ref _telegraphStarted, TelegraphFrames(CurrentAttack))) break;
                    _frames = 0;
                    switch (CurrentAttack)
                    {
                        case Attack.Sweep: Current = Move.Sweep; Sweeps++; break;
                        case Attack.Feathers: DropFeathers(); Recover(); break;
                        case Attack.Pass: FaceAcross(); Current = Move.Pass; Passes++; break;
                        case Attack.Land: Current = Move.Land; Landings++; break;
                        default: Recover(); break;
                    }
                    break;

                case Move.Sweep:
                {
                    // On the ground the wings sweep the floor; at the perch they sweep the threads.
                    var b = Collider.bounds;
                    float y = IsAloft ? b.center.y : floorY + sweepHeight * 0.5f;
                    if (!_hitThisAttack && HitWren(new Vector2(b.center.x, y), new Vector2(b.size.x + sweepReach * 2f, sweepHeight), damage, false) == Contact.Landed)
                        _hitThisAttack = true;
                    if (++_frames >= sweepFrames) Recover();
                    break;
                }

                case Move.Pass:
                {
                    var p = (Vector2)transform.position + new Vector2(Facing * passSpeed * dt, 0f);
                    p.x = Mathf.Clamp(p.x, arenaMinX, arenaMaxX);
                    SetPos(p);
                    if (p.x <= arenaMinX + 0.01f || p.x >= arenaMaxX - 0.01f) Recover();
                    break;
                }

                case Move.Land:
                {
                    // Heavy and wrong: it comes down hard where it is and the floor jumps either side.
                    if (_frames == 0) { SetPos(new Vector2(transform.position.x, RestY)); Shake.Request(Tuning.SlamShake, Tuning.SlamShakeSeconds); }
                    var b = Collider.bounds;
                    if (!_hitThisAttack && HitWren(new Vector2(b.center.x, floorY + 0.4f), new Vector2(b.size.x + landReach * 2f, 0.8f), slamDamage, false) == Contact.Landed)
                        _hitThisAttack = true;
                    if (++_frames >= landFrames) { SetPos(new Vector2(transform.position.x, passY)); Recover(); }
                    break;
                }

                case Move.Recover:
                    if (++_frames >= recoverFrames) { Current = Move.Stand; _pause = standSeconds; CurrentAttack = Attack.None; }
                    break;
            }
        }

        void Recover() { Current = Move.Recover; _frames = 0; }

        void FaceAcross()
        {
            float mid = (arenaMinX + arenaMaxX) * 0.5f;
            Face(transform.position.x < mid ? 1 : -1);
        }

        void SetPos(Vector2 p)
        {
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            Body.position = p;
        }

        // ---- feathers ---------------------------------------------------------------------------------------------------

        void MarkShadows()
        {
            ClearShadows();
            float wx = Wren != null ? Wren.Position.x : (arenaMinX + arenaMaxX) * 0.5f;
            foreach (var off in new[] { -featherSpacing, 0f, featherSpacing })
            {
                float x = Mathf.Clamp(wx + off, arenaMinX, arenaMaxX);
                _shadowXs.Add(x);
                _shadows.Add(BossPart.Prop("Shadow", transform.parent, new Vector2(x, floorY + 0.03f), new Vector2(featherSize.x * 1.6f, 0.06f), InkMaterials.Dark, 0f));
            }
        }

        void ClearShadows()
        {
            foreach (var s in _shadows) if (s != null) Destroy(s.gameObject);
            _shadows.Clear();
            _shadowXs.Clear();
        }

        void DropFeathers()
        {
            FeatherVolleys++;
            float top = (IsAloft ? perchY : floorY) + featherDrop;
            foreach (var x in _shadowXs)
            {
                var f = BossPart.Make("Feather", transform.parent, new Vector2(x, top), featherSize, InkMaterials.Lit("Gatekeeper_Stone", new Color(0.60f, 0.60f, 0.55f)));
                f.OnHit = hit =>
                {
                    if (!IsDownStrike(hit)) return false;   // pogo the feathers
                    FeathersPogoed++;
                    _feathers.Remove(f);
                    Destroy(f.gameObject);
                    return true;
                };
                Skin(f, "Feather");
                _feathers.Add(f);
            }
            ClearShadows();
        }

        void FallFeathers(float dt)
        {
            for (int i = _feathers.Count - 1; i >= 0; i--)
            {
                var f = _feathers[i];
                if (f == null) { _feathers.RemoveAt(i); continue; }
                var p = f.Position + Vector2.down * featherSpeed * dt;
                f.MoveTo(p);
                if (!_feathersThatHit.Contains(f) && HitWren(p, featherSize, damage, false) == Contact.Landed) _feathersThatHit.Add(f);
                if (p.y - featherSize.y * 0.5f <= floorY)
                {
                    _feathersThatHit.Remove(f);
                    _feathers.RemoveAt(i);
                    Destroy(f.gameObject);   // it shatters on the gate's floor
                }
            }
        }

        void ClearFeathers()
        {
            foreach (var f in _feathers) if (f != null) Destroy(f.gameObject);
            _feathers.Clear();
            _feathersThatHit.Clear();
            ClearShadows();
        }

        protected override Color TintColor() => new Color(0.52f, 0.54f, 0.46f);
    }
}
