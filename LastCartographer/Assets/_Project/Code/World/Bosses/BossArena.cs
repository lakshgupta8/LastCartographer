using System;
using System.Collections;
using System.Collections.Generic;
using OWSBG.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The room state around a boss (CMB-10): a zone that, when Wren steps in and the boss is not
    /// yet defeated, closes the doors, runs a short intro, starts the fight; on the boss's death
    /// opens the doors, writes the flag and hands out the reward; on Wren's death resets the boss
    /// so the retry is just walking back in. The first entry plays the intro cutscene (PRG-16) if one
    /// is set, retries use a short wait; an arena camera (PRG-06) takes over from the follow rig while
    /// the fight is on and hands back when it ends either way.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class BossArena : MonoBehaviour
    {
        public enum ArenaState { Idle, Intro, Fighting, Won }

        [SerializeField] string _bossId = "lamp_keeper";
        [SerializeField] Boss _boss;
        [SerializeField] List<GameObject> _doors = new List<GameObject>();
        [SerializeField] float _introSeconds = 1.2f;
        [SerializeField] float _retryIntroSeconds = 0.4f;
        [Header("Staging")]
        [SerializeField] Cutscene _introCutscene;
        [SerializeField] CinemachineCamera _arenaCamera;
        [SerializeField] int _arenaCameraPriority = 40;
        [Header("Reward")]
        [SerializeField] Ability _rewardAbility = Ability.None;
        [SerializeField] int _vellumScraps = 1;
        [SerializeField] string _beaconVantageId = "";
        [SerializeField] LayerMask _playerMask;

        public ArenaState State { get; private set; }
        public Boss Boss => _boss;
        public string BossId => _bossId;
        public string FlagKey => Bosses.FlagKey(_bossId);
        public bool IsDefeated => GameState.World.Is(FlagKey);
        public float IntroSeconds { get => _introSeconds; set => _introSeconds = value; }
        public int VellumScraps { get => _vellumScraps; set => _vellumScraps = value; }
        /// <summary>A Charter handed over when the boss falls (the Choir's Unwriter's Charter).</summary>
        public CharterKind? RewardCharter { get; set; }
        public float RetryIntroSeconds { get => _retryIntroSeconds; set => _retryIntroSeconds = value; }
        public IReadOnlyList<GameObject> Doors => _doors;
        public Cutscene IntroCutscene { get => _introCutscene; set => _introCutscene = value; }
        public CinemachineCamera ArenaCamera { get => _arenaCamera; set => _arenaCamera = value; }
        public bool ArenaCameraActive => _arenaCamera != null && _arenaCamera.gameObject.activeSelf;

        public static event Action<BossArena> FightStarted, FightWon, FightReset;
        public event Action<ArenaState> StateChanged;

        Collider2D _zone;
        WrenController _wren;
        WrenVitals _vitals;
        readonly Collider2D[] _overlaps = new Collider2D[4];
        bool _hasFoughtOnce;
        bool _armed = true;   // false after a reset until Wren has stepped out of the zone

        void Awake()
        {
            _zone = GetComponent<Collider2D>();
            _zone.isTrigger = true;
            if (_playerMask.value == 0) _playerMask = LayerMask.GetMask("Player");
            if (_boss == null) _boss = GetComponentInChildren<Boss>();
        }

        void OnEnable()
        {
            if (_boss != null) _boss.Defeated += OnBossDefeated;
            SetDoors(false);
            SetArenaCamera(false);
        }

        void OnDisable()
        {
            if (_boss != null) _boss.Defeated -= OnBossDefeated;
            if (_vitals != null) _vitals.Died -= OnWrenDied;
        }

        public void Configure(Boss boss, IEnumerable<GameObject> doors, string bossId, Ability reward)
        {
            if (_boss != null) _boss.Defeated -= OnBossDefeated;
            _boss = boss;
            _boss.Defeated += OnBossDefeated;
            _doors.Clear();
            _doors.AddRange(doors);
            _bossId = bossId;
            _rewardAbility = reward;
            SetDoors(false);
        }

        void FixedUpdate()
        {
            if (State != ArenaState.Idle || _boss == null || IsDefeated) return;
            bool inside = WrenInside();
            if (!_armed) { if (!inside) _armed = true; return; }
            if (!inside) return;
            if (_vitals != null && _vitals.IsDead) return;
            StartCoroutine(Intro());
        }

        bool WrenInside()
        {
            var b = _zone.bounds;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = _playerMask, useTriggers = false };
            int n = Physics2D.OverlapBox(b.center, b.size, 0f, filter, _overlaps);
            for (int i = 0; i < n; i++)
            {
                var w = _overlaps[i].GetComponentInParent<WrenController>();
                if (w == null) continue;
                _wren = w;
                return true;
            }
            return false;
        }

        IEnumerator Intro()
        {
            SetState(ArenaState.Intro);
            SetDoors(true);
            if (_vitals == null && _wren != null)
            {
                _vitals = _wren.GetComponent<WrenVitals>();
                if (_vitals != null) _vitals.Died += OnWrenDied;
            }
            bool first = !_hasFoughtOnce;
            float wait = first ? _introSeconds : _retryIntroSeconds;
            _hasFoughtOnce = true;
            if (_wren != null) _wren.Frozen = true;
            SetArenaCamera(true);
            FightStarted?.Invoke(this);
            if (first && _introCutscene != null)
            {
                _introCutscene.Play();
                while (_introCutscene.IsPlaying && State == ArenaState.Intro) yield return null;
            }
            else if (wait > 0f) yield return new WaitForSeconds(wait);
            if (_wren != null) _wren.Frozen = false;
            if (State != ArenaState.Intro) yield break;   // reset during the intro
            SetState(ArenaState.Fighting);
            _boss.BeginFight();
        }

        void OnWrenDied()
        {
            if (State != ArenaState.Fighting && State != ArenaState.Intro) return;
            ResetFight();
        }

        /// <summary>Boss back on its perch, doors open, waiting for the next entry.</summary>
        public void ResetFight()
        {
            StopAllCoroutines();
            if (_introCutscene != null && _introCutscene.IsPlaying) _introCutscene.Stop();
            _boss.ResetFight();
            SetDoors(false);
            SetArenaCamera(false);
            _armed = false;
            SetState(ArenaState.Idle);
            FightReset?.Invoke(this);
        }

        void OnBossDefeated(Boss boss)
        {
            SetDoors(false);
            SetArenaCamera(false);
            SetState(ArenaState.Won);
            var w = GameState.World;
            w.Set(FlagKey, true);
            if (_vellumScraps > 0)
            {
                w.Numbers.TryGetValue("$vellum_scraps", out var scraps);
                w.Numbers["$vellum_scraps"] = scraps + _vellumScraps;
            }
            if (!string.IsNullOrEmpty(_beaconVantageId)) w.MarkSurveyed(_beaconVantageId);
            if (RewardCharter.HasValue && w.Equipment.OwnedCharters.Add(RewardCharter.Value))
                Captions.Show(Loc.F("caption.charter", "Charter: {0}", CharterProfile.For(RewardCharter.Value).DisplayName), 3f);
            if (_rewardAbility != Ability.None)
            {
                var wren = _wren != null ? _wren : FindFirstObjectByType<WrenController>();
                var abilities = wren != null ? wren.GetComponent<AbilitySet>() : null;
                if (abilities != null) abilities.Unlock(_rewardAbility);
            }
            FightWon?.Invoke(this);
        }

        void SetArenaCamera(bool on)
        {
            if (_arenaCamera == null) return;
            if (on) _arenaCamera.Priority = _arenaCameraPriority;
            _arenaCamera.gameObject.SetActive(on);
        }

        void SetDoors(bool closed)
        {
            foreach (var d in _doors) if (d != null) d.SetActive(closed);
        }

        void SetState(ArenaState s)
        {
            if (State == s) return;
            State = s;
            StateChanged?.Invoke(s);
        }
    }
}
