using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Tekly.Tweenlines.Tracks
{
    /// <summary>
    /// Moves a Transform's local position. Use AnchoredPositionTrack for UI.
    /// </summary>
    [Serializable]
    [MovedFrom(true, "Tekly.Common.Tweenlines.Tracks", "Tekly.Common.Runtime")]
    public class PositionTrack : Vector3Track<Transform>
    {
        protected override TweenChannel<Vector3> Channel => TweenChannels.LocalPosition;

        protected override TweenClip<Vector3> CreateClip(float start)
        {
            return new TweenClip<Vector3> {
                Start = start,
                Duration = 0.5f,
                FromMode = ClipFrom.Previous,
                Space = ClipSpace.RelativeToOrigin
            };
        }
    }
}
