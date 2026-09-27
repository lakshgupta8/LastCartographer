using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A heavy lob on a ballistic arc: 3 damage to whatever it lands on, and it breaks
    /// <see cref="WeakFloor"/>s. One hit, then it drops out of the world.
    /// </summary>
    public sealed class PlumbWeight : MonoBehaviour
    {
        public float gravity = 30f;
        public float lifeSeconds = 3f;
        public int damage = 3;
        public LayerMask hitMask, groundMask;

        Vector2 _vel;
        float _t;
        GameObject _owner;

        public static PlumbWeight Spawn(Vector2 origin, Vector2 velocity, int damage, LayerMask hitMask, LayerMask groundMask, GameObject owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PlumbWeight";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(origin.x, origin.y, 0f);
            go.transform.localScale = Vector3.one * 0.35f;
            go.GetComponent<MeshRenderer>().sharedMaterial = InkMaterials.Dark;
            var p = go.AddComponent<PlumbWeight>();
            p._vel = velocity;
            p.damage = damage;
            p.hitMask = hitMask;
            p.groundMask = groundMask;
            p._owner = owner;
            return p;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _t += dt;
            if (_t > lifeSeconds) { Destroy(gameObject); return; }
            _vel.y -= gravity * dt;
            var pos = (Vector2)transform.position;
            var step = _vel * dt;
            var hit = Physics2D.CircleCast(pos, 0.17f, step.normalized, step.magnitude, hitMask | groundMask);
            if (hit.collider != null)
            {
                var h = hit.collider.GetComponentInParent<IHittable>();
                if (h != null)
                    h.TakeHit(new HitInfo { Damage = damage, Direction = _vel.normalized, Source = _owner, Knockback = 1.5f });
                Destroy(gameObject);
                return;
            }
            transform.position += (Vector3)step;
        }
    }
}
