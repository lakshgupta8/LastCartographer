using System;
using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>Static rules for each Instrument (combat doc 6). Uses of -1 mean unlimited with a cooldown.</summary>
    public readonly struct InstrumentInfo
    {
        public readonly InstrumentKind Kind;
        public readonly string Name;
        public readonly int Uses;
        public readonly float Cooldown;
        public readonly string Blurb;

        InstrumentInfo(InstrumentKind kind, string name, int uses, float cooldown, string blurb)
        { Kind = kind; Name = name; Uses = uses; Cooldown = cooldown; Blurb = blurb; }

        public bool Unlimited => Uses < 0;

        static readonly Dictionary<InstrumentKind, InstrumentInfo> Table = new Dictionary<InstrumentKind, InstrumentInfo>
        {
            { InstrumentKind.CompassDart,  new InstrumentInfo(InstrumentKind.CompassDart,  "Compass-dart",  12, 0.25f, "Thrown. Homes gently. Marks what it hits for the Inkthread.") },
            { InstrumentKind.PlumbWeight,  new InstrumentInfo(InstrumentKind.PlumbWeight,  "Plumb weight",   6, 0.4f,  "A heavy lob. Three damage. Breaks weak floors.") },
            { InstrumentKind.SightingLens, new InstrumentInfo(InstrumentKind.SightingLens, "Sighting lens", -1, 3f,    "Raise it and time it: reflects what flies, staggers what bites.") },
            { InstrumentKind.FieldLantern, new InstrumentInfo(InstrumentKind.FieldLantern, "Field lantern",  4, 0.5f,  "Ten seconds of honest light. Hidden things are drawn.") },
            { InstrumentKind.TetherHook,   new InstrumentInfo(InstrumentKind.TetherHook,   "Tether-hook",    3, 0.5f,  "A rope anchor. An Inkthread point where there was none.") },
            { InstrumentKind.IrisTincture, new InstrumentInfo(InstrumentKind.IrisTincture, "Iris tincture",  2, 0.2f,  "Five pips, at once.") },
            { InstrumentKind.WaxSeal,      new InstrumentInfo(InstrumentKind.WaxSeal,      "Wax seal",       1, 0.2f,  "Press it here. Once, you will come back to it.") },
        };

        public static InstrumentInfo Of(InstrumentKind k) => Table.TryGetValue(k, out var i) ? i : default;
        public static IEnumerable<InstrumentInfo> All => Table.Values;
    }

    /// <summary>
    /// On Wren. The Instrument slots (3, 4 with the upgrade) read from <see cref="WorldState.Equipment"/>.
    /// The Instrument button uses the selected slot; the cycle button moves the selection. Uses are
    /// restored at drafting desks. Effects are implemented here or in small spawned behaviours.
    /// </summary>
    [RequireComponent(typeof(WrenController))]
    public sealed class InstrumentBelt : MonoBehaviour
    {
        [Header("Compass-dart")]
        public float dartSpeed = 16f;
        public float dartMarkSeconds = 8f;
        [Header("Plumb weight")]
        public Vector2 plumbVelocity = new Vector2(7f, 9f);
        public int plumbDamage = 3;
        [Header("Sighting lens")]
        public int parryWindowFrames = 10;
        public float parryStaggerSeconds = 1f;
        [Header("Field lantern")]
        public float lanternSeconds = 10f;
        [Header("Tether-hook")]
        public float tetherSeconds = 20f;
        [Header("Iris tincture")]
        public int tincturePips = 5;
        [Header("Shared")]
        public float throwHeight = 0.8f;
        public LayerMask hitMask;
        public LayerMask groundMask;
        [Tooltip("Greybox: own every Instrument from the start, with the first three equipped.")]
        [SerializeField] bool _unlockAllForGreybox = true;
        [Tooltip("With unlock-all off: own the starting kit (compass-dart, plumb weight, sighting lens); the rest is bought (DES-05).")]
        [SerializeField] bool _starterKit = true;

        public Equipment Equipment => _bound ?? GameState.World.Equipment;
        public int SelectedSlot => Equipment.SelectedSlot;
        public bool IsParrying => _parryLeft > 0;
        /// <summary>Hale's lens (boss 6.9): once he is beaten, the sighting lens comes round in half the time.</summary>
        public static float CooldownOf(InstrumentInfo info, WorldState w)
            => info.Kind == InstrumentKind.SightingLens && w != null && w.Is(Bosses.FlagKey("hale")) ? info.Cooldown * 0.5f : info.Cooldown;

        public float CooldownLeft(int slot) => slot >= 0 && slot < _cooldowns.Length ? Mathf.Max(0f, _cooldowns[slot] - Time.time) : 0f;

        public event Action<InstrumentKind, int> Used;       // kind, slot
        public event Action<InstrumentKind, int> Refused;    // no uses / cooldown / empty
        public event Action<Enemy> Parried;
        public event Action<int> SelectionChanged;

        WrenController _ctrl;
        Inkwell _ink;
        StrikeVisual _visual;
        Equipment _bound;
        readonly float[] _cooldowns = new float[Equipment.UpgradedSlotCount];
        int _parryLeft;

        void Awake()
        {
            _ctrl = GetComponent<WrenController>();
            _ink = GetComponent<Inkwell>();
            _visual = GetComponent<StrikeVisual>();
            if (hitMask.value == 0) hitMask = LayerMask.GetMask("Enemy", "Hittable");
            if (groundMask.value == 0) groundMask = LayerMask.GetMask("Ground");
        }

        void OnEnable()
        {
            GameState.Loaded += Rebind;
            DraftingDesk.Rested += OnRested;
            Rebind();
        }

        void OnDisable()
        {
            GameState.Loaded -= Rebind;
            DraftingDesk.Rested -= OnRested;
        }

        void Rebind()
        {
            _bound = GameState.World.Equipment;
            if (_unlockAllForGreybox || _starterKit)
            {
                if (_unlockAllForGreybox) foreach (var info in InstrumentInfo.All) _bound.OwnedInstruments.Add(info.Kind);
                else
                {
                    _bound.OwnedInstruments.Add(InstrumentKind.CompassDart);
                    _bound.OwnedInstruments.Add(InstrumentKind.PlumbWeight);
                    _bound.OwnedInstruments.Add(InstrumentKind.SightingLens);
                }
                if (_bound.Slots[0].IsEmpty && _bound.Slots[1].IsEmpty && _bound.Slots[2].IsEmpty)
                {
                    Equip(0, InstrumentKind.CompassDart);
                    Equip(1, InstrumentKind.PlumbWeight);
                    Equip(2, InstrumentKind.SightingLens);
                }
            }
        }

        void OnRested(DraftingDesk _) => RestoreUses();

        /// <summary>Refill every slot to its Instrument's full uses (drafting desk).</summary>
        public void RestoreUses()
        {
            var e = Equipment;
            for (int i = 0; i < e.Slots.Length; i++)
            {
                if (e.Slots[i].IsEmpty) continue;
                var info = InstrumentInfo.Of(e.Slots[i].Kind);
                e.Slots[i].UsesLeft = info.Unlimited ? -1 : info.Uses;
                _cooldowns[i] = 0f;
            }
            e.NotifySlotsChanged();
        }

        public bool Equip(int slot, InstrumentKind kind)
        {
            var info = InstrumentInfo.Of(kind);
            bool ok = Equipment.Equip(slot, kind, info.Unlimited ? -1 : info.Uses);
            if (ok && slot < _cooldowns.Length) _cooldowns[slot] = 0f;
            return ok;
        }

        public void Select(int slot)
        {
            var e = Equipment;
            slot = ((slot % e.SlotCount) + e.SlotCount) % e.SlotCount;
            if (e.SelectedSlot == slot) return;
            e.SelectedSlot = slot;
            SelectionChanged?.Invoke(slot);
        }

        public void CycleSelection() => Select(Equipment.SelectedSlot + 1);

        void FixedUpdate()
        {
            if (_parryLeft > 0) _parryLeft--;
            if (_ctrl.Frozen || _ctrl.Input == null) return;
            if (_ctrl.Input.ConsumeCycleInstrument()) CycleSelection();
            if (_ctrl.Input.ConsumeInstrument()) TryUse(Equipment.SelectedSlot);
        }

        /// <summary>Use the Instrument in a slot. Spends a use up front; refuses when empty or cooling down.</summary>
        public bool TryUse(int slot)
        {
            var e = Equipment;
            if (slot < 0 || slot >= e.SlotCount) return false;
            var s = e.Slots[slot];
            if (s.IsEmpty) { Refused?.Invoke(InstrumentKind.None, slot); return false; }
            var info = InstrumentInfo.Of(s.Kind);
            if (Time.time < _cooldowns[slot] || (!info.Unlimited && s.UsesLeft <= 0))
            {
                Refused?.Invoke(s.Kind, slot);
                return false;
            }
            if (!Perform(s.Kind)) { Refused?.Invoke(s.Kind, slot); return false; }
            if (!info.Unlimited) e.SetUses(slot, s.UsesLeft - 1);
            _cooldowns[slot] = Time.time + CooldownOf(info, GameState.World);
            Used?.Invoke(s.Kind, slot);
            return true;
        }

        bool Perform(InstrumentKind kind)
        {
            var origin = _ctrl.Position + Vector2.up * throwHeight;
            var facing = new Vector2(_ctrl.Facing, 0f);
            switch (kind)
            {
                case InstrumentKind.CompassDart:
                    CompassDart.Spawn(origin, facing * dartSpeed, dartMarkSeconds, hitMask, groundMask, gameObject);
                    return true;

                case InstrumentKind.PlumbWeight:
                    PlumbWeight.Spawn(origin, new Vector2(facing.x * plumbVelocity.x, plumbVelocity.y), plumbDamage, hitMask, groundMask, gameObject);
                    return true;

                case InstrumentKind.SightingLens:
                    _parryLeft = parryWindowFrames;
                    return true;

                case InstrumentKind.FieldLantern:
                {
                    int n = 0;
                    foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (mb is IRevealable r) { r.Reveal(lanternSeconds); n++; }
                    _visual?.Burst(origin, Vector2.zero, 10, 6f);
                    return true;
                }

                case InstrumentKind.TetherHook:
                    TetherAnchor.Spawn(origin + Vector2.up * 2.5f, tetherSeconds);
                    return true;

                case InstrumentKind.IrisTincture:
                    if (_ink == null || _ink.Pips >= _ink.MaxPips) return false;
                    _ink.Add(tincturePips);
                    return true;

                case InstrumentKind.WaxSeal:
                {
                    var w = GameState.World;
                    w.WaxSealRoom = RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom)
                        ? RoomManager.Instance.CurrentRoom
                        : (Room.Current != null ? Room.Current.gameObject.scene.name : gameObject.scene.name);
                    w.WaxSealX = _ctrl.Position.x;
                    w.WaxSealY = _ctrl.Position.y;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Called by an enemy about to deal contact damage. True if the lens caught it: the enemy staggers instead.</summary>
        public bool TryParry(Enemy enemy)
        {
            if (_parryLeft <= 0 || enemy == null) return false;
            _parryLeft = 0;
            enemy.Stagger(parryStaggerSeconds);
            Hitstop.Request(4);
            _visual?.Burst(_ctrl.Position + Vector2.up * throwHeight, ((Vector2)enemy.transform.position - _ctrl.Position).normalized, 8, 7f);
            Parried?.Invoke(enemy);
            return true;
        }
    }
}
