using System;
using Unity.Mathematics;

namespace Tekly.Common.Utils.Tweening
{
    public enum Ease
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InSine,
        OutSine,
        InOutSine,
        InExpo,
        OutExpo,
        InOutExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        InElastic,
        OutElastic,
        InOutElastic,
        InBack,
        OutBack,
        InOutBack,
        InBounce,
        OutBounce,
        InOutBounce,
    }

    public static class Easing
    {
        public static float Evaluate(float t, Ease ease)
        {
            return ease switch {
                Ease.Linear => Linear(t),
                Ease.InQuad => InQuad(t),
                Ease.OutQuad => OutQuad(t),
                Ease.InOutQuad => InOutQuad(t),
                Ease.InCubic => InCubic(t),
                Ease.OutCubic => OutCubic(t),
                Ease.InOutCubic => InOutCubic(t),
                Ease.InQuart => InQuart(t),
                Ease.OutQuart => OutQuart(t),
                Ease.InOutQuart => InOutQuart(t),
                Ease.InQuint => InQuint(t),
                Ease.OutQuint => OutQuint(t),
                Ease.InOutQuint => InOutQuint(t),
                Ease.InSine => InSine(t),
                Ease.OutSine => OutSine(t),
                Ease.InOutSine => InOutSine(t),
                Ease.InExpo => InExpo(t),
                Ease.OutExpo => OutExpo(t),
                Ease.InOutExpo => InOutExpo(t),
                Ease.InCirc => InCirc(t),
                Ease.OutCirc => OutCirc(t),
                Ease.InOutCirc => InOutCirc(t),
                Ease.InElastic => InElastic(t),
                Ease.OutElastic => OutElastic(t),
                Ease.InOutElastic => InOutElastic(t),
                Ease.InBack => InBack(t),
                Ease.OutBack => OutBack(t),
                Ease.InOutBack => InOutBack(t),
                Ease.InBounce => InBounce(t),
                Ease.OutBounce => OutBounce(t),
                Ease.InOutBounce => InOutBounce(t),
                _ => throw new ArgumentOutOfRangeException(nameof(ease), ease, null)
            };
        }

        public static float Linear(float t) => t;

        public static float InQuad(float t) => t * t;
        public static float OutQuad(float t) => 1 - InQuad(1 - t);

        public static float InOutQuad(float t)
        {
            if (t < 0.5) {
                return InQuad(t * 2) / 2;
            }

            return 1 - InQuad((1 - t) * 2) / 2;
        }

        public static float InCubic(float t) => t * t * t;
        public static float OutCubic(float t) => 1 - InCubic(1 - t);

        public static float InOutCubic(float t)
        {
            if (t < 0.5) {
                return InCubic(t * 2) / 2;
            }

            return 1 - InCubic((1 - t) * 2) / 2;
        }

        public static float InQuart(float t) => t * t * t * t;
        public static float OutQuart(float t) => 1 - InQuart(1 - t);

        public static float InOutQuart(float t)
        {
            if (t < 0.5) {
                return InQuart(t * 2) / 2;
            }

            return 1 - InQuart((1 - t) * 2) / 2;
        }

        public static float InQuint(float t) => t * t * t * t * t;
        public static float OutQuint(float t) => 1 - InQuint(1 - t);

        public static float InOutQuint(float t)
        {
            if (t < 0.5) {
                return InQuint(t * 2) / 2;
            }

            return 1 - InQuint((1 - t) * 2) / 2;
        }

        public static float InSine(float t) => -math.cos(t * math.PI / 2);
        public static float OutSine(float t) => math.sin(t * math.PI / 2);
        public static float InOutSine(float t) => (math.cos(t * math.PI) - 1) / -2;

        public static float InExpo(float t) => math.pow(2, 10 * (t - 1));
        public static float OutExpo(float t) => 1 - InExpo(1 - t);

        public static float InOutExpo(float t)
        {
            if (t < 0.5) {
                return InExpo(t * 2) / 2;
            }

            return 1 - InExpo((1 - t) * 2) / 2;
        }

        public static float InCirc(float t) => -(math.sqrt(1 - t * t) - 1);
        public static float OutCirc(float t) => 1 - InCirc(1 - t);

        public static float InOutCirc(float t)
        {
            if (t < 0.5) {
                return InCirc(t * 2) / 2;
            }

            return 1 - InCirc((1 - t) * 2) / 2;
        }

        public static float InElastic(float t) => 1 - OutElastic(1 - t);

        public static float OutElastic(float t)
        {
            float p = 0.3f;
            return math.pow(2, -10 * t) * math.sin((t - p / 4) * (2 * math.PI) / p) + 1;
        }

        public static float InOutElastic(float t)
        {
            if (t < 0.5) {
                return InElastic(t * 2) / 2;
            }

            return 1 - InElastic((1 - t) * 2) / 2;
        }

        public static float InBack(float t)
        {
            float s = 1.70158f;
            return t * t * ((s + 1) * t - s);
        }

        public static float OutBack(float t) => 1 - InBack(1 - t);

        public static float InOutBack(float t)
        {
            if (t < 0.5) {
                return InBack(t * 2) / 2;
            }

            return 1 - InBack((1 - t) * 2) / 2;
        }

        public static float InBounce(float t) => 1 - OutBounce(1 - t);

        public static float OutBounce(float t)
        {
            float div = 2.75f;
            float mult = 7.5625f;

            if (t < 1 / div) {
                return mult * t * t;
            }

            if (t < 2 / div) {
                t -= 1.5f / div;
                return mult * t * t + 0.75f;
            }

            if (t < 2.5 / div) {
                t -= 2.25f / div;
                return mult * t * t + 0.9375f;
            }

            t -= 2.625f / div;
            return mult * t * t + 0.984375f;
        }

        public static float InOutBounce(float t)
        {
            if (t < 0.5) {
                return InBounce(t * 2) / 2;
            }

            return 1 - InBounce((1 - t) * 2) / 2;
        }
    }
}