using System;
using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>Designer-facing description of one Charter (combat doc 5).</summary>
    [Serializable]
    public sealed class CharterProfile
    {
        public CharterKind Kind;
        public string DisplayName;
        [TextArea] public string Blurb;
        public ComboStep[] Combo;
        public FlourishKind DefaultFlourish = FlourishKind.Crosshatch;
        [Header("Passives")]
        public float InkGainMultiplier = 1f;
        public int MaskBonus;          // Warden +1
        public int MaskCap;            // Drifter 4; 0 = none
        public float DashScale = 1f;   // Warden 0.7
        public int ExtraAirDashes;     // Drifter 1
        public Color Tint = Color.white;   // greybox silhouette stand-in

        public static CharterProfile Surveyor() => new CharterProfile
        {
            Kind = CharterKind.Surveyor, DisplayName = "Surveyor's Charter",
            Blurb = "Balanced, precise. Slash, slash, thrust. Ink fills a quarter faster.",
            Combo = new[]
            {
                ComboStep.Make("Slash", 2.2f, 3, 4, 8),
                ComboStep.Make("Slash", 2.2f, 3, 4, 8),
                ComboStep.Make("Thrust", 3.0f, 4, 4, 12, 1, 2.5f, 0.7f),
            },
            DefaultFlourish = FlourishKind.Crosshatch,
            InkGainMultiplier = 1.25f,
            Tint = new Color(0.98f, 0.96f, 0.90f),
        };

        public static CharterProfile Warden() => new CharterProfile
        {
            Kind = CharterKind.Warden, DisplayName = "Warden's Charter",
            Blurb = "Heavy, grounded, spacing. Sweep, shove, overhead. One more mask; a shorter Wingbeat.",
            Combo = new[]
            {
                ComboStep.Make("Sweep", 2.6f, 6, 5, 12, 2, 1.5f, 1.2f),
                ComboStep.Make("Shove", 1.8f, 4, 4, 10, 1, 3.0f, 1.2f),
                ComboStep.Make("Overhead", 2.4f, 8, 5, 14, 3, 1.5f, 1.6f),
            },
            DefaultFlourish = FlourishKind.Blot,
            MaskBonus = 1,
            DashScale = 0.7f,
            Tint = new Color(0.62f, 0.70f, 0.80f),
        };

        public static CharterProfile Drifter() => new CharterProfile
        {
            Kind = CharterKind.Drifter, DisplayName = "Drifter's Charter",
            Blurb = "Aerial, glass. Three fast slashes, no thrust. A second Wingbeat; four masks at most.",
            Combo = new[]
            {
                ComboStep.Make("Slash", 2.0f, 2, 3, 5),
                ComboStep.Make("Slash", 2.0f, 2, 3, 5),
                ComboStep.Make("Slash", 2.0f, 2, 3, 6),
            },
            DefaultFlourish = FlourishKind.Longstroke,
            MaskCap = 4,
            ExtraAirDashes = 1,
            Tint = new Color(0.90f, 0.78f, 0.62f),
        };
    }

    /// <summary>
    /// On Wren. Holds the Charter profiles and applies the equipped one (from
    /// <see cref="WorldState.Equipment"/>) to the strike, Flourishes, Inkwell, vitals and controller.
    /// Swapping happens at a drafting desk; this listens for the change.
    /// </summary>
    [RequireComponent(typeof(WrenController))]
    public sealed class CharterSet : MonoBehaviour
    {
        [SerializeField] List<CharterProfile> _profiles = new List<CharterProfile>();
        [Tooltip("Greybox: own every base Charter from the start.")]
        [SerializeField] bool _unlockAllBase = true;

        public CharterProfile Current { get; private set; }
        public event Action<CharterProfile> Applied;

        WrenController _ctrl;
        QuillStrike _strike;
        Flourishes _flourishes;
        Inkwell _ink;
        WrenVitals _vitals;
        Renderer _visual;
        Equipment _bound;
        MaterialPropertyBlock _mpb;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public IReadOnlyList<CharterProfile> Profiles => _profiles;

        public CharterProfile Find(CharterKind kind)
        {
            foreach (var p in _profiles) if (p.Kind == kind) return p;
            return null;
        }

        void Awake()
        {
            if (_profiles.Count == 0)
                _profiles.AddRange(new[] { CharterProfile.Surveyor(), CharterProfile.Warden(), CharterProfile.Drifter() });
            _ctrl = GetComponent<WrenController>();
            _strike = GetComponent<QuillStrike>();
            _flourishes = GetComponent<Flourishes>();
            _ink = GetComponent<Inkwell>();
            _vitals = GetComponent<WrenVitals>();
            _visual = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            GameState.Loaded += Rebind;
            Rebind();
        }

        void OnDisable()
        {
            GameState.Loaded -= Rebind;
            if (_bound != null) _bound.CharterChanged -= OnCharterChanged;
            _bound = null;
        }

        void Rebind()
        {
            if (_bound != null) _bound.CharterChanged -= OnCharterChanged;
            _bound = GameState.World.Equipment;
            if (_unlockAllBase)
            {
                _bound.OwnedCharters.Add(CharterKind.Surveyor);
                _bound.OwnedCharters.Add(CharterKind.Warden);
                _bound.OwnedCharters.Add(CharterKind.Drifter);
            }
            _bound.CharterChanged += OnCharterChanged;
            Apply(_bound.Charter);
        }

        void OnCharterChanged(CharterKind kind) => Apply(kind);

        /// <summary>Apply a profile directly (tests, tooling). Normal play goes through Equipment.SetCharter.</summary>
        public void Apply(CharterKind kind)
        {
            var p = Find(kind) ?? Find(CharterKind.Surveyor) ?? (_profiles.Count > 0 ? _profiles[0] : null);
            if (p == null) return;
            Current = p;
            if (_strike != null) _strike.Combo = p.Combo;
            if (_flourishes != null) _flourishes.DefaultKind = p.DefaultFlourish;
            if (_ink != null) _ink.GainMultiplier = p.InkGainMultiplier;
            if (_vitals != null)
            {
                int max = _vitals.BaseMaxMasks + p.MaskBonus;
                if (p.MaskCap > 0) max = Mathf.Min(max, p.MaskCap);
                _vitals.SetMaxMasks(max);
            }
            if (_ctrl != null)
            {
                _ctrl.DashScale = p.DashScale;
                _ctrl.ExtraAirDashes = p.ExtraAirDashes;
            }
            if (_visual != null)
            {
                _visual.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, p.Tint);
                _visual.SetPropertyBlock(_mpb);
            }
            Applied?.Invoke(p);
        }
    }
}
