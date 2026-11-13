using System;
using UnityEngine;

namespace Tekly.Common.Tweenlines.Tracks
{
    /// <summary>
    /// Scales a Transform. Relative values multiply the origin scale, so (1, 1, 1) is the rest scale.
    /// </summary>
    [Serializable]
    public class ScaleTrack : Vector3Track<Transform>
    {
        protected override TweenChannel<Vector3> Channel => TweenChannels.LocalScale;

        protected override Vector3 Add(Vector3 origin, Vector3 offset) => Vector3.Scale(origin, offset);

        protected override TweenClip<Vector3> CreateClip(float start)
        {
            return new TweenClip<Vector3> {
                Start = start,
                Duration = 0.5f,
                FromMode = ClipFrom.Previous,
                Space = ClipSpace.RelativeToOrigin,
                From = Vector3.one,
                To = Vector3.one
            };
        }
    }
}
