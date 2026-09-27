using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace OWSBG.World
{
    /// <summary>Timeline clip: drive the screen fade from one level to another over the clip.</summary>
    public sealed class PaperFadeClip : PlayableAsset, ITimelineClipAsset
    {
        [Range(0f, 1f)] public float From = 1f;
        [Range(0f, 1f)] public float To = 0f;
        public Color Color = new Color(0.96f, 0.93f, 0.85f);

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<PaperFadeBehaviour>.Create(graph);
            var b = playable.GetBehaviour();
            b.From = From; b.To = To; b.Color = Color;
            return playable;
        }
    }

    public sealed class PaperFadeBehaviour : PlayableBehaviour
    {
        public float From, To;
        public Color Color;

        public override void OnBehaviourPlay(Playable playable, FrameData info) { ScreenFade.Set(From, Color); }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            double d = playable.GetDuration();
            float t = d > 0 ? Mathf.Clamp01((float)(playable.GetTime() / d)) : 1f;
            ScreenFade.Set(Mathf.Lerp(From, To, t), Color);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (playable.GetTime() >= playable.GetDuration() - 1e-4) ScreenFade.Set(To, Color);
        }
    }
}
