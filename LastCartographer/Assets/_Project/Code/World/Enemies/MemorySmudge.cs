using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The smudge of Wren's own death (GDD 6, PRG-17): an ink-beast made of what she had bound, standing where she
    /// fell. It flickers like any smudge and lunges like one; strike it down while drawn and the memories come
    /// back to her. Drops no seeds. Spawned by <see cref="MemoryDrops"/>, never placed in a scene.
    /// </summary>
    public sealed class MemorySmudge : Smudge
    {
        /// <summary>Struck down: the drop is hers again.</summary>
        public event Action<MemorySmudge> Recovered;

        public override string Family => "MemorySmudge";

        protected override void Die()
        {
            base.Die();
            Recovered?.Invoke(this);
        }

        protected override Color TintColor() => new Color(0.10f, 0.14f, 0.30f);
    }
}
