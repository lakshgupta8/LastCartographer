using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A Guild Warden (bible 3.1, combat doc 7 long-leg family): patrols an anchored town, turns at walls and
    /// edges. While Wren is a journeyman he measures her: stops, faces her, and looks away (no lance, no contact
    /// damage). Once she is unlicensed (<see cref="Licence"/>), or once she strikes him, Wren in front lowers the
    /// sighting-lance (the telegraph) and he thrusts. The thrust is a box ahead of the body for a few frames;
    /// contact with the body itself also hurts. Answer: Parry.
    /// </summary>
    public sealed class Warden : Enemy
    {
        [SerializeField] float _walkSpeed = 1.8f;
        [SerializeField] float _lanceRange = 3.2f;
        [SerializeField] float _lanceReach = 2.4f;
        [SerializeField] float _lanceHeight = 0.7f;
        [SerializeField] int _telegraphFrames = 14;
        [SerializeField] int _thrustFrames = 6;
        [SerializeField] int _recoverFrames = 30;
        [SerializeField] float _thrustCooldown = 1.4f;
        [SerializeField] int _lanceDamage = 1;
        [SerializeField] int _measureFrames = 40;
        [SerializeField] float _measureCooldown = 4f;
        [SerializeField] LayerMask _groundMask;
        /// <summary>A flag that stands this Warden down (the Threshold's line on Halvard's word, `act2.halvard_third`): lance down, no hunt, no count; he stands where he is.</summary>
        [SerializeField] string _standDownFlag = "";

        public enum Move { Patrol, Measure, Telegraph, Thrust, Recover }
        public Move State { get; private set; }
        public int Thrusts { get; private set; }
        public int Measures { get; private set; }
        public bool IsTelegraphing => State == Move.Telegraph;
        public bool IsMeasuring => State == Move.Measure;
        public override string Clip => IsDying || HurtstunLeft > 0 ? base.Clip
            : State == Move.Measure ? "measure" : State == Move.Telegraph ? "telegraph" : State == Move.Thrust ? "thrust" : State == Move.Recover ? "recover" : base.Clip;
        public int Dir => Facing;
        /// <summary>Struck by her: this Warden hunts her whatever her papers say.</summary>
        public bool Provoked { get; private set; }
        /// <summary>Whether he lowers the lance at her: the Guild's stance, or his own grievance.</summary>
        public bool Hostile => Provoked || (!IsStoodDown && Licence.WardensHostile(GameState.World));
        public string StandDownFlag { get => _standDownFlag; set => _standDownFlag = value ?? ""; }
        /// <summary>Stood down by his own order's word: not the Guild's stance (<see cref="Licence.StoodDownFlag"/>), this line's. Struck, he still answers.</summary>
        public bool IsStoodDown => !string.IsNullOrEmpty(_standDownFlag) && GameState.World.Is(_standDownFlag);

        static string MeasureCaption => Loc.T("caption.warden_measures", "The Warden measures the cowl and looks away.");
        static bool _captioned;

        int _frames;
        float _cooldown;
        bool _hitThisThrust;
        Vector3 _baseScale;
        readonly Collider2D[] _hits = new Collider2D[4];

        protected override void Awake()
        {
            base.Awake();
            if (_groundMask.value == 0) _groundMask = LayerMask.GetMask("Ground");
            _answer = EnemyAnswer.Parry;
            _baseScale = Visual != null ? Visual.transform.localScale : Vector3.one;
        }

        /// <summary>A journeyman is measured, not struck.</summary>
        protected override bool ContactHurts => Hostile;

        protected override bool AcceptsHit(in HitInfo hit)
        {
            Provoked = true;
            return true;
        }

        protected override void OnRevived() { Provoked = false; State = Move.Patrol; _cooldown = 0f; }

        protected override void Tick(float dt)
        {
            _cooldown -= dt;
            switch (State)
            {
                case Move.Patrol:
                    if (IsStoodDown && !Provoked)
                    {
                        // Lances down: he stands, turns to watch her go by, and counts nobody.
                        Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                        if (Wren != null && Mathf.Abs(Wren.Position.x - transform.position.x) < _lanceRange * 2f) Face(Wren.Position.x >= transform.position.x ? 1 : -1);
                        break;
                    }
                    if (!GroundAhead(0.1f, 0.6f, _groundMask) || WallAhead(0.1f, _groundMask)) Face(-Facing);
                    if (Wren != null && _cooldown <= 0f)
                    {
                        var to = Wren.Position - (Vector2)transform.position;
                        if (Mathf.Abs(to.x) < _lanceRange && Mathf.Abs(to.y) < 1.6f)
                        {
                            Face(to.x >= 0f ? 1 : -1);
                            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                            _frames = 0;
                            if (Hostile) { State = Move.Telegraph; Tell(AttackKind.Strike); }
                            else
                            {
                                State = Move.Measure;
                                Measures++;
                                if (!_captioned) { _captioned = true; Captions.Show(MeasureCaption, 3f); }
                            }
                            return;
                        }
                    }
                    Body.linearVelocity = new Vector2(Facing * _walkSpeed, Body.linearVelocity.y);
                    break;

                case Move.Measure:
                    Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                    if (Hostile) { State = Move.Telegraph; _frames = 0; Tell(AttackKind.Strike); return; }   // struck mid-measure, or the count came in
                    if (++_frames >= _measureFrames) { State = Move.Patrol; _cooldown = _measureCooldown; }
                    break;

                case Move.Telegraph:
                    Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                    if (++_frames >= _telegraphFrames) { State = Move.Thrust; _frames = 0; _hitThisThrust = false; Thrusts++; }
                    break;

                case Move.Thrust:
                    Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                    LanceCheck();
                    if (++_frames >= _thrustFrames) { State = Move.Recover; _frames = 0; }
                    break;

                case Move.Recover:
                    Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                    if (++_frames >= _recoverFrames) { State = Move.Patrol; _cooldown = _thrustCooldown; }
                    break;
            }
        }

        void LanceCheck()
        {
            if (_hitThisThrust || Wren == null) return;
            var b = Collider.bounds;
            var centre = new Vector2(b.center.x + Facing * (b.extents.x + _lanceReach * 0.5f), b.center.y + 0.2f);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.Player, useTriggers = false };
            int n = Physics2D.OverlapBox(centre, new Vector2(_lanceReach, _lanceHeight), 0f, filter, _hits);
            for (int i = 0; i < n; i++)
            {
                var vitals = _hits[i].GetComponentInParent<WrenVitals>();
                if (vitals == null) continue;
                if (vitals.Damage(_lanceDamage, transform.position)) _hitThisThrust = true;
                return;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (Visual == null || IsDying || HasSheets) return;
            // The lance: lean into the telegraph, stretch on the thrust, a small tilt for the measuring (on the sprite; Face owns its sign).
            float lean = State == Move.Telegraph ? 0.85f : State == Move.Thrust ? 1.5f : State == Move.Measure ? 0.94f : 1f;
            var s = Visual.transform.localScale;
            Visual.transform.localScale = new Vector3(Facing * Mathf.Abs(_baseScale.x) * lean, s.y, s.z);
        }

        protected override Color TintColor() => new Color(0.16f, 0.22f, 0.42f);
    }
}
