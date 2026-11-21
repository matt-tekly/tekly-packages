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
    /// counts are counts. Elastic has no count: how much each swing shrinks sets how long it keeps swinging. Every ease starts at exactly 0 and ends at exactly 1, so clips land on their end
    /// value.
    /// </summary>
    [Serializable]
    public struct TweenEase
    {
        public const float DEFAULT_BACK_OVERSHOOT = 0.1f;
        public const float DEFAULT_ELASTIC_OVERSHOOT = 0.3f;
        public const int DEFAULT_BOUNCES = 3;
        public const float DEFAULT_BOUNCINESS = 0.25f;

        public const int MAX_COUNT = 16;

        private const float MIN_OVERSHOOT = 0.001f;

        // Past this Elastic barely loses height from one swing to the next, so it would never settle in a clip
        private const float MAX_ELASTIC_OVERSHOOT = 0.9f;
        private const float MAX_FALLOFF = 0.9f;
        private const float MIN_FALLOFF = 0.001f;

        // Elastic's swings have shrunk to 1 percent of the distance when the clip ends. Stored as -ln(0.01).
        // A bigger value stops more abruptly and a smaller one leaves more of the clip with nothing to see
        private const float ELASTIC_END_DECAY = 4.60517f;

        public EaseType Type;
        public EaseDirection Direction;

        [Tooltip("How far Back or Elastic goes past the end, as a fraction of the distance. 0.1 is 10 percent. For Elastic with no Falloff it is also how springy it is: a bigger overshoot swings more times")]
        [Range(MIN_OVERSHOOT, 1)] public float Overshoot;

        [Tooltip("Size of each Elastic swing relative to the one before it. Higher keeps wobbling for longer, so it needs a longer clip. 0 lets the Overshoot decide, the way a real spring behaves")]
        [Range(0, MAX_FALLOFF)] public float Falloff;

        [Tooltip("Number of rebounds after Bounce first lands")]
        [Range(0, MAX_COUNT)] public int Bounces;

        [Tooltip("Height of each Bounce rebound relative to the one before it")]
        [Range(0, 1)] public float Bounciness;

        public TweenEase(EaseType type, EaseDirection direction)
        {
            Type = type;
            Direction = direction;
            Overshoot = 0;
            Falloff = 0;
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
            Falloff = 0;
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
                    // Shifted and scaled from 2^(10(t - 1)) so it is exactly 0 at the start instead of 0.001
                    return (Mathf.Pow(2, 10 * t) - 1) / 1023f;
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
        /// A spring fitted to the clip. It is a cosine swinging across the end inside an envelope that
        /// fades. The swing rate is picked so the swings have just died away as the clip ends, which leaves
        /// little still time at the end however long the clip is.
        /// With no Falloff the envelope fades at one rate, like a damped spring, so each swing is roughly the
        /// overshoot times the one before: a small overshoot goes past once and settles, a large one keeps
        /// wobbling. With a Falloff the envelope fades at one rate on the way in, to make the first swing the
        /// overshoot, and at the Falloff's rate after that.
        /// </summary>
        private float ElasticOut(float t)
        {
            const float quarterTurn = Mathf.PI * 0.5f;

            var overshoot = Mathf.Clamp(Overshoot, MIN_OVERSHOOT, MAX_ELASTIC_OVERSHOOT);
            var target = -Mathf.Log(overshoot);

            // The curve is 1 - envelope * cos(phase), and it first reaches the end at a quarter turn. The
            // envelope is e^(-approach * phase) up to there and fades by e^(-decay * phase) after
            float approach;
            float decay;

            if (Falloff > 0) {
                decay = -Mathf.Log(Mathf.Clamp(Falloff, MIN_FALLOFF, MAX_FALLOFF)) / Mathf.PI;

                // The first peak comes a little before half a turn, at PI - atan(decay). This is the approach
                // rate that leaves the envelope at the overshoot there. It is never negative, which would dip
                // backwards at the start, so a big overshoot with a small Falloff gets a smaller first swing
                var peak = decay * (quarterTurn - Mathf.Atan(decay)) + 0.5f * Mathf.Log(1 + decay * decay);
                approach = Mathf.Max(0, (target - peak) / quarterTurn);
            } else {
                // One rate throughout. The position of the peak depends on the rate, so refine it
                decay = target / Mathf.PI;

                for (var i = 0; i < 3; i++) {
                    decay = (target - 0.5f * Mathf.Log(1 + decay * decay)) / (Mathf.PI - Mathf.Atan(decay));
                }

                approach = decay;
            }

            // How far the swing has turned by the end of the clip, which is when it has died away. A tiny
            // overshoot has already died away before it reaches the end
            var remaining = ELASTIC_END_DECAY - approach * quarterTurn;
            var turn = remaining > 0 ? quarterTurn + remaining / decay : ELASTIC_END_DECAY / approach;
            var phase = turn * t;

            var fade = phase <= quarterTurn ? approach * phase : approach * quarterTurn + decay * (phase - quarterTurn);

            // Eases out what swing is left over the back half of the clip, so it lands exactly and comes to
            // rest gently. It is close to 1 early on, which leaves the first swing at the overshoot
            var t2 = t * t;
            var landing = 1 - t2 * t2;

            return 1 - Mathf.Exp(-fade) * Mathf.Cos(phase) * landing;
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
