using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Enemies drop iris seeds when they die (DES-05): a few per family, spread on the ground where they fell.
    /// One of these lives in the persistent scene and listens to every death.
    /// </summary>
    public sealed class IrisSeedDrops : MonoBehaviour
    {
        public int Spawned { get; private set; }

        public static int DropsFor(string family)
        {
            switch (family)
            {
                case "MarshCrab": return 1;
                case "ReedSkimmer": return 1;
                case "Smudge": return 2;
                case "MemorySmudge": return 0;   // her own death's smudge (GDD 6)
                case "Cantor": return 3;
                case "Warden": return 2;
                case "LampKeeper": return 10;
                default: return 1;
            }
        }

        void OnEnable() { Enemy.AnyDied += OnDied; }
        void OnDisable() { Enemy.AnyDied -= OnDied; }

        void OnDied(Enemy e)
        {
            if (e == null) return;
            int n = DropsFor(e.Family);
            if (n <= 0) return;
            var pos = (Vector2)e.transform.position;
            for (int i = 0; i < n; i++)
            {
                var off = new Vector2((i - (n - 1) * 0.5f) * 0.45f, 0.4f);
                IrisSeed.Spawn(pos + off, 1, e.gameObject.scene);
                Spawned++;
            }
        }
    }
}
