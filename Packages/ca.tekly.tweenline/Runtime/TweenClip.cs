using System;

namespace Tekly.Tweenlines
{
    [Serializable]
    public struct TweenClip<TValue>
    {
        public float Start;
        public float Duration;
        public TweenEase Ease;
        public ClipFrom FromMode;
        public ClipSpace Space;
        public TValue From;
        public TValue To;

        public float End => Start + Duration;
    }
}
