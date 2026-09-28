using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Warden-Captain Oriel (bible 6.8, boss sheet 6.8, CMB-14). Tier III, on the Bastion's chalk-lined drill-yard; she
    /// has read Pell's report and fights to see if it is accurate. She mirrors Wren's kit: phase 1 is Wren's own
    /// Charter's combo, reversed (every step parryable); phase 2 adds Wren's default Flourish; at a third she steps
    /// away and Binds, once, and the fight is about denying it (a hit while she binds stops it). Beaten without a mask
    /// lost, she stands the Wardens down (<see cref="Licence.StoodDownFlag"/>).
    /// </summary>
    public sealed class Oriel : Boss
    {
        public enum Attack { None, Combo, Flourish, Step, Bind }
        public enum Move { Stand, Approach, Telegraph, Strike, Flourish, Step, Bind, Recover }

        [Header("Oriel")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        public float walkSpeed = 4.5f, reach = 2.4f, approachSeconds = 1.2f;
        public float stepDistance = 3.5f;
        public int stepFrames = 8;
        /// <summary>Each mirrored step's wind-up is its startup times this (never under the tier's minimum).</summary>
        public int telegraphScale = 3;
        public int flourishTelegraphFrames = 16;
        public int crosshatchFrames = 18, longstrokeFrames = 3, blotFrames = 4;
        public float crosshatchReach = 1.8f, longstrokeReach = 6f, blotRadius = 2.2f;
        public int bindFrames = 60;
        [Range(0f, 1f)] public float bindHealFraction = 1f / 3f;
        public int recoverFrames = 16;
        public float parriedStaggerSeconds = 1f, deniedStaggerSeconds = 0.8f;
        public float standSeconds = 0.35f;
        public int damage = 1;

        public Move Current { get; private set; } = Move.Stand;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        /// <summary>The Charter she mirrors: Wren's, read as the fight begins.</summary>
        public CharterKind Mirror { get; private set; } = CharterKind.Surveyor;
        public IReadOnlyList<ComboStep> MirrorCombo => _combo;
        public FlourishKind MirrorFlourish { get; private set; } = FlourishKind.Crosshatch;
        /// <summary>The combo step under way (0-based), -1 outside a combo.</summary>
        public int StepIndex => CurrentAttack == Attack.Combo ? _step : -1;
        public bool IsBinding => Current == Move.Bind;
        public bool BindSpent { get; private set; }
        /// <summary>What she will do next before her pattern resumes (the step and the Bind at a third).</summary>
        public IEnumerable<Attack> Queued => _queue;
        public int Combos { get; private set; }
        public int Strikes { get; private set; }
        public int Flourishes { get; private set; }
        public int Steps { get; private set; }
        public int Parried { get; private set; }
        public int BindsDenied { get; private set; }
        public int BindsCompleted { get; private set; }
        /// <summary>Masks Wren has lost in this attempt, to anything.</summary>
        public int MasksTaken { get; private set; }
        public bool StoodDown { get; private set; }
        public int Dir => Facing;

        static readonly Attack[] Phase1 = { Attack.Combo, Attack.Step, Attack.Combo };
        static readonly Attack[] Phase2 = { Attack.Combo, Attack.Flourish, Attack.Step };
        static readonly Attack[] Phase3 = { Attack.Flourish, Attack.Combo, Attack.Step, Attack.Combo };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md). The mirrored combo is read from
        /// its first step: the Charter she reads, backwards.</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            ComboStep first;
            if (_combo.Count > 0) first = _combo[0];
            else { var c = ProfileOf(GameState.World.Equipment.Charter).Combo; first = c[c.Length - 1]; }
            yield return new BossAttack("Mirrored combo", AttackKind.Strike, Read(first.Startup * telegraphScale), damage, PhasesOf(Attack.Combo, PatternFor));
            yield return new BossAttack("Flourish", AttackKind.Strike, Read(flourishTelegraphFrames), damage, PhasesOf(Attack.Flourish, PatternFor));
            yield return new BossAttack("Step", AttackKind.Shape, 0, 0, PhasesOf(Attack.Step, PatternFor));
            yield return new BossAttack("Bind", AttackKind.Shape, bindFrames, 0, 0b100);
        }

        readonly List<ComboStep> _combo = new List<ComboStep>();
        readonly Queue<Attack> _queue = new Queue<Attack>();
        int _frames, _patternIndex, _step, _stepDir;
        float _pause, _approachT;
        bool _telegraphStarted, _hitThisAttack;
        WrenVitals _vitals;

        /// <summary>The Charter's profile by kind, the late Charters too.</summary>
        public static CharterProfile ProfileOf(CharterKind k) => CharterProfile.For(k);

        /// <summary>Tests and tooling: begin a specific attack now.</summary>
        public void ForceAttack(Attack a)
        {
            ClearTelegraph();
            BeginAttack(a);
        }

        protected override void OnFightStarted()
        {
            var profile = ProfileOf(GameState.World.Equipment.Charter);
            Mirror = profile.Kind;
            MirrorFlourish = profile.DefaultFlourish;
            _combo.Clear();
            _combo.AddRange(profile.Combo);
            _combo.Reverse();   // her own stance, read backwards
            Current = Move.Stand;
            _pause = standSeconds;
            _patternIndex = 0;
            _queue.Clear();
            BindSpent = false;
            MasksTaken = 0;
            StoodDown = false;
            Listen(true);
            FaceWren();
        }

        protected override void OnFightReset()
        {
            Listen(false);
            Current = Move.Stand;
            CurrentAttack = Attack.None;
            _telegraphStarted = false;
            _queue.Clear();
        }

        protected override void OnPhaseStarted(int phase)
        {
            _patternIndex = 0;
            if (phase == 3 && !BindSpent)
            {
                // At a third: step clear, then Bind.
                _queue.Clear();
                _queue.Enqueue(Attack.Step);
                _queue.Enqueue(Attack.Bind);
            }
        }

        protected override void OnDefeated()
        {
            Listen(false);
            StoodDown = MasksTaken == 0;
            if (StoodDown) GameState.World.Set(Licence.StoodDownFlag, true);
        }

        void Listen(bool on)
        {
            if (_vitals == null && Wren != null) _vitals = Wren.GetComponent<WrenVitals>();
            if (_vitals == null) return;
            _vitals.Hurt -= OnWrenHurt;
            if (on) _vitals.Hurt += OnWrenHurt;
        }

        void OnWrenHurt() { if (IsFightActive) MasksTaken++; }

        void FaceWren()
        {
            if (Wren == null) return;
            Face(Wren.Position.x >= transform.position.x ? 1 : -1);
        }

        float DistanceToWren => Wren == null ? 99f : Mathf.Abs(Wren.Position.x - transform.position.x);

        Attack NextAttack()
        {
            if (_queue.Count > 0) return _queue.Dequeue();
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
            _step = 0;
            FaceWren();
            switch (a)
            {
                case Attack.Step:
                    Steps++;
                    _stepDir = DistanceToWren < reach ? -Facing : Facing;   // clear of her, or into reach
                    Current = Move.Step;
                    break;
                case Attack.Bind:
                    if (BindSpent) { Rest(); break; }
                    BindSpent = true;
                    Current = Move.Bind;
                    break;
                case Attack.Combo:
                    Combos++;
                    Current = Move.Telegraph;
                    break;
                default:
                    Current = Move.Telegraph;
                    break;
            }
        }

        int TelegraphFrames() => CurrentAttack == Attack.Combo && _combo.Count > 0
            ? _combo[_step].Startup * telegraphScale
            : flourishTelegraphFrames;

        protected override void Tick(float dt)
        {
            switch (Current)
            {
                case Move.Stand:
                    _pause -= dt;
                    if (_pause > 0f) break;
                    FaceWren();
                    var next = NextAttack();
                    if (next == Attack.Combo && DistanceToWren > reach)
                    {
                        var rest = new List<Attack>(_queue);
                        _queue.Clear();
                        _queue.Enqueue(next);
                        foreach (var r in rest) _queue.Enqueue(r);
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
                    if (!Telegraph(ref _telegraphStarted, TelegraphFrames())) break;
                    _frames = 0;
                    _hitThisAttack = false;
                    if (CurrentAttack == Attack.Combo) { Current = Move.Strike; Strikes++; }
                    else if (CurrentAttack == Attack.Flourish) { Current = Move.Flourish; Flourishes++; }
                    else Rest();
                    break;

                case Move.Strike:
                {
                    var s = _combo[_step];
                    var b = Collider.bounds;
                    var centre = new Vector2(b.center.x + Facing * (b.extents.x + s.Reach * 0.5f), floorY + 0.4f + s.Thickness * 0.5f);
                    if (!_hitThisAttack)
                    {
                        var c = HitWren(centre, new Vector2(s.Reach, s.Thickness), damage, true);
                        if (c == Contact.Landed) _hitThisAttack = true;
                        else if (c == Contact.Parried) { Parried++; Stagger(parriedStaggerSeconds); Recover(recoverFrames); break; }
                    }
                    if (++_frames < Mathf.Max(1, s.Active)) break;
                    if (_step + 1 < _combo.Count)
                    {
                        _step++;
                        _telegraphStarted = false;
                        Current = Move.Telegraph;
                    }
                    else Recover(s.Recovery + recoverFrames);
                    break;
                }

                case Move.Flourish:
                    FlourishFrame();
                    break;

                case Move.Step:
                    MoveX(_stepDir * stepDistance / Mathf.Max(1, stepFrames));
                    if (++_frames >= stepFrames) Rest();
                    break;

                case Move.Bind:
                    if (++_frames < bindFrames) break;
                    BindsCompleted++;
                    Heal(Mathf.RoundToInt(MaxHealth * bindHealFraction));
                    Recover(recoverFrames);
                    break;

                case Move.Recover:
                    if (--_frames <= 0) Rest();
                    break;
            }
        }

        void FlourishFrame()
        {
            var b = Collider.bounds;
            float y = floorY + 0.8f;
            int frames;
            switch (MirrorFlourish)
            {
                case FlourishKind.Longstroke:
                    frames = longstrokeFrames;
                    if (!_hitThisAttack && HitWren(new Vector2(b.center.x + Facing * (b.extents.x + longstrokeReach * 0.5f), y), new Vector2(longstrokeReach, 0.8f), damage, false) == Contact.Landed)
                        _hitThisAttack = true;
                    break;
                case FlourishKind.Blot:
                    frames = blotFrames;
                    if (!_hitThisAttack && HitWrenInCircle(new Vector2(b.center.x, y), blotRadius, damage) == Contact.Landed) _hitThisAttack = true;
                    break;
                default:
                    frames = crosshatchFrames;   // a flurry in front of her: her i-frames make it one mask
                    if (!_hitThisAttack && HitWren(new Vector2(b.center.x + Facing * (b.extents.x + crosshatchReach * 0.5f), y), new Vector2(crosshatchReach, 1.4f), damage, false) == Contact.Landed)
                        _hitThisAttack = true;
                    break;
            }
            if (++_frames >= frames) Recover(recoverFrames);
        }

        void Rest()
        {
            Current = Move.Stand;
            _pause = standSeconds;
            CurrentAttack = Attack.None;
        }

        void Recover(int frames) { Current = Move.Recover; _frames = Mathf.Max(1, frames); }

        void MoveX(float dx)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x + dx, arenaMinX, arenaMaxX);
            transform.position = p;
            Body.position = p;
        }

        /// <summary>A hit while she binds stops it; the Bind was her one.</summary>
        protected override void OnHealthChanged()
        {
            base.OnHealthChanged();
            if (IsDead || Current != Move.Bind) return;
            BindsDenied++;
            Stagger(deniedStaggerSeconds);
            Recover(recoverFrames);
        }

        protected override Color TintColor() => IsBinding ? new Color(0.70f, 0.84f, 0.92f) : new Color(0.92f, 0.92f, 0.88f);
    }
}
