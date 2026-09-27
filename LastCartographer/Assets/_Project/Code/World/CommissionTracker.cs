using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// Persistent-scene service that keeps taken commissions moving: re-evaluates steps whenever a flag
    /// changes or a vantage is surveyed, and bumps "kill.&lt;Family&gt;" / "kill.any" counters when an enemy dies.
    /// Fulfilment is a WorldState change, so the journal and the ledger page follow through Commissions.Changed.
    /// </summary>
    public sealed class CommissionTracker : MonoBehaviour
    {
        WorldState _bound;

        void OnEnable()
        {
            CommissionCatalog.EnsureDefaults();
            GameState.Loaded += Rebind;
            Enemy.AnyDied += OnEnemyDied;
            Rebind();
        }

        void OnDisable()
        {
            GameState.Loaded -= Rebind;
            Enemy.AnyDied -= OnEnemyDied;
            Unbind();
        }

        void Rebind()
        {
            Unbind();
            _bound = GameState.World;
            _bound.FlagChanged += OnFlag;
            _bound.VantageSurveyed += OnVantage;
            Commissions.Evaluate(_bound);
        }

        void Unbind()
        {
            if (_bound == null) return;
            _bound.FlagChanged -= OnFlag;
            _bound.VantageSurveyed -= OnVantage;
            _bound = null;
        }

        void OnFlag(string key, int value)
        {
            if (key.StartsWith(Commissions.Prefix)) return;   // our own state changes
            Commissions.Evaluate(_bound);
        }

        void OnVantage(string id) => Commissions.Evaluate(_bound);

        void OnEnemyDied(Enemy e)
        {
            var w = _bound ?? GameState.World;
            Commissions.Bump(w, "kill." + e.Family);
            Commissions.Bump(w, "kill.any");
        }

        /// <summary>Force a pass; returns the ids that became fulfilled.</summary>
        public List<string> Evaluate() => Commissions.Evaluate(_bound ?? GameState.World);
    }
}
