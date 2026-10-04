using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Wren's own ink (ENV-12): the Bind redraws her outline (a pen line traced round her, following her while it
    /// draws), and a hit splashes on her. Both are InkFx clips; without them nothing happens, as before.
    /// </summary>
    [RequireComponent(typeof(WrenVitals))]
    public sealed class WrenFx : MonoBehaviour
    {
        public const float Centre = 0.6f;

        WrenVitals _vitals;
        public int Redraws { get; private set; }

        void Awake() { _vitals = GetComponent<WrenVitals>(); }

        void OnEnable()
        {
            _vitals.Bound += OnBound;
            _vitals.Hurt += OnHurt;
        }

        void OnDisable()
        {
            _vitals.Bound -= OnBound;
            _vitals.Hurt -= OnHurt;
        }

        void OnBound()
        {
            if (InkFx.Spawn("redraw", (Vector2)transform.position + Vector2.up * Centre, 0f, 1f, transform) != null) Redraws++;
        }

        void OnHurt()
        {
            InkFx.Spawn("splash", (Vector2)transform.position + Vector2.up * Centre, Random.Range(0f, 360f), 0.8f);
        }
    }
}
