using System;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The last leg of a walk through rooms when it is a fight (bounds-walk.md §2, boss 6.4): at the bottom of Hollowvein
    /// the Collapse is the fourth verse. It wakes once the verses above are walked (its arena waits for the relay's
    /// <see cref="BoundsWalks.Relay.WakeFlag"/>), and the fight won walks the leg, which walks the whole.
    /// </summary>
    public sealed class WalkRelayFight : MonoBehaviour
    {
        [SerializeField] string _relayId = "hollowvein";
        BoundsWalks.Relay _relay;

        public static event Action<BoundsWalks.Relay> Walked;

        public void Configure(string relayId) { _relayId = relayId; _relay = null; }

        void Update()
        {
            _relay ??= BoundsWalks.FindRelay(_relayId);
            if (_relay == null || _relay.FightFlag == null) return;
            var w = GameState.World;
            int leg = _relay.Legs.Length - 1;
            if (!w.Is(_relay.FightFlag) || !BoundsWalks.IsLegDue(w, _relay, leg)) return;
            if (!BoundsWalks.WalkLeg(w, _relay, leg)) return;
            Captions.Show(Loc.F("caption.walked", "Walked. {0} is held.", Loc.T("walk." + _relay.Id + ".name", _relay.Name)), 4f);
            Walked?.Invoke(_relay);
        }
    }
}
