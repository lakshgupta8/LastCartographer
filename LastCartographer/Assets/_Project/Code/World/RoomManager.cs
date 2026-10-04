using System;
using System.Collections;
using System.Collections.Generic;
using OWSBG.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace OWSBG.World
{
    /// <summary>
    /// Loads rooms additively, moves Wren to the target spawn, re-points the camera confiner, unloads the
    /// previous room. Lives in the persistent scene with Wren and the camera.
    ///
    /// Rooms stream through Addressables (PRG-07): each room scene is an addressable whose address is its
    /// scene name, packed in its own bundle. After a room loads, the bundles of its neighbours (the targets
    /// of its RoomTransitions) are kept resident so the next transition is a scene load, not a disk read;
    /// bundles that stop being adjacent are released. A scene that is not addressable falls back to the
    /// Build Settings path, which is what editor-only setups and tests rely on.
    /// </summary>
    public sealed class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [SerializeField] string _startRoom;
        [SerializeField] string _startSpawn = "Start";
        [SerializeField] string _prologueRoom;
        [SerializeField] string _prologueSpawn = "Start";
        [SerializeField] WrenController _wren;
        [SerializeField] CinemachineConfiner2D _confiner;
        [SerializeField] bool _useAddressables = true;
        [SerializeField] bool _preloadNeighbours = true;

        readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _scenes = new Dictionary<string, AsyncOperationHandle<SceneInstance>>();
        readonly Dictionary<string, AsyncOperationHandle> _preloads = new Dictionary<string, AsyncOperationHandle>();
        Coroutine _preloading;

        public string CurrentRoom { get; private set; }
        public bool IsTransitioning { get; private set; }
        public bool UsesAddressables => _useAddressables;
        /// <summary>How long the last room took, from the request to Wren standing in it (PRG-24's budget is 100 ms).</summary>
        public float LastTransitionMs { get; private set; }
        /// <summary>
        /// Where the last transition's time went: finding the room's bundle, loading its assets, activating the scene
        /// (its Awakes and OnEnables), placing Wren, and unloading the old room (with the new one's first frame, its
        /// Starts); and the managed bytes the whole of it allocated. For the probe's log (PRG-24).
        /// </summary>
        public string LastTransitionDetail { get; private set; }
        /// <summary>A room came in, and how long it took (ms).</summary>
        public static event Action<string, float> Transitioned;
        /// <summary>True when the room came in through Addressables (as opposed to Build Settings or adoption).</summary>
        public bool IsAddressable(string scene) => !string.IsNullOrEmpty(scene) && _scenes.ContainsKey(scene);
        /// <summary>A room the catalogue can load (a built scene), or one already loaded; asked synchronously, so only once Addressables has its locators.</summary>
        public bool IsBuilt(string scene)
        {
            if (string.IsNullOrEmpty(scene)) return false;
            if (_scenes.ContainsKey(scene)) return true;
            if (!_useAddressables) return false;
            foreach (var locator in Addressables.ResourceLocators)
                if (locator.Locate(scene, typeof(SceneInstance), out var locations) && locations.Count > 0) return true;
            return false;
        }
        /// <summary>Neighbour rooms whose bundles are being kept resident.</summary>
        public IReadOnlyCollection<string> PreloadedNeighbours => _preloads.Keys;
        public bool IsPreloading { get; private set; }
        public event Action<string> RoomChanged;

        /// <summary>
        /// Rooms made at runtime instead of loaded (the Blank's islands, the epilogue's stand-ins): each generator, given a
        /// scene name, returns the Room it built in a new scene, or null for a scene it does not own. Builders register
        /// themselves at load; they are asked in order, before Addressables.
        /// </summary>
        public static readonly List<Func<string, Room>> Generators = new List<Func<string, Room>>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            foreach (var h in _preloads.Values) if (h.IsValid()) Addressables.Release(h);
            _preloads.Clear();
            foreach (var kv in _scenes) if (kv.Value.IsValid()) Addressables.UnloadSceneAsync(kv.Value, true);
            _scenes.Clear();
        }

        IEnumerator Start()
        {
            // A room already open (editor multi-scene setup, or Play pressed inside a room): adopt it.
            var existing = FindFirstObjectByType<Room>();
            if (existing != null)
            {
                Adopt(existing, _startSpawn);
                yield break;
            }
            // -continue: the last desk's save, in the room she rested in (PRO-04's testers, after a crash).
            if (Bootstrap.Continue && GameState.Load(Bootstrap.ContinueSlot) && !string.IsNullOrEmpty(GameState.World.RespawnRoom))
            {
                Debug.Log("[OWSBG] continuing from the save in slot " + Bootstrap.ContinueSlot + ": " + GameState.World.RespawnRoom);
                yield return Load(GameState.World.RespawnRoom, GameState.World.RespawnSpawn, null);
                yield break;
            }
            bool prologue = !string.IsNullOrEmpty(_prologueRoom) && !Bootstrap.SkipPrologue && !GameState.World.Is("prologue.woke_on_shore");
            var first = prologue ? _prologueRoom : _startRoom;
            if (!string.IsNullOrEmpty(first))
                yield return Load(first, prologue ? _prologueSpawn : _startSpawn, null);
        }

        void Adopt(Room room, string spawn)
        {
            var scene = room.gameObject.scene;
            if (scene.IsValid()) SceneManager.SetActiveScene(scene);
            Place(room, spawn);
            CurrentRoom = scene.name;
            RoomChanged?.Invoke(CurrentRoom);
            if (_preloadNeighbours) _preloading = StartCoroutine(PreloadNeighbours(room));
        }

        void Place(Room room, string spawn)
        {
            if (_wren != null && room != null)
            {
                var sp = room.FindSpawn(spawn);
                if (sp != null) _wren.Teleport(sp.position);
            }
            if (_confiner != null && room != null)
            {
                _confiner.BoundingShape2D = room.CameraBounds;
                _confiner.InvalidateBoundingShapeCache();
            }
        }

        public void Transition(string scene, string spawn)
        {
            if (IsTransitioning || string.IsNullOrEmpty(scene) || scene == CurrentRoom) return;
            StartCoroutine(Load(scene, spawn, CurrentRoom));
        }

        IEnumerator Load(string scene, string spawn, string unload)
        {
            IsTransitioning = true;
            float started = Time.realtimeSinceStartup;
            long allocatedAtStart = AllocatedBytes();
            float tLocate = 0f, tAssets = 0f, tActivate = 0f, tPlace = 0f;
            if (_preloading != null) { StopCoroutine(_preloading); _preloading = null; IsPreloading = false; }

            Scene loaded = default;
            bool ok = false;
            foreach (var generate in Generators)
            {
                var generated = generate(scene);
                if (generated == null) continue;
                loaded = generated.gameObject.scene;
                ok = loaded.IsValid();
                break;
            }
            if (!ok && _useAddressables)
            {
                var locations = Addressables.LoadResourceLocationsAsync(scene, typeof(SceneInstance));
                yield return locations;
                bool addressable = locations.Status == AsyncOperationStatus.Succeeded && locations.Result.Count > 0;
                Addressables.Release(locations);
                tLocate = Time.realtimeSinceStartup;
                if (addressable)
                {
                    // Loaded without activating, so the assets' load and the scene's activation (its Awakes) are timed apart.
                    var handle = Addressables.LoadSceneAsync(scene, LoadSceneMode.Additive, SceneReleaseMode.ReleaseSceneWhenSceneUnloaded, activateOnLoad: false);
                    yield return handle;
                    tAssets = Time.realtimeSinceStartup;
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        yield return handle.Result.ActivateAsync();
                        _scenes[scene] = handle;
                        loaded = handle.Result.Scene;
                        ok = true;
                    }
                    else Debug.LogError("[OWSBG] Addressables could not load room " + scene + ": " + handle.OperationException);
                }
            }
            if (!ok)
            {
                var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                if (op == null)
                {
                    Debug.LogError("[OWSBG] Room scene is neither addressable nor in build settings: " + scene);
                    IsTransitioning = false;
                    yield break;
                }
                yield return op;
                loaded = SceneManager.GetSceneByName(scene);
            }
            tActivate = Time.realtimeSinceStartup;

            if (loaded.IsValid()) SceneManager.SetActiveScene(loaded);
            Room room = null;
            if (loaded.IsValid())
            {
                foreach (var root in loaded.GetRootGameObjects())
                {
                    room = root.GetComponentInChildren<Room>(true);
                    if (room != null) break;
                }
            }
            Place(room, spawn);
            tPlace = Time.realtimeSinceStartup;

            if (!string.IsNullOrEmpty(unload)) yield return Unload(unload);

            CurrentRoom = scene;
            IsTransitioning = false;
            float ended = Time.realtimeSinceStartup;
            LastTransitionMs = (ended - started) * 1000f;
            long allocatedAtEnd = AllocatedBytes();
            if (tLocate <= 0f) tLocate = started;
            if (tAssets <= 0f) tAssets = tLocate;
            LastTransitionDetail = "locate " + ((tLocate - started) * 1000f).ToString("0") + " ms, assets " + ((tAssets - tLocate) * 1000f).ToString("0") +
                                   " ms, activate " + ((tActivate - tAssets) * 1000f).ToString("0") + " ms, place " + ((tPlace - tActivate) * 1000f).ToString("0") +
                                   " ms, unload and first frame " + ((ended - tPlace) * 1000f).ToString("0") + " ms" +
                                   (allocatedAtStart > 0 && allocatedAtEnd > allocatedAtStart ? ", " + ((allocatedAtEnd - allocatedAtStart) / 1024f).ToString("0") + " KB allocated" : "");
            RoomChanged?.Invoke(scene);
            Transitioned?.Invoke(scene, LastTransitionMs);
            if (_preloadNeighbours) _preloading = StartCoroutine(PreloadNeighbours(room));
        }

        static long AllocatedBytes()
        {
            try { return System.GC.GetAllocatedBytesForCurrentThread(); }
            catch { return -1; }
        }

        IEnumerator Unload(string scene)
        {
            if (_scenes.TryGetValue(scene, out var handle))
            {
                _scenes.Remove(scene);
                if (handle.IsValid()) yield return Addressables.UnloadSceneAsync(handle, true);
                else if (SceneManager.GetSceneByName(scene).isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            }
            else if (SceneManager.GetSceneByName(scene).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        /// <summary>Keep the bundles of the rooms reachable from this one resident; drop the rest.</summary>
        IEnumerator PreloadNeighbours(Room room)
        {
            IsPreloading = true;
            var wanted = new HashSet<string>();
            if (room != null)
                foreach (var t in room.GetComponentsInChildren<RoomTransition>(true))
                    if (!string.IsNullOrEmpty(t.TargetScene)) wanted.Add(t.TargetScene);

            foreach (var key in new List<string>(_preloads.Keys))
            {
                if (wanted.Contains(key)) continue;
                var h = _preloads[key];
                _preloads.Remove(key);
                if (h.IsValid()) Addressables.Release(h);
            }

            if (_useAddressables)
            {
                foreach (var key in wanted)
                {
                    if (_preloads.ContainsKey(key) || _scenes.ContainsKey(key)) continue;
                    var locations = Addressables.LoadResourceLocationsAsync(key, typeof(SceneInstance));
                    yield return locations;
                    if (locations.Status != AsyncOperationStatus.Succeeded || locations.Result.Count == 0)
                    {
                        Addressables.Release(locations);
                        continue;
                    }
                    var handle = Addressables.DownloadDependenciesAsync(locations.Result, false);
                    Addressables.Release(locations);
                    _preloads[key] = handle;
                    yield return handle;
                }
            }
            IsPreloading = false;
            _preloading = null;
        }
    }
}
