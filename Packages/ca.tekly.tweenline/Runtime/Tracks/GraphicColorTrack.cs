#if TWEENLINE_UGUI
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Tweenlines.Tracks
{
    /// <summary>
    /// Changes a Graphic's color, like an Image's or Text's tint and alpha. Relative values multiply the
    /// rest color, so white is the rest color, a color tints it and an alpha of 0 fades it out. Absolute
    /// values set the color directly.
    /// </summary>
    [Serializable]
    public class GraphicColorTrack : ClipTrack<Graphic, Color>
    {
        protected override TweenChannel<Color> Channel => TweenChannels.GraphicColor;

        protected override Color Lerp(Color a, Color b, float t) => Color.LerpUnclamped(a, b, t);
        protected override Color Add(Color origin, Color offset) => origin * offset;

        protected override TweenClip<Color> CreateClip(float start)
        {
            return new TweenClip<Color> {
                Start = start,
                Duration = 0.3f,
                Ease = new TweenEase(EaseType.Quad, EaseDirection.Out),
                FromMode = ClipFrom.Previous,
                Space = ClipSpace.RelativeToOrigin,
                From = Color.white,
                To = Color.white
            };
        }
    }
}
#endif
