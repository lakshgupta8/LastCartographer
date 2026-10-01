using System;
using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>What an attack is to the tuning tables (CMB-19, <c>docs/design/tuning.md</c>).</summary>
    public enum AttackKind
    {
        /// <summary>A telegraphed blow: one mask, and its read counts toward the tier's typical telegraph.</summary>
        Strike,
        /// <summary>A blow that comes down on floor it marked first: two masks and the hard shake.</summary>
        Slam,
        /// <summary>A long read the fight is built round (a ring, a sighting, a beat, the heat's glow): only the floor applies.</summary>
        Window,
        /// <summary>Changes the arena and hurts no one itself (walls, marks, a step).</summary>
        Shape,
    }

    /// <summary>One attack of a boss's kit as the fight plays it: its read in frames (the tier's floor applied), the
    /// masks it takes, and the phases it is in.</summary>
    public readonly struct BossAttack
    {
        public readonly string Name;
        public readonly AttackKind Kind;
        public readonly int Telegraph;
        public readonly int Damage;
        /// <summary>Bit n-1 set for phase n.</summary>
        public readonly int Phases;

        public BossAttack(string name, AttackKind kind, int telegraph, int damage, int phases)
        {
            Name = name; Kind = kind; Telegraph = telegraph; Damage = damage; Phases = phases;
        }

        public bool In(int phase) => phase >= 1 && (Phases & (1 << (phase - 1))) != 0;
        public const int All = 0b111;
        public override string ToString() => $"{Name} ({Kind}, {Telegraph} f, {Damage} mask{(Damage == 1 ? "" : "s")})";
    }

    /// <summary>
    /// Base for bosses (CMB-10, combat doc 8): three phases keyed to health thirds, each opened
    /// by a short line; dormant until the arena starts the fight; deactivates on death instead of
    /// being destroyed so the arena can revive it for the retry loop. Subclasses drive attacks in
    /// <see cref="Enemy.Tick"/> and use <see cref="Telegraph"/> so every attack is readable.
    /// </summary>
    public abstract class Boss : Enemy
    {
        [Header("Boss")]
        [SerializeField] string _bossName = "Boss";
        [Tooltip("Health fractions at which phases 2 and 3 begin.")]
        [SerializeField] float[] _phaseThresholds = { 2f / 3f, 1f / 3f };
        [TextArea] [SerializeField] string[] _phaseLines = { "", "", "" };
        [SerializeField] int _tier = 1;

        /// <summary>The name in the player's language (NAR-18); <see cref="EnglishName"/> is the sheet's.</summary>
        public string BossName => Bosses.NameOf(_bossName);
        public string EnglishName => _bossName;
        public int Tier => _tier;
        /// <summary>1-based. 0 before the fight starts.</summary>
        public int Phase { get; private set; }
        public int PhaseCount => _phaseThresholds.Length + 1;
        public bool IsFightActive { get; private set; }
        /// <summary>Bosses fighting now, across the scene; the mix follows it (AUD-09).</summary>
        public static int FightsActive { get; private set; }

        protected virtual void OnDestroy() { if (IsFightActive) { IsFightActive = false; FightsActive--; } }
        /// <summary>Frames left on the current telegraph; attacks start when it reaches 0.</summary>
        public int TelegraphLeft { get; private set; }
        public bool IsTelegraphing => TelegraphLeft > 0;
        /// <summary>Minimum telegraph by tier: 12 frames at Tier I down to 8 at Tier IV (combat doc 8, <see cref="Tuning.TelegraphFloor"/>).</summary>
        public int MinTelegraphFrames => Tuning.TelegraphFloor(_tier);
        /// <summary>The sheet this boss stands on (NAR-06), by its name and tier; null for a test rig.</summary>
        public BossSheet Sheet => Bosses.Named(_bossName, _tier);

        /// <summary>
        /// Every attack in the kit as the fight plays it (CMB-19): the tuning tests hold these to the tier's floor and
        /// typical read, a hit's one mask and a slam's two, and four attacks a phase. Read from the kit's own fields.
        /// </summary>
        public virtual IEnumerable<BossAttack> Kit() { yield break; }

        /// <summary>A phase mask from the phases whose pattern holds the attack.</summary>
        protected static int PhasesOf<T>(T attack, Func<int, IReadOnlyList<T>> patternFor)
        {
            int mask = 0;
            for (int p = 1; p <= 3; p++)
            {
                var pattern = patternFor(p);
                for (int i = 0; i < pattern.Count; i++) if (EqualityComparer<T>.Default.Equals(pattern[i], attack)) { mask |= 1 << (p - 1); break; }
            }
            return mask;
        }

        /// <summary>A telegraph as <see cref="Telegraph"/> plays it: never under the tier's floor.</summary>
        protected int Read(int frames) => Mathf.Max(frames, MinTelegraphFrames);
        /// <summary>Seconds as fixed steps.</summary>
        protected static int Frames(float seconds) => Mathf.RoundToInt(seconds / Time.fixedDeltaTime);

        public event Action<Boss, int, string> PhaseStarted;
        public event Action<Boss> FightStarted, FightReset, Defeated;

        Vector3 _startPosition;
        bool _startCaptured;

        protected override bool AcceptsKnockback => false;
        protected override bool DestroyOnDeath => false;

        protected override void Awake()
        {
            base.Awake();
            Body.bodyType = RigidbodyType2D.Kinematic;
            Body.gravityScale = 0f;
            CaptureStart();
            ApplyTunedHealth();
            if (Bodiless && Visual != null) Visual.enabled = false;
        }

        /// <summary>Health from the tuning table (CMB-19) when the boss stands on a sheet the table knows.</summary>
        void ApplyTunedHealth()
        {
            var sheet = Sheet;
            int h = sheet != null ? Tuning.BossHealth(sheet.Id) : 0;
            if (h > 0) SetMaxHealth(h);
        }

        void CaptureStart()
        {
            if (_startCaptured) return;
            _startPosition = transform.position;
            _startCaptured = true;
        }

        public string PhaseLine(int phase) => phase >= 1 && phase <= _phaseLines.Length ? Bosses.LineOf(_bossName, phase, _phaseLines[phase - 1]) : "";

        /// <summary>Name, tier and the three lines from the boss's sheet (NAR-06), for bosses built at runtime.</summary>
        public void ApplySheet(BossSheet sheet)
        {
            if (sheet == null) return;
            _bossName = sheet.Name;
            _tier = sheet.Tier;
            _phaseLines = sheet.Lines;
            ApplyTunedHealth();
        }

        // ---- the drawings of a fight's pieces (CHR-10, docs/design/boss-animation.md) ------------------------------------

        /// <summary>Sheets for one kind of piece the fight makes at run time (a dove, a rope, the fist), by the name the kit asks for.</summary>
        [Serializable]
        public sealed class PartSkin
        {
            public string Name;
            public float CellUnits = 2f;
            public List<SheetClip> Clips = new List<SheetClip>();
        }

        [SerializeField] List<PartSkin> _partSkins = new List<PartSkin>();

        /// <summary>The part sheets this kit can wear (Art/Characters/[name]/); the setup loads the ones that exist.</summary>
        public virtual IEnumerable<string> PartSkinNames { get { yield break; } }
        /// <summary>The body is not the drawing (the Choir is its doves): its sprite stays hidden.</summary>
        protected virtual bool Bodiless => false;
        public IReadOnlyList<PartSkin> PartSkins => _partSkins;

        /// <summary>Editor setup and tests: carry a skin for the parts named <paramref name="name"/>.</summary>
        public void AddPartSkin(string name, float cellUnits, IEnumerable<SheetClip> clips)
        {
            _partSkins.RemoveAll(s => s.Name == name);
            _partSkins.Add(new PartSkin { Name = name, CellUnits = cellUnits, Clips = new List<SheetClip>(clips) });
        }

        public PartSkin FindSkin(string name)
        {
            foreach (var s in _partSkins) if (s.Name == name && s.Clips != null && s.Clips.Count > 0) return s;
            return null;
        }

        /// <summary>Dress a part in a skin by name when the kit carries it; without it the part keeps its block.</summary>
        protected bool Skin(BossPart part, string skin, string clip = "idle", bool bottomAtFloor = false, float floorY = 0f, float wash = 0f, float lineFade = 0f)
        {
            var s = FindSkin(skin);
            if (s == null || part == null) return false;
            part.Dress(s.Clips, s.CellUnits, clip, bottomAtFloor, floorY, wash, lineFade);
            return part.IsDressed;
        }

        /// <summary>Dress a plain prop (a lamp, a stone) the same way; its state is then a clip through <see cref="BossPart.Show"/>.</summary>
        protected bool SkinProp(Transform prop, string skin, string clip = "idle")
        {
            var s = FindSkin(skin);
            if (s == null || prop == null) return false;
            return BossPart.DressProp(prop, s.Clips, s.CellUnits, clip) != null;
        }

        /// <summary>How far through a telegraph being played, 0..1, for a clip sought by it.</summary>
        protected float TelegraphProgress(int frames) => TelegraphLeft <= 0 ? 1f : 1f - Mathf.Clamp01((float)TelegraphLeft / Mathf.Max(1, Read(frames)));

        public enum Contact { None, Landed, Parried }
        readonly Collider2D[] _wrenOverlaps = new Collider2D[4];

        /// <summary>
        /// One attack box against Wren. A parryable attack meets the lens first (Parried); otherwise it lands when she can
        /// be hurt (Landed), and is None while she is invulnerable so the attack keeps looking.
        /// </summary>
        protected Contact HitWren(Vector2 centre, Vector2 size, int damage, bool parryable)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.Player, useTriggers = false };
            int n = Physics2D.OverlapBox(centre, size, 0f, filter, _wrenOverlaps);
            return Resolve(n, damage, parryable);
        }

        protected Contact HitWrenInCircle(Vector2 centre, float radius, int damage)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.Player, useTriggers = false };
            int n = Physics2D.OverlapCircle(centre, radius, filter, _wrenOverlaps);
            return Resolve(n, damage, false);
        }

        Contact Resolve(int n, int damage, bool parryable)
        {
            for (int i = 0; i < n; i++)
            {
                var vitals = _wrenOverlaps[i].GetComponentInParent<WrenVitals>();
                if (vitals == null) continue;
                var belt = vitals.GetComponent<InstrumentBelt>();
                if (parryable && belt != null && belt.TryParry(this)) return Contact.Parried;
                return vitals.Damage(damage, transform.position) ? Contact.Landed : Contact.None;
            }
            return Contact.None;
        }

        /// <summary>The forward Flourish, in the frame it strikes.</summary>
        public static bool IsLongstroke(in HitInfo hit)
            => hit.Source != null && hit.Source.TryGetComponent<Flourishes>(out var f) && f.Current == FlourishKind.Longstroke;
        /// <summary>A down-strike: the pogo.</summary>
        public static bool IsDownStrike(in HitInfo hit) => hit.Direction.y < -0.5f;
        public static bool IsUpStrike(in HitInfo hit) => hit.Direction.y > 0.5f;

        /// <summary>Called by the arena when the doors close.</summary>
        public void BeginFight()
        {
            if (IsFightActive || IsDead) return;
            if (!IsFightActive) FightsActive++;
            IsFightActive = true;
            Phase = 0;
            EnterPhase(1);
            OnFightStarted();
            FightStarted?.Invoke(this);
        }

        /// <summary>Wren died or left: back to the perch, full health, dormant until the next entry.</summary>
        public void ResetFight()
        {
            if (IsFightActive) FightsActive--;
            IsFightActive = false;
            TelegraphLeft = 0;
            Phase = 0;
            Revive();
            transform.position = _startPosition;
            Body.position = _startPosition;
            OnFightReset();
            FightReset?.Invoke(this);
        }

        protected override void FixedUpdate()
        {
            if (!IsFightActive && !IsDying) return;   // dormant on the perch
            base.FixedUpdate();
        }

        /// <summary>Subclasses call this from Tick each frame; returns true once the wind-up has finished.</summary>
        /// <summary>The kind of the attack being telegraphed, for its tell; bosses with slams, windows or shapes say which.</summary>
        protected virtual AttackKind TelegraphKind => AttackKind.Strike;

        protected bool Telegraph(ref bool started, int frames)
        {
            if (!started)
            {
                started = true;
                TelegraphLeft = Mathf.Max(frames, MinTelegraphFrames);
                Tell(TelegraphKind);
                return false;
            }
            if (TelegraphLeft > 0) { TelegraphLeft--; return TelegraphLeft == 0; }
            return true;
        }

        protected void ClearTelegraph() { TelegraphLeft = 0; }

        /// <summary>For bosses whose phases follow something other than health (Hale's stones): move on, never back.</summary>
        protected void AdvanceToPhase(int phase)
        {
            if (!IsFightActive || IsDead || phase <= Phase || phase > PhaseCount) return;
            EnterPhase(phase);
        }

        void EnterPhase(int phase)
        {
            if (phase == Phase) return;
            Phase = phase;
            OnPhaseStarted(phase);
            PhaseStarted?.Invoke(this, phase, PhaseLine(phase));
        }

        /// <summary>Checks the health thresholds after every landed hit.</summary>
        protected override void OnHealthChanged()
        {
            if (!IsFightActive || IsDead) return;
            float f = (float)Health / MaxHealth;
            int phase = 1;
            for (int i = 0; i < _phaseThresholds.Length; i++) if (f <= _phaseThresholds[i]) phase = i + 2;
            if (phase > Phase) EnterPhase(phase);
        }

        protected override void Die()
        {
            base.Die();
            if (IsFightActive) FightsActive--;
            IsFightActive = false;
            TelegraphLeft = 0;
            OnDefeated();
            Defeated?.Invoke(this);
        }

        protected virtual void OnFightStarted() { }
        protected virtual void OnFightReset() { }
        protected virtual void OnPhaseStarted(int phase) { }
        protected virtual void OnDefeated() { }
    }
}
