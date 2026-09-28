using System;
using System.Collections;
using System.Collections.Generic;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The endings runner and the epilogue walk (PRG-23; bible 7.4, 9). An ending's last scene says &lt;&lt;epilogue&gt;&gt;;
    /// once that scene has finished, the runner plays Voss's coda at the frame's door if it has not played, then walks
    /// the ending's stops (<see cref="Endings.EpilogueWalk"/>): to white, to the stop's room (the built one, or a
    /// stand-in from <see cref="EpilogueBuilder"/>), back in, the stop's scene, a moment to stand there, and on. After
    /// the last stop the screen goes white for good and the game's title is shown. Lives in the persistent scene.
    /// </summary>
    public sealed class EndingsRunner : MonoBehaviour
    {
        public const string StartedFlag = "epilogue.started";
        public const string FinishedFlag = "game.finished";
        public const string VossCodaNode = "Observatory_Voss";
        public const string VossCodaFlag = "ending.voss_coda";
        public const string Title = "The Last Cartographer";

        /// <summary>Tests that walk the epilogue by hand turn this off; the &lt;&lt;epilogue&gt;&gt; command then does nothing.</summary>
        public static bool AutoWalk = true;
        public static EndingsRunner Instance { get; private set; }

        [SerializeField] float _holdSeconds = 3f;
        [SerializeField] float _fadeSeconds = 1.2f;

        public float HoldSeconds { get => _holdSeconds; set => _holdSeconds = value; }
        public float FadeSeconds { get => _fadeSeconds; set => _fadeSeconds = value; }
        public bool IsWalking { get; private set; }
        /// <summary>The rooms the walk has been to, in order.</summary>
        public IReadOnlyList<string> Visited => _visited;
        public event Action<string> StopReached;
        public event Action Finished;

        readonly List<string> _visited = new List<string>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Where a stop's scene plays: the zone's built room if it has one, else a stand-in made for it.</summary>
        public static string SceneFor(string zone) => WorldGraph.BuiltRoomScene(zone) ?? EpilogueBuilder.SceneFor(zone);

        /// <summary>Start the walk for the chosen ending. False if nothing is chosen, it has begun already, or walking is off.</summary>
        public bool Begin()
        {
            var w = GameState.World;
            if (!AutoWalk || IsWalking || Endings.Chosen(w) == Ending.None || w.Is(StartedFlag)) return false;
            StartCoroutine(Walk(Endings.Chosen(w)));
            return true;
        }

        IEnumerator Walk(Ending ending)
        {
            IsWalking = true;
            _visited.Clear();
            var w = GameState.World;
            w.Set(StartedFlag, true);
            yield return null;
            yield return WaitForDialogue();           // the ending's own scene, which said <<epilogue>> mid-way

            if (!w.Is(VossCodaFlag)) yield return Play(VossCodaNode);   // at the frame's door, before she leaves it

            foreach (var stop in Endings.EpilogueStops(ending))
            {
                yield return Fade(1f);
                var scene = SceneFor(stop.Zone);
                var rooms = RoomManager.Instance;
                if (rooms != null && rooms.CurrentRoom != scene)
                {
                    rooms.Transition(scene, "Start");
                    float t = 0f;
                    while ((rooms.CurrentRoom != scene || rooms.IsTransitioning) && t < 20f) { t += Time.deltaTime; yield return null; }
                    if (rooms.CurrentRoom != scene) Debug.LogWarning("[OWSBG] the epilogue could not reach " + scene + " for " + stop.Node);
                }
                _visited.Add(rooms != null ? rooms.CurrentRoom : scene);
                StopReached?.Invoke(stop.Node);
                yield return Fade(0f);
                yield return Play(stop.Node);
                if (_holdSeconds > 0f) yield return new WaitForSeconds(_holdSeconds);
            }

            yield return Fade(1f);
            w.Set(FinishedFlag, true);
            Captions.Show(Title, 8f);
            IsWalking = false;
            Finished?.Invoke();
        }

        IEnumerator Play(string node)
        {
            var svc = DialogueService.Instance;
            if (svc == null || !svc.StartNode(node)) { Debug.LogWarning("[OWSBG] the epilogue has no scene " + node); yield break; }
            yield return null;
            yield return WaitForDialogue();
        }

        static IEnumerator WaitForDialogue()
        {
            var svc = DialogueService.Instance;
            float t = 0f;
            while (svc != null && svc.IsRunning && t < 300f) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator Fade(float to)
        {
            float from = ScreenFade.Level;
            if (_fadeSeconds <= 0f) { ScreenFade.Set(to, ScreenFade.White); yield break; }
            float t = 0f;
            while (t < _fadeSeconds)
            {
                t += Time.deltaTime;
                ScreenFade.Set(Mathf.Lerp(from, to, t / _fadeSeconds), ScreenFade.White);
                yield return null;
            }
            ScreenFade.Set(to, ScreenFade.White);
        }
    }
}
