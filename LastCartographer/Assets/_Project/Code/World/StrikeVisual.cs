using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Placeholder feedback for the quill until CHR-03/ENV-12 art lands: an ink slash quad in the
    /// strike direction during the swing, and a burst of ink flecks on a landed hit.
    /// Pure geometry (opaque ink-black quads) so it needs no transparent shader setup.
    /// </summary>
    [RequireComponent(typeof(QuillStrike))]
    public sealed class StrikeVisual : MonoBehaviour
    {
        [SerializeField] Material _inkMaterial;
        [SerializeField] float _slashSeconds = 0.14f;
        [SerializeField] int _fleckCount = 7;
        [SerializeField] float _fleckSpeed = 9f;
        [SerializeField] float _fleckSeconds = 0.28f;

        QuillStrike _strike;
        Transform _slash;
        float _slashT = -1f;
        Vector2 _slashDir;
        readonly List<Fleck> _flecks = new List<Fleck>();
        readonly Stack<Transform> _pool = new Stack<Transform>();

        struct Fleck { public Transform T; public Vector2 Vel; public float Life; public float Size; }

        void Awake()
        {
            if (_inkMaterial == null)
            {
                // No serialized material (scene built before this component existed): make one.
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                _inkMaterial = new Material(shader) { name = "M_Ink_Black (runtime)" };
                var ink = new Color(0.06f, 0.06f, 0.08f);
                if (_inkMaterial.HasProperty("_BaseColor")) _inkMaterial.SetColor("_BaseColor", ink);
                if (_inkMaterial.HasProperty("_Color")) _inkMaterial.SetColor("_Color", ink);
            }
            _strike = GetComponent<QuillStrike>();
            _strike.Swung += OnSwung;
            _strike.Landed += OnLanded;
            _slash = MakeQuad("Slash");
            _slash.gameObject.SetActive(false);
        }

        Transform MakeQuad(string name)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Destroy(q.GetComponent<Collider>());
            var r = q.GetComponent<MeshRenderer>();
            if (_inkMaterial != null) r.sharedMaterial = _inkMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            q.transform.SetParent(transform, false);
            return q.transform;
        }

        void OnSwung(Vector2 dir)
        {
            _slashDir = dir;
            _slashT = 0f;
            _slash.gameObject.SetActive(true);
        }

        /// <summary>Flourishes: an ink burst at a point in a direction (use Vector2.zero for all round).</summary>
        public void Burst(Vector2 origin, Vector2 dir, int count, float speed = 9f)
        {
            for (int i = 0; i < count; i++)
            {
                var t = _pool.Count > 0 ? _pool.Pop() : MakeQuad("Fleck");
                t.gameObject.SetActive(true);
                t.position = new Vector3(origin.x, origin.y, -0.2f);
                float ang = dir.sqrMagnitude > 0f
                    ? Mathf.Atan2(dir.y, dir.x) + Random.Range(-0.9f, 0.9f)
                    : Random.Range(0f, Mathf.PI * 2f);
                var vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed * Random.Range(0.5f, 1.2f);
                _flecks.Add(new Fleck { T = t, Vel = vel, Life = _fleckSeconds, Size = Random.Range(0.1f, 0.26f) });
            }
        }

        /// <summary>Flourishes: show the slash in a direction (Longstroke, Crosshatch ticks).</summary>
        public void Slash(Vector2 dir) => OnSwung(dir);

        void OnLanded(IHittable target)
        {
            _strike.GetHitbox(out var center, out var size);
            var origin = center + _slashDir * (size.x > size.y ? size.x : size.y) * 0.35f;
            for (int i = 0; i < _fleckCount; i++)
            {
                var t = _pool.Count > 0 ? _pool.Pop() : MakeQuad("Fleck");
                t.gameObject.SetActive(true);
                t.position = new Vector3(origin.x, origin.y, -0.2f);
                float ang = Mathf.Atan2(_slashDir.y, _slashDir.x) + Random.Range(-1.1f, 1.1f);
                var vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * _fleckSpeed * Random.Range(0.5f, 1.2f);
                _flecks.Add(new Fleck { T = t, Vel = vel, Life = _fleckSeconds, Size = Random.Range(0.08f, 0.2f) });
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (_slashT >= 0f)
            {
                _slashT += dt;
                float k = Mathf.Clamp01(_slashT / _slashSeconds);
                _strike.GetHitbox(out var center, out var size);
                float len = Mathf.Max(size.x, size.y);
                float thick = Mathf.Lerp(0.05f, 0.32f, Mathf.Sin(k * Mathf.PI));
                _slash.position = new Vector3(center.x, center.y, -0.2f);
                _slash.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_slashDir.y, _slashDir.x) * Mathf.Rad2Deg);
                _slash.localScale = new Vector3(len * Mathf.Lerp(0.6f, 1f, k), thick, 1f);
                if (k >= 1f) { _slashT = -1f; _slash.gameObject.SetActive(false); }
            }

            for (int i = _flecks.Count - 1; i >= 0; i--)
            {
                var f = _flecks[i];
                f.Life -= dt;
                f.Vel += Vector2.down * 20f * dt;
                f.T.position += (Vector3)(f.Vel * dt);
                float s = f.Size * Mathf.Clamp01(f.Life / _fleckSeconds);
                f.T.localScale = new Vector3(s, s, 1f);
                if (f.Life <= 0f)
                {
                    f.T.gameObject.SetActive(false);
                    _pool.Push(f.T);
                    _flecks.RemoveAt(i);
                }
                else _flecks[i] = f;
            }
        }
    }
}
