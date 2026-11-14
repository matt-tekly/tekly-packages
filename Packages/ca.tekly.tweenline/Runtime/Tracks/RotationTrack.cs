using System;
using UnityEngine;

namespace Tekly.Tweenlines.Tracks
{
    public enum RotationInterpolation
    {
        /// <summary>Blends each angle, so 0 to 360 is a full spin. Good for UI and spins</summary>
        Euler,

        /// <summary>Takes the shortest turn between the two orientations. Good for 3D objects turning to face a way</summary>
        Shortest
    }

    /// <summary>
    /// Rotates a Transform in local Euler degrees. Relative values add to the rest rotation, so a To of
    /// (0, 0, 360) spins once and returns. Values past 360 keep spinning with Euler interpolation.
    /// </summary>
    [Serializable]
    public class RotationTrack : Vector3Track<Transform>
    {
        public RotationInterpolation Interpolation {
            get => m_interpolation;
            set => m_interpolation = value;
        }

        protected override TweenChannel<Vector3> Channel => TweenChannels.LocalRotation;

        [Tooltip("Euler blends each angle so values past 360 spin. Shortest takes the shortest turn between orientations")]
        [SerializeField] private RotationInterpolation m_interpolation = RotationInterpolation.Euler;

        protected override Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            if (m_interpolation == RotationInterpolation.Shortest) {
                return Quaternion.SlerpUnclamped(Quaternion.Euler(a), Quaternion.Euler(b), t).eulerAngles;
            }

            return Vector3.LerpUnclamped(a, b, t);
        }

        protected override TweenClip<Vector3> CreateClip(float start)
        {
            return new TweenClip<Vector3> {
                Start = start,
                Duration = 0.5f,
                Ease = new TweenEase(EaseType.Cubic, EaseDirection.Out),
                FromMode = ClipFrom.Previous,
                Space = ClipSpace.RelativeToOrigin
            };
        }
    }
}
