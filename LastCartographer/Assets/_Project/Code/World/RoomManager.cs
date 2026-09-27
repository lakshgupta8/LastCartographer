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
        /// <summary>True when the room came in through Addressables (as opposed to Build Settings or adoption).</summary>
        public bool IsAddressable(string scene) => !string.IsNullOrEmpty(scene) && _scenes.ContainsKey(scene);
        /// <summary>Neighbour rooms whose bundles are being kept resident.</summary>
        public IReadOnlyCollection<string> PreloadedNeighbours => _preloads.Keys;
        public bool IsPreloading { get; private set; }
        public event Action<string> RoomChanged;

        /// <summary>
        /// Rooms made at runtime instead of loaded (the Blank's islands, PRG-20): given a scene name, returns the Room it
        /// built in a new scene, or null for a scene it does not own. Registered by the builder; asked before Addressables.
        /// </summary>
        public static Func<string, Room> Generator;

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
            if (_preloading != null) { StopCoroutine(_preloading); _preloading = null; IsPreloading = false; }

            Scene loaded = default;
            bool ok = false;
            var generated = Generator?.Invoke(scene);
            if (generated != null)
            {
                loaded = generated.gameObject.scene;
                ok = loaded.IsValid();
            }
            if (!ok && _useAddressables)
            {
                var locations = Addressables.LoadResourceLocationsAsync(scene, typeof(SceneInstance));
                yield return locations;
                bool addressable = locations.Status == AsyncOperationStatus.Succeeded && locations.Result.Count > 0;
                Addressables.Release(locations);
                if (addressable)
                {
                    var handle = Addressables.LoadSceneAsync(scene, LoadSceneMode.Additive, SceneReleaseMode.ReleaseSceneWhenSceneUnloaded);
                    yield return handle;
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
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

            if (!string.IsNullOrEmpty(unload)) yield return Unload(unload);

            CurrentRoom = scene;
            IsTransitioning = false;
            RoomChanged?.Invoke(scene);
            if (_preloadNeighbours) _preloading = StartCoroutine(PreloadNeighbours(room));
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
