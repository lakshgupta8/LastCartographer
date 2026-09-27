using System;
using OWSBG.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OWSBG.World
{
    /// <summary>
    /// Iris seeds on the ground (DES-05): the coast's money. Wren walking through takes them. Placed in rooms by
    /// the setup script, or spawned where an enemy died (IrisSeedDrops).
    /// </summary>
    public sealed class IrisSeed : MonoBehaviour
    {
        [SerializeField] int _count = 1;

        public int Count { get => _count; set => _count = value; }
        public static event Action<IrisSeed, int> Collected;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color Tint = new Color(0.93f, 0.76f, 0.34f);
        Vector3 _base;
        float _bob;
        bool _taken;

        /// <summary>Make one at runtime, in the scene of the thing that dropped it so it unloads with the room.</summary>
        public static IrisSeed Spawn(Vector2 position, int count, Scene? scene = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "IrisSeed";
            go.layer = LayerMask.NameToLayer("Trigger");
            DestroyImmediate(go.GetComponent<Collider>());   // now, not at frame end: a 2D collider cannot join a 3D one
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = Vector3.one * 0.35f;
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 1.6f;   // in local units of the 0.35 sphere: about half a unit in the world
            var seed = go.AddComponent<IrisSeed>();
            seed._count = Mathf.Max(1, count);
            if (scene.HasValue && scene.Value.IsValid()) SceneManager.MoveGameObjectToScene(go, scene.Value);
            return seed;
        }

        void Start()
        {
            _base = transform.position;
            _bob = UnityEngine.Random.value * 6f;
            var r = GetComponent<Renderer>();
            if (r != null)
            {
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, Tint);
                r.SetPropertyBlock(mpb);
            }
        }

        void Update()
        {
            _bob += Time.deltaTime;
            transform.position = _base + Vector3.up * (0.12f * Mathf.Sin(_bob * 3f));
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_taken) return;
            if (other.GetComponentInParent<WrenController>() == null) return;
            _taken = true;
            Economy.AddSeeds(GameState.World, _count);
            Collected?.Invoke(this, _count);
            Destroy(gameObject);
        }
    }
}
