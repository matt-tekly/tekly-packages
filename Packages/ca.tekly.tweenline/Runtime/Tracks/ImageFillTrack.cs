#if TWEENLINE_UGUI
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Tweenlines.Tracks
{
    /// <summary>
    /// Fills an Image, like a progress bar or a radial timer. The Image's Type must be Filled. Relative values
    /// add onto the rest fill amount.
    /// </summary>
    [Serializable]
    public class ImageFillTrack : ClipTrack<Image, float>
    {
        protected override TweenChannel<float> Channel => TweenChannels.ImageFillAmount;

        protected override float Lerp(float a, float b, float t) => Mathf.LerpUnclamped(a, b, t);
        protected override float Add(float origin, float offset) => origin + offset;

        protected override TweenClip<float> CreateClip(float start)
        {
            return new TweenClip<float> {
                Start = start,
                Duration = 0.5f,
                Ease = new TweenEase(EaseType.Cubic, EaseDirection.Out),
                FromMode = ClipFrom.Value,
                Space = ClipSpace.Absolute,
                From = 0,
                To = 1
            };
        }

#if UNITY_EDITOR
        public override string GetWarning(Tweenline owner)
        {
            if (m_target != null && m_target.type != Image.Type.Filled) {
                return "The Image's Type isn't Filled, so changing its fill amount does nothing.";
            }

            return null;
        }
#endif
    }
}
#endif
