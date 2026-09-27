using System.Collections;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Halvard's first hunt (bible 6.3, 7.1 step 3; NAR-04): the Guild finds her the moment the fourth lamp is
    /// lit. A few seconds after the Lamp-Keeper falls, or on entering the lighthouse later with the lamp lit and
    /// the hunt not yet met, Halvard walks in, measures her, and walks out (a cutscene with a dialogue clip).
    /// </summary>
    public sealed class HalvardHunt : MonoBehaviour
    {
        [SerializeField] Cutscene _cutscene;
        [SerializeField] GameObject _halvard;
        [SerializeField] string _bossFlag = "boss.lamp_keeper.defeated";
        [SerializeField] string _doneFlag = "act1.halvard_met";
        [SerializeField] float _delayAfterWin = 3f;
        [SerializeField] float _delayOnEntry = 1f;

        public bool Played { get; private set; }
        public Cutscene Cutscene { get => _cutscene; set => _cutscene = value; }
        public GameObject Halvard { get => _halvard; set => _halvard = value; }

        void OnEnable()
        {
            BossArena.FightWon += OnWon;
            if (_halvard != null) _halvard.SetActive(false);
            StartCoroutine(MaybeOnEntry());
        }

        void OnDisable()
        {
            BossArena.FightWon -= OnWon;
            if (_cutscene != null) _cutscene.Completed -= OnDone;
        }

        IEnumerator MaybeOnEntry()
        {
            var w = GameState.World;
            if (!w.Is(_bossFlag) || w.Is(_doneFlag)) yield break;
            while (RoomManager.Instance != null && RoomManager.Instance.IsTransitioning) yield return null;
            yield return Wait(_delayOnEntry);
            Play();
        }

        void OnWon(BossArena arena)
        {
            if (!isActiveAndEnabled) return;
            StartCoroutine(AfterWin());
        }

        IEnumerator AfterWin()
        {
            yield return Wait(_delayAfterWin);
            var wren = FindFirstObjectByType<WrenController>();
            while (wren != null && wren.Frozen) yield return null;   // a desk page or the arena's own staging
            Play();
        }

        static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.deltaTime; yield return null; }
        }

        void Play()
        {
            var w = GameState.World;
            if (Played || w.Is(_doneFlag) || _cutscene == null) return;
            Played = true;
            if (_halvard != null) _halvard.SetActive(true);
            _cutscene.Completed += OnDone;
            _cutscene.Play();
        }

        void OnDone(Cutscene c)
        {
            _cutscene.Completed -= OnDone;
            if (_halvard != null) _halvard.SetActive(false);   // he leaves; the reeds carry the rest
        }
    }
}
