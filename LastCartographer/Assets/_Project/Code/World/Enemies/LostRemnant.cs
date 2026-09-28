using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A lost Remnant (DES-11's rooms in the Greyfold and the Blank): one of the grey who has forgotten who it was and
    /// holds to her colour. It drifts to her through the white when she comes near and clings; its touch takes a mask and
    /// three seconds of Clarity with it (combat doc §3, PRG-18). Most of the grey are not hostile; these are the few.
    /// Greybox behaviour; its family's kit is CMB's.
    /// </summary>
    public sealed class LostRemnant : Enemy
    {
        [SerializeField] float _driftSpeed = 1.8f;
        [SerializeField] float _notices = 7f;

        public float ClarityPerTouch { get; set; } = Clarity.RemnantHitSeconds;
        protected override float ClarityDrain => ClarityPerTouch;

        protected override void Awake()
        {
            base.Awake();
            Body.gravityScale = 0f;
        }

        protected override void Tick(float dt)
        {
            if (Wren == null) { Body.linearVelocity = Vector2.zero; return; }
            var to = Wren.Position + Vector2.up * 0.6f - (Vector2)transform.position;
            if (to.magnitude > _notices) { Body.linearVelocity *= 0.8f; return; }
            Face(to.x >= 0f ? 1 : -1);
            Body.linearVelocity = to.normalized * _driftSpeed;
        }

        protected override Color TintColor() => new Color(0.66f, 0.66f, 0.64f);

        /// <summary>A lost Remnant at a point, greybox: a pale figure on the Enemy layer.</summary>
        public static LostRemnant Spawn(Transform parent, Vector2 at)
        {
            var go = new GameObject("LostRemnant") { layer = LayerMask.NameToLayer("Enemy") };
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(at.x, at.y, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.2f);
            go.AddComponent<Rigidbody2D>();
            var v = GameObject.CreatePrimitive(PrimitiveType.Cube);
            v.name = "Visual";
            Object.Destroy(v.GetComponent<Collider>());
            v.transform.SetParent(go.transform, false);
            v.transform.localScale = new Vector3(0.8f, 1.2f, 0.6f);
            v.GetComponent<MeshRenderer>().sharedMaterial = InkMaterials.Lit("LostRemnant", new Color(0.66f, 0.66f, 0.64f));
            return go.AddComponent<LostRemnant>();
        }
    }
}
