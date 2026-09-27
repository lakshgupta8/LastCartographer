using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A thrown compass needle: flies straight, bends gently toward the nearest enemy ahead, deals 1
    /// damage and marks the target (Inkthread anchor, orange pulse). Dies on ground or after a while.
    /// Pure code and a tiny stretched cube, no prefab.
    /// </summary>
    public sealed class CompassDart : MonoBehaviour
    {
        public float homingTurn = 4f;        // radians per second
        public float homingRange = 7f;
        public float lifeSeconds = 1.6f;
        public float markSeconds = 8f;
        public LayerMask hitMask, groundMask;

        Vector2 _vel;
        float _t;
        GameObject _owner;
        readonly Collider2D[] _hits = new Collider2D[8];

        public static CompassDart Spawn(Vector2 origin, Vector2 velocity, float markSeconds, LayerMask hitMask, LayerMask groundMask, GameObject owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "CompassDart";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(origin.x, origin.y, 0f);
            go.transform.localScale = new Vector3(0.5f, 0.08f, 0.08f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = InkMaterials.Dark;
            var d = go.AddComponent<CompassDart>();
            d._vel = velocity;
            d.markSeconds = markSeconds;
            d.hitMask = hitMask;
            d.groundMask = groundMask;
            d._owner = owner;
            return d;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _t += dt;
            if (_t > lifeSeconds) { Destroy(gameObject); return; }

            // Gentle homing toward the nearest enemy roughly ahead.
            var pos = (Vector2)transform.position;
            Enemy best = null; float bestD = homingRange * homingRange;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = hitMask, useTriggers = true };
            int n = Physics2D.OverlapCircle(pos, homingRange, filter, _hits);
            for (int i = 0; i < n; i++)
            {
                var e = _hits[i].GetComponentInParent<Enemy>();
                if (e == null || e.IsDead) continue;
                var to = (Vector2)e.transform.position - pos;
                if (Vector2.Dot(to, _vel) < 0f) continue;
                if (to.sqrMagnitude < bestD) { bestD = to.sqrMagnitude; best = e; }
            }
            if (best != null)
            {
                float speed = _vel.magnitude;
                var want = ((Vector2)best.transform.position - pos).normalized;
                var dir = Vector3.RotateTowards(_vel.normalized, want, homingTurn * dt, 0f);
                _vel = (Vector2)dir * speed;
            }

            var step = _vel * dt;
            var hit = Physics2D.Raycast(pos, step.normalized, step.magnitude + 0.1f, hitMask | groundMask);
            if (hit.collider != null)
            {
                var h = hit.collider.GetComponentInParent<IHittable>();
                if (h != null)
                {
                    var info = new HitInfo { Damage = 1, Direction = _vel.normalized, Source = _owner, Knockback = 0.3f };
                    h.TakeHit(info);
                    if (h is Enemy e) e.Mark(markSeconds);
                }
                Destroy(gameObject);
                return;
            }
            transform.position += (Vector3)step;
            transform.rotation = Quaternion.FromToRotation(Vector3.right, _vel.normalized);
        }
    }
}
