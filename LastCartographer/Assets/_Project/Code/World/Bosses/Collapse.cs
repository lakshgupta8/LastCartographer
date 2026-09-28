using System.Collections.Generic;
using UnityEngine;
using OWSBG.Core;

namespace OWSBG.World
{
    /// <summary>
    /// The Collapse (bible 6.4, boss sheet 6.4, CMB-13). Tier II, at the bottom of Hollowvein: the mine collapse itself,
    /// a smudge-beast as wide as the floor. The walk's beat keeps going through the fight; on each beat a chorus lamp
    /// lights one section of the floor, and only the lit section of it is drawn enough to strike, and only for the lit
    /// part of the beat (the Smudge rule). Phase 1: rubble falls where the dust rises; a block that lands under a lamp
    /// keeps that lamp dark until it is pogoed. Phase 2: ink surges along the floor; jump it or Longstroke it.
    /// Phase 3: it reaches for a lamp and puts it out unless it is struck while reaching; an out lamp's beat is lost.
    /// It never takes the last lamp. It does not hurt to touch: it is the mine, and the mine is everywhere.
    /// </summary>
    public sealed class Collapse : Boss
    {
        public enum Attack { None, Rubble, Surge, Reach }
        public enum Move { Wait, Telegraph, Fall, Surge }

        [Header("Collapse")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        public int lampCount = 4;
        public float lampHeight = 5f;
        public float beatSeconds = 0.8f;
        [Range(0.2f, 1f)] public float litFraction = 0.6f;
        public float bodyHeight = 3f;
        public float waitSeconds = 0.9f;
        public int rubbleTelegraphFrames = 16, fallFrames = 10;
        public float rubbleWidth = 1.4f, rubbleHeight = 0.8f, fallHeight = 3.4f;
        public int maxRubble = 3;
        public int surgeTelegraphFrames = 14;
        public float surgeSpeed = 9f, surgeWidth = 1.6f, surgeHeight = 0.7f;
        public int reachFrames = 70;
        public int damage = 1;
        /// <summary>The rubble comes down on dust it raised first: a slam (CMB-19).</summary>
        public int slamDamage = Tuning.Slam;

        public Move Current { get; private set; } = Move.Wait;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        /// <summary>Beats since the fight began.</summary>
        public int Beat { get; private set; }
        /// <summary>The lamp lit on this beat, or -1 when the beat is lost (the lamp is out or under rubble).</summary>
        public int LitLamp { get; private set; } = -1;
        public int LostBeats { get; private set; }
        /// <summary>Only the lit section, only for the lit part of the beat.</summary>
        public bool IsDrawn => IsFightActive && LitLamp >= 0 && _beatT < beatSeconds * litFraction;
        public float SectionWidth => (arenaMaxX - arenaMinX) / Mathf.Max(1, lampCount);
        public IReadOnlyList<BossPart> Rubble => _rubble;
        public BossPart Surge { get; private set; }
        public int ReachingFor => CurrentAttack == Attack.Reach && Current == Move.Telegraph ? _reachLamp : -1;
        public int Rubbles { get; private set; }
        public int Surges { get; private set; }
        public int Reaches { get; private set; }
        public int RubbleBroken { get; private set; }
        public int SurgesCut { get; private set; }
        public int ReachesStopped { get; private set; }
        public int LampsOut { get { int n = 0; foreach (var o in _out) if (o) n++; return n; } }

        static readonly Attack[] Phase1 = { Attack.Rubble };
        static readonly Attack[] Phase2 = { Attack.Surge, Attack.Rubble };
        static readonly Attack[] Phase3 = { Attack.Reach, Attack.Rubble, Attack.Surge };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            yield return new BossAttack("Rubble", AttackKind.Slam, Read(rubbleTelegraphFrames), slamDamage, PhasesOf(Attack.Rubble, PatternFor));
            yield return new BossAttack("Surge", AttackKind.Strike, Read(surgeTelegraphFrames), damage, PhasesOf(Attack.Surge, PatternFor));
            yield return new BossAttack("Reach", AttackKind.Window, Read(reachFrames), 0, PhasesOf(Attack.Reach, PatternFor));
        }

        readonly List<BossPart> _rubble = new List<BossPart>();
        readonly List<Transform> _lamps = new List<Transform>();
        bool[] _out = new bool[0];
        float _beatT, _wait, _targetX;
        int _frames, _patternIndex, _reachLamp = -1, _surgeDir;
        bool _telegraphStarted, _hitThisAttack;
        Transform _dust;
        Attack _forced = Attack.None;

        protected override bool ContactHurts => false;
        protected override bool AcceptsHit(in HitInfo hit) => IsDrawn;

        protected override void Start()
        {
            base.Start();
            EnsureLamps();
        }

        void EnsureLamps()
        {
            if (_out.Length != lampCount) _out = new bool[lampCount];
            if (_lamps.Count == lampCount) return;
            for (int i = 0; i < lampCount; i++)
                _lamps.Add(BossPart.Prop("Lamp_" + i, transform.parent, new Vector2(SectionCentre(i), floorY + lampHeight), new Vector2(0.5f, 0.7f), LampMaterial(false), 0.5f));
        }

        public float SectionCentre(int i) => arenaMinX + (i + 0.5f) * SectionWidth;
        public int SectionOf(float x) => Mathf.Clamp(Mathf.FloorToInt((x - arenaMinX) / SectionWidth), 0, lampCount - 1);
        public bool IsLampOut(int i) => i >= 0 && i < _out.Length && _out[i];
        public bool IsLampBlocked(int i)
        {
            foreach (var r in _rubble) if (r != null && SectionOf(r.Position.x) == i) return true;
            return false;
        }

        /// <summary>Tests and tooling: begin a specific attack now.</summary>
        public void ForceAttack(Attack a)
        {
            ClearTelegraph();
            ClearSurge();
            Begin(a);
        }

        protected override void OnFightStarted()
        {
            EnsureLamps();
            Beat = 0;
            _beatT = 0f;
            LightBeat();
            Current = Move.Wait;
            _wait = waitSeconds;
            _patternIndex = 0;
        }

        protected override void OnFightReset()
        {
            Current = Move.Wait;
            CurrentAttack = Attack.None;
            _telegraphStarted = false;
            for (int i = 0; i < _out.Length; i++) _out[i] = false;
            LitLamp = -1;
            ClearParts();
            RefreshLamps();
        }

        protected override void OnPhaseStarted(int phase) { _patternIndex = 0; }
        protected override void OnDefeated() { ClearParts(); LitLamp = -1; RefreshLamps(); }

        protected override void FixedUpdate()
        {
            if (IsFightActive) AdvanceBeat(Time.fixedDeltaTime);   // the chorus does not stop for hurtstun
            base.FixedUpdate();
        }

        void AdvanceBeat(float dt)
        {
            _beatT += dt;
            if (_beatT < beatSeconds) return;
            _beatT -= beatSeconds;
            Beat++;
            LightBeat();
        }

        void LightBeat()
        {
            int i = Beat % Mathf.Max(1, lampCount);
            if (IsLampOut(i) || IsLampBlocked(i)) { LitLamp = -1; LostBeats++; }
            else
            {
                LitLamp = i;
                var p = new Vector2(SectionCentre(i), floorY + bodyHeight * 0.5f);
                transform.position = p;
                Body.position = p;
            }
            RefreshLamps();
        }

        void RefreshLamps()
        {
            for (int i = 0; i < _lamps.Count; i++)
                if (_lamps[i] != null) _lamps[i].GetComponent<MeshRenderer>().sharedMaterial = LampMaterial(i == LitLamp && !IsLampOut(i));
        }

        static Material LampMaterial(bool lit) => lit ? InkMaterials.Lit("Collapse_Lamp_Lit", new Color(0.98f, 0.80f, 0.42f)) : InkMaterials.Lit("Collapse_Lamp_Dark", new Color(0.22f, 0.20f, 0.18f));

        Attack NextAttack()
        {
            if (_forced != Attack.None) { var f = _forced; _forced = Attack.None; return f; }
            var p = PatternFor(Phase);
            var a = p[_patternIndex % p.Count];
            _patternIndex++;
            if (a == Attack.Reach && lampCount - LampsOut <= 1) a = Attack.Rubble;   // never the last lamp
            return a;
        }

        void Begin(Attack a)
        {
            CurrentAttack = a;
            _telegraphStarted = false;
            _hitThisAttack = false;
            _frames = 0;
            Current = Move.Telegraph;
            switch (a)
            {
                case Attack.Rubble:
                    _targetX = Mathf.Clamp(Wren != null ? Wren.Position.x : SectionCentre(0), arenaMinX + rubbleWidth * 0.5f, arenaMaxX - rubbleWidth * 0.5f);
                    _dust = BossPart.Prop("Dust", transform.parent, new Vector2(_targetX, floorY + 0.15f), new Vector2(rubbleWidth, 0.3f), InkMaterials.Lit("Collapse_Dust", new Color(0.55f, 0.52f, 0.48f)));
                    break;
                case Attack.Reach:
                    Reaches++;
                    _reachLamp = NextLitLampAfter(Beat);
                    break;
            }
        }

        int NextLitLampAfter(int beat)
        {
            for (int k = 1; k <= lampCount; k++)
            {
                int i = (beat + k) % lampCount;
                if (!IsLampOut(i)) return i;
            }
            return 0;
        }

        int TelegraphFrames(Attack a) => a switch
        {
            Attack.Rubble => rubbleTelegraphFrames,
            Attack.Surge => surgeTelegraphFrames,
            Attack.Reach => reachFrames,
            _ => rubbleTelegraphFrames,
        };

        protected override void Tick(float dt)
        {
            switch (Current)
            {
                case Move.Wait:
                    _wait -= dt;
                    if (_wait <= 0f) Begin(NextAttack());
                    break;

                case Move.Telegraph:
                    if (!Telegraph(ref _telegraphStarted, TelegraphFrames(CurrentAttack))) break;
                    _frames = 0;
                    switch (CurrentAttack)
                    {
                        case Attack.Rubble: Rubbles++; Current = Move.Fall; break;
                        case Attack.Surge: SpawnSurge(); Current = Move.Surge; break;
                        case Attack.Reach: PutOut(_reachLamp); Rest(); break;
                        default: Rest(); break;
                    }
                    break;

                case Move.Fall:
                    // The block comes down the column over the dust; standing in it costs a mask.
                    float k = Mathf.Clamp01((float)_frames / Mathf.Max(1, fallFrames));
                    float top = floorY + fallHeight * (1f - k) + rubbleHeight;
                    if (!_hitThisAttack && HitWren(new Vector2(_targetX, (floorY + top) * 0.5f), new Vector2(rubbleWidth, top - floorY), slamDamage, false) == Contact.Landed)
                        _hitThisAttack = true;
                    if (++_frames >= fallFrames) { Land(); Rest(); }
                    break;

                case Move.Surge:
                    if (Surge == null) { Rest(); break; }
                    var sp = Surge.Position + new Vector2(_surgeDir * surgeSpeed * dt, 0f);
                    Surge.MoveTo(sp);
                    if (!_hitThisAttack && HitWren(sp, new Vector2(surgeWidth, surgeHeight), damage, false) == Contact.Landed) _hitThisAttack = true;
                    if (sp.x < arenaMinX - surgeWidth || sp.x > arenaMaxX + surgeWidth) { ClearSurge(); Rest(); }
                    break;
            }
        }

        void Rest()
        {
            Current = Move.Wait;
            _wait = waitSeconds;
            CurrentAttack = Attack.None;
            if (_dust != null) { Destroy(_dust.gameObject); _dust = null; }
        }

        void Land()
        {
            Shake.Request(Tuning.SlamShake, Tuning.SlamShakeSeconds);
            if (_rubble.Count >= maxRubble)
            {
                if (_rubble[0] != null) Destroy(_rubble[0].gameObject);
                _rubble.RemoveAt(0);
            }
            var block = BossPart.Make("Rubble", transform.parent, new Vector2(_targetX, floorY + rubbleHeight * 0.5f), new Vector2(rubbleWidth, rubbleHeight),
                InkMaterials.Lit("Collapse_Rubble", new Color(0.36f, 0.33f, 0.30f)));
            block.OnHit = hit =>
            {
                if (!IsDownStrike(hit)) return false;   // pogo the rubble
                _rubble.Remove(block);
                Destroy(block.gameObject);
                RubbleBroken++;
                return true;
            };
            _rubble.Add(block);
        }

        void SpawnSurge()
        {
            Surges++;
            float wx = Wren != null ? Wren.Position.x : (arenaMinX + arenaMaxX) * 0.5f;
            bool fromWest = wx - arenaMinX > arenaMaxX - wx;   // from the far end, toward her
            _surgeDir = fromWest ? 1 : -1;
            var start = new Vector2(fromWest ? arenaMinX : arenaMaxX, floorY + surgeHeight * 0.5f);
            var s = BossPart.Make("Surge", transform.parent, start, new Vector2(surgeWidth, surgeHeight), InkMaterials.Dark);
            s.OnHit = hit =>
            {
                if (!IsLongstroke(hit)) return false;   // Longstroke the surge
                SurgesCut++;
                ClearSurge();
                return true;
            };
            Surge = s;
        }

        void ClearSurge()
        {
            if (Surge != null) Destroy(Surge.gameObject);
            Surge = null;
        }

        void PutOut(int lamp)
        {
            if (lamp < 0 || lamp >= _out.Length || lampCount - LampsOut <= 1) return;
            _out[lamp] = true;
            if (LitLamp == lamp) LitLamp = -1;
            RefreshLamps();
        }

        /// <summary>Struck while reaching: the lamp stays lit.</summary>
        protected override void OnHealthChanged()
        {
            base.OnHealthChanged();
            if (IsDead || CurrentAttack != Attack.Reach || Current != Move.Telegraph) return;
            ReachesStopped++;
            ClearTelegraph();
            Rest();
        }

        void ClearParts()
        {
            foreach (var r in _rubble) if (r != null) Destroy(r.gameObject);
            _rubble.Clear();
            ClearSurge();
            if (_dust != null) { Destroy(_dust.gameObject); _dust = null; }
        }

        protected override void Update()
        {
            base.Update();
            if (Visual != null && !IsDying) Visual.enabled = !IsFightActive || IsDrawn;
        }

        protected override Color TintColor() => new Color(0.10f, 0.09f, 0.11f);
    }
}
