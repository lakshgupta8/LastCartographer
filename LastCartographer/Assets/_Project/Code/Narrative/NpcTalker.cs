using OWSBG.World;
using UnityEngine;

namespace OWSBG.Narrative
{
    /// <summary>An NPC (or inscription) that starts a Yarn node when Wren presses up on it.</summary>
    public sealed class NpcTalker : Interactable
    {
        [SerializeField] string _startNode = "Start";
        [SerializeField] bool _faceWren = true;

        public string StartNode { get => _startNode; set => _startNode = value; }

        public override bool CanInteract(Interactor who) =>
            base.CanInteract(who) && DialogueService.Instance != null && !DialogueService.Instance.IsRunning;

        public override void Interact(Interactor who)
        {
            if (_faceWren)
            {
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (who.transform.position.x < transform.position.x ? -1f : 1f);
                transform.localScale = s;
            }
            DialogueService.Instance?.StartNode(_startNode);
        }
    }
}
