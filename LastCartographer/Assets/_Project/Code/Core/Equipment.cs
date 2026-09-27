using System;
using System.Collections.Generic;

namespace OWSBG.Core
{
    /// <summary>The stances (combat doc 5). Three base Charters; later ones are found in the world.</summary>
    public enum CharterKind { Surveyor, Warden, Drifter, Ferryman, Unwriter, Remnant }

    /// <summary>The surveying tools used sideways (combat doc 6).</summary>
    public enum InstrumentKind { None, CompassDart, PlumbWeight, SightingLens, FieldLantern, TetherHook, IrisTincture, WaxSeal }

    /// <summary>One belt slot: what is in it and how many uses remain until the next desk.</summary>
    [Serializable]
    public struct InstrumentSlot
    {
        public InstrumentKind Kind;
        public int UsesLeft;
        public bool IsEmpty => Kind == InstrumentKind.None;
    }

    /// <summary>
    /// What Wren carries. Lives inside <see cref="WorldState"/> so it saves with everything else.
    /// Rules (which Charter does what, how many uses an Instrument has) live in the World assembly;
    /// this is only the record of ownership and equipment.
    /// </summary>
    [Serializable]
    public sealed class Equipment
    {
        public const int BaseSlotCount = 3;
        public const int UpgradedSlotCount = 4;

        public CharterKind Charter = CharterKind.Surveyor;
        public HashSet<CharterKind> OwnedCharters = new HashSet<CharterKind> { CharterKind.Surveyor };
        public HashSet<InstrumentKind> OwnedInstruments = new HashSet<InstrumentKind>();
        public InstrumentSlot[] Slots = new InstrumentSlot[UpgradedSlotCount];
        public bool FourthSlotUnlocked;
        public int SelectedSlot;

        public int SlotCount => FourthSlotUnlocked ? UpgradedSlotCount : BaseSlotCount;

        public event Action<CharterKind> CharterChanged;
        public event Action SlotsChanged;

        public bool OwnsCharter(CharterKind k) => OwnedCharters.Contains(k);
        public bool OwnsInstrument(InstrumentKind k) => k != InstrumentKind.None && OwnedInstruments.Contains(k);

        public bool SetCharter(CharterKind k)
        {
            if (!OwnsCharter(k)) return false;
            if (Charter == k) return false;
            Charter = k;
            CharterChanged?.Invoke(k);
            return true;
        }

        public bool IsEquipped(InstrumentKind k)
        {
            for (int i = 0; i < SlotCount; i++) if (Slots[i].Kind == k) return true;
            return false;
        }

        public int SlotOf(InstrumentKind k)
        {
            if (k == InstrumentKind.None) return -1;
            for (int i = 0; i < SlotCount; i++) if (Slots[i].Kind == k) return i;
            return -1;
        }

        /// <summary>
        /// Put an owned Instrument in a slot (None clears it). An Instrument already in another slot
        /// moves: the two slots swap contents, so nothing is ever held twice.
        /// </summary>
        public bool Equip(int slot, InstrumentKind k, int uses)
        {
            if (slot < 0 || slot >= SlotCount) return false;
            if (k != InstrumentKind.None && !OwnsInstrument(k)) return false;
            int other = SlotOf(k);
            if (other == slot) return false;
            if (other >= 0)
            {
                var moved = Slots[other];          // keeps its remaining uses
                Slots[other] = Slots[slot];
                Slots[slot] = moved;
            }
            else Slots[slot] = new InstrumentSlot { Kind = k, UsesLeft = k == InstrumentKind.None ? 0 : uses };
            SlotsChanged?.Invoke();
            return true;
        }

        public void SetUses(int slot, int uses)
        {
            if (slot < 0 || slot >= Slots.Length) return;
            Slots[slot].UsesLeft = uses;
            SlotsChanged?.Invoke();
        }

        public void NotifySlotsChanged() => SlotsChanged?.Invoke();
    }
}
