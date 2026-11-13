using System;
using Tekly.Common.Utils.Tweening;

namespace Tekly.Common.Tweenlines
{
    [Serializable]
    public struct TweenClip<TValue>
    {
        public float Start;
        public float Duration;
        public EaseData Ease;
        public ClipFrom FromMode;
        public ClipSpace Space;
        public TValue From;
        public TValue To;

        public float End => Start + Duration;
    }
}
