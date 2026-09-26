using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OWSBG.World
{
    /// <summary>
    /// Loads rooms additively, moves Wren to the target spawn, re-points the camera confiner,
    /// unloads the previous room. Lives in the persistent scene with Wren and the camera.
    /// Addressables-backed streaming (PRG-07) replaces SceneManager here later; the API stays.
    /// </summary>
    public sealed class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [SerializeField] string _startRoom;
        [SerializeField] string _startSpawn = "Start";
        [SerializeField] WrenController _wren;
        [SerializeField] CinemachineConfiner2D _confiner;

        public string CurrentRoom { get; private set; }
        public bool IsTransitioning { get; private set; }
        public event Action<string> RoomChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            // A room already open (editor multi-scene setup, or Play pressed inside a room): adopt it.
            var existing = FindFirstObjectByType<Room>();
            if (existing != null)
            {
                Adopt(existing, _startSpawn);
                yield break;
            }
            if (!string.IsNullOrEmpty(_startRoom))
                yield return Load(_startRoom, _startSpawn, null);
        }

        void Adopt(Room room, string spawn)
        {
            var scene = room.gameObject.scene;
            if (scene.IsValid()) SceneManager.SetActiveScene(scene);
            if (_wren != null)
            {
                var sp = room.FindSpawn(spawn);
                if (sp != null) _wren.Teleport(sp.position);
            }
            if (_confiner != null)
            {
                _confiner.BoundingShape2D = room.CameraBounds;
                _confiner.InvalidateBoundingShapeCache();
            }
            CurrentRoom = scene.name;
            RoomChanged?.Invoke(CurrentRoom);
        }

        public void Transition(string scene, string spawn)
        {
            if (IsTransitioning || string.IsNullOrEmpty(scene) || scene == CurrentRoom) return;
            StartCoroutine(Load(scene, spawn, CurrentRoom));
        }

        IEnumerator Load(string scene, string spawn, string unload)
        {
            IsTransitioning = true;
            var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError("[OWSBG] Room scene not in build settings: " + scene);
                IsTransitioning = false;
                yield break;
            }
            yield return op;

            var loaded = SceneManager.GetSceneByName(scene);
            SceneManager.SetActiveScene(loaded);
            Room room = null;
            foreach (var root in loaded.GetRootGameObjects())
            {
                room = root.GetComponentInChildren<Room>(true);
                if (room != null) break;
            }

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

            if (!string.IsNullOrEmpty(unload) && SceneManager.GetSceneByName(unload).isLoaded)
                yield return SceneManager.UnloadSceneAsync(unload);

            CurrentRoom = scene;
            IsTransitioning = false;
            RoomChanged?.Invoke(scene);
        }
    }
}
