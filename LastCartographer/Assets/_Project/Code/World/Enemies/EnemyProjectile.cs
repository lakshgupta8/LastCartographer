using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Something thrown at Wren (Hale's flicked ink, and later the roster's spitters): it flies straight, costs a mask
    /// on contact and breaks on the ground or when its time is up. The quill goes through it, unless she carries the
    /// Unwriter's Charter: then any strike that meets it unwrites it (combat doc 5).
    /// </summary>
    public sealed class EnemyProjectile : MonoBehaviour, IHittable
    {
        public Vector2 Velocity;
        public int Damage = 1;
        public float LifeLeft = 4f;
        public LayerMask GroundMask;

        public bool IsSpent { get; private set; }
        /// <summary>Every projectile a strike has unwritten.</summary>
        public static event Action<EnemyProjectile> Erased;

        Rigidbody2D _body;
        BoxCollider2D _box;
        readonly Collider2D[] _hits = new Collider2D[4];

        public static EnemyProjectile Spawn(string name, Transform parent, Vector2 at, Vector2 velocity, Vector2 size, int damage, Material mat)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Enemy") };
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            var p = go.AddComponent<EnemyProjectile>();
            p.Velocity = velocity;
            p.Damage = damage;
            p.GroundMask = LayerMask.GetMask("Ground");
            var v = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            v.name = "Visual";
            Destroy(v.GetComponent<Collider>());
            v.transform.SetParent(go.transform, false);
            v.transform.localScale = new Vector3(size.x, size.y, 0.3f);
            v.GetComponent<MeshRenderer>().sharedMaterial = mat != null ? mat : InkMaterials.Dark;
            return p;
        }

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _box = GetComponent<BoxCollider2D>();
        }

        /// <summary>Only the Unwriter's Charter meets it: its strikes unwrite what is thrown.</summary>
        public bool TakeHit(in HitInfo hit)
        {
            if (IsSpent || hit.Source == null) return false;
            var charters = hit.Source.GetComponent<CharterSet>();
            if (charters == null || charters.Current == null || !charters.Current.ErasesProjectiles) return false;
            Erased?.Invoke(this);
            Spend();
            return true;
        }

        void FixedUpdate()
        {
            if (IsSpent) return;
            float dt = Time.fixedDeltaTime;
            LifeLeft -= dt;
            var p = _body.position + Velocity * dt;
            _body.MovePosition(p);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            var size = _box.size;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask("Player"), useTriggers = false };
            int n = Physics2D.OverlapBox(p, size, 0f, filter, _hits);
            for (int i = 0; i < n; i++)
            {
                var vitals = _hits[i].GetComponentInParent<WrenVitals>();
                if (vitals == null) continue;
                vitals.Damage(Damage, p - Velocity.normalized);
                Spend();
                return;
            }
            if (LifeLeft <= 0f || Physics2D.OverlapBox(p, size * 0.5f, 0f, GroundMask) != null) Spend();
        }

        void Spend()
        {
            IsSpent = true;
            Destroy(gameObject);
        }
    }
}
