using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// An Inkthread point: a temporary one thrown as a tether-hook, which fades, or a permanent one the kit places
    /// (ENV-04: the anchor-points in the Verdance's branches and the Gatekeeper's roots), which stays. The thread
    /// (WrenController.TryThread) finds either by component.
    /// </summary>
    public sealed class TetherAnchor : MonoBehaviour
    {
        [SerializeField] bool _permanent;

        public float LifeLeft { get; private set; }
        /// <summary>A placed anchor-point: never fades, never pulses (its drawing is the kit's).</summary>
        public bool Permanent { get => _permanent; set => _permanent = value; }

        public static TetherAnchor Spawn(Vector2 at, float seconds)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "TetherAnchor";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(at.x, at.y, 0f);
            go.transform.localScale = Vector3.one * 0.3f;
            go.GetComponent<MeshRenderer>().sharedMaterial = InkMaterials.Dark;
            var a = go.AddComponent<TetherAnchor>();
            a.LifeLeft = seconds;
            return a;
        }

        void Update()
        {
            if (_permanent) return;
            LifeLeft -= Time.deltaTime;
            if (LifeLeft <= 0f) Destroy(gameObject);
            else transform.localScale = Vector3.one * (0.3f + 0.05f * Mathf.Sin(Time.time * 6f));
        }
    }
}
