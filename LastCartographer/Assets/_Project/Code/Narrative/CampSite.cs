using OWSBG.Core;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// One of the Long Grass Camp's three sites (PRG-21): the wagons, the fire, Idrenne and a bedroll while the camp
    /// stands here; ashes otherwise, which say the camp has gone on (and which way) or that it camps here some nights.
    /// </summary>
    public sealed class CampSite : MonoBehaviour
    {
        public const string AshesBehindNode = "Camp_Ashes_Behind";
        public const string AshesAheadNode = "Camp_Ashes_Ahead";
        public const string BedrollNode = "Camp_Bedroll";

        /// <summary>The planned room this site is (one of Camp's sites).</summary>
        public string SiteRoom;
        public GameObject CampGroup, AshesGroup;
        public NpcTalker Ashes;

        public bool IsCampHere { get; private set; }
        public int SiteIndex => Camp.SiteOf(SiteRoom);
        float _next;

        void OnEnable() { Camp.Moved += OnMoved; Refresh(); }
        void OnDisable() { Camp.Moved -= OnMoved; }
        void OnMoved(int _) => Refresh();

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.25f;
            Refresh();
        }

        public void Refresh()
        {
            var w = GameState.World;
            bool here = Camp.IsAt(w, SiteRoom);
            if (Ashes != null) Ashes.StartNode = Camp.Site(w) > SiteIndex ? AshesBehindNode : AshesAheadNode;
            IsCampHere = here;
            if (CampGroup != null && CampGroup.activeSelf != here) CampGroup.SetActive(here);
            if (AshesGroup != null && AshesGroup.activeSelf == here) AshesGroup.SetActive(!here);
        }
    }
}
