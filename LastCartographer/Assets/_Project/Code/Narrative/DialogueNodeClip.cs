#nullable enable
using OWSBG.World;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace OWSBG.Narrative
{
    /// <summary>
    /// Timeline clip: start a Yarn node. With WaitForCompletion the cutscene holds its time where the clip
    /// begins until the conversation ends, then carries on (Timeline → Yarn hook, PRG-16).
    /// </summary>
    public sealed class DialogueNodeClip : PlayableAsset, ITimelineClipAsset
    {
        public string Node = "";
        public bool WaitForCompletion = true;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<DialogueNodeBehaviour>.Create(graph);
            var b = playable.GetBehaviour();
            b.Node = Node; b.Wait = WaitForCompletion;
            return playable;
        }
    }

    public sealed class DialogueNodeBehaviour : PlayableBehaviour
    {
        public string Node = "";
        public bool Wait;

        bool _started, _done;
        Cutscene? _cutscene;
        DialogueService? _service;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_started) return;
            _started = true;
            _service = DialogueService.Instance;
            if (_service == null)
            {
                Debug.LogWarning("[OWSBG] DialogueNodeClip '" + Node + "': no DialogueService in the scene");
                return;
            }
            _cutscene = Cutscene.Current;
            if (Wait) _service.Completed += OnDone;
            if (!_service.StartNode(Node))
            {
                if (Wait) _service.Completed -= OnDone;
                return;
            }
            if (Wait && !_done && _cutscene != null) _cutscene.Pause();
        }

        void OnDone()
        {
            _done = true;
            if (_service != null) _service.Completed -= OnDone;
            _cutscene?.Resume();
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_service != null) _service.Completed -= OnDone;
        }
    }
}
