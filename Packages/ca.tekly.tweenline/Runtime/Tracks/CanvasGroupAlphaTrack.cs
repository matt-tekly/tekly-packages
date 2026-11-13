using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Tekly.Tweenlines.Tracks
{
    /// <summary>
    /// Fades a CanvasGroup's alpha. Relative values add onto the rest alpha.
    /// </summary>
    [Serializable]
    [MovedFrom(true, "Tekly.Common.Tweenlines.Tracks", "Tekly.Common.Runtime")]
    public class CanvasGroupAlphaTrack : ClipTrack<CanvasGroup, float>
    {
        protected override TweenChannel<float> Channel => TweenChannels.CanvasGroupAlpha;

        protected override float Lerp(float a, float b, float t) => Mathf.LerpUnclamped(a, b, t);
        protected override float Add(float origin, float offset) => origin + offset;

        protected override TweenClip<float> CreateClip(float start)
        {
            return new TweenClip<float> {
                Start = start,
                Duration = 0.3f,
                FromMode = ClipFrom.Previous,
                Space = ClipSpace.Absolute,
                From = 0,
                To = 1
            };
        }
    }
}
