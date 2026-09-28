using OWSBG.World;
using UnityEngine;

namespace OWSBG.Tests
{
    /// <summary>
    /// Strikes to a boss's next phase, from its health (CMB-19): the tuning table sets the health, so the fight tests
    /// count from it instead of carrying the old numbers. Phases begin at two thirds and one third.
    /// </summary>
    static class BossHits
    {
        public static int ToPhase(Boss b, int phase)
        {
            float at = phase >= 3 ? 1f / 3f : 2f / 3f;
            return Mathf.Max(0, b.Health - Mathf.FloorToInt(b.MaxHealth * at + 1e-4f));
        }
    }
}
