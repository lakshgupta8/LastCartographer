using System;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A piece of a boss fight the quill can strike that is not the boss's body: a dove of the Choir, a block of rubble,
    /// an ink surge, a stone feather. It hands every hit to its owner, who says whether it landed (and so whether the
    /// strike pogoes or refills ink). A trigger on the Enemy layer with a kinematic body, so it can be moved each frame.
    /// </summary>
    public sealed class BossPart : MonoBehaviour, IHittable
    {
        /// <summary>The owner's rule: return true when the hit lands.</summary>
        public Func<HitInfo, bool> OnHit;
        public BoxCollider2D Box { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public Transform Visual { get; private set; }
        public Vector2 Position => transform.position;

        public bool TakeHit(in HitInfo hit) => OnHit != null && OnHit(hit);

        public void MoveTo(Vector2 p)
        {
            transform.position = new Vector3(p.x, p.y, 0f);
            Body.position = p;
        }

        public void SetMaterial(Material mat)
        {
            if (Visual != null) Visual.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        public static BossPart Make(string name, Transform parent, Vector2 at, Vector2 size, Material mat)
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
            var part = go.AddComponent<BossPart>();
            part.Box = box;
            part.Body = rb;
            var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
            q.name = "Visual";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(go.transform, false);
            q.transform.localScale = new Vector3(size.x, size.y, 0.3f);
            q.GetComponent<MeshRenderer>().sharedMaterial = mat != null ? mat : InkMaterials.Dark;
            part.Visual = q.transform;
            return part;
        }

        /// <summary>A prop with no hit rule: a lamp, a floor section, a dust cloud.</summary>
        public static Transform Prop(string name, Transform parent, Vector2 at, Vector2 size, Material mat, float z = 0.3f)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
            q.name = name;
            Destroy(q.GetComponent<Collider>());
            if (parent != null) q.transform.SetParent(parent, false);
            q.transform.position = new Vector3(at.x, at.y, z);
            q.transform.localScale = new Vector3(size.x, size.y, 0.2f);
            q.GetComponent<MeshRenderer>().sharedMaterial = mat != null ? mat : InkMaterials.Dark;
            return q.transform;
        }
    }
}
