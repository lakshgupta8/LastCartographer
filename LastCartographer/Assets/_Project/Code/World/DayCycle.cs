using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Runs the day (PRG-15): advances DayClock with play time (paused while Wren is frozen: menus, talk,
    /// cutscenes) and tints the paper for the hour of the room Wren is in, which is the locked hour in an
    /// anchored place. Two globals for the paper-grain pass: _OWSBG_Dusk (warm) and _OWSBG_Night (cool, dark).
    /// </summary>
    public sealed class DayCycle : MonoBehaviour
    {
        [SerializeField] float _daySeconds = 900f;
        [SerializeField] bool _pauseWhileFrozen = true;
        [SerializeField] float _tintSeconds = 1.5f;

        public const string DuskGlobal = "_OWSBG_Dusk";
        public const string NightGlobal = "_OWSBG_Night";

        public static float Dusk { get; private set; }
        public static float Night { get; private set; }

        public float DaySeconds { get => _daySeconds; set => _daySeconds = value; }
        public string PlaceId => Room.Current != null ? Room.Current.RoomId : "";
        public DayPhase PhaseHere => DayClock.PhaseIn(GameState.World, PlaceId);
        public bool IsPaused { get; private set; }

        WrenController _wren;

        void OnDisable()
        {
            Dusk = 0f; Night = 0f;
            Shader.SetGlobalFloat(DuskGlobal, 0f);
            Shader.SetGlobalFloat(NightGlobal, 0f);
        }

        void Update()
        {
            var w = GameState.World;
            if (_wren == null) _wren = FindFirstObjectByType<WrenController>();
            IsPaused = _pauseWhileFrozen && _wren != null && _wren.Frozen;
            if (!IsPaused && _daySeconds > 0f && Time.deltaTime > 0f) DayClock.Advance(w, Time.deltaTime / _daySeconds);

            Targets(DayClock.TimeIn(w, PlaceId), out float dusk, out float night);
            float k = Time.unscaledDeltaTime / Mathf.Max(0.01f, _tintSeconds);
            Dusk = Mathf.MoveTowards(Dusk, dusk, k);
            Night = Mathf.MoveTowards(Night, night, k);
            Shader.SetGlobalFloat(DuskGlobal, Dusk);
            Shader.SetGlobalFloat(NightGlobal, Night);
        }

        /// <summary>The tint amounts for a time of day: dawn is a little of both, dusk warm, night cool.</summary>
        public static void Targets(float t, out float dusk, out float night)
        {
            dusk = 0f; night = 0f;
            if (t < DayClock.DawnEnd)
            {
                float k = t / DayClock.DawnEnd;
                dusk = 0.5f * (1f - k);
                night = 0.5f * (1f - k);
            }
            else if (t < DayClock.DayEnd) { }
            else if (t < DayClock.DuskEnd)
            {
                float k = (t - DayClock.DayEnd) / (DayClock.DuskEnd - DayClock.DayEnd);
                dusk = Mathf.Sin(k * Mathf.PI);
                night = 0.6f * k;
            }
            else
            {
                float k = (t - DayClock.DuskEnd) / (1f - DayClock.DuskEnd);
                night = Mathf.Lerp(1f, 0.6f, k);
            }
        }
    }
}
