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

        /// <summary>
        /// The talker whose node is running, from the press until the dialogue completes (the animator's talk clip,
        /// CHR-11). Settable, so a cutscene or a test can name the speaker.
        /// </summary>
        public static NpcTalker Talking { get; set; }

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
            var service = DialogueService.Instance;
            if (service != null && service.StartNode(_startNode))
            {
                Talking = this;
                service.Completed += OnCompleted;
            }
        }

        void OnCompleted()
        {
            if (Talking == this) Talking = null;
            if (DialogueService.Instance != null) DialogueService.Instance.Completed -= OnCompleted;
        }

        void OnDisable()
        {
            if (Talking == this) Talking = null;
        }
    }
}
