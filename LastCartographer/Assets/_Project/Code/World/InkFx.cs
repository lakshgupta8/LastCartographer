using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Ink is the VFX language (art-direction 7, ENV-12): one-shot sheet clips of ink doing what it does when the
    /// game hits, swings, scribbles, binds, erases and breaks. Lives in the persistent scene with the clips the
    /// setup packed from tools/characters/fx.py; anything in the world asks for one by name at a point, with an
    /// angle for the strike direction, and it plays on a pooled quad just in front of the play plane, then goes.
    /// A clip the sheets lack is a no-op (Spawn returns null), so placeholder geometry can stand in where no
    /// effects are loaded. A <see cref="Mark"/> is a looping clip the caller owns and destroys (Halvard's squares).
    /// </summary>
    public sealed class InkFx : MonoBehaviour
    {
        public const float Depth = -0.3f;

        [SerializeField] List<SheetClip> _clips = new List<SheetClip>();
        [SerializeField] Material _material;
        [SerializeField] float _cellUnits = 3f;

        public static InkFx Instance { get; private set; }
        public static bool Ready => Instance != null && Instance._clips.Count > 0;
        public IReadOnlyList<SheetClip> Clips => _clips;
        /// <summary>Every effect renders into the same cell (fx.py's CELL); the manifest says how big.</summary>
        public float CellUnits { get => _cellUnits; set => _cellUnits = value; }
        /// <summary>Effects spawned since this loaded (tests read it).</summary>
        public int Spawned { get; private set; }
        public int Live => _live.Count;

        readonly Stack<Life> _pool = new Stack<Life>();
        readonly List<Life> _live = new List<Life>();

        /// <summary>Editor setup: the clips and the shared ink material.</summary>
        public void Configure(IEnumerable<SheetClip> clips, Material material, float cellUnits)
        {
            _clips = new List<SheetClip>(clips);
            _material = material;
            _cellUnits = cellUnits;
        }

        public bool Has(string clip)
        {
            foreach (var c in _clips) if (c.Name == clip) return true;
            return false;
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>
        /// Play a one-shot at a point: <paramref name="angle"/> in degrees turns +X to the strike direction,
        /// <paramref name="scale"/> multiplies the cell, <paramref name="follow"/> keeps it on a moving thing
        /// (the Bind redraw around Wren). Returns the quad, or null when the clip is not loaded.
        /// </summary>
        public static Transform Spawn(string clip, Vector2 at, float angle = 0f, float scale = 1f, Transform follow = null)
        {
            var fx = Instance;
            if (fx == null || !fx.Has(clip)) return null;
            var life = fx._pool.Count > 0 ? fx._pool.Pop() : fx.Make();
            life.Follow = follow;
            life.Offset = follow != null ? at - (Vector2)follow.position : Vector2.zero;
            fx.Place(life, clip, at, angle, scale);
            fx._live.Add(life);
            return life.transform;
        }

        /// <summary>A lasting mark (a survey square on the floor): loops until the caller destroys the object.</summary>
        public static Transform Mark(string clip, Vector2 at, float scale = 1f)
        {
            var fx = Instance;
            if (fx == null || !fx.Has(clip)) return null;
            var life = fx.Make();
            life.name = "InkMark";
            life.transform.SetParent(null, false);
            fx.Place(life, clip, at, 0f, scale);
            return life.transform;
        }

        void Place(Life life, string clip, Vector2 at, float angle, float scale)
        {
            var t = life.transform;
            t.position = new Vector3(at.x, at.y, Depth);
            t.rotation = Quaternion.Euler(0f, 0f, angle);
            t.localScale = new Vector3(_cellUnits * scale, _cellUnits * scale, 1f);
            life.gameObject.SetActive(true);
            life.Player.Play(clip, true);
            Spawned++;
        }

        Life Make()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "InkFx";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = _material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var player = go.AddComponent<InkSheetPlayer>();
            player.Configure(r, _clips);
            var life = go.AddComponent<Life>();
            life.Player = player;
            return life;
        }

        void LateUpdate()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var l = _live[i];
                if (l == null) { _live.RemoveAt(i); continue; }
                if (l.Follow != null)
                    l.transform.position = new Vector3(l.Follow.position.x + l.Offset.x, l.Follow.position.y + l.Offset.y, Depth);
                if (!l.Player.Finished) continue;
                l.gameObject.SetActive(false);
                l.Follow = null;
                _live.RemoveAt(i);
                _pool.Push(l);
            }
        }

        /// <summary>One live effect: its player and what it follows.</summary>
        public sealed class Life : MonoBehaviour
        {
            public InkSheetPlayer Player;
            public Transform Follow;
            public Vector2 Offset;
        }
    }
}
