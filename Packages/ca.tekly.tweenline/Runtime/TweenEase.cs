using System;
using UnityEngine;

namespace Tekly.Tweenlines
{
    public enum EaseType
    {
        Linear,
        Sine,
        Quad,
        Cubic,
        Quart,
        Quint,
        Expo,
        Circ,
        Back,
        Elastic,
        Bounce
    }

    /// <summary>
    /// Which end of the clip the ease shapes. Out is first so a new ease defaults to it, since UI motion
    /// almost always wants to arrive gently.
    /// </summary>
    public enum EaseDirection
    {
        /// <summary>Starts fast and settles into the end</summary>
        Out,

        /// <summary>Starts slow and speeds into the end</summary>
        In,

        /// <summary>Slow at both ends</summary>
        InOut
    }

    /// <summary>
    /// How a clip moves from its start to its end. The type picks the shape and the direction picks which
    /// end it shapes. Settings are in units you can see: Overshoot is a fraction of the distance travelled,
    /// counts are counts. Every ease starts at exactly 0 and ends at exactly 1, so clips land on their end
    /// value.
    /// </summary>
    [Serializable]
    public struct TweenEase
    {
        public const float DEFAULT_BACK_OVERSHOOT = 0.1f;
        public const float DEFAULT_ELASTIC_OVERSHOOT = 0.3f;
        public const int DEFAULT_OSCILLATIONS = 3;
        public const int DEFAULT_BOUNCES = 3;
        public const float DEFAULT_BOUNCINESS = 0.25f;

        public const int MAX_COUNT = 16;

        private const float MIN_OVERSHOOT = 0.001f;

        public EaseType Type;
        public EaseDirection Direction;

        [Tooltip("How far Back or Elastic goes past the end, as a fraction of the distance. 0.1 is 10 percent")]
        [Range(MIN_OVERSHOOT, 1)] public float Overshoot;

        [Tooltip("Number of times Elastic swings across the end before it settles")]
        [Range(1, MAX_COUNT)] public int Oscillations;

        [Tooltip("Number of rebounds after Bounce first lands")]
        [Range(0, MAX_COUNT)] public int Bounces;

        [Tooltip("Height of each Bounce rebound relative to the one before it")]
        [Range(0, 1)] public float Bounciness;

        public TweenEase(EaseType type, EaseDirection direction)
        {
            Type = type;
            Direction = direction;
            Overshoot = 0;
            Oscillations = 0;
            Bounces = 0;
            Bounciness = 0;

            ResetSettings();
        }

        /// <summary>
        /// Puts the settings back to the defaults for the current type.
        /// </summary>
        public void ResetSettings()
        {
            Overshoot = Type == EaseType.Elastic ? DEFAULT_ELASTIC_OVERSHOOT : DEFAULT_BACK_OVERSHOOT;
            Oscillations = DEFAULT_OSCILLATIONS;
            Bounces = DEFAULT_BOUNCES;
            Bounciness = DEFAULT_BOUNCINESS;
        }

        /// <summary>
        /// The eased progress for a linear progress from 0 to 1.
        /// </summary>
        public float Evaluate(float t)
        {
            if (t <= 0) {
                return 0;
            }

            if (t >= 1) {
                return 1;
            }

            switch (Direction) {
                case EaseDirection.In:
                    return EaseIn(t);
                case EaseDirection.Out:
                    return 1 - EaseIn(1 - t);
                default:
                    return t < 0.5f ? EaseIn(t * 2) * 0.5f : 1 - EaseIn(2 - t * 2) * 0.5f;
            }
        }

        /// <summary>
        /// The In form of the type. Out and InOut are built from it.
        /// </summary>
        private float EaseIn(float t)
        {
            switch (Type) {
                case EaseType.Sine:
                    return 1 - Mathf.Cos(t * Mathf.PI * 0.5f);
                case EaseType.Quad:
                    return t * t;
                case EaseType.Cubic:
                    return t * t * t;
                case EaseType.Quart:
                    return t * t * t * t;
                case EaseType.Quint:
                    return t * t * t * t * t;
                case EaseType.Expo:
                    return Mathf.Pow(2, 10 * (t - 1));
                case EaseType.Circ:
                    return 1 - Mathf.Sqrt(1 - t * t);
                case EaseType.Back:
                    var s = BackConstant(Overshoot);
                    return t * t * ((s + 1) * t - s);
                case EaseType.Elastic:
                    return 1 - ElasticOut(1 - t);
                case EaseType.Bounce:
                    return 1 - BounceOut(1 - t);
                default:
                    return t;
            }
        }

        /// <summary>
        /// The classic Back constant that dips by the overshoot. Back's lowest point is -4s³ / 27(s + 1)²,
        /// so this solves that for s with a few Newton steps. 0.1 gives the familiar 1.70158.
        /// </summary>
        private static float BackConstant(float overshoot)
        {
            var o = Mathf.Max(MIN_OVERSHOOT, overshoot);
            var s = Mathf.Pow(6.75f * o, 1f / 3f) + 6.75f * o;

            for (var i = 0; i < 5; i++) {
                var f = 4 * s * s * s - 27 * o * (s + 1) * (s + 1);
                var df = 12 * s * s - 54 * o * (s + 1);
                s -= f / df;
            }

            return s;
        }

        /// <summary>
        /// A cosine swinging across the end inside an envelope that closes at t = 1, so it lands exactly. The
        /// envelope's power is picked so the first swing past the end is the overshoot.
        /// </summary>
        private float ElasticOut(float t)
        {
            var oscillations = Mathf.Clamp(Oscillations, 1, MAX_COUNT);
            var overshoot = Mathf.Clamp(Overshoot, MIN_OVERSHOOT, 1);

            var frequency = 2 * Mathf.PI * oscillations;

            // The first peak is near t = 1 / 2n. The envelope pulls it earlier, so refine where it is and the
            // power that makes its height the overshoot
            var peak = 0.5f / oscillations;
            var power = Mathf.Log(overshoot) / Mathf.Log(1 - peak);

            for (var i = 0; i < 3; i++) {
                var angle = Mathf.PI - Mathf.Atan(power / (frequency * (1 - peak)));
                peak = angle / frequency;
                power = Mathf.Log(overshoot / -Mathf.Cos(angle)) / Mathf.Log(1 - peak);
            }

            return 1 - Mathf.Pow(1 - t, power) * Mathf.Cos(frequency * t);
        }

        /// <summary>
        /// A fall followed by rebounds. Each rebound reaches Bounciness of the previous height and, like
        /// real gravity, its air time scales with the square root of its height. 3 bounces at 0.25 matches
        /// the classic bounce ease.
        /// </summary>
        private float BounceOut(float t)
        {
            var bounces = Mathf.Clamp(Bounces, 0, MAX_COUNT);
            var ratio = Mathf.Sqrt(Mathf.Clamp01(Bounciness));

            // The fall takes 1 unit of time and a rebound with half width w takes 2w
            var total = 1f;
            var width = 1f;
            for (var i = 0; i < bounces; i++) {
                width *= ratio;
                total += 2 * width;
            }

            var x = t * total;

            if (x < 1) {
                return x * x;
            }

            x -= 1;
            width = 1f;

            for (var i = 0; i < bounces; i++) {
                width *= ratio;

                if (x < 2 * width) {
                    var u = x - width;
                    return 1 - width * width + u * u;
                }

                x -= 2 * width;
            }

            return 1;
        }
    }
}
