using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A region's fledglings leaping in the background of one room (CHR-14; bible 10, "Flight is memory";
    /// docs/design/fledglings.md). Six young birds on one perch leap in turn, each on its own timeline: the leap at the
    /// perch, the flight, the landing, then gone (climbing back, out of sight) until it stands on the perch again before
    /// its next turn. <see cref="Dressing.At"/> says how many leap, how many of them glide and how far: a glider's
    /// reach grows half a wingspan for each piece of the sky Wren has; the rest drop as they always have; an anchored
    /// place's never glide; a fade thins them; in the Open World the newest glider does not come down. The loop reads
    /// the world on enable, on every flag (abilities, fates and fades are flags) and on load.
    /// </summary>
    public sealed class FledglingLoop : MonoBehaviour
    {
        public enum Phase { Perched, Leaping, Gliding, Dropping, Landing, Gone }

        public struct Pose
        {
            public Vector2 Position;
            public Phase Phase;
            public bool Visible;
        }

        /// <summary>How far a dropper gets, in wingspans.</summary>
        public const float DropReach = 1.5f;
        public const float LeapSeconds = 0.33f;
        public const float LandSeconds = 0.33f;
        public const float DropSeconds = 0.7f;
        /// <summary>A glide's time: this, plus a little for every wingspan past the drop.</summary>
        public const float GlideSeconds = 0.9f, GlideSecondsPerWingspan = 0.3f;
        /// <summary>How long a bird stands on the perch before its turn.</summary>
        public const float SettleSeconds = 1.4f;

        [SerializeField] Region _region;
        [SerializeField] string _placeId = "";
        [SerializeField] Vector2 _perch;
        [SerializeField] float _landingY;
        [SerializeField] int _direction = 1;
        [SerializeField] float _wingspan = 0.8f;
        [SerializeField] float _interval = 1.4f;
        [SerializeField] List<Transform> _birds = new List<Transform>();

        WorldState _world;
        readonly List<InkSheetPlayer> _sheets = new List<InkSheetPlayer>();

        public Region Region => _region;
        public string PlaceId => _placeId;
        public Vector2 Perch => _perch;
        public float LandingY => _landingY;
        public int Direction => _direction;
        public float Wingspan => _wingspan;
        public float Interval => _interval;
        public IReadOnlyList<Transform> Birds => _birds;
        /// <summary>What the loop shows now, from the world.</summary>
        public Dressing.LoopState State { get; private set; }
        /// <summary>Seconds the loop has run.</summary>
        public float Time { get; private set; }
        /// <summary>One bird's whole turn: every bird's turn, one after another.</summary>
        public float Period => Dressing.Leapers * _interval;

        public void Configure(Region region, string placeId, Vector2 perch, float landingY, int direction, List<Transform> birds, float wingspan = 0.8f, float interval = 1.4f)
        {
            _region = region; _placeId = placeId; _perch = perch; _landingY = landingY;
            _direction = direction < 0 ? -1 : 1; _birds = birds; _wingspan = wingspan; _interval = interval;
            Refresh();
        }

        void OnEnable()
        {
            _sheets.Clear();
            foreach (var b in _birds) _sheets.Add(b != null ? b.GetComponent<InkSheetPlayer>() : null);
            GameState.Loaded += Rebind;
            Rebind();
        }

        void OnDisable()
        {
            GameState.Loaded -= Rebind;
            if (_world != null) _world.FlagChanged -= OnFlag;
            _world = null;
        }

        void Rebind()
        {
            if (_world != null) _world.FlagChanged -= OnFlag;
            _world = GameState.World;
            if (_world != null) _world.FlagChanged += OnFlag;
            Refresh();
        }

        void OnFlag(string key, int value) => Refresh();

        /// <summary>Read the world: Wren's kit, the place's fate and fade, the ending chosen.</summary>
        public void Refresh()
        {
            var loop = Dressing.LoopOf(_region);
            var w = GameState.World;
            if (loop == null) { State = default; return; }
            State = w == null
                ? Dressing.At(loop, Ability.None)
                : Dressing.At(loop, AbilitySet.FromWorld(w), Places.FateOf(w, _placeId), FadeStages.Get(w, _placeId), Endings.Chosen(w));
        }

        public bool Leaps(int bird) => bird < State.Leaping;
        public bool Glides(int bird) => bird < State.Gliding;
        /// <summary>The Open World's one who doesn't come down: the newest glider.</summary>
        public bool Flies(int bird) => State.OneFlies && State.Gliding > 0 && bird == State.Gliding - 1;
        /// <summary>How far this bird gets from the perch, in units: a dropper's leap, or a glider's, half a wingspan more for each glider before it.</summary>
        public float Reach(int bird) => _wingspan * (Glides(bird) ? DropReach + (bird + 1) * Dressing.GlideStep : DropReach);
        public float FlightSeconds(int bird) => Glides(bird) ? GlideSeconds + GlideSecondsPerWingspan * (bird + 1) * Dressing.GlideStep : DropSeconds;

        /// <summary>Where a bird is and what it does at a time of the loop, from its own turn's timeline.</summary>
        public Pose Evaluate(int bird, float time)
        {
            var pose = new Pose { Position = _perch, Phase = Phase.Gone, Visible = false };
            if (!Leaps(bird)) return pose;
            float period = Period;
            float u = ((time - bird * _interval) % period + period) % period;
            float flight = FlightSeconds(bird);
            bool glides = Glides(bird), flies = Flies(bird);
            if (u < LeapSeconds) { pose.Phase = Phase.Leaping; pose.Visible = true; return pose; }
            u -= LeapSeconds;
            if (u < flight)
            {
                float f = u / flight;
                float reach = Reach(bird);
                pose.Visible = true;
                if (flies)
                {
                    // Up and away: the arc never bends down, and the drawing thins out before the top of the room.
                    pose.Phase = Phase.Gliding;
                    pose.Position = _perch + new Vector2(_direction * reach * 1.6f * f, (reach * 1.2f + 1.5f) * f);
                    return pose;
                }
                float drop = _landingY - _perch.y;
                // A glide bends gently all the way down; a drop sags at once.
                float fall = glides ? Mathf.Pow(f, 1.6f) : f * f;
                float lift = glides ? 0.25f * Mathf.Sin(f * Mathf.PI) : 0f;
                pose.Phase = glides ? Phase.Gliding : Phase.Dropping;
                pose.Position = new Vector2(_perch.x + _direction * reach * f, _perch.y + drop * fall + lift);
                return pose;
            }
            u -= flight;
            if (flies) return pose;
            if (u < LandSeconds)
            {
                pose.Phase = Phase.Landing; pose.Visible = true;
                pose.Position = new Vector2(_perch.x + _direction * Reach(bird), _landingY);
                return pose;
            }
            // Gone, climbing back; on the perch again a while before its next turn.
            if (period - u - LeapSeconds - flight <= SettleSeconds) { pose.Phase = Phase.Perched; pose.Visible = true; }
            return pose;
        }

        void Update()
        {
            Time += UnityEngine.Time.deltaTime;
            Apply(Time);
        }

        /// <summary>The loop from its start: the first bird's turn now.</summary>
        public void Restart()
        {
            Time = 0f;
            Apply(0f);
        }

        /// <summary>Put every bird where the loop has it at this time, in its clip.</summary>
        public void Apply(float time)
        {
            for (int i = 0; i < _birds.Count; i++)
            {
                var b = _birds[i];
                if (b == null) continue;
                var pose = Evaluate(i, time);
                if (b.gameObject.activeSelf != pose.Visible) b.gameObject.SetActive(pose.Visible);
                if (!pose.Visible) continue;
                b.localPosition = new Vector3(pose.Position.x, pose.Position.y, 0f);
                var sheet = i < _sheets.Count ? _sheets[i] : null;
                if (sheet != null) sheet.Play(ClipOf(pose.Phase));
            }
        }

        /// <summary>The scene at rest, for the editor and a capture: every leaping bird along its own flight, so the loop reads in one frame.</summary>
        public void Preview()
        {
            if (_sheets.Count == 0) foreach (var b in _birds) _sheets.Add(b != null ? b.GetComponent<InkSheetPlayer>() : null);
            for (int i = 0; i < _birds.Count; i++)
            {
                var b = _birds[i];
                if (b == null) continue;
                bool on = Leaps(i);
                b.gameObject.SetActive(on);
                if (!on) continue;
                float f = (i + 0.5f) / Dressing.Leapers;
                var pose = Evaluate(i, i * _interval + LeapSeconds + FlightSeconds(i) * f);
                b.localPosition = new Vector3(pose.Position.x, pose.Position.y, 0f);
            }
        }

        public static string ClipOf(Phase p) => p switch
        {
            Phase.Leaping => "leap",
            Phase.Gliding => "glide",
            Phase.Dropping => "drop",
            Phase.Landing => "land",
            _ => "idle",
        };
    }
}
