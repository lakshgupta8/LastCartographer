using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Reedmother's Brood (bible 6.2, boss sheet 6.2). Tier I, optional: a giant reed-nest in the middle of the Pale
    /// Iris Fields, which the Guild's agents have set alight, and the half-drawn chicks that defend it. The nest is
    /// closed to the quill; it opens to call a clutch (three reedlings hop out of it), and while it is open it can be
    /// struck: that is the answer, and the clutch is the price of it. Its reeds thresh the floor either side (jump
    /// them). Phase 3: the fire reaches the beds and creeps toward the nest, and she chooses what to strike. Kill the
    /// nest, or let the fire take it, and the field burns: the Ferrymen's prices rise by half (Economy). Stamp the fire
    /// out (down-strikes on it) and the Brood calms and the beds stand. Either way the Tether-hook is hers.
    /// </summary>
    public sealed class ReedmotherBrood : Boss
    {
        public enum Attack { None, Brood, Thresh }
        public enum Move { Closed, Telegraph, Open, Thresh }

        [Header("Reedmother's Brood")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        /// <summary>A dormant reedling the room keeps; the clutches are copies of it.</summary>
        public GameObject chickTemplate;
        public int clutchSize = 3, maxChicks = 6;
        public int broodTelegraphFrames = 20;
        public float[] openSecondsByPhase = { 1.6f, 1.3f, 1.0f };
        public int threshTelegraphFrames = 18;
        public float threshReach = 3.5f, threshHeight = 1.1f, threshSeconds = 0.5f;   // a jump clears the reeds
        public int threshDamage = 1;
        public float restSeconds = 0.8f;
        [Header("The fire (phase 3)")]
        public float fireStepSeconds = 4f;
        public int fireSteps = 4;
        public int stampsToOut = 3;
        public int fireDamage = 1;
        public Vector2 fireSize = new Vector2(1.2f, 1.2f);

        public Move Current { get; private set; } = Move.Closed;
        public Attack CurrentAttack { get; private set; } = Attack.None;
        protected override AttackKind TelegraphKind => CurrentAttack == Attack.Thresh ? AttackKind.Strike : AttackKind.Shape;
        public bool IsOpen => Current == Move.Open;
        public int Broods { get; private set; }
        public int Threshes { get; private set; }
        public int Stamps { get; private set; }
        public int FireStep { get; private set; }
        public BossPart Fire { get; private set; }
        public bool IsBurning => Fire != null;
        /// <summary>The fire stamped out: the Brood calmed and the beds stand.</summary>
        public bool IsCalmed { get; private set; }
        /// <summary>The nest killed or burned: the field burns, and the Ferrymen's prices with it.</summary>
        public bool FieldBurned { get; private set; }
        public IReadOnlyList<Reedling> Chicks => _chicks;
        public int LiveChicks { get { int n = 0; foreach (var c in _chicks) if (c != null && !c.IsDead) n++; return n; } }

        static readonly Attack[] Phase1 = { Attack.Brood, Attack.Thresh };
        static readonly Attack[] Phase2 = { Attack.Brood, Attack.Thresh, Attack.Thresh };
        static readonly Attack[] Phase3 = { Attack.Thresh, Attack.Brood, Attack.Thresh };
        public static IReadOnlyList<Attack> PatternFor(int phase) => phase >= 3 ? Phase3 : phase >= 2 ? Phase2 : Phase1;

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            yield return new BossAttack("Brood call", AttackKind.Shape, Read(broodTelegraphFrames), 0, PhasesOf(Attack.Brood, PatternFor));
            yield return new BossAttack("Thresh", AttackKind.Strike, Read(threshTelegraphFrames), threshDamage, PhasesOf(Attack.Thresh, PatternFor));
            // The fire is the last phase's long read: a step every four seconds toward the nest, and standing in it costs a mask.
            yield return new BossAttack("The fire", AttackKind.Window, Mathf.RoundToInt(fireStepSeconds * 60f), fireDamage, 1 << 2);
        }

        readonly List<Reedling> _chicks = new List<Reedling>();
        float _pause, _openLeft, _threshLeft, _fireT, _fireStartX;
        int _patternIndex, _spawned;
        bool _telegraphStarted, _letItLand;

        /// <summary>Closed to the quill; open between broods. (The fire's end and the burning let the last blow land.)</summary>
        protected override bool AcceptsHit(in HitInfo hit) => Current == Move.Open || _letItLand;

        public override string Clip => IsDying ? (IsCalmed ? "calm" : "death") : HurtstunLeft > 0 ? "hurt" : Current switch
        {
            Move.Telegraph => CurrentAttack == Attack.Brood ? "call" : "telegraph",
            Move.Open => "open",
            Move.Thresh => "thresh",
            _ => IsBurning ? "burn" : "idle",
        };

        /// <summary>Tests and tooling: begin a specific attack now.</summary>
        public void ForceAttack(Attack a) { ClearTelegraph(); BeginAttack(a); }

        protected override void OnFightStarted()
        {
            Current = Move.Closed;
            _pause = restSeconds;
            _patternIndex = 0;
        }

        protected override void OnFightReset()
        {
            Current = Move.Closed;
            CurrentAttack = Attack.None;
            _telegraphStarted = false;
            _letItLand = false;
            ClearChicks();
            ClearFire();
        }

        protected override void OnPhaseStarted(int phase)
        {
            _patternIndex = 0;
            if (phase >= 3) LightTheFire();
        }

        protected override void OnDefeated()
        {
            ClearChicks();
            ClearFire();
            if (!IsCalmed)
            {
                FieldBurned = true;
                GameState.World.Set(Economy.IrisBurnedFlag, true);
            }
            if (!GameState.World.Equipment.OwnsInstrument(InstrumentKind.TetherHook)) GameState.World.Equipment.OwnedInstruments.Add(InstrumentKind.TetherHook);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            // The fire as light (ENV-10): the beds take the ember light off it.
            Glow(new Color(1f, 0.55f, 0.2f), 8f);
            SetGlow(IsBurning && !IsDead ? 2.5f : 0f, Fire != null ? (Vector3?)Fire.transform.position : null);
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
            Current = Move.Telegraph;
        }

        protected override void Tick(float dt)
        {
            if (Fire != null) TickFire(dt);
            switch (Current)
            {
                case Move.Closed:
                    _pause -= dt;
                    if (_pause <= 0f) BeginAttack(NextAttack());
                    break;

                case Move.Telegraph:
                    if (!Telegraph(ref _telegraphStarted, CurrentAttack == Attack.Brood ? broodTelegraphFrames : threshTelegraphFrames)) break;
                    if (CurrentAttack == Attack.Brood)
                    {
                        Broods++;
                        SpawnClutch();
                        _openLeft = openSecondsByPhase[Mathf.Clamp(Phase - 1, 0, openSecondsByPhase.Length - 1)];
                        Current = Move.Open;
                    }
                    else
                    {
                        Threshes++;
                        _threshLeft = threshSeconds;
                        Current = Move.Thresh;
                    }
                    break;

                case Move.Open:
                    _openLeft -= dt;
                    if (_openLeft <= 0f) Rest();
                    break;

                case Move.Thresh:
                {
                    _threshLeft -= dt;
                    float half = Collider.bounds.extents.x;
                    var size = new Vector2(threshReach, threshHeight);
                    foreach (int side in new[] { -1, 1 })
                    {
                        var centre = new Vector2(transform.position.x + side * (half + threshReach * 0.5f), floorY + threshHeight * 0.5f);
                        HitWren(centre, size, threshDamage, false);
                    }
                    if (_threshLeft <= 0f) Rest();
                    break;
                }
            }
        }

        void Rest()
        {
            Current = Move.Closed;
            CurrentAttack = Attack.None;
            _pause = restSeconds;
        }

        void SpawnClutch()
        {
            if (chickTemplate == null) return;
            for (int k = 0; k < clutchSize; k++)
            {
                if (LiveChicks >= maxChicks) break;
                var go = Instantiate(chickTemplate, transform.parent);
                go.name = "Reedling_Brood_" + (++_spawned);
                var top = Collider.bounds.max.y;
                go.transform.position = new Vector3(transform.position.x + (k - (clutchSize - 1) * 0.5f) * 0.9f, top + 0.4f, 0f);
                go.SetActive(true);
                var chick = go.GetComponent<Reedling>();
                if (chick != null) _chicks.Add(chick);
            }
            _chicks.RemoveAll(c => c == null);
        }

        void ClearChicks()
        {
            foreach (var c in _chicks) if (c != null) Destroy(c.gameObject);
            _chicks.Clear();
        }

        /// <summary>Phase 3: the Guild's fire reaches the beds at the arena's east edge and creeps toward the nest.</summary>
        public void LightTheFire()
        {
            if (Fire != null || IsDead) return;
            _fireStartX = arenaMaxX - fireSize.x;
            FireStep = 0; _fireT = 0f; Stamps = 0;
            Fire = BossPart.Make("Fire", transform.parent, new Vector2(_fireStartX, floorY + fireSize.y * 0.5f), fireSize, InkMaterials.Dark);
            Fire.OnHit = hit =>
            {
                if (!IsDownStrike(hit) || IsDead) return false;   // stamped out from above, not swung at
                Stamps++;
                if (Stamps >= stampsToOut) Calm();
                return true;
            };
        }

        void TickFire(float dt)
        {
            _fireT += dt;
            if (_fireT >= fireStepSeconds)
            {
                _fireT = 0f;
                FireStep++;
                float nestEdge = transform.position.x + Collider.bounds.extents.x + fireSize.x * 0.5f;
                float x = Mathf.Lerp(_fireStartX, nestEdge, Mathf.Clamp01((float)FireStep / fireSteps));
                Fire.MoveTo(new Vector2(x, floorY + fireSize.y * 0.5f));
                if (FireStep >= fireSteps) { BurnTheNest(); return; }
            }
            HitWren(Fire.Position, fireSize, fireDamage, false);
        }

        void ClearFire()
        {
            if (Fire != null) Destroy(Fire.gameObject);
            Fire = null;
        }

        /// <summary>The fire reaches the nest: it burns, and the field with it.</summary>
        void BurnTheNest()
        {
            if (IsDead) return;
            _letItLand = true;
            TakeHit(new HitInfo { Damage = Health, Direction = Vector2.down, Source = gameObject });
        }

        /// <summary>The fire stamped out: the Brood calms, the beds stand.</summary>
        void Calm()
        {
            if (IsDead) return;
            IsCalmed = true;
            ClearFire();
            _letItLand = true;
            TakeHit(new HitInfo { Damage = Health, Direction = Vector2.down, Source = gameObject });
        }

        /// <summary>A bare reedling to copy, for arenas built at run time (the kit's test rigs); the rooms keep one drawn from its sheets.</summary>
        public static GameObject MakeChickTemplate(Transform parent, Vector2 at)
        {
            var go = new GameObject("Reedling_Template") { layer = LayerMask.NameToLayer("Enemy") };
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            go.SetActive(false);
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.7f);
            go.AddComponent<Rigidbody2D>().freezeRotation = true;
            go.AddComponent<Reedling>();
            return go;
        }

        protected override Color TintColor()
        {
            var c = IsOpen ? new Color(0.92f, 0.86f, 0.62f) : new Color(0.60f, 0.52f, 0.30f);
            if (IsTelegraphing) c = Color.Lerp(c, new Color(1f, 0.85f, 0.4f), 0.6f);
            return c;
        }
    }
}
