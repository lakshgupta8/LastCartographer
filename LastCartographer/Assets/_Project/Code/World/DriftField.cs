using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The drift (ENV-08; bible 4.7, PRG-20): islands going past the Hollow's far edge, behind the play plane. As many
    /// of the kit's island drawings show as there are islands drifting in this world (every place she left to the white,
    /// <see cref="Islands.Drifting"/>); they move west and wrap, bobbing a little, so the Blank is visibly built from her
    /// choices before she steps onto one. Nothing drifts for a player who held everything.
    /// </summary>
    public sealed class DriftField : MonoBehaviour
    {
        public float speed = 0.6f;
        /// <summary>Half the width an island travels before it wraps.</summary>
        public float wrap = 46f;
        public float bob = 0.25f;
        public List<Transform> Slabs = new List<Transform>();
        public List<float> Phases = new List<float>();
        public List<float> BaseYs = new List<float>();
        /// <summary>How many islands are shown now, from the world; -1 before the first refresh.</summary>
        public int Showing { get; private set; } = -1;

        float _t;

        void OnEnable() { Refresh(); }

        /// <summary>Read the world again: a place released since shows up as one more island.</summary>
        public void Refresh()
        {
            int n = Mathf.Min(Slabs.Count, Islands.Drifting(GameState.World).Count);
            Showing = n;
            for (int i = 0; i < Slabs.Count; i++) if (Slabs[i] != null) Slabs[i].gameObject.SetActive(i < n);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            for (int i = 0; i < Slabs.Count; i++)
            {
                var tr = Slabs[i];
                if (tr == null || !tr.gameObject.activeSelf) continue;
                var p = tr.localPosition;
                p.x -= speed * dt;
                if (p.x < -wrap) p.x += 2f * wrap;
                float baseY = i < BaseYs.Count ? BaseYs[i] : p.y;
                float phase = i < Phases.Count ? Phases[i] : 0f;
                p.y = baseY + Mathf.Sin(_t * 0.5f + phase) * bob;
                tr.localPosition = p;
            }
        }
    }
}
