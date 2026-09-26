using System;
using UnityEngine;

namespace OWSBG.Core
{
    /// <summary>Fragments of the sky. Story bible 4.0; each is a movement gate.</summary>
    [Flags]
    public enum Ability
    {
        None       = 0,
        Wingbeat   = 1 << 0,   // air dash
        Talonhold  = 1 << 1,   // wall cling and wall jump
        Inkthread  = 1 << 2,   // grapple
        Windmemory = 1 << 3,   // glide
        Clarity    = 1 << 4,   // enter fades untethered
        Sky        = 1 << 5,   // endgame flight
    }

    public sealed class AbilitySet : MonoBehaviour
    {
        [SerializeField] Ability _unlocked = Ability.None;

        public event Action<Ability> Unlocked;

        public Ability Current => _unlocked;
        public bool Has(Ability a) => (_unlocked & a) == a;

        public void Unlock(Ability a)
        {
            if (Has(a)) return;
            _unlocked |= a;
            Unlocked?.Invoke(a);
        }

        public void Set(Ability all) => _unlocked = all;
    }
}
