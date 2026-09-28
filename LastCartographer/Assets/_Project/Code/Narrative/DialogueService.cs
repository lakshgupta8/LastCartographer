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
    ///   &lt;&lt;commission id verb&gt;&gt;  post | take | fulfil | close | fail a commission (PRG-12)
    ///   &lt;&lt;cutscene id&gt;&gt;          play a Cutscene and wait for it (PRG-16)
    ///   &lt;&lt;fade place stage&gt;&gt;     advance a place's fade stage 0-4 (PRG-14); anchored places ignore it
    ///   &lt;&lt;anchor place&gt;&gt; / &lt;&lt;hold place&gt;&gt; / &lt;&lt;release place&gt;&gt;   the regional decision, once and final (PRG-13)
    /// Functions: flag(key), has_flag(key), surveyed(id), commission_state(id), commission_is(id, state).
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
            runner.AddCommandHandler("cutscene", (Func<string, YarnTask>)PlayCutsceneAsync);
            runner.AddCommandHandler<string, int>("fade", (place, stage) => FadeStages.Advance(GameState.World, place, stage));
            runner.AddCommandHandler<string>("anchor", place => Decide(place, PlaceFate.Anchored));
            runner.AddCommandHandler<string>("hold", place => Decide(place, PlaceFate.Held));
            runner.AddCommandHandler<string>("release", place => Decide(place, PlaceFate.Released));
            runner.AddCommandHandler<string>("clock", phase =>
            {
                if (DayClock.TryParse(phase, out var p)) DayClock.SetPhase(GameState.World, p);
                else Debug.LogWarning("[OWSBG] <<clock " + phase + ">>: not a phase of the day");
            });
            runner.AddCommandHandler("sleep", () => DayClock.Sleep(GameState.World));
            runner.AddCommandHandler<string>("shop", hub => Shops.Request(hub));
            runner.AddCommandHandler<string>("walk", id =>
            {
                var walk = BoundsWalk.Find(id);
                if (walk == null || !walk.Begin()) Debug.LogWarning("[OWSBG] <<walk " + id + ">>: no such walk here, or it is already walked");
            });
            runner.AddCommandHandler("epilogue", () => { EndingsRunner.Instance?.Begin(); });
            runner.AddCommandHandler<string, string>("voice", (kind, scope) =>
            {
                if (Voices.TryParse(kind, out var v)) Voices.Record(GameState.World, v, scope);
                else Debug.LogWarning("[OWSBG] <<voice " + kind + ">>: surveyor, warden or drift");
            });
            runner.AddCommandHandler<string>("grant", name =>
            {
                if (!Enum.TryParse(name, true, out Ability a) || a == Ability.None) { Debug.LogWarning("[OWSBG] <<grant " + name + ">>: no such ability"); return; }
                var wren = FindFirstObjectByType<WrenController>();
                var set = wren != null ? wren.GetComponent<AbilitySet>() : null;
                if (set != null) set.Unlock(a);
                else GameState.World.Set(AbilitySet.FlagKey(a), true);   // no Wren loaded: the flag restores it later
            });
            runner.AddCommandHandler<string>("charter", name =>
            {
                if (!Enum.TryParse(name, true, out CharterKind k)) { Debug.LogWarning("[OWSBG] <<charter " + name + ">>: no such Charter"); return; }
                if (GameState.World.Equipment.OwnedCharters.Add(k)) Captions.Show(Loc.F("caption.charter", "Charter: {0}", CharterProfile.For(k).LocalName), 3f);
            });
            runner.AddCommandHandler<string>("erase", place =>
            {
                if (!Atlas.Erase(GameState.World, place))
                    Debug.LogWarning("[OWSBG] <<erase " + place + ">> refused: already erased or anchored");
            });
            runner.AddCommandHandler("camp", (Func<string, YarnTask>)CampAsync);
            runner.AddCommandHandler<string, string>("commission", (id, verb) =>
            {
                if (!Commissions.Apply(GameState.World, id, verb))
                    Debug.LogWarning("[OWSBG] <<commission " + id + " " + verb + ">> is not a valid move from " + Commissions.StateOf(GameState.World, id));
            });
        }

        static void Decide(string place, PlaceFate fate)
        {
            if (!Places.Decide(GameState.World, place, fate))
                Debug.LogWarning("[OWSBG] <<" + Places.Describe(fate) + " " + place + ">> refused: already " + Places.Describe(Places.FateOf(GameState.World, place)));
        }

        /// <summary><c>&lt;&lt;camp walk&gt;&gt;</c>: walk the day with the Windreach camp to its next site, and wait until there.</summary>
        async YarnTask CampAsync(string verb)
        {
            if (verb != "walk") { Debug.LogWarning("[OWSBG] <<camp " + verb + ">>: only \"walk\""); return; }
            if (!Camp.IsReadyToWalk(GameState.World)) { Debug.LogWarning("[OWSBG] <<camp walk>>: the camp isn't moving yet"); return; }
            StartCoroutine(CampWalk.Go());
            while (CampWalk.IsWalking) await YarnTask.Yield();
        }

        static async YarnTask PlayCutsceneAsync(string id)
        {
            var cs = Cutscene.Find(id);
            if (cs == null) { Debug.LogWarning("[OWSBG] <<cutscene " + id + ">>: no cutscene with that id is loaded"); return; }
            cs.Play();
            while (cs.IsPlaying) await YarnTask.Yield();
        }

        // Functions are declared with attributes so the compiler knows their signatures.
        [YarnFunction("flag")]
        public static float FlagValue(string key) => GameState.World.Get(key);

        [YarnFunction("has_flag")]
        public static bool HasFlag(string key) => GameState.World.Is(key);

        [YarnFunction("surveyed")]
        public static bool IsSurveyed(string vantageId) => GameState.World.IsSurveyed(vantageId);

        /// <summary>"unknown", "posted", "taken", "fulfilled", "closed" or "failed".</summary>
        [YarnFunction("commission_state")]
        public static string CommissionStateOf(string id) => Commissions.Describe(Commissions.StateOf(GameState.World, id));

        /// <summary>"unwritten", "anchored", "held" or "released".</summary>
        [YarnFunction("place_fate")]
        public static string PlaceFateOf(string place) => Places.Describe(Places.FateOf(GameState.World, place));

        [YarnFunction("fade_stage")]
        public static float FadeStageOf(string place) => FadeStages.Get(GameState.World, place);

        /// <summary>"dawn", "day", "dusk" or "night": the world's hour.</summary>
        [YarnFunction("phase")]
        public static string PhaseOfDay() => DayClock.Describe(DayClock.Phase(GameState.World));

        /// <summary>The hour in a place: its locked hour when anchored.</summary>
        [YarnFunction("phase_in")]
        public static string PhaseIn(string place) => DayClock.Describe(DayClock.PhaseIn(GameState.World, place));

        [YarnFunction("day")]
        public static float DayCount() => DayClock.Day(GameState.World);

        /// <summary>The Windreach camp's fire has been had where it stands, and it walks at first light.</summary>
        [YarnFunction("camp_ready")]
        public static bool CampReady() => Camp.IsReadyToWalk(GameState.World);

        /// <summary>Where the Windreach camp stands: 0 the fire ring, 1 the riverbed, 2 the high grass.</summary>
        [YarnFunction("camp_site")]
        public static float CampSiteIndex() => Camp.Site(GameState.World);

        [YarnFunction("seeds")]
        public static float SeedCount() => Economy.Seeds(GameState.World);

        /// <summary>The place's bounds have been walked: it is held.</summary>
        [YarnFunction("walked")]
        public static bool IsWalked(string place) => BoundsWalks.IsWalked(GameState.World, place);

        /// <summary>Wren knows the roll-call (taught at Kettil's Rest, or heard in the whale).</summary>
        [YarnFunction("walk_known")]
        public static bool WalkKnown() => BoundsWalks.IsLearned(GameState.World);

        /// <summary>How many times Wren has chosen a voice (surveyor, warden, drift), across the game.</summary>
        [YarnFunction("voice")]
        public static float VoiceCount(string kind) => Voices.TryParse(kind, out var v) ? Voices.Count(GameState.World, v) : 0f;

        /// <summary>How many times Wren has chosen a voice in one scope (a region: "halden").</summary>
        [YarnFunction("voice_in")]
        public static float VoiceIn(string scope, string kind) => Voices.TryParse(kind, out var v) ? Voices.Count(GameState.World, v, scope) : 0f;

        /// <summary>The voice Wren has used most: "surveyor", "warden" or "drift" (Pell's last list; the epilogue's last line).</summary>
        [YarnFunction("dominant_voice")]
        public static string DominantVoice() => Voices.Dominant(GameState.World).ToString().ToLowerInvariant();

        /// <summary>Whether an ending ("fixed", "open", "unwritten", "rest") is open to her now (Endings.IsOpen).</summary>
        [YarnFunction("ending_open")]
        public static bool EndingOpen(string name) => Endings.TryParse(name, out var e) && Endings.IsOpen(GameState.World, e);

        /// <summary>How many keystones Wren carries (Voss counts them at the Threshold).</summary>
        [YarnFunction("keystones")]
        public static float KeystoneCount() => Keystones.Count(GameState.World);

        /// <summary>The Guild's count has come in: she is missing, and missing carries no licence.</summary>
        [YarnFunction("unlicensed")]
        public static bool IsUnlicensed() => Licence.IsUnlicensed(GameState.World);

        /// <summary>A Cantor's bell has wiped the place and nobody has drawn it since.</summary>
        [YarnFunction("erased")]
        public static bool IsErased(string place) => Atlas.IsErased(GameState.World, place);

        /// <summary>At least one vantage of the place is on the page.</summary>
        [YarnFunction("drawn")]
        public static bool IsDrawn(string place) => Atlas.IsDrawn(GameState.World, place);

        [YarnFunction("commission_is")]
        public static bool CommissionIs(string id, string state) => CommissionStateOf(id) == (state ?? "").Trim().ToLowerInvariant();

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
            ApplyLocale();
            _runner.StartDialogue(node).Forget();
            return true;
        }

        /// <summary>
        /// The Yarn locale for the player's (PRG-19): the project's localisation for it if there is one, else the base
        /// language. Lines are fetched in it from the next line on.
        /// </summary>
        public static string DialogueLocaleFor(YarnProject? project, string locale)
        {
            if (project != null && !string.IsNullOrEmpty(locale) && project.localizations.ContainsKey(locale)) return locale;
            return Loc.Base;
        }

        public string DialogueLocale => DialogueLocaleFor(_project, Loc.Locale);

        void ApplyLocale()
        {
            if (_runner == null) return;
            if (_runner.LineProvider is LineProviderBehaviour provider)
            {
                var code = DialogueLocale;
                provider.LocaleCode = code;
                if (provider is BuiltinLocalisedLineProvider builtin) builtin.AssetLocaleCode = code;
            }
        }

        void OnLocaleChanged(string _) => ApplyLocale();
        void OnEnable() { Loc.Changed += OnLocaleChanged; }
        void OnDisable() { Loc.Changed -= OnLocaleChanged; }

        public void Stop()
        {
            if (_runner != null && _runner.IsDialogueRunning) _runner.Stop().Forget();
            DialogueViews.Current?.Clear();   // a stopped conversation leaves no page behind
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
