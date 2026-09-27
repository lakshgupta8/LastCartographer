using System;
using System.Collections.Generic;
using OWSBG.Core;
using UnityEngine;

namespace OWSBG.World
{
    /// <summary>
    /// The board at a hub (bible 8, PRG-12). Pressing up on it posts whatever the hub's catalogue is ready
    /// to post, then raises Opened so the ledger page can show it. Taking and turning in happen on that page.
    /// </summary>
    public sealed class CommissionLedger : Interactable
    {
        [SerializeField] string _hubId = "Saltmarrow";

        public string HubId { get => _hubId; set => _hubId = value; }

        public static event Action<CommissionLedger> Opened;

        public override void Interact(Interactor who)
        {
            PostAvailable();
            Opened?.Invoke(this);
        }

        /// <summary>Post every unknown commission at this hub whose posting flag (if any) is set. Returns how many.</summary>
        public int PostAvailable()
        {
            return Commissions.PostAvailable(GameState.World, _hubId);
        }

        /// <summary>What the page lists: everything at this hub that has been posted, in catalogue order.</summary>
        public List<CommissionDef> Entries()
        {
            var w = GameState.World;
            var list = new List<CommissionDef>();
            foreach (var d in CommissionCatalog.AtHub(_hubId))
                if (Commissions.StateOf(w, d.Id) != CommissionState.Unknown) list.Add(d);
            return list;
        }
    }
}
