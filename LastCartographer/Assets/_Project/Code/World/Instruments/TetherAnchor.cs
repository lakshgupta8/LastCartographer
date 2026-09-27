using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// A temporary Inkthread point (tether-hook). Inkthread itself is a later movement ability
    /// (CMB-04); until then this is a marker that the ability will look for by component.
    /// </summary>
    public sealed class TetherAnchor : MonoBehaviour
    {
        public float LifeLeft { get; private set; }

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
            LifeLeft -= Time.deltaTime;
            if (LifeLeft <= 0f) Destroy(gameObject);
            else transform.localScale = Vector3.one * (0.3f + 0.05f * Mathf.Sin(Time.time * 6f));
        }
    }
}
