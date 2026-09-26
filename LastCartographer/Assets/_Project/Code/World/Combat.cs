using UnityEngine;

namespace OWSBG.World
{
    public struct HitInfo
    {
        public int Damage;
        public Vector2 Direction;
        public GameObject Source;
    }

    /// <summary>Anything the quill can hit: enemies, dummies, bells, breakable floors.</summary>
    public interface IHittable
    {
        /// <returns>true if the hit landed (fills the Inkwell, triggers hitstop and pogo).</returns>
        bool TakeHit(in HitInfo hit);
    }
}
