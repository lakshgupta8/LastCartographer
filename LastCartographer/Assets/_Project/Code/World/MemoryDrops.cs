using System;
using OWSBG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OWSBG.World
{
    /// <summary>
    /// Death and the smudge (GDD 6, PRG-17 second half). Lives in the persistent scene. When Wren dies with
    /// memories bound, they go into the world's drop at that spot and a <see cref="MemorySmudge"/> stands there;
    /// the smudge is rebuilt whenever its room is entered (or a save with a drop is loaded) and dissolves with
    /// the room. Striking it down recovers the memories. Dying again folds the old drop into the new one.
    /// </summary>
    public sealed class MemoryDrops : MonoBehaviour
    {
        [SerializeField] int _smudgeHealth = 3;

        public static MemoryDrops Instance { get; private set; }
        /// <summary>The live smudge, if its room is loaded.</summary>
        public static MemorySmudge Current { get; private set; }
        public static event Action<MemorySmudge> Spawned;

        WrenController _wren;
        WrenVitals _vitals;

        void Awake() { Instance = this; }

        void OnEnable()
        {
            GameState.Loaded += OnLoaded;
            if (RoomManager.Instance != null) RoomManager.Instance.RoomChanged += OnRoom;
        }

        void Start()
        {
            Bind();
            if (RoomManager.Instance != null) { RoomManager.Instance.RoomChanged -= OnRoom; RoomManager.Instance.RoomChanged += OnRoom; }
            SpawnIfHere();
        }

        void OnDisable()
        {
            GameState.Loaded -= OnLoaded;
            if (RoomManager.Instance != null) RoomManager.Instance.RoomChanged -= OnRoom;
            if (_vitals != null) _vitals.Died -= OnDied;
            if (Instance == this) Instance = null;
        }

        void Bind()
        {
            if (_vitals != null) return;
            _wren = FindFirstObjectByType<WrenController>();
            _vitals = _wren != null ? _wren.GetComponent<WrenVitals>() : null;
            if (_vitals != null) _vitals.Died += OnDied;
        }

        static string CurrentRoomId()
        {
            if (RoomManager.Instance != null && !string.IsNullOrEmpty(RoomManager.Instance.CurrentRoom)) return RoomManager.Instance.CurrentRoom;
            return Room.Current != null ? Room.Current.RoomId : SceneManager.GetActiveScene().name;
        }

        void OnDied()
        {
            Bind();
            if (_wren == null) return;
            var w = GameState.World;
            var pos = _wren.Position;
            int before = w.BoundMemories.Count;
            if (!Memories.Drop(w, CurrentRoomId(), pos.x, pos.y)) return;
            if (before > 0)
                Captions.Show(Loc.T("caption.memories_dropped", "Your memories smudge where you fell. Strike the smudge down to take them back."), 4.5f);
            Spawn();
        }

        void OnRoom(string room) { SpawnIfHere(); }
        void OnLoaded() { SpawnIfHere(); }

        void SpawnIfHere()
        {
            var w = GameState.World;
            if (!Memories.HasDrop(w)) { if (Current != null) Destroy(Current.gameObject); return; }
            if (w.DropRoom != CurrentRoomId()) return;
            if (Current != null && !Current.IsDead) return;
            Spawn();
        }

        /// <summary>Builds the smudge at the world's drop, in the current room's scene.</summary>
        public MemorySmudge Spawn()
        {
            var w = GameState.World;
            if (Current != null) Destroy(Current.gameObject);
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "MemorySmudge";
            go.layer = LayerMask.NameToLayer("Enemy");
            DestroyImmediate(go.GetComponent<Collider>());   // a 2D collider cannot join a 3D one pending destroy
            go.transform.position = new Vector3(w.DropX, w.DropY + 0.6f, 0f);
            go.transform.localScale = Vector3.one * 0.9f;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            go.AddComponent<Rigidbody2D>();
            var smudge = go.AddComponent<MemorySmudge>();
            smudge.SetMaxHealth(_smudgeHealth);
            smudge.Recovered += OnRecovered;
            var scene = Room.Current != null ? Room.Current.gameObject.scene : (Scene?)null;
            if (scene.HasValue && scene.Value.IsValid() && scene.Value.isLoaded) SceneManager.MoveGameObjectToScene(go, scene.Value);
            Current = smudge;
            Spawned?.Invoke(smudge);
            return smudge;
        }

        void OnRecovered(MemorySmudge s)
        {
            if (s != Current) return;
            var w = GameState.World;
            var names = Memories.Describe(w.DroppedMemories);
            int n = Memories.Recover(w);
            Current = null;
            if (n > 0) Captions.Show(Loc.F("caption.memories_recovered", "Recovered: {0}.", names), 4f);
        }
    }
}
