using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace OWSBG.World
{
    /// <summary>Timeline clip: move an actor from one point to another over the clip, facing the way it goes.</summary>
    public sealed class ActorMoveClip : PlayableAsset, ITimelineClipAsset
    {
        public ExposedReference<Transform> Actor;
        public Vector2 From;
        public Vector2 To;
        public bool FaceDirection = true;
        public AnimationCurve Ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ActorMoveBehaviour>.Create(graph);
            var b = playable.GetBehaviour();
            b.Actor = Actor.Resolve(graph.GetResolver());
            b.From = From; b.To = To; b.FaceDirection = FaceDirection; b.Ease = Ease;
            return playable;
        }
    }

    public sealed class ActorMoveBehaviour : PlayableBehaviour
    {
        public Transform Actor;
        public Vector2 From, To;
        public bool FaceDirection;
        public AnimationCurve Ease;

        public override void OnBehaviourPlay(Playable playable, FrameData info) { Apply(0f); }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            double d = playable.GetDuration();
            Apply(d > 0 ? Mathf.Clamp01((float)(playable.GetTime() / d)) : 1f);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            // Leaving the clip past its end: land exactly on the target.
            if (playable.GetTime() >= playable.GetDuration() - 1e-4) Apply(1f);
        }

        void Apply(float t)
        {
            if (Actor == null) return;
            float k = Ease != null && Ease.length > 0 ? Ease.Evaluate(t) : t;
            var p = Vector2.LerpUnclamped(From, To, k);
            var pos = Actor.position;
            Actor.position = new Vector3(p.x, p.y, pos.z);
            if (FaceDirection && Mathf.Abs(To.x - From.x) > 0.01f)
            {
                var s = Actor.localScale;
                s.x = Mathf.Abs(s.x) * (To.x >= From.x ? 1f : -1f);
                Actor.localScale = s;
            }
        }
    }
}
