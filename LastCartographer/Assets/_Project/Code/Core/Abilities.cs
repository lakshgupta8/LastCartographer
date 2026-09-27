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

    /// <summary>
    /// Wren's abilities. What she has learned is also written to the world as "ability.&lt;name&gt;" flags so it is saved,
    /// and restored from them on enable and on load; the serialized field is what the greybox starts her with.
    /// </summary>
    public sealed class AbilitySet : MonoBehaviour
    {
        [SerializeField] Ability _unlocked = Ability.None;

        public event Action<Ability> Unlocked;

        public Ability Current => _unlocked;
        public bool Has(Ability a) => (_unlocked & a) == a;

        public static string FlagKey(Ability a) => "ability." + a.ToString().ToLowerInvariant();

        /// <summary>Every single ability whose flag is set in the world.</summary>
        public static Ability FromWorld(WorldState w)
        {
            var have = Ability.None;
            foreach (Ability a in Enum.GetValues(typeof(Ability)))
                if (a != Ability.None && w.Is(FlagKey(a))) have |= a;
            return have;
        }

        void OnEnable() { GameState.Loaded += Restore; Restore(); }
        void OnDisable() { GameState.Loaded -= Restore; }
        void Restore() { _unlocked |= FromWorld(GameState.World); }

        public void Unlock(Ability a)
        {
            foreach (Ability one in Enum.GetValues(typeof(Ability)))
                if (one != Ability.None && (a & one) == one) GameState.World.Set(FlagKey(one), true);
            if (Has(a)) return;
            _unlocked |= a;
            Unlocked?.Invoke(a);
        }

        public void Set(Ability all) => _unlocked = all;
    }
}
