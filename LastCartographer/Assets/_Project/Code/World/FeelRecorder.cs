using System;
using System.Collections.Generic;
using System.IO;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Keeps the feel-test's numbers (PRO-03) while a tester runs the course: every named press, whether it was
    /// acted on and how many frames it waited, or ran out; jumps by wait, coyote and wall jumps, dashes, pogos,
    /// landings, hits; the seconds in each station and the falls into its pits, from which it puts the tester
    /// back at the station's start. <see cref="Save"/> writes the session as JSON beside the answers.
    /// </summary>
    public sealed class FeelRecorder : MonoBehaviour
    {
        public static readonly string[] Actions = { "Jump", "Attack", "Dash", "Flourish", "Instrument", "CycleInstrument", "Thread" };

        public WrenController Wren { get; set; }
        public string Tester { get; set; } = "";
        /// <summary>Off: no station timing, no pits (a bare test scene).</summary>
        public bool OnCourse { get; set; } = true;
        public FeelTest.Station Station { get; private set; }
        public int CoyoteJumps { get; private set; }
        public int WallJumps { get; private set; }
        public int Dashes { get; private set; }
        public int Pogos { get; private set; }
        public int Landings { get; private set; }
        public int Falls { get; private set; }
        public int Hits { get; private set; }
        public float Seconds { get; private set; }

        readonly int[] _presses = new int[7], _acted = new int[7], _dropped = new int[7];
        readonly int[] _jumpWaits = new int[8];
        readonly float[] _stationSeconds = new float[7];
        readonly int[] _stationFalls = new int[7];
        bool _wasGrounded, _bound;
        int _framesSinceGround, _lastJumpWait = -1;

        static int Index(string action) => Array.IndexOf(Actions, action);

        void OnEnable()
        {
            ButtonBuffer.Pressed += OnPressed;
            ButtonBuffer.Consumed += OnConsumed;
            ButtonBuffer.Dropped += OnDropped;
        }

        void OnDisable()
        {
            ButtonBuffer.Pressed -= OnPressed;
            ButtonBuffer.Consumed -= OnConsumed;
            ButtonBuffer.Dropped -= OnDropped;
            Unbind();
        }

        void Bind()
        {
            if (_bound || Wren == null) return;
            _bound = true;
            Wren.Jumped += OnJumped; Wren.Landed += OnLanded; Wren.Dashed += OnDashed; Wren.Pogoed += OnPogoed;
            _wasGrounded = Wren.IsGrounded;
        }

        void Unbind()
        {
            if (!_bound || Wren == null) return;
            _bound = false;
            Wren.Jumped -= OnJumped; Wren.Landed -= OnLanded; Wren.Dashed -= OnDashed; Wren.Pogoed -= OnPogoed;
        }

        void OnPressed(string a) { int i = Index(a); if (i >= 0) _presses[i]++; }
        void OnDropped(string a) { int i = Index(a); if (i >= 0) _dropped[i]++; }
        void OnConsumed(string a, int waited)
        {
            int i = Index(a);
            if (i < 0) return;
            _acted[i]++;
            if (a == "Jump") { _jumpWaits[Mathf.Clamp(waited, 0, _jumpWaits.Length - 1)]++; _lastJumpWait = waited; }
        }

        void OnJumped()
        {
            // Off the ground with no wall, and the press acted on the frame it was made: the coyote window let her.
            // (A press that waited was taken on the landing frame, which the buffer's job, not the window's.)
            if (Wren != null && !_wasGrounded)
            {
                if (Wren.WallDirection != 0) WallJumps++;
                else if (_framesSinceGround > 0 && _lastJumpWait == 0) CoyoteJumps++;
            }
            _lastJumpWait = -1;
        }
        void OnLanded() => Landings++;
        void OnDashed() => Dashes++;
        void OnPogoed() => Pogos++;
        /// <summary>A strike landed on a target; the course's targets call it.</summary>
        public void NoteHit() => Hits++;

        void FixedUpdate()
        {
            if (Wren == null) Wren = WrenController.Current;
            Bind();
            if (Wren == null) return;
            float dt = Time.fixedDeltaTime;
            Seconds += dt;
            bool grounded = Wren.IsGrounded;
            if (grounded) _framesSinceGround = 0; else _framesSinceGround++;
            _wasGrounded = grounded;
            if (!OnCourse) return;
            var pos = Wren.Position;
            Station = FeelTest.StationAt(pos.x);
            _stationSeconds[(int)Station] += dt;
            if (pos.y < FeelTest.PitY + 1f)
            {
                Falls++;
                _stationFalls[(int)Station]++;
                Wren.Teleport(FeelTest.RestartOf(Station));
            }
        }

        public FeelTest.Session Session()
        {
            var s = new FeelTest.Session
            {
                version = BuildInfo.Label, device = Controls.LastDevice.ToString(), tester = Tester ?? "",
                seconds = Seconds, actions = (string[])Actions.Clone(),
                presses = (int[])_presses.Clone(), acted = (int[])_acted.Clone(), dropped = (int[])_dropped.Clone(),
                jumpWaits = (int[])_jumpWaits.Clone(),
                coyoteJumps = CoyoteJumps, wallJumps = WallJumps, dashes = Dashes, pogos = Pogos, landings = Landings, falls = Falls, hits = Hits,
                stations = new string[FeelTest.Stations.Length], stationSeconds = (float[])_stationSeconds.Clone(), stationFalls = (int[])_stationFalls.Clone(),
            };
            for (int i = 0; i < FeelTest.Stations.Length; i++) s.stations[i] = FeelTest.Stations[i].Id.ToString();
            return s;
        }

        /// <summary>The session as JSON at the path; the folder is made. Returns the path.</summary>
        public string Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, Session().ToJson());
            Debug.Log("[OWSBG] feel: " + path);
            return path;
        }
    }
}
