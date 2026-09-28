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

        /// <summary>The Charter's name in the player's language ("charter.&lt;kind&gt;.name"; NAR-18). <see cref="DisplayName"/> is the English.</summary>
        public string LocalName => Loc.T("charter." + Kind + ".name", DisplayName);
        public string LocalBlurb => string.IsNullOrEmpty(Blurb) ? Blurb ?? "" : Loc.T("charter." + Kind + ".blurb", Blurb);
        public FlourishKind DefaultFlourish = FlourishKind.Crosshatch;
        [Header("Passives")]
        public float InkGainMultiplier = 1f;
        public int MaskBonus;          // Warden +1
        public int MaskCap;            // Drifter 4; 0 = none
        public float DashScale = 1f;   // Warden 0.7
        public int ExtraAirDashes;     // Drifter 1
        public Color Tint = Color.white;   // greybox silhouette stand-in
        public int BindCost = 3;           // Unwriter 4
        public int InkthreadCost = 2;      // Ferryman 1 (read by the Inkthread when it exists, CMB-04)
        public bool ErasesProjectiles;     // Unwriter: strikes erase enemy projectiles
        public float Drain;                // Remnant: colour each strike takes

        /// <summary>The profile of a Charter by kind.</summary>
        public static CharterProfile For(CharterKind kind) => kind switch
        {
            CharterKind.Warden => Warden(),
            CharterKind.Drifter => Drifter(),
            CharterKind.Ferryman => Ferryman(),
            CharterKind.Unwriter => Unwriter(),
            CharterKind.Remnant => Remnant(),
            _ => Surveyor(),
        };

        /// <summary>Found with Sable's tether-cord (combat doc 5): reach, a swing, and a reel that pulls. Inkthread costs 1.</summary>
        public static CharterProfile Ferryman() => new CharterProfile
        {
            Kind = CharterKind.Ferryman, DisplayName = "Ferryman's Charter",
            Blurb = "A hook on a cord. Hook, swing, reel. The thread comes cheap: one pip.",
            Combo = new[]
            {
                ComboStep.Make("Hook", 3.2f, 5, 4, 10),
                ComboStep.Make("Swing", 3.4f, 4, 5, 10, 1, 1.5f),
                ComboStep.Make("Reel", 3.0f, 6, 4, 14, 2, 1.2f).Pulling(),
            },
            DefaultFlourish = FlourishKind.Longstroke,
            InkthreadCost = 1,
            Tint = new Color(0.52f, 0.62f, 0.60f),
        };

        /// <summary>From the Choir's last dove (6.6): strikes erase what is thrown at her; Bind costs 4.</summary>
        public static CharterProfile Unwriter() => new CharterProfile
        {
            Kind = CharterKind.Unwriter, DisplayName = "Unwriter's Charter",
            Blurb = "The Cantors' own. Hush, hush, toll. What is thrown at her is unwritten. Bind costs four.",
            Combo = new[]
            {
                ComboStep.Make("Hush", 2.2f, 3, 4, 8),
                ComboStep.Make("Hush", 2.2f, 3, 4, 8),
                ComboStep.Make("Toll", 2.6f, 7, 6, 14, 2, 1.5f, 1.6f),
            },
            DefaultFlourish = FlourishKind.Blot,
            BindCost = 4,
            ErasesProjectiles = true,
            Tint = new Color(0.94f, 0.92f, 0.86f),
        };

        /// <summary>Act 3, from Ilse in the Hollow: every strike drains the colour it lands on.</summary>
        public static CharterProfile Remnant() => new CharterProfile
        {
            Kind = CharterKind.Remnant, DisplayName = "Remnant Charter",
            Blurb = "Grey is a colour you can carry. Fade, fade, pale. What she strikes goes grey and slow.",
            Combo = new[]
            {
                ComboStep.Make("Fade", 2.2f, 3, 4, 9),
                ComboStep.Make("Fade", 2.2f, 3, 4, 9),
                ComboStep.Make("Pale", 2.8f, 5, 4, 12, 1, 1f, 1.0f),
            },
            DefaultFlourish = FlourishKind.Crosshatch,
            Drain = 0.34f,
            Tint = new Color(0.62f, 0.62f, 0.62f),
        };

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
                _profiles.AddRange(new[]
                {
                    CharterProfile.Surveyor(), CharterProfile.Warden(), CharterProfile.Drifter(),
                    CharterProfile.Ferryman(), CharterProfile.Unwriter(), CharterProfile.Remnant(),
                });
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
            if (_strike != null) { _strike.Combo = p.Combo; _strike.Drain = p.Drain; }
            if (_flourishes != null) _flourishes.DefaultKind = p.DefaultFlourish;
            if (_ink != null) _ink.GainMultiplier = p.InkGainMultiplier;
            if (_vitals != null)
            {
                _vitals.BindCost = p.BindCost;
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
