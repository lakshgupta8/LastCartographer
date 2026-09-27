#nullable enable
using System;
using OWSBG.Core;
using OWSBG.World;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;

namespace OWSBG.Narrative
{
    /// <summary>
    /// The game's single entry point to Yarn Spinner (PRG-08). Owns a DialogueRunner, binds it to
    /// WorldState-backed variables and the scene's presenters, registers the project's custom
    /// commands, and freezes Wren while a conversation runs.
    ///
    /// Commands available in .yarn files:
    ///   &lt;&lt;flag key value&gt;&gt;     set a narrative flag (int)        e.g. &lt;&lt;flag prologue.started 1&gt;&gt;
    ///   &lt;&lt;survey_hint&gt;&gt;         tutorial hook (raises Tutorial "survey")
    ///   &lt;&lt;tutorial name&gt;&gt;       tutorial hook (raises Tutorial name)
    ///   &lt;&lt;bind_prompt memoryId&gt;&gt; offer a memory to bind (raises BindPrompt)
    /// </summary>
    public sealed class DialogueService : MonoBehaviour
    {
        public static DialogueService? Instance { get; private set; }

        [SerializeField] YarnProject? _project;
        [SerializeField] DialoguePresenterBase[] _presenters = Array.Empty<DialoguePresenterBase>();
        [SerializeField] bool _freezeWren = true;

        DialogueRunner? _runner;
        WorldStateVariableStorage? _storage;
        WrenController? _wren;

        public DialogueRunner? Runner => _runner;
        public bool IsRunning => _runner != null && _runner.IsDialogueRunning;
        public event Action? Started;
        public event Action? Completed;
        public event Action<string>? NodeStarted;
        public event Action<string>? Tutorial;
        public event Action<string>? BindPrompt;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            EnsureRunner();
            if (_project != null) _runner!.SetProject(_project);
            if (_presenters.Length > 0) _runner!.DialoguePresenters = _presenters;
            else _runner!.DialoguePresenters = GetComponentsInChildren<DialoguePresenterBase>(true);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>For tests and tooling: wire a project and presenters after construction.</summary>
        public void Initialize(YarnProject project, params DialoguePresenterBase[] presenters)
        {
            EnsureRunner();
            _project = project;
            _presenters = presenters;
            _runner!.DialoguePresenters = presenters;
            _runner.SetProject(project);
        }

        void EnsureRunner()
        {
            if (_runner != null) return;
            _storage = GetComponent<WorldStateVariableStorage>();
            if (_storage == null) _storage = gameObject.AddComponent<WorldStateVariableStorage>();
            _runner = GetComponent<DialogueRunner>();
            if (_runner == null) _runner = gameObject.AddComponent<DialogueRunner>();
            _runner.VariableStorage = _storage;   // before anything touches Dialogue
            _runner.onDialogueStart ??= new UnityEvent();
            _runner.onDialogueComplete ??= new UnityEvent();
            _runner.onNodeStart ??= new UnityEventString();
            _runner.onDialogueStart.AddListener(OnStart);
            _runner.onDialogueComplete.AddListener(OnComplete);
            _runner.onNodeStart.AddListener(n => NodeStarted?.Invoke(n));
            RegisterCommands(_runner);
        }

        void RegisterCommands(DialogueRunner runner)
        {
            runner.AddCommandHandler<string, int>("flag", (key, value) => GameState.World.Set(key, value));
            runner.AddCommandHandler("survey_hint", () => Tutorial?.Invoke("survey"));
            runner.AddCommandHandler<string>("tutorial", name => Tutorial?.Invoke(name));
            runner.AddCommandHandler<string>("bind_prompt", id => BindPrompt?.Invoke(id));
        }

        // Functions are declared with attributes so the compiler knows their signatures.
        [YarnFunction("flag")]
        public static float FlagValue(string key) => GameState.World.Get(key);

        [YarnFunction("has_flag")]
        public static bool HasFlag(string key) => GameState.World.Is(key);

        [YarnFunction("surveyed")]
        public static bool IsSurveyed(string vantageId) => GameState.World.IsSurveyed(vantageId);

        public bool StartNode(string node)
        {
            if (_runner == null || _project == null)
            {
                Debug.LogWarning("[OWSBG] DialogueService has no Yarn project; cannot start " + node);
                return false;
            }
            if (_runner.IsDialogueRunning) return false;
            if (!_runner.Dialogue.NodeExists(node))
            {
                Debug.LogWarning("[OWSBG] Yarn node not found: " + node);
                return false;
            }
            _runner.StartDialogue(node).Forget();
            return true;
        }

        public void Stop()
        {
            if (_runner != null && _runner.IsDialogueRunning) _runner.Stop().Forget();
        }

        void OnStart()
        {
            if (_freezeWren)
            {
                _wren = FindFirstObjectByType<WrenController>();
                if (_wren != null) _wren.Frozen = true;
            }
            Started?.Invoke();
        }

        void OnComplete()
        {
            if (_wren != null) { _wren.Frozen = false; _wren = null; }
            Completed?.Invoke();
        }
    }
}
