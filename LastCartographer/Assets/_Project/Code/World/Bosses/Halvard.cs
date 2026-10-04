using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Warden-Sergeant Halvard, hunted three times (bible 6.3, boss sheet 6.3, CMB-12). One component, three kits, the
    /// hunt chosen by the sheet's tier (or set outright).
    /// <list type="bullet">
    /// <item><b>The Salt Chapel (I).</b> A grounded lancer: he closes to reach, thrusts (parry it), lunges (jump it), and
    /// <b>surveys</b>: plants the lance and marks squares of the floor under her; the marks stay, and when he <b>calls
    /// the count</b> every mark erupts. Phase 3 opens with the whole floor marked but one pace.</item>
    /// <item><b>The Seven Bridges (II).</b> A second lance on a cord: he <b>throws</b> it level (parry it going out) and
    /// recalls it low (jump it coming back). The floor is six spans of bridge; a survey marks spans and the count
    /// <b>cuts</b> them: a marked span falls into the drop and is not there to stand on. Phase 3 is fought on the last
    /// span, the rest gone.</item>
    /// <item><b>The Threshold (III).</b> Everything: both lances, the marks, the count, and the Blank eating the arena
    /// from the west a quarter at phase 2 and half at phase 3. Halfway through he stops counting: phase 3 has no
    /// survey and no count.</item>
    /// </list>
    /// Always hittable; a parried lance staggers him. He does not die; at zero he withdraws (the arena sets the flag
    /// and he deactivates).
    /// </summary>
    public sealed class Halvard : Boss
    {
        public enum Hunt { Chapel, Bridges, Threshold }
        public enum Attack { None, Thrust, Lunge, Survey, Count, Throw }
        public enum Move { Stand, Approach, Telegraph, Thrust, Lunge, Count, Throw, Recall, Recover }

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

        [Header("The later hunts (CMB-12)")]
        /// <summary>Which hunt this is; Chapel means "by the sheet's tier".</summary>
        public Hunt hunt = Hunt.Chapel;
        public int throwTelegraphFrames = 16, throwFrames = 4, hangFrames = 6, recallFrames = 3;
        public float throwSpeed = 14f, throwReach = 7.5f, recallHeight = 0.45f;
        public Vector2 cordLanceSize = new Vector2(1.6f, 0.4f);
        /// <summary>The bridge's spans across the arena (the Seven Bridges).</summary>
        public int sections = 6;
        /// <summary>Spans a phase-3 bridge keeps round her: the last span.</summary>
        public int lastSpanSections = 3;
        /// <summary>How much of the arena the Blank has eaten from the west by phase, at the Threshold.</summary>
        public float whiteAtPhaseTwo = 0.25f, whiteAtPhaseThree = 0.5f;

        public Move Current { get; private set; } = Move.Stand;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        /// <summary>The tell for the attack being telegraphed (AUD-03): its kind in the kit.</summary>
        protected override AttackKind TelegraphKind => CurrentAttack switch { Attack.Survey => AttackKind.Shape, _ => AttackKind.Strike };
        public IReadOnlyList<float> Marks => _marks;
        public int Thrusts { get; private set; }
        public int Lunges { get; private set; }
        public int Surveys { get; private set; }
        public int Counts { get; private set; }
        public int Throws { get; private set; }
        public int Parried { get; private set; }
        public bool IsErupting => Current == Move.Count && _frames < eruptFrames;
        public int Dir => Facing;

        /// <summary>The hunt this is: set outright, else the sheet's tier says.</summary>
        public Hunt CurrentHunt => hunt != Hunt.Chapel ? hunt : Tier >= 3 ? Hunt.Threshold : Tier == 2 ? Hunt.Bridges : Hunt.Chapel;
        /// <summary>Whether he still counts: at the Threshold he stops in phase 3.</summary>
        public bool IsCounting => !(CurrentHunt == Hunt.Threshold && Phase >= 3);
        /// <summary>The cord lance while it is out, else null.</summary>
        public BossPart CordLance => _lanceOut ? _lance : null;
        public bool IsLanceOut => _lanceOut;
        /// <summary>The east edge of the white eating the Threshold's arena; the arena's west edge until it does.</summary>
        public float WhiteX { get; private set; }
        public UntetheredZone White => _white;

        /// <summary>The sheet clip for his move (CHR-07): the survey, the count and the throw have their own telegraphs.</summary>
        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip : Current switch
        {
            Move.Approach => "move",
            Move.Telegraph => CurrentAttack == Attack.Survey ? "survey" : CurrentAttack == Attack.Count ? "call" : CurrentAttack == Attack.Throw ? "aim" : "telegraph",
            Move.Thrust => "thrust",
            Move.Lunge => "lunge",
            Move.Count => "count",
            Move.Throw => _frames < throwFrames ? "throw" : "cord",
            Move.Recall => "recall",
            Move.Recover => "recover",
            _ => "idle",
        };

        static readonly Attack[] Chapel1 = { Attack.Thrust, Attack.Survey, Attack.Lunge, Attack.Count };
        static readonly Attack[] Chapel2 = { Attack.Lunge, Attack.Survey, Attack.Thrust, Attack.Survey, Attack.Count };
        static readonly Attack[] Chapel3 = { Attack.Count, Attack.Survey, Attack.Lunge, Attack.Count, Attack.Thrust, Attack.Survey, Attack.Count };
        static readonly Attack[] Bridges1 = { Attack.Thrust, Attack.Throw, Attack.Lunge };
        static readonly Attack[] Bridges2 = { Attack.Survey, Attack.Throw, Attack.Count, Attack.Thrust };
        static readonly Attack[] Bridges3 = { Attack.Throw, Attack.Lunge, Attack.Thrust, Attack.Throw };
        static readonly Attack[] Threshold1 = { Attack.Thrust, Attack.Throw, Attack.Survey, Attack.Count };
        static readonly Attack[] Threshold2 = { Attack.Lunge, Attack.Throw, Attack.Survey, Attack.Count };
        static readonly Attack[] Threshold3 = { Attack.Throw, Attack.Thrust, Attack.Lunge };

        public static IReadOnlyList<Attack> PatternFor(Hunt hunt, int phase) => hunt switch
        {
            Hunt.Bridges => phase >= 3 ? Bridges3 : phase >= 2 ? Bridges2 : Bridges1,
            Hunt.Threshold => phase >= 3 ? Threshold3 : phase >= 2 ? Threshold2 : Threshold1,
            _ => phase >= 3 ? Chapel3 : phase >= 2 ? Chapel2 : Chapel1,
        };
        /// <summary>The first hunt's pattern.</summary>
        public static IReadOnlyList<Attack> PatternFor(int phase) => PatternFor(Hunt.Chapel, phase);

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md): this hunt's attacks.</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            var h = CurrentHunt;
            System.Func<int, IReadOnlyList<Attack>> pattern = p => PatternFor(h, p);
            yield return new BossAttack("Thrust", AttackKind.Strike, Read(thrustTelegraphFrames), lanceDamage, PhasesOf(Attack.Thrust, pattern));
            yield return new BossAttack("Lunge", AttackKind.Strike, Read(lungeTelegraphFrames), lanceDamage, PhasesOf(Attack.Lunge, pattern));
            yield return new BossAttack("Survey", AttackKind.Shape, Read(surveyTelegraphFrames), 0, PhasesOf(Attack.Survey, pattern));
            yield return new BossAttack(h == Hunt.Bridges ? "The count (cuts the span)" : "Count", AttackKind.Strike, Read(countTelegraphFrames), lanceDamage, PhasesOf(Attack.Count, pattern));
            if (h != Hunt.Chapel)
                yield return new BossAttack("Cord lance", AttackKind.Strike, Read(throwTelegraphFrames), lanceDamage, PhasesOf(Attack.Throw, pattern));
        }

        /// <summary>The pieces the later hunts make wear their own sheets (CHR-07): the cord lance in flight, a span of bridge.</summary>
        public override IEnumerable<string> PartSkinNames { get { yield return "CordLance"; yield return "BridgeSpan"; } }

        readonly List<float> _marks = new List<float>();
        readonly List<Transform> _markVisuals = new List<Transform>();
        readonly Collider2D[] _overlaps = new Collider2D[4];
        int _frames, _patternIndex;
        float _pause, _approachT, _surveyX;
        bool _telegraphStarted, _hitThisAttack, _drawnMarks;
        Vector3 _baseScale;
        Attack _forced = Attack.None;

        // the cord lance
        BossPart _lance;
        Transform _cord;
        bool _lanceOut, _lanceSkinned;
        float _lanceDist;
        int _lanceDir, _hang;

        // the bridge
        readonly List<Collider2D> _floorHidden = new List<Collider2D>();
        readonly List<Renderer> _floorRenderersHidden = new List<Renderer>();
        readonly List<GameObject> _floorKept = new List<GameObject>();
        readonly List<GameObject> _spans = new List<GameObject>();
        readonly List<Transform> _spanProps = new List<Transform>();
        readonly List<bool> _spanSkinned = new List<bool>();
        readonly List<(Transform prop, float t)> _falling = new List<(Transform, float)>();
        bool[] _cut = new bool[0];

        // the white
        UntetheredZone _white;
        Transform _whiteFloor;

        Attack[] Pattern => (Attack[])PatternFor(CurrentHunt, Phase);
        int MarksPerSurvey => CurrentHunt == Hunt.Bridges ? Mathf.Clamp(Phase, 1, 2) : Mathf.Clamp(Phase, 1, 3);
        /// <summary>The west edge he and his marks keep to: the arena's, or the white's at the Threshold.</summary>
        float MinX => CurrentHunt == Hunt.Threshold ? Mathf.Max(arenaMinX, WhiteX) : arenaMinX;

        protected override void Awake()
        {
            base.Awake();
            if (playerMask.value == 0) playerMask = LayerMask.GetMask("Player");
            _baseScale = Visual != null ? Visual.transform.localScale : Vector3.one;
            WhiteX = arenaMinX;
        }

        protected override void OnFightStarted()
        {
            Current = Move.Stand;
            _pause = standSeconds;
            _patternIndex = 0;
            WhiteX = arenaMinX;
            if (CurrentHunt == Hunt.Bridges) BuildSpans();
            if (CurrentHunt != Hunt.Chapel) MakeLance();
            FaceWren();
        }

        protected override void OnFightReset()
        {
            Current = Move.Stand;
            CurrentAttack = Attack.None;
            _telegraphStarted = false;
            ClearMarks();
            PutAwayLance();
            RestoreFloor();
            ClearWhite();
        }

        protected override void OnPhaseStarted(int phase)
        {
            _patternIndex = 0;
            switch (CurrentHunt)
            {
                case Hunt.Chapel:
                    if (phase == 3) MarkAllButOne();   // "the whole floor is marked but one pace"
                    break;
                case Hunt.Bridges:
                    if (phase == 3) CutToTheLastSpan();   // "phase 3 is fought on the last span"
                    break;
                case Hunt.Threshold:
                    if (phase == 2) SetWhite(whiteAtPhaseTwo);
                    if (phase == 3) { SetWhite(whiteAtPhaseThree); ClearMarks(); }   // he stops counting
                    break;
            }
        }

        protected override void OnDefeated()
        {
            ClearMarks();
            PutAwayLance();
            RestoreFloor();   // "he leaves the span standing"
            ClearWhite();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RestoreFloor();
        }

        /// <summary>Tests and tooling: begin a specific attack from wherever he stands.</summary>
        public void ForceAttack(Attack a)
        {
            ClearTelegraph();
            PutAwayLance();
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
            if (a == Attack.Count && _marks.Count == 0) a = IsCounting ? Attack.Survey : Attack.Thrust;   // nothing to call: survey first
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
            Attack.Throw => throwTelegraphFrames,
            _ => thrustTelegraphFrames,
        };

        protected override void Tick(float dt)
        {
            TickFalling(dt);
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
                        case Attack.Throw: LaunchLance(); Current = Move.Throw; Throws++; break;
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
                    if (_frames == 0 && CurrentHunt == Hunt.Bridges) CutMarkedSpans();
                    if (_frames < eruptFrames) EruptCheck();
                    if (++_frames >= eruptFrames) { ClearMarks(); Recover(recoverFrames); }
                    break;

                case Move.Throw:
                    TickThrow(dt);
                    break;

                case Move.Recall:
                    TickRecall(dt);
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
            float min = MinX, max = arenaMaxX;
            if (CurrentHunt == Hunt.Bridges && _cut.Length > 0) StandingRun(SectionOf(p.x), out min, out max);
            p.x = Mathf.Clamp(p.x + dx, min, max);
            transform.position = p;
            Body.position = p;
        }

        // ---- the survey and the count ---------------------------------------------------------------------------------

        void Survey()
        {
            Surveys++;
            int n = MarksPerSurvey;
            if (CurrentHunt == Hunt.Bridges)
            {
                // The marks are spans: hers, then the next along his facing, then the one behind her.
                int at = SectionOf(_surveyX);
                int[] offsets = { 0, Facing, -Facing };
                for (int i = 0; i < n && i < offsets.Length; i++)
                {
                    int s = at + offsets[i];
                    if (s >= 0 && s < sections && !_cut[s]) AddMark(SectionCentre(s));
                }
                return;
            }
            float[] steps = { 0f, markSpacing, -markSpacing };
            for (int i = 0; i < n && i < steps.Length; i++) AddMark(_surveyX + steps[i] * Facing);
        }

        /// <summary>Phase 3's opening in the chapel: every square of the arena floor but the pace she stands in.</summary>
        public void MarkAllButOne()
        {
            float wrenX = Wren != null ? Wren.Position.x : transform.position.x;
            for (float x = MinX + markWidth * 0.5f; x <= arenaMaxX; x += markWidth)
                if (Mathf.Abs(x - wrenX) > markWidth * 1.5f) AddMark(x);
        }

        void AddMark(float x)
        {
            x = Mathf.Clamp(x, MinX, arenaMaxX);
            foreach (var m in _marks) if (Mathf.Abs(m - x) < markWidth * 0.5f) return;
            _marks.Add(x);
            var drawn = InkFx.Mark("mark", new Vector2(x, floorY + 0.08f), markWidth / 1.6f);   // the clip's square is 1.6 wide in a 3-unit cell
            if (drawn != null)
            {
                drawn.SetParent(transform.parent, true);
                _markVisuals.Add(drawn);
                _drawnMarks = true;
                return;
            }
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
            if (_drawnMarks)
            {
                // Drawn: each mark erupts as a column of ink once, the square staying under it.
                if (_frames == 0) foreach (var x in _marks) InkFx.Spawn("erupt", new Vector2(x, floorY + 1.1f), 0f, Mathf.Max(0.8f, markWidth / 1.4f));
            }
            else
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

        // ---- the cord lance (the Seven Bridges, the Threshold) ----------------------------------------------------------

        void MakeLance()
        {
            if (_lance != null) return;
            _lance = BossPart.Make("CordLance", transform.parent, new Vector2(transform.position.x, floorY + lanceHeight), cordLanceSize,
                InkMaterials.Lit("Halvard_CordLance", new Color(0.78f, 0.62f, 0.30f)));
            _lance.OnHit = _ => false;   // the quill does not stop a thrown lance
            _lance.Visual.localScale = new Vector3(cordLanceSize.x, cordLanceSize.y * 0.5f, 0.3f);
            _lanceSkinned = Skin(_lance, "CordLance", "fly");
            _cord = BossPart.Prop("Cord", transform.parent, _lance.Position, new Vector2(1f, 0.05f), InkMaterials.Dark, 0.2f);
            _lance.gameObject.SetActive(false);
            _cord.gameObject.SetActive(false);
        }

        void LaunchLance()
        {
            if (_lance == null) MakeLance();
            _lanceOut = true;
            _lanceDir = Facing;
            _lanceDist = 0.6f;
            _hang = 0;
            _lance.gameObject.SetActive(true);
            _cord.gameObject.SetActive(true);
            _lance.Play("fly", true);
            PlaceLance(floorY + lanceHeight, false);
        }

        void PlaceLance(float y, bool back)
        {
            var from = new Vector2(transform.position.x, floorY + lanceHeight);
            var at = new Vector2(transform.position.x + _lanceDir * _lanceDist, y);
            _lance.MoveTo(at);
            var s = _lance.Visual.localScale;
            float facing = (back ? -_lanceDir : _lanceDir) * Mathf.Abs(s.x);
            _lance.Visual.localScale = new Vector3(facing, s.y, s.z);
            var mid = (from + at) * 0.5f;
            _cord.position = new Vector3(mid.x, mid.y, 0.2f);
            float len = Vector2.Distance(from, at);
            _cord.localScale = new Vector3(Mathf.Max(0.1f, len), 0.04f, 0.1f);
            _cord.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(at.y - from.y, at.x - from.x) * Mathf.Rad2Deg);
        }

        /// <summary>How far the lance can fly before the cord or the arena stops it.</summary>
        float LanceReachNow()
        {
            float edge = _lanceDir > 0 ? arenaMaxX - transform.position.x : transform.position.x - MinX;
            return Mathf.Max(1f, Mathf.Min(throwReach, edge - 0.3f));
        }

        void TickThrow(float dt)
        {
            float reachNow = LanceReachNow();
            if (_lanceDist < reachNow)
            {
                _lanceDist = Mathf.Min(reachNow, _lanceDist + throwSpeed * dt);
                PlaceLance(floorY + lanceHeight, false);
                if (!_hitThisAttack && CordLanceHit(parryable: true)) return;
                _frames++;
                return;
            }
            PlaceLance(floorY + lanceHeight, false);
            _frames++;
            if (++_hang >= hangFrames)
            {
                Current = Move.Recall; _frames = 0; _hitThisAttack = false;
                PlaceLance(floorY + recallHeight, true);   // it drops to come back low
            }
        }

        void TickRecall(float dt)
        {
            _lanceDist -= throwSpeed * dt;
            if (_lanceDist <= 0.6f)
            {
                PutAwayLance();
                Recover(recoverFrames);
                return;
            }
            PlaceLance(floorY + recallHeight, true);   // it comes back low: jump it
            if (!_hitThisAttack) CordLanceHit(parryable: false);
            _frames++;
        }

        /// <summary>The flying lance against her; parried on the way out it drops, and he hauls it in staggered.</summary>
        bool CordLanceHit(bool parryable)
        {
            var contact = HitWren(_lance.Position, cordLanceSize, lanceDamage, parryable);
            if (contact == Contact.None) return false;
            _hitThisAttack = true;
            if (contact == Contact.Parried)
            {
                Parried++;
                PutAwayLance();
                Recover(parriedRecoverFrames);
                return true;
            }
            return false;
        }

        void PutAwayLance()
        {
            _lanceOut = false;
            if (_lance != null) _lance.gameObject.SetActive(false);
            if (_cord != null) _cord.gameObject.SetActive(false);
        }

        // ---- the spans (the Seven Bridges) ---------------------------------------------------------------------------

        public int Sections => sections;
        public float SectionWidth => (arenaMaxX - arenaMinX) / Mathf.Max(1, sections);
        public int SectionOf(float x) => Mathf.Clamp(Mathf.FloorToInt((x - arenaMinX) / SectionWidth), 0, sections - 1);
        public float SectionCentre(int i) => arenaMinX + (i + 0.5f) * SectionWidth;
        public bool IsCut(int i) => i >= 0 && i < _cut.Length && _cut[i];
        public int SpansStanding { get { int n = 0; foreach (var c in _cut) if (!c) n++; return n; } }
        public bool HasSpans => _spans.Count > 0;

        /// <summary>The bridge is spans: the room's floor under the arena stands aside and six pieces of it stand in, each one cuttable.</summary>
        void BuildSpans()
        {
            if (_spans.Count > 0) return;
            _cut = new bool[sections];
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Ground"), useTriggers = false };
            var hits = new List<Collider2D>();
            Physics2D.OverlapArea(new Vector2(arenaMinX + 0.1f, floorY - 0.9f), new Vector2(arenaMaxX - 0.1f, floorY - 0.1f), filter, hits);
            foreach (var c in hits)
            {
                if (c == null || !c.enabled || Mathf.Abs(c.bounds.max.y - floorY) > 0.15f) continue;
                if (c.transform.IsChildOf(transform)) continue;
                // The floor past the arena stays: a copy of it for each end, cut to the arena's edge.
                var b = c.bounds;
                if (b.min.x < arenaMinX - 0.05f) KeepFloor(c, b.min.x, arenaMinX);
                if (b.max.x > arenaMaxX + 0.05f) KeepFloor(c, arenaMaxX, b.max.x);
                c.enabled = false;
                _floorHidden.Add(c);
                foreach (var r in c.GetComponentsInChildren<Renderer>())
                    if (r.enabled) { r.enabled = false; _floorRenderersHidden.Add(r); }
            }
            int ground = LayerMask.NameToLayer("Ground");
            var skin = FindSkin("BridgeSpan");
            var granite = InkMaterials.Lit("Halvard_Span", new Color(0.56f, 0.56f, 0.54f));
            for (int i = 0; i < sections; i++)
            {
                var go = new GameObject("Span_" + i) { layer = ground };
                go.transform.SetParent(transform.parent, false);
                go.transform.position = new Vector3(SectionCentre(i), floorY - 0.5f, 0f);
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(SectionWidth, 1f);
                go.AddComponent<SolidGround>();
                _spans.Add(go);
                bool skinned = skin != null;
                // Drawn, the span's deck is a unit above its cell's centre (boss_parts.py BridgeSpan); a block is the floor's own shape.
                var prop = BossPart.Prop("SpanDrawing_" + i, transform.parent, new Vector2(SectionCentre(i), skinned ? floorY - 1f : floorY - 0.5f),
                    new Vector2(SectionWidth, 1f), granite, 0.25f);
                if (skinned)
                {
                    SkinProp(prop, "BridgeSpan", "idle");
                    var s = prop.localScale;
                    prop.localScale = new Vector3(s.x * SectionWidth / Mathf.Max(0.1f, skin.CellUnits), s.y, s.z);
                }
                _spanProps.Add(prop);
                _spanSkinned.Add(skinned);
            }
        }

        /// <summary>A copy of a floor for the stretch of it outside the arena: same object, same skin, scaled to the stretch.</summary>
        void KeepFloor(Collider2D floor, float x0, float x1)
        {
            float width = floor.bounds.size.x;
            if (x1 - x0 < 0.05f || width < 0.05f) return;
            var src = floor.transform;
            var copy = Instantiate(src.gameObject, src.parent);
            copy.name = src.name + "_Kept";
            var p = src.position;
            copy.transform.position = new Vector3(p.x + ((x0 + x1) * 0.5f - floor.bounds.center.x), p.y, p.z);
            var sc = src.localScale;
            copy.transform.localScale = new Vector3(sc.x * (x1 - x0) / width, sc.y, sc.z);
            _floorKept.Add(copy);
        }

        /// <summary>The count on the bridge: every marked span but his own falls, as long as the last span is left to stand on.</summary>
        void CutMarkedSpans()
        {
            if (_cut.Length == 0) return;
            int his = SectionOf(transform.position.x);
            // The marked spans first: cutting a span wipes its marks from the list being read.
            _toCut.Clear();
            foreach (var x in _marks) { int s = SectionOf(x); if (s != his && !_toCut.Contains(s)) _toCut.Add(s); }
            foreach (var s in _toCut)
            {
                if (SpansStanding <= lastSpanSections) break;
                Cut(s);
            }
        }

        readonly List<int> _toCut = new List<int>();

        /// <summary>Phase 3: the spans round her stay (the last span); the rest fall, and he steps onto the far end of it.</summary>
        public void CutToTheLastSpan()
        {
            if (_cut.Length == 0) return;
            int at = SectionOf(Wren != null ? Wren.Position.x : transform.position.x);
            int keepMin = Mathf.Clamp(at - lastSpanSections / 2, 0, Mathf.Max(0, sections - lastSpanSections));
            int keepMax = keepMin + lastSpanSections - 1;
            for (int i = 0; i < sections; i++) if (i < keepMin || i > keepMax) Cut(i);
            // he keeps to the span: on its far end from her
            float farEnd = at - keepMin <= keepMax - at ? SectionCentre(keepMax) : SectionCentre(keepMin);
            var p = transform.position; p.x = farEnd; transform.position = p; Body.position = p;
            ClearMarks();
        }

        void Cut(int i)
        {
            if (i < 0 || i >= _cut.Length || _cut[i]) return;
            _cut[i] = true;
            var col = _spans[i].GetComponent<BoxCollider2D>();
            if (col != null) col.enabled = false;
            var prop = _spanProps[i];
            if (prop != null)
            {
                if (_spanSkinned[i]) BossPart.Show(prop, "fall", null);
                _falling.Add((prop, 0f));
            }
            for (int m = _marks.Count - 1; m >= 0; m--)
                if (SectionOf(_marks[m]) == i) { if (_markVisuals[m] != null) Destroy(_markVisuals[m].gameObject); _markVisuals.RemoveAt(m); _marks.RemoveAt(m); }
        }

        /// <summary>A cut span drops out of sight: the drawing plays its fall, a block falls on its own.</summary>
        void TickFalling(float dt)
        {
            for (int i = _falling.Count - 1; i >= 0; i--)
            {
                var (prop, t) = _falling[i];
                if (prop == null) { _falling.RemoveAt(i); continue; }
                t += dt;
                int index = _spanProps.IndexOf(prop);
                if (index < 0 || !_spanSkinned[index]) prop.position += Vector3.down * (6f * dt + 12f * t * dt);
                if (t >= 0.6f) { prop.gameObject.SetActive(false); _falling.RemoveAt(i); }
                else _falling[i] = (prop, t);
            }
        }

        /// <summary>The run of standing spans round a section, as x bounds; the arena's when the span is cut under him.</summary>
        void StandingRun(int section, out float min, out float max)
        {
            min = arenaMinX; max = arenaMaxX;
            if (_cut.Length == 0 || section < 0 || section >= sections || _cut[section]) return;
            int lo = section, hi = section;
            while (lo > 0 && !_cut[lo - 1]) lo--;
            while (hi < sections - 1 && !_cut[hi + 1]) hi++;
            min = arenaMinX + lo * SectionWidth + 0.4f;
            max = arenaMinX + (hi + 1) * SectionWidth - 0.4f;
        }

        void RestoreFloor()
        {
            foreach (var c in _floorHidden) if (c != null) c.enabled = true;
            _floorHidden.Clear();
            foreach (var r in _floorRenderersHidden) if (r != null) r.enabled = true;
            _floorRenderersHidden.Clear();
            foreach (var k in _floorKept) if (k != null) Destroy(k);
            _floorKept.Clear();
            foreach (var s in _spans) if (s != null) Destroy(s);
            _spans.Clear();
            foreach (var p in _spanProps) if (p != null) Destroy(p.gameObject);
            _spanProps.Clear();
            _spanSkinned.Clear();
            _falling.Clear();
            _cut = new bool[0];
        }

        // ---- the white (the Threshold) ---------------------------------------------------------------------------------

        /// <summary>The Blank eats the arena from the west: a white patch nothing holds her in without Clarity, and his edge moves with it.</summary>
        void SetWhite(float fraction)
        {
            ClearWhite();
            WhiteX = arenaMinX + Mathf.Clamp01(fraction) * (arenaMaxX - arenaMinX);
            float w = WhiteX - arenaMinX;
            if (w <= 0.01f) return;
            var centre = new Vector2((arenaMinX + WhiteX) * 0.5f, floorY + 2f);
            _white = UntetheredZone.Make("White_Halvard", transform.parent, centre, new Vector2(w, 4f));
            _whiteFloor = BossPart.Prop("WhiteFloor", transform.parent, new Vector2(centre.x, floorY + 0.03f), new Vector2(w, 0.06f),
                InkMaterials.Lit("Halvard_White", new Color(0.97f, 0.97f, 0.95f)), 0.1f);
            if (transform.position.x < WhiteX) MoveX(WhiteX + 0.5f - transform.position.x);
        }

        void ClearWhite()
        {
            if (_white != null) Destroy(_white.gameObject);
            if (_whiteFloor != null) Destroy(_whiteFloor.gameObject);
            _white = null; _whiteFloor = null;
            WhiteX = arenaMinX;
        }

        protected override void Update()
        {
            base.Update();
            if (Visual == null || IsDying || HasSheets) return;   // the lean is the block's; the frames carry it when drawn
            float lean = Current == Move.Telegraph ? 0.85f : Current == Move.Thrust || Current == Move.Lunge ? 1.5f : 1f;
            var s = Visual.transform.localScale;
            Visual.transform.localScale = new Vector3(Facing * Mathf.Abs(_baseScale.x) * lean, s.y, s.z);
        }

        protected override Color TintColor() => new Color(0.20f, 0.24f, 0.44f);
    }
}
