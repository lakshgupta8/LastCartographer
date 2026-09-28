using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Warden-Sergeant Halvard, the first hunt (bible 6.3, boss sheet 6.3, CMB-12). Tier I, on the Salt Chapel's floor.
    /// A grounded lancer: he closes to reach, thrusts (parry it), lunges (jump it), and <b>surveys</b>: plants the
    /// lance and marks squares of the floor under her; the marks stay, and when he <b>calls the count</b> every mark
    /// erupts. Phase 3 opens with the whole floor marked but one pace. Always hittable; a parried lance staggers him.
    /// He does not die; at zero he withdraws (the arena sets the flag and he deactivates).
    /// </summary>
    public sealed class Halvard : Boss
    {
        public enum Attack { None, Thrust, Lunge, Survey, Count }
        public enum Move { Stand, Approach, Telegraph, Thrust, Lunge, Count, Recover }

        [Header("Halvard")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 13.5f;
        public float walkSpeed = 3.2f;
        public float reach = 2.6f;
        public float approachSeconds = 1.6f;
        public float lanceReach = 2.4f, lanceHeight = 0.8f;
        public int thrustTelegraphFrames = 14, thrustFrames = 6;
        public int lungeTelegraphFrames = 17, lungeFrames = 10;
        public float lungeDistance = 6f;
        public int surveyTelegraphFrames = 19;
        public int countTelegraphFrames = 22, eruptFrames = 8;
        public float eruptHeight = 2.2f;
        public float markWidth = 1.6f;
        public float markSpacing = 3f;
        public int recoverFrames = 22;
        public int parriedRecoverFrames = 48;
        public float standSeconds = 0.5f;
        public int lanceDamage = 1;
        public LayerMask playerMask;

        public Move Current { get; private set; } = Move.Stand;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        public IReadOnlyList<float> Marks => _marks;
        public int Thrusts { get; private set; }
        public int Lunges { get; private set; }
        public int Surveys { get; private set; }
        public int Counts { get; private set; }
        public int Parried { get; private set; }
        public bool IsErupting => Current == Move.Count && _frames < eruptFrames;
        public int Dir => Facing;

        static readonly Attack[] Phase1 = { Attack.Thrust, Attack.Survey, Attack.Lunge, Attack.Count };
        static readonly Attack[] Phase2 = { Attack.Lunge, Attack.Survey, Attack.Thrust, Attack.Survey, Attack.Count };
        static readonly Attack[] Phase3 = { Attack.Count, Attack.Survey, Attack.Lunge, Attack.Count, Attack.Thrust, Attack.Survey, Attack.Count };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            yield return new BossAttack("Thrust", AttackKind.Strike, Read(thrustTelegraphFrames), lanceDamage, PhasesOf(Attack.Thrust, PatternFor));
            yield return new BossAttack("Lunge", AttackKind.Strike, Read(lungeTelegraphFrames), lanceDamage, PhasesOf(Attack.Lunge, PatternFor));
            yield return new BossAttack("Survey", AttackKind.Shape, Read(surveyTelegraphFrames), 0, PhasesOf(Attack.Survey, PatternFor));
            yield return new BossAttack("Count", AttackKind.Strike, Read(countTelegraphFrames), lanceDamage, PhasesOf(Attack.Count, PatternFor));
        }

        readonly List<float> _marks = new List<float>();
        readonly List<Transform> _markVisuals = new List<Transform>();
        readonly Collider2D[] _overlaps = new Collider2D[4];
        int _frames, _patternIndex;
        float _pause, _approachT, _surveyX;
        bool _telegraphStarted, _hitThisAttack;
        Vector3 _baseScale;
        Attack _forced = Attack.None;

        Attack[] Pattern => Phase >= 3 ? Phase3 : Phase >= 2 ? Phase2 : Phase1;
        int MarksPerSurvey => Mathf.Clamp(Phase, 1, 3);

        protected override void Awake()
        {
            base.Awake();
            if (playerMask.value == 0) playerMask = LayerMask.GetMask("Player");
            _baseScale = Visual != null ? Visual.transform.localScale : Vector3.one;
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
            ClearMarks();
        }

        protected override void OnPhaseStarted(int phase)
        {
            _patternIndex = 0;
            if (phase == 3) MarkAllButOne();   // "the whole floor is marked but one pace"
        }

        protected override void OnDefeated() { ClearMarks(); }

        /// <summary>Tests and tooling: begin a specific attack from wherever he stands.</summary>
        public void ForceAttack(Attack a)
        {
            ClearTelegraph();
            BeginAttack(a);
        }

        void FaceWren()
        {
            if (Wren == null) return;
            Face(Wren.Position.x >= transform.position.x ? 1 : -1);
        }

        Attack NextAttack()
        {
            if (_forced != Attack.None) { var f = _forced; _forced = Attack.None; return f; }
            var p = Pattern;
            var a = p[_patternIndex % p.Length];
            _patternIndex++;
            if (a == Attack.Count && _marks.Count == 0) a = Attack.Survey;   // nothing to call: survey first
            return a;
        }

        void BeginAttack(Attack a)
        {
            CurrentAttack = a;
            _telegraphStarted = false;
            _hitThisAttack = false;
            _frames = 0;
            FaceWren();
            if (a == Attack.Survey && Wren != null) _surveyX = Wren.Position.x;
            Current = Move.Telegraph;
        }

        int TelegraphFrames(Attack a) => a switch
        {
            Attack.Thrust => thrustTelegraphFrames,
            Attack.Lunge => lungeTelegraphFrames,
            Attack.Survey => surveyTelegraphFrames,
            Attack.Count => countTelegraphFrames,
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
                    if (next == Attack.Thrust && Wren != null && Mathf.Abs(Wren.Position.x - transform.position.x) > reach)
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
                        case Attack.Lunge: Current = Move.Lunge; Lunges++; break;
                        case Attack.Survey: Survey(); Current = Move.Recover; break;
                        case Attack.Count: Current = Move.Count; Counts++; break;
                        default: Current = Move.Recover; break;
                    }
                    break;

                case Move.Thrust:
                    LanceCheck();
                    if (++_frames >= thrustFrames) Recover(recoverFrames);
                    break;

                case Move.Lunge:
                    MoveX(Facing * lungeDistance / Mathf.Max(1, lungeFrames));
                    LanceCheck();
                    if (++_frames >= lungeFrames) Recover(recoverFrames);
                    break;

                case Move.Count:
                    if (_frames < eruptFrames) EruptCheck();
                    if (++_frames >= eruptFrames) { ClearMarks(); Recover(recoverFrames); }
                    break;

                case Move.Recover:
                    if (++_frames >= 0 && _frames >= recoverFrames) { Current = Move.Stand; _pause = standSeconds; CurrentAttack = Attack.None; }
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

        // ---- the survey and the count ---------------------------------------------------------------------------------

        void Survey()
        {
            Surveys++;
            int n = MarksPerSurvey;
            float[] offsets = { 0f, markSpacing, -markSpacing };
            for (int i = 0; i < n && i < offsets.Length; i++) AddMark(_surveyX + offsets[i] * Facing);
        }

        /// <summary>Phase 3's opening: every square of the arena floor but the pace she stands in.</summary>
        public void MarkAllButOne()
        {
            float wrenX = Wren != null ? Wren.Position.x : transform.position.x;
            for (float x = arenaMinX + markWidth * 0.5f; x <= arenaMaxX; x += markWidth)
                if (Mathf.Abs(x - wrenX) > markWidth * 1.5f) AddMark(x);
        }

        void AddMark(float x)
        {
            x = Mathf.Clamp(x, arenaMinX, arenaMaxX);
            foreach (var m in _marks) if (Mathf.Abs(m - x) < markWidth * 0.5f) return;
            _marks.Add(x);
            var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
            q.name = "Mark";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform.parent, true);
            q.transform.position = new Vector3(x, floorY + 0.06f, 0f);
            q.transform.localScale = new Vector3(markWidth * 0.9f, 0.12f, 0.2f);
            q.GetComponent<MeshRenderer>().sharedMaterial = InkMaterials.Dark;
            _markVisuals.Add(q.transform);
        }

        void ClearMarks()
        {
            _marks.Clear();
            foreach (var v in _markVisuals) if (v != null) Destroy(v.gameObject);
            _markVisuals.Clear();
        }

        void EruptCheck()
        {
            for (int i = 0; i < _markVisuals.Count; i++)
            {
                var v = _markVisuals[i];
                if (v == null) continue;
                v.position = new Vector3(_marks[i], floorY + eruptHeight * 0.5f, 0f);
                v.localScale = new Vector3(markWidth * 0.9f, eruptHeight, 0.2f);
            }
            if (_hitThisAttack) return;
            foreach (var x in _marks)
                if (Hit(new Vector2(x, floorY + eruptHeight * 0.5f), new Vector2(markWidth, eruptHeight))) return;
        }

        void LanceCheck()
        {
            if (_hitThisAttack) return;
            var b = Collider.bounds;
            var centre = new Vector2(b.center.x + Facing * (b.extents.x + lanceReach * 0.5f), b.center.y - 0.2f);
            Hit(centre, new Vector2(lanceReach, lanceHeight));
        }

        /// <summary>One box against her: parry first (it staggers him and ends the attack), else the lance lands.</summary>
        bool Hit(Vector2 centre, Vector2 size)
        {
            if (Wren == null) return false;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = playerMask, useTriggers = false };
            int n = Physics2D.OverlapBox(centre, size, 0f, filter, _overlaps);
            for (int i = 0; i < n; i++)
            {
                var vitals = _overlaps[i].GetComponentInParent<WrenVitals>();
                if (vitals == null) continue;
                var belt = vitals.GetComponent<InstrumentBelt>();
                if (belt != null && belt.TryParry(this))
                {
                    Parried++;
                    _hitThisAttack = true;
                    ClearTelegraph();
                    Recover(parriedRecoverFrames);
                    return true;
                }
                if (vitals.Damage(lanceDamage, transform.position)) _hitThisAttack = true;
                return true;
            }
            return false;
        }

        protected override void Update()
        {
            base.Update();
            if (Visual == null || IsDying) return;
            float lean = Current == Move.Telegraph ? 0.85f : Current == Move.Thrust || Current == Move.Lunge ? 1.5f : 1f;
            var s = Visual.transform.localScale;
            Visual.transform.localScale = new Vector3(Facing * Mathf.Abs(_baseScale.x) * lean, s.y, s.z);
        }

        protected override Color TintColor() => new Color(0.20f, 0.24f, 0.44f);
    }
}
