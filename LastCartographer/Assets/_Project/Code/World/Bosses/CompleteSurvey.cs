using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The Complete Survey (bible 6.15, boss sheet 6.15, CMB-16). Tier IV, true ending only: the Great Atlas itself, on
    /// the Observatory floor as the page, trying to draw Wren in. The chorus sings the roll-call and the beat is the only
    /// safe rhythm: each beat names a section of the floor (shown a beat ahead), and when the beat lands, wherever else
    /// she stands, the region's ink fixes her there (held, and a mask). That is the bounds-walk, at speed: phase 1
    /// Saltmarrow's tide walks one section a beat, phase 2 Emberdown's ash two, phase 3 Halden's late afternoon three,
    /// each faster. Ink pools where the named ground was; strike a pool to wound the Atlas. Between verses the chorus
    /// breathes: two beats with nothing named, time to Bind.
    /// </summary>
    public sealed class CompleteSurvey : Boss
    {
        [Header("The Complete Survey")]
        public float floorY = 0f;
        public float arenaMinX = 0.5f, arenaMaxX = 17.5f;
        public int sections = 6;
        /// <summary>Each phase fights to its region's beat (AUD-01): Saltmarrow's tide, Emberdown's ash, Halden's late afternoon.</summary>
        public float[] beatSeconds = { AudioDirection.BeatOf(Region.Saltmarrow), AudioDirection.BeatOf(Region.Emberdown), AudioDirection.BeatOf(Region.Halden) };
        public int verseBeats = 8, breakBeats = 2;
        public float fixSeconds = 0.6f;
        public int fixDamage = 1;
        public int poolEveryBeats = 3, maxPools = 3;
        public Vector2 poolSize = new Vector2(1.4f, 0.3f);

        /// <summary>The region's ink in each phase.</summary>
        public static readonly string[] Inks = { "Saltmarrow's tide", "Emberdown's ash", "Halden's late afternoon" };

        public int Beat { get; private set; }
        /// <summary>The ground named on this beat: be on it when the beat lands. -1 between verses.</summary>
        public int Named { get; private set; } = -1;
        /// <summary>The ground the next beat names, shown now.</summary>
        public int NextNamed { get; private set; } = -1;
        public bool InVerseBreak => IsBreak(Beat);
        public string Ink => Inks[Mathf.Clamp(Phase - 1, 0, Inks.Length - 1)];
        public float BeatLength => beatSeconds[Mathf.Clamp(Phase - 1, 0, beatSeconds.Length - 1)];
        /// <summary>How far the named ground walks each beat: one, two, three sections.</summary>
        public int Stride => Mathf.Clamp(Phase, 1, 3);
        public bool IsFixing => _fixLeft > 0f;
        public IReadOnlyList<BossPart> Pools => _pools;
        public int Fixes { get; private set; }
        public int SafeBeats { get; private set; }
        public int PoolsStruck { get; private set; }
        public float SectionWidth => (arenaMaxX - arenaMinX) / Mathf.Max(1, sections);

        /// <summary>The kit as the tuning tables read it (CMB-19, docs/design/tuning.md).</summary>
        public override IEnumerable<BossAttack> Kit()
        {
            for (int p = 1; p <= beatSeconds.Length && p <= 3; p++)
                yield return new BossAttack("Named ground", AttackKind.Window, Read(Frames(beatSeconds[p - 1])), fixDamage, 1 << (p - 1));
        }
        public int SectionOf(float x) => Mathf.Clamp(Mathf.FloorToInt((x - arenaMinX) / SectionWidth), 0, sections - 1);
        public float SectionCentre(int i) => arenaMinX + (i + 0.5f) * SectionWidth;

        readonly List<BossPart> _pools = new List<BossPart>();
        readonly List<Transform> _tiles = new List<Transform>();
        float _beatT, _fixLeft;
        bool _routing, _frozeHer;
        WrenVitals _vitals;

        bool IsBreak(int beat) => (beat % (verseBeats + breakBeats)) >= verseBeats;

        protected override bool ContactHurts => false;
        protected override bool AcceptsHit(in HitInfo hit) => _routing;

        protected override void Start()
        {
            base.Start();
            EnsureTiles();
        }

        void EnsureTiles()
        {
            if (_tiles.Count == sections) return;
            for (int i = 0; i < sections; i++)
                _tiles.Add(BossPart.Prop("Page_" + i, transform.parent, new Vector2(SectionCentre(i), floorY + 0.04f), new Vector2(SectionWidth * 0.96f, 0.1f), TileMaterial(false, false), 0f));
        }

        static Material TileMaterial(bool named, bool next) => named
            ? InkMaterials.Lit("Page_Named", new Color(0.96f, 0.84f, 0.46f))
            : next ? InkMaterials.Lit("Page_Next", new Color(0.86f, 0.80f, 0.64f)) : InkMaterials.Lit("Page", new Color(0.93f, 0.92f, 0.88f));

        protected override void OnFightStarted()
        {
            EnsureTiles();
            Beat = 0;
            _beatT = 0f;
            Named = -1;
            NextNamed = 0;
            RefreshTiles();
        }

        protected override void OnFightReset() { ClearAll(); }
        protected override void OnDefeated() { ClearAll(); }

        void ClearAll()
        {
            Release();
            foreach (var p in _pools) if (p != null) Destroy(p.gameObject);
            _pools.Clear();
            Named = NextNamed = -1;
            RefreshTiles();
        }

        protected override void FixedUpdate()
        {
            if (IsFightActive)
            {
                float dt = Time.fixedDeltaTime;
                if (_fixLeft > 0f) { _fixLeft -= dt; if (_fixLeft <= 0f) Release(); }
                _beatT += dt;
                if (_beatT >= BeatLength) { _beatT -= BeatLength; Land(); }
            }
            base.FixedUpdate();
        }

        /// <summary>The beat lands: off the named ground, the ink fixes her.</summary>
        void Land()
        {
            Beat++;
            int previous = Named;
            if (IsBreak(Beat)) Named = -1;   // the chorus breathes
            else
            {
                Named = NextNamed >= 0 ? NextNamed : 0;
                if (Wren != null && SectionOf(Wren.Position.x) != Named) Fix();
                else SafeBeats++;
            }
            if (previous >= 0 && Beat % poolEveryBeats == 0) Pool(previous);
            if (IsBreak(Beat + 1)) NextNamed = -1;
            else
            {
                int from = Named >= 0 ? Named : (NextNamed >= 0 ? NextNamed : 0);
                NextNamed = Named >= 0 ? (from + Stride) % sections : from;
            }
            RefreshTiles();
        }

        void Fix()
        {
            Fixes++;
            if (Wren == null) return;
            if (_vitals == null) _vitals = Wren.GetComponent<WrenVitals>();
            _vitals?.Damage(fixDamage, new Vector2(Wren.Position.x, floorY - 1f));
            _fixLeft = fixSeconds;
            _frozeHer = true;
            Wren.Frozen = true;   // fixed where she stands
        }

        void Release()
        {
            _fixLeft = 0f;
            if (!_frozeHer) return;
            _frozeHer = false;
            if (Wren != null) Wren.Frozen = false;
        }

        /// <summary>Ink pools where the named ground was; a pool struck wounds the Atlas.</summary>
        void Pool(int section)
        {
            if (_pools.Count >= maxPools)
            {
                if (_pools[0] != null) Destroy(_pools[0].gameObject);
                _pools.RemoveAt(0);
            }
            var pool = BossPart.Make("InkPool", transform.parent, new Vector2(SectionCentre(section), floorY + poolSize.y * 0.5f), poolSize, InkMaterials.Dark);
            pool.OnHit = hit => StrikePool(pool);
            _pools.Add(pool);
        }

        bool StrikePool(BossPart pool)
        {
            if (!IsFightActive || !_pools.Contains(pool)) return false;
            _pools.Remove(pool);
            Destroy(pool.gameObject);
            PoolsStruck++;
            _routing = true;
            try { TakeHit(new HitInfo { Damage = 1, Direction = Vector2.down }); }
            finally { _routing = false; }
            return true;
        }

        /// <summary>Tests and tooling: lay a pool on this section now.</summary>
        public BossPart ForcePool(int section) { Pool(section); return _pools[_pools.Count - 1]; }

        void RefreshTiles()
        {
            for (int i = 0; i < _tiles.Count; i++)
                if (_tiles[i] != null) _tiles[i].GetComponent<MeshRenderer>().sharedMaterial = TileMaterial(i == Named, i == NextNamed);
        }

        protected override void Tick(float dt) { }

        void OnDisable() { Release(); }

        protected override Color TintColor() => new Color(0.90f, 0.86f, 0.74f);
    }
}
