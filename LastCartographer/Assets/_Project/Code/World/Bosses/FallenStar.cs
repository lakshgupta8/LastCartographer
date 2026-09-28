using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The Fallen Star (bible 6.10, boss sheet 6.10, CMB-14). Tier III, optional: an iron meteorite-golem in the
    /// anvil-crater, woken when the keystone is lifted from it. It is magnetic iron everywhere but the seam on top where
    /// the keystone sat: side strikes drift off it, and only a strike from above lands. Phase 1: it walks and slams,
    /// and the fist stays down a moment, iron to pogo from up onto its back. Phase 2: it draws iron walls up from the
    /// ground either side of her. Phase 3: it burns; the walls grow past any jump and the heat makes updrafts beside
    /// them, which with Windmemory are the only way over.
    /// </summary>
    public sealed class FallenStar : Boss
    {
        public enum Attack { None, Walk, Slam, Walls }
        public enum Move { Stand, Walk, Telegraph, Slam, Recover }

        [Header("The Fallen Star")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        public float walkSpeed = 1.6f, walkSeconds = 1.2f;
        public int slamTelegraphFrames = 18, slamFrames = 6, fistFrames = 45;
        public float slamReach = 5f;
        /// <summary>The one hard shake in the game (combat doc: only on boss slams), before the player's scale.</summary>
        public const float SlamShake = Tuning.SlamShake;
        public Vector2 fistSize = new Vector2(1.6f, 1f);
        public int wallTelegraphFrames = 20;
        public float wallHeight = 3.6f, burningWallHeight = 5.4f, wallWidth = 0.8f, wallOffset = 3f;
        public int maxWalls = 2;
        public float updraftSpeed = 9f, updraftWidth = 1.4f;
        public int recoverFrames = 24;
        public float standSeconds = 0.6f;
        public int damage = 1;
        /// <summary>The fist comes down on the mark it made: a slam (CMB-19).</summary>
        public int slamDamage = Tuning.Slam;

        public Move Current { get; private set; } = Move.Stand;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        public bool IsBurning => IsFightActive && Phase >= 3;
        public BossPart Fist { get; private set; }
        public IReadOnlyList<GameObject> Walls => _walls;
        public IReadOnlyList<Updraft> Updrafts => _updrafts;
        public float WallHeight => IsBurning ? burningWallHeight : wallHeight;
        public int Walks { get; private set; }
        public int Slams { get; private set; }
        public int WallRaisings { get; private set; }
        public int StrikesDrifted { get; private set; }
        public int Dir => Facing;

        static readonly Attack[] Phase1 = { Attack.Walk, Attack.Slam };
        static readonly Attack[] Phase2 = { Attack.Walls, Attack.Slam, Attack.Walk };
        static readonly Attack[] Phase3 = { Attack.Slam, Attack.Walls, Attack.Walk };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            yield return new BossAttack("Walk", AttackKind.Shape, 0, 0, PhasesOf(Attack.Walk, PatternFor));
            yield return new BossAttack("Slam", AttackKind.Slam, Read(slamTelegraphFrames), slamDamage, PhasesOf(Attack.Slam, PatternFor));
            yield return new BossAttack("Iron walls", AttackKind.Shape, Read(wallTelegraphFrames), 0, PhasesOf(Attack.Walls, PatternFor));
        }

        readonly List<GameObject> _walls = new List<GameObject>();
        readonly List<Updraft> _updrafts = new List<Updraft>();
        readonly List<Transform> _marks = new List<Transform>();
        readonly List<float> _wallXs = new List<float>();
        int _frames, _patternIndex, _fistLeft;
        float _pause, _walkT, _slamX;
        bool _telegraphStarted, _hitThisAttack;
        Attack _forced = Attack.None;

        /// <summary>Iron everywhere but the seam on top: only a strike from above lands.</summary>
        protected override bool AcceptsHit(in HitInfo hit) => IsDownStrike(hit);
        protected override void OnHitBlocked(in HitInfo hit) { base.OnHitBlocked(hit); StrikesDrifted++; }

        /// <summary>Tests and tooling: begin a specific attack now.</summary>
        public void ForceAttack(Attack a)
        {
            ClearTelegraph();
            BeginAttack(a);
        }

        protected override void OnFightStarted()
        {
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
            ClearAll();
        }

        protected override void OnPhaseStarted(int phase)
        {
            _patternIndex = 0;
            if (phase == 3) Burn();
        }

        protected override void OnDefeated() { ClearAll(); }

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
            switch (a)
            {
                case Attack.Walk:
                    Walks++;
                    _walkT = 0f;
                    Current = Move.Walk;
                    break;
                case Attack.Slam:
                {
                    float wx = Wren != null ? Wren.Position.x : transform.position.x;
                    float x = transform.position.x;
                    _slamX = Mathf.Clamp(wx, x - slamReach, x + slamReach);
                    _marks.Add(BossPart.Prop("SlamMark", transform.parent, new Vector2(_slamX, floorY + 0.03f), new Vector2(fistSize.x, 0.06f), InkMaterials.Dark, 0f));
                    Current = Move.Telegraph;
                    break;
                }
                case Attack.Walls:
                {
                    float wx = Wren != null ? Wren.Position.x : (arenaMinX + arenaMaxX) * 0.5f;
                    _wallXs.Clear();
                    foreach (var off in new[] { -wallOffset, wallOffset })
                    {
                        float x = Mathf.Clamp(wx + off, arenaMinX + wallWidth, arenaMaxX - wallWidth);
                        _wallXs.Add(x);
                        _marks.Add(BossPart.Prop("WallMark", transform.parent, new Vector2(x, floorY + 0.03f), new Vector2(wallWidth * 1.4f, 0.06f), InkMaterials.Dark, 0f));
                    }
                    Current = Move.Telegraph;
                    break;
                }
                default:
                    Current = Move.Telegraph;
                    break;
            }
        }

        int TelegraphFrames(Attack a) => a == Attack.Walls ? wallTelegraphFrames : slamTelegraphFrames;

        protected override void Tick(float dt)
        {
            if (Fist != null && --_fistLeft <= 0) ClearFist();
            switch (Current)
            {
                case Move.Stand:
                    _pause -= dt;
                    if (_pause > 0f) break;
                    BeginAttack(NextAttack());
                    break;

                case Move.Walk:
                    _walkT += dt;
                    FaceWren();
                    MoveX(Facing * walkSpeed * dt);
                    if (_walkT >= walkSeconds) Recover();
                    break;

                case Move.Telegraph:
                    if (!Telegraph(ref _telegraphStarted, TelegraphFrames(CurrentAttack))) break;
                    ClearMarks();
                    _frames = 0;
                    if (CurrentAttack == Attack.Slam) { Slams++; Current = Move.Slam; Shake.Request(SlamShake, Tuning.SlamShakeSeconds); }
                    else if (CurrentAttack == Attack.Walls) { RaiseWalls(); Recover(); }
                    else Recover();
                    break;

                case Move.Slam:
                {
                    var centre = new Vector2(_slamX, floorY + fistSize.y * 0.5f);
                    if (!_hitThisAttack && HitWren(centre, fistSize, slamDamage, false) == Contact.Landed) _hitThisAttack = true;
                    if (++_frames < slamFrames) break;
                    LeaveFist(centre);
                    Recover();
                    break;
                }

                case Move.Recover:
                    if (++_frames >= recoverFrames) { Current = Move.Stand; _pause = standSeconds; CurrentAttack = Attack.None; }
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

        /// <summary>The fist stays down a moment: iron to pogo from, up onto its back.</summary>
        void LeaveFist(Vector2 centre)
        {
            ClearFist();
            Fist = BossPart.Make("Fist", transform.parent, centre, fistSize, InkMaterials.Lit("Star_Iron", new Color(0.26f, 0.24f, 0.26f)));
            Fist.OnHit = hit => IsDownStrike(hit);   // pogo the slam
            _fistLeft = fistFrames;
        }

        void ClearFist()
        {
            if (Fist != null) Destroy(Fist.gameObject);
            Fist = null;
        }

        void RaiseWalls()
        {
            WallRaisings++;
            foreach (var x in _wallXs)
            {
                while (_walls.Count >= maxWalls) RemoveWall(0);
                var wall = new GameObject("IronWall") { layer = LayerMask.NameToLayer("Ground") };
                wall.transform.SetParent(transform.parent, false);
                wall.AddComponent<BoxCollider2D>();
                BossPart.Prop("Visual", wall.transform, Vector2.zero, Vector2.one, InkMaterials.Lit("Star_Iron", new Color(0.26f, 0.24f, 0.26f)), 0f).localPosition = Vector3.zero;
                _walls.Add(wall);
                _updrafts.Add(null);
                SizeWall(_walls.Count - 1, x);
            }
        }

        /// <summary>A wall at its height; burning, an updraft rises on the side she is on.</summary>
        void SizeWall(int i, float x)
        {
            var wall = _walls[i];
            float h = WallHeight;
            wall.transform.position = new Vector3(x, floorY + h * 0.5f, 0f);
            wall.GetComponent<BoxCollider2D>().size = new Vector2(wallWidth, h);
            var v = wall.transform.Find("Visual");
            if (v != null) v.localScale = new Vector3(wallWidth, h, 0.4f);
            if (!IsBurning || _updrafts[i] != null) return;
            float side = Wren != null && Wren.Position.x < x ? -1f : 1f;   // on her side: she is the one who needs it
            var ux = x + side * (wallWidth * 0.5f + updraftWidth * 0.5f);
            _updrafts[i] = Updraft.Make("Heat", transform.parent, new Vector2(ux, floorY + (h + 3f) * 0.5f), new Vector2(updraftWidth, h + 3f), updraftSpeed);
        }

        /// <summary>Phase 3: it burns; the walls grow and the heat rises beside them.</summary>
        void Burn()
        {
            for (int i = 0; i < _walls.Count; i++)
                if (_walls[i] != null) SizeWall(i, _walls[i].transform.position.x);
        }

        void RemoveWall(int i)
        {
            if (_walls[i] != null) Destroy(_walls[i]);
            if (_updrafts[i] != null) Destroy(_updrafts[i].gameObject);
            _walls.RemoveAt(i);
            _updrafts.RemoveAt(i);
        }

        void ClearMarks()
        {
            foreach (var m in _marks) if (m != null) Destroy(m.gameObject);
            _marks.Clear();
        }

        void ClearAll()
        {
            ClearFist();
            ClearMarks();
            while (_walls.Count > 0) RemoveWall(0);
        }

        protected override Color TintColor() => IsBurning ? new Color(0.78f, 0.34f, 0.14f) : new Color(0.24f, 0.22f, 0.24f);
    }
}
