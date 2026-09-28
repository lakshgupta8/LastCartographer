using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Rising air (bible 4.5, 6.10): a column that carries Wren up while she has Windmemory's wings, and does nothing for
    /// her without them. The Fallen Star's heat makes these beside its iron walls; the Steppe's ink-swirls will too.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Updraft : MonoBehaviour
    {
        public float speed = 9f;
        public int Lifts { get; private set; }

        BoxCollider2D _box;
        readonly Collider2D[] _hits = new Collider2D[4];

        void Awake()
        {
            _box = GetComponent<BoxCollider2D>();
            _box.isTrigger = true;
        }

        void FixedUpdate()
        {
            var b = _box.bounds;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.Player, useTriggers = false };
            int n = Physics2D.OverlapBox(b.center, b.size, 0f, filter, _hits);
            for (int i = 0; i < n; i++)
            {
                var ctrl = _hits[i].GetComponentInParent<WrenController>();
                if (ctrl == null) continue;
                if (ctrl.Abilities == null || !ctrl.Abilities.Has(Ability.Windmemory)) return;
                ctrl.Lift(speed);
                Lifts++;
                return;
            }
        }

        public static Updraft Make(string name, Transform parent, Vector2 centre, Vector2 size, float speed)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer("Trigger") };
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(centre.x, centre.y, 0f);
            go.AddComponent<BoxCollider2D>().size = size;
            var u = go.AddComponent<Updraft>();
            u.speed = speed;
            var v = GameObject.CreatePrimitive(PrimitiveType.Quad);
            v.name = "Swirl";
            Destroy(v.GetComponent<Collider>());
            v.transform.SetParent(go.transform, false);
            v.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            v.transform.localScale = new Vector3(size.x * 0.6f, size.y, 1f);
            v.GetComponent<MeshRenderer>().sharedMaterial = InkMaterials.Lit("Updraft", new Color(0.96f, 0.72f, 0.46f));
            return u;
        }
    }
}
