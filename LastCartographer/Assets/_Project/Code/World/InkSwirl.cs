using System.Collections.Generic;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// An updraft drawn as the clans draw the wind (ENV-07, bible 4.5: "updrafts drawn as visible ink-swirls"): the
    /// kit's one-turn ribbon stacked up the column, rising through it and wrapping from the top back to the foot, with a
    /// slow sway. The lift itself is the <see cref="Updraft"/> beside it; this is only the ink.
    /// </summary>
    public sealed class InkSwirl : MonoBehaviour
    {
        [Tooltip("The column's height, in units.")]
        public float height = 10f;
        [Tooltip("Each ribbon's height, in units (the kit's Prop_Swirl).")]
        public float segment = 2f;
        [Tooltip("How fast the ink rises, in units a second.")]
        public float rise = 1.8f;
        [Tooltip("How far the column sways, in units.")]
        public float sway = 0.12f;

        public List<Transform> Ribbons = new List<Transform>();

        float _t;
        public float Travelled => _t;

        void Update()
        {
            _t += Time.deltaTime * rise;
            for (int i = 0; i < Ribbons.Count; i++)
            {
                var r = Ribbons[i];
                if (r == null) continue;
                float y = Mathf.Repeat(i * segment + _t, height) - height * 0.5f + segment * 0.5f;
                float x = Mathf.Sin(Time.time * 0.9f + i * 0.8f) * sway;
                var p = r.localPosition;
                r.localPosition = new Vector3(x, y, p.z);
            }
        }
    }
}
