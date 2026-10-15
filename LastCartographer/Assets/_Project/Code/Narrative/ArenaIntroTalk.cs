using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>
    /// A boss who speaks before the fight (Brann on the Furnace Stair, <c>Stair_Brann</c>): when the arena shuts its doors
    /// the scene plays, and the fight waits until it is over. The scene decides whether there is anything to say (it stops
    /// at once once said, or once the boss is beaten), so a retry after a death goes straight to the fight.
    /// </summary>
    public sealed class ArenaIntroTalk : MonoBehaviour
    {
        [SerializeField] BossArena _arena;
        [SerializeField] string _node = "";

        public string Node => _node;
        public BossArena Arena => _arena;

        public void Configure(BossArena arena, string node) { _arena = arena; _node = node ?? ""; }

        void OnEnable() => BossArena.FightStarted += OnFightStarted;
        void OnDisable()
        {
            BossArena.FightStarted -= OnFightStarted;
            if (_arena != null) _arena.IntroHold = null;
        }

        void OnFightStarted(BossArena arena)
        {
            if (arena != _arena || string.IsNullOrEmpty(_node)) return;
            var svc = DialogueService.Instance;
            if (svc == null || !svc.StartNode(_node)) { arena.IntroHold = null; return; }
            arena.IntroHold = () => svc.IsRunning;
        }
    }
}
