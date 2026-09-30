using System.Collections.Generic;
using UnityEngine;
using OWSBG.Core;

namespace OWSBG.World
{
    /// <summary>
    /// Corra's Drawing (bible 6.13, boss sheet 6.13, CMB-16). Tier IV, in a white room with a crayon floor: a child's
    /// drawing of her father, huge and wrong. It redraws a limb whenever one is struck, and while it redraws there is
    /// nothing drawn to strike: land a hit, wait for the line to come back, land another. Phase 2: it draws a second
    /// Voss, small, holding its hand; strike the small one and the drawing draws itself bigger (it heals) and draws the
    /// small one back. Phase 3: the crayon runs out and it fights in outline, faster, and the room's colour goes.
    /// </summary>
    public sealed class CorrasDrawing : Boss
    {
        public enum Attack { None, Swipe, Stomp }
        public enum Move { Stand, Telegraph, Swipe, Stomp, Recover }

        public const string OutlineGlobal = "_OWSBG_Outline";

        [Header("Corra's Drawing")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        public float walkSpeed = 1.5f;
        public float redrawSeconds = 0.6f;
        public int swipeTelegraphFrames = 12, swipeFrames = 8;
        public float swipeReach = 4f, swipeHeight = 1.6f;
        public int stompTelegraphFrames = 14, stompFrames = 6;
        public float stompWidth = 2f, stompHeight = 3f;
        public int recoverFrames = 20;
        public float standSeconds = 0.6f;
        [Range(0.2f, 1f)] public float outlinePace = 0.5f;
        public Vector2 smallSize = new Vector2(0.6f, 1.1f);
        public float smallOffset = 1.8f;
        public int smallHeal = 2;
        public float smallRedrawSeconds = 2f;
        public int damage = 1;
        /// <summary>The stomp comes down where the red crayon marked: a slam (CMB-19).</summary>
        public int slamDamage = Tuning.Slam;

        public Move Current { get; private set; } = Move.Stand;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        /// <summary>The tell for the attack being telegraphed (AUD-03): its kind in the kit.</summary>
        protected override AttackKind TelegraphKind => CurrentAttack switch { Attack.Stomp => AttackKind.Slam, _ => AttackKind.Strike };
        /// <summary>Drawn: a limb can be struck. Just struck, it is being redrawn.</summary>
        public bool IsDrawn => _redrawLeft <= 0f;
        public bool IsOutline => IsFightActive && Phase >= 3;
        public BossPart Small { get; private set; }
        public bool SmallDrawn => Small != null && Small.gameObject.activeSelf;
        public int LimbsRedrawn { get; private set; }
        public int SmallStruck { get; private set; }
        public int Swipes { get; private set; }
        public int Stomps { get; private set; }
        public int Dir => Facing;

        static readonly Attack[] Phase1 = { Attack.Swipe, Attack.Stomp };
        static readonly Attack[] Phase2 = { Attack.Swipe, Attack.Stomp, Attack.Swipe };
        static readonly Attack[] Phase3 = { Attack.Stomp, Attack.Swipe };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            yield return new BossAttack("Swipe", AttackKind.Strike, Read(TelegraphFrames(Attack.Swipe, false)), damage, PhasesOf(Attack.Swipe, PatternFor) & 0b011);
            yield return new BossAttack("Swipe", AttackKind.Strike, Read(TelegraphFrames(Attack.Swipe, true)), damage, PhasesOf(Attack.Swipe, PatternFor) & 0b100);
            yield return new BossAttack("Stomp", AttackKind.Slam, Read(TelegraphFrames(Attack.Stomp, false)), slamDamage, PhasesOf(Attack.Stomp, PatternFor) & 0b011);
            yield return new BossAttack("Stomp", AttackKind.Slam, Read(TelegraphFrames(Attack.Stomp, true)), slamDamage, PhasesOf(Attack.Stomp, PatternFor) & 0b100);
        }

        int _frames, _patternIndex;
        float _pause, _redrawLeft, _smallBackIn, _stompX;
        bool _telegraphStarted, _hitThisAttack;
        Transform _stompMark;
        static readonly int OutlineId = Shader.PropertyToID(OutlineGlobal);

        /// <summary>Drawn frames only; drained grey (the Remnant Charter), it has no colour to redraw with.</summary>
        protected override bool AcceptsHit(in HitInfo hit) => IsDrawn || IsGrey;

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
            _redrawLeft = 0f;
            Shader.SetGlobalFloat(OutlineId, 0f);
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
            if (phase == 2) DrawSmall();
            if (phase == 3) Shader.SetGlobalFloat(OutlineId, 1f);   // the crayon runs out: the room's colour goes
        }

        protected override void OnDefeated() { ClearAll(); }

        void ClearAll()
        {
            if (Small != null) Destroy(Small.gameObject);
            Small = null;
            ClearMark();
            _redrawLeft = 0f;
            Shader.SetGlobalFloat(OutlineId, 0f);
        }

        /// <summary>A landed hit: that limb is redrawn before anything else can be struck.</summary>
        protected override void OnHealthChanged()
        {
            base.OnHealthChanged();
            if (IsDead || IsGrey) return;
            _redrawLeft = redrawSeconds * (IsOutline ? outlinePace : 1f);
            LimbsRedrawn++;
        }

        // ---- the small one ----------------------------------------------------------------------------------------------

        void DrawSmall()
        {
            if (Small == null)
            {
                Small = BossPart.Make("SmallVoss", transform.parent, SmallHome(), smallSize, InkMaterials.Lit("Crayon_Small", new Color(0.46f, 0.52f, 0.72f)));
                Small.OnHit = hit => StrikeSmall();
            }
            Small.gameObject.SetActive(true);
            Small.MoveTo(SmallHome());
        }

        Vector2 SmallHome()
        {
            var b = Collider.bounds;
            return new Vector2(Mathf.Clamp(b.center.x - Facing * (b.extents.x + smallOffset), arenaMinX, arenaMaxX), floorY + smallSize.y * 0.5f);
        }

        /// <summary>Don't. The drawing draws itself bigger, and draws him back.</summary>
        bool StrikeSmall()
        {
            if (!SmallDrawn || !IsFightActive) return false;
            SmallStruck++;
            Small.gameObject.SetActive(false);
            _smallBackIn = smallRedrawSeconds;
            Heal(smallHeal);
            return true;
        }

        // ---- attacks ----------------------------------------------------------------------------------------------------

        void FaceWren()
        {
            if (Wren == null) return;
            Face(Wren.Position.x >= transform.position.x ? 1 : -1);
        }

        Attack NextAttack()
        {
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
            if (a == Attack.Stomp)
            {
                _stompX = Mathf.Clamp(Wren != null ? Wren.Position.x : transform.position.x, arenaMinX, arenaMaxX);
                ClearMark();
                _stompMark = BossPart.Prop("CrayonMark", transform.parent, new Vector2(_stompX, floorY + 0.03f), new Vector2(stompWidth, 0.06f), InkMaterials.Lit("Crayon_Mark", new Color(0.86f, 0.30f, 0.24f)), 0f);
            }
            Current = Move.Telegraph;
        }

        int TelegraphFrames(Attack a) => TelegraphFrames(a, IsOutline);

        /// <summary>Half the wind-up in outline, never under the tier's floor (Telegraph() keeps it); a slam's two masks
        /// keep at least the tier's typical read (CMB-19).</summary>
        int TelegraphFrames(Attack a, bool outline)
        {
            int f = a == Attack.Stomp ? stompTelegraphFrames : swipeTelegraphFrames;
            if (!outline) return f;
            int fast = Mathf.RoundToInt(f * outlinePace);
            return a == Attack.Stomp ? Mathf.Max(fast, Tuning.TypicalTelegraph(Tier)) : fast;
        }

        protected override void Tick(float dt)
        {
            if (_redrawLeft > 0f) _redrawLeft -= dt;
            if (Small != null && !Small.gameObject.activeSelf && Phase >= 2)
            {
                _smallBackIn -= dt;
                if (_smallBackIn <= 0f) DrawSmall();
            }
            if (SmallDrawn) Small.MoveTo(SmallHome());   // holding its hand

            switch (Current)
            {
                case Move.Stand:
                    _pause -= dt;
                    FaceWren();
                    if (Wren != null && Mathf.Abs(Wren.Position.x - transform.position.x) > swipeReach)
                        MoveX(Facing * walkSpeed * (IsOutline ? 1f / outlinePace : 1f) * dt);
                    if (_pause <= 0f) BeginAttack(NextAttack());
                    break;

                case Move.Telegraph:
                    if (!Telegraph(ref _telegraphStarted, TelegraphFrames(CurrentAttack))) break;
                    _frames = 0;
                    if (CurrentAttack == Attack.Swipe) { Swipes++; Current = Move.Swipe; }
                    else { Stomps++; Current = Move.Stomp; ClearMark(); Shake.Request(Tuning.SlamShake, Tuning.SlamShakeSeconds); }
                    break;

                case Move.Swipe:
                {
                    var b = Collider.bounds;
                    var centre = new Vector2(b.center.x + Facing * (b.extents.x + swipeReach * 0.5f), floorY + swipeHeight * 0.5f);
                    if (!_hitThisAttack && HitWren(centre, new Vector2(swipeReach, swipeHeight), damage, false) == Contact.Landed) _hitThisAttack = true;
                    if (++_frames >= swipeFrames) Recover();
                    break;
                }

                case Move.Stomp:
                    if (!_hitThisAttack && HitWren(new Vector2(_stompX, floorY + stompHeight * 0.5f), new Vector2(stompWidth, stompHeight), slamDamage, false) == Contact.Landed)
                        _hitThisAttack = true;
                    if (++_frames >= stompFrames) Recover();
                    break;

                case Move.Recover:
                    if (++_frames >= (IsOutline ? Mathf.RoundToInt(recoverFrames * outlinePace) : recoverFrames))
                    {
                        Current = Move.Stand;
                        _pause = standSeconds * (IsOutline ? outlinePace : 1f);
                        CurrentAttack = Attack.None;
                    }
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

        void ClearMark()
        {
            if (_stompMark != null) Destroy(_stompMark.gameObject);
            _stompMark = null;
        }

        protected override void Update()
        {
            base.Update();
            if (Visual != null && !IsDying) Visual.enabled = IsDrawn || !IsFightActive;
        }

        protected override Color TintColor() => IsOutline ? new Color(0.96f, 0.95f, 0.93f) : new Color(0.84f, 0.36f, 0.26f);
    }
}
