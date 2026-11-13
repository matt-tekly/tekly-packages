using System;
using UnityEngine;

namespace Tekly.Common.Tweenlines.Tracks
{
    /// <summary>
    /// Moves a RectTransform's anchored position, so movement stays relative to its anchors.
    /// </summary>
    [Serializable]
    public class AnchoredPositionTrack : Vector2Track<RectTransform>
    {
        protected override TweenChannel<Vector2> Channel => TweenChannels.AnchoredPosition;

        protected override TweenClip<Vector2> CreateClip(float start)
        {
            return new TweenClip<Vector2> {
                Start = start,
                Duration = 0.5f,
                FromMode = ClipFrom.Previous,
                Space = ClipSpace.RelativeToOrigin
            };
        }
    }
}
