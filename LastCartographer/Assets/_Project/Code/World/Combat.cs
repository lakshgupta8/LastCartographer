using UnityEngine;

namespace OWSBG.World
{
    public struct HitInfo
    {
        public int Damage;
        public Vector2 Direction;
        public GameObject Source;
        /// <summary>Knockback multiplier on the target's own knockback (1 = normal; the combo thrust uses 2.5).</summary>
        public float Knockback;
        public float KnockbackOrDefault => Knockback <= 0f ? 1f : Knockback;
    }

    /// <summary>One forward swing of the combo (combat doc 2.1 and 5). Frames at 60 Hz, reach in units.</summary>
    [System.Serializable]
    public struct ComboStep
    {
        public string Name;
        public float Reach;
        public float Thickness;
        public int Startup, Active, Recovery;
        public int Damage;
        public float Knockback;   // multiplier on the target's knockback

        public static ComboStep Make(string name, float reach, int startup, int active, int recovery, int damage = 1, float knockback = 1f, float thickness = 0.9f)
            => new ComboStep { Name = name, Reach = reach, Thickness = thickness, Startup = startup, Active = active, Recovery = recovery, Damage = damage, Knockback = knockback };
    }

    /// <summary>Something a Field lantern reveals for a while (hidden platforms, undrawn Smudges).</summary>
    public interface IRevealable
    {
        void Reveal(float seconds);
    }

    /// <summary>Anything the quill can hit: enemies, dummies, bells, breakable floors.</summary>
    public interface IHittable
    {
        /// <returns>true if the hit landed (fills the Inkwell, triggers hitstop and pogo).</returns>
        bool TakeHit(in HitInfo hit);
    }
}
