using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Isolde fighting beside Wren at the Edge (bible 7.0, NAR-03): when the smudges come out at dusk she goes to the
    /// nearest, strikes it only while it is drawn (the Smudge rule she has just taught), and leaves the last one to
    /// Wren, stepping back to say so. Then she goes back to where she stood, for the walk into the white. She is a
    /// teacher, not a turret: slow, one stroke at a time, never at a smudge that is not there to be hit.
    /// </summary>
    public sealed class IsoldeAlly : MonoBehaviour
    {
        [SerializeField] Enemy[] _targets = Array.Empty<Enemy>();
        [SerializeField] float _speed = 3.2f;
        [SerializeField] float _reach = 1.7f;
        [Tooltip("Seconds between her strokes.")]
        [SerializeField] float _strokeEvery = 1.4f;
        [SerializeField] int _damage = 1;

        Vector3 _post;
        float _cooldown;
        bool _yielded;

        /// <summary>Strokes she has landed.</summary>
        public int Strokes { get; private set; }
        /// <summary>She has stepped back and left the last smudge to Wren.</summary>
        public bool Yielded => _yielded;
        public Enemy Target { get; private set; }
        public static event Action<IsoldeAlly, Enemy> Struck;

        public void Configure(Enemy[] targets) { _targets = targets ?? Array.Empty<Enemy>(); }

        void Awake() { _post = transform.position; }

        void Update()
        {
            int alive = 0;
            Enemy nearest = null;
            float best = float.MaxValue;
            Vector2 at = transform.position;
            foreach (var e in _targets)
            {
                if (e == null || !e.isActiveAndEnabled || e.IsDead || e.IsDying) continue;
                alive++;
                float d = Mathf.Abs(e.transform.position.x - at.x);
                if (d < best) { best = d; nearest = e; }
            }
            _cooldown -= Time.deltaTime;

            if (alive == 0) { Target = null; Walk(_post.x); return; }
            if (alive == 1)
            {
                // The last one is Wren's.
                Target = null;
                if (!_yielded)
                {
                    _yielded = true;
                    Captions.Show(Loc.T("caption.isolde.yours", "Isolde: \"That one's yours.\""), 2.5f);
                }
                Walk(_post.x);
                return;
            }

            Target = nearest;
            float tx = nearest.transform.position.x;
            if (Mathf.Abs(tx - at.x) > _reach * 0.8f) { Walk(tx); return; }
            if (_cooldown > 0f) return;
            if (nearest is Smudge s && !s.IsDrawn) return;   // nothing there to hit: wait for the ink
            var dir = new Vector2(Mathf.Sign(tx - at.x), 0.2f).normalized;
            if (nearest.TakeHit(new HitInfo { Damage = _damage, Direction = dir, Source = gameObject }))
            {
                Strokes++;
                Struck?.Invoke(this, nearest);
            }
            _cooldown = _strokeEvery;
        }

        void Walk(float x)
        {
            var p = transform.position;
            if (Mathf.Abs(x - p.x) < 0.05f) return;
            p.x = Mathf.MoveTowards(p.x, x, _speed * Time.deltaTime);
            transform.position = p;
        }
    }
}
