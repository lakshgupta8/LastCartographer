#nullable enable
using System.Collections;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The Edge (bible 7.0, NAR-03). Sequences the prologue from flags so a reload lands in the right beat:
    ///   arrive cutscene → Prologue_Edge_Arrive
    ///   the vantage surveyed → Prologue_Edge_Survey (bind, seal) → Dusk
    ///   Dusk over → three smudges; all dead → Prologue_Edge_AfterSmudges → Departure (&lt;&lt;cutscene edge_departure&gt;&gt;)
    ///   Isolde in → the wall goes, the Blank's edge drives the white; crossing it → the Saltmarrow shore.
    /// </summary>
    public sealed class PrologueDirector : MonoBehaviour
    {
        public const string Started = "prologue.started";
        public const string Sealed = "prologue.sealed_edge_page";
        public const string FirstSmudges = "prologue.first_smudges";
        public const string IsoldeEntered = "prologue.isolde_entered";
        public const string Crossed = "prologue.crossed";

        [SerializeField] Cutscene? _arrive;
        [SerializeField] VantagePoint? _vantage;
        [SerializeField] GameObject? _isolde;
        [SerializeField] GameObject[] _smudges = System.Array.Empty<GameObject>();
        [SerializeField] GameObject? _blankWall;
        [SerializeField] BlankEdge? _blankEdge;
        [SerializeField] string _shoreScene = "Greybox_Saltmarrow_A";
        [SerializeField] string _shoreSpawn = "Shore";

        public bool SmudgesReleased { get; private set; }
        public bool BlankOpen { get; private set; }

        DialogueService? _service;
        WorldState? _world;
        bool _afterSmudgesQueued;

        static WorldState W => GameState.World;

        IEnumerator Start()
        {
            while (RoomManager.Instance != null && RoomManager.Instance.IsTransitioning) yield return null;
            yield return null;
            Bind();
            var w = W;
            if (w.Is(IsoldeEntered)) { OpenTheBlank(); yield break; }
            if (w.Is(Sealed) && !w.Is(FirstSmudges)) { ReleaseSmudges(); yield break; }
            if (!w.Is(Started))
            {
                if (_arrive != null) _arrive.Play();
                else _service?.StartNode("Prologue_Edge_Arrive");
            }
        }

        void Bind()
        {
            _world = W;
            _world.VantageSurveyed += OnSurveyed;
            _service = DialogueService.Instance;
            if (_service != null)
            {
                _service.Completed += OnDialogueCompleted;
                _service.BindPrompt += OnBindPrompt;
                _service.Tutorial += OnTutorial;
            }
            if (_blankEdge != null) _blankEdge.Crossed += OnCrossed;
        }

        void OnDestroy()
        {
            if (_world != null) _world.VantageSurveyed -= OnSurveyed;
            if (_service != null)
            {
                _service.Completed -= OnDialogueCompleted;
                _service.BindPrompt -= OnBindPrompt;
                _service.Tutorial -= OnTutorial;
            }
            if (_blankEdge != null) _blankEdge.Crossed -= OnCrossed;
        }

        void OnSurveyed(string id)
        {
            if (_vantage == null || id != _vantage.VantageId || W.Is(Sealed)) return;
            StartCoroutine(StartWhenFree("Prologue_Edge_Survey"));
        }

        IEnumerator StartWhenFree(string node)
        {
            var svc = _service ?? DialogueService.Instance;
            while (svc != null && svc.IsRunning) yield return null;
            svc?.StartNode(node);
        }

        void OnBindPrompt(string memoryId)
        {
            var w = W;
            if (!w.BoundMemories.Contains(memoryId)) w.BoundMemories.Add(memoryId);
            Captions.Show("Bound: " + MemoryName(memoryId), 4f);
        }

        public static string MemoryName(string id) => id switch
        {
            "isolde.first_sight" => "the first time she saw you",
            _ => id,
        };

        void OnTutorial(string name)
        {
            if (name != "seal") return;
            var wren = FindFirstObjectByType<WrenController>();
            if (wren == null) return;
            var w = W;
            w.WaxSealRoom = RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom)
                ? RoomManager.Instance.CurrentRoom : gameObject.scene.name;
            w.WaxSealX = wren.Position.x;
            w.WaxSealY = wren.Position.y;
        }

        void OnDialogueCompleted()
        {
            var w = W;
            if (w.Is(IsoldeEntered)) { OpenTheBlank(); return; }
            if (w.Is(Sealed) && !w.Is(FirstSmudges)) ReleaseSmudges();
        }

        void ReleaseSmudges()
        {
            if (SmudgesReleased) return;
            SmudgesReleased = true;
            foreach (var s in _smudges) if (s != null) s.SetActive(true);
        }

        void Update()
        {
            if (!SmudgesReleased || _afterSmudgesQueued || W.Is(FirstSmudges) || !AllSmudgesDead()) return;
            _afterSmudgesQueued = true;
            W.Set(FirstSmudges, 1);
            StartCoroutine(StartWhenFree("Prologue_Edge_AfterSmudges"));
        }

        bool AllSmudgesDead()
        {
            foreach (var s in _smudges)
            {
                if (s == null) continue;                    // destroyed on death
                var e = s.GetComponent<Enemy>();
                if (e == null || !e.IsDead) return false;
            }
            return true;
        }

        void OpenTheBlank()
        {
            if (BlankOpen) return;
            BlankOpen = true;
            if (_blankWall != null) _blankWall.SetActive(false);
            if (_isolde != null)
            {
                var talker = _isolde.GetComponent<NpcTalker>();
                if (talker != null) talker.enabled = false;
            }
            if (_blankEdge != null) _blankEdge.Active = true;
        }

        void OnCrossed()
        {
            var w = W;
            if (w.Is(Crossed)) return;
            w.Set(Crossed, 1);
            if (_blankEdge != null) _blankEdge.Active = false;   // the white stays at full until the shore thins it
            RoomManager.Instance?.Transition(_shoreScene, _shoreSpawn);
        }
    }
}
