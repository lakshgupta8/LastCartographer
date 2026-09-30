using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>How a boss can be struck, which scales its health from the tier's base (CMB-19).</summary>
    public enum BossAccess
    {
        /// <summary>The body can be struck whenever Wren reaches it.</summary>
        Open,
        /// <summary>Strikes land only in windows the fight opens (grounded after a dive, drawn frames, the seam, the
        /// drawing hand), or the fight takes her time elsewhere (Hale's stones).</summary>
        Windowed,
        /// <summary>The fight hands out a strike at its own pace (the Survey's pools, every third beat).</summary>
        Paced,
        /// <summary>Health is a count of things, not strikes (the Bells' four ropes).</summary>
        Counted,
    }

    /// <summary>
    /// The tuning pass (CMB-19, <c>docs/design/tuning.md</c>): the numbers that cut across the fights, in one place.
    /// Damage to Wren (a hit takes one mask, a slam two), telegraph frames per tier (a floor no attack goes under and
    /// the typical read a tier's bosses sit around), boss health from tier and access, enemy families, and the
    /// Inkwell's costs and gains. Frames are fixed steps at 60 Hz. Kit-specific numbers (a beam's speed, a ring's
    /// length) stay with their kits; the tuning tests hold the kits to the rules here.
    /// </summary>
    public static class Tuning
    {
        // ---- damage to Wren (combat doc 1) -----------------------------------------------------------------------------

        /// <summary>Masks most hits take.</summary>
        public const int Hit = 1;
        /// <summary>Masks a boss slam takes: an attack that comes down on floor it has marked first.</summary>
        public const int Slam = 2;
        /// <summary>The camera's hard shake, only on a boss slam (combat doc 1).</summary>
        public const float SlamShake = 0.35f, SlamShakeSeconds = 0.35f;

        // ---- telegraphs per tier (combat doc 8) ------------------------------------------------------------------------

        public const int MinTier = 1, MaxTier = 4;

        /// <summary>No attack is read in fewer frames: 12 at Tier I, 11 at II, 10 at III, 8 at IV.</summary>
        public static int TelegraphFloor(int tier)
        {
            int t = System.Math.Clamp(tier, MinTier, MaxTier);
            return System.Math.Max(8, 12 - (t - 1) * 4 / 3);
        }

        /// <summary>
        /// The read a tier's bosses sit around: the median telegraph of a boss's strikes stays within
        /// <see cref="TelegraphSpread"/> of it. 18 frames at Tier I (300 ms), 16, 14, and 12 at Tier IV (200 ms).
        /// </summary>
        public static int TypicalTelegraph(int tier) => 18 - (System.Math.Clamp(tier, MinTier, MaxTier) - 1) * 2;
        public const int TelegraphSpread = 2;

        public static bool TypicalFits(int tier, float median) => System.Math.Abs(median - TypicalTelegraph(tier)) <= TelegraphSpread;

        // ---- boss health -----------------------------------------------------------------------------------------------

        /// <summary>Strikes of the Surveyor's quill (one damage each) to fell an Open boss of the tier: 30, 34, 38, 42.</summary>
        public static int BossBase(int tier) => 30 + (System.Math.Clamp(tier, MinTier, MaxTier) - 1) * 4;

        public static float AccessScale(BossAccess a) => a switch
        {
            BossAccess.Windowed => 0.8f,
            BossAccess.Paced => 0.6f,
            _ => 1f,
        };

        public readonly struct BossTune
        {
            public readonly string Id;
            public readonly int Tier;
            public readonly BossAccess Access;
            /// <summary>Only for <see cref="BossAccess.Counted"/>: the count.</summary>
            public readonly int Count;
            public BossTune(string id, int tier, BossAccess access, int count = 0) { Id = id; Tier = tier; Access = access; Count = count; }
            public int Health => Access == BossAccess.Counted ? Count : (int)System.Math.Round(BossBase(Tier) * AccessScale(Access), System.MidpointRounding.AwayFromZero);
        }

        static readonly Dictionary<string, BossTune> _bosses = new Dictionary<string, BossTune>();
        static void B(string id, int tier, BossAccess access, int count = 0) => _bosses[id] = new BossTune(id, tier, access, count);

        public static IEnumerable<BossTune> Bosses => _bosses.Values;
        public static bool TryBoss(string id, out BossTune t) => _bosses.TryGetValue(id ?? "", out t);

        /// <summary>A boss's health by its sheet id; 0 when the table doesn't know it.</summary>
        public static int BossHealth(string id) => TryBoss(id, out var t) ? t.Health : 0;

        // ---- enemies (combat doc 7) ------------------------------------------------------------------------------------

        public readonly struct EnemyTune
        {
            public readonly string Family;
            /// <summary>Strikes to fell with the Surveyor's quill.</summary>
            public readonly int Health;
            /// <summary>Masks its body takes on contact.</summary>
            public readonly int Contact;
            public EnemyTune(string family, int health, int contact) { Family = family; Health = health; Contact = contact; }
        }

        static readonly Dictionary<string, EnemyTune> _enemies = new Dictionary<string, EnemyTune>();
        static void E(string family, int health, int contact = Hit) => _enemies[family] = new EnemyTune(family, health, contact);

        public static IEnumerable<EnemyTune> Enemies => _enemies.Values;
        /// <summary>An enemy family's numbers ("MarshCrab"); false for a family the table leaves to its scene.</summary>
        public static bool TryEnemy(string family, out EnemyTune t) => _enemies.TryGetValue(family ?? "", out t);

        // ---- the Inkwell (combat doc 4) ---------------------------------------------------------------------------------

        public const int InkMax = 9;
        /// <summary>Pips a landed quill strike fills (before the Charter's multiplier).</summary>
        public const int InkPerStrike = 1;
        /// <summary>Seconds per pip while Wren stands at a vantage.</summary>
        public const float VantageSecondsPerPip = 10f;
        public const int BindCost = 3, CrosshatchCost = 3, LongstrokeCost = 3, BlotCost = 4, InkthreadCost = 2;
        public const int TincturePips = 5;

        /// <summary>The Flourishes' damage: Crosshatch is six hits of one, Longstroke one piercing blow, Blot one round her.</summary>
        public const int CrosshatchHits = 6, CrosshatchDamage = 1, LongstrokeDamage = 3, BlotDamage = 1;

        /// <summary>Damage a Flourish does to one target for each pip it costs.</summary>
        public static float DamagePerPip(int damage, int cost) => cost <= 0 ? 0f : (float)damage / cost;

        static Tuning()
        {
            // Health = the tier's base × access (docs/design/tuning.md §3). Every sheet, built or not.
            B("lamp_keeper", 1, BossAccess.Windowed);        // open only while grounded after a dive
            B("reedmother_brood", 1, BossAccess.Windowed);   // a swarm: the nest opens between broods
            B("halvard", 1, BossAccess.Open);
            B("collapse", 2, BossAccess.Windowed);           // drawn for 60% of a beat, in one section
            B("brann", 2, BossAccess.Open);
            B("choir", 2, BossAccess.Windowed);              // only the doves
            B("gatekeeper", 2, BossAccess.Open);
            B("halvard_2", 2, BossAccess.Open);
            B("oriel", 3, BossAccess.Open);
            B("hale", 3, BossAccess.Windowed);               // the stones take her time
            B("fallen_star", 3, BossAccess.Windowed);        // only the seam, from above
            B("voss", 3, BossAccess.Open);
            B("halvard_3", 3, BossAccess.Open);
            B("bells", 3, BossAccess.Counted, 4);            // four ropes
            B("corras_drawing", 4, BossAccess.Windowed);     // 0.6 s redraw after every hit
            B("archivist", 4, BossAccess.Windowed);          // the hand, while he draws
            B("complete_survey", 4, BossAccess.Paced);       // a pool every third beat

            // Strikes to fell; the families the rooms place. Contact is one mask for all of them.
            E("ReedSkimmer", 2);    // fodder on the wing
            E("MarshCrab", 3);      // pogo only
            E("Smudge", 3);         // drawn frames only
            E("Cantor", 4);         // Longstroke reaches it; a hit stops the ring
            E("Warden", 5);         // parry: the 1 s stagger is a combo and change
            E("LostRemnant", 5);    // the Blank's
            E("CaveBat", 2);        // Emberdown's fodder on the wing: struck as it swoops
            E("Salamander", 3);     // its back burns: pogo only
        }
    }
}
